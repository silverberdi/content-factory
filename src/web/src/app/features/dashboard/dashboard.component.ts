import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterModule } from '@angular/router';
import { ApiService, DashboardSummaryDto, ChannelDto } from '../../core/api.service';
import { FactoryHealthWidgetComponent } from './factory-health-widget.component';
import { ChannelSummaryWidgetComponent } from './channel-summary-widget.component';
import { AttentionWidgetComponent } from './attention-widget.component';
import { DiscoverySummaryWidgetComponent } from './discovery-summary-widget.component';
import { ContentPipelineSummaryWidgetComponent } from './content-pipeline-widget.component';
import { ChannelDrawerComponent } from '../channels/channel-drawer.component';
import { QuickSubmitModalComponent } from '../discovery/quick-submit-modal.component';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    FactoryHealthWidgetComponent,
    ChannelSummaryWidgetComponent,
    AttentionWidgetComponent,
    DiscoverySummaryWidgetComponent,
    ContentPipelineSummaryWidgetComponent,
    ChannelDrawerComponent,
    QuickSubmitModalComponent
  ],
  template: `
    <div class="space-y-4 max-w-full">
      <!-- Cockpit Header Strip -->
      <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-3 pb-3 border-b border-[var(--app-card-border)]">
        <div>
          <h1 class="text-base sm:text-lg font-bold tracking-tight text-[var(--app-text)]">Operations Dashboard</h1>
          <p class="text-xs text-[var(--app-muted)]">Real-time status of content factory pipelines, active channel registry, and operational attention queue.</p>
        </div>
        
        <div class="flex items-center gap-2.5">
          <button (click)="refresh()" 
                  class="px-3 py-1.5 rounded-lg border border-[var(--app-card-border)] bg-[var(--app-card-bg)] hover:bg-[var(--app-surface-hover)] text-xs font-semibold transition-all flex items-center gap-2 cursor-pointer shadow-2xs">
            <i class="pi pi-refresh" [ngClass]="{ 'animate-spin': isLoading() }"></i> 
            <span>Refresh</span>
          </button>
          <button (click)="isQuickSubmitOpen = true" 
                  class="px-3.5 py-1.5 rounded-lg bg-[var(--app-card-bg)] border border-[var(--app-card-border)] hover:bg-[var(--app-surface-hover)] text-[var(--app-text)] text-xs font-semibold transition-all flex items-center gap-1.5 cursor-pointer shadow-2xs">
            <i class="pi pi-bolt text-amber-500 text-[10px]"></i> <span>Quick Submit</span>
          </button>
          <button (click)="openCreateChannel()" 
                  class="px-3.5 py-1.5 rounded-lg bg-blue-600 hover:bg-blue-700 text-white text-xs font-semibold transition-all flex items-center gap-1.5 shadow-xs cursor-pointer">
            <i class="pi pi-plus text-[10px]"></i> <span>New Channel</span>
          </button>
        </div>
      </div>

      <!-- Main Operational Cockpit Grid -->
      <div class="grid grid-cols-1 lg:grid-cols-12 gap-4">
        <!-- Full-Width Factory Health & Runtime Telemetry -->
        <div class="lg:col-span-12">
          <app-factory-health-widget [health]="summary()?.factoryHealth || null"></app-factory-health-widget>
        </div>

        <!-- Content Pipeline Summary Widget: 6 cols on Desktop -->
        <div class="lg:col-span-6">
          <app-content-pipeline-widget [pipeline]="summary()?.contentPipeline || null"></app-content-pipeline-widget>
        </div>

        <!-- Discovery Summary Widget: 6 cols on Desktop -->
        <div class="lg:col-span-6">
          <app-discovery-summary-widget 
            [summary]="summary()?.discovery || null"
            (onQuickSubmit)="isQuickSubmitOpen = true">
          </app-discovery-summary-widget>
        </div>

        <!-- Channel Summary: 12 cols on Desktop -->
        <div class="lg:col-span-12">
          <app-channel-summary-widget 
            [channels]="summary()?.channels || []"
            (onCreateChannel)="openCreateChannel()"
            (onSelectChannel)="editChannel($event)"
            (onViewAll)="goToChannels()">
          </app-channel-summary-widget>
        </div>

        <!-- Attention & Actions: 12 cols on Desktop -->
        <div class="lg:col-span-12">
          <app-attention-widget [items]="summary()?.attentionItems || []"></app-attention-widget>
        </div>
      </div>

      <!-- Channel Creation / Edit Slide-over Drawer -->
      <app-channel-drawer 
        [visible]="isDrawerVisible" 
        [channel]="selectedChannel"
        (onClose)="isDrawerVisible = false"
        (onSaved)="handleChannelSaved($event)">
      </app-channel-drawer>

      <!-- Quick Submit Modal -->
      <app-quick-submit-modal
        [isOpen]="isQuickSubmitOpen"
        [channels]="summary()?.channels || []"
        (onClose)="isQuickSubmitOpen = false"
        (onSubmitted)="onQuickSubmitted()">
      </app-quick-submit-modal>
    </div>
  `
})
export class DashboardComponent implements OnInit {
  private readonly api = inject(ApiService);
  private readonly router = inject(Router);

  readonly summary = signal<DashboardSummaryDto | null>(null);
  readonly isLoading = signal<boolean>(false);

  isDrawerVisible: boolean = false;
  isQuickSubmitOpen: boolean = false;
  selectedChannel: ChannelDto | null = null;

  ngOnInit(): void {
    this.refresh();
  }

  refresh(): void {
    this.isLoading.set(true);
    this.api.getDashboardSummary().subscribe({
      next: data => {
        this.summary.set(data);
        this.isLoading.set(false);
      },
      error: err => {
        console.error('Error loading dashboard summary', err);
        this.isLoading.set(false);
      }
    });
  }

  openCreateChannel(): void {
    this.selectedChannel = null;
    this.isDrawerVisible = true;
  }

  editChannel(channel: ChannelDto): void {
    this.selectedChannel = channel;
    this.isDrawerVisible = true;
  }

  goToChannels(): void {
    this.router.navigate(['/channels']);
  }

  onQuickSubmitted(): void {
    this.refresh();
  }

  handleChannelSaved(channel: ChannelDto): void {
    if (channel.id === 'new') {
      this.api.createChannel({
        name: channel.name,
        slug: channel.slug,
        language: channel.language,
        niche: channel.niche,
        status: channel.status
      }).subscribe(() => {
        this.isDrawerVisible = false;
        this.refresh();
      });
    } else {
      this.api.updateChannel(channel.id, {
        name: channel.name,
        language: channel.language,
        niche: channel.niche,
        status: channel.status
      }).subscribe(() => {
        this.isDrawerVisible = false;
        this.refresh();
      });
    }
  }
}
