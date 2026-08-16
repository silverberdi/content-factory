import { Component, EventEmitter, Input, OnChanges, Output, SimpleChanges, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService, ContentIdeaDto, CreateIdeaRequest, UpdateIdeaRequest } from '../../core/api.service';

@Component({
  selector: 'app-idea-edit-drawer',
  standalone: true,
  imports: [CommonModule, FormsModule],
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
                <span class="px-2 py-0.5 rounded text-[10px] font-bold"
                      [ngClass]="isEditMode ? 'bg-amber-500/15 text-amber-600 dark:text-amber-400 border border-amber-500/30' : 'bg-blue-500/15 text-blue-600 dark:text-blue-400 border border-blue-500/30'">
                  {{ isEditMode ? 'Editar Idea (v' + (idea?.version || 1) + ')' : 'Nueva Idea Manual' }}
                </span>
                <span *ngIf="isEditMode" class="font-mono text-[10px] text-[var(--app-muted)]">
                  Bloqueo Optimista Activo
                </span>
              </div>
              <h3 class="text-sm sm:text-base font-bold text-[var(--app-text)]">
                {{ isEditMode ? 'Modificar Parámetros Creativos' : 'Añadir Propuesta Creativa' }}
              </h3>
            </div>
            <button (click)="close()" [disabled]="isSaving()" class="w-8 h-8 rounded-lg flex items-center justify-center text-[var(--app-muted)] hover:text-[var(--app-text)] hover:bg-[var(--app-card-bg)] transition-colors cursor-pointer disabled:opacity-50">
              <i class="pi pi-times text-sm"></i>
            </button>
          </div>

          <!-- Form Body -->
          <form (ngSubmit)="save()" class="flex-1 overflow-y-auto p-4 sm:p-5 space-y-4">
            
            <!-- Concurrency Conflict Banner (409) -->
            <div *ngIf="concurrencyError()" class="p-4 rounded-xl bg-amber-500/10 border border-amber-500/30 text-amber-900 dark:text-amber-200 space-y-2">
              <div class="flex items-start gap-2">
                <i class="pi pi-exclamation-triangle text-amber-600 dark:text-amber-400 text-base mt-0.5 shrink-0"></i>
                <div>
                  <span class="font-bold text-xs block">Conflicto de Edición Concurrente (HTTP 409)</span>
                  <p class="text-[11px] text-amber-800 dark:text-amber-300 mt-1 leading-relaxed">
                    {{ concurrencyError() }}
                  </p>
                </div>
              </div>
              <div class="pt-2 flex justify-end">
                <button type="button" (click)="reloadAndDismissConflict()" class="px-3 py-1 rounded-lg bg-amber-600 hover:bg-amber-500 text-white font-bold text-xs cursor-pointer shadow-xs">
                  <i class="pi pi-refresh mr-1 text-[10px]"></i> Recargar Versión Más Reciente
                </button>
              </div>
            </div>

            <!-- Generic Error Alert -->
            <div *ngIf="errorMessage() && !concurrencyError()" class="p-3 rounded-xl bg-red-500/10 border border-red-500/30 text-red-700 dark:text-red-300 flex items-start gap-2">
              <i class="pi pi-times-circle text-red-500 mt-0.5 shrink-0"></i>
              <span class="text-xs">{{ errorMessage() }}</span>
            </div>

            <!-- Title -->
            <div class="space-y-1">
              <label class="font-bold text-[var(--app-text)]">Título de la Idea *</label>
              <input type="text"
                     [(ngModel)]="form.title"
                     name="title"
                     required
                     placeholder="Ej: 3 Claves de razonamiento que la IA no reemplaza en 2026"
                     class="w-full px-3 py-2 rounded-lg bg-[var(--app-bg)] border border-[var(--app-card-border)] text-[var(--app-text)] focus:border-blue-500 outline-hidden transition-all text-xs" />
            </div>

            <!-- Angle -->
            <div class="space-y-1">
              <label class="font-bold text-[var(--app-text)]">Ángulo / Tesis Editorial *</label>
              <textarea [(ngModel)]="form.angle"
                        name="angle"
                        required
                        rows="2"
                        placeholder="Ej: Enfoque contraintuitivo: El criterio crítico supera a la memorización de prompts..."
                        class="w-full px-3 py-2 rounded-lg bg-[var(--app-bg)] border border-[var(--app-card-border)] text-[var(--app-text)] focus:border-blue-500 outline-hidden transition-all text-xs resize-y"></textarea>
            </div>

            <!-- Hook Strategy -->
            <div class="space-y-1">
              <label class="font-bold text-[var(--app-text)]">Estrategia de Gancho (Primeros 3-5 segundos) *</label>
              <textarea [(ngModel)]="form.hookStrategy"
                        name="hookStrategy"
                        required
                        rows="2"
                        placeholder="Ej: ¿Crees que un prompt te salvará en 2026? Estas 3 habilidades valen 10 veces más..."
                        class="w-full px-3 py-2 rounded-lg bg-[var(--app-bg)] border border-[var(--app-card-border)] text-[var(--app-text)] focus:border-blue-500 outline-hidden transition-all text-xs resize-y"></textarea>
            </div>

            <!-- Audience Value -->
            <div class="space-y-1">
              <label class="font-bold text-[var(--app-text)]">Valor para la Audiencia / Aprendizaje Concreto *</label>
              <textarea [(ngModel)]="form.audienceValue"
                        name="audienceValue"
                        required
                        rows="2"
                        placeholder="Ej: El espectador aprende a auditar respuestas complejas y evitar errores costosos..."
                        class="w-full px-3 py-2 rounded-lg bg-[var(--app-bg)] border border-[var(--app-card-border)] text-[var(--app-text)] focus:border-blue-500 outline-hidden transition-all text-xs resize-y"></textarea>
            </div>

            <!-- Grid: Format & Intended Outcome -->
            <div class="grid grid-cols-1 sm:grid-cols-2 gap-3">
              <div class="space-y-1">
                <label class="font-bold text-[var(--app-text)]">Formato</label>
                <select [(ngModel)]="form.format"
                        name="format"
                        class="w-full px-3 py-2 rounded-lg bg-[var(--app-bg)] border border-[var(--app-card-border)] text-[var(--app-text)] focus:border-blue-500 outline-hidden transition-all text-xs">
                  <option value="YouTube Short 30-60s">YouTube Short 30-60s</option>
                  <option value="YouTube Short 60s">YouTube Short 60s</option>
                  <option value="TikTok 60-90s">TikTok 60-90s</option>
                  <option value="Instagram Reel 30-45s">Instagram Reel 30-45s</option>
                </select>
              </div>

              <div class="space-y-1">
                <label class="font-bold text-[var(--app-text)]">Objetivo de Retención</label>
                <input type="text"
                       [(ngModel)]="form.intendedOutcome"
                       name="intendedOutcome"
                       placeholder="Ej: Inspiración / Retención alta"
                       class="w-full px-3 py-2 rounded-lg bg-[var(--app-bg)] border border-[var(--app-card-border)] text-[var(--app-text)] focus:border-blue-500 outline-hidden transition-all text-xs" />
              </div>
            </div>

            <!-- Grid: Freshness & Priority -->
            <div class="grid grid-cols-1 sm:grid-cols-2 gap-3">
              <div class="space-y-1">
                <label class="font-bold text-[var(--app-text)]">Caducidad / Frescura</label>
                <select [(ngModel)]="form.freshnessClass"
                        name="freshnessClass"
                        class="w-full px-3 py-2 rounded-lg bg-[var(--app-bg)] border border-[var(--app-card-border)] text-[var(--app-text)] focus:border-blue-500 outline-hidden transition-all text-xs">
                  <option value="Timely">Timely (Tendencia actual)</option>
                  <option value="Evergreen">Evergreen (Perenne)</option>
                  <option value="Breaking">Breaking (Noticia de última hora)</option>
                </select>
              </div>

              <div class="space-y-1">
                <label class="font-bold text-[var(--app-text)]">Prioridad</label>
                <select [(ngModel)]="form.priority"
                        name="priority"
                        class="w-full px-3 py-2 rounded-lg bg-[var(--app-bg)] border border-[var(--app-card-border)] text-[var(--app-text)] focus:border-blue-500 outline-hidden transition-all text-xs">
                  <option value="Normal">Normal</option>
                  <option value="High">High</option>
                  <option value="Urgent">Urgent</option>
                  <option value="Low">Low</option>
                </select>
              </div>
            </div>

            <!-- Rationale -->
            <div class="space-y-1">
              <label class="font-bold text-[var(--app-text)]">Justificación Editorial</label>
              <textarea [(ngModel)]="form.rationale"
                        name="rationale"
                        rows="2"
                        placeholder="Por qué esta idea es prometedora según los datos del TruthSource..."
                        class="w-full px-3 py-2 rounded-lg bg-[var(--app-bg)] border border-[var(--app-card-border)] text-[var(--app-text)] focus:border-blue-500 outline-hidden transition-all text-xs resize-y"></textarea>
            </div>

            <!-- Change Summary (Only in Edit Mode) -->
            <div *ngIf="isEditMode" class="space-y-1 p-3 rounded-xl bg-blue-500/5 border border-blue-500/20">
              <label class="font-bold text-[var(--app-text)] block">Resumen del Cambio (Auditoría de Versión)</label>
              <input type="text"
                     [(ngModel)]="form.changeSummary"
                     name="changeSummary"
                     placeholder="Ej: Ajuste del gancho inicial para aumentar impacto..."
                     class="w-full px-3 py-2 rounded-lg bg-[var(--app-bg)] border border-[var(--app-card-border)] text-[var(--app-text)] focus:border-blue-500 outline-hidden transition-all text-xs" />
            </div>

          </form>

          <!-- Footer -->
          <div class="p-4 sm:p-5 border-t border-[var(--app-card-border)] bg-[var(--app-bg)] flex items-center justify-between">
            <button type="button" 
                    (click)="close()" 
                    [disabled]="isSaving()"
                    class="px-3.5 py-2 rounded-lg border border-[var(--app-card-border)] text-[var(--app-muted)] hover:text-[var(--app-text)] hover:bg-[var(--app-card-bg)] font-semibold text-xs cursor-pointer transition-colors disabled:opacity-50">
              Cancelar
            </button>

            <button type="button" 
                    (click)="save()" 
                    [disabled]="isSaving() || !form.title || !form.angle || !form.hookStrategy || !form.audienceValue"
                    class="px-4 py-2 rounded-lg bg-blue-600 hover:bg-blue-500 text-white font-bold text-xs flex items-center gap-1.5 cursor-pointer shadow-md shadow-blue-500/20 transition-all disabled:opacity-50">
              <i class="pi" [ngClass]="isSaving() ? 'pi-spin pi-spinner' : 'pi-check'"></i>
              <span>{{ isSaving() ? 'Guardando...' : (isEditMode ? 'Guardar Versión' : 'Crear Idea') }}</span>
            </button>
          </div>

        </div>
      </div>
    </div>
  `
})
export class IdeaEditDrawerComponent implements OnChanges {
  private readonly apiService = inject(ApiService);

  @Input() isOpen = false;
  @Input() contentItemId = '';
  @Input() idea: ContentIdeaDto | null = null;

  @Output() closeEvent = new EventEmitter<void>();
  @Output() ideaSaved = new EventEmitter<ContentIdeaDto>();
  @Output() conflictReload = new EventEmitter<void>();

  isEditMode = false;
  isSaving = signal<boolean>(false);
  errorMessage = signal<string | null>(null);
  concurrencyError = signal<string | null>(null);

  form = {
    title: '',
    angle: '',
    hookStrategy: '',
    audienceValue: '',
    format: 'YouTube Short 30-60s',
    intendedOutcome: '',
    freshnessClass: 'Timely',
    priority: 'Normal',
    rationale: '',
    changeSummary: ''
  };

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['isOpen']?.currentValue) {
      this.resetErrors();
      if (this.idea) {
        this.isEditMode = true;
        this.form = {
          title: this.idea.title,
          angle: this.idea.angle,
          hookStrategy: this.idea.hookStrategy,
          audienceValue: this.idea.audienceValue,
          format: this.idea.format || 'YouTube Short 30-60s',
          intendedOutcome: this.idea.intendedOutcome || '',
          freshnessClass: this.idea.freshnessClass || 'Timely',
          priority: this.idea.priority || 'Normal',
          rationale: this.idea.rationale || '',
          changeSummary: ''
        };
      } else {
        this.isEditMode = false;
        this.form = {
          title: '',
          angle: '',
          hookStrategy: '',
          audienceValue: '',
          format: 'YouTube Short 30-60s',
          intendedOutcome: '',
          freshnessClass: 'Timely',
          priority: 'Normal',
          rationale: '',
          changeSummary: ''
        };
      }
    }
  }

  resetErrors() {
    this.errorMessage.set(null);
    this.concurrencyError.set(null);
  }

  reloadAndDismissConflict() {
    this.conflictReload.emit();
    this.close();
  }

  close() {
    if (this.isSaving()) return;
    this.resetErrors();
    this.closeEvent.emit();
  }

  save() {
    if (!this.contentItemId || this.isSaving()) return;
    if (!this.form.title?.trim() || !this.form.angle?.trim() || !this.form.hookStrategy?.trim() || !this.form.audienceValue?.trim()) {
      this.errorMessage.set('Por favor complete todos los campos obligatorios (*).');
      return;
    }

    this.isSaving.set(true);
    this.resetErrors();

    if (this.isEditMode && this.idea) {
      const request: UpdateIdeaRequest = {
        title: this.form.title.trim(),
        angle: this.form.angle.trim(),
        hookStrategy: this.form.hookStrategy.trim(),
        audienceValue: this.form.audienceValue.trim(),
        format: this.form.format,
        intendedOutcome: this.form.intendedOutcome?.trim() || null,
        freshnessClass: this.form.freshnessClass,
        priority: this.form.priority,
        rationale: this.form.rationale?.trim() || null,
        changeSummary: this.form.changeSummary?.trim() || 'Modificación por el operador',
        expectedVersion: this.idea.version
      };

      this.apiService.updateIdea(this.contentItemId, this.idea.id, request).subscribe({
        next: (saved) => {
          this.isSaving.set(false);
          this.ideaSaved.emit(saved);
          this.close();
        },
        error: (err) => {
          this.isSaving.set(false);
          if (err.status === 409) {
            const conflictMsg = err.error?.message || 'La idea fue modificada por otro operador concurrentemente. Por favor recarga los últimos cambios.';
            this.concurrencyError.set(conflictMsg);
          } else {
            this.errorMessage.set(err?.error?.message || err?.message || 'Error al actualizar la idea.');
          }
        }
      });
    } else {
      const request: CreateIdeaRequest = {
        title: this.form.title.trim(),
        angle: this.form.angle.trim(),
        hookStrategy: this.form.hookStrategy.trim(),
        audienceValue: this.form.audienceValue.trim(),
        format: this.form.format,
        intendedOutcome: this.form.intendedOutcome?.trim() || null,
        freshnessClass: this.form.freshnessClass,
        priority: this.form.priority,
        rationale: this.form.rationale?.trim() || null
      };

      this.apiService.createManualIdea(this.contentItemId, request).subscribe({
        next: (created) => {
          this.isSaving.set(false);
          this.ideaSaved.emit(created);
          this.close();
        },
        error: (err) => {
          this.isSaving.set(false);
          this.errorMessage.set(err?.error?.message || err?.message || 'Error al crear la idea.');
        }
      });
    }
  }
}
