import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-reject-storyboard-modal',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/60 backdrop-blur-xs animate-fade-in">
      <div class="bg-[var(--app-card-bg)] border border-[var(--app-card-border)] rounded-2xl w-full max-w-md shadow-2xl overflow-hidden flex flex-col">
        
        <!-- Modal Header -->
        <div class="px-5 py-4 border-b border-[var(--app-card-border)] flex items-center justify-between bg-red-500/10">
          <div class="flex items-center gap-2">
            <div class="w-8 h-8 rounded-lg bg-red-500/20 text-red-600 dark:text-red-400 flex items-center justify-center border border-red-500/30">
              <i class="pi pi-times-circle text-base"></i>
            </div>
            <div>
              <h2 class="text-sm font-bold text-[var(--app-text)]">Rechazar Storyboard y Plan</h2>
              <p class="text-[11px] text-[var(--app-muted)]">Devolver la planificación a borrador con correcciones</p>
            </div>
          </div>
          <button (click)="cancel.emit()" class="text-[var(--app-muted)] hover:text-[var(--app-text)] p-1 rounded-lg hover:bg-[var(--app-card-border)] transition-colors">
            <i class="pi pi-times text-xs"></i>
          </button>
        </div>

        <!-- Modal Body -->
        <div class="p-5 space-y-3 text-xs">
          <label class="block font-bold text-[var(--app-text)]">
            Motivo Obligatorio del Rechazo <span class="text-red-500">*</span>
          </label>
          <p class="text-[11px] text-[var(--app-muted)]">
            Explica con precisión qué aspectos visuales, encuadres, tiempos o requerimientos deben corregirse.
          </p>
          <textarea [(ngModel)]="reason" rows="4"
                    class="cf-input w-full text-xs resize-none font-sans"
                    placeholder="Ej: Los encuadres de la escena 2 no corresponden al tono sobrio del canal; ajustar el ritmo de las tomas..."></textarea>
          
          <div *ngIf="showValidation && !reason.trim()" class="text-red-500 text-[11px] flex items-center gap-1 font-semibold">
            <i class="pi pi-exclamation-triangle text-[10px]"></i>
            <span>Debes ingresar un motivo para el rechazo.</span>
          </div>
        </div>

        <!-- Modal Footer -->
        <div class="px-5 py-3 border-t border-[var(--app-card-border)] flex items-center justify-end gap-2 bg-[var(--app-bg)]">
          <button (click)="cancel.emit()" [disabled]="isLoading" class="cf-btn-secondary">
            Cancelar
          </button>
          <button (click)="submit()" [disabled]="isLoading" class="cf-btn-danger">
            <i class="pi" [ngClass]="isLoading ? 'pi-spin pi-spinner' : 'pi-times'"></i>
            <span>{{ isLoading ? 'Rechazando...' : 'Confirmar Rechazo' }}</span>
          </button>
        </div>

      </div>
    </div>
  `
})
export class RejectStoryboardModalComponent {
  @Input() isLoading = false;
  @Output() reject = new EventEmitter<string>();
  @Output() cancel = new EventEmitter<void>();

  reason = '';
  showValidation = false;

  submit() {
    if (!this.reason.trim()) {
      this.showValidation = true;
      return;
    }
    this.reject.emit(this.reason.trim());
  }
}
