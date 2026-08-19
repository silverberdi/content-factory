import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { StoryboardCritiqueResultDto } from '../../core/api.service';

@Component({
  selector: 'app-storyboard-review-panel',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="fixed inset-y-0 right-0 z-50 w-full max-w-md bg-[var(--app-card-bg)] border-l border-[var(--app-card-border)] shadow-2xl flex flex-col animate-slide-left">
      
      <!-- Drawer Header -->
      <div class="px-5 py-4 border-b border-[var(--app-card-border)] flex items-center justify-between bg-[var(--app-bg)]">
        <div class="flex items-center gap-2">
          <i class="pi pi-sparkles text-purple-500 text-sm"></i>
          <div>
            <h3 class="text-sm font-bold text-[var(--app-text)]">Revisión Asesora IA (Storyboard)</h3>
            <p class="text-[11px] text-[var(--app-muted)]">Crítica consultiva de alineación visual y temporal</p>
          </div>
        </div>
        <button (click)="close.emit()" class="text-[var(--app-muted)] hover:text-[var(--app-text)] p-1 rounded-lg hover:bg-[var(--app-card-border)] transition-colors">
          <i class="pi pi-times text-xs"></i>
        </button>
      </div>

      <!-- Drawer Body -->
      <div class="p-5 space-y-4 overflow-y-auto flex-1 text-xs">
        
        <!-- Score & Overall Status Banner -->
        <div class="p-4 rounded-xl border flex items-center justify-between gap-4"
             [ngClass]="{
               'bg-emerald-500/10 border-emerald-500/30 text-emerald-600 dark:text-emerald-400': critique?.overallStatus === 'Pass',
               'bg-amber-500/10 border-amber-500/30 text-amber-600 dark:text-amber-400': critique?.overallStatus === 'Warning',
               'bg-red-500/10 border-red-500/30 text-red-600 dark:text-red-400': critique?.overallStatus === 'Critical'
             }">
          <div class="space-y-1">
            <div class="text-[10px] font-bold uppercase tracking-wider">Estado General</div>
            <div class="text-sm font-bold flex items-center gap-1.5">
              <i class="pi" [ngClass]="critique?.overallStatus === 'Pass' ? 'pi-check-circle' : 'pi-exclamation-triangle'"></i>
              <span>{{ critique?.overallStatus === 'Pass' ? 'Aprobado por IA' : (critique?.overallStatus === 'Warning' ? 'Observaciones Leves' : 'Crítico') }}</span>
            </div>
          </div>

          <!-- Gauge Score -->
          <div class="text-right">
            <div class="text-2xl font-black font-mono">
              {{ critique?.visualAlignmentScore || 0 }}%
            </div>
            <div class="text-[9px] uppercase font-semibold opacity-80">Alineación Visual</div>
          </div>
        </div>

        <!-- Narrative & Timing Summaries -->
        <div class="space-y-2">
          <div class="p-3 rounded-lg bg-[var(--app-bg)] border border-[var(--app-card-border)] space-y-1">
            <div class="font-bold text-[var(--app-text)] flex items-center gap-1.5 text-xs">
              <i class="pi pi-eye text-blue-500 text-xs"></i>
              <span>Continuidad Narrativa</span>
            </div>
            <p class="text-[11px] text-[var(--app-muted)] leading-relaxed">
              {{ critique?.narrativeContinuityAssessment || 'Sin análisis disponible.' }}
            </p>
          </div>

          <div class="p-3 rounded-lg bg-[var(--app-bg)] border border-[var(--app-card-border)] space-y-1">
            <div class="font-bold text-[var(--app-text)] flex items-center gap-1.5 text-xs">
              <i class="pi pi-clock text-amber-500 text-xs"></i>
              <span>Evaluación de Ritmo y Timing</span>
            </div>
            <p class="text-[11px] text-[var(--app-muted)] leading-relaxed">
              {{ critique?.timingPacingAssessment || 'Sin análisis disponible.' }}
            </p>
          </div>
        </div>

        <!-- Dimensional Review Breakdown -->
        <div class="space-y-2">
          <h4 class="font-bold text-[var(--app-text)] uppercase tracking-wider text-[10px]">
            Dimensiones de Calidad Visual
          </h4>
          <div class="space-y-1.5">
            <div *ngFor="let dim of critique?.dimensions" 
                 class="p-2.5 rounded-lg bg-[var(--app-bg)] border border-[var(--app-card-border)] flex items-start justify-between gap-2">
              <div class="space-y-0.5 flex-1">
                <div class="font-bold text-[var(--app-text)] text-xs">{{ dim.dimension }}</div>
                <div class="text-[10px] text-[var(--app-muted)] leading-normal">{{ dim.notes }}</div>
              </div>
              <span class="px-1.5 py-0.5 rounded text-[9px] font-bold uppercase font-mono shrink-0 border"
                    [ngClass]="{
                      'bg-emerald-500/15 text-emerald-600 border-emerald-500/30': dim.status === 'Pass',
                      'bg-amber-500/15 text-amber-600 border-amber-500/30': dim.status === 'Warning',
                      'bg-red-500/15 text-red-600 border-red-500/30': dim.status === 'Critical'
                    }">
                {{ dim.status }}
              </span>
            </div>
          </div>
        </div>

        <!-- Frame-by-Frame Critiques -->
        <div *ngIf="critique?.frameCritiques && critique!.frameCritiques.length > 0" class="space-y-2">
          <h4 class="font-bold text-[var(--app-text)] uppercase tracking-wider text-[10px]">
            Crítica por Toma Individual
          </h4>
          <div class="space-y-2">
            <div *ngFor="let fc of critique?.frameCritiques" 
                 class="p-3 rounded-lg bg-[var(--app-bg)] border border-[var(--app-card-border)] space-y-1.5">
              <div class="flex items-center justify-between">
                <span class="font-bold text-xs text-[var(--app-text)]">
                  Toma #{{ fc.orderIndex }} (Escena {{ fc.scriptSceneOrderIndex }})
                </span>
                <span class="px-1.5 py-0.5 rounded text-[9px] font-bold uppercase font-mono border"
                      [ngClass]="{
                        'bg-emerald-500/15 text-emerald-600 border-emerald-500/30': fc.status === 'Pass',
                        'bg-amber-500/15 text-amber-600 border-amber-500/30': fc.status === 'Warning',
                        'bg-red-500/15 text-red-600 border-red-500/30': fc.status === 'Critical'
                      }">
                  {{ fc.status }}
                </span>
              </div>

              <div class="text-[11px] text-[var(--app-muted)] leading-relaxed">
                {{ fc.visualNarrativeFidelityNotes }}
              </div>

              <div *ngIf="fc.suggestions && fc.suggestions.length > 0" class="pt-1 space-y-1">
                <div *ngFor="let sug of fc.suggestions" class="text-[10px] text-blue-500 flex items-start gap-1">
                  <i class="pi pi-arrow-right text-[8px] mt-0.5 shrink-0"></i>
                  <span>{{ sug }}</span>
                </div>
              </div>
            </div>
          </div>
        </div>

        <!-- Actionable Recommendations -->
        <div *ngIf="critique?.actionableRecommendations && critique!.actionableRecommendations.length > 0" class="space-y-2">
          <h4 class="font-bold text-[var(--app-text)] uppercase tracking-wider text-[10px]">
            Recomendaciones de Acción
          </h4>
          <div class="p-3 rounded-lg bg-blue-500/10 border border-blue-500/20 space-y-1.5">
            <div *ngFor="let rec of critique?.actionableRecommendations" class="text-[11px] text-blue-600 dark:text-blue-400 flex items-start gap-1.5">
              <i class="pi pi-check text-[9px] mt-1 shrink-0"></i>
              <span>{{ rec }}</span>
            </div>
          </div>
        </div>

      </div>

      <!-- Drawer Footer -->
      <div class="px-5 py-3 border-t border-[var(--app-card-border)] bg-[var(--app-bg)] text-right">
        <button (click)="close.emit()" class="cf-btn-secondary w-full">
          Cerrar Panel
        </button>
      </div>

    </div>
  `
})
export class StoryboardReviewPanelComponent {
  @Input() critique?: StoryboardCritiqueResultDto | null;
  @Output() close = new EventEmitter<void>();
}
