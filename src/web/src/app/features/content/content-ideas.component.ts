import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterModule } from '@angular/router';
import { ApiService, ContentIdeaDto, ContentItemDetailDto, DismissIdeaRequest, ReopenIdeaRequest, SelectIdeaRequest } from '../../core/api.service';
import { GenerateIdeasModalComponent } from './generate-ideas-modal.component';
import { IdeaEditDrawerComponent } from './idea-edit-drawer.component';
import { IdeaVersionHistoryDrawerComponent } from './idea-version-history-drawer.component';

@Component({
  selector: 'app-content-ideas',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    RouterModule,
    GenerateIdeasModalComponent,
    IdeaEditDrawerComponent,
    IdeaVersionHistoryDrawerComponent
  ],
  template: `
    <!-- Loading State -->
    <div *ngIf="isLoading()" class="p-12 text-center text-xs text-[var(--app-muted)] space-y-2">
      <i class="pi pi-spin pi-spinner text-2xl text-blue-500 block mx-auto"></i>
      <span class="font-medium text-sm text-[var(--app-text)]">Cargando Matriz de Ideas y Ángulos...</span>
    </div>

    <!-- Error State -->
    <div *ngIf="errorMessage() && !isLoading()" class="p-6 rounded-xl bg-red-500/10 border border-red-500/30 text-center space-y-3 max-w-lg mx-auto my-8 text-xs">
      <i class="pi pi-exclamation-triangle text-2xl text-red-500 block"></i>
      <p class="font-bold text-sm text-[var(--app-text)]">{{ errorMessage() }}</p>
      <div class="flex items-center justify-center gap-2 pt-2">
        <button (click)="loadAllData()" class="px-3.5 py-1.5 rounded-lg bg-blue-600 hover:bg-blue-500 text-white font-bold text-xs cursor-pointer shadow-xs">
          <i class="pi pi-refresh mr-1 text-[10px]"></i> Reintentar
        </button>
        <a [routerLink]="['/content/items', contentItemId()]" class="px-3.5 py-1.5 rounded-lg border border-[var(--app-card-border)] text-xs text-[var(--app-muted)] hover:text-[var(--app-text)]">
          Volver a la Pieza
        </a>
      </div>
    </div>

    <!-- Loaded Workspace View -->
    <div *ngIf="!isLoading() && contentItem()" class="space-y-5 max-w-7xl mx-auto text-xs">
      
      <!-- Operational Header -->
      <div class="bg-[var(--app-card-bg)] border border-[var(--app-card-border)] rounded-2xl p-4 sm:p-5 shadow-xs">
        <div class="flex flex-col lg:flex-row lg:items-center justify-between gap-4">
          
          <div class="space-y-1.5">
            <div class="flex items-center gap-2 flex-wrap text-xs">
              <a [routerLink]="['/content/items', contentItemId()]" class="text-[var(--app-muted)] hover:text-[var(--app-text)] flex items-center gap-1">
                <i class="pi pi-arrow-left text-[10px]"></i> Detalle de Pieza
              </a>
              <span class="text-[var(--app-muted)]">/</span>
              <span class="px-2 py-0.5 rounded bg-blue-500/15 text-blue-600 dark:text-blue-400 border border-blue-500/30 text-[10px] font-bold">
                {{ contentItem()?.channelName || 'Canal' }}
              </span>
              <span class="px-2 py-0.5 rounded text-[10px] font-bold uppercase tracking-wider border font-mono"
                    [ngClass]="{
                      'bg-emerald-500/15 text-emerald-600 dark:text-emerald-400 border-emerald-500/30': contentItem()?.stage === 'IdeaSelected',
                      'bg-purple-500/15 text-purple-600 dark:text-purple-400 border-purple-500/30': contentItem()?.stage === 'TruthSourceApproved',
                      'bg-slate-500/15 text-slate-500 border-slate-500/30': contentItem()?.stage !== 'IdeaSelected' && contentItem()?.stage !== 'TruthSourceApproved'
                    }">
                {{ contentItem()?.stage }}
              </span>
              <span *ngIf="contentItem()?.truthSource" class="px-2 py-0.5 rounded bg-emerald-500/10 text-emerald-600 dark:text-emerald-400 border border-emerald-500/20 text-[10px] font-mono font-semibold flex items-center gap-1">
                <i class="pi pi-verified text-[10px]"></i> TruthSource v{{ contentItem()?.truthSource?.version }} Aprobado
              </span>
            </div>

            <div class="flex items-center gap-3">
              <h1 class="text-base sm:text-xl font-bold text-[var(--app-text)] leading-snug">
                Matriz de Ideas y Ángulos Creativos
              </h1>
            </div>
            <p class="text-xs text-[var(--app-muted)]">
              Pieza: <span class="font-bold text-[var(--app-text)]">{{ contentItem()?.title }}</span> • Explora, genera con DeepSeek Reasoning y selecciona exactamente una idea activa para guionización.
            </p>
          </div>

          <!-- Action Buttons -->
          <div class="flex items-center gap-2 shrink-0 flex-wrap">
            <button (click)="openGenerateModal()"
                    [disabled]="!isTruthSourceApproved()"
                    [title]="!isTruthSourceApproved() ? 'Requiere TruthSource Aprobado' : ''"
                    class="px-3.5 py-2 rounded-xl bg-gradient-to-r from-purple-600 to-indigo-600 hover:from-purple-500 hover:to-indigo-500 text-white font-bold text-xs flex items-center gap-1.5 cursor-pointer shadow-sm shadow-purple-500/20 transition-all disabled:opacity-40 disabled:cursor-not-allowed">
              <i class="pi pi-sparkles"></i>
              <span>Generar con IA</span>
            </button>

            <button (click)="openCreateDrawer()"
                    [disabled]="!isTruthSourceApproved()"
                    class="px-3.5 py-2 rounded-xl border border-[var(--app-card-border)] bg-[var(--app-bg)] hover:bg-[var(--app-card-bg)] text-[var(--app-text)] font-bold text-xs flex items-center gap-1.5 cursor-pointer transition-colors disabled:opacity-40 disabled:cursor-not-allowed">
              <i class="pi pi-plus text-blue-600 dark:text-blue-400"></i>
              <span>Añadir Idea Manual</span>
            </button>
          </div>

        </div>
      </div>

      <!-- TruthSource Warning if Not Approved -->
      <div *ngIf="!isTruthSourceApproved()" class="p-4 rounded-xl bg-amber-500/10 border border-amber-500/30 text-amber-900 dark:text-amber-200 flex items-start gap-3">
        <i class="pi pi-lock text-amber-600 dark:text-amber-400 text-base mt-0.5 shrink-0"></i>
        <div class="space-y-1">
          <span class="font-bold text-xs block">Generación Bloqueada: TruthSource No Aprobado</span>
          <p class="text-[11px] text-amber-800 dark:text-amber-300 leading-relaxed">
            Para garantizar la exactitud factual y el control editorial de la Content Factory, la generación y creación de ideas requiere que el TruthSource esté formalmente aprobado.
          </p>
          <div class="pt-1">
            <a [routerLink]="['/content/items', contentItemId(), 'truth-source']" class="font-bold underline hover:text-amber-950 dark:hover:text-amber-100">
              Ir al TruthSource Review Studio para revisar y aprobar →
            </a>
          </div>
        </div>
      </div>

      <!-- Active Selected Idea Hero Banner (If any idea is selected) -->
      <div *ngIf="selectedIdea()" class="bg-gradient-to-r from-emerald-500/10 via-emerald-500/5 to-transparent border-2 border-emerald-500/40 rounded-2xl p-4 sm:p-5 shadow-sm space-y-3">
        <div class="flex items-center justify-between gap-2 flex-wrap">
          <div class="flex items-center gap-2">
            <span class="px-2.5 py-1 rounded-lg bg-emerald-600 text-white text-[10px] font-extrabold uppercase tracking-wider shadow-xs flex items-center gap-1">
              <i class="pi pi-check-circle"></i>
              Idea Activa Seleccionada
            </span>
            <span class="font-mono text-[11px] text-[var(--app-muted)]">
              v{{ selectedIdea()?.version }} • Seleccionada por {{ selectedIdea()?.selectedByEmail }}
            </span>
          </div>

          <div class="flex items-center gap-2">
            <button (click)="openEditDrawer(selectedIdea()!)" class="px-2.5 py-1 rounded-lg border border-[var(--app-card-border)] hover:bg-[var(--app-card-bg)] text-[var(--app-text)] font-semibold text-[11px] flex items-center gap-1 cursor-pointer">
              <i class="pi pi-pencil text-[10px]"></i> Editar
            </button>
            <button (click)="openHistoryDrawer(selectedIdea()!)" class="px-2.5 py-1 rounded-lg border border-[var(--app-card-border)] hover:bg-[var(--app-card-bg)] text-[var(--app-text)] font-semibold text-[11px] flex items-center gap-1 cursor-pointer">
              <i class="pi pi-history text-[10px]"></i> Historial
            </button>
          </div>
        </div>

        <div class="space-y-1.5">
          <h2 class="text-base sm:text-lg font-bold text-[var(--app-text)]">
            {{ selectedIdea()?.title }}
          </h2>
          <p class="text-xs text-[var(--app-text)] leading-relaxed">
            <span class="font-bold text-[var(--app-muted)]">Ángulo:</span> {{ selectedIdea()?.angle }}
          </p>
          <div class="p-2.5 rounded-xl bg-[var(--app-card-bg)] border border-[var(--app-card-border)] flex items-start gap-2">
            <i class="pi pi-bolt text-amber-500 text-sm mt-0.5 shrink-0"></i>
            <div>
              <span class="font-bold text-[10px] text-amber-600 dark:text-amber-400 block uppercase">Gancho de Retención (3-5s):</span>
              <p class="text-[11px] font-medium text-[var(--app-text)]">"{{ selectedIdea()?.hookStrategy }}"</p>
            </div>
          </div>
        </div>

        <div class="flex items-center justify-between text-[11px] pt-1 border-t border-emerald-500/20 text-[var(--app-muted)] flex-wrap gap-2">
          <span>Audiencia: <strong class="text-[var(--app-text)]">{{ selectedIdea()?.audienceValue }}</strong></span>
          <span class="font-mono">Lineage: TruthSource v{{ selectedIdea()?.truthSourceVersionId ? selectedIdea()?.truthSourceVersionId?.substring(0,8) : 'N/A' }}</span>
        </div>
      </div>

      <!-- Filter Tabs & Stats Bar -->
      <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-3 bg-[var(--app-card-bg)] border border-[var(--app-card-border)] rounded-xl p-2.5">
        <div class="flex items-center gap-1.5 overflow-x-auto">
          <button *ngFor="let filter of filters"
                  (click)="activeFilter.set(filter.key)"
                  [class.bg-blue-600]="activeFilter() === filter.key"
                  [class.text-white]="activeFilter() === filter.key"
                  [class.text-[var(--app-muted)]]="activeFilter() !== filter.key"
                  [class.hover:text-[var(--app-text)]]="activeFilter() !== filter.key"
                  class="px-3 py-1.5 rounded-lg font-bold text-xs transition-colors cursor-pointer flex items-center gap-1.5 whitespace-nowrap">
            <span>{{ filter.label }}</span>
            <span class="px-1.5 py-0.2 rounded-full text-[10px] font-mono"
                  [ngClass]="activeFilter() === filter.key ? 'bg-white/20 text-white' : 'bg-[var(--app-bg)] text-[var(--app-muted)] border border-[var(--app-card-border)]'">
              {{ getCountForFilter(filter.key) }}
            </span>
          </button>
        </div>

        <div class="text-[11px] text-[var(--app-muted)] px-2 font-medium">
          Total: <strong class="text-[var(--app-text)]">{{ ideas().length }}</strong> propuestas registradas
        </div>
      </div>

      <!-- Empty State -->
      <div *ngIf="filteredIdeas().length === 0" class="py-16 text-center bg-[var(--app-card-bg)] border border-[var(--app-card-border)] rounded-2xl p-8 space-y-4">
        <div class="w-12 h-12 rounded-2xl bg-purple-500/10 border border-purple-500/20 flex items-center justify-center mx-auto text-purple-600 dark:text-purple-400">
          <i class="pi pi-lightbulb text-xl"></i>
        </div>
        <div class="space-y-1 max-w-md mx-auto">
          <h3 class="text-sm font-bold text-[var(--app-text)]">No hay ideas en este estado</h3>
          <p class="text-xs text-[var(--app-muted)]">
            Genera un lote de propuestas optimizadas con DeepSeek Reasoning o añade una idea manualmente.
          </p>
        </div>
        <div *ngIf="isTruthSourceApproved()" class="pt-2 flex justify-center gap-2">
          <button (click)="openGenerateModal()" class="px-4 py-2 rounded-xl bg-purple-600 hover:bg-purple-500 text-white font-bold text-xs flex items-center gap-1.5 cursor-pointer shadow-xs">
            <i class="pi pi-sparkles"></i> Generar Ideas con IA
          </button>
          <button (click)="openCreateDrawer()" class="px-4 py-2 rounded-xl border border-[var(--app-card-border)] text-[var(--app-text)] font-semibold text-xs cursor-pointer hover:bg-[var(--app-bg)]">
            Crear Manualmente
          </button>
        </div>
      </div>

      <!-- Idea Matrix Grid -->
      <div *ngIf="filteredIdeas().length > 0" class="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-3 gap-4">
        
        <div *ngFor="let idea of filteredIdeas()" 
             class="bg-[var(--app-card-bg)] border rounded-2xl p-4 sm:p-5 shadow-xs flex flex-col justify-between space-y-4 transition-all duration-200"
             [ngClass]="{
               'border-emerald-500/60 ring-2 ring-emerald-500/20 shadow-emerald-500/5': idea.status === 'Selected',
               'border-[var(--app-card-border)] hover:border-blue-500/40': idea.status === 'Proposed',
               'border-[var(--app-card-border)] opacity-60 bg-[var(--app-bg)]/50': idea.status === 'Dismissed'
             }">
          
          <!-- Card Top: Status, Version, Tags -->
          <div class="space-y-3">
            <div class="flex items-center justify-between gap-2 flex-wrap">
              <div class="flex items-center gap-1.5">
                <span class="px-2 py-0.5 rounded text-[10px] font-bold uppercase tracking-wider border font-mono"
                      [ngClass]="{
                        'bg-emerald-500/15 text-emerald-600 dark:text-emerald-400 border-emerald-500/30': idea.status === 'Selected',
                        'bg-blue-500/15 text-blue-600 dark:text-blue-400 border-blue-500/30': idea.status === 'Proposed',
                        'bg-rose-500/15 text-rose-600 dark:text-rose-400 border-rose-500/30': idea.status === 'Dismissed'
                      }">
                  {{ idea.status }}
                </span>

                <span class="px-1.5 py-0.5 rounded bg-[var(--app-bg)] border border-[var(--app-card-border)] font-mono text-[10px] text-[var(--app-muted)]">
                  v{{ idea.version }}
                </span>
              </div>

              <div class="flex items-center gap-1">
                <span *ngIf="idea.freshnessClass" class="px-2 py-0.5 rounded text-[10px] font-semibold bg-indigo-500/10 text-indigo-600 dark:text-indigo-400 border border-indigo-500/20">
                  {{ idea.freshnessClass }}
                </span>
                <span *ngIf="idea.priority" class="px-2 py-0.5 rounded text-[10px] font-semibold"
                      [ngClass]="{
                        'bg-red-500/10 text-red-600 dark:text-red-400 border border-red-500/20': idea.priority === 'High' || idea.priority === 'Urgent',
                        'bg-slate-500/10 text-slate-600 dark:text-slate-400 border border-slate-500/20': idea.priority === 'Normal' || idea.priority === 'Low'
                      }">
                  {{ idea.priority }}
                </span>
              </div>
            </div>

            <!-- Title & Angle -->
            <div class="space-y-1.5">
              <h3 class="text-sm sm:text-base font-bold text-[var(--app-text)] leading-snug">
                {{ idea.title }}
              </h3>
              <p class="text-[11px] text-[var(--app-muted)] leading-relaxed">
                <strong class="text-[var(--app-text)]">Ángulo:</strong> {{ idea.angle }}
              </p>
            </div>

            <!-- Hook Strategy Box -->
            <div class="p-3 rounded-xl bg-purple-500/5 border border-purple-500/20 space-y-1">
              <div class="flex items-center gap-1.5 text-purple-600 dark:text-purple-400 font-bold text-[10px] uppercase tracking-wide">
                <i class="pi pi-bolt text-[10px]"></i>
                <span>Gancho Estratégico (Hook)</span>
              </div>
              <p class="text-[11px] font-medium text-[var(--app-text)] italic">
                "{{ idea.hookStrategy }}"
              </p>
            </div>

            <!-- Audience Value -->
            <div class="space-y-1 text-[11px]">
              <span class="text-[var(--app-muted)] font-semibold text-[10px] block">Valor para la Audiencia:</span>
              <p class="text-[var(--app-text)]">{{ idea.audienceValue }}</p>
            </div>

            <!-- Rationale if present -->
            <div *ngIf="idea.rationale" class="p-2 rounded-lg bg-[var(--app-bg)] border border-[var(--app-card-border)] text-[10px] text-[var(--app-muted)]">
              <strong class="text-[var(--app-text)]">Justificación:</strong> {{ idea.rationale }}
            </div>

            <!-- Dismissal Notes if dismissed -->
            <div *ngIf="idea.status === 'Dismissed' && idea.dismissalNotes" class="p-2 rounded-lg bg-red-500/10 border border-red-500/20 text-red-700 dark:text-red-300 text-[10px]">
              <strong>Motivo de descarte:</strong> {{ idea.dismissalNotes }}
            </div>

          </div>

          <!-- Card Bottom: Metadata & Action Bar -->
          <div class="space-y-3 pt-3 border-t border-[var(--app-card-border)]">
            <div class="flex items-center justify-between text-[10px] text-[var(--app-muted)]">
              <span>Por: {{ idea.createdByEmail }}</span>
              <span class="font-mono">TS v{{ idea.truthSourceVersionId ? idea.truthSourceVersionId.substring(0,8) : 'N/A' }}</span>
            </div>

            <!-- Action Buttons -->
            <div class="flex items-center justify-between gap-2 pt-1">
              
              <!-- Proposed Actions -->
              <ng-container *ngIf="idea.status === 'Proposed'">
                <button (click)="confirmSelectIdea(idea)"
                        [disabled]="isActionInProgress()"
                        class="flex-1 py-1.5 px-2.5 rounded-lg bg-emerald-600 hover:bg-emerald-500 text-white font-bold text-xs flex items-center justify-center gap-1 cursor-pointer shadow-xs transition-colors disabled:opacity-50">
                  <i class="pi pi-check"></i>
                  <span>Seleccionar</span>
                </button>

                <button (click)="openEditDrawer(idea)"
                        [disabled]="isActionInProgress()"
                        class="p-1.5 rounded-lg border border-[var(--app-card-border)] hover:bg-[var(--app-bg)] text-[var(--app-text)] font-semibold cursor-pointer"
                        title="Editar Idea">
                  <i class="pi pi-pencil text-xs"></i>
                </button>

                <button (click)="promptDismissIdea(idea)"
                        [disabled]="isActionInProgress()"
                        class="p-1.5 rounded-lg border border-[var(--app-card-border)] hover:bg-red-500/10 hover:text-red-500 text-[var(--app-muted)] cursor-pointer"
                        title="Descartar Idea">
                  <i class="pi pi-ban text-xs"></i>
                </button>

                <button (click)="openHistoryDrawer(idea)"
                        class="p-1.5 rounded-lg border border-[var(--app-card-border)] hover:bg-[var(--app-bg)] text-[var(--app-muted)] hover:text-[var(--app-text)] cursor-pointer"
                        title="Historial de Versiones">
                  <i class="pi pi-history text-xs"></i>
                </button>
              </ng-container>

              <!-- Selected Actions -->
              <ng-container *ngIf="idea.status === 'Selected'">
                <div class="flex-1 py-1.5 px-2.5 rounded-lg bg-emerald-500/15 border border-emerald-500/30 text-emerald-600 dark:text-emerald-400 font-bold text-xs flex items-center justify-center gap-1">
                  <i class="pi pi-check-circle"></i>
                  <span>Selección Activa</span>
                </div>

                <button (click)="openEditDrawer(idea)"
                        class="p-1.5 rounded-lg border border-[var(--app-card-border)] hover:bg-[var(--app-bg)] text-[var(--app-text)] font-semibold cursor-pointer"
                        title="Editar Idea">
                  <i class="pi pi-pencil text-xs"></i>
                </button>

                <button (click)="openHistoryDrawer(idea)"
                        class="p-1.5 rounded-lg border border-[var(--app-card-border)] hover:bg-[var(--app-bg)] text-[var(--app-muted)] hover:text-[var(--app-text)] cursor-pointer"
                        title="Historial de Versiones">
                  <i class="pi pi-history text-xs"></i>
                </button>
              </ng-container>

              <!-- Dismissed Actions -->
              <ng-container *ngIf="idea.status === 'Dismissed'">
                <button (click)="reopenIdea(idea)"
                        [disabled]="isActionInProgress()"
                        class="flex-1 py-1.5 px-2.5 rounded-lg border border-[var(--app-card-border)] bg-[var(--app-bg)] hover:bg-[var(--app-card-bg)] text-[var(--app-text)] font-bold text-xs flex items-center justify-center gap-1 cursor-pointer transition-colors disabled:opacity-50">
                  <i class="pi pi-undo text-blue-600 dark:text-blue-400"></i>
                  <span>Reabrir</span>
                </button>

                <button (click)="openHistoryDrawer(idea)"
                        class="p-1.5 rounded-lg border border-[var(--app-card-border)] hover:bg-[var(--app-bg)] text-[var(--app-muted)] hover:text-[var(--app-text)] cursor-pointer"
                        title="Historial de Versiones">
                  <i class="pi pi-history text-xs"></i>
                </button>
              </ng-container>

            </div>

          </div>

        </div>

      </div>

    </div>

    <!-- Dismiss Idea Modal / Prompt -->
    <div *ngIf="dismissingIdea()" class="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/60 backdrop-blur-xs animate-in fade-in duration-150 text-xs">
      <div class="bg-[var(--app-card-bg)] border border-[var(--app-card-border)] rounded-2xl w-full max-w-md p-5 shadow-2xl space-y-4">
        <div class="flex items-center justify-between border-b border-[var(--app-card-border)] pb-3">
          <div class="flex items-center gap-2 text-red-500 font-bold">
            <i class="pi pi-ban"></i>
            <span>Descartar Idea Creativa</span>
          </div>
          <button (click)="dismissingIdea.set(null)" class="text-[var(--app-muted)] hover:text-[var(--app-text)] cursor-pointer">
            <i class="pi pi-times"></i>
          </button>
        </div>

        <div class="space-y-2">
          <p class="text-[var(--app-text)] font-semibold">
            ¿Deseas descartar la idea "{{ dismissingIdea()?.title }}"?
          </p>
          <div class="space-y-1">
            <label class="text-[var(--app-muted)] font-semibold text-[10px]">Motivo del Descarte (Opcional):</label>
            <textarea [(ngModel)]="dismissNotes"
                      rows="3"
                      placeholder="Ej: Ángulo demasiado técnico para la audiencia general..."
                      class="w-full px-3 py-2 rounded-lg bg-[var(--app-bg)] border border-[var(--app-card-border)] text-[var(--app-text)] focus:border-red-500 outline-hidden transition-all text-xs resize-y"></textarea>
          </div>
        </div>

        <div class="flex items-center justify-end gap-2 pt-2 border-t border-[var(--app-card-border)]">
          <button (click)="dismissingIdea.set(null)" class="px-3 py-1.5 rounded-lg border border-[var(--app-card-border)] text-[var(--app-muted)] hover:text-[var(--app-text)] cursor-pointer font-semibold">
            Cancelar
          </button>
          <button (click)="confirmDismissIdea()" class="px-3.5 py-1.5 rounded-lg bg-red-600 hover:bg-red-500 text-white font-bold cursor-pointer shadow-xs">
            Confirmar Descarte
          </button>
        </div>
      </div>
    </div>

    <!-- Confirm Selection Replacement Modal -->
    <div *ngIf="confirmingSelectionIdea()" class="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/60 backdrop-blur-xs animate-in fade-in duration-150 text-xs">
      <div class="bg-[var(--app-card-bg)] border border-[var(--app-card-border)] rounded-2xl w-full max-w-md p-5 shadow-2xl space-y-4">
        <div class="flex items-center gap-2 text-emerald-600 dark:text-emerald-400 font-bold border-b border-[var(--app-card-border)] pb-3">
          <i class="pi pi-check-circle text-base"></i>
          <span>Confirmar Selección de Idea</span>
        </div>

        <div class="space-y-2 text-[var(--app-text)] leading-relaxed">
          <p>
            Vas a seleccionar la idea <strong class="text-blue-600 dark:text-blue-400">"{{ confirmingSelectionIdea()?.title }}"</strong> como base creativa para la etapa de guionización.
          </p>
          
          <div *ngIf="selectedIdea() && selectedIdea()?.id !== confirmingSelectionIdea()?.id" class="p-3 rounded-xl bg-amber-500/10 border border-amber-500/20 text-amber-900 dark:text-amber-200 space-y-1">
            <span class="font-bold text-[11px] block">Reemplazo Atómico de Selección</span>
            <p class="text-[10px] text-amber-800 dark:text-amber-300">
              La idea actualmente seleccionada (<em>"{{ selectedIdea()?.title }}"</em>) volverá automáticamente al estado <strong>Propuesta</strong>, registrando un snapshot histórico para ambas.
            </p>
          </div>
        </div>

        <div class="flex items-center justify-end gap-2 pt-2 border-t border-[var(--app-card-border)]">
          <button (click)="confirmingSelectionIdea.set(null)" class="px-3 py-1.5 rounded-lg border border-[var(--app-card-border)] text-[var(--app-muted)] hover:text-[var(--app-text)] cursor-pointer font-semibold">
            Cancelar
          </button>
          <button (click)="executeSelectIdea()" class="px-4 py-1.5 rounded-lg bg-emerald-600 hover:bg-emerald-500 text-white font-bold cursor-pointer shadow-xs">
            Confirmar Selección
          </button>
        </div>
      </div>
    </div>

    <!-- Modals & Drawers -->
    <app-generate-ideas-modal
      [isOpen]="isGenerateModalOpen()"
      [contentItemId]="contentItemId()"
      [truthSourceVersionNumber]="contentItem()?.truthSource?.version || 1"
      (closeEvent)="isGenerateModalOpen.set(false)"
      (ideasGenerated)="onIdeasGenerated($event)">
    </app-generate-ideas-modal>

    <app-idea-edit-drawer
      [isOpen]="isEditDrawerOpen()"
      [contentItemId]="contentItemId()"
      [idea]="editingIdea()"
      (closeEvent)="isEditDrawerOpen.set(false)"
      (ideaSaved)="onIdeaSaved($event)"
      (conflictReload)="loadAllData()">
    </app-idea-edit-drawer>

    <app-idea-version-history-drawer
      [isOpen]="isHistoryDrawerOpen()"
      [contentItemId]="contentItemId()"
      [idea]="historyIdea()"
      (closeEvent)="isHistoryDrawerOpen.set(false)">
    </app-idea-version-history-drawer>
  `
})
export class ContentIdeasComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly apiService = inject(ApiService);

  contentItemId = signal<string>('');
  contentItem = signal<ContentItemDetailDto | null>(null);
  ideas = signal<ContentIdeaDto[]>([]);
  isLoading = signal<boolean>(true);
  errorMessage = signal<string | null>(null);
  isActionInProgress = signal<boolean>(false);

  // Filter Tabs
  activeFilter = signal<'ALL' | 'PROPOSED' | 'SELECTED' | 'DISMISSED'>('ALL');
  filters = [
    { key: 'ALL' as const, label: 'Todas' },
    { key: 'PROPOSED' as const, label: 'Propuestas' },
    { key: 'SELECTED' as const, label: 'Seleccionada' },
    { key: 'DISMISSED' as const, label: 'Descartadas' }
  ];

  // Drawer / Modal states
  isGenerateModalOpen = signal<boolean>(false);
  isEditDrawerOpen = signal<boolean>(false);
  editingIdea = signal<ContentIdeaDto | null>(null);
  isHistoryDrawerOpen = signal<boolean>(false);
  historyIdea = signal<ContentIdeaDto | null>(null);

  // Action prompts
  dismissingIdea = signal<ContentIdeaDto | null>(null);
  dismissNotes = '';
  confirmingSelectionIdea = signal<ContentIdeaDto | null>(null);

  selectedIdea = computed(() => {
    return this.ideas().find(i => i.status === 'Selected') || null;
  });

  isTruthSourceApproved = computed(() => {
    const stage = this.contentItem()?.stage;
    const tsStatus = this.contentItem()?.truthSource?.status;
    return stage === 'TruthSourceApproved' || stage === 'IdeaSelected' || tsStatus === 'Approved';
  });

  filteredIdeas = computed(() => {
    const list = this.ideas();
    const filter = this.activeFilter();
    switch (filter) {
      case 'PROPOSED':
        return list.filter(i => i.status === 'Proposed');
      case 'SELECTED':
        return list.filter(i => i.status === 'Selected');
      case 'DISMISSED':
        return list.filter(i => i.status === 'Dismissed');
      default:
        return list;
    }
  });

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.contentItemId.set(id);
      this.loadAllData();
    } else {
      this.errorMessage.set('ID de pieza de contenido no proporcionado.');
      this.isLoading.set(false);
    }
  }

  loadAllData(): void {
    const id = this.contentItemId();
    if (!id) return;
    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.apiService.getContentItemDetail(id).subscribe({
      next: (item) => {
        this.contentItem.set(item);
        this.apiService.getContentIdeas(id).subscribe({
          next: (ideas) => {
            this.ideas.set(ideas);
            this.isLoading.set(false);
          },
          error: (err) => {
            this.isLoading.set(false);
            this.errorMessage.set(err?.error?.message || err?.message || 'Error al cargar ideas de la pieza.');
          }
        });
      },
      error: (err) => {
        this.isLoading.set(false);
        this.errorMessage.set(err?.error?.message || err?.message || 'Error al cargar detalle de la pieza.');
      }
    });
  }

  getCountForFilter(key: 'ALL' | 'PROPOSED' | 'SELECTED' | 'DISMISSED'): number {
    const list = this.ideas();
    switch (key) {
      case 'PROPOSED':
        return list.filter(i => i.status === 'Proposed').length;
      case 'SELECTED':
        return list.filter(i => i.status === 'Selected').length;
      case 'DISMISSED':
        return list.filter(i => i.status === 'Dismissed').length;
      default:
        return list.length;
    }
  }

  openGenerateModal(): void {
    this.isGenerateModalOpen.set(true);
  }

  openCreateDrawer(): void {
    this.editingIdea.set(null);
    this.isEditDrawerOpen.set(true);
  }

  openEditDrawer(idea: ContentIdeaDto): void {
    this.editingIdea.set(idea);
    this.isEditDrawerOpen.set(true);
  }

  openHistoryDrawer(idea: ContentIdeaDto): void {
    this.historyIdea.set(idea);
    this.isHistoryDrawerOpen.set(true);
  }

  onIdeasGenerated(newIdeas: ContentIdeaDto[]): void {
    this.ideas.set(newIdeas);
  }

  onIdeaSaved(saved: ContentIdeaDto): void {
    this.loadAllData();
  }

  confirmSelectIdea(idea: ContentIdeaDto): void {
    this.confirmingSelectionIdea.set(idea);
  }

  executeSelectIdea(): void {
    const idea = this.confirmingSelectionIdea();
    const contentItemId = this.contentItemId();
    if (!idea || !contentItemId) return;

    this.isActionInProgress.set(true);
    const request: SelectIdeaRequest = {
      expectedVersion: idea.version
    };

    this.apiService.selectIdea(contentItemId, idea.id, request).subscribe({
      next: (selected) => {
        this.isActionInProgress.set(false);
        this.confirmingSelectionIdea.set(null);
        this.loadAllData();
      },
      error: (err) => {
        this.isActionInProgress.set(false);
        this.confirmingSelectionIdea.set(null);
        if (err.status === 409) {
          alert('Conflicto de concurrencia: la idea fue modificada por otro operador. Recargando datos más recientes.');
          this.loadAllData();
        } else {
          alert(err?.error?.message || err?.message || 'Error al seleccionar la idea.');
        }
      }
    });
  }

  promptDismissIdea(idea: ContentIdeaDto): void {
    this.dismissNotes = '';
    this.dismissingIdea.set(idea);
  }

  confirmDismissIdea(): void {
    const idea = this.dismissingIdea();
    const contentItemId = this.contentItemId();
    if (!idea || !contentItemId) return;

    this.isActionInProgress.set(true);
    const request: DismissIdeaRequest = {
      notes: this.dismissNotes || null,
      expectedVersion: idea.version
    };

    this.apiService.dismissIdea(contentItemId, idea.id, request).subscribe({
      next: () => {
        this.isActionInProgress.set(false);
        this.dismissingIdea.set(null);
        this.loadAllData();
      },
      error: (err) => {
        this.isActionInProgress.set(false);
        this.dismissingIdea.set(null);
        if (err.status === 409) {
          alert('Conflicto de concurrencia: la idea fue modificada por otro operador.');
          this.loadAllData();
        } else {
          alert(err?.error?.message || err?.message || 'Error al descartar la idea.');
        }
      }
    });
  }

  reopenIdea(idea: ContentIdeaDto): void {
    const contentItemId = this.contentItemId();
    if (!idea || !contentItemId) return;

    this.isActionInProgress.set(true);
    const request: ReopenIdeaRequest = {
      expectedVersion: idea.version
    };

    this.apiService.reopenIdea(contentItemId, idea.id, request).subscribe({
      next: () => {
        this.isActionInProgress.set(false);
        this.loadAllData();
      },
      error: (err) => {
        this.isActionInProgress.set(false);
        if (err.status === 409) {
          alert('Conflicto de concurrencia: la idea fue modificada por otro operador.');
          this.loadAllData();
        } else {
          alert(err?.error?.message || err?.message || 'Error al reabrir la idea.');
        }
      }
    });
  }
}
