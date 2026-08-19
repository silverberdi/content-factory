import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-page-header',
  standalone: true,
  imports: [CommonModule, RouterLink],
  host: { class: 'block w-full' },
  template: `
    <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-3 pb-3 border-b border-[var(--app-card-border)]">
      <!-- Left: Title, Subtitle, Back Link & Badges -->
      <div class="space-y-0.5">
        <div class="flex items-center gap-2.5 flex-wrap">
          <!-- Optional Back Link -->
          <a *ngIf="backLink" [routerLink]="backLink" 
             class="w-7 h-7 rounded-lg border border-[var(--app-card-border)] bg-[var(--app-card-bg)] hover:bg-[var(--app-surface-hover)] flex items-center justify-center text-[var(--app-muted)] hover:text-[var(--app-text)] transition-all cursor-pointer shadow-2xs"
             [title]="backLabel || 'Volver'">
            <i class="pi pi-arrow-left text-xs"></i>
          </a>

          <h1 class="text-base sm:text-lg font-bold tracking-tight text-[var(--app-text)] flex items-center gap-2">
            <span>{{ title }}</span>
            <span *ngIf="badge !== undefined && badge !== null" 
                  class="px-2 py-0.5 rounded-full text-[10px] font-mono font-bold"
                  [ngClass]="getBadgeClass()">
              {{ badge }}
            </span>
          </h1>

          <ng-content select="[meta]"></ng-content>
        </div>

        <p *ngIf="subtitle" class="text-xs text-[var(--app-muted)] leading-relaxed">
          {{ subtitle }}
        </p>
      </div>

      <!-- Right: Primary & Secondary Actions Slot -->
      <div class="flex items-center gap-2 shrink-0 flex-wrap">
        <ng-content select="[actions]"></ng-content>
      </div>
    </div>
  `
})
export class PageHeaderComponent {
  @Input({ required: true }) title!: string;
  @Input() subtitle?: string;
  @Input() badge?: string | number;
  @Input() badgeSeverity: 'info' | 'success' | 'warn' | 'danger' | 'neutral' = 'neutral';
  @Input() backLink?: string | any[];
  @Input() backLabel?: string;

  getBadgeClass(): string {
    switch (this.badgeSeverity) {
      case 'info':
        return 'bg-blue-500/15 text-blue-600 dark:text-blue-400 border border-blue-500/30';
      case 'success':
        return 'bg-emerald-500/15 text-emerald-600 dark:text-emerald-400 border border-emerald-500/30';
      case 'warn':
        return 'bg-amber-500/15 text-amber-600 dark:text-amber-400 border border-amber-500/30';
      case 'danger':
        return 'bg-red-500/15 text-red-600 dark:text-red-400 border border-red-500/30';
      default:
        return 'bg-[var(--app-surface-hover)] text-[var(--app-muted)] border border-[var(--app-card-border)]';
    }
  }
}
