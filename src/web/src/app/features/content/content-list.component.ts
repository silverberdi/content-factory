import { Component, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { ApiService, ChannelDto, ContentItemDto, CreateContentItemRequest } from '../../core/api.service';
import { PageHeaderComponent } from '../../shared/layout/page-header.component';
import { PageToolbarComponent } from '../../shared/layout/page-toolbar.component';

@Component({
  selector: 'app-content-list',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule, PageHeaderComponent, PageToolbarComponent],
  host: { class: 'block w-full' },
  template: `
    <div class="cf-page-container space-y-4 text-xs">
      
      <!-- Canonical Page Header -->
      <app-page-header 
        title="Workspace de Contenido" 
        subtitle="Gestión operativa de piezas editoriales, proveniencia de evidencia, ideas y guiones"
        [badge]="items.length"
        badgeSeverity="info">
        <div actions class="flex items-center gap-2">
          <button (click)="openCreateModal()" 
                  class="cf-btn-primary">
            <i class="pi pi-plus text-xs"></i>
            <span>Nueva Pieza</span>
          </button>
        </div>
      </app-page-header>

      <!-- Canonical Page Toolbar -->
      <app-page-toolbar>
        <div start class="flex items-center gap-2 flex-wrap flex-1">
          <!-- Search Input -->
          <div class="relative min-w-[220px] flex-1 sm:max-w-xs">
            <input type="text" [(ngModel)]="searchQuery" (ngModelChange)="onFilterChange()"
                   placeholder="Buscar por título o slug..."
                   class="cf-toolbar-control w-full pl-8" />
            <i class="pi pi-search absolute left-2.5 top-2.5 text-[var(--app-muted)] text-xs"></i>
          </div>

          <!-- Channel Filter -->
          <select [(ngModel)]="selectedChannelId" (ngModelChange)="onFilterChange()"
                  class="cf-toolbar-control min-w-[150px]">
            <option value="">Todos los Canales</option>
            <option *ngFor="let ch of channels" [value]="ch.id">{{ ch.name }}</option>
          </select>

          <!-- Stage Filter -->
          <select [(ngModel)]="selectedStage" (ngModelChange)="onFilterChange()"
                  class="cf-toolbar-control min-w-[160px]">
            <option value="">Todas las Fases</option>
            <option value="DraftingEvidence">Drafting Evidence</option>
            <option value="TruthSourceApproved">TruthSource Aprobado</option>
            <option value="IdeaDrafting">Idea Drafting</option>
            <option value="IdeaSelected">Idea Seleccionada</option>
            <option value="ScriptDrafted">Script Borrador</option>
            <option value="ScriptUnderReview">Script en Revisión</option>
            <option value="ScriptApproved">Script Aprobado</option>
            <option value="Published">Publicado</option>
          </select>
        </div>
      </app-page-toolbar>

      <!-- Content Items Table / List -->
      <div class="cf-card overflow-hidden">
        
        <div *ngIf="isLoading" class="p-8 text-center text-xs text-[var(--app-muted)]">
          <i class="pi pi-spin pi-spinner text-lg mb-2 block"></i>
          <span>Cargando piezas de contenido...</span>
        </div>

        <div *ngIf="!isLoading && items.length === 0" class="p-12 text-center text-xs text-[var(--app-muted)] space-y-3">
          <div class="w-12 h-12 rounded-full bg-[var(--app-bg)] flex items-center justify-center mx-auto text-[var(--app-muted)]">
            <i class="pi pi-inbox text-xl"></i>
          </div>
          <p class="font-medium text-[var(--app-text)]">No se encontraron piezas de contenido</p>
          <p class="text-[11px] max-w-sm mx-auto">Promueve candidatos desde el Discovery Triage o crea una nueva pieza directamente.</p>
          <button (click)="openCreateModal()" class="px-3 py-1.5 rounded-lg bg-blue-600 text-white font-bold text-xs cursor-pointer">
            <i class="pi pi-plus mr-1"></i> Crear Pieza
          </button>
        </div>

        <div *ngIf="!isLoading && items.length > 0" class="overflow-x-auto">
          <table class="cf-table">
            <thead>
              <tr>
                <th>Pieza de Contenido</th>
                <th>Canal</th>
                <th>Fase Editorial</th>
                <th class="text-center">Evidencias</th>
                <th>TruthSource</th>
                <th>Actualización</th>
                <th class="text-right">Acciones</th>
              </tr>
            </thead>
            <tbody>
              <tr *ngFor="let item of items" class="hover:bg-[var(--app-surface-hover)] transition-colors group">
                
                <!-- Title & Slug -->
                <td class="py-3 px-4">
                  <div class="flex flex-col">
                    <a [routerLink]="['/content/items', item.id]" 
                       class="font-bold text-[var(--app-text)] group-hover:text-blue-600 dark:group-hover:text-blue-400 transition-colors">
                      {{ item.title }}
                    </a>
                    <span class="text-[10px] font-mono text-[var(--app-muted)] truncate max-w-xs">
                      /{{ item.slug }} (v{{ item.version }})
                    </span>
                  </div>
                </td>

                <!-- Channel -->
                <td class="py-3 px-3">
                  <span class="px-2 py-0.5 rounded text-[10px] font-medium bg-blue-500/10 text-blue-600 dark:text-blue-400 border border-blue-500/20">
                    {{ item.channelName || 'Canal' }}
                  </span>
                </td>

                <!-- Stage Chip -->
                <td class="py-3 px-3">
                  <span class="px-2 py-0.5 rounded text-[10px] font-bold uppercase tracking-wider border font-mono"
                        [ngClass]="{
                          'bg-amber-500/15 text-amber-600 dark:text-amber-400 border-amber-500/30': item.stage === 'DraftingEvidence' || item.stage === 'ScriptDrafted',
                          'bg-indigo-500/15 text-indigo-600 dark:text-indigo-400 border-indigo-500/30': item.stage === 'TruthSourceApproved',
                          'bg-purple-500/15 text-purple-600 dark:text-purple-400 border-purple-500/30': item.stage === 'IdeaDrafting' || item.stage === 'ScriptUnderReview',
                          'bg-emerald-500/15 text-emerald-600 dark:text-emerald-400 border-emerald-500/30': item.stage === 'IdeaSelected' || item.stage === 'ScriptApproved',
                          'bg-slate-500/15 text-slate-500 border-slate-500/30': item.stage === 'Published'
                        }">
                    {{ item.stage }}
                  </span>
                </td>

                <!-- Evidences Count -->
                <td class="py-3 px-3 text-center font-mono">
                  <span class="inline-flex items-center gap-1 px-1.5 py-0.5 rounded bg-[var(--app-bg)] border border-[var(--app-card-border)] text-[11px]"
                        [ngClass]="item.evidenceCount > 0 ? 'text-[var(--app-text)] font-semibold' : 'text-amber-500'">
                    <i class="pi pi-paperclip text-[10px]"></i>
                    {{ item.evidenceCount }}
                  </span>
                </td>

                <!-- TruthSource Status -->
                <td class="py-3 px-3">
                  <div class="flex items-center gap-1.5">
                    <span *ngIf="item.truthSourceStatus" 
                          class="px-2 py-0.5 rounded text-[10px] font-semibold border font-mono"
                          [ngClass]="{
                            'bg-emerald-500/15 text-emerald-600 dark:text-emerald-400 border-emerald-500/30': item.truthSourceStatus === 'Approved',
                            'bg-amber-500/15 text-amber-600 dark:text-amber-400 border-amber-500/30': item.truthSourceStatus === 'UnderReview',
                            'bg-blue-500/15 text-blue-600 dark:text-blue-400 border-blue-500/30': item.truthSourceStatus === 'Draft',
                            'bg-red-500/15 text-red-600 dark:text-red-400 border-red-500/30': item.truthSourceStatus === 'Rejected'
                          }">
                      {{ item.truthSourceStatus }} (v{{ item.truthSourceVersion || 1 }})
                    </span>
                    <span *ngIf="!item.truthSourceStatus" class="text-[10px] text-[var(--app-muted)] italic">
                      Sin TruthSource
                    </span>
                  </div>
                </td>

                <!-- Timestamp -->
                <td class="py-3 px-3 text-[11px] text-[var(--app-muted)] font-mono">
                  {{ item.updatedAtUtc | date:'yyyy-MM-dd HH:mm' }}
                </td>

                <!-- Actions -->
                <td class="py-3 px-4 text-right">
                  <div class="flex items-center justify-end gap-1.5">
                    <a [routerLink]="['/content/items', item.id, 'script']" 
                       class="px-2 py-1 rounded-md bg-blue-500/10 hover:bg-blue-500/20 text-blue-600 dark:text-blue-400 border border-blue-500/20 font-semibold text-[11px] transition-colors"
                       title="Abrir Script Studio">
                      <i class="pi pi-file-edit mr-1 text-[10px]"></i> Guión
                    </a>
                    <a [routerLink]="['/content/items', item.id, 'ideas']" 
                       class="px-2 py-1 rounded-md bg-purple-500/10 hover:bg-purple-500/20 text-purple-600 dark:text-purple-400 border border-purple-500/20 font-semibold text-[11px] transition-colors"
                       title="Abrir Matriz de Ideas">
                      <i class="pi pi-lightbulb mr-1 text-[10px]"></i> Ideas
                    </a>
                    <a [routerLink]="['/content/items', item.id]" 
                       class="px-2 py-1 rounded-md border border-[var(--app-card-border)] hover:bg-[var(--app-surface-hover)] text-[var(--app-text)] font-medium text-[11px] transition-colors"
                       title="Ver Detalles y Evidencias">
                      <i class="pi pi-eye mr-1 text-[10px]"></i> Detalle
                    </a>
                  </div>
                </td>

              </tr>
            </tbody>
          </table>
        </div>
      </div>

      <!-- Create ContentItem Modal -->
      <div *ngIf="isCreateModalOpen" class="fixed inset-0 z-50 overflow-y-auto flex items-center justify-center p-4">
        <div (click)="closeCreateModal()" class="fixed inset-0 bg-slate-900/50 dark:bg-black/70 backdrop-blur-xs transition-opacity"></div>
        <div class="relative w-full max-w-md bg-[var(--app-card-bg)] border border-[var(--app-card-border)] rounded-xl shadow-2xl overflow-hidden flex flex-col z-10 animate-scale-in">
          <div class="px-5 py-4 border-b border-[var(--app-card-border)] flex items-center justify-between bg-[var(--app-header-bg)]">
            <h3 class="text-sm font-bold text-[var(--app-text)]">Crear Nueva Pieza de Contenido</h3>
            <button (click)="closeCreateModal()" class="p-1 rounded text-[var(--app-muted)] hover:text-[var(--app-text)] cursor-pointer">
              <i class="pi pi-times text-xs"></i>
            </button>
          </div>
          <form (ngSubmit)="submitCreate()" class="p-5 space-y-4 text-xs">
            <div class="space-y-1">
              <label class="font-bold text-[var(--app-text)]">Canal Destino *</label>
              <select [(ngModel)]="newChannelId" name="channelId" required
                      class="w-full px-3 py-2 rounded-lg bg-[var(--app-bg)] border border-[var(--app-card-border)] text-[var(--app-text)] focus:border-blue-500 focus:outline-hidden">
                <option *ngFor="let ch of channels" [value]="ch.id">{{ ch.name }} ({{ ch.language }})</option>
              </select>
            </div>
            <div class="space-y-1">
              <label class="font-bold text-[var(--app-text)]">Título de la Pieza *</label>
              <input type="text" [(ngModel)]="newTitle" name="title" required
                     placeholder="Ej: 3 Formas de Optimizar Flujos de Oficina con Modelos Locales"
                     class="w-full px-3 py-2 rounded-lg bg-[var(--app-bg)] border border-[var(--app-card-border)] text-[var(--app-text)] focus:border-blue-500 focus:outline-hidden" />
            </div>
            <div *ngIf="createError" class="p-2.5 rounded-lg bg-red-500/10 border border-red-500/30 text-red-600 dark:text-red-400">
              {{ createError }}
            </div>
            <div class="flex items-center justify-end gap-2 pt-2 border-t border-[var(--app-card-border)]">
              <button type="button" (click)="closeCreateModal()" class="px-3 py-1.5 rounded-lg border border-[var(--app-card-border)] hover:bg-[var(--app-surface-hover)] text-[var(--app-muted)] cursor-pointer">
                Cancelar
              </button>
              <button type="submit" [disabled]="isCreating || !newTitle || !newChannelId"
                      class="px-4 py-1.5 rounded-lg bg-blue-600 hover:bg-blue-500 text-white font-bold cursor-pointer disabled:opacity-50 flex items-center gap-1.5">
                <i *ngIf="isCreating" class="pi pi-spin pi-spinner text-xs"></i>
                <span>{{ isCreating ? 'Creando...' : 'Crear y Continuar' }}</span>
              </button>
            </div>
          </form>
        </div>
      </div>

    </div>
  `
})
export class ContentListComponent implements OnInit {
  private readonly api = inject(ApiService);
  private readonly router = inject(Router);
  private readonly cdr = inject(ChangeDetectorRef);

  channels: ChannelDto[] = [];
  items: ContentItemDto[] = [];
  isLoading = false;

  selectedChannelId = '';
  selectedStage = '';
  searchQuery = '';

  isCreateModalOpen = false;
  newChannelId = '';
  newTitle = '';
  isCreating = false;
  createError: string | null = null;

  ngOnInit() {
    this.loadChannels();
    this.loadItems();
  }

  loadChannels() {
    this.api.getChannels().subscribe(ch => {
      this.channels = ch;
      if (ch.length > 0 && !this.newChannelId) {
        this.newChannelId = ch[0].id;
      }
      this.cdr.markForCheck();
    });
  }

  loadItems() {
    this.isLoading = true;
    this.cdr.markForCheck();
    this.api.getContentItems(
      this.selectedChannelId || undefined,
      this.selectedStage || undefined,
      undefined,
      this.searchQuery || undefined
    ).subscribe({
      next: (items) => {
        this.items = items;
        this.isLoading = false;
        this.cdr.markForCheck();
      },
      error: () => {
        this.isLoading = false;
        this.cdr.markForCheck();
      }
    });
  }

  onFilterChange() {
    this.loadItems();
  }

  openCreateModal() {
    this.newTitle = '';
    this.createError = null;
    this.isCreating = false;
    if (this.channels.length > 0 && !this.newChannelId) {
      this.newChannelId = this.channels[0].id;
    }
    this.isCreateModalOpen = true;
  }

  closeCreateModal() {
    this.isCreateModalOpen = false;
    this.isCreating = false;
    this.createError = null;
  }

  submitCreate() {
    if (!this.newTitle || !this.newChannelId) return;
    this.isCreating = true;
    this.createError = null;

    const req: CreateContentItemRequest = {
      channelId: this.newChannelId,
      title: this.newTitle.trim()
    };

    this.api.createContentItem(req).subscribe({
      next: (created) => {
        this.isCreating = false;
        this.closeCreateModal();
        this.router.navigate(['/content/items', created.id]);
      },
      error: (err) => {
        this.isCreating = false;
        this.createError = err.error?.message || err.error?.error || (err.status ? `Error HTTP ${err.status}: ${err.statusText || 'Fallo de conexión'}` : 'Error al crear la pieza.');
      }
    });
  }
}
