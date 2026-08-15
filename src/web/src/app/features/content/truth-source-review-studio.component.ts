import { Component, OnInit, inject, signal, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import {
  ApiService,
  ContentItemDetailDto,
  ContentItemEvidenceDto,
  RejectTruthSourceRequest,
  SaveTruthSourceRequest,
  TruthSourceDto,
  TruthSourceVersionDto,
  VerifiableClaimDto
} from '../../core/api.service';
import { AuthService } from '../../core/auth.service';

@Component({
  selector: 'app-truth-source-review-studio',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  template: `
    <!-- Loading State -->
    <div *ngIf="isLoading()" class="p-12 text-center text-xs text-[var(--app-muted)] space-y-2">
      <i class="pi pi-spin pi-spinner text-2xl text-indigo-500 block mx-auto"></i>
      <span class="font-medium text-sm text-[var(--app-text)]">Cargando TruthSource Review Studio...</span>
    </div>

    <!-- Error State -->
    <div *ngIf="errorMessage() && !isLoading()" class="p-6 rounded-xl bg-red-500/10 border border-red-500/30 text-center space-y-3 max-w-lg mx-auto my-8">
      <i class="pi pi-exclamation-triangle text-2xl text-red-500 block"></i>
      <p class="font-bold text-sm text-[var(--app-text)]">{{ errorMessage() }}</p>
      <div class="flex items-center justify-center gap-2 pt-2">
        <button (click)="loadData()" class="px-3.5 py-1.5 rounded-lg bg-indigo-600 hover:bg-indigo-500 text-white font-bold text-xs cursor-pointer shadow-xs">
          <i class="pi pi-refresh mr-1 text-[10px]"></i> Reintentar
        </button>
        <a [routerLink]="['/content/items']" class="px-3.5 py-1.5 rounded-lg border border-[var(--app-card-border)] text-xs text-[var(--app-muted)] hover:text-[var(--app-text)]">
          Volver al Workspace
        </a>
      </div>
    </div>

    <div *ngIf="!isLoading() && contentItem()" class="space-y-3 max-w-7xl mx-auto flex flex-col h-[calc(100vh-5.5rem)]">
      
      <!-- Studio Header & Action Bar -->
      <div class="bg-[var(--app-card-bg)] border border-[var(--app-card-border)] rounded-xl p-3 sm:p-4 shadow-xs shrink-0">
        <div class="flex flex-col md:flex-row md:items-center justify-between gap-3">
          
          <div class="space-y-1">
            <div class="flex items-center gap-2 flex-wrap text-xs">
              <a [routerLink]="['/content/items', contentItem()?.id]" class="text-[var(--app-muted)] hover:text-[var(--app-text)] flex items-center gap-1 font-medium">
                <i class="pi pi-arrow-left text-[10px]"></i> {{ contentItem()?.title }}
              </a>
              <span class="text-[var(--app-muted)]">/</span>
              <span class="px-2 py-0.5 rounded bg-blue-500/15 text-blue-600 dark:text-blue-400 border border-blue-500/30 text-[10px] font-bold">
                {{ contentItem()?.channelName || 'Canal' }}
              </span>
              <span *ngIf="truthSource()" 
                    class="px-2 py-0.5 rounded text-[10px] font-bold uppercase tracking-wider font-mono border"
                    [ngClass]="{
                      'bg-emerald-500/15 text-emerald-600 dark:text-emerald-400 border-emerald-500/30': truthSource()?.status === 'Approved',
                      'bg-amber-500/15 text-amber-600 dark:text-amber-400 border-amber-500/30': truthSource()?.status === 'UnderReview',
                      'bg-blue-500/15 text-blue-600 dark:text-blue-400 border-blue-500/30': truthSource()?.status === 'Draft',
                      'bg-red-500/15 text-red-600 dark:text-red-400 border-red-500/30': truthSource()?.status === 'Rejected'
                    }">
                {{ truthSource()?.status }} (v{{ truthSource()?.version }})
              </span>
              <span class="text-[10px] font-mono text-[var(--app-muted)]">
                Última edición: {{ (truthSource()?.updatedAtUtc || contentItem()?.updatedAtUtc) | date:'yyyy-MM-dd HH:mm' }}
              </span>
            </div>
            <h1 class="text-sm sm:text-base font-bold text-[var(--app-text)] flex items-center gap-2">
              <i class="pi pi-check-circle text-indigo-600 dark:text-indigo-400"></i>
              <span>TruthSource Review Studio</span>
            </h1>
          </div>

          <!-- Studio Actions Bar -->
          <div class="flex items-center gap-1.5 flex-wrap shrink-0">
            
            <!-- History Button -->
            <button (click)="openHistoryDrawer()" 
                    class="px-2.5 py-1.5 rounded-lg border border-[var(--app-card-border)] hover:bg-[var(--app-surface-hover)] text-[var(--app-text)] text-xs font-semibold flex items-center gap-1 cursor-pointer transition-colors"
                    title="Ver historial de versiones y auditoría">
              <i class="pi pi-history text-[11px]"></i>
              <span class="hidden sm:inline">Historial</span>
            </button>

            <!-- Generate AI Draft Button -->
            <button (click)="triggerAiDraft()" [disabled]="isGeneratingAi() || !hasUsableEvidence"
                    class="px-3 py-1.5 rounded-lg bg-indigo-600/15 hover:bg-indigo-600/25 text-indigo-600 dark:text-indigo-400 border border-indigo-500/30 text-xs font-bold flex items-center gap-1.5 cursor-pointer disabled:opacity-50 transition-all"
                    title="Sintetizar propuesta de TruthSource usando IA sobre las evidencias">
              <i class="pi" [ngClass]="isGeneratingAi() ? 'pi-spin pi-spinner' : 'pi-sparkles'"></i>
              <span>{{ isGeneratingAi() ? 'Sintetizando...' : 'Generar IA' }}</span>
            </button>

            <!-- Edit / Save Button -->
            <button *ngIf="!isEditing() && truthSource()" (click)="enableEditMode()"
                    class="px-3 py-1.5 rounded-lg border border-[var(--app-card-border)] hover:bg-[var(--app-surface-hover)] text-[var(--app-text)] text-xs font-bold flex items-center gap-1 cursor-pointer">
              <i class="pi pi-pencil text-[11px]"></i>
              <span>Editar</span>
            </button>

            <button *ngIf="isEditing()" (click)="saveChanges()" [disabled]="isSaving()"
                    class="px-3.5 py-1.5 rounded-lg bg-blue-600 hover:bg-blue-500 text-white text-xs font-bold flex items-center gap-1.5 cursor-pointer shadow-xs disabled:opacity-50">
              <i class="pi" [ngClass]="isSaving() ? 'pi-spin pi-spinner' : 'pi-save'"></i>
              <span>{{ isSaving() ? 'Guardando...' : 'Guardar (v' + (truthSource()?.version || 1) + ')' }}</span>
            </button>

            <button *ngIf="isEditing()" (click)="cancelEdit()"
                    class="px-2.5 py-1.5 rounded-lg border border-[var(--app-card-border)] hover:bg-[var(--app-surface-hover)] text-[var(--app-muted)] text-xs cursor-pointer">
              Cancelar
            </button>

            <!-- Submit for Review Button (if Draft or Rejected) -->
            <button *ngIf="truthSource() && (truthSource()?.status === 'Draft' || truthSource()?.status === 'Rejected') && !isEditing()"
                    (click)="submitForReview()"
                    class="px-3 py-1.5 rounded-lg bg-amber-600 hover:bg-amber-500 text-white text-xs font-bold flex items-center gap-1 cursor-pointer shadow-xs">
              <i class="pi pi-send text-[11px]"></i>
              <span>Enviar a Revisión</span>
            </button>

            <!-- Approve Button (if Editorial Role) -->
            <button *ngIf="truthSource() && truthSource()?.status !== 'Approved' && !isEditing()"
                    (click)="approveTruthSource()" [disabled]="!isEditorialUser"
                    class="px-3.5 py-1.5 rounded-lg bg-emerald-600 hover:bg-emerald-500 text-white text-xs font-bold flex items-center gap-1.5 cursor-pointer shadow-xs disabled:opacity-40 disabled:cursor-not-allowed"
                    title="Aprobar TruthSource como verdad humana autorizada">
              <i class="pi pi-check"></i>
              <span>Aprobar</span>
            </button>

            <!-- Reject Button (if Editorial Role) -->
            <button *ngIf="truthSource() && truthSource()?.status === 'UnderReview' && !isEditing()"
                    (click)="openRejectModal()" [disabled]="!isEditorialUser"
                    class="px-3 py-1.5 rounded-lg bg-red-600/15 hover:bg-red-600/25 text-red-600 dark:text-red-400 border border-red-500/30 text-xs font-bold flex items-center gap-1 cursor-pointer">
              <i class="pi pi-times"></i>
              <span>Rechazar</span>
            </button>

          </div>

        </div>
      </div>

      <!-- Main Studio 2-Column Split Workspace -->
      <div class="grid grid-cols-1 lg:grid-cols-12 gap-3 flex-1 min-h-0">
        
        <!-- Left Pane: Captured Evidence Reader (5 cols on lg) -->
        <div class="lg:col-span-5 bg-[var(--app-card-bg)] border border-[var(--app-card-border)] rounded-xl flex flex-col overflow-hidden shadow-xs">
          
          <!-- Left Header -->
          <div class="px-4 py-3 border-b border-[var(--app-card-border)] bg-[var(--app-header-bg)] flex items-center justify-between">
            <div class="flex items-center gap-2">
              <i class="pi pi-paperclip text-blue-600 dark:text-blue-400 text-xs"></i>
              <h2 class="text-xs font-bold text-[var(--app-text)] uppercase tracking-wider">Evidencias Capturadas</h2>
            </div>
            <span class="px-2 py-0.5 rounded bg-[var(--app-bg)] border border-[var(--app-card-border)] font-mono text-[10px] font-bold">
              {{ contentItem()?.evidences?.length || 0 }} fuentes
            </span>
          </div>

          <!-- Evidence Selector Tabs -->
          <div *ngIf="(contentItem()?.evidences?.length || 0) > 0" class="flex border-b border-[var(--app-card-border)] bg-[var(--app-bg)] overflow-x-auto text-xs p-1 gap-1">
            <button *ngFor="let ev of contentItem()?.evidences; let i = index"
                    (click)="selectedEvidenceIndex.set(i)"
                    class="px-2.5 py-1.5 rounded-md font-medium text-left truncate max-w-[160px] transition-all cursor-pointer text-[11px]"
                    [ngClass]="selectedEvidenceIndex() === i ? 'bg-[var(--app-card-bg)] text-blue-600 dark:text-blue-400 shadow-xs font-bold' : 'text-[var(--app-muted)] hover:text-[var(--app-text)]'">
              <span class="mr-1">#{{ i + 1 }}</span>
              <span>{{ ev.title }}</span>
            </button>
          </div>

          <!-- Selected Evidence Body Reader -->
          <div *ngIf="selectedEvidence" class="flex-1 overflow-y-auto p-4 space-y-3 text-xs">
            
            <!-- Metadata box -->
            <div class="p-3 rounded-lg bg-[var(--app-bg)] border border-[var(--app-card-border)] space-y-1.5">
              <div class="flex items-center justify-between text-[11px]">
                <span class="font-bold text-[var(--app-text)]">{{ selectedEvidence.title }}</span>
                <span class="px-2 py-0.5 rounded text-[10px] font-mono border font-bold"
                      [ngClass]="selectedEvidence.status === 'Captured' ? 'bg-emerald-500/15 text-emerald-600 border-emerald-500/30' : 'bg-red-500/15 text-red-600 border-red-500/30'">
                  {{ selectedEvidence.status }}
                </span>
              </div>

              <div *ngIf="selectedEvidence.originUrl">
                <a [href]="selectedEvidence.originUrl" target="_blank" rel="noopener noreferrer"
                   class="text-blue-600 dark:text-blue-400 hover:underline flex items-center gap-1 font-mono break-all text-[10px]">
                  <i class="pi pi-external-link text-[9px]"></i>
                  <span>{{ selectedEvidence.originUrl }}</span>
                </a>
              </div>

              <div class="text-[10px] text-[var(--app-muted)] font-mono flex items-center justify-between pt-1 border-t border-[var(--app-card-border)]">
                <span class="truncate max-w-[280px]">Hash: {{ selectedEvidence.contentHash || 'N/A' }}</span>
                <span>Rol: {{ selectedEvidence.role }}</span>
              </div>
            </div>

            <!-- Extracted text reader -->
            <div class="space-y-1">
              <span class="text-[10px] font-bold uppercase tracking-wider text-[var(--app-muted)] block">Texto Fuente Extraído</span>
              <div class="p-3 rounded-lg bg-[var(--app-bg)] border border-[var(--app-card-border)] text-[var(--app-text)] font-sans text-xs leading-relaxed whitespace-pre-wrap select-text">
                {{ selectedEvidence.extractedText || selectedEvidence.rawContent || 'Sin contenido extraído.' }}
              </div>
            </div>

          </div>

          <!-- Empty Evidence State -->
          <div *ngIf="(contentItem()?.evidences?.length || 0) === 0" class="p-8 text-center text-xs text-[var(--app-muted)] my-auto">
            <i class="pi pi-paperclip text-2xl mb-2 text-[var(--app-muted)] block"></i>
            <p>No hay evidencias vinculadas a esta pieza.</p>
          </div>

        </div>

        <!-- Right Pane: TruthSource Structured Form / Reviewer (7 cols on lg) -->
        <div class="lg:col-span-7 bg-[var(--app-card-bg)] border border-[var(--app-card-border)] rounded-xl flex flex-col overflow-hidden shadow-xs">
          
          <!-- Right Header -->
          <div class="px-4 py-3 border-b border-[var(--app-card-border)] bg-[var(--app-header-bg)] flex items-center justify-between">
            <div class="flex items-center gap-2">
              <i class="pi pi-shield text-indigo-600 dark:text-indigo-400 text-xs"></i>
              <h2 class="text-xs font-bold text-[var(--app-text)] uppercase tracking-wider">Verdad Factual y Guardrails</h2>
            </div>
            <span *ngIf="isEditing()" class="px-2 py-0.5 rounded bg-blue-500/15 text-blue-600 dark:text-blue-400 font-mono text-[10px] font-bold">
              MODO EDICIÓN
            </span>
          </div>

          <!-- Form Content -->
          <div *ngIf="truthSource()" class="flex-1 overflow-y-auto p-4 space-y-4 text-xs">
            
            <!-- Summary -->
            <div class="space-y-1.5">
              <label class="font-bold text-[var(--app-text)] text-xs flex items-center justify-between">
                <span>Resumen Factual (Summary)</span>
                <span class="text-[10px] text-[var(--app-muted)]">Síntesis factual verificada</span>
              </label>
              <textarea *ngIf="isEditing()" [(ngModel)]="editModel.summary" rows="3"
                        class="w-full px-3 py-2 rounded-lg bg-[var(--app-bg)] border border-[var(--app-card-border)] text-[var(--app-text)] focus:border-blue-500 focus:outline-hidden leading-relaxed"></textarea>
              <div *ngIf="!isEditing()" class="p-3 rounded-lg bg-[var(--app-bg)] border border-[var(--app-card-border)] text-[var(--app-text)] leading-relaxed">
                {{ truthSource()?.summary }}
              </div>
            </div>

            <!-- Key Ideas -->
            <div class="space-y-1.5">
              <div class="flex items-center justify-between">
                <label class="font-bold text-[var(--app-text)] text-xs">Ideas Clave (Key Ideas)</label>
                <button *ngIf="isEditing()" (click)="addKeyIdea()" class="text-blue-600 hover:underline text-[11px] font-semibold cursor-pointer">
                  + Agregar Idea
                </button>
              </div>

              <!-- Edit list -->
              <div *ngIf="isEditing()" class="space-y-2">
                <div *ngFor="let idea of editModel.keyIdeas; let i = index; trackBy: trackByIndex" class="flex items-center gap-1.5">
                  <input type="text" [(ngModel)]="editModel.keyIdeas[i]"
                         class="flex-1 px-3 py-1.5 rounded-lg bg-[var(--app-bg)] border border-[var(--app-card-border)] text-[var(--app-text)] focus:border-blue-500 focus:outline-hidden text-xs" />
                  <button (click)="removeKeyIdea(i)" class="p-1.5 text-[var(--app-muted)] hover:text-red-500 cursor-pointer">
                    <i class="pi pi-trash text-xs"></i>
                  </button>
                </div>
              </div>

              <!-- View list -->
              <ul *ngIf="!isEditing()" class="space-y-1.5">
                <li *ngFor="let idea of truthSource()?.keyIdeas" class="p-2.5 rounded-lg bg-[var(--app-bg)] border border-[var(--app-card-border)] text-[var(--app-text)] flex items-start gap-2">
                  <i class="pi pi-check text-emerald-500 text-[10px] mt-0.5"></i>
                  <span>{{ idea }}</span>
                </li>
              </ul>
            </div>

            <!-- Verifiable Claims -->
            <div class="space-y-1.5">
              <div class="flex items-center justify-between">
                <label class="font-bold text-[var(--app-text)] text-xs">Afirmaciones Verificables (Claims Con Cita)</label>
                <button *ngIf="isEditing()" (click)="addClaim()" class="text-blue-600 hover:underline text-[11px] font-semibold cursor-pointer">
                  + Agregar Claim
                </button>
              </div>

              <!-- Edit claims -->
              <div *ngIf="isEditing()" class="space-y-2">
                <div *ngFor="let c of editModel.verifiableClaims; let i = index" class="p-2.5 rounded-lg bg-[var(--app-bg)] border border-[var(--app-card-border)] space-y-2">
                  <div class="flex items-center justify-between">
                    <span class="text-[10px] font-bold uppercase text-[var(--app-muted)]">Claim #{{ i + 1 }}</span>
                    <button (click)="removeClaim(i)" class="text-[var(--app-muted)] hover:text-red-500 cursor-pointer">
                      <i class="pi pi-trash text-xs"></i>
                    </button>
                  </div>
                  <input type="text" [(ngModel)]="c.claim" placeholder="Afirmación factual..."
                         class="w-full px-2.5 py-1.5 rounded bg-[var(--app-card-bg)] border border-[var(--app-card-border)] text-[var(--app-text)] text-xs" />
                  <div class="grid grid-cols-2 gap-2">
                    <input type="text" [(ngModel)]="c.sourceCitation" placeholder="Cita de fuente (ej. El País 2026)"
                           class="w-full px-2.5 py-1.5 rounded bg-[var(--app-card-bg)] border border-[var(--app-card-border)] text-[var(--app-text)] text-[11px]" />
                    <select [(ngModel)]="c.evidenceId"
                            class="w-full px-2.5 py-1.5 rounded bg-[var(--app-card-bg)] border border-[var(--app-card-border)] text-[var(--app-text)] text-[11px]">
                      <option *ngFor="let ev of contentItem()?.evidences" [value]="ev.id">{{ ev.title }}</option>
                    </select>
                  </div>
                </div>
              </div>

              <!-- View claims -->
              <div *ngIf="!isEditing()" class="space-y-2">
                <div *ngFor="let c of truthSource()?.verifiableClaims" class="p-2.5 rounded-lg bg-[var(--app-bg)] border border-[var(--app-card-border)] space-y-1">
                  <p class="font-medium text-[var(--app-text)]">"{{ c.claim }}"</p>
                  <div class="flex items-center justify-between text-[10px] text-[var(--app-muted)] font-mono">
                    <span>Fuente: {{ c.sourceCitation }}</span>
                    <span>Ref Evidencia: {{ c.evidenceId | slice:0:8 }}</span>
                  </div>
                </div>
              </div>
            </div>

            <!-- Guardrails & Do Not Say Constraints -->
            <div class="space-y-1.5">
              <div class="flex items-center justify-between">
                <label class="font-bold text-red-600 dark:text-red-400 text-xs">Restricciones 'Do Not Say' (Guardrails)</label>
                <button *ngIf="isEditing()" (click)="addConstraint()" class="text-red-600 hover:underline text-[11px] font-semibold cursor-pointer">
                  + Agregar Restricción
                </button>
              </div>

              <div *ngIf="isEditing()" class="space-y-1.5">
                <div *ngFor="let con of editModel.doNotSayConstraints; let i = index; trackBy: trackByIndex" class="flex items-center gap-1.5">
                  <input type="text" [(ngModel)]="editModel.doNotSayConstraints[i]"
                         class="flex-1 px-3 py-1.5 rounded-lg bg-[var(--app-bg)] border border-red-500/30 text-[var(--app-text)] focus:border-red-500 focus:outline-hidden text-xs" />
                  <button (click)="removeConstraint(i)" class="p-1.5 text-[var(--app-muted)] hover:text-red-500 cursor-pointer">
                    <i class="pi pi-trash text-xs"></i>
                  </button>
                </div>
              </div>

              <ul *ngIf="!isEditing()" class="space-y-1">
                <li *ngFor="let con of truthSource()?.doNotSayConstraints" class="p-2 rounded-lg bg-red-500/5 border border-red-500/20 text-[var(--app-text)] flex items-start gap-2">
                  <i class="pi pi-ban text-red-500 text-[10px] mt-0.5"></i>
                  <span>{{ con }}</span>
                </li>
              </ul>
            </div>

            <!-- Possible Angles & Localization -->
            <div class="grid grid-cols-1 sm:grid-cols-2 gap-3 pt-2 border-t border-[var(--app-card-border)]">
              
              <!-- Angles -->
              <div class="space-y-1.5">
                <div class="flex items-center justify-between">
                  <label class="font-bold text-[var(--app-text)] text-[11px]">Ángulos Editoriales</label>
                  <button *ngIf="isEditing()" (click)="addAngle()" class="text-blue-600 hover:underline text-[10px] font-semibold cursor-pointer">+ Agregar</button>
                </div>

                <div *ngIf="isEditing()" class="space-y-1.5">
                  <div *ngFor="let ang of editModel.possibleAngles; let i = index; trackBy: trackByIndex" class="flex items-center gap-1">
                    <input type="text" [(ngModel)]="editModel.possibleAngles[i]"
                           class="flex-1 px-2.5 py-1 rounded bg-[var(--app-bg)] border border-[var(--app-card-border)] text-[var(--app-text)] text-[11px]" />
                    <button (click)="removeAngle(i)" class="p-1 text-[var(--app-muted)] hover:text-red-500 cursor-pointer">
                      <i class="pi pi-trash text-[10px]"></i>
                    </button>
                  </div>
                </div>

                <ul *ngIf="!isEditing()" class="space-y-1 text-[11px]">
                  <li *ngFor="let ang of truthSource()?.possibleAngles" class="p-1.5 rounded bg-[var(--app-bg)] border border-[var(--app-card-border)] text-[var(--app-text)]">
                    • {{ ang }}
                  </li>
                </ul>
              </div>

              <!-- Localization -->
              <div class="space-y-1.5">
                <label class="font-bold text-[var(--app-text)] text-[11px]">Notas de Localización</label>
                <textarea *ngIf="isEditing()" [(ngModel)]="editModel.localizationNotes" rows="2"
                          class="w-full px-2.5 py-1.5 rounded bg-[var(--app-bg)] border border-[var(--app-card-border)] text-[var(--app-text)] text-[11px]"></textarea>
                <div *ngIf="!isEditing()" class="p-2 rounded bg-[var(--app-bg)] border border-[var(--app-card-border)] text-[var(--app-text)] text-[11px]">
                  {{ truthSource()?.localizationNotes }}
                </div>
              </div>

            </div>

          </div>

          <!-- Empty Studio State -->
          <div *ngIf="!truthSource()" class="p-12 text-center text-xs text-[var(--app-muted)] my-auto space-y-3">
            <div class="w-12 h-12 rounded-full bg-[var(--app-bg)] flex items-center justify-center mx-auto text-indigo-500">
              <i class="pi pi-sparkles text-xl"></i>
            </div>
            <p class="font-bold text-sm text-[var(--app-text)]">Borrador de TruthSource no generado</p>
            <p class="max-w-sm mx-auto text-[11px]">Haz clic en 'Generar IA' para sintetizar hechos verificables y restricciones desde el bundle de evidencias.</p>
            <button (click)="triggerAiDraft()" [disabled]="isGeneratingAi() || !hasUsableEvidence"
                    class="px-4 py-2 rounded-lg bg-indigo-600 hover:bg-indigo-500 text-white font-bold text-xs cursor-pointer shadow-xs disabled:opacity-50">
              <i class="pi pi-sparkles mr-1.5"></i> Sintetizar con IA
            </button>
            <p *ngIf="!hasUsableEvidence" class="text-[10px] text-amber-500">
              Requiere al menos 1 evidencia capturada con éxito.
            </p>
          </div>

        </div>

      </div>

      <!-- 409 Optimistic Concurrency Conflict Dialog -->
      <div *ngIf="isConflictDialogOpen()" class="fixed inset-0 z-50 overflow-y-auto flex items-center justify-center p-4">
        <div class="fixed inset-0 bg-slate-900/60 backdrop-blur-xs"></div>
        <div class="relative w-full max-w-md bg-[var(--app-card-bg)] border border-amber-500/40 rounded-xl shadow-2xl p-5 space-y-4 z-10 animate-scale-in">
          <div class="flex items-center gap-3">
            <div class="w-10 h-10 rounded-full bg-amber-500/20 text-amber-600 flex items-center justify-center shrink-0">
              <i class="pi pi-exclamation-triangle text-lg"></i>
            </div>
            <div>
              <h3 class="text-sm font-bold text-[var(--app-text)]">Conflicto de Edición Concurrente (409)</h3>
              <p class="text-xs text-[var(--app-muted)]">El TruthSource fue modificado por otro operador en la versión v{{ conflictServerVersion() }}.</p>
            </div>
          </div>
          <p class="text-xs text-[var(--app-text)] leading-relaxed">
            Para evitar sobreescribir los cambios de otro operador, recarga la última versión del servidor antes de aplicar nuevas modificaciones.
          </p>
          <div class="flex items-center justify-end gap-2 pt-2 border-t border-[var(--app-card-border)]">
            <button (click)="isConflictDialogOpen.set(false)" class="px-3 py-1.5 rounded-lg border border-[var(--app-card-border)] text-xs text-[var(--app-muted)] hover:text-[var(--app-text)] cursor-pointer">
              Cerrar
            </button>
            <button (click)="reloadAndReconcile()" class="px-4 py-1.5 rounded-lg bg-amber-600 hover:bg-amber-500 text-white text-xs font-bold cursor-pointer shadow-xs">
              Recargar Última Versión
            </button>
          </div>
        </div>
      </div>

      <!-- Rejection Modal -->
      <div *ngIf="isRejectModalOpen()" class="fixed inset-0 z-50 overflow-y-auto flex items-center justify-center p-4">
        <div (click)="closeRejectModal()" class="fixed inset-0 bg-slate-900/50 backdrop-blur-xs"></div>
        <div class="relative w-full max-w-md bg-[var(--app-card-bg)] border border-[var(--app-card-border)] rounded-xl shadow-2xl p-5 space-y-4 z-10 animate-scale-in">
          <div class="flex items-center justify-between border-b border-[var(--app-card-border)] pb-2">
            <h3 class="text-sm font-bold text-red-600 dark:text-red-400 flex items-center gap-1.5">
              <i class="pi pi-times-circle"></i>
              <span>Rechazar TruthSource</span>
            </h3>
            <button (click)="closeRejectModal()" class="p-1 text-[var(--app-muted)] hover:text-[var(--app-text)] cursor-pointer">
              <i class="pi pi-times text-xs"></i>
            </button>
          </div>
          <form (ngSubmit)="submitReject()" class="space-y-3 text-xs">
            <div class="space-y-1">
              <label class="font-bold text-[var(--app-text)]">Motivo Obligatorio del Rechazo *</label>
              <textarea [(ngModel)]="rejectionReason" name="rejectionReason" rows="3" required
                        placeholder="Explica qué hechos deben verificarse o qué guardrails deben añadirse..."
                        class="w-full px-3 py-2 rounded-lg bg-[var(--app-bg)] border border-[var(--app-card-border)] text-[var(--app-text)] focus:border-red-500 focus:outline-hidden"></textarea>
            </div>
            <div *ngIf="rejectError()" class="p-2 rounded bg-red-500/10 text-red-600 text-[11px]">
              {{ rejectError() }}
            </div>
            <div class="flex items-center justify-end gap-2 pt-2 border-t border-[var(--app-card-border)]">
              <button type="button" (click)="closeRejectModal()" class="px-3 py-1.5 rounded-lg border border-[var(--app-card-border)] text-[var(--app-muted)] cursor-pointer">
                Cancelar
              </button>
              <button type="submit" [disabled]="!rejectionReason || isRejecting()"
                      class="px-4 py-1.5 rounded-lg bg-red-600 hover:bg-red-500 text-white font-bold cursor-pointer disabled:opacity-50">
                <span>{{ isRejecting() ? 'Rechazando...' : 'Confirmar Rechazo' }}</span>
              </button>
            </div>
          </form>
        </div>
      </div>

      <!-- Version History Drawer -->
      <div *ngIf="isHistoryDrawerOpen()" class="fixed inset-0 z-50 overflow-hidden flex justify-end">
        <div (click)="closeHistoryDrawer()" class="fixed inset-0 bg-slate-900/40 backdrop-blur-xs"></div>
        <div class="relative w-full max-w-lg bg-[var(--app-card-bg)] border-l border-[var(--app-card-border)] shadow-2xl flex flex-col h-full z-10 animate-slide-in">
          <div class="p-4 border-b border-[var(--app-card-border)] flex items-center justify-between bg-[var(--app-header-bg)]">
            <h3 class="text-sm font-bold text-[var(--app-text)] flex items-center gap-1.5">
              <i class="pi pi-history text-indigo-500"></i>
              <span>Historial de Versiones de TruthSource</span>
            </h3>
            <button (click)="closeHistoryDrawer()" class="p-1 text-[var(--app-muted)] hover:text-[var(--app-text)] cursor-pointer">
              <i class="pi pi-times text-xs"></i>
            </button>
          </div>
          <div class="flex-1 overflow-y-auto p-4 space-y-3 text-xs">
            <div *ngIf="versions().length === 0" class="text-center text-[var(--app-muted)] py-8">
              No hay versiones históricas archivadas aún.
            </div>
            <div *ngFor="let v of versions()" class="p-3 rounded-lg bg-[var(--app-bg)] border border-[var(--app-card-border)] space-y-2">
              <div class="flex items-center justify-between text-[11px]">
                <span class="font-bold text-[var(--app-text)]">Versión {{ v.versionNumber }}</span>
                <span class="text-[10px] font-mono text-[var(--app-muted)]">{{ v.createdAtUtc | date:'yyyy-MM-dd HH:mm' }} UTC</span>
              </div>
              <p class="text-[11px] text-[var(--app-muted)] italic">
                {{ v.changeSummary || 'Sin resumen de cambio.' }}
              </p>
              <div class="text-[10px] text-[var(--app-muted)] font-mono flex items-center justify-between">
                <span>Por: {{ v.createdByEmail }}</span>
                <span>{{ v.supportingEvidenceIds.length }} fuentes vinculadas</span>
              </div>
            </div>
          </div>
        </div>
      </div>

    </div>
  `
})
export class TruthSourceReviewStudioComponent implements OnInit {
  private readonly api = inject(ApiService);
  private readonly auth = inject(AuthService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly cdr = inject(ChangeDetectorRef);

  contentItemId!: string;
  readonly contentItem = signal<ContentItemDetailDto | null>(null);
  readonly truthSource = signal<TruthSourceDto | null>(null);
  readonly selectedEvidenceIndex = signal<number>(0);

  readonly isLoading = signal<boolean>(true);
  readonly errorMessage = signal<string | null>(null);
  readonly isEditing = signal<boolean>(false);
  readonly isSaving = signal<boolean>(false);
  readonly isGeneratingAi = signal<boolean>(false);

  // Conflict Dialog
  readonly isConflictDialogOpen = signal<boolean>(false);
  readonly conflictServerVersion = signal<number>(1);

  // Rejection Modal
  readonly isRejectModalOpen = signal<boolean>(false);
  rejectionReason = '';
  readonly isRejecting = signal<boolean>(false);
  readonly rejectError = signal<string | null>(null);

  // History Drawer
  readonly isHistoryDrawerOpen = signal<boolean>(false);
  readonly versions = signal<TruthSourceVersionDto[]>([]);

  // Edit Model
  editModel: {
    summary: string;
    keyIdeas: string[];
    verifiableClaims: VerifiableClaimDto[];
    evidenceReferences: string[];
    riskNotes: string;
    doNotSayConstraints: string[];
    possibleAngles: string[];
    localizationNotes: string;
  } = {
    summary: '',
    keyIdeas: [],
    verifiableClaims: [],
    evidenceReferences: [],
    riskNotes: '',
    doNotSayConstraints: [],
    possibleAngles: [],
    localizationNotes: ''
  };

  get isEditorialUser(): boolean {
    const user = this.auth.currentUser();
    return user ? user.roles.includes('EDITORIAL') || user.isOwner : true;
  }

  get hasUsableEvidence(): boolean {
    const cur = this.contentItem();
    if (!cur || !cur.evidences) return false;
    return cur.evidences.some(e => e.status === 'Captured');
  }

  get selectedEvidence(): ContentItemEvidenceDto | null {
    const cur = this.contentItem();
    if (!cur || cur.evidences.length === 0) return null;
    return cur.evidences[this.selectedEvidenceIndex()] || cur.evidences[0];
  }

  ngOnInit() {
    this.route.paramMap.subscribe(params => {
      const id = params.get('id');
      if (id) {
        this.contentItemId = id;
        this.loadData();
      } else {
        this.isLoading.set(false);
        this.errorMessage.set('Identificador de pieza de contenido inválido o ausente.');
        this.cdr.markForCheck();
      }
    });
  }

  loadData() {
    this.isLoading.set(true);
    this.errorMessage.set(null);
    this.cdr.markForCheck();

    this.api.getContentItemDetail(this.contentItemId).subscribe({
      next: (item) => {
        this.contentItem.set(item);
        this.truthSource.set(item.truthSource || null);
        this.isLoading.set(false);
        this.cdr.markForCheck();
      },
      error: (err) => {
        this.isLoading.set(false);
        this.errorMessage.set(err.status === 404
          ? 'No se encontró la pieza de contenido o TruthSource correspondiente.'
          : (err.error?.message || err.error?.error || `Error al cargar TruthSource Review Studio (HTTP ${err.status || 'Error'}).`));
        this.cdr.markForCheck();
      }
    });
  }

  triggerAiDraft() {
    this.isGeneratingAi.set(true);
    this.cdr.markForCheck();

    this.api.generateAiDraft(this.contentItemId).subscribe({
      next: (ts) => {
        this.isGeneratingAi.set(false);
        this.truthSource.set(ts);
        const cur = this.contentItem();
        if (cur) {
          this.contentItem.set({ ...cur, truthSource: ts });
        }
        this.cdr.markForCheck();
      },
      error: () => {
        this.isGeneratingAi.set(false);
        this.cdr.markForCheck();
      }
    });
  }

  enableEditMode() {
    const ts = this.truthSource();
    if (!ts) return;
    this.editModel = {
      summary: ts.summary,
      keyIdeas: [...ts.keyIdeas],
      verifiableClaims: ts.verifiableClaims.map(c => ({ ...c })),
      evidenceReferences: [...ts.evidenceReferences],
      riskNotes: ts.riskNotes,
      doNotSayConstraints: [...ts.doNotSayConstraints],
      possibleAngles: [...ts.possibleAngles],
      localizationNotes: ts.localizationNotes
    };
    this.isEditing.set(true);
    this.cdr.markForCheck();
  }

  cancelEdit() {
    this.isEditing.set(false);
    this.cdr.markForCheck();
  }

  saveChanges() {
    const ts = this.truthSource();
    if (!ts) return;
    this.isSaving.set(true);
    this.cdr.markForCheck();

    const req: SaveTruthSourceRequest = {
      summary: this.editModel.summary,
      keyIdeas: this.editModel.keyIdeas,
      verifiableClaims: this.editModel.verifiableClaims,
      evidenceReferences: this.editModel.evidenceReferences,
      riskNotes: this.editModel.riskNotes,
      doNotSayConstraints: this.editModel.doNotSayConstraints,
      possibleAngles: this.editModel.possibleAngles,
      localizationNotes: this.editModel.localizationNotes,
      expectedVersion: ts.version,
      changeSummary: 'Edición manual desde TruthSource Review Studio'
    };

    this.api.saveTruthSource(this.contentItemId, req).subscribe({
      next: (updated) => {
        this.isSaving.set(false);
        this.truthSource.set(updated);
        const cur = this.contentItem();
        if (cur) {
          this.contentItem.set({ ...cur, truthSource: updated });
        }
        this.isEditing.set(false);
        this.cdr.markForCheck();
      },
      error: (err) => {
        this.isSaving.set(false);
        if (err.status === 409) {
          this.conflictServerVersion.set(err.error?.currentVersion || ts.version + 1);
          this.isConflictDialogOpen.set(true);
        }
        this.cdr.markForCheck();
      }
    });
  }

  reloadAndReconcile() {
    this.isConflictDialogOpen.set(false);
    this.isEditing.set(false);
    this.loadData();
  }

  submitForReview() {
    this.api.submitTruthSourceReview(this.contentItemId).subscribe({
      next: (updated) => {
        this.truthSource.set(updated);
        const cur = this.contentItem();
        if (cur) {
          this.contentItem.set({ ...cur, truthSource: updated });
        }
        this.cdr.markForCheck();
      }
    });
  }

  approveTruthSource() {
    this.api.approveTruthSource(this.contentItemId).subscribe({
      next: (updated) => {
        this.truthSource.set(updated);
        const cur = this.contentItem();
        if (cur) {
          this.contentItem.set({ ...cur, truthSource: updated, stage: 'TruthSourceApproved' });
        }
        this.cdr.markForCheck();
      }
    });
  }

  openRejectModal() {
    this.rejectionReason = '';
    this.rejectError.set(null);
    this.isRejectModalOpen.set(true);
    this.cdr.markForCheck();
  }

  closeRejectModal() {
    this.isRejectModalOpen.set(false);
    this.cdr.markForCheck();
  }

  submitReject() {
    if (!this.rejectionReason.trim()) return;
    this.isRejecting.set(true);
    this.rejectError.set(null);
    this.cdr.markForCheck();

    const req: RejectTruthSourceRequest = {
      reason: this.rejectionReason.trim()
    };

    this.api.rejectTruthSource(this.contentItemId, req).subscribe({
      next: (updated) => {
        this.isRejecting.set(false);
        this.truthSource.set(updated);
        const cur = this.contentItem();
        if (cur) {
          this.contentItem.set({ ...cur, truthSource: updated });
        }
        this.closeRejectModal();
        this.cdr.markForCheck();
      },
      error: (err) => {
        this.isRejecting.set(false);
        this.rejectError.set(err.error?.message || err.error?.error || 'Error al rechazar.');
        this.cdr.markForCheck();
      }
    });
  }

  openHistoryDrawer() {
    this.api.getTruthSourceVersions(this.contentItemId).subscribe({
      next: (vers) => {
        this.versions.set(vers);
        this.isHistoryDrawerOpen.set(true);
        this.cdr.markForCheck();
      }
    });
  }

  closeHistoryDrawer() {
    this.isHistoryDrawerOpen.set(false);
    this.cdr.markForCheck();
  }

  // Edit helpers
  addKeyIdea() {
    this.editModel.keyIdeas.push('Nueva idea clave');
  }

  removeKeyIdea(index: number) {
    this.editModel.keyIdeas.splice(index, 1);
  }

  addClaim() {
    const cur = this.contentItem();
    const evId = cur?.evidences[0]?.id || '';
    this.editModel.verifiableClaims.push({
      claim: '',
      sourceCitation: '',
      evidenceId: evId
    });
  }

  removeClaim(index: number) {
    this.editModel.verifiableClaims.splice(index, 1);
  }

  addConstraint() {
    this.editModel.doNotSayConstraints.push('Restricción / no decir');
  }

  removeConstraint(index: number) {
    this.editModel.doNotSayConstraints.splice(index, 1);
  }

  addAngle() {
    this.editModel.possibleAngles.push('Nuevo ángulo editorial');
  }

  removeAngle(index: number) {
    this.editModel.possibleAngles.splice(index, 1);
  }

  trackByIndex(index: number): number {
    return index;
  }
}
