import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AssetPlanDto, AssetRequirementDto, AssetType } from '../../core/api.service';

@Component({
  selector: 'app-asset-plan-summary',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="bg-[var(--app-card-bg)] border border-[var(--app-card-border)] rounded-xl p-4 sm:p-5 shadow-xs space-y-4">
      
      <!-- Asset Plan Header -->
      <div class="flex items-center justify-between border-b border-[var(--app-card-border)] pb-3 flex-wrap gap-2">
        <div class="flex items-center gap-2">
          <i class="pi pi-box text-blue-500 text-base"></i>
          <div>
            <h3 class="text-sm font-bold text-[var(--app-text)]">Especificación del Plan de Producción (Asset Plan)</h3>
            <p class="text-[10px] text-[var(--app-muted)]">
              Requerimientos de activos desacoplados de motores de render y neutrales a proveedores
            </p>
          </div>
        </div>

        <div class="flex items-center gap-2">
          <!-- Readiness Status Badge -->
          <span class="px-2.5 py-0.5 rounded text-[10px] font-bold uppercase tracking-wider border font-mono"
                [ngClass]="{
                  'bg-emerald-500/15 text-emerald-600 dark:text-emerald-400 border-emerald-500/30': assetPlan?.status === 'ReadyForGeneration',
                  'bg-blue-500/15 text-blue-600 dark:text-blue-400 border-blue-500/30': assetPlan?.status === 'Planned'
                }">
            <i class="pi mr-1 text-[9px]" [ngClass]="assetPlan?.status === 'ReadyForGeneration' ? 'pi-check-circle' : 'pi-hourglass'"></i>
            {{ assetPlan?.status === 'ReadyForGeneration' ? 'Listo para Producción' : 'En Planificación' }}
          </span>

          <span class="px-2 py-0.5 rounded bg-[var(--app-bg)] border border-[var(--app-card-border)] text-[10px] font-mono text-[var(--app-muted)]">
            {{ assetPlan?.requirements?.length || 0 }} Requerimientos
          </span>
        </div>
      </div>

      <!-- Asset Requirement Summary Metrics Grid -->
      <div class="grid grid-cols-2 sm:grid-cols-4 gap-2.5">
        
        <!-- Visuals Metric -->
        <div class="p-3 rounded-lg bg-[var(--app-bg)] border border-[var(--app-card-border)] space-y-1">
          <div class="flex items-center justify-between text-[var(--app-muted)]">
            <span class="text-[10px] font-bold uppercase tracking-wider">Visuales 9:16</span>
            <i class="pi pi-image text-blue-500 text-xs"></i>
          </div>
          <div class="text-base font-bold font-mono text-[var(--app-text)]">
            {{ visualRequirements.length }}
          </div>
          <div class="text-[9px] text-[var(--app-muted)] truncate">
            Imágenes & clips AI
          </div>
        </div>

        <!-- Voiceover Metric -->
        <div class="p-3 rounded-lg bg-[var(--app-bg)] border border-[var(--app-card-border)] space-y-1">
          <div class="flex items-center justify-between text-[var(--app-muted)]">
            <span class="text-[10px] font-bold uppercase tracking-wider">Locución TTS</span>
            <i class="pi pi-microphone text-purple-500 text-xs"></i>
          </div>
          <div class="text-base font-bold font-mono text-[var(--app-text)]">
            {{ ttsRequirements.length }}
          </div>
          <div class="text-[9px] text-[var(--app-muted)] truncate">
            Guión voz en off
          </div>
        </div>

        <!-- Audio & SFX Metric -->
        <div class="p-3 rounded-lg bg-[var(--app-bg)] border border-[var(--app-card-border)] space-y-1">
          <div class="flex items-center justify-between text-[var(--app-muted)]">
            <span class="text-[10px] font-bold uppercase tracking-wider">Música & SFX</span>
            <i class="pi pi-volume-up text-amber-500 text-xs"></i>
          </div>
          <div class="text-base font-bold font-mono text-[var(--app-text)]">
            {{ audioRequirements.length }}
          </div>
          <div class="text-[9px] text-[var(--app-muted)] truncate">
            Fondo y efectos
          </div>
        </div>

        <!-- Subtitles Metric -->
        <div class="p-3 rounded-lg bg-[var(--app-bg)] border border-[var(--app-card-border)] space-y-1">
          <div class="flex items-center justify-between text-[var(--app-muted)]">
            <span class="text-[10px] font-bold uppercase tracking-wider">Subtítulos</span>
            <i class="pi pi-align-center text-emerald-500 text-xs"></i>
          </div>
          <div class="text-base font-bold font-mono text-[var(--app-text)]">
            {{ subtitleRequirements.length }}
          </div>
          <div class="text-[9px] text-[var(--app-muted)] truncate">
            Captions cinéticos
          </div>
        </div>

      </div>

      <!-- Asset Requirements Filter / Tabs -->
      <div class="flex items-center gap-1.5 border-b border-[var(--app-card-border)] pb-2 overflow-x-auto text-xs">
        <button (click)="activeTab = 'all'" 
                class="px-2.5 py-1 rounded-md text-[11px] font-semibold transition-colors"
                [ngClass]="activeTab === 'all' ? 'bg-blue-500/15 text-blue-600 dark:text-blue-400 font-bold' : 'text-[var(--app-muted)] hover:text-[var(--app-text)]'">
          Todos ({{ assetPlan?.requirements?.length || 0 }})
        </button>
        <button (click)="activeTab = 'visual'" 
                class="px-2.5 py-1 rounded-md text-[11px] font-semibold transition-colors"
                [ngClass]="activeTab === 'visual' ? 'bg-blue-500/15 text-blue-600 dark:text-blue-400 font-bold' : 'text-[var(--app-muted)] hover:text-[var(--app-text)]'">
          Visuales ({{ visualRequirements.length }})
        </button>
        <button (click)="activeTab = 'audio'" 
                class="px-2.5 py-1 rounded-md text-[11px] font-semibold transition-colors"
                [ngClass]="activeTab === 'audio' ? 'bg-blue-500/15 text-blue-600 dark:text-blue-400 font-bold' : 'text-[var(--app-muted)] hover:text-[var(--app-text)]'">
          Audio & Voz ({{ ttsRequirements.length + audioRequirements.length }})
        </button>
        <button (click)="activeTab = 'subtitles'" 
                class="px-2.5 py-1 rounded-md text-[11px] font-semibold transition-colors"
                [ngClass]="activeTab === 'subtitles' ? 'bg-blue-500/15 text-blue-600 dark:text-blue-400 font-bold' : 'text-[var(--app-muted)] hover:text-[var(--app-text)]'">
          Subtítulos ({{ subtitleRequirements.length }})
        </button>
      </div>

      <!-- Requirements Table / Cards List -->
      <div class="space-y-2 max-h-[420px] overflow-y-auto pr-1">
        <div *ngFor="let req of filteredRequirements" 
             class="p-3 rounded-lg bg-[var(--app-bg)] border border-[var(--app-card-border)] hover:border-blue-500/30 transition-all flex flex-col sm:flex-row sm:items-center justify-between gap-3 text-xs">
          
          <div class="space-y-1 flex-1">
            <div class="flex items-center gap-2 flex-wrap">
              <!-- Asset Type Badge -->
              <span class="px-1.5 py-0.5 rounded text-[9px] font-mono font-bold uppercase border"
                    [ngClass]="{
                      'bg-blue-500/15 text-blue-600 dark:text-blue-400 border-blue-500/30': req.assetType === 'AiImage' || req.assetType === 'AiVideo' || req.assetType === 'BRoll',
                      'bg-purple-500/15 text-purple-600 dark:text-purple-400 border-purple-500/30': req.assetType === 'TtsVoiceover',
                      'bg-amber-500/15 text-amber-600 dark:text-amber-400 border-amber-500/30': req.assetType === 'BackgroundMusic' || req.assetType === 'SoundEffect',
                      'bg-emerald-500/15 text-emerald-600 dark:text-emerald-400 border-emerald-500/30': req.assetType === 'SubtitleTrack'
                    }">
                {{ req.assetType }}
              </span>

              <!-- Aspect Ratio -->
              <span *ngIf="req.aspectRatio && req.aspectRatio !== 'N/A'" class="px-1 py-0.5 rounded bg-[var(--app-card-bg)] text-[9px] font-mono text-[var(--app-muted)] border border-[var(--app-card-border)]">
                {{ req.aspectRatio }}
              </span>

              <!-- Frame Association -->
              <span *ngIf="req.frameOrderIndex" class="text-[10px] text-[var(--app-muted)] font-mono">
                Toma #{{ req.frameOrderIndex }}
              </span>

              <!-- Target Duration -->
              <span *ngIf="req.targetDurationSeconds" class="text-[10px] text-[var(--app-muted)] font-mono">
                • ~{{ req.targetDurationSeconds }}s
              </span>
            </div>

            <!-- Description / Visual Prompt / Voice Profile -->
            <p class="text-xs text-[var(--app-text)] font-medium line-clamp-2">
              {{ req.visualPrompt || req.voiceIntent || req.musicMood || req.subtitleProfile }}
            </p>

            <!-- Metadata Pills -->
            <div class="flex items-center gap-2 text-[10px] text-[var(--app-muted)] flex-wrap pt-0.5">
              <span *ngIf="req.styleIntent" class="truncate max-w-[200px]">
                <i class="pi pi-palette mr-0.5 text-[9px]"></i> {{ req.styleIntent }}
              </span>
              <span *ngIf="req.motionIntent" class="truncate max-w-[200px]">
                <i class="pi pi-arrows-alt mr-0.5 text-[9px]"></i> {{ req.motionIntent }}
              </span>
              <span *ngIf="req.voiceIntent && req.assetType !== 'TtsVoiceover'" class="truncate max-w-[200px]">
                <i class="pi pi-microphone mr-0.5 text-[9px]"></i> {{ req.voiceIntent }}
              </span>
            </div>
          </div>

          <!-- Status Indicator Pill -->
          <div class="flex sm:flex-col items-end justify-between sm:justify-center gap-1 shrink-0">
            <span class="text-[9px] font-mono text-emerald-500 flex items-center gap-1 font-semibold">
              <i class="pi pi-check text-[8px]"></i> Especificado
            </span>
          </div>

        </div>

        <div *ngIf="filteredRequirements.length === 0" class="p-6 text-center text-xs text-[var(--app-muted)]">
          No hay requerimientos en esta categoría.
        </div>
      </div>

    </div>
  `
})
export class AssetPlanSummaryComponent {
  @Input() assetPlan?: AssetPlanDto | null;
  @Input() isReadOnly = false;

  activeTab: 'all' | 'visual' | 'audio' | 'subtitles' = 'all';

  get requirements(): AssetRequirementDto[] {
    return this.assetPlan?.requirements || [];
  }

  get visualRequirements(): AssetRequirementDto[] {
    return this.requirements.filter(r => r.assetType === 'AiImage' || r.assetType === 'AiVideo' || r.assetType === 'BRoll' || r.assetType === 'GraphicOverlay');
  }

  get ttsRequirements(): AssetRequirementDto[] {
    return this.requirements.filter(r => r.assetType === 'TtsVoiceover');
  }

  get audioRequirements(): AssetRequirementDto[] {
    return this.requirements.filter(r => r.assetType === 'BackgroundMusic' || r.assetType === 'SoundEffect');
  }

  get subtitleRequirements(): AssetRequirementDto[] {
    return this.requirements.filter(r => r.assetType === 'SubtitleTrack');
  }

  get filteredRequirements(): AssetRequirementDto[] {
    switch (this.activeTab) {
      case 'visual':
        return this.visualRequirements;
      case 'audio':
        return [...this.ttsRequirements, ...this.audioRequirements];
      case 'subtitles':
        return this.subtitleRequirements;
      default:
        return this.requirements;
    }
  }
}
