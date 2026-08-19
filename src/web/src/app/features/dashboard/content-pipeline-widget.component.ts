import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { ContentPipelineSummaryDto } from '../../core/api.service';

@Component({
  selector: 'app-content-pipeline-widget',
  standalone: true,
  imports: [CommonModule, RouterModule],
  host: { class: 'block w-full' },
  template: `
    <div class="p-4 sm:p-5 rounded-xl bg-[var(--app-card-bg)] border border-[var(--app-card-border)] shadow-xs flex flex-col justify-between h-full space-y-4">
      
      <!-- Widget Header -->
      <div class="flex items-center justify-between">
        <div class="flex items-center gap-2">
          <div class="w-8 h-8 rounded-lg bg-indigo-500/15 text-indigo-600 dark:text-indigo-400 flex items-center justify-center">
            <i class="pi pi-folder-open text-sm"></i>
          </div>
          <div>
            <h2 class="text-xs font-bold text-[var(--app-text)] uppercase tracking-wider">Pipeline de Contenido & TruthSources</h2>
            <p class="text-[11px] text-[var(--app-muted)]">Piezas editoriales activas y estado de verificación de verdad</p>
          </div>
        </div>

        <a [routerLink]="['/content/items']" 
           class="text-[11px] font-semibold text-blue-600 dark:text-blue-400 hover:underline flex items-center gap-1">
          <span>Ver Workspace</span>
          <i class="pi pi-arrow-right text-[9px]"></i>
        </a>
      </div>

      <!-- Metrics Grid -->
      <div class="grid grid-cols-2 sm:grid-cols-5 gap-2 text-center">
        
        <!-- Total Items -->
        <div class="p-2.5 rounded-lg bg-[var(--app-bg)] border border-[var(--app-card-border)] space-y-1">
          <span class="text-[10px] font-bold text-[var(--app-muted)] uppercase tracking-wider block truncate">Total</span>
          <span class="text-base sm:text-lg font-extrabold text-[var(--app-text)] font-mono">
            {{ pipeline?.totalContentItemsCount || 0 }}
          </span>
        </div>

        <!-- Drafting Evidence -->
        <div class="p-2.5 rounded-lg bg-amber-500/10 border border-amber-500/20 space-y-1">
          <span class="text-[10px] font-bold text-amber-600 dark:text-amber-400 uppercase tracking-wider block truncate">Drafting</span>
          <span class="text-base sm:text-lg font-extrabold text-amber-600 dark:text-amber-400 font-mono">
            {{ pipeline?.draftingEvidenceCount || 0 }}
          </span>
        </div>

        <!-- TruthSources Approved -->
        <div class="p-2.5 rounded-lg bg-indigo-500/10 border border-indigo-500/20 space-y-1">
          <span class="text-[10px] font-bold text-indigo-600 dark:text-indigo-400 uppercase tracking-wider block truncate">Truth Aprobado</span>
          <span class="text-base sm:text-lg font-extrabold text-indigo-600 dark:text-indigo-400 font-mono">
            {{ pipeline?.truthSourceApprovedCount || 0 }}
          </span>
        </div>

        <!-- Ideas Selected -->
        <div class="p-2.5 rounded-lg bg-purple-500/10 border border-purple-500/20 space-y-1">
          <span class="text-[10px] font-bold text-purple-600 dark:text-purple-400 uppercase tracking-wider block truncate">Idea Seleccionada</span>
          <span class="text-base sm:text-lg font-extrabold text-purple-600 dark:text-purple-400 font-mono">
            {{ pipeline?.ideaSelectedCount || 0 }}
          </span>
        </div>

        <!-- TruthSources Under Review -->
        <div class="p-2.5 rounded-lg bg-amber-500/10 border border-amber-500/20 space-y-1">
          <span class="text-[10px] font-bold text-amber-600 dark:text-amber-400 uppercase tracking-wider block truncate">En Revisión</span>
          <span class="text-base sm:text-lg font-extrabold text-amber-600 dark:text-amber-400 font-mono">
            {{ pipeline?.underReviewTruthSourcesCount || 0 }}
          </span>
        </div>

      </div>

      <!-- Quick Action Strip -->
      <div class="flex items-center justify-between pt-2 border-t border-[var(--app-card-border)] text-xs">
        <span class="text-[11px] text-[var(--app-muted)]">
          {{ pipeline?.pendingEditorialTasksCount || 0 }} tarea(s) de revisión editorial pendiente(s)
        </span>
        <a [routerLink]="['/editorial/tasks']" 
           class="px-2.5 py-1 rounded bg-indigo-600 hover:bg-indigo-500 text-white font-semibold text-[11px] flex items-center gap-1 transition-colors">
          <i class="pi pi-check-square text-[10px]"></i>
          <span>Revisar Atención</span>
        </a>
      </div>

    </div>
  `
})
export class ContentPipelineSummaryWidgetComponent {
  @Input() pipeline: ContentPipelineSummaryDto | null = null;
}
