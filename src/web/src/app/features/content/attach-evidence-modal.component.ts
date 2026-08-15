import { Component, EventEmitter, Input, Output, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService, AttachEvidenceRequest, ContentItemEvidenceDto } from '../../core/api.service';

@Component({
  selector: 'app-attach-evidence-modal',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div *ngIf="isOpen" class="fixed inset-0 z-50 overflow-y-auto flex items-center justify-center p-3 sm:p-4">
      <!-- Backdrop -->
      <div (click)="close()" class="fixed inset-0 bg-slate-900/50 dark:bg-black/70 backdrop-blur-xs transition-opacity"></div>

      <!-- Modal Card -->
      <div class="relative w-full max-w-lg bg-[var(--app-card-bg)] border border-[var(--app-card-border)] rounded-xl shadow-2xl overflow-hidden flex flex-col z-10 animate-scale-in">
        
        <!-- Header -->
        <div class="px-5 py-4 border-b border-[var(--app-card-border)] flex items-center justify-between bg-[var(--app-header-bg)]">
          <div class="flex items-center gap-2">
            <div class="w-8 h-8 rounded-lg bg-blue-500/15 text-blue-600 dark:text-blue-400 flex items-center justify-center">
              <i class="pi pi-paperclip text-sm"></i>
            </div>
            <div>
              <h3 class="text-sm font-bold text-[var(--app-text)]">Adjuntar Evidencia de Origen</h3>
              <p class="text-[11px] text-[var(--app-muted)]">Captura y calcula hash inmutable para síntesis de TruthSource</p>
            </div>
          </div>
          <button (click)="close()" class="p-1.5 rounded-lg hover:bg-[var(--app-surface-hover)] text-[var(--app-muted)] hover:text-[var(--app-text)] cursor-pointer">
            <i class="pi pi-times text-xs"></i>
          </button>
        </div>

        <!-- Form -->
        <form (ngSubmit)="submit()" class="p-5 space-y-4 text-xs">
          
          <!-- Mode Selector -->
          <div class="flex rounded-lg bg-[var(--app-bg)] p-1 border border-[var(--app-card-border)]">
            <button type="button" (click)="leadType = 'url'" 
                    class="flex-1 py-1.5 rounded-md font-semibold text-center transition-all cursor-pointer"
                    [ngClass]="leadType === 'url' ? 'bg-[var(--app-card-bg)] shadow-xs text-blue-600 dark:text-blue-400' : 'text-[var(--app-muted)] hover:text-[var(--app-text)]'">
              <i class="pi pi-link mr-1"></i> Enlace Web / URL
            </button>
            <button type="button" (click)="leadType = 'text'" 
                    class="flex-1 py-1.5 rounded-md font-semibold text-center transition-all cursor-pointer"
                    [ngClass]="leadType === 'text' ? 'bg-[var(--app-card-bg)] shadow-xs text-blue-600 dark:text-blue-400' : 'text-[var(--app-muted)] hover:text-[var(--app-text)]'">
              <i class="pi pi-file-edit mr-1"></i> Nota / Texto Directo
            </button>
          </div>

          <!-- Title -->
          <div class="space-y-1">
            <label class="font-bold text-[var(--app-text)] flex items-center justify-between">
              <span>Título de la Evidencia *</span>
            </label>
            <input type="text" [(ngModel)]="title" name="title" required
                   placeholder="Ej: Informe de adopción de IA en sector servicios 2026"
                   class="w-full px-3 py-2 rounded-lg bg-[var(--app-bg)] border border-[var(--app-card-border)] text-[var(--app-text)] focus:border-blue-500 focus:outline-hidden" />
          </div>

          <!-- URL Field (if url mode) -->
          <div *ngIf="leadType === 'url'" class="space-y-1">
            <label class="font-bold text-[var(--app-text)]">URL de Origen *</label>
            <div class="relative">
              <input type="url" [(ngModel)]="originUrl" name="originUrl" required
                     placeholder="https://elpais.com/tecnologia/..."
                     class="w-full pl-8 pr-3 py-2 rounded-lg bg-[var(--app-bg)] border border-[var(--app-card-border)] text-[var(--app-text)] focus:border-blue-500 focus:outline-hidden font-mono text-[11px]" />
              <i class="pi pi-globe absolute left-2.5 top-2.5 text-[var(--app-muted)] text-xs"></i>
            </div>
            <p class="text-[10px] text-[var(--app-muted)]">El sistema extraerá el texto in-process y computará el SHA-256 inmutable.</p>
          </div>

          <!-- Role -->
          <div class="space-y-1">
            <label class="font-bold text-[var(--app-text)]">Rol en el Contenido</label>
            <select [(ngModel)]="role" name="role"
                    class="w-full px-3 py-2 rounded-lg bg-[var(--app-bg)] border border-[var(--app-card-border)] text-[var(--app-text)] focus:border-blue-500 focus:outline-hidden">
              <option value="PrimaryLead">PrimaryLead (Fuente principal que originó la pieza)</option>
              <option value="SupportingEvidence">SupportingEvidence (Evidencia de apoyo o contexto)</option>
              <option value="Counterpoint">Counterpoint (Contrapunto o perspectiva crítica)</option>
              <option value="StyleReference">StyleReference (Referencia de formato o estilo)</option>
            </select>
          </div>

          <!-- Text content -->
          <div class="space-y-1">
            <label class="font-bold text-[var(--app-text)]">
              <span>{{ leadType === 'url' ? 'Texto / Fragmento Clave (Opcional)' : 'Contenido Textual de la Evidencia *' }}</span>
            </label>
            <textarea [(ngModel)]="contentText" name="contentText" rows="4" [required]="leadType === 'text'"
                      placeholder="Extractos textuales o notas directas..."
                      class="w-full px-3 py-2 rounded-lg bg-[var(--app-bg)] border border-[var(--app-card-border)] text-[var(--app-text)] focus:border-blue-500 focus:outline-hidden leading-relaxed"></textarea>
          </div>

          <!-- Notes -->
          <div class="space-y-1">
            <label class="font-bold text-[var(--app-text)]">Notas Editoriales (Opcional)</label>
            <input type="text" [(ngModel)]="notes" name="notes"
                   placeholder="Instrucciones para el redactor o síntesis de TruthSource"
                   class="w-full px-3 py-2 rounded-lg bg-[var(--app-bg)] border border-[var(--app-card-border)] text-[var(--app-text)] focus:border-blue-500 focus:outline-hidden" />
          </div>

          <!-- Error Alert -->
          <div *ngIf="errorMessage" class="p-2.5 rounded-lg bg-red-500/10 border border-red-500/30 text-red-600 dark:text-red-400 flex items-center gap-2">
            <i class="pi pi-exclamation-triangle"></i>
            <span>{{ errorMessage }}</span>
          </div>

          <!-- Actions -->
          <div class="flex items-center justify-end gap-2 pt-2 border-t border-[var(--app-card-border)]">
            <button type="button" (click)="close()" 
                    class="px-3 py-1.5 rounded-lg border border-[var(--app-card-border)] hover:bg-[var(--app-surface-hover)] text-[var(--app-muted)] hover:text-[var(--app-text)] transition-colors cursor-pointer font-medium">
              Cancelar
            </button>
            <button type="submit" [disabled]="isSubmitting || !title || (leadType === 'url' && !originUrl) || (leadType === 'text' && !contentText)"
                    class="px-4 py-1.5 rounded-lg bg-blue-600 hover:bg-blue-500 text-white font-bold transition-all disabled:opacity-50 disabled:cursor-not-allowed flex items-center gap-1.5 cursor-pointer shadow-xs">
              <i *ngIf="isSubmitting" class="pi pi-spin pi-spinner text-xs"></i>
              <span>{{ isSubmitting ? 'Capturando...' : 'Adjuntar y Hashear' }}</span>
            </button>
          </div>

        </form>
      </div>
    </div>
  `
})
export class AttachEvidenceModalComponent {
  private readonly api = inject(ApiService);

  @Input() isOpen = false;
  @Input() contentItemId!: string;
  @Output() closed = new EventEmitter<void>();
  @Output() attached = new EventEmitter<ContentItemEvidenceDto>();

  leadType: 'url' | 'text' = 'url';
  title = '';
  originUrl = '';
  role = 'SupportingEvidence';
  contentText = '';
  notes = '';
  isSubmitting = false;
  errorMessage: string | null = null;

  close() {
    this.reset();
    this.closed.emit();
  }

  submit() {
    if (!this.title) return;
    if (this.leadType === 'url' && !this.originUrl) return;
    if (this.leadType === 'text' && !this.contentText) return;

    this.isSubmitting = true;
    this.errorMessage = null;

    const req: AttachEvidenceRequest = {
      title: this.title.trim(),
      originUrl: this.leadType === 'url' ? this.originUrl.trim() : null,
      contentText: this.contentText ? this.contentText.trim() : null,
      role: this.role,
      notes: this.notes ? this.notes.trim() : null
    };

    this.api.attachEvidence(this.contentItemId, req).subscribe({
      next: (evidence) => {
        this.isSubmitting = false;
        this.attached.emit(evidence);
        this.close();
      },
      error: (err) => {
        this.isSubmitting = false;
        this.errorMessage = err.error?.message || err.error?.error || 'Error al capturar evidencia.';
      }
    });
  }

  private reset() {
    this.leadType = 'url';
    this.title = '';
    this.originUrl = '';
    this.role = 'SupportingEvidence';
    this.contentText = '';
    this.notes = '';
    this.isSubmitting = false;
    this.errorMessage = null;
  }
}
