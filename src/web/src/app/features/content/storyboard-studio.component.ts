import { Component, OnInit, inject, signal, computed, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterModule } from '@angular/router';
import {
  ApiService,
  ContentItemDetailDto,
  PlanStoryboardOptions,
  ProductionEligibilityDto,
  SaveStoryboardFrameRequest,
  ScriptDto,
  StoryboardCritiqueResultDto,
  StoryboardDto,
  StoryboardFrameDto,
  StoryboardVersionDto,
  UpdateStoryboardRequest
} from '../../core/api.service';
import { StoryboardFrameCardComponent } from './storyboard-frame-card.component';
import { AssetPlanSummaryComponent } from './asset-plan-summary.component';
import { GenerateStoryboardModalComponent } from './generate-storyboard-modal.component';
import { StoryboardReviewPanelComponent } from './storyboard-review-panel.component';
import { StoryboardVersionHistoryDrawerComponent } from './storyboard-version-history-drawer.component';
import { RejectStoryboardModalComponent } from './reject-storyboard-modal.component';
import { PageHeaderComponent } from '../../shared/layout/page-header.component';

@Component({
  selector: 'app-storyboard-studio',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    RouterModule,
    StoryboardFrameCardComponent,
    AssetPlanSummaryComponent,
    GenerateStoryboardModalComponent,
    StoryboardReviewPanelComponent,
    StoryboardVersionHistoryDrawerComponent,
    RejectStoryboardModalComponent,
    PageHeaderComponent
  ],
  host: { class: 'block w-full' },
  template: `
    <!-- Loading State -->
    <div *ngIf="isLoading()" class="p-12 text-center text-xs text-[var(--app-muted)] space-y-2">
      <i class="pi pi-spin pi-spinner text-2xl text-blue-500 block mx-auto"></i>
      <span class="font-medium text-sm text-[var(--app-text)]">Cargando Storyboard & Production Studio...</span>
    </div>

    <!-- Error State -->
    <div *ngIf="errorMessage() && !isLoading()" class="p-6 rounded-xl bg-red-500/10 border border-red-500/30 text-center space-y-3 max-w-lg mx-auto my-8 text-xs">
      <i class="pi pi-exclamation-triangle text-2xl text-red-500 block"></i>
      <p class="font-bold text-sm text-[var(--app-text)]">{{ errorMessage() }}</p>
      <div class="flex items-center justify-center gap-2 pt-2">
        <button (click)="retryLoad()" class="cf-btn-primary">
          <i class="pi pi-refresh mr-1 text-[10px]"></i> Reintentar
        </button>
        <a [routerLink]="['/content/items', contentItemId()]" class="cf-btn-secondary">
          Volver a Detalle
        </a>
      </div>
    </div>

    <!-- Main Studio Layout -->
    <div *ngIf="!isLoading() && contentItem()" class="cf-page-container space-y-4 text-xs pb-20">
      
      <!-- Canonical Operational Header -->
      <app-page-header 
        title="Storyboard Studio"
        [subtitle]="storyboard()?.title || ('Storyboard • ' + contentItem()?.title)"
        [backLink]="['/content/items', contentItemId()]"
        backLabel="Detalle de Pieza">
        
        <div meta class="flex items-center gap-2 flex-wrap text-xs">
          <span class="px-2 py-0.5 rounded bg-blue-500/15 text-blue-600 dark:text-blue-400 border border-blue-500/30 text-[10px] font-bold">
            {{ contentItem()?.channelName || 'Canal' }}
          </span>

          <!-- Storyboard Status Badge -->
          <span *ngIf="storyboard()" class="px-2 py-0.5 rounded text-[10px] font-bold uppercase tracking-wider font-mono border"
                [ngClass]="{
                  'bg-emerald-500/15 text-emerald-600 dark:text-emerald-400 border-emerald-500/30': storyboard()?.status === 'Approved',
                  'bg-amber-500/15 text-amber-600 dark:text-amber-400 border-amber-500/30': storyboard()?.status === 'UnderReview',
                  'bg-blue-500/15 text-blue-600 dark:text-blue-400 border-blue-500/30': storyboard()?.status === 'Draft',
                  'bg-red-500/15 text-red-600 dark:text-red-400 border-red-500/30': storyboard()?.status === 'Rejected'
                }">
            {{ storyboard()?.status }}
          </span>

          <!-- Version Tag -->
          <span *ngIf="storyboard()" class="px-2 py-0.5 rounded bg-purple-500/15 text-purple-600 dark:text-purple-400 border border-purple-500/30 text-[10px] font-mono font-bold">
            v{{ storyboard()?.version }}
          </span>

          <!-- Stale Invalidation Pill -->
          <span *ngIf="storyboard()?.isStale" class="px-2 py-0.5 rounded bg-rose-500/15 text-rose-600 dark:text-rose-400 border border-rose-500/30 text-[10px] font-bold flex items-center gap-1 animate-pulse">
            <i class="pi pi-exclamation-triangle text-[9px]"></i>
            <span>Lineage Desactualizado</span>
          </span>

          <!-- Production Eligibility Pill -->
          <span *ngIf="eligibility()" class="px-2 py-0.5 rounded text-[10px] font-mono font-semibold border"
                [ngClass]="eligibility()?.isEligible ? 'bg-emerald-500/15 text-emerald-600 border-emerald-500/30' : 'bg-slate-500/10 text-slate-500 border-slate-500/20'">
            <i class="pi mr-0.5 text-[9px]" [ngClass]="eligibility()?.isEligible ? 'pi-check-circle' : 'pi-lock'"></i>
            {{ eligibility()?.isEligible ? 'Listo para Producción' : 'Bloqueado para Producción' }}
          </span>
        </div>

        <div actions class="flex items-center gap-2 flex-wrap">
          <!-- AI Actions -->
          <button *ngIf="canEdit()" (click)="isAiModalOpen.set(true)" class="cf-btn-primary">
            <i class="pi pi-sparkles text-xs"></i>
            <span>Planificar con IA</span>
          </button>

          <button (click)="runAiReview()" [disabled]="isReviewing() || !storyboard()" class="cf-btn-secondary">
            <i class="pi" [ngClass]="isReviewing() ? 'pi-spin pi-spinner' : 'pi-eye'"></i>
            <span>Revisión IA</span>
          </button>

          <!-- Script Reference Drawer Toggle -->
          <button (click)="showScriptDrawer.set(!showScriptDrawer())" class="cf-btn-secondary">
            <i class="pi pi-file text-xs"></i>
            <span>Guión Aprobado</span>
          </button>

          <!-- Version History -->
          <button (click)="openVersionHistory()" class="cf-btn-secondary">
            <i class="pi pi-history text-xs"></i>
            <span>Historial</span>
          </button>
        </div>

      </app-page-header>

      <!-- Stale Lineage Invalidation Alert Banner -->
      <div *ngIf="storyboard()?.isStale" class="p-4 rounded-xl bg-amber-500/15 border border-amber-500/40 flex flex-col sm:flex-row sm:items-center justify-between gap-3 text-xs animate-pulse">
        <div class="flex items-start gap-2.5">
          <i class="pi pi-exclamation-triangle text-amber-500 text-lg shrink-0 mt-0.5"></i>
          <div>
            <div class="font-bold text-[var(--app-text)] text-sm">Lineage Desactualizado (Stale Storyboard)</div>
            <p class="text-[11px] text-[var(--app-muted)]">
              {{ storyboard()?.staleReason || 'El guión o truth source upstream fue modificado y aprobado posteriormente.' }}
            </p>
          </div>
        </div>

        <button (click)="reconcileStoryboard()" [disabled]="isReconciling()" class="cf-btn-primary shrink-0">
          <i class="pi" [ngClass]="isReconciling() ? 'pi-spin pi-spinner' : 'pi-sync'"></i>
          <span>Reconciliar Storyboard Sucesor</span>
        </button>
      </div>

      <!-- Rejection Banner if Rejected -->
      <div *ngIf="storyboard()?.status === 'Rejected'" class="p-4 rounded-xl bg-red-500/10 border border-red-500/30 text-xs space-y-1">
        <div class="flex items-center gap-2 text-red-600 dark:text-red-400 font-bold">
          <i class="pi pi-times-circle"></i>
          <span>Storyboard Rechazado en Revisión Editorial</span>
        </div>
        <p class="text-[11px] text-[var(--app-text)]">
          <strong>Motivo:</strong> {{ storyboard()?.rejectionReason }}
        </p>
        <div class="text-[10px] text-[var(--app-muted)]">
          Rechazado por {{ storyboard()?.rejectedByEmail }} el {{ storyboard()?.rejectedAtUtc | date:'yyyy-MM-dd HH:mm' }}
        </div>
      </div>

      <!-- Timing Coherence & Durations Bar -->
      <div class="bg-[var(--app-card-bg)] border border-[var(--app-card-border)] rounded-xl p-4 shadow-xs space-y-3">
        <div class="flex items-center justify-between flex-wrap gap-2 text-xs">
          <div class="flex items-center gap-3">
            <div>
              <span class="text-[10px] uppercase font-bold text-[var(--app-muted)] block">Duración Estimada Tomas</span>
              <span class="text-base font-bold font-mono text-[var(--app-text)]">
                {{ totalEstimatedDuration() }}s
              </span>
            </div>
            <div class="text-[var(--app-muted)]">•</div>
            <div>
              <span class="text-[10px] uppercase font-bold text-[var(--app-muted)] block">Objetivo Guión</span>
              <span class="text-base font-bold font-mono text-[var(--app-text)]">
                {{ approvedScript()?.targetDurationSeconds || storyboard()?.targetDurationSeconds || 45 }}s
              </span>
            </div>
            <div class="text-[var(--app-muted)]">•</div>
            <div>
              <span class="text-[10px] uppercase font-bold text-[var(--app-muted)] block">Tomas Totales</span>
              <span class="text-base font-bold font-mono text-blue-500">
                {{ frames().length }}
              </span>
            </div>
          </div>

          <!-- Timing Delta Badge -->
          <div class="flex items-center gap-1.5">
            <span class="px-2 py-0.5 rounded text-[10px] font-mono font-bold border"
                  [ngClass]="timingDeltaClass()">
              <i class="pi mr-0.5 text-[9px]" [ngClass]="timingDeltaIcon()"></i>
              Delta: {{ timingDelta() > 0 ? '+' : '' }}{{ timingDelta() }}s
            </span>
          </div>
        </div>

        <!-- Progress Timing Bar -->
        <div class="w-full bg-[var(--app-bg)] h-2 rounded-full overflow-hidden border border-[var(--app-card-border)] relative">
          <div class="h-full bg-blue-500 transition-all duration-300 rounded-full"
               [style.width.%]="durationProgressPercent()"></div>
        </div>

        <!-- Timing Warning Alert if > 5s mismatch -->
        <div *ngIf="hasMaterialTimingMismatch()" class="p-2.5 rounded-lg bg-amber-500/10 border border-amber-500/30 text-amber-600 dark:text-amber-400 text-[11px] flex items-center gap-2">
          <i class="pi pi-exclamation-triangle text-xs shrink-0"></i>
          <span>
            Aviso de Ritmo: La duración de las tomas ({{ totalEstimatedDuration() }}s) difiere del guión ({{ approvedScript()?.targetDurationSeconds || 45 }}s) en más de 5 segundos.
          </span>
        </div>
      </div>

      <!-- Studio Navigation Views (Tomas / Asset Plan / Elegibilidad) -->
      <div class="flex items-center gap-2 border-b border-[var(--app-card-border)] pb-2 text-xs">
        <button (click)="activeStudioView.set('frames')"
                class="px-3 py-1.5 rounded-lg text-xs font-semibold transition-all flex items-center gap-1.5"
                [ngClass]="activeStudioView() === 'frames' ? 'bg-blue-500 text-white shadow-xs font-bold' : 'text-[var(--app-muted)] hover:text-[var(--app-text)] hover:bg-[var(--app-surface-hover)]'">
          <i class="pi pi-images text-xs"></i>
          <span>Tomas Visuales ({{ frames().length }})</span>
        </button>

        <button (click)="activeStudioView.set('assets')"
                class="px-3 py-1.5 rounded-lg text-xs font-semibold transition-all flex items-center gap-1.5"
                [ngClass]="activeStudioView() === 'assets' ? 'bg-blue-500 text-white shadow-xs font-bold' : 'text-[var(--app-muted)] hover:text-[var(--app-text)] hover:bg-[var(--app-surface-hover)]'">
          <i class="pi pi-box text-xs"></i>
          <span>Plan de Activos ({{ storyboard()?.assetPlan?.requirements?.length || 0 }})</span>
        </button>

        <button (click)="activeStudioView.set('eligibility')"
                class="px-3 py-1.5 rounded-lg text-xs font-semibold transition-all flex items-center gap-1.5"
                [ngClass]="activeStudioView() === 'eligibility' ? 'bg-blue-500 text-white shadow-xs font-bold' : 'text-[var(--app-muted)] hover:text-[var(--app-text)] hover:bg-[var(--app-surface-hover)]'">
          <i class="pi pi-shield text-xs"></i>
          <span>Gate de Producción</span>
        </button>
      </div>

      <!-- VIEW 1: Visual Frames Grid / List -->
      <div *ngIf="activeStudioView() === 'frames'" class="space-y-4">
        
        <!-- Empty State -->
        <div *ngIf="frames().length === 0" class="p-12 text-center text-xs text-[var(--app-muted)] space-y-3 bg-[var(--app-card-bg)] border border-[var(--app-card-border)] rounded-xl">
          <i class="pi pi-video text-3xl text-blue-500/50 block mx-auto"></i>
          <p class="font-bold text-sm text-[var(--app-text)]">No hay tomas visuales creadas para este guión.</p>
          <p class="text-[11px]">Genera una propuesta automática con IA o añade tomas manualmente.</p>
          <div class="flex items-center justify-center gap-2 pt-2">
            <button (click)="isAiModalOpen.set(true)" class="cf-btn-primary">
              <i class="pi pi-sparkles mr-1"></i> Planificar con IA
            </button>
            <button (click)="addNewFrame()" class="cf-btn-secondary">
              <i class="pi pi-plus mr-1"></i> Agregar Toma Manual
            </button>
          </div>
        </div>

        <!-- Frames List -->
        <div *ngIf="frames().length > 0" class="space-y-3">
          <app-storyboard-frame-card
            *ngFor="let f of frames(); let i = index"
            [frame]="f"
            [isReadOnly]="!canEdit()"
            [isFirst]="i === 0"
            [isLast]="i === frames().length - 1"
            (frameChange)="onFrameChange()"
            (moveUp)="moveFrameUp($event)"
            (moveDown)="moveFrameDown($event)"
            (deleteFrame)="deleteFrame($event)"
            (splitFrame)="splitFrame($event)">
          </app-storyboard-frame-card>

          <!-- Add Frame Bottom Button -->
          <div *ngIf="canEdit()" class="pt-2">
            <button (click)="addNewFrame()" class="w-full py-3 rounded-xl border border-dashed border-[var(--app-card-border)] hover:border-blue-500/50 hover:bg-blue-500/5 text-blue-500 transition-all font-semibold flex items-center justify-center gap-2">
              <i class="pi pi-plus"></i>
              <span>Agregar Nueva Toma Visual (9:16)</span>
            </button>
          </div>
        </div>

      </div>

      <!-- VIEW 2: Asset Plan Summary -->
      <div *ngIf="activeStudioView() === 'assets'">
        <app-asset-plan-summary
          [assetPlan]="storyboard()?.assetPlan"
          [isReadOnly]="!canEdit()">
        </app-asset-plan-summary>
      </div>

      <!-- VIEW 3: Downstream Production Eligibility Gate -->
      <div *ngIf="activeStudioView() === 'eligibility'" class="space-y-4">
        <div class="bg-[var(--app-card-bg)] border border-[var(--app-card-border)] rounded-xl p-5 shadow-xs space-y-4">
          
          <div class="flex items-center justify-between border-b border-[var(--app-card-border)] pb-3">
            <div class="flex items-center gap-2">
              <i class="pi pi-shield text-blue-500 text-lg"></i>
              <div>
                <h3 class="text-sm font-bold text-[var(--app-text)]">Condiciones de Gating de Producción</h3>
                <p class="text-[11px] text-[var(--app-muted)]">Precondiciones requeridas para habilitar pipelines de generación de medios</p>
              </div>
            </div>

            <span class="px-2.5 py-1 rounded-full text-xs font-bold uppercase font-mono border"
                  [ngClass]="eligibility()?.isEligible ? 'bg-emerald-500/15 text-emerald-600 border-emerald-500/30' : 'bg-red-500/15 text-red-600 border-red-500/30'">
              {{ eligibility()?.isEligible ? 'Elegible para Producción' : 'No Elegible' }}
            </span>
          </div>

          <!-- Eligibility Checks Grid -->
          <div class="grid grid-cols-1 sm:grid-cols-2 gap-3 text-xs">
            
            <div class="p-3 rounded-lg bg-[var(--app-bg)] border border-[var(--app-card-border)] flex items-center justify-between">
              <span>Storyboard Actual Existe</span>
              <i class="pi" [ngClass]="eligibility()?.currentStoryboardExists ? 'pi-check-circle text-emerald-500' : 'pi-times-circle text-red-500'"></i>
            </div>

            <div class="p-3 rounded-lg bg-[var(--app-bg)] border border-[var(--app-card-border)] flex items-center justify-between">
              <span>Storyboard Aprobado (Status: Approved)</span>
              <i class="pi" [ngClass]="eligibility()?.isApproved ? 'pi-check-circle text-emerald-500' : 'pi-times-circle text-red-500'"></i>
            </div>

            <div class="p-3 rounded-lg bg-[var(--app-bg)] border border-[var(--app-card-border)] flex items-center justify-between">
              <span>Lineage Vigente (IsStale == false)</span>
              <i class="pi" [ngClass]="eligibility()?.isNotStale ? 'pi-check-circle text-emerald-500' : 'pi-times-circle text-red-500'"></i>
            </div>

            <div class="p-3 rounded-lg bg-[var(--app-bg)] border border-[var(--app-card-border)] flex items-center justify-between">
              <span>Asset Plan Completo (ReadyForGeneration)</span>
              <i class="pi" [ngClass]="eligibility()?.isAssetPlanComplete ? 'pi-check-circle text-emerald-500' : 'pi-times-circle text-red-500'"></i>
            </div>

          </div>

          <!-- Blockers if any -->
          <div *ngIf="eligibility()?.blockerReasons && eligibility()!.blockerReasons.length > 0" class="p-3.5 rounded-lg bg-red-500/10 border border-red-500/20 space-y-1 text-red-600 dark:text-red-400 text-xs">
            <strong class="block">Motivos de Bloqueo:</strong>
            <ul class="list-disc pl-5 space-y-0.5 text-[11px]">
              <li *ngFor="let b of eligibility()!.blockerReasons">{{ b }}</li>
            </ul>
          </div>

        </div>
      </div>

      <!-- Sticky Editorial Action Bar -->
      <div class="fixed bottom-0 left-0 right-0 z-40 bg-[var(--app-card-bg)]/95 backdrop-blur-md border-t border-[var(--app-card-border)] px-4 py-3 shadow-lg">
        <div class="max-w-[1600px] mx-auto flex items-center justify-between gap-3 flex-wrap text-xs">
          
          <!-- Left: Unsaved Changes Indicator -->
          <div class="flex items-center gap-2">
            <span *ngIf="hasUnsavedChanges()" class="text-amber-500 font-semibold flex items-center gap-1">
              <i class="pi pi-circle-fill text-[8px] animate-ping"></i>
              <span>Cambios sin guardar en borrador</span>
            </span>
            <span *ngIf="!hasUnsavedChanges() && storyboard()" class="text-[var(--app-muted)] text-[11px]">
              Última modificación: v{{ storyboard()?.version }} • {{ storyboard()?.updatedAtUtc | date:'HH:mm:ss' }}
            </span>
          </div>

          <!-- Right: Actions based on Status -->
          <div class="flex items-center gap-2 flex-wrap">
            
            <!-- Save Draft (Draft / Rejected) -->
            <button *ngIf="canEdit()" (click)="saveDraft()" [disabled]="isSaving()" class="cf-btn-secondary">
              <i class="pi" [ngClass]="isSaving() ? 'pi-spin pi-spinner' : 'pi-save'"></i>
              <span>{{ isSaving() ? 'Guardando...' : 'Guardar Borrador' }}</span>
            </button>

            <!-- Submit for Review (Draft / Rejected) -->
            <button *ngIf="storyboard()?.status === 'Draft' || storyboard()?.status === 'Rejected'"
                    (click)="submitForReview()" [disabled]="isSubmitting() || frames().length === 0" class="cf-btn-primary">
              <i class="pi" [ngClass]="isSubmitting() ? 'pi-spin pi-spinner' : 'pi-send'"></i>
              <span>Enviar a Revisión Editorial</span>
            </button>

            <!-- Single Editorial Gate Actions (UnderReview) -->
            <ng-container *ngIf="storyboard()?.status === 'UnderReview'">
              <button (click)="isRejectModalOpen.set(true)" [disabled]="isRejecting()" class="cf-btn-danger">
                <i class="pi pi-times"></i>
                <span>Rechazar</span>
              </button>

              <button (click)="approveStoryboard()" [disabled]="isApproving()" class="cf-btn-success">
                <i class="pi" [ngClass]="isApproving() ? 'pi-spin pi-spinner' : 'pi-check'"></i>
                <span>Aprobar Storyboard y Plan</span>
              </button>
            </ng-container>

            <!-- Reopen (Approved / Rejected) -->
            <button *ngIf="storyboard()?.status === 'Approved' || storyboard()?.status === 'Rejected'" (click)="reopenStoryboard()" [disabled]="isReopening()" class="cf-btn-secondary">
              <i class="pi" [ngClass]="isReopening() ? 'pi-spin pi-spinner' : 'pi-lock-open'"></i>
              <span>Reabrir a Borrador</span>
            </button>

          </div>
        </div>
      </div>

      <!-- Slide-over Script Reference Drawer -->
      <div *ngIf="showScriptDrawer()" class="fixed inset-y-0 right-0 z-50 w-full max-w-md bg-[var(--app-card-bg)] border-l border-[var(--app-card-border)] shadow-2xl flex flex-col animate-slide-left">
        <div class="px-5 py-4 border-b border-[var(--app-card-border)] flex items-center justify-between bg-[var(--app-bg)]">
          <div class="flex items-center gap-2">
            <i class="pi pi-file text-blue-500 text-sm"></i>
            <div>
              <h3 class="text-sm font-bold text-[var(--app-text)]">Guión Aprobado</h3>
              <p class="text-[11px] text-[var(--app-muted)]">{{ approvedScript()?.title }} (v{{ approvedScript()?.version }})</p>
            </div>
          </div>
          <button (click)="showScriptDrawer.set(false)" class="text-[var(--app-muted)] hover:text-[var(--app-text)] p-1 rounded-lg">
            <i class="pi pi-times text-xs"></i>
          </button>
        </div>

        <div class="p-5 space-y-3 overflow-y-auto flex-1 text-xs">
          <div *ngFor="let sc of approvedScript()?.scenes" class="p-3 rounded-lg bg-[var(--app-bg)] border border-[var(--app-card-border)] space-y-1">
            <div class="flex items-center justify-between font-bold text-[var(--app-text)]">
              <span>Escena {{ sc.orderIndex }}: {{ sc.sceneType }}</span>
              <span class="font-mono text-[10px] text-blue-500">~{{ sc.estimatedDurationSeconds }}s</span>
            </div>
            <p class="text-[11px] text-[var(--app-muted)] leading-relaxed italic">
              "{{ sc.narrationText }}"
            </p>
            <div class="text-[10px] text-[var(--app-text)] font-medium pt-1">
              <strong>Visual Prompt Original:</strong> {{ sc.visualPrompt }}
            </div>
          </div>
        </div>
      </div>

      <!-- AI Plan Modal -->
      <app-generate-storyboard-modal
        *ngIf="isAiModalOpen()"
        [initialDurationSeconds]="approvedScript()?.targetDurationSeconds || 45"
        [isLoading]="isGeneratingAi()"
        (generate)="onGenerateAi($event)"
        (cancel)="isAiModalOpen.set(false)">
      </app-generate-storyboard-modal>

      <!-- AI Review Drawer -->
      <app-storyboard-review-panel
        *ngIf="showReviewPanel()"
        [critique]="critiqueResult()"
        (close)="showReviewPanel.set(false)">
      </app-storyboard-review-panel>

      <!-- Version History Drawer -->
      <app-storyboard-version-history-drawer
        *ngIf="showVersionDrawer()"
        [versions]="versions()"
        [isLoading]="isLoadingVersions()"
        (close)="showVersionDrawer.set(false)">
      </app-storyboard-version-history-drawer>

      <!-- Reject Modal -->
      <app-reject-storyboard-modal
        *ngIf="isRejectModalOpen()"
        [isLoading]="isRejecting()"
        (reject)="onConfirmReject($event)"
        (cancel)="isRejectModalOpen.set(false)">
      </app-reject-storyboard-modal>

    </div>
  `
})
export class StoryboardStudioComponent implements OnInit {
  private readonly api = inject(ApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly cdr = inject(ChangeDetectorRef);

  contentItemId = signal<string>('');
  contentItem = signal<ContentItemDetailDto | null>(null);
  storyboard = signal<StoryboardDto | null>(null);
  approvedScript = signal<ScriptDto | null>(null);
  eligibility = signal<ProductionEligibilityDto | null>(null);
  critiqueResult = signal<StoryboardCritiqueResultDto | null>(null);
  versions = signal<StoryboardVersionDto[]>([]);

  isLoading = signal<boolean>(true);
  errorMessage = signal<string | null>(null);
  isSaving = signal<boolean>(false);
  isSubmitting = signal<boolean>(false);
  isApproving = signal<boolean>(false);
  isRejecting = signal<boolean>(false);
  isReopening = signal<boolean>(false);
  isReconciling = signal<boolean>(false);
  isGeneratingAi = signal<boolean>(false);
  isReviewing = signal<boolean>(false);
  isLoadingVersions = signal<boolean>(false);
  hasUnsavedChanges = signal<boolean>(false);

  activeStudioView = signal<'frames' | 'assets' | 'eligibility'>('frames');
  isAiModalOpen = signal<boolean>(false);
  showReviewPanel = signal<boolean>(false);
  showVersionDrawer = signal<boolean>(false);
  showScriptDrawer = signal<boolean>(false);
  isRejectModalOpen = signal<boolean>(false);

  frames = signal<StoryboardFrameDto[]>([]);

  totalEstimatedDuration = computed(() => {
    const list = this.frames();
    const sum = list.reduce((acc, f) => acc + (f.estimatedDurationSeconds || 0), 0);
    return Math.round(sum * 10) / 10;
  });

  timingDelta = computed(() => {
    const target = this.approvedScript()?.targetDurationSeconds || this.storyboard()?.targetDurationSeconds || 45;
    return Math.round((this.totalEstimatedDuration() - target) * 10) / 10;
  });

  hasMaterialTimingMismatch = computed(() => Math.abs(this.timingDelta()) > 5);

  durationProgressPercent = computed(() => {
    const target = this.approvedScript()?.targetDurationSeconds || this.storyboard()?.targetDurationSeconds || 45;
    if (!target || target <= 0) return 100;
    return Math.min(100, Math.round((this.totalEstimatedDuration() / target) * 100));
  });

  ngOnInit() {
    this.route.paramMap.subscribe(params => {
      const id = params.get('id');
      if (id) {
        this.contentItemId.set(id);
        this.loadData();
      }
    });
  }

  loadData() {
    this.isLoading.set(true);
    this.errorMessage.set(null);
    const id = this.contentItemId();

    this.api.getContentItemDetail(id).subscribe({
      next: (item: ContentItemDetailDto) => {
        this.contentItem.set(item);
        
        // Load approved script
        this.api.getScript(id).subscribe({
          next: (script) => this.approvedScript.set(script),
          error: () => {}
        });

        // Load storyboard
        this.api.getStoryboard(id).subscribe({
          next: (sb) => {
            this.storyboard.set(sb);
            this.frames.set(sb.frames ? JSON.parse(JSON.stringify(sb.frames)) : []);
            this.hasUnsavedChanges.set(false);
            this.isLoading.set(false);
            this.loadEligibility();
            this.cdr.markForCheck();
          },
          error: (err) => {
            if (err.status === 404) {
              // Storyboard doesn't exist yet, we can create one
              this.storyboard.set(null);
              this.frames.set([]);
              this.isLoading.set(false);
              this.cdr.markForCheck();
            } else {
              this.errorMessage.set('Error al cargar storyboard: ' + (err.error?.message || err.message));
              this.isLoading.set(false);
              this.cdr.markForCheck();
            }
          }
        });
      },
      error: (err) => {
        this.errorMessage.set('Error al cargar detalle de contenido: ' + (err.error?.message || err.message));
        this.isLoading.set(false);
        this.cdr.markForCheck();
      }
    });
  }

  retryLoad() {
    this.loadData();
  }

  loadEligibility() {
    this.api.getProductionEligibility(this.contentItemId()).subscribe({
      next: (el) => this.eligibility.set(el),
      error: () => {}
    });
  }

  canEdit(): boolean {
    const sb = this.storyboard();
    if (!sb) return true;
    return sb.status === 'Draft' || sb.status === 'Rejected';
  }

  onFrameChange() {
    this.hasUnsavedChanges.set(true);
  }

  timingDeltaClass(): string {
    const delta = Math.abs(this.timingDelta());
    if (delta <= 2) return 'bg-emerald-500/15 text-emerald-600 border-emerald-500/30';
    if (delta <= 5) return 'bg-blue-500/15 text-blue-600 border-blue-500/30';
    return 'bg-amber-500/15 text-amber-600 border-amber-500/30';
  }

  timingDeltaIcon(): string {
    const delta = Math.abs(this.timingDelta());
    if (delta <= 2) return 'pi-check';
    if (delta <= 5) return 'pi-info-circle';
    return 'pi-exclamation-triangle';
  }

  addNewFrame() {
    const current = this.frames();
    const newOrder = current.length + 1;
    const newFrame: StoryboardFrameDto = {
      id: '',
      storyboardId: this.storyboard()?.id || '',
      orderIndex: newOrder,
      scriptSceneId: this.approvedScript()?.scenes?.[0]?.id || '',
      scriptSceneOrderIndex: 1,
      framingIntent: 'MediumShot',
      compositionIntent: 'Encuadre limpio vertical 9:16',
      cameraMotionIntent: 'Static',
      subject: 'Sujeto visual',
      environment: 'Estudio / Ambiente neutro',
      styleIntent: 'Cinematográfico',
      visualPrompt: 'Plano vertical 9:16 de alta definición...',
      negativePrompt: 'blur, noise, distorted',
      audioCue: 'Voz en off',
      estimatedDurationSeconds: 4.0,
      onScreenText: '',
      transitionIntent: 'Cut',
      createdAtUtc: new Date().toISOString(),
      updatedAtUtc: new Date().toISOString()
    };
    this.frames.set([...current, newFrame]);
    this.hasUnsavedChanges.set(true);
  }

  moveFrameUp(orderIndex: number) {
    const list = [...this.frames()];
    const idx = list.findIndex(f => f.orderIndex === orderIndex);
    if (idx > 0) {
      const temp = list[idx - 1];
      list[idx - 1] = list[idx];
      list[idx] = temp;
      list.forEach((f, i) => f.orderIndex = i + 1);
      this.frames.set(list);
      this.hasUnsavedChanges.set(true);
    }
  }

  moveFrameDown(orderIndex: number) {
    const list = [...this.frames()];
    const idx = list.findIndex(f => f.orderIndex === orderIndex);
    if (idx >= 0 && idx < list.length - 1) {
      const temp = list[idx + 1];
      list[idx + 1] = list[idx];
      list[idx] = temp;
      list.forEach((f, i) => f.orderIndex = i + 1);
      this.frames.set(list);
      this.hasUnsavedChanges.set(true);
    }
  }

  deleteFrame(orderIndex: number) {
    const list = this.frames().filter(f => f.orderIndex !== orderIndex);
    list.forEach((f, i) => f.orderIndex = i + 1);
    this.frames.set(list);
    this.hasUnsavedChanges.set(true);
  }

  splitFrame(frame: StoryboardFrameDto) {
    const list = [...this.frames()];
    const idx = list.findIndex(f => f.orderIndex === frame.orderIndex);
    if (idx >= 0) {
      const halfDuration = Math.max(1.5, Math.round((frame.estimatedDurationSeconds / 2) * 10) / 10);
      frame.estimatedDurationSeconds = halfDuration;

      const splitCopy: StoryboardFrameDto = {
        ...JSON.parse(JSON.stringify(frame)),
        id: '',
        orderIndex: frame.orderIndex + 1,
        framingIntent: 'CloseUp',
        cameraMotionIntent: 'SlowZoomIn',
        visualPrompt: frame.visualPrompt + ' (Toma de detalle complementaria)'
      };

      list.splice(idx + 1, 0, splitCopy);
      list.forEach((f, i) => f.orderIndex = i + 1);
      this.frames.set(list);
      this.hasUnsavedChanges.set(true);
    }
  }

  saveDraft() {
    const sb = this.storyboard();
    const id = this.contentItemId();
    this.isSaving.set(true);

    const frameRequests: SaveStoryboardFrameRequest[] = this.frames().map(f => ({
      id: f.id || null,
      orderIndex: f.orderIndex,
      scriptSceneId: f.scriptSceneId,
      scriptSceneOrderIndex: f.scriptSceneOrderIndex,
      framingIntent: f.framingIntent,
      compositionIntent: f.compositionIntent,
      cameraMotionIntent: f.cameraMotionIntent,
      subject: f.subject,
      environment: f.environment,
      styleIntent: f.styleIntent,
      visualPrompt: f.visualPrompt,
      negativePrompt: f.negativePrompt,
      audioCue: f.audioCue,
      estimatedDurationSeconds: f.estimatedDurationSeconds,
      onScreenText: f.onScreenText,
      transitionIntent: f.transitionIntent
    }));

    if (!sb) {
      this.api.createStoryboard(id, {
        title: this.contentItem()?.title || 'Storyboard',
        targetDurationSeconds: this.approvedScript()?.targetDurationSeconds || 45,
        frames: frameRequests
      }).subscribe({
        next: (created) => {
          this.storyboard.set(created);
          this.frames.set(created.frames ? JSON.parse(JSON.stringify(created.frames)) : []);
          this.hasUnsavedChanges.set(false);
          this.isSaving.set(false);
          this.loadEligibility();
          this.cdr.markForCheck();
        },
        error: (err) => {
          alert('Error al guardar: ' + (err.error?.message || err.message));
          this.isSaving.set(false);
          this.cdr.markForCheck();
        }
      });
    } else {
      const updateReq: UpdateStoryboardRequest = {
        title: sb.title,
        targetDurationSeconds: sb.targetDurationSeconds,
        frames: frameRequests,
        changeSummary: 'Edición manual de tomas y plan de activos',
        expectedVersion: sb.version
      };

      this.api.updateStoryboard(id, sb.id, updateReq).subscribe({
        next: (updated) => {
          this.storyboard.set(updated);
          this.frames.set(updated.frames ? JSON.parse(JSON.stringify(updated.frames)) : []);
          this.hasUnsavedChanges.set(false);
          this.isSaving.set(false);
          this.loadEligibility();
          this.cdr.markForCheck();
        },
        error: (err) => {
          if (err.status === 409) {
            alert('Conflicto de concurrencia: El storyboard fue modificado por otro operador. Se recargarán los cambios más recientes.');
            this.loadData();
          } else {
            alert('Error al actualizar: ' + (err.error?.message || err.message));
          }
          this.isSaving.set(false);
          this.cdr.markForCheck();
        }
      });
    }
  }

  submitForReview() {
    const sb = this.storyboard();
    if (!sb) return;
    this.isSubmitting.set(true);

    this.api.submitStoryboardForReview(this.contentItemId(), sb.id, { expectedVersion: sb.version }).subscribe({
      next: (res) => {
        this.storyboard.set(res);
        this.frames.set(res.frames ? JSON.parse(JSON.stringify(res.frames)) : []);
        this.hasUnsavedChanges.set(false);
        this.isSubmitting.set(false);
        this.loadEligibility();
        this.cdr.markForCheck();
      },
      error: (err) => {
        alert('Error al enviar a revisión: ' + (err.error?.message || err.message));
        this.isSubmitting.set(false);
        this.cdr.markForCheck();
      }
    });
  }

  approveStoryboard() {
    const sb = this.storyboard();
    if (!sb) return;
    this.isApproving.set(true);

    this.api.approveStoryboard(this.contentItemId(), sb.id, { expectedVersion: sb.version }).subscribe({
      next: (res) => {
        this.storyboard.set(res);
        this.frames.set(res.frames ? JSON.parse(JSON.stringify(res.frames)) : []);
        this.hasUnsavedChanges.set(false);
        this.isApproving.set(false);
        this.loadEligibility();
        this.cdr.markForCheck();
      },
      error: (err) => {
        alert('Error al aprobar: ' + (err.error?.message || err.message));
        this.isApproving.set(false);
        this.cdr.markForCheck();
      }
    });
  }

  onConfirmReject(reason: string) {
    const sb = this.storyboard();
    if (!sb) return;
    this.isRejecting.set(true);

    this.api.rejectStoryboard(this.contentItemId(), sb.id, { reason, expectedVersion: sb.version }).subscribe({
      next: (res) => {
        this.storyboard.set(res);
        this.frames.set(res.frames ? JSON.parse(JSON.stringify(res.frames)) : []);
        this.hasUnsavedChanges.set(false);
        this.isRejecting.set(false);
        this.isRejectModalOpen.set(false);
        this.loadEligibility();
        this.cdr.markForCheck();
      },
      error: (err) => {
        alert('Error al rechazar: ' + (err.error?.message || err.message));
        this.isRejecting.set(false);
        this.cdr.markForCheck();
      }
    });
  }

  reopenStoryboard() {
    const sb = this.storyboard();
    if (!sb) return;
    this.isReopening.set(true);

    this.api.reopenStoryboard(this.contentItemId(), sb.id, { expectedVersion: sb.version }).subscribe({
      next: (res) => {
        this.storyboard.set(res);
        this.frames.set(res.frames ? JSON.parse(JSON.stringify(res.frames)) : []);
        this.hasUnsavedChanges.set(false);
        this.isReopening.set(false);
        this.loadEligibility();
        this.cdr.markForCheck();
      },
      error: (err) => {
        alert('Error al reabrir: ' + (err.error?.message || err.message));
        this.isReopening.set(false);
        this.cdr.markForCheck();
      }
    });
  }

  reconcileStoryboard() {
    const sb = this.storyboard();
    if (!sb) return;
    this.isReconciling.set(true);

    this.api.reconcileStoryboard(this.contentItemId(), sb.id, { expectedVersion: sb.version, reuseFramePlanning: true }).subscribe({
      next: (res) => {
        this.storyboard.set(res);
        this.frames.set(res.frames ? JSON.parse(JSON.stringify(res.frames)) : []);
        this.hasUnsavedChanges.set(false);
        this.isReconciling.set(false);
        this.loadEligibility();
        this.cdr.markForCheck();
      },
      error: (err) => {
        alert('Error al reconciliar: ' + (err.error?.message || err.message));
        this.isReconciling.set(false);
        this.cdr.markForCheck();
      }
    });
  }

  onGenerateAi(options: PlanStoryboardOptions) {
    this.isGeneratingAi.set(true);
    this.api.generateAiStoryboard(this.contentItemId(), options).subscribe({
      next: (res) => {
        this.storyboard.set(res);
        this.frames.set(res.frames ? JSON.parse(JSON.stringify(res.frames)) : []);
        this.hasUnsavedChanges.set(false);
        this.isGeneratingAi.set(false);
        this.isAiModalOpen.set(false);
        this.loadEligibility();
        this.cdr.markForCheck();
      },
      error: (err) => {
        alert('Error en planificación IA: ' + (err.error?.message || err.message));
        this.isGeneratingAi.set(false);
        this.cdr.markForCheck();
      }
    });
  }

  runAiReview() {
    const sb = this.storyboard();
    if (!sb) return;
    this.isReviewing.set(true);

    this.api.reviewStoryboard(this.contentItemId(), sb.id).subscribe({
      next: (critique) => {
        this.critiqueResult.set(critique);
        this.isReviewing.set(false);
        this.showReviewPanel.set(true);
        this.cdr.markForCheck();
      },
      error: (err) => {
        alert('Error al ejecutar revisión IA: ' + (err.error?.message || err.message));
        this.isReviewing.set(false);
        this.cdr.markForCheck();
      }
    });
  }

  openVersionHistory() {
    const sb = this.storyboard();
    if (!sb) return;
    this.showVersionDrawer.set(true);
    this.isLoadingVersions.set(true);

    this.api.getStoryboardVersions(this.contentItemId(), sb.id).subscribe({
      next: (vList) => {
        this.versions.set(vList);
        this.isLoadingVersions.set(false);
        this.cdr.markForCheck();
      },
      error: () => {
        this.isLoadingVersions.set(false);
        this.cdr.markForCheck();
      }
    });
  }
}
