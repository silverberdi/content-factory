import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { DialogModule } from 'primeng/dialog';
import { ApiService, JobDto } from '../../core/api.service';

@Component({
  selector: 'app-job-diagnostics-drawer',
  standalone: true,
  imports: [CommonModule, DialogModule],
  template: `
    <p-dialog [(visible)]="visible"
              [modal]="true"
              [dismissableMask]="true"
              [style]="{ width: '90vw', maxWidth: '750px' }"
              header="Technical Job Execution Diagnostics"
              (onHide)="closeDrawer()"
              styleClass="p-dialog-custom">

      @if (job) {
        <div class="space-y-5 p-2">

          <!-- Job Identity & Status Header -->
          <div class="p-4 rounded-xl bg-slate-900 border border-slate-800 flex items-center justify-between flex-wrap gap-3">
            <div class="space-y-1">
              <div class="flex items-center gap-2">
                <span class="text-xs font-mono font-bold text-cyan-400">{{ job.capability }}</span>
                <span class="text-slate-500">•</span>
                <span class="text-xs text-slate-300 font-semibold">{{ job.provider }}</span>
              </div>
              <p class="text-[11px] font-mono text-slate-400">Job ID: {{ job.id }}</p>
              <p class="text-[11px] font-mono text-slate-500">Correlation: {{ job.correlationId }}</p>
            </div>

            <div class="flex flex-col items-end gap-1.5">
              @if (job.status === 'Succeeded') {
                <span class="px-2.5 py-1 rounded-md bg-emerald-950 border border-emerald-500/50 text-emerald-300 text-xs font-bold flex items-center gap-1">
                  <svg class="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7"/></svg>
                  Succeeded
                </span>
              } @else if (job.status === 'Running') {
                <span class="px-2.5 py-1 rounded-md bg-cyan-950 border border-cyan-500/50 text-cyan-300 text-xs font-bold flex items-center gap-1 animate-pulse">
                  <svg class="w-3.5 h-3.5 animate-spin" fill="none" stroke="currentColor" viewBox="0 0 24 24"><circle cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4" class="opacity-25"></circle><path fill="currentColor" d="M4 12a8 8 0 018-8v8H4z" class="opacity-75"></path></svg>
                  Running
                </span>
              } @else if (job.status === 'Queued') {
                <span class="px-2.5 py-1 rounded-md bg-amber-950 border border-amber-500/50 text-amber-300 text-xs font-bold">
                  Queued
                </span>
              } @else if (job.status === 'FailedRetryable') {
                <span class="px-2.5 py-1 rounded-md bg-orange-950 border border-orange-500/50 text-orange-300 text-xs font-bold">
                  Failed (Transient / Retryable)
                </span>
              } @else if (job.status === 'FailedActionRequired') {
                <span class="px-2.5 py-1 rounded-md bg-red-950 border border-red-500/50 text-red-300 text-xs font-bold">
                  Failed (Action Required)
                </span>
              }
              <span class="text-[11px] text-slate-400 font-mono">Attempt {{ job.attemptCount }} of {{ job.maxAttempts }}</span>
            </div>
          </div>

          <!-- Technical Metrics Grid -->
          <div class="grid grid-cols-2 sm:grid-cols-4 gap-3">
            <div class="p-3 rounded-xl bg-slate-900/60 border border-slate-800">
              <span class="text-[11px] text-slate-400 block mb-1">Execution Duration</span>
              <span class="text-sm font-mono font-bold text-slate-100">{{ job.durationMs }} ms</span>
            </div>

            <div class="p-3 rounded-xl bg-slate-900/60 border border-slate-800">
              <span class="text-[11px] text-slate-400 block mb-1">Estimated Cost</span>
              <span class="text-sm font-mono font-bold text-cyan-300">
                {{ job.estimatedCostUsd != null ? ('$' + job.estimatedCostUsd.toFixed(4)) : 'N/A' }}
              </span>
            </div>

            <div class="p-3 rounded-xl bg-slate-900/60 border border-slate-800">
              <span class="text-[11px] text-slate-400 block mb-1">Actual Cost</span>
              <span class="text-sm font-mono font-bold text-emerald-300">
                {{ job.actualCostUsd != null ? ('$' + job.actualCostUsd.toFixed(4)) : 'N/A' }}
              </span>
            </div>

            <div class="p-3 rounded-xl bg-slate-900/60 border border-slate-800">
              <span class="text-[11px] text-slate-400 block mb-1">Candidates</span>
              <span class="text-sm font-mono font-bold text-slate-100">{{ job.candidateCount }}</span>
            </div>
          </div>

          <!-- Error Diagnostics (if any) -->
          @if (job.errorCode || job.sanitizedErrorMessage) {
            <div class="p-4 rounded-xl bg-red-950/30 border border-red-800/80 space-y-2">
              <div class="flex items-center justify-between">
                <span class="text-xs font-bold text-red-400 uppercase tracking-wider">Error Details</span>
                <span class="text-xs font-mono font-bold text-red-300 bg-red-900/50 px-2 py-0.5 rounded border border-red-700">
                  {{ job.errorCode ?? 'UNKNOWN' }}
                </span>
              </div>
              <p class="text-xs text-red-200 font-mono bg-slate-950 p-2.5 rounded-lg border border-red-900/50">
                {{ job.sanitizedErrorMessage }}
              </p>
            </div>
          }

          <!-- Execution Attempts History -->
          <div class="space-y-2">
            <h4 class="text-xs font-bold uppercase tracking-wider text-slate-400">Execution Attempt History</h4>

            @if (job.attempts && job.attempts.length > 0) {
              <div class="space-y-2">
                @for (attempt of job.attempts; track attempt.id) {
                  <div class="p-3 rounded-xl bg-slate-900/60 border border-slate-800 flex items-center justify-between gap-3 text-xs">
                    <div class="space-y-0.5">
                      <div class="flex items-center gap-2">
                        <span class="font-bold text-slate-200">Attempt #{{ attempt.attemptNumber }}</span>
                        <span class="px-2 py-0.5 rounded text-[10px] font-bold"
                              [ngClass]="{
                                'bg-emerald-950 text-emerald-300 border border-emerald-500/30': attempt.status === 'Succeeded',
                                'bg-red-950 text-red-300 border border-red-500/30': attempt.status !== 'Succeeded'
                              }">
                          {{ attempt.status }}
                        </span>
                      </div>
                      <span class="text-[11px] text-slate-400 font-mono">
                        {{ attempt.startedAtUtc | date:'HH:mm:ss.SSS' }} ({{ attempt.durationMs }}ms)
                      </span>
                    </div>

                    @if (attempt.errorCode) {
                      <div class="text-right">
                        <span class="text-[11px] text-red-400 font-mono">{{ attempt.errorCode }}</span>
                      </div>
                    }
                  </div>
                }
              </div>
            } @else {
              <p class="text-xs text-slate-500 italic">No execution attempts recorded yet.</p>
            }
          </div>

          <!-- Footer Actions -->
          <div class="pt-4 border-t border-slate-800 flex items-center justify-between">
            <div>
              @if (job.status === 'FailedActionRequired' || job.status === 'FailedRetryable') {
                <button (click)="retryJob()"
                        [disabled]="isRetrying"
                        type="button"
                        class="px-4 py-2 rounded-xl bg-cyan-600 hover:bg-cyan-500 disabled:opacity-50 text-white text-xs font-bold flex items-center gap-1.5 shadow-lg shadow-cyan-950/40">
                  <svg class="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15"/>
                  </svg>
                  {{ isRetrying ? 'Triggering Retry...' : 'Retry Job' }}
                </button>
              }
            </div>

            <button (click)="closeDrawer()"
                    type="button"
                    class="px-4 py-2 rounded-xl bg-slate-800 hover:bg-slate-700 text-slate-300 text-xs font-medium">
              Close
            </button>
          </div>

        </div>
      }

    </p-dialog>
  `
})
export class JobDiagnosticsDrawerComponent {
  @Input() visible = false;
  @Input() job: JobDto | null = null;

  @Output() visibleChange = new EventEmitter<boolean>();
  @Output() onJobRetried = new EventEmitter<JobDto>();

  isRetrying = false;

  constructor(private apiService: ApiService) {}

  closeDrawer(): void {
    this.visible = false;
    this.visibleChange.emit(false);
  }

  retryJob(): void {
    if (!this.job) return;
    this.isRetrying = true;
    this.apiService.retryJob(this.job.id).subscribe({
      next: (updatedJob) => {
        this.isRetrying = false;
        this.job = updatedJob;
        this.onJobRetried.emit(updatedJob);
      },
      error: () => {
        this.isRetrying = false;
      }
    });
  }
}
