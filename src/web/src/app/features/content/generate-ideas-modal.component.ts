import { Component, EventEmitter, Input, Output, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService, ContentIdeaDto, GenerateIdeasOptions } from '../../core/api.service';

@Component({
  selector: 'app-generate-ideas-modal',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div *ngIf="isOpen" class="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/60 backdrop-blur-xs animate-in fade-in duration-150">
      <div class="bg-[var(--app-card-bg)] border border-[var(--app-card-border)] rounded-2xl w-full max-w-lg shadow-2xl overflow-hidden flex flex-col max-h-[90vh] animate-in zoom-in-95 duration-150 text-xs">
        
        <!-- Header -->
        <div class="px-5 py-4 border-b border-[var(--app-card-border)] flex items-center justify-between bg-gradient-to-r from-purple-500/10 via-transparent to-blue-500/10">
          <div class="flex items-center gap-2.5">
            <div class="w-8 h-8 rounded-lg bg-purple-500/15 border border-purple-500/30 flex items-center justify-center text-purple-600 dark:text-purple-400">
              <i class="pi pi-sparkles text-sm"></i>
            </div>
            <div>
              <h3 class="text-sm font-bold text-[var(--app-text)]">Generar Propuestas de Ideas con IA</h3>
              <p class="text-[11px] text-[var(--app-muted)]">DeepSeek Reasoning • Basado en TruthSource v{{ truthSourceVersionNumber }}</p>
            </div>
          </div>
          <button (click)="close()" 
                  [disabled]="isGenerating()"
                  class="w-7 h-7 rounded-lg flex items-center justify-center text-[var(--app-muted)] hover:text-[var(--app-text)] hover:bg-[var(--app-bg)] transition-colors cursor-pointer disabled:opacity-50">
            <i class="pi pi-times text-xs"></i>
          </button>
        </div>

        <!-- Body -->
        <div class="p-5 space-y-4 overflow-y-auto">
          
          <!-- TruthSource Lineage Notice -->
          <div class="p-3 rounded-xl bg-purple-500/10 border border-purple-500/20 text-purple-900 dark:text-purple-200 flex items-start gap-2.5">
            <i class="pi pi-shield text-purple-600 dark:text-purple-400 text-sm mt-0.5 shrink-0"></i>
            <div class="space-y-0.5">
              <span class="font-bold text-[11px] block">Anclaje Factual Inmutable</span>
              <p class="text-[10px] text-purple-800 dark:text-purple-300 leading-relaxed">
                Las ideas se derivarán estrictamente de los hechos comprobados y ángulos aprobados de la versión actual. Duplicados y propuestas casi equivalentes se filtrarán automáticamente.
              </p>
            </div>
          </div>

          <!-- Error Alert -->
          <div *ngIf="errorMessage()" class="p-3 rounded-xl bg-red-500/10 border border-red-500/30 text-red-700 dark:text-red-300 flex items-start gap-2">
            <i class="pi pi-exclamation-circle text-red-500 mt-0.5 shrink-0"></i>
            <span class="text-xs">{{ errorMessage() }}</span>
          </div>

          <!-- Number of Ideas to generate -->
          <div class="space-y-1.5">
            <label class="font-bold text-[var(--app-text)] flex items-center justify-between">
              <span>Cantidad de Propuestas</span>
              <span class="font-mono text-purple-600 dark:text-purple-400 font-bold">{{ count }} ideas</span>
            </label>
            <div class="grid grid-cols-4 gap-2">
              <button *ngFor="let opt of [2, 3, 4, 5]"
                      type="button"
                      (click)="count = opt"
                      [class.bg-purple-600]="count === opt"
                      [class.text-white]="count === opt"
                      [class.border-purple-600]="count === opt"
                      [class.bg-[var(--app-bg)]]="count !== opt"
                      [class.text-[var(--app-text)]]="count !== opt"
                      [class.border-[var(--app-card-border)]]="count !== opt"
                      class="py-2 rounded-lg border font-bold text-xs cursor-pointer transition-all hover:border-purple-500/50">
                {{ opt }}
              </button>
            </div>
          </div>

          <!-- Angle Style / Focus -->
          <div class="space-y-1.5">
            <label class="font-bold text-[var(--app-text)]">Estilo de Ángulo / Enfoque (Opcional)</label>
            <select [(ngModel)]="angleStyle" 
                    class="w-full px-3 py-2 rounded-lg bg-[var(--app-bg)] border border-[var(--app-card-border)] text-[var(--app-text)] focus:border-purple-500 outline-hidden transition-all text-xs">
              <option value="">Balanceado / Mixto (Recomendado)</option>
              <option value="Contraintuitivo / Empoderamiento">Contraintuitivo / Empoderamiento</option>
              <option value="Tutorial Práctico Paso a Paso">Tutorial Práctico Paso a Paso</option>
              <option value="Alerta de Riesgo / Caso de Negocio">Alerta de Riesgo / Caso de Negocio</option>
              <option value="Debunking / Análisis Realista">Debunking / Análisis Realista</option>
            </select>
          </div>

          <!-- Target Audience Override -->
          <div class="space-y-1.5">
            <label class="font-bold text-[var(--app-text)]">Audiencia Objetivo / Nivel de Conciencia (Opcional)</label>
            <input type="text"
                   [(ngModel)]="targetAudience"
                   placeholder="Ej: Emprendedores y directores no técnicos buscando optimizar tiempo"
                   class="w-full px-3 py-2 rounded-lg bg-[var(--app-bg)] border border-[var(--app-card-border)] text-[var(--app-text)] focus:border-purple-500 outline-hidden transition-all text-xs" />
          </div>

          <!-- Generating Loader Indicator -->
          <div *ngIf="isGenerating()" class="p-4 rounded-xl bg-purple-500/5 border border-purple-500/20 text-center space-y-2">
            <i class="pi pi-spin pi-spinner text-xl text-purple-600 dark:text-purple-400 block mx-auto"></i>
            <div class="space-y-0.5">
              <span class="font-bold text-xs text-[var(--app-text)]">Razonando ángulos y deduplicando propuestas...</span>
              <p class="text-[10px] text-[var(--app-muted)]">Extrayendo ganchos de alta retención alineados a la línea editorial</p>
            </div>
          </div>

        </div>

        <!-- Footer -->
        <div class="px-5 py-3.5 border-t border-[var(--app-card-border)] bg-[var(--app-bg)] flex items-center justify-between">
          <button (click)="close()" 
                  [disabled]="isGenerating()"
                  class="px-3.5 py-1.5 rounded-lg border border-[var(--app-card-border)] text-[var(--app-muted)] hover:text-[var(--app-text)] hover:bg-[var(--app-card-bg)] font-semibold text-xs cursor-pointer transition-colors disabled:opacity-50">
            Cancelar
          </button>

          <button (click)="generate()" 
                  [disabled]="isGenerating()"
                  class="px-4 py-2 rounded-lg bg-gradient-to-r from-purple-600 to-indigo-600 hover:from-purple-500 hover:to-indigo-500 text-white font-bold text-xs flex items-center gap-1.5 cursor-pointer shadow-md shadow-purple-500/20 transition-all disabled:opacity-50">
            <i class="pi" [ngClass]="isGenerating() ? 'pi-spin pi-spinner' : 'pi-sparkles'"></i>
            <span>{{ isGenerating() ? 'Generando...' : 'Generar Ideas' }}</span>
          </button>
        </div>

      </div>
    </div>
  `
})
export class GenerateIdeasModalComponent {
  private readonly apiService = inject(ApiService);

  @Input() isOpen = false;
  @Input() contentItemId = '';
  @Input() truthSourceVersionNumber = 1;

  @Output() closeEvent = new EventEmitter<void>();
  @Output() ideasGenerated = new EventEmitter<ContentIdeaDto[]>();

  count = 3;
  angleStyle = '';
  targetAudience = '';

  isGenerating = signal<boolean>(false);
  errorMessage = signal<string | null>(null);

  close() {
    if (this.isGenerating()) return;
    this.errorMessage.set(null);
    this.closeEvent.emit();
  }

  generate() {
    if (!this.contentItemId || this.isGenerating()) return;
    this.isGenerating.set(true);
    this.errorMessage.set(null);

    const options: GenerateIdeasOptions = {
      count: this.count,
      focusAngleStyle: this.angleStyle || null,
      targetAudience: this.targetAudience || null
    };

    this.apiService.generateAiIdeas(this.contentItemId, options).subscribe({
      next: (ideas) => {
        this.isGenerating.set(false);
        this.ideasGenerated.emit(ideas);
        this.close();
      },
      error: (err) => {
        this.isGenerating.set(false);
        this.errorMessage.set(err?.error?.message || err?.message || 'Error al generar ideas con IA. Verifique que el TruthSource esté aprobado.');
      }
    });
  }
}
