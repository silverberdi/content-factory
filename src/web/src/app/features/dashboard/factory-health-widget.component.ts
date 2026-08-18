import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FactoryHealthDto } from '../../core/api.service';

@Component({
  selector: 'app-factory-health-widget',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="rounded-xl border border-[var(--app-card-border)] bg-[var(--app-card-bg)] p-5 shadow-xs">
      <!-- Widget Header -->
      <div class="flex items-center justify-between pb-3 mb-4 border-b border-[var(--app-card-border)]">
        <div class="flex items-center gap-2.5">
          <span class="relative flex h-2.5 w-2.5">
            <span class="animate-ping absolute inline-flex h-full w-full rounded-full opacity-75"
                  [ngClass]="health?.status === 'healthy' ? 'bg-emerald-400' : 'bg-amber-400'"></span>
            <span class="relative inline-flex rounded-full h-2.5 w-2.5"
                  [ngClass]="health?.status === 'healthy' ? 'bg-emerald-500' : 'bg-amber-500'"></span>
          </span>
          <h2 class="font-bold text-sm tracking-tight text-[var(--app-text)] uppercase">Factory Health & Runtime Telemetry</h2>
        </div>
        <div class="flex items-center gap-2">
          <span class="text-[11px] font-mono px-2.5 py-1 rounded-md font-bold uppercase tracking-wider border"
                [ngClass]="health?.status === 'healthy' 
                  ? 'bg-emerald-500/10 text-emerald-600 dark:text-emerald-400 border-emerald-500/30' 
                  : 'bg-amber-500/10 text-amber-600 dark:text-amber-400 border-amber-500/30'">
            {{ health?.status || 'HEALTHY' }}
          </span>
        </div>
      </div>

      <!-- Live Metric Tiles Grid -->
      <div class="grid grid-cols-2 md:grid-cols-4 lg:grid-cols-6 gap-3.5">
        <!-- Total Channels -->
        <div class="p-3.5 rounded-lg bg-[var(--app-bg)] border border-[var(--app-card-border)] flex flex-col justify-between">
          <span class="text-[11px] font-medium text-[var(--app-muted)]">Total Channels</span>
          <div class="flex items-baseline gap-1 mt-2">
            <span class="text-2xl font-bold font-mono text-[var(--app-text)]">{{ health?.totalChannelsCount ?? 0 }}</span>
            <span class="text-[10px] text-[var(--app-muted)]">registered</span>
          </div>
        </div>

        <!-- Pilot Channels -->
        <div class="p-3.5 rounded-lg bg-[var(--app-bg)] border border-[var(--app-card-border)] flex flex-col justify-between">
          <span class="text-[11px] font-medium text-[var(--app-muted)]">Pilot Channels</span>
          <div class="flex items-baseline gap-1 mt-2">
            <span class="text-2xl font-bold font-mono text-amber-500">{{ health?.pilotChannelsCount ?? 0 }}</span>
            <span class="text-[10px] text-amber-500/80">initial slice</span>
          </div>
        </div>

        <!-- Active Channels -->
        <div class="p-3.5 rounded-lg bg-[var(--app-bg)] border border-[var(--app-card-border)] flex flex-col justify-between">
          <span class="text-[11px] font-medium text-[var(--app-muted)]">Active Channels</span>
          <div class="flex items-baseline gap-1 mt-2">
            <span class="text-2xl font-bold font-mono text-emerald-500">{{ health?.activeChannelsCount ?? 0 }}</span>
            <span class="text-[10px] text-emerald-500/80">live production</span>
          </div>
        </div>

        <!-- Environment -->
        <div class="p-3.5 rounded-lg bg-[var(--app-bg)] border border-[var(--app-card-border)] flex flex-col justify-between">
          <span class="text-[11px] font-medium text-[var(--app-muted)]">Environment</span>
          <div class="mt-2">
            <span class="inline-block px-2 py-0.5 rounded bg-indigo-500/15 border border-indigo-500/30 text-indigo-600 dark:text-indigo-400 font-mono text-xs font-semibold uppercase">
              {{ health?.environment || 'Development' }}
            </span>
          </div>
        </div>

        <!-- Database Engine -->
        <div class="p-3.5 rounded-lg bg-[var(--app-bg)] border border-[var(--app-card-border)] flex flex-col justify-between lg:col-span-1">
          <span class="text-[11px] font-medium text-[var(--app-muted)] flex items-center gap-1.5">
            <i class="pi pi-database text-blue-500 text-xs"></i> Database Persistence
          </span>
          <div class="mt-2">
            <span class="text-xs font-mono font-medium text-[var(--app-text)] block truncate" [title]="health?.databaseStatus || 'PostgreSQL'">
              {{ health?.databaseStatus || 'Connected' }}
            </span>
          </div>
        </div>

        <!-- Off-Site Backup (Truthful Status) -->
        <div class="p-3.5 rounded-lg bg-[var(--app-bg)] border border-[var(--app-card-border)] flex flex-col justify-between lg:col-span-1">
          <span class="text-[11px] font-medium text-[var(--app-muted)] flex items-center gap-1.5">
            <i class="pi pi-cloud text-amber-500 text-xs"></i> Off-site Backup
          </span>
          <div class="mt-2">
            <span class="inline-block px-2 py-0.5 rounded bg-slate-500/10 border border-slate-500/20 text-slate-600 dark:text-slate-400 text-[10px] font-mono font-medium truncate">
              {{ health?.backupStatus || 'Not Configured (CF-001)' }}
            </span>
          </div>
        </div>
      </div>
    </div>
  `
})
export class FactoryHealthWidgetComponent {
  @Input() health: FactoryHealthDto | null = null;
}
