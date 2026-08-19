import { Component, Input, Output, EventEmitter, computed, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { SceneType, ScriptSceneDto, VerifiableClaimDto } from '../../core/api.service';

@Component({
  selector: 'app-script-scene-card',
  standalone: true,
  imports: [CommonModule, FormsModule],
  host: { class: 'block w-full' },
  template: `
    <div class="rounded-xl border transition-all duration-200 shadow-xs"
         [ngClass]="{
           'bg-[var(--app-card-bg)] border-[var(--app-card-border)] hover:border-blue-500/40': !isDragActive,
           'border-blue-500 bg-blue-500/5': isDragActive
         }">
      
      <!-- Card Header -->
      <div class="flex items-center justify-between px-4 py-2.5 bg-[var(--app-bg)]/60 border-b border-[var(--app-card-border)] rounded-t-xl gap-2">
        <div class="flex items-center gap-2 flex-wrap">
          <!-- Order Badge -->
          <span class="w-6 h-6 rounded-full bg-[var(--app-card-bg)] border border-[var(--app-card-border)] flex items-center justify-center font-mono font-bold text-xs text-[var(--app-text)]">
            #{{ scene.orderIndex }}
          </span>

          <!-- Scene Type Selector -->
          <select [(ngModel)]="scene.sceneType" (ngModelChange)="onFieldChange()" [disabled]="readOnly"
                  class="px-2 py-0.5 rounded text-[11px] font-bold uppercase tracking-wider font-mono border bg-[var(--app-card-bg)] text-[var(--app-text)] cursor-pointer"
                  [ngClass]="{
                    'border-amber-500/40 text-amber-600 dark:text-amber-400': scene.sceneType === 'Hook',
                    'border-rose-500/40 text-rose-600 dark:text-rose-400': scene.sceneType === 'Problem',
                    'border-indigo-500/40 text-indigo-600 dark:text-indigo-400': scene.sceneType === 'Insight',
                    'border-purple-500/40 text-purple-600 dark:text-purple-400': scene.sceneType === 'Climax',
                    'border-emerald-500/40 text-emerald-600 dark:text-emerald-400': scene.sceneType === 'CallToAction'
                  }">
            <option value="Hook">🎣 Hook (0-3s)</option>
            <option value="Problem">⚠️ Problem</option>
            <option value="Insight">💡 Insight</option>
            <option value="Climax">🔥 Climax</option>
            <option value="CallToAction">📣 Call To Action</option>
          </select>

          <!-- Live Metrics (Words & Duration) -->
          <div class="flex items-center gap-2 font-mono text-[11px] text-[var(--app-muted)]">
            <span class="flex items-center gap-1">
              <i class="pi pi-align-left text-[10px]"></i>
              <strong class="text-[var(--app-text)]">{{ liveWordCount() }}</strong> palabras
            </span>
            <span>•</span>
            <span class="flex items-center gap-1">
              <i class="pi pi-clock text-[10px]"></i>
              <strong class="text-blue-600 dark:text-blue-400">{{ liveDurationSeconds() }}s</strong>
              <span class="text-[9px] text-[var(--app-muted)]">(@{{ pacingWpm }} WPM)</span>
            </span>
          </div>
        </div>

        <!-- Action buttons: Move Up, Move Down, Delete -->
        <div *ngIf="!readOnly" class="flex items-center gap-1">
          <button (click)="moveUp.emit()" [disabled]="isFirst"
                  class="p-1 rounded hover:bg-[var(--app-card-bg)] text-[var(--app-muted)] hover:text-[var(--app-text)] disabled:opacity-30 cursor-pointer"
                  title="Mover arriba">
            <i class="pi pi-chevron-up text-xs"></i>
          </button>
          <button (click)="moveDown.emit()" [disabled]="isLast"
                  class="p-1 rounded hover:bg-[var(--app-card-bg)] text-[var(--app-muted)] hover:text-[var(--app-text)] disabled:opacity-30 cursor-pointer"
                  title="Mover abajo">
            <i class="pi pi-chevron-down text-xs"></i>
          </button>
          <button (click)="delete.emit()"
                  class="p-1 rounded hover:bg-red-500/10 text-[var(--app-muted)] hover:text-red-500 cursor-pointer"
                  title="Eliminar escena">
            <i class="pi pi-trash text-xs"></i>
          </button>
        </div>
      </div>

      <!-- Card Body -->
      <div class="p-4 space-y-3 text-xs">
        
        <!-- Narration Text Area -->
        <div class="space-y-1">
          <div class="flex items-center justify-between">
            <label class="font-bold text-[var(--app-text)] flex items-center gap-1.5">
              <i class="pi pi-microphone text-blue-500"></i>
              <span>Locución / Guión de Voz (Español)</span>
            </label>
            <span class="text-[10px] text-[var(--app-muted)]">Texto exacto a locutar en cámara o TTS</span>
          </div>
          <textarea [(ngModel)]="scene.narrationText" (ngModelChange)="onFieldChange()" [disabled]="readOnly"
                    rows="3"
                    placeholder="Escribe el texto de la locución para esta escena..."
                    class="w-full p-2.5 rounded-lg bg-[var(--app-bg)] border border-[var(--app-card-border)] text-[var(--app-text)] text-xs leading-relaxed focus:outline-none focus:border-blue-500 transition-colors resize-y"></textarea>
        </div>

        <!-- Visual Prompt Input -->
        <div class="space-y-1">
          <div class="flex items-center justify-between">
            <label class="font-bold text-[var(--app-text)] flex items-center gap-1.5">
              <i class="pi pi-video text-purple-500"></i>
              <span>Instrucción Visual / Prompt B-Roll</span>
            </label>
            <span class="text-[10px] text-[var(--app-muted)]">Indicación para generador visual o edición</span>
          </div>
          <input type="text" [(ngModel)]="scene.visualPrompt" (ngModelChange)="onFieldChange()" [disabled]="readOnly"
                 placeholder="Ej. Primer plano directo a cámara con texto animado '2026'..."
                 class="w-full px-2.5 py-1.5 rounded-lg bg-[var(--app-bg)] border border-[var(--app-card-border)] text-[var(--app-text)] text-xs focus:outline-none focus:border-purple-500 transition-colors" />
        </div>

        <!-- Factual Claim References Section -->
        <div class="pt-2 border-t border-[var(--app-card-border)] space-y-2">
          <div class="flex items-center justify-between">
            <div class="flex items-center gap-1.5">
              <i class="pi pi-shield text-emerald-600 dark:text-emerald-400 text-[11px]"></i>
              <span class="font-bold text-[11px] text-[var(--app-text)]">Trazabilidad Factual</span>
              <span class="px-1.5 py-0.2 rounded bg-emerald-500/15 text-emerald-600 dark:text-emerald-400 font-mono text-[9px] font-bold">
                {{ scene.evidenceReferences.length }} referencia(s)
              </span>
            </div>

            <!-- Add reference button if claims exist -->
            <div *ngIf="!readOnly && availableClaims.length > 0" class="relative">
              <button (click)="isClaimsDropdownOpen.set(!isClaimsDropdownOpen())"
                      class="px-2 py-0.5 rounded bg-[var(--app-bg)] hover:bg-[var(--app-card-bg)] border border-[var(--app-card-border)] text-[var(--app-text)] font-semibold text-[10px] flex items-center gap-1 cursor-pointer">
                <i class="pi pi-plus text-[8px]"></i>
                <span>Vincular Claim</span>
              </button>

              <!-- Dropdown Menu -->
              <div *ngIf="isClaimsDropdownOpen()" class="absolute right-0 bottom-full mb-1 w-72 max-h-48 overflow-y-auto bg-[var(--app-card-bg)] border border-[var(--app-card-border)] rounded-lg shadow-lg z-20 p-1 space-y-1 text-xs">
                <div class="px-2 py-1 text-[10px] font-bold text-[var(--app-muted)] uppercase border-b border-[var(--app-card-border)]">
                  Afirmaciones del TruthSource:
                </div>
                <div *ngFor="let c of availableClaims" (click)="addClaimReference(c)"
                     class="p-1.5 rounded hover:bg-blue-500/10 cursor-pointer transition-colors text-[11px] text-[var(--app-text)]">
                  <p class="font-medium line-clamp-2">{{ c.claim }}</p>
                  <span class="text-[9px] text-[var(--app-muted)]">Cita: {{ c.sourceCitation }}</span>
                </div>
              </div>
            </div>
          </div>

          <!-- Existing References Chips -->
          <div *ngIf="scene.evidenceReferences.length > 0" class="space-y-1.5">
            <div *ngFor="let ref of scene.evidenceReferences; let refIdx = index" 
                 class="p-2 rounded-lg bg-emerald-500/5 border border-emerald-500/20 flex items-start justify-between gap-2">
              <div class="space-y-0.5 flex-1">
                <p class="text-[11px] text-[var(--app-text)] font-medium leading-snug">
                  "{{ ref.claimStatement }}"
                </p>
                <div *ngIf="!readOnly" class="flex items-center gap-1 text-[10px]">
                  <input type="text" [(ngModel)]="ref.editorialNote" (ngModelChange)="onFieldChange()"
                         placeholder="Nota editorial opcional (ej. Gancho inicial respaldado)..."
                         class="w-full bg-transparent border-b border-dashed border-[var(--app-card-border)] text-[var(--app-muted)] focus:text-[var(--app-text)] focus:outline-none text-[10px] py-0.5" />
                </div>
                <div *ngIf="readOnly && ref.editorialNote" class="text-[10px] text-[var(--app-muted)] italic">
                  Nota: {{ ref.editorialNote }}
                </div>
              </div>

              <button *ngIf="!readOnly" (click)="removeClaimReference(refIdx)"
                      class="p-0.5 rounded text-[var(--app-muted)] hover:text-red-500 cursor-pointer"
                      title="Eliminar referencia">
                <i class="pi pi-times text-[10px]"></i>
              </button>
            </div>
          </div>

          <!-- No references note -->
          <div *ngIf="scene.evidenceReferences.length === 0" class="text-[10px] text-[var(--app-muted)] italic">
            Sin afirmaciones factuales vinculadas a esta escena.
          </div>
        </div>

      </div>

    </div>
  `
})
export class ScriptSceneCardComponent {
  @Input({ required: true }) scene!: ScriptSceneDto;
  @Input() pacingWpm: number = 140;
  @Input() availableClaims: VerifiableClaimDto[] = [];
  @Input() readOnly: boolean = false;
  @Input() isFirst: boolean = false;
  @Input() isLast: boolean = false;
  @Input() isDragActive: boolean = false;

  @Output() changed = new EventEmitter<void>();
  @Output() moveUp = new EventEmitter<void>();
  @Output() moveDown = new EventEmitter<void>();
  @Output() delete = new EventEmitter<void>();

  readonly isClaimsDropdownOpen = signal<boolean>(false);

  liveWordCount = computed(() => {
    const text = this.scene?.narrationText?.trim();
    if (!text) return 0;
    return text.split(/\s+/).filter(Boolean).length;
  });

  liveDurationSeconds = computed(() => {
    const words = this.liveWordCount();
    const wpm = this.pacingWpm > 0 ? this.pacingWpm : 140;
    return Math.round((words / (wpm / 60.0)) * 10) / 10;
  });

  onFieldChange() {
    this.scene.wordCount = this.liveWordCount();
    this.scene.estimatedDurationSeconds = this.liveDurationSeconds();
    this.changed.emit();
  }

  addClaimReference(claim: VerifiableClaimDto) {
    this.scene.evidenceReferences.push({
      id: '',
      scriptSceneId: this.scene.id,
      truthSourceClaimId: claim.evidenceId,
      claimStatement: claim.claim,
      editorialNote: ''
    });
    this.isClaimsDropdownOpen.set(false);
    this.onFieldChange();
  }

  removeClaimReference(index: number) {
    this.scene.evidenceReferences.splice(index, 1);
    this.onFieldChange();
  }
}
