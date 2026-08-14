import { Component, EventEmitter, Input, Output, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService, ChannelDto, CreateDiscoverySourceRequest, DiscoverySourceDto, UpdateDiscoverySourceRequest } from '../../core/api.service';

@Component({
  selector: 'app-source-drawer',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div *ngIf="isOpen" class="fixed inset-0 z-50 overflow-hidden flex justify-end">
      <!-- Backdrop -->
      <div (click)="close()" class="fixed inset-0 bg-slate-900/40 dark:bg-black/60 backdrop-blur-xs transition-opacity"></div>

      <!-- Drawer Panel -->
      <div class="relative w-full max-w-lg bg-[var(--app-card-bg)] border-l border-[var(--app-card-border)] shadow-2xl flex flex-col h-full z-10">
        
        <!-- Header -->
        <div class="p-4 sm:p-5 border-b border-[var(--app-card-border)] flex items-center justify-between bg-[var(--app-header-bg)]">
          <div class="flex items-center gap-2.5">
            <div class="w-8 h-8 rounded-lg bg-blue-600/15 text-blue-600 dark:text-blue-400 flex items-center justify-center font-bold text-xs">
              <i class="pi" [ngClass]="isEdit ? 'pi-pencil' : 'pi-plus'"></i>
            </div>
            <div>
              <h2 class="text-sm sm:text-base font-bold text-[var(--app-text)]">
                {{ isEdit ? 'Editar Fuente de Discovery' : 'Registrar Nueva Fuente' }}
              </h2>
              <span class="text-[11px] text-[var(--app-muted)]">Catálogo de fuentes externas para ingesta automática.</span>
            </div>
          </div>
          <button (click)="close()" class="p-1.5 rounded-lg hover:bg-[var(--app-surface-hover)] text-[var(--app-muted)] hover:text-[var(--app-text)] transition-colors cursor-pointer">
            <i class="pi pi-times text-xs"></i>
          </button>
        </div>

        <!-- Form Body -->
        <form (ngSubmit)="save()" class="flex-1 overflow-y-auto p-4 sm:p-5 space-y-4 text-xs">
          
          <!-- Error banner -->
          <div *ngIf="errorMessage" class="p-3 rounded-lg bg-red-500/10 border border-red-500/30 text-red-600 dark:text-red-400 text-xs font-medium">
            {{ errorMessage }}
          </div>

          <!-- Channel Selection -->
          <div>
            <label class="block font-bold text-[var(--app-text)] mb-1">Canal de Destino *</label>
            <select [(ngModel)]="source.channelId" name="channelId" [disabled]="isEdit" required
                    class="w-full text-xs p-2 rounded-lg border border-[var(--app-card-border)] bg-[var(--app-bg)] text-[var(--app-text)] focus:outline-none focus:border-blue-500 disabled:opacity-60">
              <option *ngFor="let ch of channels" [value]="ch.id">{{ ch.name }} ({{ ch.language | uppercase }})</option>
            </select>
          </div>

          <!-- Source Name -->
          <div>
            <label class="block font-bold text-[var(--app-text)] mb-1">Nombre Descriptivo de la Fuente *</label>
            <input type="text" [(ngModel)]="source.name" name="name" required placeholder="Ej: Xataka Inteligencia Artificial"
                   class="w-full text-xs p-2 rounded-lg border border-[var(--app-card-border)] bg-[var(--app-bg)] text-[var(--app-text)] focus:outline-none focus:border-blue-500" />
          </div>

          <!-- Origin URL -->
          <div>
            <label class="block font-bold text-[var(--app-text)] mb-1">URL de Origen / Feed RSS *</label>
            <input type="url" [(ngModel)]="source.originUrl" name="originUrl" required placeholder="https://..."
                   class="w-full text-xs p-2 rounded-lg border border-[var(--app-card-border)] bg-[var(--app-bg)] text-[var(--app-text)] focus:outline-none focus:border-blue-500 font-mono" />
          </div>

          <div class="grid grid-cols-2 gap-3">
            <!-- Source Type -->
            <div>
              <label class="block font-bold text-[var(--app-text)] mb-1">Tipo de Fuente</label>
              <select [(ngModel)]="source.sourceType" name="sourceType"
                      class="w-full text-xs p-2 rounded-lg border border-[var(--app-card-border)] bg-[var(--app-bg)] text-[var(--app-text)] focus:outline-none focus:border-blue-500">
                <option value="Feed">Feed (RSS / Atom)</option>
                <option value="Web">Web Portal / Blog</option>
                <option value="Podcast">Podcast Feed</option>
                <option value="Curated">Canal Curado</option>
                <option value="Manual">Manual</option>
                <option value="ProviderApi">Provider API</option>
              </select>
            </div>

            <!-- Language -->
            <div>
              <label class="block font-bold text-[var(--app-text)] mb-1">Idioma</label>
              <select [(ngModel)]="source.language" name="language"
                      class="w-full text-xs p-2 rounded-lg border border-[var(--app-card-border)] bg-[var(--app-bg)] text-[var(--app-text)] focus:outline-none focus:border-blue-500">
                <option value="es">Español (es)</option>
                <option value="en">Inglés (en)</option>
                <option value="pt">Portugués (pt)</option>
              </select>
            </div>
          </div>

          <div class="grid grid-cols-2 gap-3">
            <!-- Polling Interval -->
            <div>
              <label class="block font-bold text-[var(--app-text)] mb-1">Intervalo de Sincronización (minutos)</label>
              <input type="number" [(ngModel)]="source.pollingIntervalMinutes" name="pollingIntervalMinutes" min="15" max="1440"
                     class="w-full text-xs p-2 rounded-lg border border-[var(--app-card-border)] bg-[var(--app-bg)] text-[var(--app-text)] focus:outline-none focus:border-blue-500 font-mono" />
            </div>

            <!-- Status (if edit) -->
            <div *ngIf="isEdit">
              <label class="block font-bold text-[var(--app-text)] mb-1">Estado Operativo</label>
              <select [(ngModel)]="source.status" name="status"
                      class="w-full text-xs p-2 rounded-lg border border-[var(--app-card-border)] bg-[var(--app-bg)] text-[var(--app-text)] focus:outline-none focus:border-blue-500">
                <option value="Active">Activo</option>
                <option value="Paused">Pausado</option>
                <option value="Error">Error / Degradado</option>
              </select>
            </div>
          </div>

        </form>

        <!-- Footer Actions -->
        <div class="p-4 border-t border-[var(--app-card-border)] bg-[var(--app-header-bg)] flex items-center justify-end gap-2 shrink-0">
          <button (click)="close()" class="px-3.5 py-1.5 rounded-lg border border-[var(--app-card-border)] bg-[var(--app-bg)] text-[var(--app-muted)] hover:text-[var(--app-text)] text-xs font-semibold cursor-pointer">
            Cancelar
          </button>
          <button (click)="save()" [disabled]="isSaving || !source.channelId || !source.name || !source.originUrl"
                  class="px-4 py-1.5 rounded-lg bg-blue-600 hover:bg-blue-700 disabled:opacity-50 text-white text-xs font-bold transition-all shadow-sm flex items-center gap-1.5 cursor-pointer">
            <i *ngIf="isSaving" class="pi pi-spin pi-spinner text-xs"></i>
            <span>{{ isEdit ? 'Guardar Cambios' : 'Registrar Fuente' }}</span>
          </button>
        </div>

      </div>
    </div>
  `
})
export class SourceDrawerComponent {
  private readonly api = inject(ApiService);

  @Input() isOpen = false;
  @Input() isEdit = false;
  @Input() channels: ChannelDto[] = [];
  @Input() sourceData: DiscoverySourceDto | null = null;
  @Output() onClose = new EventEmitter<void>();
  @Output() onSaved = new EventEmitter<void>();

  source = {
    id: '',
    channelId: '',
    name: '',
    originUrl: '',
    sourceType: 'Feed',
    language: 'es',
    pollingIntervalMinutes: 60,
    status: 'Active'
  };

  isSaving = false;
  errorMessage = '';

  ngOnChanges() {
    if (this.isEdit && this.sourceData) {
      this.source = {
        id: this.sourceData.id,
        channelId: this.sourceData.channelId,
        name: this.sourceData.name,
        originUrl: this.sourceData.originUrl,
        sourceType: this.sourceData.sourceType,
        language: this.sourceData.language,
        pollingIntervalMinutes: this.sourceData.pollingIntervalMinutes,
        status: this.sourceData.status
      };
    } else {
      this.source = {
        id: '',
        channelId: this.channels.length > 0 ? this.channels[0].id : '',
        name: '',
        originUrl: '',
        sourceType: 'Feed',
        language: 'es',
        pollingIntervalMinutes: 60,
        status: 'Active'
      };
    }
  }

  close() {
    this.errorMessage = '';
    this.isSaving = false;
    this.onClose.emit();
  }

  save() {
    if (!this.source.channelId || !this.source.name.trim() || !this.source.originUrl.trim()) {
      this.errorMessage = 'Por favor complete todos los campos obligatorios.';
      return;
    }

    this.isSaving = true;
    this.errorMessage = '';

    if (this.isEdit && this.source.id) {
      const updateReq: UpdateDiscoverySourceRequest = {
        name: this.source.name.trim(),
        originUrl: this.source.originUrl.trim(),
        sourceType: this.source.sourceType,
        language: this.source.language,
        pollingIntervalMinutes: this.source.pollingIntervalMinutes,
        status: this.source.status
      };

      this.api.updateDiscoverySource(this.source.id, updateReq).subscribe({
        next: () => {
          this.isSaving = false;
          this.onSaved.emit();
          this.close();
        },
        error: (err) => {
          this.isSaving = false;
          this.errorMessage = err.error?.error || 'Error al actualizar fuente.';
        }
      });
    } else {
      const createReq: CreateDiscoverySourceRequest = {
        channelId: this.source.channelId,
        name: this.source.name.trim(),
        originUrl: this.source.originUrl.trim(),
        sourceType: this.source.sourceType,
        language: this.source.language,
        pollingIntervalMinutes: this.source.pollingIntervalMinutes
      };

      this.api.createDiscoverySource(createReq).subscribe({
        next: () => {
          this.isSaving = false;
          this.onSaved.emit();
          this.close();
        },
        error: (err) => {
          this.isSaving = false;
          this.errorMessage = err.error?.error || 'Error al registrar fuente.';
        }
      });
    }
  }
}
