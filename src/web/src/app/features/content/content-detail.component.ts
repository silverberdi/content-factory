import { Component, OnInit, inject, signal, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { ApiService, ContentIdeaDto, ContentItemDetailDto, ContentItemEvidenceDto, TruthSourceDto } from '../../core/api.service';
import { AttachEvidenceModalComponent } from './attach-evidence-modal.component';
import { PageHeaderComponent } from '../../shared/layout/page-header.component';

@Component({
  selector: 'app-content-detail',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule, AttachEvidenceModalComponent, PageHeaderComponent],
  host: { class: 'block w-full' },
  template: `
    <!-- Loading State -->
    <div *ngIf="isLoading()" class="p-12 text-center text-xs text-[var(--app-muted)] space-y-2">
      <i class="pi pi-spin pi-spinner text-2xl text-blue-500 block mx-auto"></i>
      <span class="font-medium text-sm text-[var(--app-text)]">Cargando detalle de la pieza...</span>
    </div>

    <!-- Error State -->
    <div *ngIf="errorMessage() && !isLoading()" class="p-6 rounded-xl bg-red-500/10 border border-red-500/30 text-center space-y-3 max-w-lg mx-auto my-8">
      <i class="pi pi-exclamation-triangle text-2xl text-red-500 block"></i>
      <p class="font-bold text-sm text-[var(--app-text)]">{{ errorMessage() }}</p>
      <div class="flex items-center justify-center gap-2 pt-2">
        <button (click)="retryLoad()" class="cf-btn-primary">
          <i class="pi pi-refresh mr-1 text-[10px]"></i> Reintentar
        </button>
        <a [routerLink]="['/content/items']" class="cf-btn-secondary">
          Volver al Workspace
        </a>
      </div>
    </div>

    <!-- Loaded Content Item Detail -->
    <div *ngIf="!isLoading() && item()" class="cf-page-container space-y-4 text-xs">
      
      <!-- Canonical Page Header -->
      <app-page-header 
        [title]="item()?.title || 'Detalle de Pieza'"
        [subtitle]="'Slug: /' + item()?.slug + ' • Creado por: ' + item()?.createdByEmail"
        backLink="/content/items"
        backLabel="Volver al Workspace">
        
        <div meta class="flex items-center gap-2 flex-wrap">
          <span class="px-2 py-0.5 rounded bg-blue-500/15 text-blue-600 dark:text-blue-400 border border-blue-500/30 text-[10px] font-bold">
            {{ item()?.channelName || 'Canal' }}
          </span>
          <span class="px-2 py-0.5 rounded text-[10px] font-bold uppercase tracking-wider border font-mono"
                [ngClass]="{
                  'bg-amber-500/15 text-amber-600 dark:text-amber-400 border-amber-500/30': item()?.stage === 'DraftingEvidence',
                  'bg-emerald-500/15 text-emerald-600 dark:text-emerald-400 border-emerald-500/30': item()?.stage === 'TruthSourceApproved',
                  'bg-purple-500/15 text-purple-600 dark:text-purple-400 border-purple-500/30': item()?.stage === 'IdeaDrafting',
                  'bg-slate-500/15 text-slate-500 border-slate-500/30': item()?.stage === 'Published'
                }">
            {{ item()?.stage }}
          </span>
          <span class="text-[10px] font-mono text-[var(--app-muted)]">
            v{{ item()?.version }} • Actualizado {{ item()?.updatedAtUtc | date:'yyyy-MM-dd HH:mm' }}
          </span>
        </div>

        <div actions class="flex items-center gap-2 flex-wrap">
          <a [routerLink]="['/content/items', item()?.id, 'script']"
             class="cf-btn-primary">
            <i class="pi pi-file-edit"></i>
            <span>Script Studio</span>
          </a>

          <a [routerLink]="['/content/items', item()?.id, 'ideas']"
             class="cf-btn-secondary">
            <i class="pi pi-lightbulb text-purple-500"></i>
            <span>Matriz de Ideas</span>
          </a>

          <a [routerLink]="['/content/items', item()?.id, 'truth-source']"
             class="cf-btn-secondary">
            <i class="pi pi-check-square text-indigo-500"></i>
            <span>TruthSource Studio</span>
          </a>
        </div>

      </app-page-header>

      <!-- Main Layout: Evidences (Left / 2 cols) & TruthSource/Ideas Summary (Right / 1 col) -->
      <div class="grid grid-cols-1 lg:grid-cols-3 gap-4 text-xs">
        
        <!-- Left: Evidence Bundle Provenance Panel (2 cols) -->
        <div class="lg:col-span-2 space-y-4">
          <div class="bg-[var(--app-card-bg)] border border-[var(--app-card-border)] rounded-xl p-4 sm:p-5 shadow-xs space-y-4">
            
            <div class="flex items-center justify-between border-b border-[var(--app-card-border)] pb-3">
              <div class="flex items-center gap-2">
                <i class="pi pi-paperclip text-blue-600 dark:text-blue-400"></i>
                <h2 class="text-sm font-bold text-[var(--app-text)]">Bundle de Evidencias Capturadas</h2>
                <span class="px-1.5 py-0.5 rounded bg-[var(--app-bg)] border border-[var(--app-card-border)] font-mono text-[10px] font-bold">
                  {{ item()?.evidences?.length || 0 }}
                </span>
              </div>
              <button (click)="isAttachModalOpen.set(true)" 
                      class="px-2.5 py-1 rounded-md bg-blue-600 hover:bg-blue-500 text-white font-semibold text-[11px] flex items-center gap-1 cursor-pointer">
                <i class="pi pi-plus text-[10px]"></i>
                <span>Adjuntar Evidencia</span>
              </button>
            </div>

            <!-- Empty Evidences State -->
            <div *ngIf="(item()?.evidences?.length || 0) === 0" class="p-6 text-center text-[var(--app-muted)] space-y-2">
              <p class="font-medium text-sm text-[var(--app-text)]">No hay evidencias adjuntas a esta pieza.</p>
              <p class="text-[11px]">Agrega una fuente externa o nota contextual para generar el TruthSource.</p>
            </div>

            <!-- Evidences Cards List -->
            <div *ngIf="(item()?.evidences?.length || 0) > 0" class="space-y-3">
              <div *ngFor="let ev of item()?.evidences" 
                   class="p-3.5 rounded-lg border transition-colors space-y-2.5"
                   [ngClass]="{
                     'bg-[var(--app-bg)] border-[var(--app-card-border)]': ev.status === 'Captured',
                     'bg-red-500/5 border-red-500/30': ev.status === 'CaptureFailed',
                     'bg-slate-500/5 border-slate-500/20 opacity-60': ev.status === 'Excluded'
                   }">
                
                <!-- Card Header -->
                <div class="flex items-start justify-between gap-2">
                  <div>
                    <div class="flex items-center gap-1.5 mb-1 flex-wrap">
                      <span class="px-2 py-0.5 rounded text-[10px] font-bold uppercase tracking-wider font-mono border"
                            [ngClass]="{
                              'bg-emerald-500/15 text-emerald-600 dark:text-emerald-400 border-emerald-500/30': ev.status === 'Captured',
                              'bg-red-500/15 text-red-600 dark:text-red-400 border-red-500/30': ev.status === 'CaptureFailed',
                              'bg-slate-500/15 text-slate-500 border-slate-500/30': ev.status === 'Excluded'
                            }">
                        {{ ev.status }}
                      </span>
                      <span class="px-2 py-0.5 rounded bg-indigo-500/15 text-indigo-600 dark:text-indigo-400 border border-indigo-500/30 text-[10px] font-mono">
                        {{ ev.role }}
                      </span>
                      <span *ngIf="ev.capturedAtUtc" class="text-[10px] font-mono text-[var(--app-muted)]">
                        {{ ev.capturedAtUtc | date:'yyyy-MM-dd HH:mm' }} UTC
                      </span>
                    </div>
                    <h3 class="font-bold text-[var(--app-text)] text-xs">
                      {{ ev.title }}
                    </h3>
                  </div>

                  <!-- Actions for this evidence -->
                  <div class="flex items-center gap-1 shrink-0">
                    <button *ngIf="ev.status === 'CaptureFailed'" (click)="retryCapture(ev.id)"
                            [disabled]="retryingId() === ev.id"
                            class="px-2 py-1 rounded bg-amber-600 hover:bg-amber-500 text-white font-semibold text-[10px] flex items-center gap-1 cursor-pointer disabled:opacity-50"
                            title="Reintentar captura de URL">
                      <i class="pi" [ngClass]="retryingId() === ev.id ? 'pi-spin pi-spinner' : 'pi-refresh'"></i>
                      <span>Reintentar</span>
                    </button>
                    <button *ngIf="ev.status !== 'Excluded'" (click)="detachEvidence(ev.id)"
                            class="p-1 rounded text-[var(--app-muted)] hover:text-red-500 cursor-pointer"
                            title="Desvincular o excluir evidencia">
                      <i class="pi pi-trash text-[11px]"></i>
                    </button>
                  </div>
                </div>

                <!-- URL link if present -->
                <div *ngIf="ev.originUrl" class="text-[11px]">
                  <a [href]="ev.originUrl" target="_blank" rel="noopener noreferrer"
                     class="text-blue-600 dark:text-blue-400 hover:underline flex items-center gap-1 font-mono break-all text-[10px]">
                    <i class="pi pi-external-link text-[9px]"></i>
                    <span>{{ ev.originUrl }}</span>
                  </a>
                </div>

                <!-- Error Message if failed -->
                <div *ngIf="ev.status === 'CaptureFailed'" class="p-2 rounded bg-red-500/10 text-red-600 dark:text-red-400 text-[11px] font-mono">
                  <i class="pi pi-exclamation-circle mr-1"></i>
                  <span>{{ ev.errorMessage || 'Fallo de conexión o extracción HTTP.' }}</span>
                </div>

                <!-- Extracted Text preview -->
                <div *ngIf="ev.extractedText || ev.rawContent" 
                     class="p-2.5 rounded bg-[var(--app-card-bg)] border border-[var(--app-card-border)] text-[var(--app-text)] text-[11px] leading-relaxed max-h-24 overflow-y-auto">
                  {{ ev.extractedText || ev.rawContent }}
                </div>

                <!-- SHA-256 Content Hash preview -->
                <div *ngIf="ev.contentHash" class="flex items-center justify-between text-[10px] text-[var(--app-muted)] pt-1 border-t border-[var(--app-card-border)]">
                  <span class="flex items-center gap-1 font-mono truncate max-w-md">
                    <i class="pi pi-shield text-[9px]"></i>
                    <span>SHA-256: {{ ev.contentHash }}</span>
                  </span>
                  <span class="font-mono text-[9px]">Inmutable</span>
                </div>

              </div>
            </div>

          </div>
        </div>

        <!-- Right: TruthSource Snapshot Card (1 col) -->
        <div class="space-y-4">
          <div class="bg-[var(--app-card-bg)] border border-[var(--app-card-border)] rounded-xl p-4 sm:p-5 shadow-xs space-y-4">
            
            <div class="flex items-center justify-between border-b border-[var(--app-card-border)] pb-3">
              <div class="flex items-center gap-2">
                <i class="pi pi-check-circle text-indigo-600 dark:text-indigo-400"></i>
                <h2 class="text-sm font-bold text-[var(--app-text)]">TruthSource Factual</h2>
              </div>
              <span *ngIf="item()?.truthSource" 
                    class="px-2 py-0.5 rounded text-[10px] font-bold uppercase tracking-wider font-mono border"
                    [ngClass]="{
                      'bg-emerald-500/15 text-emerald-600 dark:text-emerald-400 border-emerald-500/30': item()?.truthSource?.status === 'Approved',
                      'bg-amber-500/15 text-amber-600 dark:text-amber-400 border-amber-500/30': item()?.truthSource?.status === 'UnderReview',
                      'bg-blue-500/15 text-blue-600 dark:text-blue-400 border-blue-500/30': item()?.truthSource?.status === 'Draft',
                      'bg-red-500/15 text-red-600 dark:text-red-400 border-red-500/30': item()?.truthSource?.status === 'Rejected'
                    }">
                {{ item()?.truthSource?.status }} (v{{ item()?.truthSource?.version }})
              </span>
            </div>

            <!-- If TruthSource exists -->
            <div *ngIf="item()?.truthSource" class="space-y-3">
              
              <!-- Rejection Notice if rejected -->
              <div *ngIf="item()?.truthSource?.status === 'Rejected'" class="p-3 rounded-lg bg-red-500/10 border border-red-500/30 space-y-1">
                <span class="font-bold text-red-600 dark:text-red-400 block text-[11px]">Motivo de Rechazo:</span>
                <p class="text-[11px] text-[var(--app-text)] italic">"{{ item()?.truthSource?.rejectionReason }}"</p>
                <span class="text-[9px] text-[var(--app-muted)] block">Rechazado por {{ item()?.truthSource?.rejectedByEmail }} el {{ item()?.truthSource?.rejectedAtUtc | date:'yyyy-MM-dd HH:mm' }}</span>
              </div>

              <!-- Approval Notice if approved -->
              <div *ngIf="item()?.truthSource?.status === 'Approved'" class="p-3 rounded-lg bg-emerald-500/10 border border-emerald-500/30 space-y-1">
                <span class="font-bold text-emerald-600 dark:text-emerald-400 block text-[11px]">Aprobado para Producción</span>
                <span class="text-[9px] text-[var(--app-muted)] block">Aprobado por {{ item()?.truthSource?.approvedByEmail }} el {{ item()?.truthSource?.approvedAtUtc | date:'yyyy-MM-dd HH:mm' }}</span>
              </div>

              <!-- Summary snippet -->
              <div class="space-y-1">
                <span class="text-[10px] font-bold uppercase tracking-wider text-[var(--app-muted)] block">Resumen Factual</span>
                <p class="text-[11px] text-[var(--app-text)] leading-relaxed p-2.5 rounded bg-[var(--app-bg)] border border-[var(--app-card-border)]">
                  {{ item()?.truthSource?.summary }}
                </p>
              </div>

              <!-- Key Ideas Count & Claims Count -->
              <div class="grid grid-cols-2 gap-2 text-center text-[11px]">
                <div class="p-2 rounded bg-[var(--app-bg)] border border-[var(--app-card-border)]">
                  <span class="text-[var(--app-muted)] block text-[10px]">Ideas Clave</span>
                  <span class="font-bold text-[var(--app-text)]">{{ item()?.truthSource?.keyIdeas?.length || 0 }}</span>
                </div>
                <div class="p-2 rounded bg-[var(--app-bg)] border border-[var(--app-card-border)]">
                  <span class="text-[var(--app-muted)] block text-[10px]">Claims Verificables</span>
                  <span class="font-bold text-[var(--app-text)]">{{ item()?.truthSource?.verifiableClaims?.length || 0 }}</span>
                </div>
              </div>

              <div class="pt-2">
                <a [routerLink]="['/content/items', item()?.id, 'truth-source']"
                   class="w-full py-2 rounded-lg bg-indigo-600 hover:bg-indigo-500 text-white font-bold text-xs flex items-center justify-center gap-1.5 transition-all shadow-xs">
                  <i class="pi pi-sliders-h"></i>
                  <span>Abrir en Review Studio</span>
                </a>
              </div>

            </div>

            <!-- If NO TruthSource exists yet -->
            <div *ngIf="!item()?.truthSource" class="text-center py-6 space-y-3 text-[var(--app-muted)]">
              <div class="w-10 h-10 rounded-full bg-[var(--app-bg)] flex items-center justify-center mx-auto text-indigo-500">
                <i class="pi pi-sparkles text-lg"></i>
              </div>
              <p class="font-bold text-sm text-[var(--app-text)]">Aún no se ha sintetizado el TruthSource</p>
              <p class="text-[11px]">Genera una propuesta inicial de hechos verificables y restricciones editoriales a partir de las evidencias.</p>
              
              <button (click)="generateAiDraft()" [disabled]="isGeneratingDraft() || !hasUsableEvidence"
                      class="w-full py-2 rounded-lg bg-indigo-600 hover:bg-indigo-500 text-white font-bold text-xs flex items-center justify-center gap-1.5 cursor-pointer disabled:opacity-50 shadow-xs">
                <i *ngIf="isGeneratingDraft()" class="pi pi-spin pi-spinner text-xs"></i>
                <i *ngIf="!isGeneratingDraft()" class="pi pi-sparkles text-xs"></i>
                <span>{{ isGeneratingDraft() ? 'Sintetizando con IA...' : 'Generar Borrador con IA' }}</span>
              </button>
              <p *ngIf="!hasUsableEvidence" class="text-[10px] text-amber-500">
                Requiere al menos 1 evidencia capturada con éxito.
              </p>
            </div>

          </div>

          <!-- Ideas & Creative Matrix Card (Right Col) -->
          <div class="bg-[var(--app-card-bg)] border border-[var(--app-card-border)] rounded-xl p-4 sm:p-5 shadow-xs space-y-3">
            <div class="flex items-center justify-between border-b border-[var(--app-card-border)] pb-3">
              <div class="flex items-center gap-2">
                <i class="pi pi-lightbulb text-purple-600 dark:text-purple-400"></i>
                <h2 class="text-sm font-bold text-[var(--app-text)]">Ideas & Ángulos</h2>
                <span class="px-1.5 py-0.5 rounded bg-[var(--app-bg)] border border-[var(--app-card-border)] font-mono text-[10px] font-bold">
                  {{ ideas().length }}
                </span>
              </div>

              <a [routerLink]="['/content/items', item()?.id, 'ideas']" 
                 class="text-xs font-bold text-purple-600 dark:text-purple-400 hover:underline flex items-center gap-1">
                <span>Ver Matriz</span>
                <i class="pi pi-arrow-right text-[10px]"></i>
              </a>
            </div>

            <!-- Active Selected Idea Preview -->
            <div *ngIf="activeSelectedIdea()" class="p-3 rounded-xl bg-emerald-500/10 border border-emerald-500/20 space-y-1.5">
              <div class="flex items-center gap-1.5 text-emerald-600 dark:text-emerald-400 font-extrabold text-[10px] uppercase tracking-wider">
                <i class="pi pi-check-circle"></i>
                <span>Idea Activa Seleccionada</span>
              </div>
              <p class="font-bold text-xs text-[var(--app-text)] line-clamp-2">
                {{ activeSelectedIdea()?.title }}
              </p>
              <p class="text-[10px] text-[var(--app-muted)] line-clamp-1 italic">
                "{{ activeSelectedIdea()?.hookStrategy }}"
              </p>
            </div>

            <!-- No Idea Selected but Proposed Ideas Exist -->
            <div *ngIf="!activeSelectedIdea() && ideas().length > 0" class="p-3 rounded-xl bg-blue-500/10 border border-blue-500/20 space-y-1 text-center">
              <p class="text-[11px] font-bold text-blue-600 dark:text-blue-400">
                {{ ideas().length }} ideas propuestas disponibles
              </p>
              <p class="text-[10px] text-[var(--app-muted)]">
                Selecciona una idea activa en la Matriz para continuar a guionización.
              </p>
            </div>

            <!-- No Ideas Yet -->
            <div *ngIf="ideas().length === 0" class="py-3 text-center text-[var(--app-muted)] text-[11px]">
              <p>No se han generado ideas para esta pieza todavía.</p>
            </div>

            <a [routerLink]="['/content/items', item()?.id, 'ideas']"
               class="w-full py-2 rounded-lg bg-[var(--app-bg)] hover:bg-[var(--app-card-bg)] border border-[var(--app-card-border)] text-[var(--app-text)] font-bold text-xs flex items-center justify-center gap-1.5 transition-colors">
              <i class="pi pi-sparkles text-purple-500"></i>
              <span>Abrir Matriz de Ideas</span>
            </a>
          </div>

          <!-- Script Studio Summary Card (Right Col) -->
          <div class="bg-[var(--app-card-bg)] border border-[var(--app-card-border)] rounded-xl p-4 sm:p-5 shadow-xs space-y-3">
            <div class="flex items-center justify-between border-b border-[var(--app-card-border)] pb-3">
              <div class="flex items-center gap-2">
                <i class="pi pi-file-edit text-blue-600 dark:text-blue-400"></i>
                <h2 class="text-sm font-bold text-[var(--app-text)]">Guión Editorial</h2>
              </div>
              <span *ngIf="script()" 
                    class="px-2 py-0.5 rounded text-[10px] font-bold uppercase tracking-wider font-mono border"
                    [ngClass]="{
                      'bg-emerald-500/15 text-emerald-600 dark:text-emerald-400 border-emerald-500/30': script()?.status === 'Approved',
                      'bg-amber-500/15 text-amber-600 dark:text-amber-400 border-amber-500/30': script()?.status === 'UnderReview',
                      'bg-blue-500/15 text-blue-600 dark:text-blue-400 border-blue-500/30': script()?.status === 'Draft',
                      'bg-red-500/15 text-red-600 dark:text-red-400 border-red-500/30': script()?.status === 'Rejected'
                    }">
                {{ script()?.status }} (v{{ script()?.version }})
              </span>
            </div>

            <!-- Stale Alert if script is stale -->
            <div *ngIf="script()?.isStale" class="p-2.5 rounded-lg bg-rose-500/10 border border-rose-500/30 space-y-1">
              <div class="flex items-center gap-1.5 text-rose-600 dark:text-rose-400 font-bold text-[10px]">
                <i class="pi pi-exclamation-triangle"></i>
                <span>Lineage Desactualizado</span>
              </div>
              <p class="text-[10px] text-[var(--app-text)] leading-tight">
                {{ script()?.staleReason || 'El guión requiere reconciliación con la idea/TruthSource actual.' }}
              </p>
            </div>

            <!-- Script Metrics if exists -->
            <div *ngIf="script()" class="space-y-2">
              <p class="font-bold text-xs text-[var(--app-text)] line-clamp-1">
                {{ script()?.title }}
              </p>
              <div class="grid grid-cols-2 gap-2 text-center text-[11px]">
                <div class="p-2 rounded bg-[var(--app-bg)] border border-[var(--app-card-border)]">
                  <span class="text-[var(--app-muted)] block text-[10px]">Escenas</span>
                  <span class="font-bold text-[var(--app-text)]">{{ script()?.scenes?.length || 0 }}</span>
                </div>
                <div class="p-2 rounded bg-[var(--app-bg)] border border-[var(--app-card-border)]">
                  <span class="text-[var(--app-muted)] block text-[10px]">Duración</span>
                  <span class="font-bold text-blue-600 dark:text-blue-400">~{{ script()?.estimatedDurationSeconds?.toFixed(1) }}s</span>
                </div>
              </div>
            </div>

            <!-- No Script Yet -->
            <div *ngIf="!script()" class="py-3 text-center text-[var(--app-muted)] text-[11px]">
              <p>Aún no se ha redactado el guión para esta pieza.</p>
            </div>

            <a [routerLink]="['/content/items', item()?.id, 'script']"
               class="w-full py-2 rounded-lg bg-blue-600 hover:bg-blue-500 text-white font-bold text-xs flex items-center justify-center gap-1.5 transition-colors shadow-xs">
              <i class="pi pi-file-edit"></i>
              <span>{{ script() ? 'Abrir Script Studio' : 'Crear / Generar Guión' }}</span>
            </a>
          </div>

        </div>

      </div>

      <!-- Attach Evidence Modal -->
      <app-attach-evidence-modal
        [isOpen]="isAttachModalOpen()"
        [contentItemId]="item()?.id || ''"
        (closed)="isAttachModalOpen.set(false)"
        (attached)="onEvidenceAttached($event)">
      </app-attach-evidence-modal>

    </div>
  `
})
export class ContentDetailComponent implements OnInit {
  private readonly api = inject(ApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly cdr = inject(ChangeDetectorRef);

  readonly item = signal<ContentItemDetailDto | null>(null);
  readonly ideas = signal<ContentIdeaDto[]>([]);
  readonly script = signal<any | null>(null);
  readonly isLoading = signal<boolean>(true);
  readonly errorMessage = signal<string | null>(null);
  readonly currentId = signal<string | null>(null);

  readonly isAttachModalOpen = signal<boolean>(false);
  readonly isGeneratingDraft = signal<boolean>(false);
  readonly retryingId = signal<string | null>(null);

  get hasUsableEvidence(): boolean {
    const cur = this.item();
    if (!cur || !cur.evidences) return false;
    return cur.evidences.some(e => e.status === 'Captured');
  }

  get activeSelectedIdea(): () => ContentIdeaDto | null {
    return () => this.ideas().find(i => i.status === 'Selected') || null;
  }

  ngOnInit() {
    this.route.paramMap.subscribe(params => {
      const id = params.get('id');
      if (id) {
        this.currentId.set(id);
        this.loadDetail(id);
      } else {
        this.isLoading.set(false);
        this.errorMessage.set('Identificador de pieza de contenido inválido o ausente.');
        this.cdr.markForCheck();
      }
    });
  }

  loadDetail(id: string) {
    this.isLoading.set(true);
    this.errorMessage.set(null);
    this.cdr.markForCheck();

    this.api.getContentItemDetail(id).subscribe({
      next: (detail) => {
        this.item.set(detail);
        
        // Fetch ideas
        this.api.getContentIdeas(id).subscribe({
          next: (ideas) => this.ideas.set(ideas),
          error: () => {}
        });

        // Fetch script if any
        this.api.getScript(id).subscribe({
          next: (sc) => {
            this.script.set(sc);
            this.isLoading.set(false);
            this.cdr.markForCheck();
          },
          error: () => {
            this.script.set(null);
            this.isLoading.set(false);
            this.cdr.markForCheck();
          }
        });
      },
      error: (err) => {
        this.isLoading.set(false);
        this.errorMessage.set(err.status === 404
          ? 'No se encontró la pieza de contenido solicitada.'
          : (err.error?.message || err.error?.error || `Error al cargar la pieza de contenido (HTTP ${err.status || 'Error'}).`));
        this.cdr.markForCheck();
      }
    });
  }

  retryLoad() {
    const id = this.currentId();
    if (id) {
      this.loadDetail(id);
    }
  }

  onEvidenceAttached(newEvidence: ContentItemEvidenceDto) {
    const current = this.item();
    if (current) {
      this.item.set({
        ...current,
        evidences: [...current.evidences, newEvidence]
      });
      this.cdr.markForCheck();
    }
  }

  retryCapture(evidenceId: string) {
    const current = this.item();
    if (!current) return;
    this.retryingId.set(evidenceId);
    this.cdr.markForCheck();

    this.api.retryEvidenceCapture(current.id, evidenceId).subscribe({
      next: (updated) => {
        this.retryingId.set(null);
        const cur = this.item();
        if (cur) {
          const idx = cur.evidences.findIndex(e => e.id === evidenceId);
          if (idx !== -1) {
            const nextEv = [...cur.evidences];
            nextEv[idx] = updated;
            this.item.set({ ...cur, evidences: nextEv });
          }
        }
        this.cdr.markForCheck();
      },
      error: () => {
        this.retryingId.set(null);
        this.cdr.markForCheck();
      }
    });
  }

  detachEvidence(evidenceId: string) {
    const current = this.item();
    if (!current) return;
    this.api.detachEvidence(current.id, evidenceId).subscribe({
      next: () => {
        const cur = this.item();
        if (cur) {
          this.item.set({
            ...cur,
            evidences: cur.evidences.filter(e => e.id !== evidenceId)
          });
          this.cdr.markForCheck();
        }
      }
    });
  }

  generateAiDraft() {
    const current = this.item();
    if (!current) return;
    this.isGeneratingDraft.set(true);
    this.cdr.markForCheck();

    this.api.generateAiDraft(current.id).subscribe({
      next: (ts) => {
        this.isGeneratingDraft.set(false);
        this.router.navigate(['/content/items', current.id, 'truth-source']);
      },
      error: () => {
        this.isGeneratingDraft.set(false);
        this.cdr.markForCheck();
      }
    });
  }
}
