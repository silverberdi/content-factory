import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { AttentionItemDto } from '../../core/api.service';

@Component({
  selector: 'app-attention-widget',
  standalone: true,
  imports: [CommonModule, RouterLink],
  template: `
    <div class="rounded-xl border border-[var(--app-card-border)] bg-[var(--app-card-bg)] p-5 shadow-xs h-full flex flex-col justify-between">
      <div>
        <!-- Widget Header -->
        <div class="flex items-center justify-between pb-3 mb-3.5 border-b border-[var(--app-card-border)]">
          <div class="flex items-center gap-2">
            <div class="w-6 h-6 rounded-md bg-amber-500/10 text-amber-500 flex items-center justify-center">
              <i class="pi pi-bell text-xs"></i>
            </div>
            <div>
              <h3 class="font-bold text-sm tracking-tight text-[var(--app-text)]">Exceptions & Attention Center</h3>
            </div>
          </div>
          <span class="text-[11px] font-mono font-bold px-2.5 py-0.5 rounded-full border bg-amber-500/10 text-amber-600 dark:text-amber-400 border-amber-500/30">
            {{ items ? items.length : 0 }} Actionable
          </span>
        </div>

        <!-- Attention Item List -->
        <div *ngIf="items && items.length > 0; else noAttention" class="space-y-2.5 max-h-[300px] overflow-y-auto pr-1">
          <div *ngFor="let item of items" 
               class="p-3.5 rounded-lg border flex items-start gap-3 transition-all"
               [ngClass]="{
                 'bg-amber-500/5 border-amber-500/30 text-amber-900 dark:text-amber-200': item.severity === 'warning',
                 'bg-blue-500/5 border-blue-500/30 text-blue-900 dark:text-blue-200': item.severity === 'info',
                 'bg-rose-500/5 border-rose-500/30 text-rose-900 dark:text-rose-200': item.severity === 'critical'
               }">
            
            <div class="p-1 rounded-md shrink-0 mt-0.5" 
                 [ngClass]="{
                   'text-amber-500 bg-amber-500/10': item.severity === 'warning',
                   'text-blue-500 bg-blue-500/10': item.severity === 'info',
                   'text-rose-500 bg-rose-500/10': item.severity === 'critical'
                 }">
              <i class="pi text-xs" 
                 [ngClass]="{
                   'pi-exclamation-triangle': item.severity === 'warning',
                   'pi-info-circle': item.severity === 'info',
                   'pi-times-circle': item.severity === 'critical'
                 }"></i>
            </div>

            <div class="flex-1 min-w-0">
              <div class="flex items-center gap-1.5 flex-wrap">
                <span class="font-bold text-xs text-[var(--app-text)]">{{ item.title }}</span>
                <span *ngIf="item.isRepresentativeDemo" 
                      class="px-1.5 py-0.2 rounded bg-indigo-500/15 text-indigo-600 dark:text-indigo-400 border border-indigo-500/30 text-[9px] font-mono font-semibold uppercase">
                  Dev Seed
                </span>
              </div>
              <p class="text-[11px] text-[var(--app-muted)] mt-1 leading-snug">{{ item.description }}</p>
              <div *ngIf="item.actionPath" class="mt-2">
                <a [routerLink]="item.actionPath" class="text-xs font-semibold text-blue-600 dark:text-blue-400 hover:underline inline-flex items-center gap-1">
                  <span>Take Action</span> <i class="pi pi-arrow-up-right text-[10px]"></i>
                </a>
              </div>
            </div>
          </div>
        </div>

        <ng-template #noAttention>
          <div class="p-8 text-center text-[var(--app-muted)] rounded-lg bg-[var(--app-bg)] border border-dashed border-[var(--app-card-border)]">
            <i class="pi pi-check-circle text-3xl text-emerald-500 mb-2 block"></i>
            <p class="text-xs font-medium">All systems operational. No exceptions require attention.</p>
          </div>
        </ng-template>
      </div>

      <!-- Footer -->
      <div class="pt-3 mt-3 border-t border-[var(--app-card-border)] flex items-center justify-between text-[11px] text-[var(--app-muted)]">
        <span>Anomalies and decisions route here automatically</span>
        <span class="text-emerald-600 dark:text-emerald-400 font-semibold flex items-center gap-1">
          <i class="pi pi-shield text-[10px]"></i> <span>Supervised</span>
        </span>
      </div>
    </div>
  `
})
export class AttentionWidgetComponent {
  @Input() items: AttentionItemDto[] = [];
}
