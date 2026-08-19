import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { StoryboardFrameDto, FramingIntent, CameraMotionIntent, TransitionIntent } from '../../core/api.service';

@Component({
  selector: 'app-storyboard-frame-card',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="bg-[var(--app-card-bg)] border border-[var(--app-card-border)] rounded-xl p-4 transition-all duration-200 hover:border-blue-500/40 shadow-xs relative flex flex-col justify-between group"
         [ngClass]="{
           'ring-1 ring-blue-500/50': isSelected,
           'border-red-500/40': hasTimingWarning
         }">
      
      <!-- Card Header: Frame Number, Timing & Actions -->
      <div class="flex items-center justify-between gap-2 pb-3 border-b border-[var(--app-card-border)]">
        <div class="flex items-center gap-2">
          <span class="w-6 h-6 rounded-md bg-blue-500/15 text-blue-600 dark:text-blue-400 font-mono font-bold text-xs flex items-center justify-center border border-blue-500/30">
            #{{ frame.orderIndex }}
          </span>
          <span class="text-[11px] font-bold text-[var(--app-text)] truncate max-w-[140px] sm:max-w-[200px]">
            Escena {{ frame.scriptSceneOrderIndex }}
          </span>
        </div>

        <!-- Duration & Timing Badge -->
        <div class="flex items-center gap-1.5">
          <div class="flex items-center gap-1 px-2 py-0.5 rounded bg-[var(--app-bg)] border border-[var(--app-card-border)] text-[11px] font-mono text-[var(--app-text)] font-semibold">
            <i class="pi pi-clock text-[10px] text-blue-500"></i>
            <span *ngIf="isReadOnly">{{ frame.estimatedDurationSeconds }}s</span>
            <input *ngIf="!isReadOnly" type="number" step="0.5" min="0.5" max="60"
                   [(ngModel)]="frame.estimatedDurationSeconds" (ngModelChange)="onFieldChange()"
                   class="w-12 bg-transparent text-right outline-none font-mono font-bold text-xs focus:text-blue-500" />
            <span *ngIf="!isReadOnly" class="text-[10px] text-[var(--app-muted)]">s</span>
          </div>

          <!-- Move / Delete Actions -->
          <div *ngIf="!isReadOnly" class="flex items-center gap-0.5 opacity-80 group-hover:opacity-100 transition-opacity">
            <button (click)="moveUp.emit(frame.orderIndex)" [disabled]="isFirst" title="Mover arriba"
                    class="p-1 rounded hover:bg-[var(--app-surface-hover)] text-[var(--app-muted)] hover:text-[var(--app-text)] disabled:opacity-30 disabled:cursor-not-allowed">
              <i class="pi pi-arrow-up text-[10px]"></i>
            </button>
            <button (click)="moveDown.emit(frame.orderIndex)" [disabled]="isLast" title="Mover abajo"
                    class="p-1 rounded hover:bg-[var(--app-surface-hover)] text-[var(--app-muted)] hover:text-[var(--app-text)] disabled:opacity-30 disabled:cursor-not-allowed">
              <i class="pi pi-arrow-down text-[10px]"></i>
            </button>
            <button (click)="splitFrame.emit(frame)" title="Dividir toma"
                    class="p-1 rounded hover:bg-blue-500/15 text-[var(--app-muted)] hover:text-blue-500">
              <i class="pi pi-clone text-[10px]"></i>
            </button>
            <button (click)="deleteFrame.emit(frame.orderIndex)" title="Eliminar toma"
                    class="p-1 rounded hover:bg-red-500/15 text-[var(--app-muted)] hover:text-red-500">
              <i class="pi pi-trash text-[10px]"></i>
            </button>
          </div>
        </div>
      </div>

      <!-- Card Body: 9:16 Vertical Preview & Visual Intent -->
      <div class="grid grid-cols-1 md:grid-cols-12 gap-3.5 py-3">
        
        <!-- Left: 9:16 Vertical Framing Preview Aspect Box -->
        <div class="md:col-span-4 flex flex-col items-center">
          <div class="w-full max-w-[130px] aspect-[9/16] rounded-lg border-2 border-dashed border-[var(--app-card-border)] bg-gradient-to-b from-[var(--app-bg)] to-[var(--app-surface-hover)] p-2.5 flex flex-col justify-between relative overflow-hidden group/aspect transition-all duration-200 hover:border-blue-500/50 shadow-inner">
            
            <!-- 9:16 Top Tag & Framing Badge -->
            <div class="flex items-center justify-between gap-1 w-full z-10">
              <span class="px-1 py-0.2 rounded bg-black/70 text-white font-mono text-[8px] font-bold">
                9:16
              </span>
              <span class="px-1 py-0.2 rounded bg-blue-500/80 text-white font-mono text-[8px] font-bold truncate max-w-[70px]">
                {{ frame.framingIntent }}
              </span>
            </div>

            <!-- Visual Subject & Motion Silhouette Placeholder -->
            <div class="my-auto text-center space-y-1 z-10">
              <i class="pi pi-video text-xl text-[var(--app-muted)] group-hover/aspect:text-blue-500 transition-colors"></i>
              <div class="text-[9px] font-semibold text-[var(--app-text)] line-clamp-2 px-1">
                {{ frame.subject || 'Sujeto principal' }}
              </div>
            </div>

            <!-- Bottom On-Screen Text / Caption Overlay Preview -->
            <div class="w-full z-10" *ngIf="frame.onScreenText">
              <div class="bg-black/80 text-amber-300 text-[8px] font-bold text-center px-1 py-0.5 rounded backdrop-blur-xs truncate shadow-xs">
                "{{ frame.onScreenText }}"
              </div>
            </div>

            <!-- Background Aesthetic Grid Lines -->
            <div class="absolute inset-0 opacity-10 bg-[radial-gradient(#3b82f6_1px,transparent_1px)] [background-size:8px_8px] pointer-events-none"></div>
          </div>

          <!-- Motion & Transition Badges below aspect -->
          <div class="flex flex-wrap gap-1 items-center justify-center mt-2">
            <span class="px-1.5 py-0.5 rounded bg-purple-500/10 text-purple-600 dark:text-purple-400 border border-purple-500/20 text-[9px] font-mono font-medium flex items-center gap-1">
              <i class="pi pi-arrows-alt text-[8px]"></i> {{ frame.cameraMotionIntent }}
            </span>
            <span class="px-1.5 py-0.5 rounded bg-amber-500/10 text-amber-600 dark:text-amber-400 border border-amber-500/20 text-[9px] font-mono font-medium flex items-center gap-1">
              <i class="pi pi-arrow-right-arrow-left text-[8px]"></i> {{ frame.transitionIntent }}
            </span>
          </div>
        </div>

        <!-- Right: Frame Properties & Prompts Editor -->
        <div class="md:col-span-8 space-y-2.5">
          
          <!-- Selectors: Framing Intent & Camera Motion -->
          <div class="grid grid-cols-2 sm:grid-cols-3 gap-2">
            <div>
              <label class="block text-[10px] font-bold text-[var(--app-muted)] uppercase tracking-wider mb-0.5">Encuadre</label>
              <select *ngIf="!isReadOnly" [(ngModel)]="frame.framingIntent" (ngModelChange)="onFieldChange()"
                      class="cf-input text-xs w-full py-1">
                <option *ngFor="let opt of framingOptions" [value]="opt">{{ opt }}</option>
              </select>
              <span *ngIf="isReadOnly" class="text-xs font-semibold text-[var(--app-text)]">{{ frame.framingIntent }}</span>
            </div>

            <div>
              <label class="block text-[10px] font-bold text-[var(--app-muted)] uppercase tracking-wider mb-0.5">Mov. Cámara</label>
              <select *ngIf="!isReadOnly" [(ngModel)]="frame.cameraMotionIntent" (ngModelChange)="onFieldChange()"
                      class="cf-input text-xs w-full py-1">
                <option *ngFor="let opt of cameraMotionOptions" [value]="opt">{{ opt }}</option>
              </select>
              <span *ngIf="isReadOnly" class="text-xs font-semibold text-[var(--app-text)]">{{ frame.cameraMotionIntent }}</span>
            </div>

            <div class="col-span-2 sm:col-span-1">
              <label class="block text-[10px] font-bold text-[var(--app-muted)] uppercase tracking-wider mb-0.5">Transición</label>
              <select *ngIf="!isReadOnly" [(ngModel)]="frame.transitionIntent" (ngModelChange)="onFieldChange()"
                      class="cf-input text-xs w-full py-1">
                <option *ngFor="let opt of transitionOptions" [value]="opt">{{ opt }}</option>
              </select>
              <span *ngIf="isReadOnly" class="text-xs font-semibold text-[var(--app-text)]">{{ frame.transitionIntent }}</span>
            </div>
          </div>

          <!-- Visual Prompt Textarea -->
          <div>
            <label class="block text-[10px] font-bold text-[var(--app-muted)] uppercase tracking-wider mb-0.5">
              <i class="pi pi-sparkles text-blue-500 mr-1"></i>Prompt Visual
            </label>
            <textarea *ngIf="!isReadOnly" [(ngModel)]="frame.visualPrompt" (ngModelChange)="onFieldChange()"
                      rows="2" class="cf-input w-full text-xs font-sans resize-none"
                      placeholder="Descripción visual detallada para generación (9:16 vertical)..."></textarea>
            <p *ngIf="isReadOnly" class="text-xs text-[var(--app-text)] bg-[var(--app-bg)] p-2 rounded border border-[var(--app-card-border)] whitespace-pre-wrap">
              {{ frame.visualPrompt }}
            </p>
          </div>

          <!-- Subject & Environment Row -->
          <div class="grid grid-cols-1 sm:grid-cols-2 gap-2">
            <div>
              <label class="block text-[9px] font-semibold text-[var(--app-muted)] uppercase mb-0.5">Sujeto / Enfoque</label>
              <input *ngIf="!isReadOnly" type="text" [(ngModel)]="frame.subject" (ngModelChange)="onFieldChange()"
                     placeholder="Ej: Mano sosteniendo smartphone..." class="cf-input w-full text-xs py-1" />
              <span *ngIf="isReadOnly" class="text-xs text-[var(--app-text)] truncate block">{{ frame.subject || '—' }}</span>
            </div>

            <div>
              <label class="block text-[9px] font-semibold text-[var(--app-muted)] uppercase mb-0.5">Entorno / Iluminación</label>
              <input *ngIf="!isReadOnly" type="text" [(ngModel)]="frame.environment" (ngModelChange)="onFieldChange()"
                     placeholder="Ej: Oficina oscura, luz azul lateral..." class="cf-input w-full text-xs py-1" />
              <span *ngIf="isReadOnly" class="text-xs text-[var(--app-text)] truncate block">{{ frame.environment || '—' }}</span>
            </div>
          </div>

          <!-- Audio Cue & OnScreen Text Row -->
          <div class="grid grid-cols-1 sm:grid-cols-2 gap-2">
            <div>
              <label class="block text-[9px] font-semibold text-[var(--app-muted)] uppercase mb-0.5">
                <i class="pi pi-volume-up text-purple-500 mr-1 text-[8px]"></i>Cue de Audio / Locución
              </label>
              <input *ngIf="!isReadOnly" type="text" [(ngModel)]="frame.audioCue" (ngModelChange)="onFieldChange()"
                     placeholder="Sincronización con voz o efecto sonoro..." class="cf-input w-full text-xs py-1" />
              <span *ngIf="isReadOnly" class="text-xs text-[var(--app-text)] truncate block">{{ frame.audioCue || '—' }}</span>
            </div>

            <div>
              <label class="block text-[9px] font-semibold text-[var(--app-muted)] uppercase mb-0.5">
                <i class="pi pi-comment text-amber-500 mr-1 text-[8px]"></i>Texto en Pantalla (Overlay)
              </label>
              <input *ngIf="!isReadOnly" type="text" [(ngModel)]="frame.onScreenText" (ngModelChange)="onFieldChange()"
                     placeholder="Palabras clave en pantalla..." class="cf-input w-full text-xs py-1" />
              <span *ngIf="isReadOnly" class="text-xs text-[var(--app-text)] truncate block">{{ frame.onScreenText || '—' }}</span>
            </div>
          </div>

          <!-- Negative Prompt & Style Intent (Collapsible Details) -->
          <div *ngIf="showAdvanced" class="pt-2 border-t border-[var(--app-card-border)] space-y-2">
            <div class="grid grid-cols-1 sm:grid-cols-2 gap-2">
              <div>
                <label class="block text-[9px] font-semibold text-[var(--app-muted)] uppercase mb-0.5">Prompt Negativo</label>
                <input *ngIf="!isReadOnly" type="text" [(ngModel)]="frame.negativePrompt" (ngModelChange)="onFieldChange()"
                       placeholder="Elementos a evitar (ej: blur, low quality, artifacts)..." class="cf-input w-full text-xs py-1" />
                <span *ngIf="isReadOnly" class="text-xs text-[var(--app-text)] truncate block">{{ frame.negativePrompt || '—' }}</span>
              </div>

              <div>
                <label class="block text-[9px] font-semibold text-[var(--app-muted)] uppercase mb-0.5">Intención de Estilo</label>
                <input *ngIf="!isReadOnly" type="text" [(ngModel)]="frame.styleIntent" (ngModelChange)="onFieldChange()"
                       placeholder="Ej: Hiperrealista 8k, cinematic color grade..." class="cf-input w-full text-xs py-1" />
                <span *ngIf="isReadOnly" class="text-xs text-[var(--app-text)] truncate block">{{ frame.styleIntent || '—' }}</span>
              </div>
            </div>
          </div>

          <!-- Toggle Advanced Options -->
          <button (click)="showAdvanced = !showAdvanced" class="text-[10px] text-blue-500 hover:underline flex items-center gap-1 pt-1 font-medium">
            <i class="pi" [ngClass]="showAdvanced ? 'pi-chevron-up' : 'pi-chevron-down'"></i>
            <span>{{ showAdvanced ? 'Menos opciones' : 'Prompt negativo y estilo' }}</span>
          </button>

        </div>
      </div>

    </div>
  `
})
export class StoryboardFrameCardComponent {
  @Input({ required: true }) frame!: StoryboardFrameDto;
  @Input() isReadOnly = false;
  @Input() isSelected = false;
  @Input() isFirst = false;
  @Input() isLast = false;
  @Input() hasTimingWarning = false;

  @Output() frameChange = new EventEmitter<StoryboardFrameDto>();
  @Output() moveUp = new EventEmitter<number>();
  @Output() moveDown = new EventEmitter<number>();
  @Output() deleteFrame = new EventEmitter<number>();
  @Output() splitFrame = new EventEmitter<StoryboardFrameDto>();

  showAdvanced = false;

  readonly framingOptions: FramingIntent[] = [
    'ExtremeCloseUp',
    'CloseUp',
    'MediumShot',
    'WideShot',
    'IsometricUi',
    'MotionGraphic'
  ];

  readonly cameraMotionOptions: CameraMotionIntent[] = [
    'Static',
    'SlowZoomIn',
    'PanUp',
    'TrackingShot',
    'DynamicGlitch'
  ];

  readonly transitionOptions: TransitionIntent[] = [
    'Cut',
    'Dissolve',
    'Wipe',
    'ZoomIn',
    'Glitch',
    'PanUp'
  ];

  onFieldChange() {
    this.frameChange.emit(this.frame);
  }
}
