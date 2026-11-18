import { Component, EventEmitter, Input, Output, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService, GeneratedAssetDto } from '../../core/api.service';
import { VoiceAudioPlayerComponent } from './voice-audio-player.component';

@Component({
  selector: 'app-voice-candidate-card',
  standalone: true,
  imports: [CommonModule, FormsModule, VoiceAudioPlayerComponent],
  template: `
    <div class="rounded-2xl border transition-all overflow-hidden flex flex-col justify-between"
         [ngClass]="{
           'bg-violet-950/20 border-violet-500/50 shadow-md ring-1 ring-violet-500/30': candidate.isSelectedForAssembly,
           'bg-slate-900/70 border-emerald-500/30': candidate.status === 'Approved' && !candidate.isSelectedForAssembly,
           'bg-slate-900/50 border-red-500/30 opacity-75': candidate.status === 'Rejected',
           'bg-slate-900/60 border-slate-800 hover:border-slate-700': candidate.status === 'PendingReview' && !candidate.isSelectedForAssembly
         }">

      <!-- Card Header: Variant, Timing, Status Tags -->
      <div class="p-4 border-b border-slate-800/80 flex items-center justify-between gap-2 flex-wrap">
        <div class="flex items-center gap-2">
          <!-- Variant Badge -->
          <span class="px-2 py-0.5 rounded text-xs font-mono font-bold"
                [ngClass]="{
                  'bg-violet-500/20 text-violet-300 border border-violet-500/40': candidate.isSelectedForAssembly,
                  'bg-slate-800 text-slate-300 border border-slate-700': !candidate.isSelectedForAssembly
                }">
            Toma #{{ candidate.variantIndex }}
          </span>

          <!-- Selected for Assembly Chip -->
          @if (candidate.isSelectedForAssembly) {
            <span class="px-2 py-0.5 rounded text-[11px] font-bold bg-violet-600 text-white flex items-center gap-1 shadow-xs">
              <i class="pi pi-check text-[10px]"></i>
              <span>Voz Maestra Activa</span>
            </span>
          }

          <!-- Review Status Badge -->
          <span class="px-2 py-0.5 rounded text-[10px] font-bold uppercase tracking-wider font-mono border"
                [ngClass]="{
                  'bg-emerald-500/15 text-emerald-400 border-emerald-500/30': candidate.status === 'Approved',
                  'bg-red-500/15 text-red-400 border-red-500/30': candidate.status === 'Rejected',
                  'bg-amber-500/15 text-amber-400 border-amber-500/30': candidate.status === 'PendingReview'
                }">
            {{ candidate.status === 'Approved' ? 'Aprobada' : candidate.status === 'Rejected' ? 'Rechazada' : 'Pendiente QA' }}
          </span>
        </div>

        <!-- Measured Duration & Timing Comparison Badge -->
        <div class="flex items-center gap-2">
          <span class="px-2 py-0.5 rounded bg-slate-950 border border-slate-800 font-mono text-xs font-bold text-slate-200"
                title="Duración medida exacta de la locución">
            <i class="pi pi-clock text-[10px] text-violet-400 mr-1"></i>
            {{ candidate.durationSeconds ? candidate.durationSeconds + 's' : 'N/A' }}
          </span>

          @if (estimatedDurationSeconds && candidate.durationSeconds) {
            <span class="px-1.5 py-0.5 rounded text-[10px] font-mono font-bold"
                  [ngClass]="{
                    'bg-emerald-950 text-emerald-300 border border-emerald-800/40': getTimingDeviation().status === 'Normal',
                    'bg-amber-950 text-amber-300 border border-amber-800/40': getTimingDeviation().status === 'Notice',
                    'bg-red-950 text-red-300 border border-red-800/40': getTimingDeviation().status === 'Warning'
                  }"
                  [title]="'Desviación contra guión estimado (' + estimatedDurationSeconds + 's)'">
              {{ getTimingDeviation().text }}
            </span>
          }
        </div>
      </div>

      <!-- Card Body: Player & Metadata -->
      <div class="p-4 space-y-3">
        <!-- Audio Player Component -->
        <app-voice-audio-player
          [audioUrl]="getAudioStreamUrl(candidate.id)"
          [initialDurationSeconds]="candidate.durationSeconds">
        </app-voice-audio-player>

        <!-- Technical Metadata Chips -->
        <div class="flex items-center gap-1.5 flex-wrap">
          <span class="px-2 py-0.5 rounded bg-slate-800 text-[10px] font-mono text-slate-300">
            WAV 16-bit
          </span>
          <span class="px-2 py-0.5 rounded bg-slate-800 text-[10px] font-mono text-slate-300">
            Mono
          </span>
          <span class="px-2 py-0.5 rounded bg-slate-800 text-[10px] font-mono text-slate-300">
            24 kHz
          </span>
          @if (candidate.fileSizeBytes) {
            <span class="px-2 py-0.5 rounded bg-slate-800 text-[10px] font-mono text-slate-300">
              {{ (candidate.fileSizeBytes / 1024).toFixed(1) }} KB
            </span>
          }
          <span class="px-2 py-0.5 rounded bg-slate-800 text-[10px] font-mono text-cyan-400">
            {{ candidate.provider }}
          </span>
        </div>

        <!-- Rejection Reason if present -->
        @if (candidate.status === 'Rejected' && candidate.rejectionReason) {
          <div class="p-2.5 rounded-lg bg-red-950/40 border border-red-500/30 text-xs text-red-300 space-y-1">
            <div class="flex items-center gap-1 font-bold text-[11px] text-red-400">
              <i class="pi pi-times-circle text-[10px]"></i>
              <span>Motivo del Rechazo:</span>
            </div>
            <p class="text-[11px] leading-relaxed">{{ candidate.rejectionReason }}</p>
          </div>
        }

        <!-- Technical Parameters Inspection Drawer / Collapsible -->
        <div class="border-t border-slate-800/70 pt-2.5">
          <button (click)="isParamsExpanded.set(!isParamsExpanded())"
                  type="button"
                  class="text-[11px] text-slate-400 hover:text-slate-200 flex items-center gap-1 cursor-pointer transition-colors">
            <i class="pi text-[9px]" [ngClass]="isParamsExpanded() ? 'pi-chevron-down' : 'pi-chevron-right'"></i>
            <span>Parámetros de Síntesis e Inmutabilidad</span>
          </button>

          @if (isParamsExpanded()) {
            <div class="mt-2 p-2.5 rounded-lg bg-slate-950/90 border border-slate-800/80 font-mono text-[11px] space-y-1 text-slate-300">
              <div class="flex justify-between">
                <span class="text-slate-500">Proveedor:</span>
                <span class="text-slate-200 font-bold">{{ candidate.provider }} ({{ candidate.providerModelOrWorkflow }})</span>
              </div>
              <div class="flex justify-between">
                <span class="text-slate-500">Formato:</span>
                <span class="text-slate-200">WAV PCM 16-bit Mono</span>
              </div>
              <div class="flex justify-between">
                <span class="text-slate-500">Storage MinIO:</span>
                <span class="text-slate-400 truncate max-w-[220px]" [title]="candidate.storageKey">{{ candidate.storageKey }}</span>
              </div>
              <div class="flex justify-between">
                <span class="text-slate-500">Checksum SHA-256:</span>
                <span class="text-slate-400 truncate max-w-[140px]" [title]="candidate.checksumSha256">{{ candidate.checksumSha256 }}</span>
              </div>
              @if (candidate.reviewedByEmail) {
                <div class="flex justify-between pt-1 border-t border-slate-800 text-[10px]">
                  <span class="text-slate-500">Revisado por:</span>
                  <span class="text-slate-400">{{ candidate.reviewedByEmail }} ({{ candidate.reviewedAtUtc | date:'MM/dd HH:mm' }})</span>
                </div>
              }
            </div>
          }
        </div>
      </div>

      <!-- Card Footer: Editorial QA Actions -->
      <div class="p-3.5 bg-slate-950/60 border-t border-slate-800/80 flex items-center justify-between gap-2 flex-wrap">
        
        <!-- Rejection Dialog Inline Trigger -->
        @if (isRejecting()) {
          <div class="w-full space-y-2 py-1">
            <input type="text"
                   [(ngModel)]="rejectionInput"
                   placeholder="Especifique el motivo del rechazo (ej. dicción defectuosa, tono plano)..."
                   class="w-full px-3 py-1.5 rounded-lg bg-slate-900 border border-red-500/40 text-xs text-slate-100 placeholder-slate-500 focus:outline-none focus:border-red-400">
            <div class="flex items-center justify-end gap-2">
              <button (click)="cancelReject()"
                      type="button"
                      class="px-2.5 py-1 rounded-md text-xs text-slate-400 hover:text-slate-200 cursor-pointer">
                Cancelar
              </button>
              <button (click)="confirmReject()"
                      type="button"
                      [disabled]="!rejectionInput.trim()"
                      class="px-3 py-1 rounded-md bg-red-600 hover:bg-red-500 disabled:opacity-50 text-white text-xs font-bold cursor-pointer transition-colors shadow-xs">
                Confirmar Rechazo
              </button>
            </div>
          </div>
        } @else {
          <!-- Left: Reject Button -->
          <div>
            @if (candidate.status !== 'Rejected') {
              <button (click)="startReject()"
                      type="button"
                      class="px-2.5 py-1.5 rounded-lg bg-slate-900 hover:bg-red-950/40 border border-slate-800 hover:border-red-500/40 text-slate-400 hover:text-red-300 text-xs font-medium flex items-center gap-1.5 cursor-pointer transition-colors">
                <i class="pi pi-times text-[10px]"></i>
                <span>Rechazar</span>
              </button>
            }
          </div>

          <!-- Right: Approve / Select for Assembly -->
          <div class="flex items-center gap-2">
            @if (candidate.status !== 'Approved') {
              <button (click)="onApprove.emit(candidate.id)"
                      type="button"
                      class="px-3 py-1.5 rounded-lg bg-emerald-600 hover:bg-emerald-500 text-white text-xs font-bold flex items-center gap-1.5 cursor-pointer transition-colors shadow-xs">
                <i class="pi pi-check text-[10px]"></i>
                <span>Aprobar</span>
              </button>
            }

            @if (candidate.status === 'Approved' && !candidate.isSelectedForAssembly) {
              <button (click)="onSelectForAssembly.emit(candidate.id)"
                      type="button"
                      class="px-3 py-1.5 rounded-lg bg-violet-600 hover:bg-violet-500 text-white text-xs font-bold flex items-center gap-1.5 cursor-pointer transition-colors shadow-xs">
                <i class="pi pi-star text-[10px]"></i>
                <span>Elegir para Montaje</span>
              </button>
            }
          </div>
        }

      </div>

    </div>
  `
})
export class VoiceCandidateCardComponent {
  @Input({ required: true }) candidate!: GeneratedAssetDto;
  @Input() estimatedDurationSeconds?: number;
  @Input() timingTolerancePolicy?: { noticeThresholdSeconds: number; warningThresholdSeconds: number };

  @Output() onApprove = new EventEmitter<string>();
  @Output() onReject = new EventEmitter<{ id: string; reason: string }>();
  @Output() onSelectForAssembly = new EventEmitter<string>();

  readonly isParamsExpanded = signal<boolean>(false);
  readonly isRejecting = signal<boolean>(false);
  rejectionInput: string = '';

  constructor(private apiService: ApiService) {}

  getAudioStreamUrl(assetId: string): string {
    return this.apiService.getGeneratedAssetAudioStreamUrl(assetId);
  }

  getTimingDeviation(): { text: string; status: 'Normal' | 'Notice' | 'Warning' } {
    if (!this.estimatedDurationSeconds || !this.candidate.durationSeconds) {
      return { text: '0.0s', status: 'Normal' };
    }
    const diff = Math.round((this.candidate.durationSeconds - this.estimatedDurationSeconds) * 10) / 10;
    const sign = diff >= 0 ? `+${diff}s` : `${diff}s`;
    const abs = Math.abs(diff);

    const noticeLimit = this.timingTolerancePolicy?.noticeThresholdSeconds ?? 2.0;
    const warningLimit = this.timingTolerancePolicy?.warningThresholdSeconds ?? 4.0;

    if (abs <= noticeLimit) return { text: sign, status: 'Normal' };
    if (abs <= warningLimit) return { text: sign, status: 'Notice' };
    return { text: sign, status: 'Warning' };
  }

  startReject(): void {
    this.rejectionInput = '';
    this.isRejecting.set(true);
  }

  cancelReject(): void {
    this.isRejecting.set(false);
    this.rejectionInput = '';
  }

  confirmReject(): void {
    if (!this.rejectionInput.trim()) return;
    this.onReject.emit({
      id: this.candidate.id,
      reason: this.rejectionInput.trim()
    });
    this.isRejecting.set(false);
  }
}
