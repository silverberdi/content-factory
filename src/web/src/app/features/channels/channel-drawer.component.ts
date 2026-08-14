import { Component, Input, Output, EventEmitter, OnChanges, SimpleChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { DrawerModule } from 'primeng/drawer';
import { ChannelDto, CreateChannelRequest, UpdateChannelRequest } from '../../core/api.service';

@Component({
  selector: 'app-channel-drawer',
  standalone: true,
  imports: [CommonModule, FormsModule, DrawerModule],
  template: `
    <p-drawer [(visible)]="visible" position="right" [style]="{ width: '440px', maxWidth: '100vw' }" (onHide)="onClose.emit()">
      <ng-template #header>
        <div class="flex items-center gap-2.5">
          <div class="w-7 h-7 rounded-md bg-blue-500/10 text-blue-500 flex items-center justify-center">
            <i class="pi pi-video text-xs"></i>
          </div>
          <div>
            <span class="font-bold text-sm text-[var(--app-text)] block">{{ isEditing ? 'Edit Channel' : 'Register New Channel' }}</span>
            <span class="text-[10px] text-[var(--app-muted)] font-mono uppercase block">Channel Registry Slice</span>
          </div>
        </div>
      </ng-template>

      <div class="p-4 space-y-4 text-xs text-[var(--app-text)]">
        <div>
          <label class="block font-semibold mb-1 text-[var(--app-text)]">Channel Name</label>
          <input type="text" [(ngModel)]="form.name" placeholder="e.g. IA Simple ES" 
                 class="w-full px-3 py-2 rounded-lg border border-[var(--app-card-border)] bg-[var(--app-bg)] text-xs text-[var(--app-text)] focus:outline-none focus:border-blue-500" />
        </div>

        <div>
          <label class="block font-semibold mb-1 text-[var(--app-text)]">Channel Slug (Unique Storage Prefix)</label>
          <input type="text" [(ngModel)]="form.slug" [disabled]="isEditing" placeholder="e.g. ia-simple-es" 
                 class="w-full px-3 py-2 rounded-lg border border-[var(--app-card-border)] bg-[var(--app-bg)] text-xs font-mono text-[var(--app-text)] disabled:opacity-60 focus:outline-none focus:border-blue-500" />
          <span class="text-[10px] text-[var(--app-muted)] mt-1 block">Immutable identifier used for URL paths and MinIO media directories.</span>
        </div>

        <div class="grid grid-cols-2 gap-3">
          <div>
            <label class="block font-semibold mb-1 text-[var(--app-text)]">Language</label>
            <input type="text" [(ngModel)]="form.language" placeholder="e.g. es, en, pt-BR" 
                   class="w-full px-3 py-2 rounded-lg border border-[var(--app-card-border)] bg-[var(--app-bg)] text-xs font-mono text-[var(--app-text)] focus:outline-none focus:border-blue-500" />
          </div>

          <div>
            <label class="block font-semibold mb-1 text-[var(--app-text)]">Lifecycle Status</label>
            <select [(ngModel)]="form.status" class="w-full px-3 py-2 rounded-lg border border-[var(--app-card-border)] bg-[var(--app-bg)] text-xs text-[var(--app-text)] focus:outline-none focus:border-blue-500">
              <option value="pilot">Pilot</option>
              <option value="active">Active</option>
              <option value="scaling">Scaling</option>
              <option value="paused">Paused</option>
              <option value="archived">Archived</option>
              <option value="idea">Idea</option>
              <option value="setup-pending">Setup Pending</option>
            </select>
          </div>
        </div>

        <div>
          <label class="block font-semibold mb-1 text-[var(--app-text)]">Editorial Niche & Audience Promise</label>
          <textarea rows="4" [(ngModel)]="form.niche" placeholder="e.g. AI tools and future of work for Spanish speakers" 
                    class="w-full px-3 py-2 rounded-lg border border-[var(--app-card-border)] bg-[var(--app-bg)] text-xs text-[var(--app-text)] resize-none focus:outline-none focus:border-blue-500"></textarea>
        </div>

        <div *ngIf="errorMessage" class="p-3 rounded-lg bg-rose-500/10 border border-rose-500/30 text-rose-500 text-[11px] font-medium">
          {{ errorMessage }}
        </div>
      </div>

      <ng-template #footer>
        <div class="flex items-center justify-end gap-2.5 p-4 border-t border-[var(--app-card-border)]">
          <button (click)="onClose.emit()" class="px-3.5 py-2 rounded-lg border border-[var(--app-card-border)] text-xs font-semibold hover:bg-[var(--app-bg)] transition-colors cursor-pointer">
            Cancel
          </button>
          <button (click)="save()" [disabled]="isSaving || !form.name" 
                  class="px-4 py-2 rounded-lg bg-blue-600 hover:bg-blue-700 text-white text-xs font-semibold transition-all disabled:opacity-50 cursor-pointer shadow-sm">
            {{ isSaving ? 'Saving...' : (isEditing ? 'Update Channel' : 'Create Channel') }}
          </button>
        </div>
      </ng-template>
    </p-drawer>
  `
})
export class ChannelDrawerComponent implements OnChanges {
  @Input() visible: boolean = false;
  @Input() channel: ChannelDto | null = null;
  @Output() onClose = new EventEmitter<void>();
  @Output() onSaved = new EventEmitter<ChannelDto>();

  isEditing: boolean = false;
  isSaving: boolean = false;
  errorMessage: string | null = null;

  form = {
    name: '',
    slug: '',
    language: 'es',
    niche: '',
    status: 'pilot'
  };

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['channel'] && this.channel) {
      this.isEditing = true;
      this.form = {
        name: this.channel.name,
        slug: this.channel.slug,
        language: this.channel.language,
        niche: this.channel.niche,
        status: this.channel.status
      };
    } else if (!this.channel) {
      this.isEditing = false;
      this.form = {
        name: '',
        slug: '',
        language: 'es',
        niche: '',
        status: 'pilot'
      };
    }
    this.errorMessage = null;
  }

  save(): void {
    if (!this.form.name.trim()) {
      this.errorMessage = 'Channel name is required.';
      return;
    }

    this.isSaving = true;
    this.errorMessage = null;

    if (this.isEditing && this.channel) {
      const updateReq: UpdateChannelRequest = {
        name: this.form.name,
        language: this.form.language,
        niche: this.form.niche,
        status: this.form.status
      };
      this.onSaved.emit({
        ...this.channel,
        ...updateReq,
        updatedAtUtc: new Date().toISOString()
      });
      this.isSaving = false;
    } else {
      const createReq: CreateChannelRequest = {
        name: this.form.name,
        slug: this.form.slug || this.form.name.toLowerCase().replace(/\s+/g, '-'),
        language: this.form.language,
        niche: this.form.niche,
        status: this.form.status
      };
      this.onSaved.emit({
        id: 'new',
        slug: createReq.slug!,
        name: createReq.name,
        language: createReq.language,
        niche: createReq.niche,
        status: createReq.status!,
        createdAtUtc: new Date().toISOString(),
        updatedAtUtc: new Date().toISOString()
      });
      this.isSaving = false;
    }
  }
}
