import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService, ChannelDto, DiscoverySourceDto } from '../../core/api.service';
import { AuthService } from '../../core/auth.service';
import { SourceDrawerComponent } from './source-drawer.component';

@Component({
  selector: 'app-discovery-sources',
  standalone: true,
  imports: [CommonModule, FormsModule, SourceDrawerComponent],
  template: `
    <div class="space-y-4 max-w-full">
      
      <!-- Header Section -->
      <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-3 pb-3 border-b border-[var(--app-card-border)]">
        <div>
          <div class="flex items-center gap-2">
            <h1 class="text-base sm:text-lg font-bold tracking-tight text-[var(--app-text)]">Catálogo de Fuentes de Discovery</h1>
            <span class="px-2 py-0.5 rounded bg-blue-500/15 border border-blue-500/30 text-blue-600 dark:text-blue-400 font-bold text-[10px] font-mono">
              {{ sources().length }} Fuentes
            </span>
          </div>
          <p class="text-xs text-[var(--app-muted)] mt-0.5">Gestión de feeds RSS, publicaciones web y orígenes automáticos por canal.</p>
        </div>

        <div class="flex items-center gap-2 flex-wrap">
          <!-- Channel Filter -->
          <select [(ngModel)]="selectedChannelId" (change)="loadSources()" 
                  class="text-xs px-2.5 py-1.5 rounded-lg border border-[var(--app-card-border)] bg-[var(--app-card-bg)] text-[var(--app-text)] focus:outline-none focus:border-blue-500">
            <option value="">Todos los Canales</option>
            <option *ngFor="let ch of channels()" [value]="ch.id">{{ ch.name }}</option>
          </select>

          <!-- Status Filter -->
          <select [(ngModel)]="selectedStatus" (change)="loadSources()" 
                  class="text-xs px-2.5 py-1.5 rounded-lg border border-[var(--app-card-border)] bg-[var(--app-card-bg)] text-[var(--app-text)] focus:outline-none focus:border-blue-500">
            <option value="">Todos los Estados</option>
            <option value="Active">Activas</option>
            <option value="Paused">Pausadas</option>
            <option value="Error">Degradadas / Error</option>
          </select>

          <!-- Register Source Button -->
          <button (click)="openCreateSource()" 
                  class="px-3.5 py-1.5 rounded-lg bg-blue-600 hover:bg-blue-700 text-white text-xs font-semibold transition-all flex items-center gap-1.5 shadow-sm cursor-pointer">
            <i class="pi pi-plus text-[10px]"></i>
            <span>Nueva Fuente</span>
          </button>
        </div>
      </div>

      <!-- Sync Feedback Notification -->
      <div *ngIf="syncNotification" class="p-3 rounded-lg bg-blue-500/10 border border-blue-500/30 text-blue-600 dark:text-blue-400 text-xs flex items-center justify-between">
        <span>{{ syncNotification }}</span>
        <button (click)="syncNotification = null" class="p-1 hover:text-[var(--app-text)]"><i class="pi pi-times text-xs"></i></button>
      </div>

      <!-- Sources Data Table -->
      <div class="rounded-xl border border-[var(--app-card-border)] bg-[var(--app-card-bg)] shadow-xs overflow-hidden">
        <div class="overflow-x-auto">
          <table class="w-full text-left text-xs border-collapse">
            <thead class="bg-[var(--app-bg)] text-[var(--app-muted)] uppercase text-[10px] tracking-wider border-b border-[var(--app-card-border)]">
              <tr>
                <th class="py-3 px-4 font-bold">Fuente</th>
                <th class="py-3 px-4 font-bold">Canal</th>
                <th class="py-3 px-4 font-bold">Tipo</th>
                <th class="py-3 px-4 font-bold">Estado</th>
                <th class="py-3 px-4 font-bold">Intervalo</th>
                <th class="py-3 px-4 font-bold">Última Sincronización</th>
                <th class="py-3 px-4 font-bold text-right">Acciones</th>
              </tr>
            </thead>
            <tbody class="divide-y divide-[var(--app-card-border)]">
              <tr *ngFor="let s of sources()" class="hover:bg-[var(--app-surface-hover)] transition-colors">
                
                <!-- Source Info -->
                <td class="py-3 px-4">
                  <div class="font-bold text-[var(--app-text)]">{{ s.name }}</div>
                  <a [href]="s.originUrl" target="_blank" rel="noopener noreferrer" 
                     class="text-[11px] text-[var(--app-muted)] hover:text-blue-500 font-mono truncate max-w-xs block" title="{{ s.originUrl }}">
                    {{ s.originUrl }}
                  </a>
                  <!-- Error banner if degraded -->
                  <div *ngIf="s.status === 'Error' && s.lastErrorMessage" class="text-[10px] text-red-500 dark:text-red-400 mt-1 max-w-sm truncate" title="{{ s.lastErrorMessage }}">
                    <i class="pi pi-exclamation-triangle text-[9px] mr-0.5"></i> {{ s.lastErrorMessage }}
                  </div>
                </td>

                <!-- Channel -->
                <td class="py-3 px-4">
                  <span class="px-2 py-0.5 rounded bg-blue-500/15 text-blue-600 dark:text-blue-400 font-semibold text-[10px]">
                    {{ s.channelName || 'Canal' }}
                  </span>
                </td>

                <!-- Type & Language -->
                <td class="py-3 px-4 font-mono">
                  <span class="px-2 py-0.5 rounded bg-[var(--app-bg)] border border-[var(--app-card-border)] text-[10px]">
                    {{ s.sourceType }} ({{ s.language | uppercase }})
                  </span>
                </td>

                <!-- Status Badge -->
                <td class="py-3 px-4">
                  <span class="px-2.5 py-0.5 rounded text-[10px] font-bold uppercase tracking-wider border"
                        [ngClass]="{
                          'bg-emerald-500/15 text-emerald-600 dark:text-emerald-400 border-emerald-500/30': s.status === 'Active',
                          'bg-slate-500/15 text-slate-500 border-slate-500/30': s.status === 'Paused',
                          'bg-red-500/15 text-red-600 dark:text-red-400 border-red-500/30': s.status === 'Error'
                        }">
                    {{ s.status }}
                  </span>
                </td>

                <!-- Interval -->
                <td class="py-3 px-4 text-[var(--app-muted)] font-mono text-[11px]">
                  {{ s.pollingIntervalMinutes }} min
                </td>

                <!-- Last Sync -->
                <td class="py-3 px-4 font-mono text-[11px] text-[var(--app-muted)]">
                  {{ s.lastSyncAtUtc ? (s.lastSyncAtUtc | date:'yyyy-MM-dd HH:mm') : 'Pendiente' }}
                </td>

                <!-- Row Actions -->
                <td class="py-3 px-4 text-right">
                  <div class="flex items-center justify-end gap-1">
                    <!-- Sync Now Button -->
                    <button (click)="syncSource(s)" [disabled]="syncingSourceId === s.id || s.status === 'Paused'"
                            class="p-1.5 rounded-md hover:bg-[var(--app-bg)] text-blue-600 dark:text-blue-400 disabled:opacity-40 transition-colors cursor-pointer" 
                            title="Sincronizar Ahora">
                      <i class="pi" [ngClass]="syncingSourceId === s.id ? 'pi-spin pi-spinner' : 'pi-refresh'"></i>
                    </button>

                    <!-- Toggle Pause/Resume -->
                    <button (click)="togglePause(s)" 
                            class="p-1.5 rounded-md hover:bg-[var(--app-bg)] text-[var(--app-muted)] hover:text-[var(--app-text)] transition-colors cursor-pointer"
                            [title]="s.status === 'Paused' ? 'Reanudar Fuente' : 'Pausar Fuente'">
                      <i class="pi" [ngClass]="s.status === 'Paused' ? 'pi-play text-emerald-500' : 'pi-pause'"></i>
                    </button>

                    <!-- Edit Source -->
                    <button (click)="openEditSource(s)" 
                            class="p-1.5 rounded-md hover:bg-[var(--app-bg)] text-[var(--app-muted)] hover:text-blue-500 transition-colors cursor-pointer" 
                            title="Editar Parámetros">
                      <i class="pi pi-pencil text-xs"></i>
                    </button>

                    <!-- Delete Source (Technical only) -->
                    <button *ngIf="authService.isTechnical()" (click)="deleteSource(s)" 
                            class="p-1.5 rounded-md hover:bg-red-500/10 text-red-500 transition-colors cursor-pointer" 
                            title="Eliminar Fuente">
                      <i class="pi pi-trash text-xs"></i>
                    </button>
                  </div>
                </td>

              </tr>

              <!-- Empty state -->
              <tr *ngIf="sources().length === 0">
                <td colspan="7" class="py-8 text-center text-[var(--app-muted)]">
                  <i class="pi pi-inbox text-2xl mb-2 block opacity-40"></i>
                  No se encontraron fuentes de discovery registradas.
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>

      <!-- Source Drawer (Create / Edit) -->
      <app-source-drawer 
        [isOpen]="isDrawerOpen" 
        [isEdit]="isEditMode" 
        [channels]="channels()" 
        [sourceData]="selectedSource"
        (onClose)="isDrawerOpen = false" 
        (onSaved)="onSourceSaved()">
      </app-source-drawer>

    </div>
  `
})
export class DiscoverySourcesComponent implements OnInit {
  private readonly api = inject(ApiService);
  readonly authService = inject(AuthService);

  sources = signal<DiscoverySourceDto[]>([]);
  channels = signal<ChannelDto[]>([]);

  selectedChannelId = '';
  selectedStatus = '';
  syncingSourceId: string | null = null;
  syncNotification: string | null = null;

  isDrawerOpen = false;
  isEditMode = false;
  selectedSource: DiscoverySourceDto | null = null;

  ngOnInit() {
    this.loadChannels();
    this.loadSources();
  }

  loadChannels() {
    this.api.getChannels().subscribe({
      next: (data) => this.channels.set(data)
    });
  }

  loadSources() {
    this.api.getDiscoverySources(this.selectedChannelId || undefined, this.selectedStatus || undefined).subscribe({
      next: (data) => this.sources.set(data)
    });
  }

  openCreateSource() {
    this.isEditMode = false;
    this.selectedSource = null;
    this.isDrawerOpen = true;
  }

  openEditSource(source: DiscoverySourceDto) {
    this.isEditMode = true;
    this.selectedSource = source;
    this.isDrawerOpen = true;
  }

  onSourceSaved() {
    this.loadSources();
  }

  syncSource(source: DiscoverySourceDto) {
    this.syncingSourceId = source.id;
    this.syncNotification = null;

    this.api.syncDiscoverySource(source.id).subscribe({
      next: (res) => {
        this.syncingSourceId = null;
        this.syncNotification = `Sincronización de '${source.name}' completada: ${res.newItemsCount} nuevos leads añadidos al triage.`;
        this.loadSources();
      },
      error: (err) => {
        this.syncingSourceId = null;
        this.syncNotification = `Error en sincronización de '${source.name}': ${err.error?.error || err.message}`;
        this.loadSources();
      }
    });
  }

  togglePause(source: DiscoverySourceDto) {
    const newStatus = source.status === 'Paused' ? 'Active' : 'Paused';
    this.api.updateDiscoverySource(source.id, {
      name: source.name,
      originUrl: source.originUrl,
      sourceType: source.sourceType,
      language: source.language,
      pollingIntervalMinutes: source.pollingIntervalMinutes,
      status: newStatus
    }).subscribe({
      next: () => this.loadSources()
    });
  }

  deleteSource(source: DiscoverySourceDto) {
    if (!confirm(`¿Eliminar la fuente '${source.name}'?`)) return;
    this.api.deleteDiscoverySource(source.id).subscribe({
      next: () => this.loadSources()
    });
  }
}
