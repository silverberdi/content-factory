import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { StoryboardVersionDto } from '../../core/api.service';

@Component({
  selector: 'app-storyboard-version-history-drawer',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="fixed inset-y-0 right-0 z-50 w-full max-w-md bg-[var(--app-card-bg)] border-l border-[var(--app-card-border)] shadow-2xl flex flex-col animate-slide-left">
      
      <!-- Header -->
      <div class="px-5 py-4 border-b border-[var(--app-card-border)] flex items-center justify-between bg-[var(--app-bg)]">
        <div class="flex items-center gap-2">
          <i class="pi pi-history text-purple-500 text-sm"></i>
          <div>
            <h3 class="text-sm font-bold text-[var(--app-text)]">Historial de Versiones (Storyboard)</h3>
            <p class="text-[11px] text-[var(--app-muted)]">Snapshots inmutables de planificación y especificación</p>
          </div>
        </div>
        <button (click)="close.emit()" class="text-[var(--app-muted)] hover:text-[var(--app-text)] p-1 rounded-lg hover:bg-[var(--app-card-border)] transition-colors">
          <i class="pi pi-times text-xs"></i>
        </button>
      </div>

      <!-- Body -->
      <div class="p-5 space-y-4 overflow-y-auto flex-1 text-xs">
        
        <div *ngIf="isLoading" class="p-8 text-center text-xs text-[var(--app-muted)]">
          <i class="pi pi-spin pi-spinner text-lg mb-2 block text-purple-500"></i>
          <span>Cargando snapshots de versión...</span>
        </div>

        <div *ngIf="!isLoading && versions.length === 0" class="p-8 text-center text-xs text-[var(--app-muted)]">
          No hay versiones previas registradas.
        </div>

        <!-- Version Timeline -->
        <div *ngIf="!isLoading && versions.length > 0" class="relative pl-6 space-y-4 before:absolute before:left-2.5 before:top-2 before:bottom-2 before:w-0.5 before:bg-[var(--app-card-border)]">
          
          <div *ngFor="let v of versions" class="relative group">
            <!-- Timeline dot -->
            <div class="absolute -left-6 top-1.5 w-3 h-3 rounded-full border-2 border-[var(--app-card-bg)]"
                 [ngClass]="{
                   'bg-emerald-500 ring-2 ring-emerald-500/20': v.status === 'Approved',
                   'bg-amber-500 ring-2 ring-amber-500/20': v.status === 'UnderReview',
                   'bg-blue-500 ring-2 ring-blue-500/20': v.status === 'Draft',
                   'bg-red-500 ring-2 ring-red-500/20': v.status === 'Rejected'
                 }">
            </div>

            <!-- Version Card -->
            <div class="p-3.5 rounded-xl bg-[var(--app-bg)] border border-[var(--app-card-border)] hover:border-purple-500/40 transition-all space-y-2">
              
              <div class="flex items-center justify-between gap-2">
                <div class="flex items-center gap-1.5">
                  <span class="px-2 py-0.5 rounded bg-purple-500/15 text-purple-600 dark:text-purple-400 font-mono font-bold text-[10px] border border-purple-500/30">
                    v{{ v.versionNumber }}
                  </span>
                  <span class="px-1.5 py-0.5 rounded text-[9px] font-bold uppercase font-mono border"
                        [ngClass]="{
                          'bg-emerald-500/15 text-emerald-600 border-emerald-500/30': v.status === 'Approved',
                          'bg-amber-500/15 text-amber-600 border-amber-500/30': v.status === 'UnderReview',
                          'bg-blue-500/15 text-blue-600 border-blue-500/30': v.status === 'Draft',
                          'bg-red-500/15 text-red-600 border-red-500/30': v.status === 'Rejected'
                        }">
                    {{ v.status }}
                  </span>
                </div>

                <span class="text-[10px] font-mono text-[var(--app-muted)]">
                  {{ v.createdAtUtc | date:'yyyy-MM-dd HH:mm' }}
                </span>
              </div>

              <!-- Change Summary -->
              <p class="text-xs text-[var(--app-text)] font-medium">
                {{ v.changeSummary || 'Sin resumen de cambios' }}
              </p>

              <!-- Metrics & Author -->
              <div class="flex items-center justify-between text-[10px] text-[var(--app-muted)] pt-1 border-t border-[var(--app-card-border)]/60">
                <div class="flex items-center gap-2">
                  <span>{{ v.frameCount }} tomas</span>
                  <span>•</span>
                  <span>{{ v.assetRequirementCount }} activos</span>
                  <span>•</span>
                  <span>~{{ v.totalEstimatedDurationSeconds }}s</span>
                </div>
                <span class="truncate max-w-[120px] font-mono">
                  {{ v.createdByEmail }}
                </span>
              </div>

              <!-- Rejection Reason if any -->
              <div *ngIf="v.rejectionReason" class="p-2 rounded bg-red-500/10 border border-red-500/20 text-red-600 dark:text-red-400 text-[10px]">
                <strong class="block">Motivo de rechazo:</strong>
                {{ v.rejectionReason }}
              </div>

            </div>
          </div>

        </div>

      </div>

      <!-- Footer -->
      <div class="px-5 py-3 border-t border-[var(--app-card-border)] bg-[var(--app-bg)]">
        <button (click)="close.emit()" class="cf-btn-secondary w-full">
          Cerrar Historial
        </button>
      </div>

    </div>
  `
})
export class StoryboardVersionHistoryDrawerComponent {
  @Input() versions: StoryboardVersionDto[] = [];
  @Input() isLoading = false;
  @Output() close = new EventEmitter<void>();
}
