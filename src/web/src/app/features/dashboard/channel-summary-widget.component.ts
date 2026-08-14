import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ChannelDto } from '../../core/api.service';

@Component({
  selector: 'app-channel-summary-widget',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="rounded-xl border border-[var(--app-card-border)] bg-[var(--app-card-bg)] p-5 shadow-xs h-full flex flex-col justify-between">
      <div>
        <!-- Widget Header -->
        <div class="flex items-center justify-between pb-3 mb-3.5 border-b border-[var(--app-card-border)]">
          <div class="flex items-center gap-2">
            <div class="w-6 h-6 rounded-md bg-blue-500/10 text-blue-500 flex items-center justify-center">
              <i class="pi pi-video text-xs"></i>
            </div>
            <div>
              <h3 class="font-bold text-sm tracking-tight text-[var(--app-text)]">Channel Portfolio & Registry</h3>
            </div>
          </div>
          <button (click)="onCreateChannel.emit()" 
                  class="px-3 py-1.5 rounded-lg bg-blue-600 hover:bg-blue-700 text-white text-xs font-semibold transition-all flex items-center gap-1.5 shadow-2xs cursor-pointer">
            <i class="pi pi-plus text-[10px]"></i> <span>New Channel</span>
          </button>
        </div>

        <!-- Channel List -->
        <div *ngIf="channels && channels.length > 0; else emptyChannels" class="space-y-2.5 max-h-[300px] overflow-y-auto pr-1">
          <div *ngFor="let channel of channels" 
               (click)="onSelectChannel.emit(channel)"
               class="p-3.5 rounded-lg bg-[var(--app-bg)] border border-[var(--app-card-border)] hover:border-blue-500/50 hover:bg-[var(--app-surface-hover)] cursor-pointer transition-all flex items-center justify-between group">
            
            <div class="flex items-center gap-3">
              <div class="w-9 h-9 rounded-lg bg-blue-500/15 border border-blue-500/30 text-blue-600 dark:text-blue-400 flex items-center justify-center font-bold text-xs uppercase tracking-wider font-mono">
                {{ channel.language }}
              </div>
              <div>
                <div class="flex items-center gap-2">
                  <span class="font-bold text-xs text-[var(--app-text)] group-hover:text-blue-500 transition-colors">{{ channel.name }}</span>
                  <span class="text-[10px] font-mono text-[var(--app-muted)] bg-[var(--app-card-border)]/50 px-1.5 py-0.2 rounded">{{ channel.slug }}</span>
                </div>
                <p class="text-[11px] text-[var(--app-muted)] mt-0.5 line-clamp-1 max-w-sm">{{ channel.niche }}</p>
              </div>
            </div>

            <div class="flex items-center gap-3">
              <span class="px-2.5 py-0.5 rounded text-[10px] font-bold uppercase tracking-wider border"
                    [ngClass]="{
                      'bg-amber-500/15 text-amber-600 dark:text-amber-400 border-amber-500/30': channel.status === 'pilot',
                      'bg-emerald-500/15 text-emerald-600 dark:text-emerald-400 border-emerald-500/30': channel.status === 'active',
                      'bg-blue-500/15 text-blue-600 dark:text-blue-400 border-blue-500/30': channel.status === 'scaling',
                      'bg-slate-500/15 text-slate-500 border-slate-500/30': channel.status === 'paused' || channel.status === 'archived'
                    }">
                {{ channel.status }}
              </span>
              <i class="pi pi-chevron-right text-xs text-[var(--app-muted)] group-hover:text-blue-500 transition-colors"></i>
            </div>
          </div>
        </div>

        <ng-template #emptyChannels>
          <div class="p-8 text-center text-[var(--app-muted)] rounded-lg bg-[var(--app-bg)] border border-dashed border-[var(--app-card-border)]">
            <i class="pi pi-video text-3xl mb-2 text-slate-400 block"></i>
            <p class="text-xs font-medium">No channels registered in registry.</p>
            <button (click)="onCreateChannel.emit()" class="mt-3 px-3 py-1.5 rounded bg-blue-600 text-white text-xs font-semibold">
              Create Pilot Channel
            </button>
          </div>
        </ng-template>
      </div>

      <!-- Footer -->
      <div class="pt-3 mt-3 border-t border-[var(--app-card-border)] flex items-center justify-between text-xs text-[var(--app-muted)]">
        <span class="text-[11px]">Channels define autonomous editorial discovery</span>
        <button (click)="onViewAll.emit()" class="text-blue-600 dark:text-blue-400 font-semibold hover:underline cursor-pointer flex items-center gap-1">
          <span>Manage Registry</span> <i class="pi pi-arrow-right text-[10px]"></i>
        </button>
      </div>
    </div>
  `
})
export class ChannelSummaryWidgetComponent {
  @Input() channels: ChannelDto[] = [];
  @Output() onCreateChannel = new EventEmitter<void>();
  @Output() onSelectChannel = new EventEmitter<ChannelDto>();
  @Output() onViewAll = new EventEmitter<void>();
}
