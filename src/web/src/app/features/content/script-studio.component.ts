import { Component, OnInit, inject, signal, computed, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterModule } from '@angular/router';
import {
  ApiService,
  ContentIdeaDto,
  ContentItemDetailDto,
  GenerateScriptOptions,
  SaveScriptSceneRequest,
  SceneType,
  ScriptDto,
  ScriptReviewResultDto,
  ScriptSceneDto,
  ScriptVersionDto,
  TruthSourceDto,
  VerifiableClaimDto
} from '../../core/api.service';
import { ScriptSceneCardComponent } from './script-scene-card.component';
import { GenerateScriptModalComponent } from './generate-script-modal.component';
import { ScriptReviewPanelComponent } from './script-review-panel.component';
import { ScriptVersionHistoryDrawerComponent } from './script-version-history-drawer.component';
import { RejectScriptModalComponent } from './reject-script-modal.component';
import { PageHeaderComponent } from '../../shared/layout/page-header.component';

@Component({
  selector: 'app-script-studio',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    RouterModule,
    ScriptSceneCardComponent,
    GenerateScriptModalComponent,
    ScriptReviewPanelComponent,
    ScriptVersionHistoryDrawerComponent,
    RejectScriptModalComponent,
    PageHeaderComponent
  ],
  host: { class: 'block w-full' },
  template: `
    <!-- Loading State -->
    <div *ngIf="isLoading()" class="p-12 text-center text-xs text-[var(--app-muted)] space-y-2">
      <i class="pi pi-spin pi-spinner text-2xl text-blue-500 block mx-auto"></i>
      <span class="font-medium text-sm text-[var(--app-text)]">Cargando Script Studio...</span>
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

    <!-- Main Script Studio Layout -->
    <div *ngIf="!isLoading() && contentItem()" class="cf-page-container space-y-4 text-xs pb-16">
      
      <!-- Canonical Operational Header -->
      <app-page-header 
        title="Script Studio"
        [subtitle]="script()?.title || contentItem()?.title || ''"
        [backLink]="['/content/items', contentItemId()]"
        backLabel="Detalle de Pieza">
        
        <div meta class="flex items-center gap-2 flex-wrap text-xs">
          <span class="px-2 py-0.5 rounded bg-blue-500/15 text-blue-600 dark:text-blue-400 border border-blue-500/30 text-[10px] font-bold">
            {{ contentItem()?.channelName || 'Canal' }}
          </span>
          
          <!-- Script Status Badge -->
          <span *ngIf="script()" class="px-2 py-0.5 rounded text-[10px] font-bold uppercase tracking-wider font-mono border"
                [ngClass]="{
                  'bg-emerald-500/15 text-emerald-600 dark:text-emerald-400 border-emerald-500/30': script()?.status === 'Approved',
                  'bg-amber-500/15 text-amber-600 dark:text-amber-400 border-amber-500/30': script()?.status === 'UnderReview',
                  'bg-blue-500/15 text-blue-600 dark:text-blue-400 border-blue-500/30': script()?.status === 'Draft',
                  'bg-red-500/15 text-red-600 dark:text-red-400 border-red-500/30': script()?.status === 'Rejected'
                }">
            {{ script()?.status }}
          </span>

          <!-- Version Tag -->
          <span *ngIf="script()" class="px-2 py-0.5 rounded bg-purple-500/15 text-purple-600 dark:text-purple-400 border border-purple-500/30 text-[10px] font-mono font-bold">
            v{{ script()?.version }}
          </span>

          <!-- Stale Invalidation Pill -->
          <span *ngIf="script()?.isStale" class="px-2 py-0.5 rounded bg-rose-500/15 text-rose-600 dark:text-rose-400 border border-rose-500/30 text-[10px] font-bold flex items-center gap-1 animate-pulse">
            <i class="pi pi-exclamation-triangle text-[9px]"></i>
            <span>Lineage Desactualizado</span>
          </span>
        </div>

        <div actions class="flex items-center gap-2 flex-wrap">
          <button (click)="openAiGenerateModal()" [disabled]="isActionInProgress()"
                  class="cf-btn-primary">
            <i class="pi pi-sparkles"></i>
            <span>{{ script() ? 'Regenerar con IA' : 'Generar Guión con IA' }}</span>
          </button>

          <button *ngIf="script()" (click)="requestAiReview()" [disabled]="isReviewing() || isActionInProgress()"
                  class="cf-btn-secondary">
            <i class="pi" [ngClass]="isReviewing() ? 'pi-spin pi-spinner' : 'pi-verified text-indigo-500'"></i>
            <span>{{ isReviewing() ? 'Auditando...' : 'Auditoría IA (Consultiva)' }}</span>
          </button>

          <button *ngIf="script()" (click)="isVersionHistoryOpen.set(true)"
                  class="cf-btn-secondary">
            <i class="pi pi-history"></i>
            <span>Versiones</span>
          </button>
        </div>

      </app-page-header>

      <!-- Live Pacing & Duration Dashboard Strip -->
      <div *ngIf="script()" class="cf-card p-3 sm:p-4 grid grid-cols-2 sm:grid-cols-4 gap-3 text-xs">
          
          <!-- Words Aggregate -->
          <div class="p-2.5 rounded-lg bg-[var(--app-bg)] border border-[var(--app-card-border)] space-y-0.5">
            <span class="text-[10px] text-[var(--app-muted)] font-bold uppercase tracking-wider block">Palabras Totales</span>
            <div class="flex items-baseline gap-1.5 font-mono">
              <span class="text-base font-extrabold text-[var(--app-text)]">{{ totalWords() }}</span>
              <span class="text-[10px] text-[var(--app-muted)]">palabras</span>
            </div>
          </div>

          <!-- Configured Speaking Rate (Pacing) -->
          <div class="p-2.5 rounded-lg bg-[var(--app-bg)] border border-[var(--app-card-border)] space-y-0.5">
            <span class="text-[10px] text-[var(--app-muted)] font-bold uppercase tracking-wider block">Pacing Configurado</span>
            <div class="flex items-center gap-1 font-mono">
              <select [(ngModel)]="currentPacingWpm" (ngModelChange)="onPacingChange()" [disabled]="script()?.status === 'Approved'"
                      class="px-1.5 py-0.5 rounded bg-[var(--app-card-bg)] border border-[var(--app-card-border)] text-xs font-bold text-blue-600 dark:text-blue-400">
                <option [ngValue]="130">130 WPM (Pausado)</option>
                <option [ngValue]="140">140 WPM (IA Simple ES)</option>
                <option [ngValue]="150">150 WPM (Rápido)</option>
              </select>
            </div>
          </div>

          <!-- Estimated Narration Duration -->
          <div class="p-2.5 rounded-lg bg-[var(--app-bg)] border border-[var(--app-card-border)] space-y-0.5">
            <span class="text-[10px] text-[var(--app-muted)] font-bold uppercase tracking-wider block">Duración Estimada</span>
            <div class="flex items-baseline gap-1.5 font-mono">
              <span class="text-base font-extrabold"
                    [ngClass]="{
                      'text-emerald-600 dark:text-emerald-400': totalDurationSeconds() >= 30 && totalDurationSeconds() <= 60,
                      'text-amber-600 dark:text-amber-400': (totalDurationSeconds() > 20 && totalDurationSeconds() < 30) || (totalDurationSeconds() > 60 && totalDurationSeconds() <= 70),
                      'text-red-600 dark:text-red-400': totalDurationSeconds() <= 20 || totalDurationSeconds() > 70
                    }">
                ~{{ totalDurationSeconds().toFixed(1) }}s
              </span>
              <span class="text-[10px] text-[var(--app-muted)]">/ {{ script()?.targetDurationSeconds || 45 }}s obj</span>
            </div>
          </div>

          <!-- Format Target Variance -->
          <div class="p-2.5 rounded-lg bg-[var(--app-bg)] border border-[var(--app-card-border)] space-y-0.5">
            <span class="text-[10px] text-[var(--app-muted)] font-bold uppercase tracking-wider block">Alineación Formato</span>
            <div class="flex items-center gap-1.5 font-mono text-xs">
              <span class="px-2 py-0.5 rounded text-[10px] font-bold uppercase"
                    [ngClass]="{
                      'bg-emerald-500/15 text-emerald-600': totalDurationSeconds() >= 30 && totalDurationSeconds() <= 60,
                      'bg-amber-500/15 text-amber-600': (totalDurationSeconds() > 20 && totalDurationSeconds() < 30) || (totalDurationSeconds() > 60 && totalDurationSeconds() <= 70),
                      'bg-red-500/15 text-red-600': totalDurationSeconds() <= 20 || totalDurationSeconds() > 70
                    }">
                {{ totalDurationSeconds() >= 30 && totalDurationSeconds() <= 60 ? 'Óptimo 30-60s' : (totalDurationSeconds() > 60 ? 'Excede 60s' : 'Demasiado Corto') }}
              </span>
            </div>
          </div>

        </div>

      <!-- Upstream Stale Lineage Alert Banner -->
      <div *ngIf="script()?.isStale" class="p-4 rounded-xl bg-rose-500/10 border border-rose-500/30 space-y-2">
        <div class="flex items-center gap-2 text-rose-600 dark:text-rose-400 font-extrabold text-xs">
          <i class="pi pi-exclamation-triangle text-sm"></i>
          <span>Advertencia de Trazabilidad: El Guión no está alineado con la base upstream actual</span>
        </div>
        <p class="text-[11px] text-[var(--app-text)] leading-relaxed">
          {{ script()?.staleReason || 'La idea seleccionada o el TruthSource evolucionaron a una versión más reciente. El guión no satisfará la compuerta de producción downstream hasta ser regenerado o reconciliado.' }}
        </p>
        <div class="flex items-center gap-2 pt-1">
          <button (click)="openAiGenerateModal()" class="px-3 py-1 rounded bg-rose-600 hover:bg-rose-500 text-white font-bold text-xs cursor-pointer shadow-xs">
            <i class="pi pi-refresh mr-1 text-[10px]"></i> Regenerar con Idea Actual
          </button>
        </div>
      </div>

      <!-- Active Rejection Notice Banner -->
      <div *ngIf="script()?.status === 'Rejected'" class="p-4 rounded-xl bg-red-500/10 border border-red-500/30 space-y-2">
        <div class="flex items-center justify-between">
          <div class="flex items-center gap-2 text-red-600 dark:text-red-400 font-extrabold text-xs">
            <i class="pi pi-times-circle text-sm"></i>
            <span>Guión en Estado Rechazado</span>
          </div>
          <span class="text-[10px] text-[var(--app-muted)]">Rechazado el {{ script()?.rejectedAtUtc | date:'yyyy-MM-dd HH:mm' }}</span>
        </div>
        <p class="text-[11px] text-[var(--app-text)] italic leading-relaxed">
          "{{ script()?.rejectionReason }}"
        </p>
        <div class="flex items-center gap-2 pt-1">
          <button (click)="reopenScript()" [disabled]="isActionInProgress()"
                  class="px-3.5 py-1.5 rounded-lg bg-blue-600 hover:bg-blue-500 text-white font-bold text-xs flex items-center gap-1 cursor-pointer disabled:opacity-50 shadow-xs">
            <i class="pi pi-replay text-xs"></i>
            <span>Reabrir para Revisión / Corrección (Draft)</span>
          </button>
        </div>
      </div>

      <!-- AI Review Advisory Result Panel (Collapsible) -->
      <app-script-review-panel
        *ngIf="aiReviewResult()"
        [reviewResult]="aiReviewResult()!"
        (close)="aiReviewResult.set(null)">
      </app-script-review-panel>

      <!-- Upstream Context Collapsible Strip (Idea + TruthSource Guardrails) -->
      <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
        
        <!-- Active Selected Idea Card -->
        <div class="bg-[var(--app-card-bg)] border border-[var(--app-card-border)] rounded-xl p-3.5 sm:p-4 shadow-xs space-y-2">
          <div class="flex items-center justify-between border-b border-[var(--app-card-border)] pb-2">
            <div class="flex items-center gap-1.5 text-purple-600 dark:text-purple-400 font-bold text-xs">
              <i class="pi pi-lightbulb"></i>
              <span>Idea Creativa Seleccionada</span>
            </div>
            <a [routerLink]="['/content/items', contentItemId(), 'ideas']" class="text-[10px] text-purple-600 dark:text-purple-400 hover:underline">
              Cambiar en Matriz →
            </a>
          </div>

          <div *ngIf="activeSelectedIdea()" class="space-y-1 text-xs">
            <p class="font-bold text-[var(--app-text)]">{{ activeSelectedIdea()?.title }}</p>
            <p class="text-[11px] text-[var(--app-muted)] line-clamp-2"><strong>Ángulo:</strong> {{ activeSelectedIdea()?.angle }}</p>
            <p class="text-[11px] text-[var(--app-muted)] line-clamp-2 italic"><strong>Gancho:</strong> "{{ activeSelectedIdea()?.hookStrategy }}"</p>
          </div>
          <div *ngIf="!activeSelectedIdea()" class="text-[11px] text-[var(--app-muted)] italic py-1">
            No hay idea seleccionada en la Matriz creativa.
          </div>
        </div>

        <!-- TruthSource Guardrails Card -->
        <div class="bg-[var(--app-card-bg)] border border-[var(--app-card-border)] rounded-xl p-3.5 sm:p-4 shadow-xs space-y-2">
          <div class="flex items-center justify-between border-b border-[var(--app-card-border)] pb-2">
            <div class="flex items-center gap-1.5 text-indigo-600 dark:text-indigo-400 font-bold text-xs">
              <i class="pi pi-shield"></i>
              <span>Guardrails del TruthSource</span>
            </div>
            <a [routerLink]="['/content/items', contentItemId(), 'truth-source']" class="text-[10px] text-indigo-600 dark:text-indigo-400 hover:underline">
              Review Studio →
            </a>
          </div>

          <div *ngIf="truthSource()" class="space-y-1.5 text-xs">
            <div class="flex items-center gap-2">
              <span class="px-1.5 py-0.5 rounded bg-emerald-500/15 text-emerald-600 font-mono text-[9px] font-bold">
                {{ truthSource()?.verifiableClaims?.length || 0 }} Claims Verificables
              </span>
              <span class="px-1.5 py-0.5 rounded bg-amber-500/15 text-amber-600 font-mono text-[9px] font-bold">
                {{ truthSource()?.doNotSayConstraints?.length || 0 }} Restricciones Do-Not-Say
              </span>
            </div>
            <div *ngIf="truthSource()?.doNotSayConstraints?.length" class="space-y-0.5 text-[10px] text-red-600 dark:text-red-400">
              <div *ngFor="let sc of truthSource()?.doNotSayConstraints" class="flex items-start gap-1">
                <span>🚫</span>
                <span class="line-clamp-1">{{ sc }}</span>
              </div>
            </div>
          </div>
        </div>

      </div>

      <!-- No Script Created Yet Banner -->
      <div *ngIf="!script()" class="bg-[var(--app-card-bg)] border border-[var(--app-card-border)] rounded-xl p-8 text-center space-y-4 shadow-xs">
        <div class="w-12 h-12 rounded-full bg-blue-500/10 text-blue-500 flex items-center justify-center mx-auto text-xl">
          <i class="pi pi-file-edit"></i>
        </div>
        <div class="space-y-1 max-w-md mx-auto">
          <h2 class="text-base font-bold text-[var(--app-text)]">Aún no existe guión para esta pieza</h2>
          <p class="text-[11px] text-[var(--app-muted)] leading-relaxed">
            Puedes generar una propuesta estructurada en 5 escenas automáticamente con IA o crear una estructura vacía para redactar manualmente.
          </p>
        </div>
        <div class="flex items-center justify-center gap-3 pt-2">
          <button (click)="openAiGenerateModal()" class="px-4 py-2 rounded-lg bg-blue-600 hover:bg-blue-500 text-white font-bold text-xs flex items-center gap-1.5 cursor-pointer shadow-xs">
            <i class="pi pi-sparkles"></i>
            <span>Generar Guión con IA</span>
          </button>
          <button (click)="createManualEmptyScript()" class="px-4 py-2 rounded-lg border border-[var(--app-card-border)] text-[var(--app-text)] hover:bg-[var(--app-card-bg)] font-semibold text-xs cursor-pointer">
            <i class="pi pi-plus mr-1"></i> Crear Guión Manual
          </button>
        </div>
      </div>

      <!-- Scene List Editor -->
      <div *ngIf="script()" class="space-y-4">
        
        <div class="flex items-center justify-between">
          <div class="flex items-center gap-2">
            <i class="pi pi-list text-blue-600 dark:text-blue-400"></i>
            <h2 class="text-sm font-bold text-[var(--app-text)]">Escenas del Guión</h2>
            <span class="px-2 py-0.5 rounded-full bg-[var(--app-bg)] border border-[var(--app-card-border)] font-mono text-[10px] font-bold">
              {{ scenes().length }} escenas
            </span>
          </div>

          <div *ngIf="script()?.status !== 'Approved'" class="flex items-center gap-2">
            <button (click)="addNewScene()" 
                    class="px-3 py-1.5 rounded-lg border border-[var(--app-card-border)] bg-[var(--app-card-bg)] hover:bg-[var(--app-bg)] text-[var(--app-text)] font-semibold text-xs flex items-center gap-1 cursor-pointer shadow-xs">
              <i class="pi pi-plus text-[10px]"></i>
              <span>Agregar Escena</span>
            </button>
          </div>
        </div>

        <!-- Scene Cards -->
        <div class="space-y-3">
          <app-script-scene-card
            *ngFor="let scene of scenes(); let idx = index"
            [scene]="scene"
            [pacingWpm]="currentPacingWpm"
            [availableClaims]="truthSource()?.verifiableClaims || []"
            [readOnly]="script()?.status === 'Approved'"
            [isFirst]="idx === 0"
            [isLast]="idx === scenes().length - 1"
            (changed)="onSceneChanged()"
            (moveUp)="moveScene(idx, -1)"
            (moveDown)="moveScene(idx, 1)"
            (delete)="deleteScene(idx)">
          </app-script-scene-card>
        </div>

      </div>

      <!-- Bottom Fixed Sticky Governance Action Bar -->
      <div *ngIf="script()" class="fixed bottom-0 inset-x-0 bg-[var(--app-card-bg)]/95 backdrop-blur-md border-t border-[var(--app-card-border)] py-2.5 px-3 sm:px-4 md:px-5 z-30 shadow-xl">
        <div class="w-full max-w-full flex items-center justify-between gap-4 flex-wrap text-xs">
          
          <div class="flex items-center gap-3">
            <span class="font-mono text-xs text-[var(--app-muted)]">
              Estado: <strong class="text-[var(--app-text)]">{{ script()?.status }}</strong> (v{{ script()?.version }})
            </span>
            <span class="text-[var(--app-muted)]">•</span>
            <span class="font-mono text-xs text-blue-600 dark:text-blue-400 font-bold">
              {{ totalWords() }} palabras (~{{ totalDurationSeconds().toFixed(1) }}s @{{ currentPacingWpm }} WPM)
            </span>
          </div>

          <!-- Governance State Machine Transitions -->
          <div class="flex items-center gap-2 flex-wrap">
            
            <!-- Save Changes in Draft state -->
            <button *ngIf="script()?.status === 'Draft'" (click)="saveDraft()" [disabled]="isSaving() || isActionInProgress()"
                    class="px-3.5 py-1.5 rounded-lg border border-[var(--app-card-border)] bg-[var(--app-bg)] hover:bg-[var(--app-card-bg)] text-[var(--app-text)] font-semibold text-xs flex items-center gap-1 cursor-pointer disabled:opacity-50 shadow-xs">
              <i *ngIf="isSaving()" class="pi pi-spin pi-spinner text-xs"></i>
              <i *ngIf="!isSaving()" class="pi pi-save text-xs"></i>
              <span>{{ isSaving() ? 'Guardando...' : 'Guardar Cambios' }}</span>
            </button>

            <!-- Submit for Review -->
            <button *ngIf="script()?.status === 'Draft'" (click)="submitForReview()" [disabled]="isActionInProgress()"
                    class="px-4 py-1.5 rounded-lg bg-amber-600 hover:bg-amber-500 text-white font-bold text-xs flex items-center gap-1.5 cursor-pointer disabled:opacity-50 shadow-xs">
              <i class="pi pi-send text-xs"></i>
              <span>Enviar a Revisión</span>
            </button>

            <!-- Reject Script (in UnderReview) -->
            <button *ngIf="script()?.status === 'UnderReview'" (click)="isRejectModalOpen.set(true)" [disabled]="isActionInProgress()"
                    class="px-3.5 py-1.5 rounded-lg bg-red-600 hover:bg-red-500 text-white font-bold text-xs flex items-center gap-1.5 cursor-pointer disabled:opacity-50 shadow-xs">
              <i class="pi pi-times-circle text-xs"></i>
              <span>Rechazar</span>
            </button>

            <!-- Approve Script (in UnderReview) -->
            <button *ngIf="script()?.status === 'UnderReview'" (click)="approveScript()" [disabled]="isActionInProgress() || script()?.isStale"
                    class="px-4 py-1.5 rounded-lg bg-emerald-600 hover:bg-emerald-500 text-white font-bold text-xs flex items-center gap-1.5 cursor-pointer disabled:opacity-50 shadow-xs"
                    [title]="script()?.isStale ? 'No se puede aprobar un guión con lineage desactualizado' : 'Aprobar guión para producción'">
              <i class="pi pi-check-circle text-xs"></i>
              <span>Aprobar Guión</span>
            </button>

            <!-- Reopen Script (in Rejected) -->
            <button *ngIf="script()?.status === 'Rejected'" (click)="reopenScript()" [disabled]="isActionInProgress()"
                    class="px-4 py-1.5 rounded-lg bg-blue-600 hover:bg-blue-500 text-white font-bold text-xs flex items-center gap-1.5 cursor-pointer disabled:opacity-50 shadow-xs">
              <i class="pi pi-replay text-xs"></i>
              <span>Reabrir a Borrador (Draft)</span>
            </button>

          </div>

        </div>
      </div>

      <!-- Generate Script Modal -->
      <app-generate-script-modal
        [isOpen]="isAiModalOpen()"
        [isLoading]="isGeneratingAi()"
        [selectedIdea]="activeSelectedIdea()"
        [truthSource]="truthSource()"
        (closed)="isAiModalOpen.set(false)"
        (generate)="onGenerateAiScript($event)">
      </app-generate-script-modal>

      <!-- Version History Drawer -->
      <app-script-version-history-drawer
        [isOpen]="isVersionHistoryOpen()"
        [versions]="versions()"
        (close)="isVersionHistoryOpen.set(false)">
      </app-script-version-history-drawer>

      <!-- Reject Script Modal -->
      <app-reject-script-modal
        [isOpen]="isRejectModalOpen()"
        [isLoading]="isActionInProgress()"
        (closed)="isRejectModalOpen.set(false)"
        (rejected)="onConfirmReject($event)">
      </app-reject-script-modal>

    </div>
  `
})
export class ScriptStudioComponent implements OnInit {
  private readonly api = inject(ApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly cdr = inject(ChangeDetectorRef);

  readonly contentItemId = signal<string>('');
  readonly contentItem = signal<ContentItemDetailDto | null>(null);
  readonly ideas = signal<ContentIdeaDto[]>([]);
  readonly truthSource = signal<TruthSourceDto | null>(null);
  readonly script = signal<ScriptDto | null>(null);
  readonly scenes = signal<ScriptSceneDto[]>([]);
  readonly versions = signal<ScriptVersionDto[]>([]);

  readonly isLoading = signal<boolean>(true);
  readonly errorMessage = signal<string | null>(null);
  readonly isSaving = signal<boolean>(false);
  readonly isGeneratingAi = signal<boolean>(false);
  readonly isReviewing = signal<boolean>(false);
  readonly isActionInProgress = signal<boolean>(false);

  readonly isAiModalOpen = signal<boolean>(false);
  readonly isVersionHistoryOpen = signal<boolean>(false);
  readonly isRejectModalOpen = signal<boolean>(false);

  readonly aiReviewResult = signal<ScriptReviewResultDto | null>(null);

  currentPacingWpm: number = 140;

  get activeSelectedIdea(): () => ContentIdeaDto | null {
    return () => this.ideas().find(i => i.status === 'Selected') || null;
  }

  totalWords = computed(() => {
    return this.scenes().reduce((acc, sc) => {
      const words = sc.narrationText ? sc.narrationText.trim().split(/\s+/).filter(Boolean).length : 0;
      return acc + words;
    }, 0);
  });

  totalDurationSeconds = computed(() => {
    const words = this.totalWords();
    const wpm = this.currentPacingWpm > 0 ? this.currentPacingWpm : 140;
    return Math.round((words / (wpm / 60.0)) * 10) / 10;
  });

  ngOnInit() {
    this.route.paramMap.subscribe(params => {
      const id = params.get('id');
      if (id) {
        this.contentItemId.set(id);
        this.loadAllData(id);
      } else {
        this.isLoading.set(false);
        this.errorMessage.set('ID de pieza no encontrado en la ruta.');
        this.cdr.markForCheck();
      }
    });
  }

  loadAllData(id: string) {
    this.isLoading.set(true);
    this.errorMessage.set(null);
    this.cdr.markForCheck();

    this.api.getContentItemDetail(id).subscribe({
      next: (detail) => {
        this.contentItem.set(detail);
        this.truthSource.set(detail.truthSource || null);

        // Fetch ideas
        this.api.getContentIdeas(id).subscribe({
          next: (ideas) => this.ideas.set(ideas),
          error: () => {}
        });

        // Fetch script if any
        this.api.getScript(id).subscribe({
          next: (sc) => {
            this.script.set(sc);
            this.currentPacingWpm = sc.pacingWpm > 0 ? sc.pacingWpm : 140;
            this.scenes.set([...sc.scenes]);
            this.isLoading.set(false);
            this.loadVersions(id, sc.id);
            this.cdr.markForCheck();
          },
          error: (err) => {
            // 404 is valid if script is not created yet
            this.script.set(null);
            this.scenes.set([]);
            this.isLoading.set(false);
            this.cdr.markForCheck();
          }
        });
      },
      error: (err) => {
        this.isLoading.set(false);
        this.errorMessage.set(err.error?.message || err.error?.error || 'Error al cargar la información de la pieza.');
        this.cdr.markForCheck();
      }
    });
  }

  loadVersions(contentItemId: string, scriptId: string) {
    this.api.getScriptVersions(contentItemId, scriptId).subscribe({
      next: (vers) => {
        this.versions.set(vers);
        this.cdr.markForCheck();
      },
      error: () => {}
    });
  }

  retryLoad() {
    const id = this.contentItemId();
    if (id) this.loadAllData(id);
  }

  onPacingChange() {
    this.onSceneChanged();
  }

  onSceneChanged() {
    const wpm = this.currentPacingWpm > 0 ? this.currentPacingWpm : 140;
    const updated = this.scenes().map(s => {
      const words = s.narrationText ? s.narrationText.trim().split(/\s+/).filter(Boolean).length : 0;
      const dur = Math.round((words / (wpm / 60.0)) * 10) / 10;
      return { ...s, wordCount: words, estimatedDurationSeconds: dur };
    });
    this.scenes.set(updated);
    this.cdr.markForCheck();
  }

  addNewScene() {
    const nextOrder = this.scenes().length + 1;
    const defaultTypes: (SceneType | string)[] = ['Hook', 'Problem', 'Insight', 'Climax', 'CallToAction'];
    const chosenType = nextOrder <= defaultTypes.length ? defaultTypes[nextOrder - 1] : 'Insight';

    const newSc: ScriptSceneDto = {
      id: '',
      scriptId: this.script()?.id || '',
      orderIndex: nextOrder,
      sceneType: chosenType,
      narrationText: '',
      visualPrompt: '',
      estimatedDurationSeconds: 0,
      wordCount: 0,
      evidenceReferences: []
    };

    this.scenes.set([...this.scenes(), newSc]);
    this.onSceneChanged();
  }

  moveScene(index: number, delta: number) {
    const targetIdx = index + delta;
    if (targetIdx < 0 || targetIdx >= this.scenes().length) return;

    const list = [...this.scenes()];
    const temp = list[index];
    list[index] = list[targetIdx];
    list[targetIdx] = temp;

    // Recalculate orderIndex
    list.forEach((s, i) => s.orderIndex = i + 1);
    this.scenes.set(list);
    this.cdr.markForCheck();
  }

  deleteScene(index: number) {
    const list = [...this.scenes()];
    list.splice(index, 1);
    list.forEach((s, i) => s.orderIndex = i + 1);
    this.scenes.set(list);
    this.cdr.markForCheck();
  }

  createManualEmptyScript() {
    const activeIdea = this.activeSelectedIdea();
    const ts = this.truthSource();

    if (!activeIdea) {
      alert('Se requiere una Idea seleccionada en la Matriz Creativa para iniciar el guión.');
      return;
    }
    if (!ts || ts.status !== 'Approved') {
      alert('Se requiere un TruthSource en estado Aprobado para iniciar el guión.');
      return;
    }

    const defaultScenes: SaveScriptSceneRequest[] = [
      { orderIndex: 1, sceneType: 'Hook', narrationText: '', visualPrompt: '' },
      { orderIndex: 2, sceneType: 'Problem', narrationText: '', visualPrompt: '' },
      { orderIndex: 3, sceneType: 'Insight', narrationText: '', visualPrompt: '' },
      { orderIndex: 4, sceneType: 'Climax', narrationText: '', visualPrompt: '' },
      { orderIndex: 5, sceneType: 'CallToAction', narrationText: '', visualPrompt: '' }
    ];

    this.isActionInProgress.set(true);
    this.api.createScript(this.contentItemId(), {
      title: activeIdea.title,
      targetDurationSeconds: 45,
      pacingWpm: 140,
      language: 'es-ES',
      scenes: defaultScenes
    }).subscribe({
      next: (created) => {
        this.isActionInProgress.set(false);
        this.script.set(created);
        this.scenes.set([...created.scenes]);
        this.loadVersions(this.contentItemId(), created.id);
        this.cdr.markForCheck();
      },
      error: (err) => {
        this.isActionInProgress.set(false);
        alert(err.error?.message || err.error?.error || 'Error al crear el guión manual.');
        this.cdr.markForCheck();
      }
    });
  }

  openAiGenerateModal() {
    this.isAiModalOpen.set(true);
  }

  onGenerateAiScript(options: GenerateScriptOptions) {
    this.isGeneratingAi.set(true);
    this.api.generateAiScript(this.contentItemId(), options).subscribe({
      next: (sc) => {
        this.isGeneratingAi.set(false);
        this.isAiModalOpen.set(false);
        this.script.set(sc);
        this.currentPacingWpm = sc.pacingWpm > 0 ? sc.pacingWpm : 140;
        this.scenes.set([...sc.scenes]);
        this.loadVersions(this.contentItemId(), sc.id);
        this.cdr.markForCheck();
      },
      error: (err) => {
        this.isGeneratingAi.set(false);
        alert(err.error?.message || err.error?.error || 'Error en la generación de guión con IA.');
        this.cdr.markForCheck();
      }
    });
  }

  requestAiReview() {
    const sc = this.script();
    if (!sc) return;

    this.isReviewing.set(true);
    this.api.reviewScript(this.contentItemId(), sc.id).subscribe({
      next: (result) => {
        this.isReviewing.set(false);
        this.aiReviewResult.set(result);
        this.cdr.markForCheck();
      },
      error: (err) => {
        this.isReviewing.set(false);
        alert(err.error?.message || err.error?.error || 'Error al ejecutar la auditoría consultiva de IA.');
        this.cdr.markForCheck();
      }
    });
  }

  saveDraft() {
    const sc = this.script();
    if (!sc) return;

    this.isSaving.set(true);
    const saveScenes: SaveScriptSceneRequest[] = this.scenes().map(s => ({
      id: s.id || null,
      orderIndex: s.orderIndex,
      sceneType: s.sceneType,
      narrationText: s.narrationText,
      visualPrompt: s.visualPrompt,
      evidenceReferences: s.evidenceReferences.map(er => ({
        id: er.id || undefined,
        truthSourceClaimId: er.truthSourceClaimId || undefined,
        claimStatement: er.claimStatement,
        editorialNote: er.editorialNote
      }))
    }));

    this.api.updateScript(this.contentItemId(), sc.id, {
      title: sc.title,
      targetDurationSeconds: sc.targetDurationSeconds,
      pacingWpm: this.currentPacingWpm,
      language: sc.language,
      scenes: saveScenes,
      changeSummary: 'Actualización manual de escenas y locución',
      expectedVersion: sc.version
    }).subscribe({
      next: (updated) => {
        this.isSaving.set(false);
        this.script.set(updated);
        this.scenes.set([...updated.scenes]);
        this.loadVersions(this.contentItemId(), updated.id);
        this.cdr.markForCheck();
      },
      error: (err) => {
        this.isSaving.set(false);
        if (err.status === 409) {
          alert('Conflicto de concurrencia: Otro operador modificó este guión. Se recargarán los cambios más recientes.');
          this.loadAllData(this.contentItemId());
        } else {
          alert(err.error?.message || err.error?.error || 'Error al guardar el borrador del guión.');
        }
        this.cdr.markForCheck();
      }
    });
  }

  submitForReview() {
    const sc = this.script();
    if (!sc) return;

    if (this.scenes().length === 0 || this.totalWords() === 0) {
      alert('El guión debe contener al menos una escena con locución antes de enviarse a revisión.');
      return;
    }

    this.isActionInProgress.set(true);
    this.api.submitScriptForReview(this.contentItemId(), sc.id, { expectedVersion: sc.version }).subscribe({
      next: (updated) => {
        this.isActionInProgress.set(false);
        this.script.set(updated);
        this.loadVersions(this.contentItemId(), updated.id);
        this.cdr.markForCheck();
      },
      error: (err) => {
        this.isActionInProgress.set(false);
        alert(err.error?.message || err.error?.error || 'Error al enviar a revisión.');
        this.cdr.markForCheck();
      }
    });
  }

  approveScript() {
    const sc = this.script();
    if (!sc) return;

    if (sc.isStale) {
      alert('No se puede aprobar un guión con lineage desactualizado. Se requiere regenerar o reconciliar primero.');
      return;
    }

    this.isActionInProgress.set(true);
    this.api.approveScript(this.contentItemId(), sc.id, { expectedVersion: sc.version }).subscribe({
      next: (updated) => {
        this.isActionInProgress.set(false);
        this.script.set(updated);
        this.loadVersions(this.contentItemId(), updated.id);
        this.cdr.markForCheck();
      },
      error: (err) => {
        this.isActionInProgress.set(false);
        alert(err.error?.message || err.error?.error || 'Error al aprobar el guión.');
        this.cdr.markForCheck();
      }
    });
  }

  onConfirmReject(reason: string) {
    const sc = this.script();
    if (!sc) return;

    this.isActionInProgress.set(true);
    this.api.rejectScript(this.contentItemId(), sc.id, { reason, expectedVersion: sc.version }).subscribe({
      next: (updated) => {
        this.isActionInProgress.set(false);
        this.isRejectModalOpen.set(false);
        this.script.set(updated);
        this.loadVersions(this.contentItemId(), updated.id);
        this.cdr.markForCheck();
      },
      error: (err) => {
        this.isActionInProgress.set(false);
        alert(err.error?.message || err.error?.error || 'Error al rechazar el guión.');
        this.cdr.markForCheck();
      }
    });
  }

  reopenScript() {
    const sc = this.script();
    if (!sc) return;

    this.isActionInProgress.set(true);
    this.api.reopenScript(this.contentItemId(), sc.id, { expectedVersion: sc.version }).subscribe({
      next: (updated) => {
        this.isActionInProgress.set(false);
        this.script.set(updated);
        this.loadVersions(this.contentItemId(), updated.id);
        this.cdr.markForCheck();
      },
      error: (err) => {
        this.isActionInProgress.set(false);
        alert(err.error?.message || err.error?.error || 'Error al reabrir el guión para revisión.');
        this.cdr.markForCheck();
      }
    });
  }
}
