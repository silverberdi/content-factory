import { ChangeDetectorRef, Component, EventEmitter, Input, OnChanges, OnDestroy, OnInit, Output, SimpleChanges, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Subscription, interval } from 'rxjs';
import {
  ApiService,
  DispatchVoiceGenerationRequest,
  GeneratedAssetDto,
  JobDto,
  VoiceProductionOverviewDto,
  VoiceProfileDto,
  VoiceRequirementProductionDto,
  VoiceScriptSceneSummaryDto
} from '../../core/api.service';
import { VoiceCandidateCardComponent } from './voice-candidate-card.component';
import { JobDiagnosticsDrawerComponent } from './job-diagnostics-drawer.component';

@Component({
  selector: 'app-voice-production-studio',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    VoiceCandidateCardComponent,
    JobDiagnosticsDrawerComponent
  ],
  template: `
    <div class="space-y-6">

      <!-- Loading State -->
      @if (isLoading && !overview) {
        <div class="p-12 text-center rounded-2xl bg-slate-900/60 border border-slate-800">
          <i class="pi pi-spin pi-spinner text-2xl text-violet-400 mb-3 block"></i>
          <p class="text-sm text-slate-300 font-medium">Cargando Voice Production Studio...</p>
        </div>
      } @else if (overview) {

        <!-- Ineligibility / Staleness Banner -->
        @if (!overview.isEligibleForGeneration) {
          <div class="p-4 rounded-2xl bg-amber-950/40 border border-amber-600/40 text-amber-200 flex items-center justify-between flex-wrap gap-3">
            <div class="flex items-center gap-3">
              <div class="p-2 rounded-xl bg-amber-900/50 text-amber-400">
                <i class="pi pi-exclamation-triangle text-xl"></i>
              </div>
              <div>
                <h4 class="text-xs font-bold uppercase tracking-wider text-amber-300">Despacho de Voz Bloqueado</h4>
                <p class="text-xs text-amber-200/90 mt-0.5">{{ overview.ineligibilityReason ?? 'El Storyboard no se encuentra en un estado aprobado y vigente.' }}</p>
              </div>
            </div>

            <button (click)="onNavigateToStoryboard.emit()"
                    type="button"
                    class="px-3.5 py-1.5 rounded-xl bg-amber-600 hover:bg-amber-500 text-slate-950 text-xs font-bold transition-colors shadow">
              Abrir Storyboard
            </button>
          </div>
        }

        <!-- Top Control & Telemetry Bar -->
        <div class="p-5 rounded-2xl bg-slate-900/90 border border-slate-800/80 shadow-xl space-y-4">
          <div class="flex items-center justify-between flex-wrap gap-4">

            <!-- Title & Status Chips -->
            <div class="space-y-1">
              <div class="flex items-center gap-2 flex-wrap">
                <h3 class="text-base font-bold text-slate-100 flex items-center gap-2">
                  <i class="pi pi-volume-up text-violet-400"></i>
                  <span>Producción de Voz & Locución TTS</span>
                </h3>
                <span class="px-2 py-0.5 rounded text-[11px] font-mono bg-violet-950 border border-violet-500/40 text-violet-300 font-bold">
                  Storyboard v{{ overview.storyboardVersion }}
                </span>
                @if (overview.activeJobsCount > 0) {
                  <span class="px-2 py-0.5 rounded text-[11px] font-mono bg-violet-500/20 text-violet-300 animate-pulse flex items-center gap-1">
                    <span class="w-1.5 h-1.5 rounded-full bg-violet-400"></span>
                    {{ overview.activeJobsCount }} Sintetizando
                  </span>
                }
              </div>
              <p class="text-xs text-slate-400">
                Generación, revisión humana de dicción y selección de locución en formato WAV 16-bit Mono.
              </p>
            </div>

            <!-- Batch Dispatch Actions -->
            <div class="flex items-center gap-2.5 flex-wrap">
              <!-- Pronunciation Guidance Button -->
              <button (click)="isGuidanceModalOpen.set(true)"
                      type="button"
                      class="px-3 py-1.5 rounded-xl bg-slate-950 hover:bg-slate-800 border border-slate-800 text-xs text-violet-300 font-medium flex items-center gap-1.5 cursor-pointer transition-colors">
                <i class="pi pi-book text-[11px]"></i>
                <span>Guía de Pronunciación</span>
                @if (pronunciationGuidanceText) {
                  <span class="w-2 h-2 rounded-full bg-violet-400"></span>
                }
              </button>

              <!-- Candidates Selector -->
              <div class="flex items-center gap-2 bg-slate-950 px-3 py-1.5 rounded-xl border border-slate-800 text-xs">
                <span class="text-slate-400">Tomas:</span>
                <select [(ngModel)]="candidateCount"
                        class="bg-transparent text-slate-200 font-bold focus:outline-none cursor-pointer">
                  <option [value]="1" class="bg-slate-900">1 toma</option>
                  <option [value]="2" class="bg-slate-900">2 tomas (comparación)</option>
                </select>
              </div>

              <!-- Dispatch Button -->
              <button (click)="dispatchVoiceGeneration()"
                      type="button"
                      [disabled]="!overview.isEligibleForGeneration || isDispatching"
                      class="px-4 py-1.5 rounded-xl bg-violet-600 hover:bg-violet-500 disabled:opacity-50 text-white text-xs font-bold flex items-center gap-2 transition-all shadow-md cursor-pointer disabled:cursor-not-allowed">
                @if (isDispatching) {
                  <i class="pi pi-spin pi-spinner text-xs"></i>
                  <span>Despachando...</span>
                } @else {
                  <i class="pi pi-microphone text-xs"></i>
                  <span>{{ overview.generatedCount > 0 ? 'Regenerar Locución' : 'Generar Locución' }}</span>
                }
              </button>
            </div>
          </div>

          <!-- KPI & Voice Profile Overview -->
          <div class="grid grid-cols-2 sm:grid-cols-4 gap-3 pt-2 border-t border-slate-800/80 text-xs">
            <!-- Voice Profile -->
            <div class="p-3 rounded-xl bg-slate-950/60 border border-slate-800/60 space-y-1">
              <span class="text-slate-500 block text-[10px] uppercase font-bold tracking-wider">Perfil de Voz Activo</span>
              <div class="font-bold text-slate-200 truncate" [title]="overview.effectiveVoiceProfile?.profileName ?? 'IA Simple ES Narrator'">
                {{ overview.effectiveVoiceProfile?.profileName ?? 'IA Simple ES Narrator' }}
              </div>
              <div class="text-[10px] text-violet-400 font-mono">
                {{ overview.effectiveVoiceProfile?.speakingStyle ?? 'Explicativo' }} • {{ overview.effectiveVoiceProfile?.configuredLocale ?? 'es-ES' }}
              </div>
            </div>

            <!-- Script Estimated Duration -->
            <div class="p-3 rounded-xl bg-slate-950/60 border border-slate-800/60 space-y-1">
              <span class="text-slate-500 block text-[10px] uppercase font-bold tracking-wider">Duración Estimada Guión</span>
              <div class="font-mono text-base font-bold text-slate-200">
                {{ overview.scriptEstimatedDurationSeconds }}s
              </div>
              <div class="text-[10px] text-slate-400">
                Basado en ~140 PPM
              </div>
            </div>

            <!-- Measured Voice Duration -->
            <div class="p-3 rounded-xl bg-slate-950/60 border border-slate-800/60 space-y-1">
              <span class="text-slate-500 block text-[10px] uppercase font-bold tracking-wider">Duración Medida Locución</span>
              <div class="font-mono text-base font-bold"
                   [ngClass]="overview.measuredVoiceDurationSeconds ? 'text-violet-300' : 'text-slate-500'">
                {{ overview.measuredVoiceDurationSeconds ? overview.measuredVoiceDurationSeconds + 's' : 'Pendiente' }}
              </div>
              <div class="text-[10px] text-slate-400">
                {{ overview.measuredVoiceDurationSeconds ? 'Audio master WAV generado' : 'Sin tomas seleccionadas' }}
              </div>
            </div>

            <!-- Timing Tolerance & Deviation -->
            <div class="p-3 rounded-xl bg-slate-950/60 border border-slate-800/60 space-y-1">
              <span class="text-slate-500 block text-[10px] uppercase font-bold tracking-wider">Desviación / Tolerancia</span>
              <div class="font-mono text-base font-bold flex items-center gap-1.5">
                @if (overview.timingDeviationSeconds !== null && overview.timingDeviationSeconds !== undefined) {
                  <span [ngClass]="{
                    'text-emerald-400': overview.timingStatus === 'Normal',
                    'text-amber-400': overview.timingStatus === 'Notice',
                    'text-red-400': overview.timingStatus === 'Warning'
                  }">
                    {{ overview.timingDeviationSeconds >= 0 ? '+' + overview.timingDeviationSeconds + 's' : overview.timingDeviationSeconds + 's' }}
                  </span>
                  <span class="px-1.5 py-0.5 rounded text-[10px] font-sans font-bold"
                        [ngClass]="{
                          'bg-emerald-950 text-emerald-300 border border-emerald-800': overview.timingStatus === 'Normal',
                          'bg-amber-950 text-amber-300 border border-amber-800': overview.timingStatus === 'Notice',
                          'bg-red-950 text-red-300 border border-red-800': overview.timingStatus === 'Warning'
                        }">
                    {{ overview.timingStatus }}
                  </span>
                } @else {
                  <span class="text-slate-500 text-sm">N/A</span>
                }
              </div>
              <div class="text-[10px] text-slate-400">
                Tolerancia: ±2s aviso / ±4s alerta
              </div>
            </div>
          </div>
        </div>

        <!-- Narration Script Source Accordion -->
        <div class="rounded-2xl bg-slate-900/60 border border-slate-800/80 overflow-hidden shadow-sm">
          <button (click)="isScriptAccordionExpanded.set(!isScriptAccordionExpanded())"
                  type="button"
                  class="w-full p-4 flex items-center justify-between text-left hover:bg-slate-800/40 transition-colors cursor-pointer">
            <div class="flex items-center gap-2.5">
              <i class="pi pi-align-left text-violet-400"></i>
              <span class="text-xs font-bold text-slate-200">Texto de Locución del Guión Aprobado</span>
              <span class="px-2 py-0.5 rounded bg-slate-950 border border-slate-800 text-[10px] font-mono text-slate-400">
                {{ overview.scriptScenes.length }} escenas
              </span>
            </div>
            <i class="pi text-xs text-slate-400" [ngClass]="isScriptAccordionExpanded() ? 'pi-chevron-up' : 'pi-chevron-down'"></i>
          </button>

          @if (isScriptAccordionExpanded()) {
            <div class="p-4 pt-0 border-t border-slate-800/60 space-y-3 mt-3">
              @for (scene of overview.scriptScenes; track scene.sceneId) {
                <div class="p-3 rounded-xl bg-slate-950/70 border border-slate-800/60 text-xs space-y-1">
                  <div class="flex items-center justify-between text-[10px] text-slate-400 font-mono">
                    <span class="font-bold text-violet-300">Escena #{{ scene.orderIndex }} ({{ scene.sceneType }})</span>
                    <span>{{ scene.wordCount }} palabras • ~{{ scene.estimatedDurationSeconds }}s</span>
                  </div>
                  <p class="text-slate-200 leading-relaxed font-sans">{{ scene.narrationText }}</p>
                </div>
              }
            </div>
          }
        </div>

        <!-- Requirements & Candidate Takes Grid -->
        <div class="space-y-6">
          @for (req of overview.requirements; track req.requirement.id) {
            <div class="space-y-4">
              <!-- Requirement Title / Active Job Status -->
              <div class="flex items-center justify-between flex-wrap gap-2">
                <div class="flex items-center gap-2">
                  <h4 class="text-sm font-bold text-slate-100 flex items-center gap-2">
                    <i class="pi pi-microphone text-violet-400"></i>
                    <span>Tomas de Locución para Asset Requirement</span>
                  </h4>
                  <span class="px-2 py-0.5 rounded bg-slate-900 border border-slate-800 text-[11px] font-mono text-slate-300">
                    {{ req.candidates.length }} tomas generadas
                  </span>
                </div>

                <!-- Active Job Diagnostics Trigger -->
                @if (req.activeJob) {
                  <button (click)="openJobDiagnostics(req.activeJob)"
                          type="button"
                          class="px-2.5 py-1 rounded-lg bg-slate-900 hover:bg-slate-800 border border-slate-800 text-[11px] font-mono text-slate-300 flex items-center gap-1.5 cursor-pointer">
                    <i class="pi pi-cog text-[10px]"></i>
                    <span>Job #{{ req.activeJob.id.substring(0, 8) }} ({{ req.activeJob.status }})</span>
                  </button>
                }
              </div>

              <!-- Candidate Comparison Cards -->
              @if (req.candidates.length === 0 && !req.activeJob) {
                <div class="p-8 text-center rounded-2xl bg-slate-900/40 border border-dashed border-slate-800 text-slate-400 space-y-2">
                  <i class="pi pi-volume-off text-2xl text-slate-600 block"></i>
                  <p class="text-xs font-medium">Aún no se ha generado ninguna toma de voz para esta pieza.</p>
                  <p class="text-[11px] text-slate-500">Haga clic en "Generar Locución" para sintetizar las tomas maestras.</p>
                </div>
              } @else {
                <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
                  @for (candidate of req.candidates; track candidate.id) {
                    <app-voice-candidate-card
                      [candidate]="candidate"
                      [estimatedDurationSeconds]="overview.scriptEstimatedDurationSeconds"
                      (onApprove)="handleApproveCandidate($event)"
                      (onReject)="handleRejectCandidate($event)"
                      (onSelectForAssembly)="handleSelectForAssembly($event)">
                    </app-voice-candidate-card>
                  }
                </div>
              }
            </div>
          }
        </div>

      }

      <!-- Pronunciation Guidance Modal -->
      @if (isGuidanceModalOpen()) {
        <div class="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/70 backdrop-blur-xs">
          <div class="w-full max-w-lg rounded-2xl bg-slate-900 border border-slate-800 shadow-2xl p-6 space-y-4">
            <div class="flex items-center justify-between border-b border-slate-800 pb-3">
              <h3 class="text-sm font-bold text-slate-100 flex items-center gap-2">
                <i class="pi pi-book text-violet-400"></i>
                <span>Guía de Pronunciación y Fonética</span>
              </h3>
              <button (click)="isGuidanceModalOpen.set(false)" type="button" class="text-slate-400 hover:text-slate-200 cursor-pointer">
                <i class="pi pi-times text-sm"></i>
              </button>
            </div>

            <p class="text-xs text-slate-400">
              Instruya al modelo TTS sobre la pronunciación de acrónimos en inglés o tecnicismos (ej. "IA" -> "inteligencia artificial", "ChatGPT" -> "chat-ge-pe-te").
            </p>

            <textarea [(ngModel)]="pronunciationGuidanceText"
                      rows="4"
                      placeholder="Ejemplo: Pronunciar 'AI' como 'inteligencia artificial', 'LLM' como 'ele-ele-eme'..."
                      class="w-full p-3 rounded-xl bg-slate-950 border border-slate-800 text-xs text-slate-200 placeholder-slate-500 focus:outline-none focus:border-violet-500">
            </textarea>

            <div class="flex items-center justify-end gap-2 pt-2">
              <button (click)="isGuidanceModalOpen.set(false)"
                      type="button"
                      class="px-3.5 py-1.5 rounded-xl bg-slate-800 hover:bg-slate-700 text-slate-200 text-xs font-semibold cursor-pointer">
                Guardar y Cerrar
              </button>
              <button (click)="handleSaveGuidanceAndRegenerate()"
                      type="button"
                      [disabled]="isDispatching"
                      class="px-3.5 py-1.5 rounded-xl bg-violet-600 hover:bg-violet-500 disabled:opacity-50 text-white text-xs font-bold flex items-center gap-1.5 cursor-pointer shadow-md">
                <i class="pi pi-sparkles text-[10px]"></i>
                <span>Guardar y Regenerar</span>
              </button>
            </div>
          </div>
        </div>
      }

      <!-- Job Technical Diagnostics Drawer -->
      <app-job-diagnostics-drawer
        [(visible)]="isJobDrawerVisible"
        [job]="drawerActiveJob"
        (onJobRetried)="handleJobRetried($event)">
      </app-job-diagnostics-drawer>

    </div>
  `
})
export class VoiceProductionStudioComponent implements OnInit, OnChanges, OnDestroy {
  @Input({ required: true }) contentItemId!: string;
  @Input({ required: true }) storyboardId!: string;

  @Output() onNavigateToStoryboard = new EventEmitter<void>();

  overview: VoiceProductionOverviewDto | null = null;
  isLoading = false;
  isDispatching = false;
  candidateCount = 1;
  pronunciationGuidanceText = '';

  readonly isScriptAccordionExpanded = signal<boolean>(false);
  readonly isGuidanceModalOpen = signal<boolean>(false);
  
  isJobDrawerVisible = false;
  drawerActiveJob: JobDto | null = null;

  private pollSubscription?: Subscription;

  constructor(private apiService: ApiService, private cdr: ChangeDetectorRef) {}

  ngOnInit(): void {
    this.loadOverview();
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['contentItemId'] || changes['storyboardId']) {
      this.loadOverview();
    }
  }

  ngOnDestroy(): void {
    this.stopPolling();
  }

  loadOverview(): void {
    if (!this.contentItemId || !this.storyboardId) return;

    this.isLoading = true;
    this.apiService.getVoiceProductionOverview(this.contentItemId, this.storyboardId).subscribe({
      next: (res) => {
        this.overview = res;
        this.isLoading = false;
        this.cdr.markForCheck();
        this.evaluatePolling();
      },
      error: (err) => {
        console.error('Failed to load voice production overview:', err);
        this.isLoading = false;
        this.cdr.markForCheck();
      }
    });
  }

  dispatchVoiceGeneration(): void {
    if (!this.overview || !this.overview.isEligibleForGeneration || this.isDispatching) return;

    this.isDispatching = true;
    const request: DispatchVoiceGenerationRequest = {
      candidateCount: this.candidateCount,
      pronunciationGuidance: this.pronunciationGuidanceText.trim() || null
    };

    this.apiService.dispatchVoiceGeneration(this.contentItemId, this.storyboardId, request).subscribe({
      next: () => {
        this.isDispatching = false;
        this.loadOverview();
      },
      error: (err) => {
        console.error('Failed to dispatch voice generation:', err);
        this.isDispatching = false;
      }
    });
  }

  handleApproveCandidate(candidateId: string): void {
    this.apiService.reviewCandidate(candidateId, { status: 'Approved' }).subscribe({
      next: () => {
        this.loadOverview();
      },
      error: (err) => {
        console.error('Failed to approve voice candidate:', err);
      }
    });
  }

  handleRejectCandidate(event: { id: string; reason: string }): void {
    this.apiService.reviewCandidate(event.id, { status: 'Rejected', rejectionReason: event.reason }).subscribe({
      next: () => {
        this.loadOverview();
      },
      error: (err) => {
        console.error('Failed to reject voice candidate:', err);
      }
    });
  }

  handleSelectForAssembly(candidateId: string): void {
    this.apiService.selectCandidateForAssembly(candidateId).subscribe({
      next: () => {
        this.loadOverview();
      },
      error: (err) => {
        console.error('Failed to select voice candidate for assembly:', err);
      }
    });
  }

  handleSaveGuidanceAndRegenerate(): void {
    this.isGuidanceModalOpen.set(false);
    this.dispatchVoiceGeneration();
  }

  openJobDiagnostics(job: JobDto): void {
    this.drawerActiveJob = job;
    this.isJobDrawerVisible = true;
  }

  handleJobRetried(updatedJob: JobDto): void {
    this.drawerActiveJob = updatedJob;
    this.loadOverview();
  }

  private evaluatePolling(): void {
    if (this.overview && this.overview.activeJobsCount > 0) {
      if (!this.pollSubscription) {
        this.pollSubscription = interval(2500).subscribe(() => {
          this.silentRefresh();
        });
      }
    } else {
      this.stopPolling();
    }
  }

  private silentRefresh(): void {
    if (!this.contentItemId || !this.storyboardId) return;

    this.apiService.getVoiceProductionOverview(this.contentItemId, this.storyboardId).subscribe({
      next: (res) => {
        this.overview = res;
        this.evaluatePolling();
      },
      error: (err) => console.error('Silent refresh failed:', err)
    });
  }

  private stopPolling(): void {
    if (this.pollSubscription) {
      this.pollSubscription.unsubscribe();
      this.pollSubscription = undefined;
    }
  }
}
