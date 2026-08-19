import { Component, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { ApiService, ChannelDto, EditorialTaskDto } from '../../core/api.service';
import { PageHeaderComponent } from '../../shared/layout/page-header.component';
import { PageToolbarComponent } from '../../shared/layout/page-toolbar.component';

@Component({
  selector: 'app-editorial-tasks-list',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule, PageHeaderComponent, PageToolbarComponent],
  host: { class: 'block w-full' },
  template: `
    <div class="cf-page-container space-y-4 text-xs">
      
      <!-- Canonical Page Header -->
      <app-page-header 
        title="Atención Editorial y Tareas" 
        subtitle="Atención contextual y revisiones pendientes en el flujo editorial"
        [badge]="pendingCount + ' Pendientes'"
        [badgeSeverity]="pendingCount > 0 ? 'warn' : 'success'">
      </app-page-header>

      <!-- Canonical Page Toolbar -->
      <app-page-toolbar>
        <div start class="flex items-center gap-2 flex-wrap flex-1">
          <!-- Channel Filter -->
          <select [(ngModel)]="selectedChannelId" (ngModelChange)="loadTasks()"
                  class="cf-toolbar-control min-w-[150px]">
            <option value="">Todos los Canales</option>
            <option *ngFor="let ch of channels" [value]="ch.id">{{ ch.name }}</option>
          </select>

          <!-- Priority Filter -->
          <select [(ngModel)]="selectedPriority" (ngModelChange)="loadTasks()"
                  class="cf-toolbar-control min-w-[140px]">
            <option value="">Todas las Prioridades</option>
            <option value="Urgent">Urgente</option>
            <option value="High">Alta</option>
            <option value="Normal">Normal</option>
            <option value="Low">Baja</option>
          </select>

          <!-- Status Filter -->
          <select [(ngModel)]="selectedStatus" (ngModelChange)="loadTasks()"
                  class="cf-toolbar-control min-w-[140px]">
            <option value="">Todos los Estados</option>
            <option value="Pending">Pendiente</option>
            <option value="InProgress">En Progreso</option>
            <option value="Completed">Completada</option>
          </select>
        </div>
      </app-page-toolbar>

      <!-- Tasks List Table -->
      <div class="cf-card overflow-hidden">
        <div *ngIf="isLoading" class="p-8 text-center text-xs text-[var(--app-muted)]">
          <i class="pi pi-spin pi-spinner text-lg mb-2 block"></i>
          <span>Cargando tareas editoriales...</span>
        </div>

        <div *ngIf="!isLoading && tasks.length === 0" class="p-12 text-center text-xs text-[var(--app-muted)] space-y-2">
          <i class="pi pi-check-circle text-2xl text-emerald-500 block"></i>
          <p class="font-bold text-[var(--app-text)]">¡Al día! No hay tareas editoriales pendientes.</p>
        </div>

        <div *ngIf="!isLoading && tasks.length > 0" class="overflow-x-auto">
          <table class="cf-table">
            <thead>
              <tr>
                <th>Pieza / Tarea</th>
                <th>Canal</th>
                <th>Tipo</th>
                <th>Prioridad</th>
                <th>Estado</th>
                <th>Vencimiento</th>
                <th class="text-right">Acción</th>
              </tr>
            </thead>
            <tbody>
              <tr *ngFor="let t of tasks" class="hover:bg-[var(--app-surface-hover)] transition-colors">
                <td class="py-3 px-4 font-bold text-[var(--app-text)]">
                  {{ t.contentTitle || 'Pieza de Contenido' }}
                </td>
                <td class="py-3 px-3">
                  <span class="px-2 py-0.5 rounded text-[10px] bg-blue-500/10 text-blue-600 dark:text-blue-400 border border-blue-500/20">
                    {{ t.channelName || 'Canal' }}
                  </span>
                </td>
                <td class="py-3 px-3 font-mono text-[11px] text-[var(--app-text)]">
                  {{ t.taskType }}
                </td>
                <td class="py-3 px-3">
                  <span class="px-2 py-0.5 rounded text-[10px] font-bold uppercase font-mono border"
                        [ngClass]="{
                          'bg-red-500/15 text-red-600 border-red-500/30': t.priority === 'Urgent' || t.priority === 'High',
                          'bg-blue-500/15 text-blue-600 border-blue-500/30': t.priority === 'Normal',
                          'bg-slate-500/15 text-slate-500 border-slate-500/30': t.priority === 'Low'
                        }">
                    {{ t.priority }}
                  </span>
                </td>
                <td class="py-3 px-3">
                  <span class="px-2 py-0.5 rounded text-[10px] font-semibold border font-mono"
                        [ngClass]="{
                          'bg-amber-500/15 text-amber-600 border-amber-500/30': t.status === 'Pending',
                          'bg-blue-500/15 text-blue-600 border-blue-500/30': t.status === 'InProgress',
                          'bg-emerald-500/15 text-emerald-600 border-emerald-500/30': t.status === 'Completed'
                        }">
                    {{ t.status }}
                  </span>
                </td>
                <td class="py-3 px-3 font-mono text-[11px] text-[var(--app-muted)]">
                  {{ (t.dueDateUtc | date:'yyyy-MM-dd HH:mm') || 'Sin fecha' }}
                </td>
                <td class="py-3 px-4 text-right">
                  <a *ngIf="t.taskType === 'ReviewStoryboard'" [routerLink]="['/content/items', t.contentItemId, 'storyboard']"
                     class="cf-btn-primary">
                    <i class="pi pi-images text-xs"></i>
                    <span>Revisar Storyboard</span>
                  </a>
                  <a *ngIf="t.taskType === 'ReviewScript'" [routerLink]="['/content/items', t.contentItemId, 'script']"
                     class="cf-btn-primary">
                    <i class="pi pi-file-edit text-xs"></i>
                    <span>Revisar Guión</span>
                  </a>
                  <a *ngIf="t.taskType !== 'ReviewScript' && t.taskType !== 'ReviewStoryboard'" [routerLink]="['/content/items', t.contentItemId, 'truth-source']"
                     class="cf-btn-secondary">
                    <i class="pi pi-check-square text-indigo-500 text-xs"></i>
                    <span>Revisar TruthSource</span>
                  </a>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>

    </div>
  `
})
export class EditorialTasksListComponent implements OnInit {
  private readonly api = inject(ApiService);
  private readonly cdr = inject(ChangeDetectorRef);

  channels: ChannelDto[] = [];
  tasks: EditorialTaskDto[] = [];
  isLoading = false;

  selectedChannelId = '';
  selectedPriority = '';
  selectedStatus = '';

  get pendingCount(): number {
    return this.tasks.filter(t => t.status === 'Pending' || t.status === 'InProgress').length;
  }

  ngOnInit() {
    this.api.getChannels().subscribe(ch => {
      this.channels = ch;
      this.cdr.markForCheck();
    });
    this.loadTasks();
  }

  loadTasks() {
    this.isLoading = true;
    this.cdr.markForCheck();
    this.api.getEditorialTasks(
      this.selectedChannelId || undefined,
      this.selectedStatus || undefined,
      this.selectedPriority || undefined
    ).subscribe({
      next: (tasks) => {
        this.tasks = tasks;
        this.isLoading = false;
        this.cdr.markForCheck();
      },
      error: () => {
        this.isLoading = false;
        this.cdr.markForCheck();
      }
    });
  }
}
