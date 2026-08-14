import { Component, EventEmitter, Input, Output, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService, ChannelDto, QuickSubmitCandidateRequest } from '../../core/api.service';

@Component({
  selector: 'app-quick-submit-modal',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div *ngIf="isOpen" class="fixed inset-0 z-50 overflow-y-auto flex items-center justify-center p-4">
      <!-- Backdrop -->
      <div (click)="close()" class="fixed inset-0 bg-slate-900/50 dark:bg-black/70 backdrop-blur-xs transition-opacity"></div>

      <!-- Modal Card -->
      <div class="relative w-full max-w-lg rounded-2xl bg-[var(--app-card-bg)] border border-[var(--app-card-border)] shadow-2xl p-5 sm:p-6 z-10 space-y-4">
        
        <!-- Header -->
        <div class="flex items-start justify-between border-b border-[var(--app-card-border)] pb-3">
          <div>
            <div class="flex items-center gap-2">
              <div class="w-7 h-7 rounded-lg bg-blue-600/15 text-blue-600 dark:text-blue-400 flex items-center justify-center font-bold text-xs">
                <i class="pi pi-bolt"></i>
              </div>
              <h2 class="text-base font-bold text-[var(--app-text)]">Quick Submit</h2>
            </div>
            <p class="text-xs text-[var(--app-muted)] mt-1">Add a URL or note for discovery.</p>
          </div>
          <button (click)="close()" class="p-1 rounded-lg hover:bg-[var(--app-surface-hover)] text-[var(--app-muted)] hover:text-[var(--app-text)] transition-colors cursor-pointer">
            <i class="pi pi-times text-xs"></i>
          </button>
        </div>

        <!-- Error feedback -->
        <div *ngIf="errorMessage" class="p-3 rounded-lg bg-red-500/10 border border-red-500/30 text-red-600 dark:text-red-400 text-xs font-medium">
          {{ errorMessage }}
        </div>

        <!-- Form Fields -->
        <form (ngSubmit)="submit()" class="space-y-3.5 text-xs">
          <!-- Channel selector -->
          <div>
            <label class="block font-bold text-[var(--app-text)] mb-1">Canal de Destino *</label>
            <select [(ngModel)]="channelId" name="channelId" required
                    class="w-full text-xs p-2 rounded-lg border border-[var(--app-card-border)] bg-[var(--app-bg)] text-[var(--app-text)] focus:outline-none focus:border-blue-500">
              <option *ngFor="let ch of channels" [value]="ch.id">{{ ch.name }} ({{ ch.language | uppercase }})</option>
            </select>
          </div>

          <!-- URL Input (optional) -->
          <div>
            <label class="block font-bold text-[var(--app-text)] mb-1">URL de Origen (Opcional si es nota directa)</label>
            <div class="relative">
              <input type="url" [(ngModel)]="externalUrl" name="externalUrl" placeholder="https://..."
                     class="w-full text-xs pl-8 pr-3 py-2 rounded-lg border border-[var(--app-card-border)] bg-[var(--app-bg)] text-[var(--app-text)] focus:outline-none focus:border-blue-500 font-mono" />
              <i class="pi pi-link absolute left-2.5 top-2.5 text-[var(--app-muted)] text-xs"></i>
            </div>
            <span class="text-[10px] text-[var(--app-muted)] mt-0.5 block">Los parámetros de rastreo se limpiarán automáticamente.</span>
          </div>

          <!-- Title / Main Lead -->
          <div>
            <label class="block font-bold text-[var(--app-text)] mb-1">Título o Idea Principal *</label>
            <input type="text" [(ngModel)]="title" name="title" required placeholder="Ej: Nuevo avance en agentes autónomos para pymes"
                   class="w-full text-xs p-2 rounded-lg border border-[var(--app-card-border)] bg-[var(--app-bg)] text-[var(--app-text)] focus:outline-none focus:border-blue-500" />
          </div>

          <!-- Notes / Summary -->
          <div>
            <label class="block font-bold text-[var(--app-text)] mb-1">Detalles / Nota Editorial (Opcional)</label>
            <textarea [(ngModel)]="summary" name="summary" rows="3" placeholder="Contexto clave, puntos a verificar, datos relevantes..."
                      class="w-full text-xs p-2 rounded-lg border border-[var(--app-card-border)] bg-[var(--app-bg)] text-[var(--app-text)] focus:outline-none focus:border-blue-500"></textarea>
          </div>

          <!-- Actions -->
          <div class="flex items-center justify-end gap-2 pt-2 border-t border-[var(--app-card-border)]">
            <button type="button" (click)="close()" class="px-3 py-1.5 rounded-lg border border-[var(--app-card-border)] bg-[var(--app-bg)] text-[var(--app-muted)] hover:text-[var(--app-text)] text-xs font-semibold cursor-pointer">
              Cancelar
            </button>
            <button type="submit" [disabled]="isSubmitting || !channelId || (!title && !externalUrl)"
                    class="px-4 py-1.5 rounded-lg bg-blue-600 hover:bg-blue-700 disabled:opacity-50 text-white text-xs font-bold transition-all shadow-sm flex items-center gap-1.5 cursor-pointer">
              <i *ngIf="isSubmitting" class="pi pi-spin pi-spinner text-xs"></i>
              <span>Enviar a Triage</span>
            </button>
          </div>
        </form>

      </div>
    </div>
  `
})
export class QuickSubmitModalComponent {
  private readonly api = inject(ApiService);

  @Input() isOpen = false;
  @Input() channels: ChannelDto[] = [];
  @Input() defaultChannelId: string = '';
  @Output() onClose = new EventEmitter<void>();
  @Output() onSubmitted = new EventEmitter<void>();

  channelId = '';
  externalUrl = '';
  title = '';
  summary = '';
  errorMessage = '';
  isSubmitting = false;

  ngOnChanges() {
    if (this.defaultChannelId) {
      this.channelId = this.defaultChannelId;
    } else if (this.channels.length > 0 && !this.channelId) {
      this.channelId = this.channels[0].id;
    }
  }

  close() {
    this.errorMessage = '';
    this.isSubmitting = false;
    this.externalUrl = '';
    this.title = '';
    this.summary = '';
    this.onClose.emit();
  }

  submit() {
    if (!this.channelId) {
      this.errorMessage = 'Debe seleccionar un canal de destino.';
      return;
    }
    if (!this.title.trim() && !this.externalUrl.trim()) {
      this.errorMessage = 'Debe ingresar un título o una URL.';
      return;
    }

    this.isSubmitting = true;
    this.errorMessage = '';

    const req: QuickSubmitCandidateRequest = {
      channelId: this.channelId,
      externalUrl: this.externalUrl.trim() || null,
      title: this.title.trim() || this.externalUrl.trim(),
      summary: this.summary.trim() || null
    };

    this.api.quickSubmitCandidate(req).subscribe({
      next: () => {
        this.isSubmitting = false;
        this.externalUrl = '';
        this.title = '';
        this.summary = '';
        this.onSubmitted.emit();
        this.close();
      },
      error: (err) => {
        this.isSubmitting = false;
        this.errorMessage = err.error?.error || 'Error al enviar lead para discovery.';
      }
    });
  }
}
