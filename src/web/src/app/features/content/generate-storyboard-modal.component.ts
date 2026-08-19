import { Component, Input, Output, EventEmitter, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { PlanStoryboardOptions } from '../../core/api.service';

@Component({
  selector: 'app-generate-storyboard-modal',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/60 backdrop-blur-xs animate-fade-in">
      <div class="bg-[var(--app-card-bg)] border border-[var(--app-card-border)] rounded-2xl w-full max-w-lg shadow-2xl overflow-hidden flex flex-col max-h-[90vh]">
        
        <!-- Modal Header -->
        <div class="px-5 py-4 border-b border-[var(--app-card-border)] flex items-center justify-between bg-[var(--app-bg)]">
          <div class="flex items-center gap-2">
            <div class="w-8 h-8 rounded-lg bg-blue-500/15 text-blue-600 dark:text-blue-400 flex items-center justify-center border border-blue-500/30">
              <i class="pi pi-sparkles text-sm"></i>
            </div>
            <div>
              <h2 class="text-sm font-bold text-[var(--app-text)]">Generar Storyboard con IA</h2>
              <p class="text-[11px] text-[var(--app-muted)]">Planificación visual vertical 9:16 basada en el guión aprobado</p>
            </div>
          </div>
          <button (click)="cancel.emit()" class="text-[var(--app-muted)] hover:text-[var(--app-text)] p-1 rounded-lg hover:bg-[var(--app-card-border)] transition-colors">
            <i class="pi pi-times text-xs"></i>
          </button>
        </div>

        <!-- Modal Body -->
        <div class="p-5 space-y-4 overflow-y-auto text-xs">
          
          <!-- Visual Style Preset Selection -->
          <div class="space-y-1.5">
            <label class="block font-bold text-[var(--app-text)]">
              Estilo Visual Predeterminado
            </label>
            <p class="text-[11px] text-[var(--app-muted)]">Define la dirección artística para los prompts visuales de cada toma</p>
            <div class="grid grid-cols-1 sm:grid-cols-2 gap-2 pt-1">
              <div *ngFor="let style of stylePresets" 
                   (click)="selectedStyle = style.id"
                   class="p-2.5 rounded-lg border cursor-pointer transition-all flex flex-col justify-between"
                   [ngClass]="selectedStyle === style.id ? 'border-blue-500 bg-blue-500/10 ring-1 ring-blue-500/40' : 'border-[var(--app-card-border)] bg-[var(--app-bg)] hover:border-blue-500/30'">
                <div class="font-bold text-[var(--app-text)] flex items-center justify-between text-xs">
                  <span>{{ style.name }}</span>
                  <i *ngIf="selectedStyle === style.id" class="pi pi-check text-blue-500 text-[10px]"></i>
                </div>
                <div class="text-[10px] text-[var(--app-muted)] mt-1">
                  {{ style.description }}
                </div>
              </div>
            </div>
          </div>

          <!-- Camera Motion Intensity -->
          <div class="space-y-1.5">
            <label class="block font-bold text-[var(--app-text)]">
              Intensidad de Movimiento de Cámara
            </label>
            <div class="grid grid-cols-3 gap-2">
              <button *ngFor="let motion of motionIntensities"
                      type="button"
                      (click)="selectedMotion = motion.id"
                      class="py-2 px-3 rounded-lg border text-center font-semibold text-xs transition-all"
                      [ngClass]="selectedMotion === motion.id ? 'border-blue-500 bg-blue-500/15 text-blue-600 dark:text-blue-400 font-bold' : 'border-[var(--app-card-border)] bg-[var(--app-bg)] text-[var(--app-muted)] hover:text-[var(--app-text)]'">
                {{ motion.label }}
              </button>
            </div>
          </div>

          <!-- Frame Density Multiplier -->
          <div class="space-y-1.5">
            <label class="block font-bold text-[var(--app-text)]">
              Densidad de Tomas por Escena
            </label>
            <select [(ngModel)]="frameDensity" class="cf-input w-full text-xs py-1.5">
              <option [value]="1.0">Estándar (1 toma por escena de guión - ritmo pausado)</option>
              <option [value]="1.5">Dinámica (1 a 2 tomas por escena - ritmo equilibrado)</option>
              <option [value]="2.0">Alta Densidad (2 a 3 tomas por escena - ritmo dinámico TikTok/Reels)</option>
            </select>
          </div>

          <!-- Target Duration Override (Optional) -->
          <div class="space-y-1.5">
            <label class="block font-bold text-[var(--app-text)]">
              Duración Objetivo (Segundos)
            </label>
            <input type="number" [(ngModel)]="targetDuration" min="10" max="300"
                   class="cf-input w-full text-xs font-mono py-1.5" />
          </div>

          <!-- Advisory Context Notice -->
          <div class="p-3 rounded-lg bg-blue-500/10 border border-blue-500/20 text-blue-600 dark:text-blue-400 text-[11px] flex items-start gap-2">
            <i class="pi pi-info-circle text-xs shrink-0 mt-0.5"></i>
            <div>
              La IA estructurará las tomas en proporción vertical 9:16, estimará los tiempos alineados a la narración y generará un plan de activos neutro.
            </div>
          </div>

        </div>

        <!-- Modal Footer -->
        <div class="px-5 py-3 border-t border-[var(--app-card-border)] flex items-center justify-end gap-2 bg-[var(--app-bg)]">
          <button (click)="cancel.emit()" [disabled]="isLoading" class="cf-btn-secondary">
            Cancelar
          </button>
          <button (click)="onSubmit()" [disabled]="isLoading" class="cf-btn-primary">
            <i class="pi" [ngClass]="isLoading ? 'pi-spin pi-spinner' : 'pi-sparkles'"></i>
            <span>{{ isLoading ? 'Generando Plan...' : 'Comenzar Generación' }}</span>
          </button>
        </div>

      </div>
    </div>
  `
})
export class GenerateStoryboardModalComponent implements OnInit {
  @Input() initialDurationSeconds: number = 45;
  @Input() isLoading: boolean = false;

  @Output() generate = new EventEmitter<PlanStoryboardOptions>();
  @Output() cancel = new EventEmitter<void>();

  selectedStyle = 'cinematic';
  selectedMotion = 'dynamic';
  frameDensity = 1.0;
  targetDuration = 45;

  readonly stylePresets = [
    { id: 'cinematic', name: 'Cinemático Realista', description: 'Iluminación dramática, 8k, texturas fotorrealistas' },
    { id: '3d_render', name: '3D Render Isométrico', description: 'Estilo blender, minimalista, limpio, colores vivos' },
    { id: 'dark_tech', name: 'Cyberpunk Dark Tech', description: 'Luces neón, HUD futurista, interfaz tecnológica' },
    { id: 'editorial', name: 'Editorial Minimalista', description: 'Composición limpia, fotografía periodística sobria' }
  ];

  readonly motionIntensities = [
    { id: 'subtle', label: 'Suave / Estático' },
    { id: 'dynamic', label: 'Dinámico' },
    { id: 'high_energy', label: 'Alta Energía' }
  ];

  ngOnInit() {
    if (this.initialDurationSeconds > 0) {
      this.targetDuration = this.initialDurationSeconds;
    }
  }

  onSubmit() {
    const options: PlanStoryboardOptions = {
      targetDurationSeconds: this.targetDuration > 0 ? this.targetDuration : 45,
      visualStylePreset: this.selectedStyle,
      cameraMotionIntensity: this.selectedMotion,
      frameDensityMultiplier: Number(this.frameDensity)
    };
    this.generate.emit(options);
  }
}
