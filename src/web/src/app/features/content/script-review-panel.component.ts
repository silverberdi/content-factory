import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ScriptReviewResultDto } from '../../core/api.service';

@Component({
  selector: 'app-script-review-panel',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="bg-[var(--app-card-bg)] border border-[var(--app-card-border)] rounded-xl p-4 sm:p-5 shadow-xs space-y-4 text-xs animate-fade-in">
      
      <!-- Panel Header -->
      <div class="flex items-center justify-between border-b border-[var(--app-card-border)] pb-3">
        <div class="flex items-center gap-2">
          <div class="w-7 h-7 rounded-lg bg-indigo-500/10 border border-indigo-500/20 flex items-center justify-center text-indigo-500">
            <i class="pi pi-verified text-sm"></i>
          </div>
          <div>
            <h3 class="font-bold text-sm text-[var(--app-text)]">Auditoría Editorial Consultiva (IA)</h3>
            <p class="text-[10px] text-[var(--app-muted)]">Evaluación analítica frente al TruthSource y pacing configurado</p>
          </div>
        </div>

        <div class="flex items-center gap-2">
          <!-- Overall Status Badge -->
          <span class="px-2.5 py-1 rounded-full text-xs font-bold uppercase tracking-wider font-mono border flex items-center gap-1.5"
                [ngClass]="{
                  'bg-emerald-500/15 text-emerald-600 dark:text-emerald-400 border-emerald-500/30': reviewResult.overallStatus === 'Pass',
                  'bg-amber-500/15 text-amber-600 dark:text-amber-400 border-amber-500/30': reviewResult.overallStatus === 'Warning',
                  'bg-red-500/15 text-red-600 dark:text-red-400 border-red-500/30': reviewResult.overallStatus === 'Critical'
                }">
            <i class="pi" [ngClass]="{
              'pi-check-circle': reviewResult.overallStatus === 'Pass',
              'pi-exclamation-triangle': reviewResult.overallStatus === 'Warning',
              'pi-times-circle': reviewResult.overallStatus === 'Critical'
            }"></i>
            <span>{{ reviewResult.overallStatus }}</span>
          </span>

          <button (click)="close.emit()" class="p-1 rounded text-[var(--app-muted)] hover:text-[var(--app-text)] cursor-pointer" title="Cerrar panel">
            <i class="pi pi-times"></i>
          </button>
        </div>
      </div>

      <!-- Advisory Governance Disclaimer Notice -->
      <div class="p-2.5 rounded-lg bg-blue-500/10 border border-blue-500/20 text-[11px] text-blue-700 dark:text-blue-300 flex items-center gap-2">
        <i class="pi pi-info-circle shrink-0"></i>
        <span><strong>Dictamen Consultivo:</strong> Esta auditoría es una herramienta de apoyo analítico. La decisión de aprobación o rechazo final corresponde exclusivamente al criterio del operador editorial humano.</span>
      </div>

      <!-- Top Summary Metrics Grid -->
      <div class="grid grid-cols-1 sm:grid-cols-3 gap-3">
        
        <!-- Factual Alignment -->
        <div class="p-3 rounded-lg bg-[var(--app-bg)] border border-[var(--app-card-border)] space-y-1">
          <div class="flex items-center justify-between text-[10px] text-[var(--app-muted)]">
            <span class="font-bold uppercase tracking-wider">Alineación Factual</span>
            <i class="pi pi-shield text-emerald-500"></i>
          </div>
          <p class="text-base font-extrabold text-emerald-600 dark:text-emerald-400 font-mono">
            {{ (reviewResult.factualAlignmentScore * 100).toFixed(0) }}%
          </p>
          <span class="text-[10px] text-[var(--app-muted)] block">Consistencia con TruthSource</span>
        </div>

        <!-- Retention Analysis -->
        <div class="p-3 rounded-lg bg-[var(--app-bg)] border border-[var(--app-card-border)] space-y-1 sm:col-span-2">
          <div class="flex items-center justify-between text-[10px] text-[var(--app-muted)]">
            <span class="font-bold uppercase tracking-wider">Retención & Gancho (0-3s)</span>
            <i class="pi pi-bolt text-amber-500"></i>
          </div>
          <p class="text-[11px] text-[var(--app-text)] leading-snug">
            {{ reviewResult.retentionAnalysis }}
          </p>
        </div>

      </div>

      <!-- Pacing Assessment -->
      <div class="p-3 rounded-lg bg-[var(--app-bg)] border border-[var(--app-card-border)] space-y-1">
        <div class="flex items-center justify-between text-[10px] text-[var(--app-muted)]">
          <span class="font-bold uppercase tracking-wider">Ritmo y Duración Estimada</span>
          <i class="pi pi-clock text-blue-500"></i>
        </div>
        <p class="text-[11px] text-[var(--app-text)] leading-snug">
          {{ reviewResult.pacingAssessment }}
        </p>
      </div>

      <!-- Dimensions Evaluation Breakdown -->
      <div *ngIf="reviewResult.dimensions?.length" class="space-y-2">
        <span class="text-[10px] font-bold uppercase tracking-wider text-[var(--app-muted)] block">Dimensiones Evaluadas</span>
        <div class="grid grid-cols-1 sm:grid-cols-2 gap-2">
          <div *ngFor="let dim of reviewResult.dimensions" 
               class="p-2.5 rounded-lg border bg-[var(--app-bg)] space-y-1"
               [ngClass]="{
                 'border-emerald-500/30': dim.status === 'Pass',
                 'border-amber-500/30': dim.status === 'Warning',
                 'border-red-500/30': dim.status === 'Critical'
               }">
            <div class="flex items-center justify-between">
              <span class="font-bold text-[11px] text-[var(--app-text)]">{{ dim.dimension }}</span>
              <span class="px-1.5 py-0.2 rounded text-[9px] font-bold font-mono uppercase"
                    [ngClass]="{
                      'bg-emerald-500/15 text-emerald-600': dim.status === 'Pass',
                      'bg-amber-500/15 text-amber-600': dim.status === 'Warning',
                      'bg-red-500/15 text-red-600': dim.status === 'Critical'
                    }">
                {{ dim.status }}
              </span>
            </div>
            <p class="text-[10px] text-[var(--app-muted)] leading-relaxed">{{ dim.notes }}</p>
          </div>
        </div>
      </div>

      <!-- Scene-by-Scene Critiques -->
      <div *ngIf="reviewResult.sceneCritiques?.length" class="space-y-2">
        <span class="text-[10px] font-bold uppercase tracking-wider text-[var(--app-muted)] block">Auditoría por Escena</span>
        <div class="space-y-2">
          <div *ngFor="let sc of reviewResult.sceneCritiques" 
               class="p-2.5 rounded-lg bg-[var(--app-bg)] border border-[var(--app-card-border)] space-y-1.5">
            <div class="flex items-center justify-between">
              <div class="flex items-center gap-1.5">
                <span class="w-5 h-5 rounded-full bg-[var(--app-card-bg)] border border-[var(--app-card-border)] flex items-center justify-center font-mono font-bold text-[10px]">
                  #{{ sc.orderIndex }}
                </span>
                <span class="font-bold text-[11px] text-[var(--app-text)]">{{ sc.sceneType }}</span>
              </div>
              <span class="px-1.5 py-0.2 rounded text-[9px] font-bold font-mono uppercase"
                    [ngClass]="{
                      'bg-emerald-500/15 text-emerald-600': sc.status === 'Pass',
                      'bg-amber-500/15 text-amber-600': sc.status === 'Warning',
                      'bg-red-500/15 text-red-600': sc.status === 'Critical'
                    }">
                {{ sc.status }}
              </span>
            </div>

            <p class="text-[11px] text-[var(--app-text)]">{{ sc.claimFidelityNotes }}</p>

            <div *ngIf="sc.suggestions?.length" class="pl-3 border-l-2 border-amber-500/40 text-[10px] text-[var(--app-muted)] space-y-0.5">
              <div *ngFor="let sug of sc.suggestions" class="italic">💡 {{ sug }}</div>
            </div>
          </div>
        </div>
      </div>

      <!-- Actionable Recommendations -->
      <div *ngIf="reviewResult.actionableRecommendations?.length" class="space-y-1.5">
        <span class="text-[10px] font-bold uppercase tracking-wider text-[var(--app-muted)] block">Recomendaciones Accionables</span>
        <ul class="space-y-1 p-2.5 rounded-lg bg-indigo-500/5 border border-indigo-500/20 text-[11px] text-[var(--app-text)]">
          <li *ngFor="let rec of reviewResult.actionableRecommendations" class="flex items-start gap-1.5">
            <i class="pi pi-check text-indigo-500 mt-0.5 text-[10px]"></i>
            <span>{{ rec }}</span>
          </li>
        </ul>
      </div>

    </div>
  `
})
export class ScriptReviewPanelComponent {
  @Input({ required: true }) reviewResult!: ScriptReviewResultDto;
  @Output() close = new EventEmitter<void>();
}
