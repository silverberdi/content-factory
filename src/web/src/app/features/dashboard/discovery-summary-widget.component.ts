import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { DiscoverySummaryDto } from '../../core/api.service';

@Component({
  selector: 'app-discovery-summary-widget',
  standalone: true,
  imports: [CommonModule, RouterLink],
  template: `
    <div class="rounded-xl border border-[var(--app-card-border)] bg-[var(--app-card-bg)] p-5 shadow-xs flex flex-col justify-between">
      <div>
        <!-- Widget Header -->
        <div class="flex items-center justify-between pb-3 mb-3.5 border-b border-[var(--app-card-border)]">
          <div class="flex items-center gap-2">
            <div class="w-6 h-6 rounded-md bg-blue-500/10 text-blue-500 flex items-center justify-center">
              <i class="pi pi-compass text-xs"></i>
            </div>
            <div>
              <h3 class="font-bold text-sm tracking-tight text-[var(--app-text)]">Discovery & Ingesta</h3>
            </div>
          </div>
          
          <button (click)="onQuickSubmit.emit()" 
                  class="px-2.5 py-1 rounded-lg bg-blue-600 hover:bg-blue-700 text-white text-xs font-bold transition-all shadow-2xs flex items-center gap-1 cursor-pointer">
            <i class="pi pi-plus text-[9px]"></i>
            <span>Quick Submit</span>
          </button>
        </div>

        <!-- Metrics Grid -->
        <div class="grid grid-cols-3 gap-2.5 mb-3.5">
          <!-- Pending Review -->
          <a routerLink="/discovery/triage" 
             class="p-2.5 rounded-lg border border-[var(--app-card-border)] bg-[var(--app-bg)] hover:bg-[var(--app-surface-hover)] transition-all flex flex-col justify-between group">
            <span class="text-[10px] font-bold uppercase tracking-wider text-amber-600 dark:text-amber-400">Triage Pendiente</span>
            <div class="flex items-baseline justify-between mt-1">
              <span class="text-lg font-extrabold text-[var(--app-text)] group-hover:text-blue-500 transition-colors">
                {{ summary?.pendingCandidatesCount ?? 0 }}
              </span>
              <i class="pi pi-arrow-up-right text-[9px] text-[var(--app-muted)] group-hover:text-blue-500"></i>
            </div>
          </a>

          <!-- Promoted -->
          <a routerLink="/discovery/triage" 
             class="p-2.5 rounded-lg border border-[var(--app-card-border)] bg-[var(--app-bg)] hover:bg-[var(--app-surface-hover)] transition-all flex flex-col justify-between group">
            <span class="text-[10px] font-bold uppercase tracking-wider text-emerald-600 dark:text-emerald-400">Promovidos</span>
            <div class="flex items-baseline justify-between mt-1">
              <span class="text-lg font-extrabold text-[var(--app-text)] group-hover:text-blue-500 transition-colors">
                {{ summary?.promotedCandidatesCount ?? 0 }}
              </span>
              <i class="pi pi-arrow-up-right text-[9px] text-[var(--app-muted)] group-hover:text-blue-500"></i>
            </div>
          </a>

          <!-- Monitored Sources -->
          <a routerLink="/discovery/sources" 
             class="p-2.5 rounded-lg border border-[var(--app-card-border)] bg-[var(--app-bg)] hover:bg-[var(--app-surface-hover)] transition-all flex flex-col justify-between group">
            <span class="text-[10px] font-bold uppercase tracking-wider text-blue-600 dark:text-blue-400">Fuentes Activas</span>
            <div class="flex items-baseline justify-between mt-1">
              <span class="text-lg font-extrabold text-[var(--app-text)] group-hover:text-blue-500 transition-colors">
                {{ summary?.activeSourcesCount ?? 0 }}
              </span>
              <i class="pi pi-arrow-up-right text-[9px] text-[var(--app-muted)] group-hover:text-blue-500"></i>
            </div>
          </a>
        </div>

        <!-- Quick Status Context -->
        <div class="text-xs text-[var(--app-muted)] flex items-center justify-between">
          <span *ngIf="(summary?.errorSourcesCount ?? 0) === 0" class="text-emerald-600 dark:text-emerald-400 font-medium flex items-center gap-1">
            <i class="pi pi-check text-[10px]"></i> Todas las fuentes sincronizan correctamente
          </span>
          <span *ngIf="(summary?.errorSourcesCount ?? 0) > 0" class="text-red-500 font-bold flex items-center gap-1">
            <i class="pi pi-exclamation-triangle text-[10px]"></i> {{ summary?.errorSourcesCount }} fuentes con errores
          </span>
        </div>
      </div>

      <!-- Footer Links -->
      <div class="pt-3 mt-3 border-t border-[var(--app-card-border)] flex items-center justify-between text-[11px]">
        <a routerLink="/discovery/triage" class="text-blue-600 dark:text-blue-400 hover:underline font-semibold flex items-center gap-1">
          <span>Abrir Triage Workspace</span> <i class="pi pi-chevron-right text-[9px]"></i>
        </a>
        <a routerLink="/discovery/sources" class="text-[var(--app-muted)] hover:text-[var(--app-text)]">
          Ver Catálogo
        </a>
      </div>
    </div>
  `
})
export class DiscoverySummaryWidgetComponent {
  @Input() summary: DiscoverySummaryDto | null = null;
  @Output() onQuickSubmit = new EventEmitter<void>();
}
