import { Component, Input, Output, EventEmitter, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ScriptVersionDto } from '../../core/api.service';

@Component({
  selector: 'app-script-version-history-drawer',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div *ngIf="isOpen" class="fixed inset-0 z-50 overflow-hidden animate-fade-in">
      <div class="absolute inset-0 bg-black/60 backdrop-blur-xs" (click)="close.emit()"></div>

      <div class="absolute inset-y-0 right-0 max-w-full flex pl-10">
        <div class="w-screen max-w-md bg-[var(--app-card-bg)] border-l border-[var(--app-card-border)] shadow-2xl flex flex-col animate-slide-left text-xs">
          
          <!-- Header -->
          <div class="px-5 py-4 border-b border-[var(--app-card-border)] flex items-center justify-between bg-[var(--app-bg)]/50">
            <div class="flex items-center gap-2">
              <div class="w-7 h-7 rounded-lg bg-blue-500/10 border border-blue-500/20 flex items-center justify-center text-blue-500">
                <i class="pi pi-history text-sm"></i>
              </div>
              <div>
                <h3 class="font-bold text-sm text-[var(--app-text)]">Historial de Versiones</h3>
                <p class="text-[10px] text-[var(--app-muted)]">Snapshots inmutables de auditoría editorial</p>
              </div>
            </div>
            <button (click)="close.emit()" class="p-1 rounded-md text-[var(--app-muted)] hover:text-[var(--app-text)] cursor-pointer">
              <i class="pi pi-times"></i>
            </button>
          </div>

          <!-- Body / List -->
          <div class="flex-1 p-5 space-y-4 overflow-y-auto">
            
            <div *ngIf="versions.length === 0" class="text-center py-8 text-[var(--app-muted)]">
              <p>No hay versiones previas registradas.</p>
            </div>

            <div *ngFor="let ver of versions" 
                 class="p-3.5 rounded-xl border transition-all space-y-2"
                 [ngClass]="{
                   'bg-[var(--app-bg)] border-[var(--app-card-border)]': selectedVersionId() !== ver.id,
                   'bg-blue-500/5 border-blue-500/40 shadow-xs': selectedVersionId() === ver.id
                 }">
              
              <div class="flex items-start justify-between gap-2">
                <div class="flex items-center gap-2">
                  <span class="px-2 py-0.5 rounded bg-blue-500/15 text-blue-600 dark:text-blue-400 font-bold font-mono text-[11px] border border-blue-500/30">
                    v{{ ver.versionNumber }}
                  </span>
                  <span class="px-2 py-0.5 rounded text-[10px] font-bold uppercase tracking-wider font-mono border"
                        [ngClass]="{
                          'bg-emerald-500/15 text-emerald-600 dark:text-emerald-400 border-emerald-500/30': ver.status === 'Approved',
                          'bg-amber-500/15 text-amber-600 dark:text-amber-400 border-amber-500/30': ver.status === 'UnderReview',
                          'bg-blue-500/15 text-blue-600 dark:text-blue-400 border-blue-500/30': ver.status === 'Draft',
                          'bg-red-500/15 text-red-600 dark:text-red-400 border-red-500/30': ver.status === 'Rejected'
                        }">
                    {{ ver.status }}
                  </span>
                </div>

                <span class="text-[10px] font-mono text-[var(--app-muted)]">
                  {{ ver.createdAtUtc | date:'yyyy-MM-dd HH:mm' }}
                </span>
              </div>

              <!-- Change Summary -->
              <p class="text-[11px] text-[var(--app-text)] font-medium leading-snug">
                {{ ver.changeSummary || 'Sin resumen descriptivo' }}
              </p>

              <!-- Metrics -->
              <div class="flex items-center gap-3 font-mono text-[10px] text-[var(--app-muted)] pt-1 border-t border-[var(--app-card-border)]">
                <span>{{ ver.totalWordCount }} palabras</span>
                <span>•</span>
                <span>~{{ ver.estimatedDurationSeconds.toFixed(1) }}s</span>
                <span>•</span>
                <span>{{ ver.pacingWpm }} WPM</span>
              </div>

              <!-- Author / Rejection reason if any -->
              <div class="flex items-center justify-between text-[9px] text-[var(--app-muted)] pt-0.5">
                <span>Por: {{ ver.createdByEmail }}</span>
                <button (click)="toggleInspect(ver.id)" 
                        class="text-blue-600 dark:text-blue-400 hover:underline font-bold cursor-pointer">
                  {{ selectedVersionId() === ver.id ? 'Ocultar JSON' : 'Ver Snapshot' }}
                </button>
              </div>

              <!-- Inspect Snapshot JSON -->
              <div *ngIf="selectedVersionId() === ver.id" class="pt-2">
                <pre class="p-2.5 rounded bg-[var(--app-card-bg)] border border-[var(--app-card-border)] text-[9px] font-mono text-[var(--app-text)] max-h-48 overflow-auto leading-relaxed">{{ formatJson(ver.snapshotJson) }}</pre>
              </div>

            </div>

          </div>

          <!-- Footer -->
          <div class="px-5 py-3 border-t border-[var(--app-card-border)] bg-[var(--app-bg)]/50 flex items-center justify-between text-[11px] text-[var(--app-muted)]">
            <span>Total: {{ versions.length }} versión(es)</span>
            <button (click)="close.emit()" class="px-3 py-1 rounded-lg border border-[var(--app-card-border)] text-[var(--app-text)] hover:bg-[var(--app-card-bg)] cursor-pointer font-semibold">
              Cerrar
            </button>
          </div>

        </div>
      </div>
    </div>
  `
})
export class ScriptVersionHistoryDrawerComponent {
  @Input() isOpen: boolean = false;
  @Input() versions: ScriptVersionDto[] = [];
  @Output() close = new EventEmitter<void>();

  readonly selectedVersionId = signal<string | null>(null);

  toggleInspect(id: string) {
    this.selectedVersionId.set(this.selectedVersionId() === id ? null : id);
  }

  formatJson(json: string): string {
    try {
      return JSON.stringify(JSON.parse(json), null, 2);
    } catch {
      return json;
    }
  }
}
