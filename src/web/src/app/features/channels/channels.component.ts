import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService, ChannelDto } from '../../core/api.service';
import { AuthService } from '../../core/auth.service';
import { ChannelDrawerComponent } from './channel-drawer.component';
import { PageHeaderComponent } from '../../shared/layout/page-header.component';
import { PageToolbarComponent } from '../../shared/layout/page-toolbar.component';

@Component({
  selector: 'app-channels',
  standalone: true,
  imports: [CommonModule, FormsModule, ChannelDrawerComponent, PageHeaderComponent, PageToolbarComponent],
  host: { class: 'block w-full' },
  template: `
    <div class="cf-page-container space-y-4 text-xs">
      
      <!-- Canonical Page Header -->
      <app-page-header 
        title="Registro de Canales Editoriales" 
        subtitle="Gestión del portafolio multicanal, nichos temáticos y configuración de distribución"
        [badge]="channels().length"
        badgeSeverity="info">
        <div actions class="flex items-center gap-2">
          <button (click)="openCreateChannel()" *ngIf="authService.isTechnical()" 
                  class="cf-btn-primary">
            <i class="pi pi-plus text-xs"></i> <span>Nuevo Canal</span>
          </button>
        </div>
      </app-page-header>

      <!-- Canonical Page Toolbar -->
      <app-page-toolbar>
        <div start class="flex items-center gap-2 flex-wrap flex-1">
          <div class="relative min-w-[220px] flex-1 sm:max-w-xs">
            <input type="text" [(ngModel)]="searchTerm" placeholder="Filtrar canales por nombre, slug, nicho..." 
                   class="cf-toolbar-control w-full pl-8" />
            <i class="pi pi-search absolute left-2.5 top-2.5 text-[var(--app-muted)] text-xs"></i>
          </div>
        </div>
      </app-page-toolbar>

      <!-- Channels Data Table -->
      <div class="cf-card overflow-hidden">
        <div class="overflow-x-auto">
          <table class="cf-table">
            <thead>
              <tr>
                <th>Channel</th>
                <th>Slug</th>
                <th>Language</th>
                <th>Editorial Niche</th>
                <th>Status</th>
                <th>Registered</th>
                <th class="text-right">Actions</th>
              </tr>
            </thead>
            <tbody>
              <tr *ngFor="let ch of filteredChannels()" class="hover:bg-[var(--app-surface-hover)] transition-colors">
                <td class="py-3 px-4 font-bold text-[var(--app-text)]">{{ ch.name }}</td>
                <td class="py-3 px-4 font-mono text-[var(--app-muted)] text-[11px]">{{ ch.slug }}</td>
                <td class="py-3 px-4">
                  <span class="px-2 py-0.5 rounded bg-blue-500/15 border border-blue-500/30 text-blue-600 dark:text-blue-400 font-bold uppercase text-[10px] font-mono">
                    {{ ch.language }}
                  </span>
                </td>
                <td class="py-3 px-4 text-[var(--app-muted)] max-w-xs truncate">{{ ch.niche }}</td>
                <td class="py-3 px-4">
                  <span class="px-2.5 py-0.5 rounded text-[10px] font-bold uppercase tracking-wider border"
                        [ngClass]="{
                          'bg-amber-500/15 text-amber-600 dark:text-amber-400 border-amber-500/30': ch.status === 'pilot',
                          'bg-emerald-500/15 text-emerald-600 dark:text-emerald-400 border-emerald-500/30': ch.status === 'active',
                          'bg-blue-500/15 text-blue-600 dark:text-blue-400 border-blue-500/30': ch.status === 'scaling',
                          'bg-slate-500/15 text-slate-500 border-slate-500/30': ch.status === 'paused' || ch.status === 'archived'
                        }">
                    {{ ch.status }}
                  </span>
                </td>
                <td class="py-3 px-4 text-[var(--app-muted)] text-[11px] font-mono">{{ ch.createdAtUtc | date:'yyyy-MM-dd' }}</td>
                <td class="py-3 px-4 text-right">
                  <div class="flex items-center justify-end gap-1">
                    <button (click)="openEditChannel(ch)" *ngIf="authService.isTechnical()" 
                            class="p-1.5 rounded-md hover:bg-[var(--app-bg)] text-blue-600 dark:text-blue-400 transition-colors cursor-pointer" title="Edit channel">
                      <i class="pi pi-pencil text-xs"></i>
                    </button>
                    <button (click)="deleteChannel(ch)" *ngIf="authService.isTechnical()" 
                            class="p-1.5 rounded-md hover:bg-[var(--app-bg)] text-rose-500 transition-colors cursor-pointer" title="Delete channel">
                      <i class="pi pi-trash text-xs"></i>
                    </button>
                  </div>
                </td>
              </tr>
              <tr *ngIf="filteredChannels().length === 0">
                <td colspan="7" class="py-8 text-center text-[var(--app-muted)]">
                  No channels match the filter criteria.
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>

      <!-- Channel Drawer Modal -->
      <app-channel-drawer 
        [visible]="isDrawerVisible" 
        [channel]="selectedChannel"
        (onClose)="isDrawerVisible = false"
        (onSaved)="handleChannelSaved($event)">
      </app-channel-drawer>
    </div>
  `
})
export class ChannelsComponent implements OnInit {
  private readonly api = inject(ApiService);
  readonly authService = inject(AuthService);

  readonly channels = signal<ChannelDto[]>([]);
  searchTerm: string = '';

  isDrawerVisible: boolean = false;
  selectedChannel: ChannelDto | null = null;

  ngOnInit(): void {
    this.loadChannels();
  }

  loadChannels(): void {
    this.api.getChannels().subscribe(data => this.channels.set(data));
  }

  filteredChannels(): ChannelDto[] {
    const term = this.searchTerm.trim().toLowerCase();
    if (!term) return this.channels();
    return this.channels().filter(c => 
      c.name.toLowerCase().includes(term) || 
      c.slug.toLowerCase().includes(term) ||
      c.niche.toLowerCase().includes(term) ||
      c.status.toLowerCase().includes(term)
    );
  }

  openCreateChannel(): void {
    this.selectedChannel = null;
    this.isDrawerVisible = true;
  }

  openEditChannel(channel: ChannelDto): void {
    this.selectedChannel = channel;
    this.isDrawerVisible = true;
  }

  deleteChannel(channel: ChannelDto): void {
    if (confirm(`Delete channel '${channel.name}'?`)) {
      this.api.deleteChannel(channel.id).subscribe(() => this.loadChannels());
    }
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
        this.loadChannels();
      });
    } else {
      this.api.updateChannel(channel.id, {
        name: channel.name,
        language: channel.language,
        niche: channel.niche,
        status: channel.status
      }).subscribe(() => {
        this.isDrawerVisible = false;
        this.loadChannels();
      });
    }
  }
}
