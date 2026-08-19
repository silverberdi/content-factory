import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterOutlet, RouterLink, RouterLinkActive } from '@angular/router';
import { ThemeService } from '../core/theme.service';
import { AuthService } from '../core/auth.service';
import { ApiService } from '../core/api.service';

@Component({
  selector: 'app-shell',
  standalone: true,
  imports: [CommonModule, RouterOutlet, RouterLink, RouterLinkActive],
  template: `
    <div class="min-h-screen flex flex-col bg-[var(--app-bg)] text-[var(--app-text)] antialiased">
      <!-- Unified Header Control Center Bar -->
      <header class="h-16 border-b border-[var(--app-card-border)] bg-[var(--app-header-bg)] px-3 sm:px-4 md:px-5 flex items-center justify-between shrink-0 sticky top-0 z-30 shadow-xs">
        
        <!-- Left: Product Branding & Context Tag -->
        <div class="flex items-center gap-3">
          <div class="flex items-center gap-2.5">
            <div class="w-8 h-8 rounded-lg bg-blue-600 flex items-center justify-center text-white shadow-xs shadow-blue-500/30">
              <i class="pi pi-bolt text-sm"></i>
            </div>
            <div>
              <span class="font-bold text-sm sm:text-base tracking-tight block leading-tight text-[var(--app-text)]">Content Factory</span>
              <span class="text-[10px] text-[var(--app-muted)] font-mono uppercase tracking-wider block">Control Center</span>
            </div>
          </div>
        </div>

        <!-- Center: Primary Navigation Links -->
        <nav class="flex items-center gap-1 p-1 rounded-xl bg-[var(--app-bg)] border border-[var(--app-card-border)] overflow-x-auto">
          <!-- Overview -->
          <a routerLink="/dashboard" routerLinkActive="bg-[var(--app-card-bg)] text-blue-600 dark:text-blue-400 font-semibold shadow-xs" 
             class="px-2.5 py-1.5 rounded-lg text-xs transition-all hover:text-blue-500 text-[var(--app-muted)] flex items-center gap-1.5 shrink-0">
            <i class="pi pi-th-large text-xs"></i> <span class="hidden md:inline">Overview</span>
          </a>

          <!-- Discovery Triage with Pending Badge -->
          <a routerLink="/discovery/triage" routerLinkActive="bg-[var(--app-card-bg)] text-blue-600 dark:text-blue-400 font-semibold shadow-xs" 
             class="px-2.5 py-1.5 rounded-lg text-xs transition-all hover:text-blue-500 text-[var(--app-muted)] flex items-center gap-1.5 shrink-0">
            <i class="pi pi-filter text-xs"></i> <span>Triage</span>
            <span *ngIf="pendingCandidatesCount() > 0" 
                  class="px-1.5 py-0.2 rounded-full bg-amber-500 text-slate-900 font-extrabold text-[10px] font-mono leading-none">
              {{ pendingCandidatesCount() }}
            </span>
          </a>

          <!-- Discovery Sources -->
          <a routerLink="/discovery/sources" routerLinkActive="bg-[var(--app-card-bg)] text-blue-600 dark:text-blue-400 font-semibold shadow-xs" 
             class="px-2.5 py-1.5 rounded-lg text-xs transition-all hover:text-blue-500 text-[var(--app-muted)] flex items-center gap-1.5 shrink-0">
            <i class="pi pi-compass text-xs"></i> <span>Sources</span>
          </a>

          <!-- Content Workspace (CF-003) -->
          <a routerLink="/content/items" routerLinkActive="bg-[var(--app-card-bg)] text-blue-600 dark:text-blue-400 font-semibold shadow-xs" 
             class="px-2.5 py-1.5 rounded-lg text-xs transition-all hover:text-blue-500 text-[var(--app-muted)] flex items-center gap-1.5 shrink-0">
            <i class="pi pi-folder-open text-xs"></i> <span>Workspace</span>
          </a>

          <!-- Editorial Tasks (CF-003) -->
          <a routerLink="/editorial/tasks" routerLinkActive="bg-[var(--app-card-bg)] text-blue-600 dark:text-blue-400 font-semibold shadow-xs" 
             class="px-2.5 py-1.5 rounded-lg text-xs transition-all hover:text-blue-500 text-[var(--app-muted)] flex items-center gap-1.5 shrink-0">
            <i class="pi pi-check-square text-xs"></i> <span>Attention</span>
            <span *ngIf="pendingTasksCount() > 0" 
                  class="px-1.5 py-0.2 rounded-full bg-indigo-500 text-white font-extrabold text-[10px] font-mono leading-none">
              {{ pendingTasksCount() }}
            </span>
          </a>

          <!-- Channels -->
          <a routerLink="/channels" routerLinkActive="bg-[var(--app-card-bg)] text-blue-600 dark:text-blue-400 font-semibold shadow-xs" 
             class="px-2.5 py-1.5 rounded-lg text-xs transition-all hover:text-blue-500 text-[var(--app-muted)] flex items-center gap-1.5 shrink-0">
            <i class="pi pi-video text-xs"></i> <span>Channels</span>
          </a>

          <!-- System -->
          <a routerLink="/system" routerLinkActive="bg-[var(--app-card-bg)] text-blue-600 dark:text-blue-400 font-semibold shadow-xs" 
             class="px-2.5 py-1.5 rounded-lg text-xs transition-all hover:text-blue-500 text-[var(--app-muted)] flex items-center gap-1.5 shrink-0">
            <i class="pi pi-shield text-xs"></i> <span>System</span>
          </a>
        </nav>

        <!-- Right: Theme Switcher & Identity Presentation -->
        <div class="flex items-center gap-2 sm:gap-3">
          <!-- Theme Toggle -->
          <button (click)="themeService.toggleTheme()" 
                  class="w-8 h-8 rounded-lg border border-[var(--app-card-border)] bg-[var(--app-card-bg)] hover:bg-[var(--app-surface-hover)] text-xs transition-all flex items-center justify-center cursor-pointer shadow-2xs"
                  [attr.aria-label]="themeService.currentTheme() === 'dark' ? 'Switch to light mode' : 'Switch to dark mode'">
            <i class="pi" [ngClass]="themeService.currentTheme() === 'dark' ? 'pi-sun text-amber-400' : 'pi-moon text-slate-600'"></i>
          </button>

          <!-- Compact Identity Capsule -->
          <div class="hidden sm:flex items-center gap-2.5 pl-3 border-l border-[var(--app-card-border)]">
            <div class="flex flex-col items-end">
              <span class="font-medium text-xs text-[var(--app-text)] truncate max-w-[170px]">
                {{ authService.currentUser()?.email || 'silverio.bernal@gmail.com' }}
              </span>
              <div class="flex items-center gap-1.5 mt-0.5">
                <span *ngIf="authService.currentUser()?.isOwner" 
                      class="px-1.5 py-0.2 rounded bg-amber-500/15 text-amber-600 dark:text-amber-400 border border-amber-500/30 font-bold text-[9px] uppercase tracking-wider">
                  OWNER
                </span>
                <span *ngIf="authService.isTechnical()" 
                      class="px-1.5 py-0.2 rounded bg-indigo-500/15 text-indigo-600 dark:text-indigo-400 border border-indigo-500/30 font-semibold text-[9px] uppercase tracking-wider">
                  TECH
                </span>
                <span *ngIf="authService.isEditorial()" 
                      class="px-1.5 py-0.2 rounded bg-emerald-500/15 text-emerald-600 dark:text-emerald-400 border border-emerald-500/30 font-semibold text-[9px] uppercase tracking-wider">
                  EDIT
                </span>
              </div>
            </div>
            <div class="w-8 h-8 rounded-full bg-blue-600/15 border border-blue-500/30 text-blue-600 dark:text-blue-400 flex items-center justify-center font-bold text-xs">
              <i class="pi pi-user text-xs"></i>
            </div>
          </div>
        </div>
      </header>

      <!-- Main Operational Viewport Container -->
      <main class="flex-1 w-full p-3 sm:p-4 md:p-5 max-w-full min-w-0">
        <router-outlet></router-outlet>
      </main>
    </div>
  `
})
export class ShellComponent implements OnInit {
  readonly themeService = inject(ThemeService);
  readonly authService = inject(AuthService);
  private readonly api = inject(ApiService);

  pendingCandidatesCount = signal<number>(0);
  pendingTasksCount = signal<number>(0);

  ngOnInit() {
    this.refreshSummary();
  }

  refreshSummary() {
    this.api.getDashboardSummary().subscribe({
      next: (summary) => {
        if (summary.discovery) {
          this.pendingCandidatesCount.set(summary.discovery.pendingCandidatesCount);
        }
        if (summary.contentPipeline) {
          this.pendingTasksCount.set(summary.contentPipeline.pendingEditorialTasksCount || summary.contentPipeline.underReviewTruthSourcesCount);
        }
      },
      error: () => {}
    });
  }
}
