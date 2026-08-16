import { Component, EventEmitter, Input, OnChanges, Output, SimpleChanges, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ApiService, ContentIdeaDto, ContentIdeaVersionDto } from '../../core/api.service';

@Component({
  selector: 'app-idea-version-history-drawer',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div *ngIf="isOpen" class="fixed inset-0 z-50 overflow-hidden animate-in fade-in duration-150">
      <!-- Backdrop -->
      <div (click)="close()" class="absolute inset-0 bg-black/50 backdrop-blur-xs transition-opacity"></div>

      <div class="fixed inset-y-0 right-0 max-w-full flex pl-10">
        <div class="w-screen max-w-xl bg-[var(--app-card-bg)] border-l border-[var(--app-card-border)] shadow-2xl flex flex-col animate-in slide-in-from-right duration-200 text-xs">
          
          <!-- Header -->
          <div class="p-4 sm:p-5 border-b border-[var(--app-card-border)] bg-[var(--app-bg)] flex items-center justify-between">
            <div class="space-y-1">
              <div class="flex items-center gap-2">
                <span class="px-2 py-0.5 rounded bg-blue-500/15 text-blue-600 dark:text-blue-400 border border-blue-500/30 text-[10px] font-bold">
                  Historial de Versiones
                </span>
                <span class="font-mono text-[10px] text-[var(--app-muted)]">v{{ idea?.version || 1 }} actual</span>
              </div>
              <h3 class="text-sm sm:text-base font-bold text-[var(--app-text)] line-clamp-1">
                {{ idea?.title || 'Idea' }}
              </h3>
            </div>
            <button (click)="close()" class="w-8 h-8 rounded-lg flex items-center justify-center text-[var(--app-muted)] hover:text-[var(--app-text)] hover:bg-[var(--app-card-bg)] transition-colors cursor-pointer">
              <i class="pi pi-times text-sm"></i>
            </button>
          </div>

          <!-- Content List -->
          <div class="flex-1 overflow-y-auto p-4 sm:p-5 space-y-4">
            
            <!-- Loading -->
            <div *ngIf="isLoading()" class="py-12 text-center text-xs text-[var(--app-muted)] space-y-2">
              <i class="pi pi-spin pi-spinner text-2xl text-blue-500 block mx-auto"></i>
              <span>Cargando snapshots de versiones...</span>
            </div>

            <!-- Error -->
            <div *ngIf="errorMessage()" class="p-4 rounded-xl bg-red-500/10 border border-red-500/30 text-red-700 dark:text-red-300 text-xs">
              {{ errorMessage() }}
            </div>

            <!-- Empty -->
            <div *ngIf="!isLoading() && versions().length === 0" class="py-12 text-center text-[var(--app-muted)]">
              <i class="pi pi-history text-3xl block mx-auto mb-2 opacity-40"></i>
              <p>No hay versiones históricas archivadas para esta idea todavía.</p>
            </div>

            <!-- Timeline -->
            <div *ngIf="!isLoading() && versions().length > 0" class="space-y-4 relative before:absolute before:inset-0 before:left-3.5 before:w-0.5 before:bg-[var(--app-card-border)]">
              
              <div *ngFor="let ver of versions(); let i = index" class="relative pl-8 space-y-2">
                <!-- Timeline Dot -->
                <div class="absolute left-2 top-2 -translate-x-1/2 w-3.5 h-3.5 rounded-full border-2 border-[var(--app-card-bg)] shadow-xs"
                     [ngClass]="i === 0 ? 'bg-blue-600 ring-4 ring-blue-500/20' : 'bg-slate-400 dark:bg-slate-600'"></div>

                <div class="p-3.5 rounded-xl bg-[var(--app-bg)] border border-[var(--app-card-border)] space-y-2.5">
                  
                  <div class="flex items-center justify-between gap-2 flex-wrap">
                    <div class="flex items-center gap-2">
                      <span class="px-2 py-0.5 rounded bg-purple-500/15 text-purple-600 dark:text-purple-400 font-bold font-mono text-[10px]">
                        v{{ ver.versionNumber }}
                      </span>
                      <span class="px-2 py-0.5 rounded text-[10px] font-bold uppercase tracking-wider border font-mono"
                            [ngClass]="{
                              'bg-emerald-500/15 text-emerald-600 dark:text-emerald-400 border-emerald-500/30': ver.status === 'Selected',
                              'bg-blue-500/15 text-blue-600 dark:text-blue-400 border-blue-500/30': ver.status === 'Proposed',
                              'bg-rose-500/15 text-rose-600 dark:text-rose-400 border-rose-500/30': ver.status === 'Dismissed'
                            }">
                        {{ ver.status }}
                      </span>
                    </div>
                    <span class="text-[10px] text-[var(--app-muted)] font-mono">
                      {{ ver.editedAtUtc | date:'yyyy-MM-dd HH:mm' }}
                    </span>
                  </div>

                  <!-- Change Summary -->
                  <div class="p-2 rounded-lg bg-[var(--app-card-bg)] border border-[var(--app-card-border)] text-[11px] text-[var(--app-text)] italic">
                    "{{ ver.changeSummary || 'Sin resumen de cambio' }}"
                  </div>

                  <!-- Snapshot Details -->
                  <div class="space-y-1.5 text-[11px]">
                    <div>
                      <span class="text-[var(--app-muted)] font-semibold block text-[10px]">Título:</span>
                      <span class="font-bold text-[var(--app-text)]">{{ ver.title }}</span>
                    </div>

                    <div>
                      <span class="text-[var(--app-muted)] font-semibold block text-[10px]">Ángulo:</span>
                      <p class="text-[var(--app-text)] leading-relaxed">{{ ver.angle }}</p>
                    </div>

                    <div>
                      <span class="text-[var(--app-muted)] font-semibold block text-[10px]">Estrategia de Gancho:</span>
                      <p class="text-purple-700 dark:text-purple-300 font-medium">"{{ ver.hookStrategy }}"</p>
                    </div>

                    <div>
                      <span class="text-[var(--app-muted)] font-semibold block text-[10px]">Valor para la Audiencia:</span>
                      <p class="text-[var(--app-text)]">{{ ver.audienceValue }}</p>
                    </div>

                    <div *ngIf="ver.dismissalNotes" class="p-2 rounded bg-red-500/10 border border-red-500/20 text-red-700 dark:text-red-300">
                      <span class="font-bold block text-[10px]">Motivo de descarte:</span>
                      <span>{{ ver.dismissalNotes }}</span>
                    </div>

                    <div class="pt-1 text-[10px] text-[var(--app-muted)] flex items-center justify-between border-t border-[var(--app-card-border)]">
                      <span>Operador: {{ ver.editedByEmail }}</span>
                      <span class="font-mono">TS v{{ ver.truthSourceVersionId ? ver.truthSourceVersionId.substring(0,8) : 'N/A' }}</span>
                    </div>
                  </div>

                </div>
              </div>

            </div>

          </div>

          <!-- Footer -->
          <div class="p-4 border-t border-[var(--app-card-border)] bg-[var(--app-bg)] flex justify-end">
            <button (click)="close()" class="px-4 py-2 rounded-lg border border-[var(--app-card-border)] text-[var(--app-text)] hover:bg-[var(--app-card-bg)] font-semibold text-xs cursor-pointer">
              Cerrar
            </button>
          </div>

        </div>
      </div>
    </div>
  `
})
export class IdeaVersionHistoryDrawerComponent implements OnChanges {
  private readonly apiService = inject(ApiService);

  @Input() isOpen = false;
  @Input() contentItemId = '';
  @Input() idea: ContentIdeaDto | null = null;

  @Output() closeEvent = new EventEmitter<void>();

  versions = signal<ContentIdeaVersionDto[]>([]);
  isLoading = signal<boolean>(false);
  errorMessage = signal<string | null>(null);

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['isOpen']?.currentValue && this.idea && this.contentItemId) {
      this.loadVersions();
    }
  }

  loadVersions() {
    if (!this.contentItemId || !this.idea) return;
    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.apiService.getIdeaVersions(this.contentItemId, this.idea.id).subscribe({
      next: (data) => {
        this.versions.set(data);
        this.isLoading.set(false);
      },
      error: (err) => {
        this.isLoading.set(false);
        this.errorMessage.set(err?.error?.message || err?.message || 'Error al cargar historial de versiones.');
      }
    });
  }

  close() {
    this.closeEvent.emit();
  }
}
