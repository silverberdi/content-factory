import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-reject-script-modal',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div *ngIf="isOpen" class="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/60 backdrop-blur-xs animate-fade-in">
      <div class="bg-[var(--app-card-bg)] border border-[var(--app-card-border)] rounded-xl w-full max-w-md shadow-2xl overflow-hidden animate-scale-in text-xs">
        
        <!-- Header -->
        <div class="px-5 py-4 border-b border-[var(--app-card-border)] flex items-center justify-between bg-[var(--app-bg)]/50">
          <div class="flex items-center gap-2">
            <div class="w-8 h-8 rounded-lg bg-red-500/10 border border-red-500/20 flex items-center justify-center text-red-500">
              <i class="pi pi-times-circle text-sm"></i>
            </div>
            <div>
              <h3 class="font-bold text-sm text-[var(--app-text)]">Rechazar Guión</h3>
              <p class="text-[10px] text-[var(--app-muted)]">El guión entrará en estado Rechazado hasta su reapertura</p>
            </div>
          </div>
          <button (click)="close()" class="p-1 rounded-md text-[var(--app-muted)] hover:text-[var(--app-text)] cursor-pointer">
            <i class="pi pi-times"></i>
          </button>
        </div>

        <!-- Body -->
        <div class="p-5 space-y-3">
          <p class="text-[var(--app-text)] leading-relaxed">
            Indica el motivo detallado del rechazo editorial para que el operador o guionista pueda corregir la locución o visuales.
          </p>

          <div class="space-y-1">
            <label class="font-bold text-[var(--app-text)]">
              Motivo del Rechazo <span class="text-red-500">*</span>
            </label>
            <textarea [(ngModel)]="reason" rows="3"
                      placeholder="Ej. La afirmación en la escena #3 excede los datos del TruthSource; recortar gancho para no superar 3 segundos..."
                      class="w-full p-2.5 rounded-lg bg-[var(--app-bg)] border border-[var(--app-card-border)] text-[var(--app-text)] text-xs leading-relaxed focus:outline-none focus:border-red-500 transition-colors"></textarea>
          </div>

          <div class="p-2.5 rounded bg-amber-500/10 border border-amber-500/20 text-amber-700 dark:text-amber-300 text-[10px]">
            <i class="pi pi-info-circle mr-1"></i>
            <span>Para continuar editando tras el rechazo, se requerirá una reapertura explícita a estado Borrador (Draft).</span>
          </div>
        </div>

        <!-- Footer -->
        <div class="px-5 py-3 border-t border-[var(--app-card-border)] bg-[var(--app-bg)]/50 flex items-center justify-end gap-2">
          <button (click)="close()" [disabled]="isLoading"
                  class="px-3.5 py-1.5 rounded-lg border border-[var(--app-card-border)] text-[var(--app-text)] hover:bg-[var(--app-card-bg)] cursor-pointer font-semibold text-xs disabled:opacity-50">
            Cancelar
          </button>
          <button (click)="submit()" [disabled]="!reason.trim() || isLoading"
                  class="px-4 py-1.5 rounded-lg bg-red-600 hover:bg-red-500 text-white font-bold text-xs flex items-center gap-1.5 cursor-pointer disabled:opacity-50 shadow-xs">
            <i *ngIf="isLoading" class="pi pi-spin pi-spinner text-xs"></i>
            <i *ngIf="!isLoading" class="pi pi-times text-xs"></i>
            <span>{{ isLoading ? 'Rechazando...' : 'Confirmar Rechazo' }}</span>
          </button>
        </div>

      </div>
    </div>
  `
})
export class RejectScriptModalComponent {
  @Input() isOpen: boolean = false;
  @Input() isLoading: boolean = false;
  @Output() closed = new EventEmitter<void>();
  @Output() rejected = new EventEmitter<string>();

  reason: string = '';

  close() {
    this.reason = '';
    this.closed.emit();
  }

  submit() {
    if (!this.reason.trim()) return;
    this.rejected.emit(this.reason.trim());
    this.reason = '';
  }
}
