import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService, ChannelDto } from '../../core/api.service';
import { AuthService } from '../../core/auth.service';
import { ChannelDrawerComponent } from './channel-drawer.component';

@Component({
  selector: 'app-channels',
  standalone: true,
  imports: [CommonModule, FormsModule, ChannelDrawerComponent],
  template: `
    <div class="space-y-4 max-w-full">
      <!-- Section Header -->
      <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-3 pb-3 border-b border-[var(--app-card-border)]">
        <div>
          <h1 class="text-base sm:text-lg font-bold tracking-tight text-[var(--app-text)]">Editorial Channel Registry</h1>
          <p class="text-xs text-[var(--app-muted)]">Multi-channel portfolio management, audience promises, and pipeline status.</p>
        </div>
        <div class="flex items-center gap-2.5">
          <input type="text" [(ngModel)]="searchTerm" placeholder="Filter channels..." 
                 class="text-xs px-3 py-1.5 rounded-lg border border-[var(--app-card-border)] bg-[var(--app-card-bg)] text-[var(--app-text)] focus:outline-none focus:border-blue-500 max-w-[220px]" />
          <button (click)="openCreateChannel()" *ngIf="authService.isTechnical()" 
                  class="px-3.5 py-1.5 rounded-lg bg-blue-600 hover:bg-blue-700 text-white text-xs font-semibold transition-all flex items-center gap-1.5 shadow-sm cursor-pointer">
            <i class="pi pi-plus text-[10px]"></i> <span>New Channel</span>
          </button>
        </div>
      </div>

      <!-- Channels Data Table -->
      <div class="rounded-xl border border-[var(--app-card-border)] bg-[var(--app-card-bg)] shadow-xs overflow-hidden">
        <div class="overflow-x-auto">
          <table class="w-full text-left text-xs border-collapse">
            <thead class="bg-[var(--app-bg)] text-[var(--app-muted)] uppercase text-[10px] tracking-wider border-b border-[var(--app-card-border)]">
              <tr>
                <th class="py-3 px-4 font-bold">Channel</th>
                <th class="py-3 px-4 font-bold">Slug</th>
                <th class="py-3 px-4 font-bold">Language</th>
                <th class="py-3 px-4 font-bold">Editorial Niche</th>
                <th class="py-3 px-4 font-bold">Status</th>
                <th class="py-3 px-4 font-bold">Registered</th>
                <th class="py-3 px-4 font-bold text-right">Actions</th>
              </tr>
            </thead>
            <tbody class="divide-y divide-[var(--app-card-border)]">
              <tr *ngFor="let ch of filteredChannels()" class="hover:bg-[var(--app-surface-hover)] transition-colors">
                <td class="py-3 px-4 font-bold text-[var(--app-text)]">{{ ch.name }}</td>
                <td class="py-3 px-4 font-mono text-[var(--app-muted)] text-[11px]">{{ ch.slug }}</td>
                <td class="py-3 px-4">
                  <span class="px-2 py-0.5 rounded bg-blue-500/15 border border-blue-500/30 text-blue-600 dark:text-blue-400 font-bold uppercase text-[10px] font-mono">
                    {{ ch.language }}
                  </span>
                </td>
                <td class="py-3 px-4 text-[var(--app-muted)] max-w-xs truncate">{{ ch.niche }}</td>
                <td class="py-3 px-4">
                  <span class="px-2.5 py-0.5 rounded text-[10px] font-bold uppercase tracking-wider border"
                        [ngClass]="{
                          'bg-amber-500/15 text-amber-600 dark:text-amber-400 border-amber-500/30': ch.status === 'pilot',
                          'bg-emerald-500/15 text-emerald-600 dark:text-emerald-400 border-emerald-500/30': ch.status === 'active',
                          'bg-blue-500/15 text-blue-600 dark:text-blue-400 border-blue-500/30': ch.status === 'scaling',
                          'bg-slate-500/15 text-slate-500 border-slate-500/30': ch.status === 'paused' || ch.status === 'archived'
                        }">
                    {{ ch.status }}
                  </span>
                </td>
                <td class="py-3 px-4 text-[var(--app-muted)] text-[11px] font-mono">{{ ch.createdAtUtc | date:'yyyy-MM-dd' }}</td>
                <td class="py-3 px-4 text-right">
                  <div class="flex items-center justify-end gap-1">
                    <button (click)="openEditChannel(ch)" *ngIf="authService.isTechnical()" 
                            class="p-1.5 rounded-md hover:bg-[var(--app-bg)] text-blue-600 dark:text-blue-400 transition-colors cursor-pointer" title="Edit channel">
                      <i class="pi pi-pencil text-xs"></i>
                    </button>
                    <button (click)="deleteChannel(ch)" *ngIf="authService.isTechnical()" 
                            class="p-1.5 rounded-md hover:bg-[var(--app-bg)] text-rose-500 transition-colors cursor-pointer" title="Delete channel">
                      <i class="pi pi-trash text-xs"></i>
                    </button>
                  </div>
                </td>
              </tr>
              <tr *ngIf="filteredChannels().length === 0">
                <td colspan="7" class="py-8 text-center text-[var(--app-muted)]">
                  No channels match the filter criteria.
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>

      <!-- Channel Drawer Modal -->
      <app-channel-drawer 
        [visible]="isDrawerVisible" 
        [channel]="selectedChannel"
        (onClose)="isDrawerVisible = false"
        (onSaved)="handleChannelSaved($event)">
      </app-channel-drawer>
    </div>
  `
})
export class ChannelsComponent implements OnInit {
  private readonly api = inject(ApiService);
  readonly authService = inject(AuthService);

  readonly channels = signal<ChannelDto[]>([]);
  searchTerm: string = '';

  isDrawerVisible: boolean = false;
  selectedChannel: ChannelDto | null = null;

  ngOnInit(): void {
    this.loadChannels();
  }

  loadChannels(): void {
    this.api.getChannels().subscribe(data => this.channels.set(data));
  }

  filteredChannels(): ChannelDto[] {
    const term = this.searchTerm.trim().toLowerCase();
    if (!term) return this.channels();
    return this.channels().filter(c => 
      c.name.toLowerCase().includes(term) || 
      c.slug.toLowerCase().includes(term) ||
      c.niche.toLowerCase().includes(term) ||
      c.status.toLowerCase().includes(term)
    );
  }

  openCreateChannel(): void {
    this.selectedChannel = null;
    this.isDrawerVisible = true;
  }

  openEditChannel(channel: ChannelDto): void {
    this.selectedChannel = channel;
    this.isDrawerVisible = true;
  }

  deleteChannel(channel: ChannelDto): void {
    if (confirm(`Delete channel '${channel.name}'?`)) {
      this.api.deleteChannel(channel.id).subscribe(() => this.loadChannels());
    }
  }

  handleChannelSaved(channel: ChannelDto): void {
    if (channel.id === 'new') {
      this.api.createChannel({
        name: channel.name,
        slug: channel.slug,
        language: channel.language,
        niche: channel.niche,
        status: channel.status
      }).subscribe(() => {
        this.isDrawerVisible = false;
        this.loadChannels();
      });
    } else {
      this.api.updateChannel(channel.id, {
        name: channel.name,
        language: channel.language,
        niche: channel.niche,
        status: channel.status
      }).subscribe(() => {
        this.isDrawerVisible = false;
        this.loadChannels();
      });
    }
  }
}
