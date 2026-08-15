import { Component, EventEmitter, Input, Output, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { ApiService, ContentItemDto, DiscoveryCandidateDto } from '../../core/api.service';

@Component({
  selector: 'app-candidate-preview-drawer',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div *ngIf="isOpen" class="fixed inset-0 z-50 overflow-hidden flex justify-end">
      <!-- Backdrop -->
      <div (click)="close()" class="fixed inset-0 bg-slate-900/40 dark:bg-black/60 backdrop-blur-xs transition-opacity"></div>

      <!-- Slide-over panel (Desktop drawer / Mobile full screen) -->
      <div class="relative w-full max-w-xl bg-[var(--app-card-bg)] border-l border-[var(--app-card-border)] shadow-2xl flex flex-col h-full z-10 animate-slide-in">
        
        <!-- Header -->
        <div class="p-4 sm:p-5 border-b border-[var(--app-card-border)] flex items-start justify-between gap-3 bg-[var(--app-header-bg)]">
          <div>
            <div class="flex items-center gap-2 mb-1.5 flex-wrap">
              <span class="px-2 py-0.5 rounded text-[10px] font-bold uppercase tracking-wider border font-mono"
                    [ngClass]="{
                      'bg-amber-500/15 text-amber-600 dark:text-amber-400 border-amber-500/30': candidate?.status === 'PendingReview',
                      'bg-emerald-500/15 text-emerald-600 dark:text-emerald-400 border-emerald-500/30': candidate?.status === 'Promoted',
                      'bg-slate-500/15 text-slate-500 border-slate-500/30': candidate?.status === 'Dismissed'
                    }">
                {{ candidate?.status }}
              </span>
              <span class="px-2 py-0.5 rounded bg-blue-500/15 text-blue-600 dark:text-blue-400 border border-blue-500/30 text-[10px] font-bold">
                {{ candidate?.channelName || 'Channel' }}
              </span>
              <span class="px-2 py-0.5 rounded bg-indigo-500/15 text-indigo-600 dark:text-indigo-400 border border-indigo-500/30 text-[10px] font-mono">
                {{ candidate?.originType }}
              </span>
            </div>
            <h2 class="text-sm sm:text-base font-bold text-[var(--app-text)] leading-snug">
              {{ candidate?.title }}
            </h2>
          </div>
          <button (click)="close()" class="p-1.5 rounded-lg hover:bg-[var(--app-surface-hover)] text-[var(--app-muted)] hover:text-[var(--app-text)] transition-colors cursor-pointer" aria-label="Close">
            <i class="pi pi-times text-xs"></i>
          </button>
        </div>

        <!-- Body Content -->
        <div class="flex-1 overflow-y-auto p-4 sm:p-5 space-y-4 text-xs">
          
          <!-- Provenance Metadata Card -->
          <div class="p-3 rounded-lg bg-[var(--app-bg)] border border-[var(--app-card-border)] space-y-2">
            <span class="text-[10px] font-bold uppercase tracking-wider text-[var(--app-muted)] block">Proveniencia Editorial</span>
            <div class="grid grid-cols-2 gap-2 text-[11px]">
              <div>
                <span class="text-[var(--app-muted)] block">Fuente:</span>
                <span class="font-medium text-[var(--app-text)]">{{ candidate?.sourceName || candidate?.originType || 'Direct Manual' }}</span>
              </div>
              <div>
                <span class="text-[var(--app-muted)] block">Descubierto:</span>
                <span class="font-mono text-[var(--app-text)]">{{ candidate?.discoveredAtUtc | date:'yyyy-MM-dd HH:mm' }} UTC</span>
              </div>
              <div *ngIf="candidate?.author">
                <span class="text-[var(--app-muted)] block">Autor / Submitter:</span>
                <span class="text-[var(--app-text)]">{{ candidate?.author }}</span>
              </div>
              <div>
                <span class="text-[var(--app-muted)] block">Idioma:</span>
                <span class="uppercase font-mono text-[var(--app-text)]">{{ candidate?.language }}</span>
              </div>
            </div>

            <!-- External Link -->
            <div *ngIf="candidate?.externalUrl" class="pt-2 border-t border-[var(--app-card-border)]">
              <a [href]="candidate?.externalUrl" target="_blank" rel="noopener noreferrer" 
                 class="text-blue-600 dark:text-blue-400 hover:underline flex items-center gap-1.5 break-all font-mono text-[11px]">
                <i class="pi pi-external-link text-[10px]"></i>
                <span>{{ candidate?.externalUrl }}</span>
              </a>
            </div>
          </div>

          <!-- Summary / Note -->
          <div class="space-y-1.5">
            <span class="text-[10px] font-bold uppercase tracking-wider text-[var(--app-muted)] block">Resumen / Contenido</span>
            <div class="p-3.5 rounded-lg bg-[var(--app-card-bg)] border border-[var(--app-card-border)] text-[var(--app-text)] leading-relaxed whitespace-pre-wrap">
              {{ candidate?.summary || candidate?.rawContent || 'Sin resumen disponible.' }}
            </div>
          </div>

          <!-- Editorial Notes if Promoted -->
          <div *ngIf="candidate?.status === 'Promoted'" class="p-3.5 rounded-lg bg-emerald-500/10 border border-emerald-500/30 space-y-3">
            <div class="flex items-center justify-between text-[11px]">
              <span class="font-bold text-emerald-600 dark:text-emerald-400 flex items-center gap-1">
                <i class="pi pi-check-circle"></i>
                <span>Promovido a Pipeline</span>
              </span>
              <span class="text-[10px] text-[var(--app-muted)]">{{ candidate?.promotedAtUtc | date:'yyyy-MM-dd HH:mm' }} por {{ candidate?.promotedByEmail }}</span>
            </div>
            <p *ngIf="candidate?.editorialNotes" class="text-[11px] text-[var(--app-text)] italic">
              "{{ candidate?.editorialNotes }}"
            </p>

            <!-- Downstream Continuation Actions (CF-003) -->
            <div class="pt-2 border-t border-emerald-500/20 space-y-2">
              <span class="text-[10px] font-bold uppercase tracking-wider text-[var(--app-muted)] block">Continuación Downstream</span>
              <div class="grid grid-cols-1 sm:grid-cols-2 gap-2">
                <button (click)="initiateContentItem()" [disabled]="isInitiating"
                        class="px-3 py-2 rounded-lg bg-blue-600 hover:bg-blue-500 text-white font-bold text-xs flex items-center justify-center gap-1.5 cursor-pointer shadow-xs disabled:opacity-50">
                  <i *ngIf="isInitiating" class="pi pi-spin pi-spinner text-xs"></i>
                  <i *ngIf="!isInitiating" class="pi pi-file-plus text-xs"></i>
                  <span>Iniciar Nueva Pieza</span>
                </button>
                <button (click)="openAttachToContentDialog()"
                        class="px-3 py-2 rounded-lg border border-[var(--app-card-border)] hover:bg-[var(--app-surface-hover)] text-[var(--app-text)] font-semibold text-xs flex items-center justify-center gap-1.5 cursor-pointer">
                  <i class="pi pi-link text-xs"></i>
                  <span>Vincular a Pieza...</span>
                </button>
              </div>
            </div>
          </div>

          <!-- Dismissal Reason if Dismissed -->
          <div *ngIf="candidate?.status === 'Dismissed'" class="p-3 rounded-lg bg-slate-500/10 border border-slate-500/30 space-y-1">
            <span class="font-bold text-slate-500 text-[11px]">Descartado</span>
            <p class="text-[11px] text-[var(--app-muted)]">Motivo: {{ candidate?.dismissalReason }}</p>
          </div>

          <!-- Promotion Note Input (when promoting) -->
          <div *ngIf="isPromoting" class="p-3 rounded-lg bg-blue-500/10 border border-blue-500/30 space-y-2">
            <label class="block font-bold text-xs text-blue-600 dark:text-blue-400">Nota Editorial para Producción (Opcional)</label>
            <textarea [(ngModel)]="promotionNotes" rows="2" placeholder="Ángulo propuesto, enfoque para el guión, público objetivo..." 
                      class="w-full text-xs p-2 rounded border border-[var(--app-card-border)] bg-[var(--app-card-bg)] text-[var(--app-text)] focus:outline-hidden focus:border-blue-500"></textarea>
            <div class="flex justify-end gap-2">
              <button (click)="isPromoting = false" class="px-2.5 py-1 rounded bg-[var(--app-bg)] text-[var(--app-muted)] hover:text-[var(--app-text)] text-xs cursor-pointer">
                Cancelar
              </button>
              <button (click)="confirmPromote()" class="px-3 py-1 rounded bg-emerald-600 hover:bg-emerald-700 text-white font-bold text-xs cursor-pointer">
                Confirmar Promoción
              </button>
            </div>
          </div>

          <!-- Dismissal Reason Selector (when dismissing) -->
          <div *ngIf="isDismissing" class="p-3 rounded-lg bg-amber-500/10 border border-amber-500/30 space-y-2">
            <label class="block font-bold text-xs text-amber-600 dark:text-amber-400">Seleccionar Motivo de Descarte</label>
            <div class="grid grid-cols-2 gap-1.5">
              <button *ngFor="let reason of dismissalReasons" (click)="confirmDismiss(reason)"
                      class="px-2 py-1.5 text-left rounded border border-[var(--app-card-border)] bg-[var(--app-card-bg)] hover:bg-amber-500/20 text-[11px] font-medium transition-colors cursor-pointer">
                {{ reason }}
              </button>
            </div>
            <button (click)="isDismissing = false" class="px-2.5 py-1 rounded bg-[var(--app-bg)] text-[var(--app-muted)] hover:text-[var(--app-text)] text-xs cursor-pointer">
              Cancelar
            </button>
          </div>
        </div>

        <!-- Footer Actions Bar -->
        <div class="p-4 border-t border-[var(--app-card-border)] bg-[var(--app-header-bg)] flex items-center justify-between gap-2 shrink-0">
          <div class="flex items-center gap-2">
            <button (click)="onPrev.emit()" class="px-2.5 py-1.5 rounded border border-[var(--app-card-border)] bg-[var(--app-card-bg)] hover:bg-[var(--app-surface-hover)] text-xs transition-colors cursor-pointer" title="Anterior">
              <i class="pi pi-chevron-left text-[10px]"></i>
            </button>
            <button (click)="onNext.emit()" class="px-2.5 py-1.5 rounded border border-[var(--app-card-border)] bg-[var(--app-card-bg)] hover:bg-[var(--app-surface-hover)] text-xs transition-colors cursor-pointer" title="Siguiente">
              <i class="pi pi-chevron-right text-[10px]"></i>
            </button>
          </div>

          <div class="flex items-center gap-2">
            <button *ngIf="candidate?.status === 'PendingReview' && !isPromoting && !isDismissing"
                    (click)="isDismissing = true"
                    class="px-3 py-1.5 rounded border border-red-500/30 text-red-600 dark:text-red-400 hover:bg-red-500/10 text-xs font-semibold transition-colors cursor-pointer">
              <i class="pi pi-ban text-[10px] mr-1"></i> Descartar
            </button>
            <button *ngIf="candidate?.status === 'PendingReview' && !isPromoting && !isDismissing"
                    (click)="isPromoting = true"
                    class="px-3.5 py-1.5 rounded bg-emerald-600 hover:bg-emerald-700 text-white text-xs font-bold transition-all shadow-xs cursor-pointer">
              <i class="pi pi-check text-[10px] mr-1"></i> Promover a Pipeline
            </button>
            <button *ngIf="candidate?.status !== 'PendingReview'"
                    (click)="reopenForReview()"
                    class="px-3 py-1.5 rounded border border-[var(--app-card-border)] bg-[var(--app-card-bg)] hover:bg-[var(--app-surface-hover)] text-xs text-[var(--app-text)] cursor-pointer">
              Reabrir Triage
            </button>
          </div>
        </div>

      </div>
    </div>

    <!-- Attach to Existing Content Item Dialog -->
    <div *ngIf="isAttachDialogOpen" class="fixed inset-0 z-60 overflow-y-auto flex items-center justify-center p-4">
      <div (click)="isAttachDialogOpen = false" class="fixed inset-0 bg-slate-900/60 backdrop-blur-xs"></div>
      <div class="relative w-full max-w-md bg-[var(--app-card-bg)] border border-[var(--app-card-border)] rounded-xl shadow-2xl p-5 space-y-4 z-10 animate-scale-in text-xs">
        <div class="flex items-center justify-between border-b border-[var(--app-card-border)] pb-2">
          <h3 class="font-bold text-sm text-[var(--app-text)] flex items-center gap-1.5">
            <i class="pi pi-link text-blue-500"></i>
            <span>Vincular Evidencia a Pieza Existente</span>
          </h3>
          <button (click)="isAttachDialogOpen = false" class="p-1 text-[var(--app-muted)] hover:text-[var(--app-text)] cursor-pointer">
            <i class="pi pi-times text-xs"></i>
          </button>
        </div>

        <div class="space-y-3">
          <div class="space-y-1">
            <label class="font-bold text-[var(--app-text)]">Seleccionar Pieza de Contenido</label>
            <select [(ngModel)]="targetContentItemId"
                    class="w-full px-3 py-2 rounded-lg bg-[var(--app-bg)] border border-[var(--app-card-border)] text-[var(--app-text)]">
              <option *ngFor="let item of existingContentItems" [value]="item.id">
                {{ item.title }} ({{ item.stage }})
              </option>
            </select>
          </div>

          <div class="space-y-1">
            <label class="font-bold text-[var(--app-text)]">Rol de la Evidencia</label>
            <select [(ngModel)]="attachRole"
                    class="w-full px-3 py-2 rounded-lg bg-[var(--app-bg)] border border-[var(--app-card-border)] text-[var(--app-text)]">
              <option value="SupportingEvidence">SupportingEvidence (Evidencia de apoyo)</option>
              <option value="Counterpoint">Counterpoint (Contrapunto)</option>
              <option value="StyleReference">StyleReference (Referencia de estilo)</option>
            </select>
          </div>
        </div>

        <div class="flex items-center justify-end gap-2 pt-2 border-t border-[var(--app-card-border)]">
          <button (click)="isAttachDialogOpen = false" class="px-3 py-1.5 rounded border border-[var(--app-card-border)] text-[var(--app-muted)] cursor-pointer">
            Cancelar
          </button>
          <button (click)="confirmAttachToContent()" [disabled]="!targetContentItemId || isAttaching"
                  class="px-4 py-1.5 rounded bg-blue-600 hover:bg-blue-500 text-white font-bold cursor-pointer disabled:opacity-50">
            <span>{{ isAttaching ? 'Vinculando...' : 'Confirmar Vinculación' }}</span>
          </button>
        </div>
      </div>
    </div>
  `
})
export class CandidatePreviewDrawerComponent {
  private readonly api = inject(ApiService);
  private readonly router = inject(Router);

  @Input() isOpen = false;
  @Input() candidate: DiscoveryCandidateDto | null = null;
  @Output() onClose = new EventEmitter<void>();
  @Output() onTriage = new EventEmitter<{ id: string; status: 'PendingReview' | 'Promoted' | 'Dismissed'; reason?: string; notes?: string }>();
  @Output() onNext = new EventEmitter<void>();
  @Output() onPrev = new EventEmitter<void>();

  isPromoting = false;
  isDismissing = false;
  promotionNotes = '';
  dismissalReasons = ['Irrelevante / Fuera de nicho', 'Baja calidad / Sin evidencia', 'Duplicado / Ya cubierto', 'Desactualizado'];

  isInitiating = false;
  isAttachDialogOpen = false;
  isAttaching = false;
  existingContentItems: ContentItemDto[] = [];
  targetContentItemId = '';
  attachRole = 'SupportingEvidence';

  close() {
    this.isPromoting = false;
    this.isDismissing = false;
    this.promotionNotes = '';
    this.isAttachDialogOpen = false;
    this.onClose.emit();
  }

  confirmPromote() {
    if (!this.candidate) return;
    this.onTriage.emit({
      id: this.candidate.id,
      status: 'Promoted',
      notes: this.promotionNotes.trim() || undefined
    });
    this.close();
  }

  confirmDismiss(reason: string) {
    if (!this.candidate) return;
    this.onTriage.emit({
      id: this.candidate.id,
      status: 'Dismissed',
      reason
    });
    this.close();
  }

  reopenForReview() {
    if (!this.candidate) return;
    this.onTriage.emit({
      id: this.candidate.id,
      status: 'PendingReview'
    });
    this.close();
  }

  initiateContentItem() {
    if (!this.candidate) return;
    this.isInitiating = true;

    this.api.initiateContentFromCandidate(this.candidate.id, {
      titleOverride: this.candidate.title
    }).subscribe({
      next: (item) => {
        this.isInitiating = false;
        this.close();
        this.router.navigate(['/content/items', item.id]);
      },
      error: () => {
        this.isInitiating = false;
      }
    });
  }

  openAttachToContentDialog() {
    if (!this.candidate) return;
    this.api.getContentItems(this.candidate.channelId).subscribe({
      next: (items) => {
        this.existingContentItems = items;
        if (items.length > 0) {
          this.targetContentItemId = items[0].id;
          this.isAttachDialogOpen = true;
        } else {
          // If no content item exists for this channel, initiate one directly
          this.initiateContentItem();
        }
      }
    });
  }

  confirmAttachToContent() {
    if (!this.candidate || !this.targetContentItemId) return;
    this.isAttaching = true;

    this.api.attachCandidateToContent(this.candidate.id, {
      contentItemId: this.targetContentItemId,
      role: this.attachRole
    }).subscribe({
      next: () => {
        this.isAttaching = false;
        this.isAttachDialogOpen = false;
        this.close();
        this.router.navigate(['/content/items', this.targetContentItemId]);
      },
      error: () => {
        this.isAttaching = false;
      }
    });
  }
}
