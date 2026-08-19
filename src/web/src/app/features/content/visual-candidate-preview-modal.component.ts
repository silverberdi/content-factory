import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { DialogModule } from 'primeng/dialog';
import { ApiService, GeneratedAssetDto } from '../../core/api.service';

@Component({
  selector: 'app-visual-candidate-preview-modal',
  standalone: true,
  imports: [CommonModule, FormsModule, DialogModule],
  template: `
    <p-dialog [(visible)]="visible"
              [modal]="true"
              [dismissableMask]="true"
              [style]="{ width: '90vw', maxWidth: '1100px' }"
              [header]="'Visual Candidate #' + (activeCandidate?.variantIndex ?? 1) + ' Inspection'"
              (onHide)="closeModal()"
              styleClass="p-dialog-custom">

      @if (activeCandidate) {
        <div class="grid grid-cols-1 lg:grid-cols-12 gap-6 p-2">

          <!-- Left Column: Visual Preview (9:16) -->
          <div class="lg:col-span-5 flex flex-col items-center">
            <div class="relative w-full max-w-[340px] aspect-[9/16] bg-slate-950 rounded-2xl border border-slate-800 overflow-hidden shadow-2xl flex items-center justify-center">
              <img [src]="getStreamUrl(activeCandidate.id)"
                   [alt]="'Variant ' + activeCandidate.variantIndex"
                   class="w-full h-full object-cover" />

              @if (activeCandidate.isSelectedForAssembly) {
                <div class="absolute top-3 right-3 px-2.5 py-1 rounded-full bg-emerald-500 text-slate-950 text-xs font-extrabold shadow-lg flex items-center gap-1">
                  <svg class="w-3.5 h-3.5 fill-current" viewBox="0 0 20 20">
                    <path d="M9.049 2.927c.3-.921 1.603-.921 1.902 0l1.07 3.292a1 1 0 00.95.69h3.462c.969 0 1.371 1.24.588 1.81l-2.8 2.034a1 1 0 00-.364 1.118l1.07 3.292c.3.921-.755 1.688-1.54 1.118l-2.8-2.034a1 1 0 00-1.175 0l-2.8 2.034c-.784.57-1.838-.197-1.539-1.118l1.07-3.292a1 1 0 00-.364-1.118L2.98 8.72c-.783-.57-.38-1.81.588-1.81h3.461a1 1 0 00.951-.69l1.07-3.292z"/>
                  </svg>
                  Selected For Assembly
                </div>
              }
            </div>

            <!-- Sibling Candidate Switcher Bar -->
            @if (allCandidates.length > 1) {
              <div class="mt-4 flex items-center gap-2 p-1.5 rounded-xl bg-slate-900 border border-slate-800">
                <span class="text-xs text-slate-400 px-2 font-medium">Variants:</span>
                @for (cand of allCandidates; track cand.id) {
                  <button (click)="activeCandidate = cand"
                          type="button"
                          [class]="activeCandidate.id === cand.id
                            ? 'px-3 py-1 rounded-lg bg-cyan-600 text-white font-bold text-xs shadow'
                            : 'px-3 py-1 rounded-lg bg-slate-800 text-slate-300 hover:bg-slate-700 text-xs'">
                    #{{ cand.variantIndex }}
                    @if (cand.isSelectedForAssembly) { ⭐ }
                  </button>
                }
              </div>
            }
          </div>

          <!-- Right Column: Metadata & Review Actions -->
          <div class="lg:col-span-7 flex flex-col justify-between space-y-4">

            <div class="space-y-4">
              <!-- Header Status Bar -->
              <div class="flex items-center justify-between p-3 rounded-xl bg-slate-900/90 border border-slate-800">
                <div class="flex items-center gap-2">
                  <span class="text-xs font-semibold uppercase tracking-wider text-slate-400">Status:</span>
                  @if (activeCandidate.status === 'Approved') {
                    <span class="px-2 py-0.5 rounded bg-emerald-950 border border-emerald-500/50 text-emerald-300 text-xs font-bold">Approved</span>
                  } @else if (activeCandidate.status === 'PendingReview') {
                    <span class="px-2 py-0.5 rounded bg-amber-950 border border-amber-500/50 text-amber-300 text-xs font-bold">Pending Editorial QA</span>
                  } @else if (activeCandidate.status === 'Rejected') {
                    <span class="px-2 py-0.5 rounded bg-red-950 border border-red-500/50 text-red-300 text-xs font-bold">Rejected</span>
                  }
                </div>

                <div class="flex items-center gap-3 text-xs text-slate-400 font-mono">
                  <span>{{ activeCandidate.width }}×{{ activeCandidate.height }}</span>
                  <span>•</span>
                  <span>{{ (activeCandidate.fileSizeBytes / 1024).toFixed(0) }} KB</span>
                  <span>•</span>
                  <span>{{ activeCandidate.provider }}</span>
                </div>
              </div>

              <!-- Rejection Reason Warning (if rejected) -->
              @if (activeCandidate.status === 'Rejected' && activeCandidate.rejectionReason) {
                <div class="p-3 rounded-xl bg-red-950/40 border border-red-800/80 text-xs text-red-200">
                  <span class="font-bold text-red-400">Editorial Rejection Reason:</span>
                  <p class="mt-1">{{ activeCandidate.rejectionReason }}</p>
                </div>
              }

              <!-- Generation Telemetry & Prompt Parameters -->
              <div class="p-4 rounded-xl bg-slate-900/60 border border-slate-800/80 space-y-3">
                <h4 class="text-xs font-bold uppercase tracking-wider text-slate-400">Production Specifications</h4>

                <div>
                  <label class="text-[11px] text-slate-500 block mb-0.5">Workflow / Model</label>
                  <div class="text-xs font-mono text-cyan-300 bg-slate-950 p-2 rounded-lg border border-slate-800">
                    {{ activeCandidate.providerModelOrWorkflow }}
                  </div>
                </div>

                <div>
                  <label class="text-[11px] text-slate-500 block mb-0.5">Storage Key (MinIO Object)</label>
                  <div class="text-[11px] font-mono text-slate-400 bg-slate-950 p-2 rounded-lg border border-slate-800 break-all">
                    {{ activeCandidate.storageKey }}
                  </div>
                </div>

                <div>
                  <label class="text-[11px] text-slate-500 block mb-0.5">Checksum (SHA-256)</label>
                  <div class="text-[11px] font-mono text-slate-400 bg-slate-950 p-2 rounded-lg border border-slate-800 break-all">
                    {{ activeCandidate.checksumSha256 }}
                  </div>
                </div>

                @if (activeCandidate.generationParametersSnapshot) {
                  <div>
                    <label class="text-[11px] text-slate-500 block mb-0.5">Parameter Snapshot</label>
                    <pre class="text-[10px] font-mono text-slate-300 bg-slate-950 p-2.5 rounded-lg border border-slate-800 max-h-32 overflow-y-auto whitespace-pre-wrap">{{ activeCandidate.generationParametersSnapshot }}</pre>
                  </div>
                }
              </div>

              <!-- Rejection Form Drawer/Block (when rejecting) -->
              @if (isRejecting) {
                <div class="p-4 rounded-xl bg-red-950/30 border border-red-800/80 space-y-2.5 animate-fadeIn">
                  <label class="text-xs font-bold text-red-300 block">
                    Mandatory Rejection Feedback <span class="text-red-400">*</span>
                  </label>
                  <textarea [(ngModel)]="rejectionReason"
                            rows="2"
                            placeholder="Specify why this visual output does not satisfy editorial quality or narrative requirements..."
                            class="w-full text-xs p-2.5 rounded-lg bg-slate-950 border border-red-800/80 text-slate-200 focus:outline-none focus:ring-1 focus:ring-red-500"></textarea>
                  <div class="flex justify-end gap-2">
                    <button (click)="isRejecting = false"
                            type="button"
                            class="px-3 py-1.5 rounded-lg bg-slate-800 hover:bg-slate-700 text-slate-300 text-xs font-medium">
                      Cancel
                    </button>
                    <button (click)="submitRejection()"
                            [disabled]="!rejectionReason.trim()"
                            type="button"
                            class="px-3 py-1.5 rounded-lg bg-red-600 hover:bg-red-500 disabled:opacity-50 text-white text-xs font-bold">
                      Confirm Rejection
                    </button>
                  </div>
                </div>
              }
            </div>

            <!-- Footer Actions -->
            <div class="pt-4 border-t border-slate-800 flex items-center justify-between gap-3">
              <div class="flex items-center gap-2">
                @if (activeCandidate.status !== 'Approved') {
                  <button (click)="approveActiveCandidate()"
                          type="button"
                          class="px-4 py-2 rounded-xl bg-emerald-600 hover:bg-emerald-500 text-white text-xs font-bold flex items-center gap-1.5 shadow-lg shadow-emerald-950/40">
                    <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7"/>
                    </svg>
                    Approve Candidate
                  </button>
                } @else if (!activeCandidate.isSelectedForAssembly) {
                  <button (click)="selectActiveCandidate()"
                          type="button"
                          class="px-4 py-2 rounded-xl bg-cyan-600 hover:bg-cyan-500 text-white text-xs font-bold flex items-center gap-1.5 shadow-lg shadow-cyan-950/40">
                    <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11.049 2.927c.3-.921 1.603-.921 1.902 0l1.519 4.674a1 1 0 00.95.69h4.915c.969 0 1.371 1.24.588 1.81l-3.976 2.888a1 1 0 00-.363 1.118l1.518 4.674c.3.922-.755 1.688-1.538 1.118l-3.976-2.888a1 1 0 00-1.176 0l-3.976 2.888c-.783.57-1.838-.197-1.538-1.118l1.518-4.674a1 1 0 00-.363-1.118l-3.976-2.888c-.784-.57-.38-1.81.588-1.81h4.914a1 1 0 00.951-.69l1.519-4.674z"/>
                    </svg>
                    Select for Video Assembly
                  </button>
                }

                @if (!isRejecting && activeCandidate.status !== 'Rejected') {
                  <button (click)="isRejecting = true"
                          type="button"
                          class="px-3 py-2 rounded-xl bg-slate-800 hover:bg-red-950 hover:text-red-300 text-slate-300 border border-slate-700 text-xs font-medium">
                    Reject...
                  </button>
                }
              </div>

              <button (click)="closeModal()"
                      type="button"
                      class="px-4 py-2 rounded-xl bg-slate-800 hover:bg-slate-700 text-slate-300 text-xs font-medium">
                Close
              </button>
            </div>

          </div>

        </div>
      }

    </p-dialog>
  `
})
export class VisualCandidatePreviewModalComponent {
  @Input() visible = false;
  @Input() activeCandidate: GeneratedAssetDto | null = null;
  @Input() allCandidates: GeneratedAssetDto[] = [];

  @Output() visibleChange = new EventEmitter<boolean>();
  @Output() onApprove = new EventEmitter<GeneratedAssetDto>();
  @Output() onSelect = new EventEmitter<GeneratedAssetDto>();
  @Output() onReject = new EventEmitter<{ candidate: GeneratedAssetDto; reason: string }>();

  isRejecting = false;
  rejectionReason = '';

  constructor(private apiService: ApiService) {}

  getStreamUrl(assetId: string): string {
    return this.apiService.getGeneratedAssetStreamUrl(assetId);
  }

  closeModal(): void {
    this.visible = false;
    this.visibleChange.emit(false);
    this.isRejecting = false;
    this.rejectionReason = '';
  }

  approveActiveCandidate(): void {
    if (this.activeCandidate) {
      this.onApprove.emit(this.activeCandidate);
      this.closeModal();
    }
  }

  selectActiveCandidate(): void {
    if (this.activeCandidate) {
      this.onSelect.emit(this.activeCandidate);
      this.closeModal();
    }
  }

  submitRejection(): void {
    if (this.activeCandidate && this.rejectionReason.trim()) {
      this.onReject.emit({
        candidate: this.activeCandidate,
        reason: this.rejectionReason.trim()
      });
      this.closeModal();
    }
  }
}
