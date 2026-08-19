import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ApiService, GeneratedAssetDto } from '../../core/api.service';

@Component({
  selector: 'app-visual-candidate-card',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="group relative flex flex-col rounded-xl border bg-slate-900/80 overflow-hidden transition-all duration-200 hover:shadow-lg hover:shadow-cyan-950/30"
         [ngClass]="{
           'border-emerald-500 ring-2 ring-emerald-500/30': candidate.isSelectedForAssembly,
           'border-slate-700/80 hover:border-slate-600': !candidate.isSelectedForAssembly && candidate.status !== 'Rejected',
           'border-red-900/50 bg-red-950/10 opacity-75': candidate.status === 'Rejected'
         }">

      <!-- 9:16 Aspect Ratio Visual Container -->
      <div class="relative w-full aspect-[9/16] bg-slate-950 flex items-center justify-center overflow-hidden cursor-pointer"
           (click)="onInspect.emit(candidate)">

        <!-- Media Preview -->
        <img [src]="getThumbnailUrl(candidate.id)"
             [alt]="'Candidate variant ' + candidate.variantIndex"
             class="w-full h-full object-cover transition-transform duration-300 group-hover:scale-105"
             loading="lazy" />

        <!-- Overlay Gradient -->
        <div class="absolute inset-0 bg-gradient-to-t from-slate-950/90 via-transparent to-black/40 opacity-80 group-hover:opacity-60 transition-opacity"></div>

        <!-- Variant Number Badge -->
        <div class="absolute top-2 left-2 flex items-center gap-1.5 px-2 py-0.5 rounded-md bg-slate-900/90 backdrop-blur-md border border-slate-700 text-xs font-semibold text-slate-200 shadow">
          <span class="text-cyan-400">#{{ candidate.variantIndex }}</span>
          <span class="text-slate-400 text-[10px] uppercase font-mono">{{ candidate.provider }}</span>
        </div>

        <!-- Status & Selection Badge -->
        <div class="absolute top-2 right-2 flex flex-col items-end gap-1">
          @if (candidate.isSelectedForAssembly) {
            <span class="inline-flex items-center gap-1 px-2 py-0.5 rounded-md bg-emerald-500/90 text-slate-950 text-xs font-bold shadow-md animate-pulse">
              <svg class="w-3.5 h-3.5 fill-current" viewBox="0 0 20 20">
                <path d="M9.049 2.927c.3-.921 1.603-.921 1.902 0l1.07 3.292a1 1 0 00.95.69h3.462c.969 0 1.371 1.24.588 1.81l-2.8 2.034a1 1 0 00-.364 1.118l1.07 3.292c.3.921-.755 1.688-1.54 1.118l-2.8-2.034a1 1 0 00-1.175 0l-2.8 2.034c-.784.57-1.838-.197-1.539-1.118l1.07-3.292a1 1 0 00-.364-1.118L2.98 8.72c-.783-.57-.38-1.81.588-1.81h3.461a1 1 0 00.951-.69l1.07-3.292z"/>
              </svg>
              Selected
            </span>
          } @else if (candidate.status === 'Approved') {
            <span class="inline-flex items-center px-1.5 py-0.5 rounded bg-emerald-950/80 border border-emerald-500/40 text-emerald-300 text-[11px] font-medium">
              Approved
            </span>
          } @else if (candidate.status === 'PendingReview') {
            <span class="inline-flex items-center px-1.5 py-0.5 rounded bg-amber-950/80 border border-amber-500/40 text-amber-300 text-[11px] font-medium">
              Pending Review
            </span>
          } @else if (candidate.status === 'Rejected') {
            <span class="inline-flex items-center px-1.5 py-0.5 rounded bg-red-950/80 border border-red-500/40 text-red-300 text-[11px] font-medium">
              Rejected
            </span>
          }
        </div>

        <!-- Quick Zoom Icon on Hover -->
        <div class="absolute inset-0 flex items-center justify-center opacity-0 group-hover:opacity-100 transition-opacity pointer-events-none">
          <div class="p-2 rounded-full bg-slate-900/80 backdrop-blur-md border border-slate-700 text-cyan-300 shadow-xl">
            <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0zM10 7v6m3-3H7"/>
            </svg>
          </div>
        </div>

        <!-- Rejection Reason Tooltip/Badge -->
        @if (candidate.status === 'Rejected' && candidate.rejectionReason) {
          <div class="absolute bottom-2 inset-x-2 p-1.5 rounded bg-red-950/90 border border-red-800 text-[11px] text-red-200 line-clamp-2">
            <span class="font-semibold text-red-400">Rejected:</span> {{ candidate.rejectionReason }}
          </div>
        }
      </div>

      <!-- Card Action Footer -->
      <div class="p-2 bg-slate-950/60 border-t border-slate-800/80 flex flex-col gap-1.5">
        <div class="flex items-center justify-between text-[11px] text-slate-400">
          <span>{{ candidate.width }}x{{ candidate.height }}</span>
          <span>{{ (candidate.fileSizeBytes / 1024).toFixed(0) }} KB</span>
        </div>

        <div class="grid grid-cols-2 gap-1 mt-0.5">
          @if (candidate.status !== 'Approved') {
            <button (click)="onApprove.emit(candidate)"
                    [disabled]="disabled"
                    type="button"
                    class="px-2 py-1 rounded bg-emerald-600 hover:bg-emerald-500 text-white text-xs font-medium transition-colors disabled:opacity-50 flex items-center justify-center gap-1 shadow-sm">
              <svg class="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7"/>
              </svg>
              Approve
            </button>
          } @else if (!candidate.isSelectedForAssembly) {
            <button (click)="onSelect.emit(candidate)"
                    [disabled]="disabled"
                    type="button"
                    class="px-2 py-1 rounded bg-cyan-600 hover:bg-cyan-500 text-white text-xs font-medium transition-colors disabled:opacity-50 flex items-center justify-center gap-1 shadow-sm">
              <svg class="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11.049 2.927c.3-.921 1.603-.921 1.902 0l1.519 4.674a1 1 0 00.95.69h4.915c.969 0 1.371 1.24.588 1.81l-3.976 2.888a1 1 0 00-.363 1.118l1.518 4.674c.3.922-.755 1.688-1.538 1.118l-3.976-2.888a1 1 0 00-1.176 0l-3.976 2.888c-.783.57-1.838-.197-1.538-1.118l1.518-4.674a1 1 0 00-.363-1.118l-3.976-2.888c-.784-.57-.38-1.81.588-1.81h4.914a1 1 0 00.951-.69l1.519-4.674z"/>
              </svg>
              Select
            </button>
          } @else {
            <div class="px-2 py-1 rounded bg-emerald-950/60 border border-emerald-600/40 text-emerald-300 text-xs font-medium text-center">
              Active Pick
            </div>
          }

          @if (candidate.status !== 'Rejected') {
            <button (click)="onReject.emit(candidate)"
                    [disabled]="disabled"
                    type="button"
                    class="px-2 py-1 rounded bg-slate-800 hover:bg-red-950 hover:text-red-300 hover:border-red-800 text-slate-300 border border-slate-700 text-xs font-medium transition-colors disabled:opacity-50 flex items-center justify-center gap-1">
              <svg class="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"/>
              </svg>
              Reject
            </button>
          } @else {
            <button (click)="onInspect.emit(candidate)"
                    type="button"
                    class="px-2 py-1 rounded bg-slate-800 hover:bg-slate-700 text-slate-300 border border-slate-700 text-xs font-medium transition-colors">
              Details
            </button>
          }
        </div>
      </div>
    </div>
  `
})
export class VisualCandidateCardComponent {
  @Input({ required: true }) candidate!: GeneratedAssetDto;
  @Input() disabled = false;

  @Output() onApprove = new EventEmitter<GeneratedAssetDto>();
  @Output() onReject = new EventEmitter<GeneratedAssetDto>();
  @Output() onSelect = new EventEmitter<GeneratedAssetDto>();
  @Output() onInspect = new EventEmitter<GeneratedAssetDto>();

  constructor(private apiService: ApiService) {}

  getThumbnailUrl(assetId: string): string {
    return this.apiService.getGeneratedAssetThumbnailUrl(assetId);
  }
}
