import { Component, EventEmitter, Input, OnChanges, OnDestroy, OnInit, Output, SimpleChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Subscription, interval } from 'rxjs';
import {
  ApiService,
  GeneratedAssetDto,
  JobDto,
  StoryboardDto,
  VisualProductionOverviewDto,
  VisualRequirementProductionDto
} from '../../core/api.service';
import { VisualCandidateCardComponent } from './visual-candidate-card.component';
import { VisualCandidatePreviewModalComponent } from './visual-candidate-preview-modal.component';
import { JobDiagnosticsDrawerComponent } from './job-diagnostics-drawer.component';

@Component({
  selector: 'app-visual-asset-studio',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    VisualCandidateCardComponent,
    VisualCandidatePreviewModalComponent,
    JobDiagnosticsDrawerComponent
  ],
  template: `
    <div class="space-y-6">

      <!-- Loading State -->
      @if (isLoading && !overview) {
        <div class="p-12 text-center rounded-2xl bg-slate-900/60 border border-slate-800">
          <svg class="w-8 h-8 animate-spin mx-auto text-cyan-400 mb-3" fill="none" viewBox="0 0 24 24">
            <circle cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4" class="opacity-25"></circle>
            <path fill="currentColor" d="M4 12a8 8 0 018-8v8H4z" class="opacity-75"></path>
          </svg>
          <p class="text-sm text-slate-300 font-medium">Loading visual production studio...</p>
        </div>
      } @else if (overview) {

        <!-- Ineligibility / Staleness Banner -->
        @if (!overview.isEligibleForGeneration) {
          <div class="p-4 rounded-2xl bg-amber-950/40 border border-amber-600/40 text-amber-200 flex items-center justify-between flex-wrap gap-3">
            <div class="flex items-center gap-3">
              <div class="p-2 rounded-xl bg-amber-900/50 text-amber-400">
                <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z"/>
                </svg>
              </div>
              <div>
                <h4 class="text-xs font-bold uppercase tracking-wider text-amber-300">Production Dispatch Blocked</h4>
                <p class="text-xs text-amber-200/90 mt-0.5">{{ overview.ineligibilityReason ?? 'Storyboard is not in an approved, non-stale state.' }}</p>
              </div>
            </div>

            <button (click)="onNavigateToStoryboard.emit()"
                    type="button"
                    class="px-3.5 py-1.5 rounded-xl bg-amber-600 hover:bg-amber-500 text-slate-950 text-xs font-bold transition-colors shadow">
              Open Storyboard
            </button>
          </div>
        }

        <!-- Top Control & Telemetry Bar -->
        <div class="p-5 rounded-2xl bg-slate-900/90 border border-slate-800/80 shadow-xl space-y-4">
          <div class="flex items-center justify-between flex-wrap gap-4">

            <!-- Title & Counts -->
            <div class="space-y-1">
              <div class="flex items-center gap-2">
                <h3 class="text-base font-bold text-slate-100">Visual Production Execution</h3>
                <span class="px-2 py-0.5 rounded text-[11px] font-mono bg-cyan-950 border border-cyan-500/40 text-cyan-300 font-bold">
                  v{{ overview.storyboardVersion }} Approved
                </span>
                @if (overview.activeJobsCount > 0) {
                  <span class="px-2 py-0.5 rounded text-[11px] font-mono bg-cyan-500/20 text-cyan-300 animate-pulse flex items-center gap-1">
                    <span class="w-1.5 h-1.5 rounded-full bg-cyan-400"></span>
                    {{ overview.activeJobsCount }} Generating
                  </span>
                }
              </div>
              <p class="text-xs text-slate-400">
                Generate, review, and select provider-neutral 9:16 vertical visual assets for approved frames.
              </p>
            </div>

            <!-- Batch Dispatch Actions -->
            <div class="flex items-center gap-3">
              <div class="flex items-center gap-2 bg-slate-950 px-3 py-1.5 rounded-xl border border-slate-800 text-xs">
                <span class="text-slate-400">Variants:</span>
                <select [(ngModel)]="batchCandidateCount"
                        class="bg-transparent text-slate-200 font-bold focus:outline-none cursor-pointer">
                  <option [value]="1" class="bg-slate-900">1 candidate</option>
                  <option [value]="2" class="bg-slate-900">2 candidates</option>
                  <option [value]="3" class="bg-slate-900">3 candidates</option>
                  <option [value]="4" class="bg-slate-900">4 candidates</option>
                </select>
              </div>

              <button (click)="dispatchBatchGeneration()"
                      [disabled]="!overview.isEligibleForGeneration || isDispatching"
                      type="button"
                      class="px-4 py-2 rounded-xl bg-gradient-to-r from-cyan-600 to-blue-600 hover:from-cyan-500 hover:to-blue-500 disabled:opacity-40 disabled:cursor-not-allowed text-white text-xs font-bold shadow-lg shadow-cyan-950/50 flex items-center gap-2 transition-all">
                @if (isDispatching) {
                  <svg class="w-4 h-4 animate-spin" fill="none" viewBox="0 0 24 24">
                    <circle cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4" class="opacity-25"></circle>
                    <path fill="currentColor" d="M4 12a8 8 0 018-8v8H4z" class="opacity-75"></path>
                  </svg>
                  <span>Dispatching...</span>
                } @else {
                  <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M14.752 11.168l-3.197-2.132A1 1 0 0010 9.87v4.263a1 1 0 001.555.832l3.197-2.132a1 1 0 000-1.664z"/>
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 12a9 9 0 11-18 0 9 9 0 0118 0z"/>
                  </svg>
                  <span>Dispatch All Visuals</span>
                }
              </button>
            </div>
          </div>

          <!-- Progress Metrics Grid -->
          <div class="grid grid-cols-2 sm:grid-cols-4 gap-3 pt-2 border-t border-slate-800/80">
            <div class="p-3 rounded-xl bg-slate-950/60 border border-slate-800">
              <span class="text-[11px] text-slate-400 block mb-0.5">Total Visual Needs</span>
              <span class="text-base font-mono font-bold text-slate-100">{{ overview.totalRequirementsCount }} frames</span>
            </div>

            <div class="p-3 rounded-xl bg-slate-950/60 border border-slate-800">
              <span class="text-[11px] text-slate-400 block mb-0.5">Generated</span>
              <span class="text-base font-mono font-bold text-cyan-300">{{ overview.generatedCount }} / {{ overview.totalRequirementsCount }}</span>
            </div>

            <div class="p-3 rounded-xl bg-slate-950/60 border border-slate-800">
              <span class="text-[11px] text-slate-400 block mb-0.5">Approved & Selected</span>
              <span class="text-base font-mono font-bold text-emerald-400">{{ overview.approvedCount }} / {{ overview.totalRequirementsCount }}</span>
            </div>

            <div class="p-3 rounded-xl bg-slate-950/60 border border-slate-800">
              <span class="text-[11px] text-slate-400 block mb-0.5">Pending Editorial QA</span>
              <span class="text-base font-mono font-bold text-amber-400">{{ overview.pendingReviewCount }}</span>
            </div>
          </div>
        </div>

        <!-- Requirements Production List -->
        <div class="space-y-6">
          @for (reqGroup of overview.requirements; track reqGroup.requirement.id) {
            <div class="p-5 rounded-2xl bg-slate-900/60 border border-slate-800 space-y-4 hover:border-slate-700/80 transition-colors">

              <!-- Requirement Header Info -->
              <div class="flex items-start justify-between flex-wrap gap-3 pb-3 border-b border-slate-800/60">
                <div class="space-y-1">
                  <div class="flex items-center gap-2">
                    <span class="px-2 py-0.5 rounded-md bg-slate-800 border border-slate-700 text-slate-200 text-xs font-bold">
                      Frame #{{ reqGroup.frameOrderIndex }}
                    </span>
                    <span class="text-xs font-semibold text-cyan-400">{{ reqGroup.scriptSceneName }}</span>
                    <span class="text-slate-500">•</span>
                    <span class="text-xs text-slate-300 font-mono">{{ reqGroup.estimatedDurationSeconds }}s</span>
                    <span class="text-slate-500">•</span>
                    <span class="text-xs text-slate-400 font-medium">{{ reqGroup.framingIntent }}</span>
                  </div>

                  <p class="text-xs text-slate-200 font-medium line-clamp-2 mt-1">
                    {{ reqGroup.requirement.visualPrompt }}
                  </p>
                  @if (reqGroup.requirement.styleIntent) {
                    <p class="text-[11px] text-slate-400 italic">Style: {{ reqGroup.requirement.styleIntent }}</p>
                  }
                </div>

                <!-- Active Job Status & Single Dispatch -->
                <div class="flex items-center gap-2">
                  @if (reqGroup.activeJob) {
                    <button (click)="openJobDiagnostics(reqGroup.activeJob)"
                            type="button"
                            class="px-2.5 py-1 rounded-lg border text-xs font-mono font-semibold flex items-center gap-1.5 transition-colors cursor-pointer"
                            [ngClass]="{
                              'bg-cyan-950/80 border-cyan-500/50 text-cyan-300 animate-pulse': reqGroup.activeJob.status === 'Running',
                              'bg-amber-950/80 border-amber-500/50 text-amber-300': reqGroup.activeJob.status === 'Queued',
                              'bg-emerald-950/80 border-emerald-500/50 text-emerald-300': reqGroup.activeJob.status === 'Succeeded',
                              'bg-red-950/80 border-red-500/50 text-red-300': reqGroup.activeJob.status.startsWith('Failed')
                            }">
                      @if (reqGroup.activeJob.status === 'Running') {
                        <svg class="w-3.5 h-3.5 animate-spin" fill="none" viewBox="0 0 24 24"><circle cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4" class="opacity-25"></circle><path fill="currentColor" d="M4 12a8 8 0 018-8v8H4z" class="opacity-75"></path></svg>
                      }
                      Job: {{ reqGroup.activeJob.status }}
                      <span class="text-[10px] text-slate-400">🔍</span>
                    </button>
                  }

                  <button (click)="dispatchRequirementGeneration(reqGroup.requirement.id)"
                          [disabled]="!overview.isEligibleForGeneration || isDispatching"
                          type="button"
                          class="px-3 py-1 rounded-lg bg-slate-800 hover:bg-slate-700 disabled:opacity-40 text-slate-200 border border-slate-700 text-xs font-medium transition-colors flex items-center gap-1 shadow-sm">
                    <svg class="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4"/>
                    </svg>
                    <span>Generate Candidate</span>
                  </button>
                </div>
              </div>

              <!-- Candidates Strip Grid -->
              @if (reqGroup.candidates.length > 0) {
                <div class="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-5 xl:grid-cols-6 gap-3">
                  @for (candidate of reqGroup.candidates; track candidate.id) {
                    <app-visual-candidate-card
                      [candidate]="candidate"
                      (onApprove)="handleApproveCandidate($event)"
                      (onReject)="handleOpenRejectModal(candidate, reqGroup.candidates)"
                      (onSelect)="handleSelectCandidate($event)"
                      (onInspect)="handleInspectCandidate(candidate, reqGroup.candidates)">
                    </app-visual-candidate-card>
                  }
                </div>
              } @else {
                <div class="p-6 text-center rounded-xl bg-slate-950/40 border border-dashed border-slate-800">
                  <p class="text-xs text-slate-500">No visual assets generated yet for this frame requirement.</p>
                </div>
              }

            </div>
          }
        </div>

      }

      <!-- Modals and Drawers -->
      <app-visual-candidate-preview-modal
        [(visible)]="isPreviewModalVisible"
        [activeCandidate]="modalActiveCandidate"
        [allCandidates]="modalSiblingCandidates"
        (onApprove)="handleApproveCandidate($event)"
        (onSelect)="handleSelectCandidate($event)"
        (onReject)="handleConfirmRejectCandidate($event)">
      </app-visual-candidate-preview-modal>

      <app-job-diagnostics-drawer
        [(visible)]="isJobDrawerVisible"
        [job]="drawerActiveJob"
        (onJobRetried)="handleJobRetried($event)">
      </app-job-diagnostics-drawer>

    </div>
  `
})
export class VisualAssetStudioComponent implements OnInit, OnChanges, OnDestroy {
  @Input({ required: true }) contentItemId!: string;
  @Input({ required: true }) storyboard!: StoryboardDto;

  @Output() onNavigateToStoryboard = new EventEmitter<void>();

  overview: VisualProductionOverviewDto | null = null;
  isLoading = false;
  isDispatching = false;
  batchCandidateCount = 1;

  // Modals state
  isPreviewModalVisible = false;
  modalActiveCandidate: GeneratedAssetDto | null = null;
  modalSiblingCandidates: GeneratedAssetDto[] = [];

  isJobDrawerVisible = false;
  drawerActiveJob: JobDto | null = null;

  private pollSubscription?: Subscription;

  constructor(private apiService: ApiService) {}

  ngOnInit(): void {
    this.loadOverview();
    this.startPollingIfActive();
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['storyboard'] || changes['contentItemId']) {
      this.loadOverview();
    }
  }

  ngOnDestroy(): void {
    this.stopPolling();
  }

  loadOverview(): void {
    if (!this.contentItemId || !this.storyboard) return;
    this.isLoading = true;
    this.apiService.getVisualProductionOverview(this.contentItemId, this.storyboard.id).subscribe({
      next: (data) => {
        this.overview = data;
        this.isLoading = false;
        this.startPollingIfActive();
      },
      error: () => {
        this.isLoading = false;
      }
    });
  }

  private startPollingIfActive(): void {
    if (this.overview && this.overview.activeJobsCount > 0) {
      if (!this.pollSubscription || this.pollSubscription.closed) {
        this.pollSubscription = interval(2000).subscribe(() => {
          if (!this.contentItemId || !this.storyboard) return;
          this.apiService.getVisualProductionOverview(this.contentItemId, this.storyboard.id).subscribe({
            next: (data) => {
              this.overview = data;
              if (data.activeJobsCount === 0) {
                this.stopPolling();
              }
            }
          });
        });
      }
    } else {
      this.stopPolling();
    }
  }

  private stopPolling(): void {
    if (this.pollSubscription) {
      this.pollSubscription.unsubscribe();
      this.pollSubscription = undefined;
    }
  }

  dispatchBatchGeneration(): void {
    if (!this.overview || !this.overview.isEligibleForGeneration) return;
    this.isDispatching = true;
    this.apiService.dispatchVisualGeneration(this.contentItemId, this.storyboard.id, {
      candidateCount: Number(this.batchCandidateCount)
    }).subscribe({
      next: () => {
        this.isDispatching = false;
        this.loadOverview();
      },
      error: () => {
        this.isDispatching = false;
      }
    });
  }

  dispatchRequirementGeneration(requirementId: string): void {
    if (!this.overview || !this.overview.isEligibleForGeneration) return;
    this.isDispatching = true;
    this.apiService.dispatchVisualGeneration(this.contentItemId, this.storyboard.id, {
      assetRequirementId: requirementId,
      candidateCount: 1
    }).subscribe({
      next: () => {
        this.isDispatching = false;
        this.loadOverview();
      },
      error: () => {
        this.isDispatching = false;
      }
    });
  }

  handleApproveCandidate(candidate: GeneratedAssetDto): void {
    this.apiService.reviewCandidate(candidate.id, {
      status: 'Approved'
    }).subscribe({
      next: () => {
        this.loadOverview();
      }
    });
  }

  handleOpenRejectModal(candidate: GeneratedAssetDto, siblings: GeneratedAssetDto[]): void {
    this.modalActiveCandidate = candidate;
    this.modalSiblingCandidates = siblings;
    this.isPreviewModalVisible = true;
  }

  handleInspectCandidate(candidate: GeneratedAssetDto, siblings: GeneratedAssetDto[]): void {
    this.modalActiveCandidate = candidate;
    this.modalSiblingCandidates = siblings;
    this.isPreviewModalVisible = true;
  }

  handleConfirmRejectCandidate(event: { candidate: GeneratedAssetDto; reason: string }): void {
    this.apiService.reviewCandidate(event.candidate.id, {
      status: 'Rejected',
      rejectionReason: event.reason
    }).subscribe({
      next: () => {
        this.loadOverview();
      }
    });
  }

  handleSelectCandidate(candidate: GeneratedAssetDto): void {
    this.apiService.selectCandidateForAssembly(candidate.id).subscribe({
      next: () => {
        this.loadOverview();
      }
    });
  }

  openJobDiagnostics(job: JobDto): void {
    this.drawerActiveJob = job;
    this.isJobDrawerVisible = true;
  }

  handleJobRetried(job: JobDto): void {
    this.drawerActiveJob = job;
    this.loadOverview();
  }
}
