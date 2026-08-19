import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ContentIdeaDto, GenerateScriptOptions, TruthSourceDto } from '../../core/api.service';

@Component({
  selector: 'app-generate-script-modal',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div *ngIf="isOpen" class="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/60 backdrop-blur-xs animate-fade-in">
      <div class="bg-[var(--app-card-bg)] border border-[var(--app-card-border)] rounded-xl w-full max-w-xl shadow-2xl overflow-hidden animate-scale-in text-xs">
        
        <!-- Header -->
        <div class="px-5 py-4 border-b border-[var(--app-card-border)] flex items-center justify-between bg-[var(--app-bg)]/50">
          <div class="flex items-center gap-2">
            <div class="w-8 h-8 rounded-lg bg-blue-500/10 border border-blue-500/20 flex items-center justify-center text-blue-500">
              <i class="pi pi-sparkles text-sm"></i>
            </div>
            <div>
              <h3 class="font-bold text-sm text-[var(--app-text)]">Generar Guión con IA</h3>
              <p class="text-[10px] text-[var(--app-muted)]">Estructuración escena por escena basada en TruthSource e Idea seleccionada</p>
            </div>
          </div>
          <button (click)="close()" class="p-1 rounded-md text-[var(--app-muted)] hover:text-[var(--app-text)] cursor-pointer">
            <i class="pi pi-times"></i>
          </button>
        </div>

        <!-- Body -->
        <div class="p-5 space-y-4 max-h-[75vh] overflow-y-auto">
          
          <!-- Selected Idea Context Banner -->
          <div *ngIf="selectedIdea" class="p-3 rounded-lg bg-purple-500/10 border border-purple-500/20 space-y-1">
            <span class="text-[10px] font-bold uppercase tracking-wider text-purple-600 dark:text-purple-400 block">Idea Seleccionada</span>
            <p class="font-bold text-xs text-[var(--app-text)]">{{ selectedIdea.title }}</p>
            <p class="text-[11px] text-[var(--app-muted)] line-clamp-1 italic">"{{ selectedIdea.hookStrategy }}"</p>
          </div>

          <!-- Target Duration & Speaking Rate (Pacing) -->
          <div class="grid grid-cols-2 gap-4">
            
            <div class="space-y-1">
              <label class="font-bold text-[var(--app-text)] flex items-center gap-1">
                <i class="pi pi-clock text-blue-500"></i>
                <span>Duración Objetivo (s)</span>
              </label>
              <select [(ngModel)]="targetDurationSeconds" class="w-full p-2 rounded-lg bg-[var(--app-bg)] border border-[var(--app-card-border)] text-[var(--app-text)] font-mono text-xs">
                <option [ngValue]="30">30 segundos (Ultra corto)</option>
                <option [ngValue]="45">45 segundos (Estándar Shorts)</option>
                <option [ngValue]="60">60 segundos (Máximo Shorts)</option>
              </select>
            </div>

            <div class="space-y-1">
              <label class="font-bold text-[var(--app-text)] flex items-center gap-1">
                <i class="pi pi-sliders-h text-indigo-500"></i>
                <span>Pacing de Lectura (WPM)</span>
              </label>
              <select [(ngModel)]="pacingWpm" class="w-full p-2 rounded-lg bg-[var(--app-bg)] border border-[var(--app-card-border)] text-[var(--app-text)] font-mono text-xs">
                <option [ngValue]="130">130 WPM (Pausado / Énfasis)</option>
                <option [ngValue]="140">140 WPM (IA Simple ES - Estándar)</option>
                <option [ngValue]="150">150 WPM (Dinámico / Rápido)</option>
              </select>
            </div>

          </div>

          <!-- Calculated Target Words Preview -->
          <div class="p-2.5 rounded-lg bg-[var(--app-bg)] border border-[var(--app-card-border)] flex items-center justify-between font-mono text-[11px]">
            <span class="text-[var(--app-muted)]">Volumen estimado de palabras:</span>
            <span class="font-bold text-blue-600 dark:text-blue-400">
              ~{{ estimatedTargetWords }} palabras
            </span>
          </div>

          <!-- Tone & Stylistic Nuance -->
          <div class="space-y-1">
            <label class="font-bold text-[var(--app-text)]">Tono Estilístico</label>
            <select [(ngModel)]="toneStyle" class="w-full p-2 rounded-lg bg-[var(--app-bg)] border border-[var(--app-card-border)] text-[var(--app-text)] text-xs">
              <option value="Sobrio y accesible">Sobrio, profesional y accesible (Recomendado para IA Simple ES)</option>
              <option value="Directo y contundente">Directo, dinámico y contundente</option>
              <option value="Didáctico paso a paso">Didáctico y tutorial paso a paso</option>
              <option value="Analítico y crítico">Analítico, crítico y riguroso</option>
            </select>
          </div>

          <!-- Custom Editorial Instructions -->
          <div class="space-y-1">
            <label class="font-bold text-[var(--app-text)]">Instrucciones Adicionales (Opcional)</label>
            <textarea [(ngModel)]="customInstructions" rows="2"
                      placeholder="Ej. Enfatizar que no se requiere saber programación; reforzar la llamada a la acción hacia comentarios..."
                      class="w-full p-2.5 rounded-lg bg-[var(--app-bg)] border border-[var(--app-card-border)] text-[var(--app-text)] text-xs leading-relaxed focus:outline-none focus:border-blue-500 transition-colors"></textarea>
          </div>

          <!-- Inherited Guardrails Note -->
          <div class="p-2.5 rounded bg-amber-500/10 border border-amber-500/20 text-amber-700 dark:text-amber-300 text-[10px] space-y-0.5">
            <div class="font-bold flex items-center gap-1">
              <i class="pi pi-shield"></i>
              <span>Guardrails del TruthSource aplicados automáticamente</span>
            </div>
            <p>Se mantendrán todas las restricciones de 'Do Not Say' y las afirmaciones verificables vinculadas.</p>
          </div>

        </div>

        <!-- Footer -->
        <div class="px-5 py-3 border-t border-[var(--app-card-border)] bg-[var(--app-bg)]/50 flex items-center justify-end gap-2">
          <button (click)="close()" [disabled]="isLoading"
                  class="px-3.5 py-1.5 rounded-lg border border-[var(--app-card-border)] text-[var(--app-text)] hover:bg-[var(--app-card-bg)] cursor-pointer font-semibold text-xs disabled:opacity-50">
            Cancelar
          </button>
          <button (click)="submit()" [disabled]="isLoading"
                  class="px-4 py-1.5 rounded-lg bg-blue-600 hover:bg-blue-500 text-white font-bold text-xs flex items-center gap-1.5 cursor-pointer disabled:opacity-50 shadow-xs">
            <i *ngIf="isLoading" class="pi pi-spin pi-spinner text-xs"></i>
            <i *ngIf="!isLoading" class="pi pi-sparkles text-xs"></i>
            <span>{{ isLoading ? 'Generando Guión...' : 'Generar Guión con IA' }}</span>
          </button>
        </div>

      </div>
    </div>
  `
})
export class GenerateScriptModalComponent {
  @Input() isOpen: boolean = false;
  @Input() isLoading: boolean = false;
  @Input() selectedIdea: ContentIdeaDto | null = null;
  @Input() truthSource: TruthSourceDto | null = null;

  @Output() closed = new EventEmitter<void>();
  @Output() generate = new EventEmitter<GenerateScriptOptions>();

  targetDurationSeconds: number = 45;
  pacingWpm: number = 140;
  toneStyle: string = 'Sobrio y accesible';
  customInstructions: string = '';

  get estimatedTargetWords(): number {
    return Math.round((this.targetDurationSeconds * this.pacingWpm) / 60);
  }

  close() {
    this.closed.emit();
  }

  submit() {
    this.generate.emit({
      targetDurationSeconds: this.targetDurationSeconds,
      pacingWpm: this.pacingWpm,
      toneStyle: this.toneStyle,
      customInstructions: this.customInstructions?.trim() || null
    });
  }
}
