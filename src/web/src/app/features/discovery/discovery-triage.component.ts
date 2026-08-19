import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService, ChannelDto, DiscoveryCandidateDto, DiscoverySummaryDto } from '../../core/api.service';
import { CandidatePreviewDrawerComponent } from './candidate-preview-drawer.component';
import { QuickSubmitModalComponent } from './quick-submit-modal.component';
import { PageHeaderComponent } from '../../shared/layout/page-header.component';
import { PageToolbarComponent } from '../../shared/layout/page-toolbar.component';

@Component({
  selector: 'app-discovery-triage',
  standalone: true,
  imports: [CommonModule, FormsModule, CandidatePreviewDrawerComponent, QuickSubmitModalComponent, PageHeaderComponent, PageToolbarComponent],
  host: { class: 'block w-full' },
  template: `
    <div class="cf-page-container space-y-4 text-xs">
      
      <!-- Canonical Page Header -->
      <app-page-header 
        title="Triage de Candidatos de Discovery" 
        subtitle="Evaluación inicial de leads, filtrado de relevancia y promoción hacia la pipeline editorial"
        [badge]="(summary()?.pendingCandidatesCount ?? 0) + ' Pendientes'"
        [badgeSeverity]="(summary()?.pendingCandidatesCount ?? 0) > 0 ? 'warn' : 'success'">
        <div actions class="flex items-center gap-2">
          <button (click)="isQuickSubmitOpen = true" 
                  class="cf-btn-primary">
            <i class="pi pi-plus text-xs"></i>
            <span>Envío Rápido</span>
          </button>
        </div>
      </app-page-header>

      <!-- Top Operational Summary Chips Bar -->
      <div class="grid grid-cols-2 sm:grid-cols-4 gap-3">
        <!-- Pending Triage Metric -->
        <div class="p-3 rounded-xl border border-[var(--app-card-border)] bg-[var(--app-card-bg)] shadow-2xs flex items-center justify-between">
          <div>
            <span class="text-[10px] font-bold uppercase tracking-wider text-amber-600 dark:text-amber-400 block">Pendientes Triage</span>
            <span class="text-xl font-extrabold text-[var(--app-text)]">{{ summary()?.pendingCandidatesCount ?? 0 }}</span>
          </div>
          <div class="w-8 h-8 rounded-lg bg-amber-500/15 text-amber-600 dark:text-amber-400 flex items-center justify-center font-bold text-xs">
            <i class="pi pi-filter"></i>
          </div>
        </div>

        <!-- Promoted Metric -->
        <div class="p-3 rounded-xl border border-[var(--app-card-border)] bg-[var(--app-card-bg)] shadow-2xs flex items-center justify-between">
          <div>
            <span class="text-[10px] font-bold uppercase tracking-wider text-emerald-600 dark:text-emerald-400 block">Promovidos</span>
            <span class="text-xl font-extrabold text-[var(--app-text)]">{{ summary()?.promotedCandidatesCount ?? 0 }}</span>
          </div>
          <div class="w-8 h-8 rounded-lg bg-emerald-500/15 text-emerald-600 dark:text-emerald-400 flex items-center justify-center font-bold text-xs">
            <i class="pi pi-check"></i>
          </div>
        </div>

        <!-- Dismissed Metric -->
        <div class="p-3 rounded-xl border border-[var(--app-card-border)] bg-[var(--app-card-bg)] shadow-2xs flex items-center justify-between">
          <div>
            <span class="text-[10px] font-bold uppercase tracking-wider text-slate-500 block">Descartados</span>
            <span class="text-xl font-extrabold text-[var(--app-text)]">{{ summary()?.dismissedCandidatesCount ?? 0 }}</span>
          </div>
          <div class="w-8 h-8 rounded-lg bg-slate-500/15 text-slate-500 flex items-center justify-center font-bold text-xs">
            <i class="pi pi-times"></i>
          </div>
        </div>

        <!-- Active Sources Metric -->
        <div class="p-3 rounded-xl border border-[var(--app-card-border)] bg-[var(--app-card-bg)] shadow-2xs flex items-center justify-between">
          <div>
            <span class="text-[10px] font-bold uppercase tracking-wider text-blue-600 dark:text-blue-400 block">Fuentes Activas</span>
            <span class="text-xl font-extrabold text-[var(--app-text)]">{{ summary()?.activeSourcesCount ?? 0 }}</span>
          </div>
          <div class="w-8 h-8 rounded-lg bg-blue-500/15 text-blue-600 dark:text-blue-400 flex items-center justify-center font-bold text-xs">
            <i class="pi pi-compass"></i>
          </div>
        </div>
      </div>

      <!-- Canonical Page Toolbar -->
      <app-page-toolbar>
        <!-- Status Filter Pills -->
        <div start class="flex items-center gap-2 flex-wrap flex-1">
          <button (click)="setStatusFilter('PendingReview')"
                  class="px-2.5 py-1 rounded-lg text-xs font-semibold transition-all cursor-pointer flex items-center gap-1.5 shrink-0"
                  [ngClass]="selectedStatus === 'PendingReview' ? 'bg-amber-500/20 text-amber-600 dark:text-amber-400 border border-amber-500/40 shadow-2xs' : 'text-[var(--app-muted)] hover:text-[var(--app-text)]'">
            <i class="pi pi-clock text-[10px]"></i>
            <span>Pendientes ({{ summary()?.pendingCandidatesCount ?? 0 }})</span>
          </button>

          <button (click)="setStatusFilter('Promoted')"
                  class="px-2.5 py-1 rounded-lg text-xs font-semibold transition-all cursor-pointer flex items-center gap-1.5 shrink-0"
                  [ngClass]="selectedStatus === 'Promoted' ? 'bg-emerald-500/20 text-emerald-600 dark:text-emerald-400 border border-emerald-500/40 shadow-2xs' : 'text-[var(--app-muted)] hover:text-[var(--app-text)]'">
            <i class="pi pi-check-circle text-[10px]"></i>
            <span>Promovidos</span>
          </button>

          <button (click)="setStatusFilter('Dismissed')"
                  class="px-2.5 py-1 rounded-lg text-xs font-semibold transition-all cursor-pointer flex items-center gap-1.5 shrink-0"
                  [ngClass]="selectedStatus === 'Dismissed' ? 'bg-slate-500/20 text-slate-400 border border-slate-500/40 shadow-2xs' : 'text-[var(--app-muted)] hover:text-[var(--app-text)]'">
            <i class="pi pi-ban text-[10px]"></i>
            <span>Descartados</span>
          </button>

          <button (click)="setStatusFilter('')"
                  class="px-2.5 py-1 rounded-lg text-xs font-semibold transition-all cursor-pointer flex items-center gap-1.5 shrink-0"
                  [ngClass]="selectedStatus === '' ? 'bg-blue-500/20 text-blue-600 dark:text-blue-400 border border-blue-500/40 shadow-2xs' : 'text-[var(--app-muted)] hover:text-[var(--app-text)]'">
            <span>Todos</span>
          </button>
        </div>

        <!-- Controls: Channel, Search -->
        <div end class="flex items-center gap-2 flex-wrap">
          <!-- Channel selector -->
          <select [(ngModel)]="selectedChannelId" (change)="loadData()" 
                  class="cf-toolbar-control min-w-[150px]">
            <option value="">Todos los Canales</option>
            <option *ngFor="let ch of channels()" [value]="ch.id">{{ ch.name }}</option>
          </select>

          <!-- Search term -->
          <div class="relative">
            <input type="text" [(ngModel)]="searchTerm" (input)="loadCandidates()" placeholder="Buscar leads..." 
                   class="cf-toolbar-control pl-7 max-w-[170px]" />
            <i class="pi pi-search absolute left-2.5 top-2 text-[var(--app-muted)] text-[10px]"></i>
          </div>
        </div>
      </app-page-toolbar>

      <!-- Triage List / Cards -->
      <div class="space-y-2.5">
        
        <!-- Candidate Card Item -->
        <div *ngFor="let c of candidates(); let idx = index" 
             (click)="openPreview(c, idx)"
             class="p-3.5 rounded-xl border border-[var(--app-card-border)] bg-[var(--app-card-bg)] hover:bg-[var(--app-surface-hover)] hover:border-blue-500/40 transition-all shadow-xs cursor-pointer flex flex-col sm:flex-row sm:items-center justify-between gap-3">
          
          <!-- Left: Title, Metadata, Snippet -->
          <div class="space-y-1.5 flex-1 min-w-0">
            <div class="flex items-center gap-2 flex-wrap text-[11px]">
              <!-- Status Pill -->
              <span class="px-2 py-0.5 rounded text-[10px] font-bold uppercase tracking-wider border font-mono"
                    [ngClass]="{
                      'bg-amber-500/15 text-amber-600 dark:text-amber-400 border-amber-500/30': c.status === 'PendingReview',
                      'bg-emerald-500/15 text-emerald-600 dark:text-emerald-400 border-emerald-500/30': c.status === 'Promoted',
                      'bg-slate-500/15 text-slate-500 border-slate-500/30': c.status === 'Dismissed'
                    }">
                {{ c.status }}
              </span>

              <!-- Channel Tag -->
              <span class="px-2 py-0.5 rounded bg-blue-500/15 text-blue-600 dark:text-blue-400 font-bold text-[10px]">
                {{ c.channelName || 'Canal' }}
              </span>

              <!-- Origin Source -->
              <span class="text-[var(--app-muted)] text-[10px] font-mono flex items-center gap-1">
                <i class="pi" [ngClass]="c.originType === 'Manual' ? 'pi-user' : 'pi-rss'"></i>
                <span>{{ c.sourceName || c.originType }}</span>
              </span>

              <!-- Timestamp -->
              <span class="text-[var(--app-muted)] text-[10px] font-mono">
                {{ c.discoveredAtUtc | date:'MM-dd HH:mm' }}
              </span>
            </div>

            <!-- Title -->
            <h3 class="text-xs sm:text-sm font-bold text-[var(--app-text)] line-clamp-2">
              {{ c.title }}
            </h3>

            <!-- Summary snippet -->
            <p class="text-xs text-[var(--app-muted)] line-clamp-2 leading-relaxed">
              {{ c.summary || c.rawContent || 'Sin resumen provisto.' }}
            </p>

            <!-- External link indicator if present -->
            <div *ngIf="c.externalUrl" class="text-[10px] text-blue-500 dark:text-blue-400 font-mono truncate max-w-md">
              <i class="pi pi-link text-[9px] mr-1"></i>{{ c.externalUrl }}
            </div>
          </div>

          <!-- Right: Quick Triage Buttons -->
          <div class="flex items-center gap-1.5 shrink-0 pt-2 sm:pt-0 border-t sm:border-t-0 border-[var(--app-card-border)] justify-end"
               (click)="$event.stopPropagation()">
            
            <button (click)="openPreview(c, idx)" 
                    class="px-2.5 py-1 rounded-lg border border-[var(--app-card-border)] bg-[var(--app-bg)] hover:bg-[var(--app-surface-hover)] text-xs text-[var(--app-muted)] hover:text-[var(--app-text)] transition-colors cursor-pointer" title="Inspeccionar">
              <i class="pi pi-eye text-[10px] mr-1"></i> Ver
            </button>

            <button *ngIf="c.status === 'PendingReview'" (click)="quickDismiss(c)" 
                    class="px-2.5 py-1 rounded-lg border border-red-500/20 text-red-500 hover:bg-red-500/10 text-xs font-semibold transition-colors cursor-pointer" title="Descartar lead">
              <i class="pi pi-times text-[10px]"></i>
            </button>

            <button *ngIf="c.status === 'PendingReview'" (click)="quickPromote(c)" 
                    class="px-3 py-1 rounded-lg bg-emerald-600 hover:bg-emerald-700 text-white text-xs font-bold transition-all shadow-2xs cursor-pointer flex items-center gap-1" title="Promover a Producción">
              <i class="pi pi-check text-[10px]"></i> Promover
            </button>
          </div>

        </div>

        <!-- Empty state -->
        <div *ngIf="candidates().length === 0" class="py-12 text-center rounded-xl border border-[var(--app-card-border)] bg-[var(--app-card-bg)] text-[var(--app-muted)]">
          <i class="pi pi-check-circle text-3xl mb-2 text-emerald-500/50 block"></i>
          <span class="font-bold text-sm text-[var(--app-text)] block">Triage al día</span>
          <span class="text-xs text-[var(--app-muted)]">No hay candidatos en este estado para los filtros seleccionados.</span>
        </div>

      </div>

      <!-- Slide-over Preview Drawer -->
      <app-candidate-preview-drawer
        [isOpen]="isPreviewOpen"
        [candidate]="activeCandidate"
        (onClose)="isPreviewOpen = false"
        (onTriage)="handleTriage($event)"
        (onNext)="selectNextCandidate()"
        (onPrev)="selectPrevCandidate()">
      </app-candidate-preview-drawer>

      <!-- Quick Submit Modal -->
      <app-quick-submit-modal
        [isOpen]="isQuickSubmitOpen"
        [channels]="channels()"
        [defaultChannelId]="selectedChannelId"
        (onClose)="isQuickSubmitOpen = false"
        (onSubmitted)="onLeadSubmitted()">
      </app-quick-submit-modal>

    </div>
  `
})
export class DiscoveryTriageComponent implements OnInit {
  private readonly api = inject(ApiService);

  candidates = signal<DiscoveryCandidateDto[]>([]);
  channels = signal<ChannelDto[]>([]);
  summary = signal<DiscoverySummaryDto | null>(null);

  selectedStatus = 'PendingReview';
  selectedChannelId = '';
  searchTerm = '';

  isPreviewOpen = false;
  isQuickSubmitOpen = false;
  activeCandidate: DiscoveryCandidateDto | null = null;
  activeCandidateIndex = -1;

  ngOnInit() {
    this.loadChannels();
    this.loadData();
  }

  loadChannels() {
    this.api.getChannels().subscribe({
      next: (data) => this.channels.set(data)
    });
  }

  loadData() {
    this.loadSummary();
    this.loadCandidates();
  }

  loadSummary() {
    this.api.getDiscoverySummary(this.selectedChannelId || undefined).subscribe({
      next: (data) => this.summary.set(data)
    });
  }

  loadCandidates() {
    this.api.getDiscoveryCandidates(
      this.selectedChannelId || undefined,
      this.selectedStatus || undefined,
      undefined,
      this.searchTerm || undefined,
      100
    ).subscribe({
      next: (data) => this.candidates.set(data)
    });
  }

  setStatusFilter(status: string) {
    this.selectedStatus = status;
    this.loadCandidates();
  }

  openPreview(candidate: DiscoveryCandidateDto, index: number) {
    this.activeCandidate = candidate;
    this.activeCandidateIndex = index;
    this.isPreviewOpen = true;
  }

  selectNextCandidate() {
    const list = this.candidates();
    if (this.activeCandidateIndex < list.length - 1) {
      this.activeCandidateIndex++;
      this.activeCandidate = list[this.activeCandidateIndex];
    }
  }

  selectPrevCandidate() {
    const list = this.candidates();
    if (this.activeCandidateIndex > 0) {
      this.activeCandidateIndex--;
      this.activeCandidate = list[this.activeCandidateIndex];
    }
  }

  quickPromote(candidate: DiscoveryCandidateDto) {
    this.api.triageCandidate(candidate.id, {
      status: 'Promoted'
    }).subscribe({
      next: () => this.loadData()
    });
  }

  quickDismiss(candidate: DiscoveryCandidateDto) {
    this.api.triageCandidate(candidate.id, {
      status: 'Dismissed',
      dismissalReason: 'Descartado en triage rápido'
    }).subscribe({
      next: () => this.loadData()
    });
  }

  handleTriage(event: { id: string; status: 'PendingReview' | 'Promoted' | 'Dismissed'; reason?: string; notes?: string }) {
    this.api.triageCandidate(event.id, {
      status: event.status,
      dismissalReason: event.reason,
      editorialNotes: event.notes
    }).subscribe({
      next: () => {
        this.loadData();
        // Advance to next if possible
        if (this.activeCandidateIndex < this.candidates().length - 1) {
          this.selectNextCandidate();
        } else {
          this.isPreviewOpen = false;
        }
      }
    });
  }

  onLeadSubmitted() {
    this.loadData();
  }
}
