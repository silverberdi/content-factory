import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-page-toolbar',
  standalone: true,
  imports: [CommonModule],
  host: { class: 'block w-full' },
  template: `
    <div class="flex flex-wrap items-center justify-between gap-2.5 p-2.5 sm:p-3 rounded-xl bg-[var(--app-card-bg)] border border-[var(--app-card-border)] shadow-2xs">
      <!-- Start Slot: Search, Filters, Stage Buttons -->
      <div class="flex items-center gap-2 flex-wrap flex-1 min-w-[200px]">
        <ng-content select="[start]"></ng-content>
      </div>

      <!-- End Slot: Auxiliary Toggles, Sort, Metrics, Secondary Actions -->
      <div class="flex items-center gap-2 flex-wrap shrink-0">
        <ng-content select="[end]"></ng-content>
      </div>
    </div>
  `
})
export class PageToolbarComponent {}
