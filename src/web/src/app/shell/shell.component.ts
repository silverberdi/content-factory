import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterOutlet, RouterLink, RouterLinkActive } from '@angular/router';
import { ThemeService } from '../core/theme.service';
import { AuthService } from '../core/auth.service';

@Component({
  selector: 'app-shell',
  standalone: true,
  imports: [CommonModule, RouterOutlet, RouterLink, RouterLinkActive],
  template: `
    <div class="min-h-screen flex flex-col bg-[var(--app-bg)] text-[var(--app-text)] antialiased">
      <!-- Unified Header Control Center Bar -->
      <header class="h-16 border-b border-[var(--app-card-border)] bg-[var(--app-header-bg)] px-4 sm:px-6 flex items-center justify-between shrink-0 sticky top-0 z-30 shadow-xs">
        
        <!-- Left: Product Branding & Context Tag -->
        <div class="flex items-center gap-3">
          <div class="flex items-center gap-2.5">
            <div class="w-8 h-8 rounded-lg bg-blue-600 flex items-center justify-center text-white shadow-sm shadow-blue-500/30">
              <i class="pi pi-bolt text-sm"></i>
            </div>
            <div>
              <span class="font-bold text-sm sm:text-base tracking-tight block leading-tight text-[var(--app-text)]">Content Factory</span>
              <span class="text-[10px] text-[var(--app-muted)] font-mono uppercase tracking-wider block">Control Center</span>
            </div>
          </div>
        </div>

        <!-- Center: Primary Navigation Links -->
        <nav class="flex items-center gap-1.5 p-1 rounded-xl bg-[var(--app-bg)] border border-[var(--app-card-border)]">
          <a routerLink="/dashboard" routerLinkActive="bg-[var(--app-card-bg)] text-blue-600 dark:text-blue-400 font-semibold shadow-xs" 
             class="px-3.5 py-1.5 rounded-lg text-xs transition-all hover:text-blue-500 text-[var(--app-muted)] flex items-center gap-2">
            <i class="pi pi-th-large text-xs"></i> <span>Overview</span>
          </a>
          <a routerLink="/channels" routerLinkActive="bg-[var(--app-card-bg)] text-blue-600 dark:text-blue-400 font-semibold shadow-xs" 
             class="px-3.5 py-1.5 rounded-lg text-xs transition-all hover:text-blue-500 text-[var(--app-muted)] flex items-center gap-2">
            <i class="pi pi-video text-xs"></i> <span>Channels</span>
          </a>
          <a routerLink="/system" routerLinkActive="bg-[var(--app-card-bg)] text-blue-600 dark:text-blue-400 font-semibold shadow-xs" 
             class="px-3.5 py-1.5 rounded-lg text-xs transition-all hover:text-blue-500 text-[var(--app-muted)] flex items-center gap-2">
            <i class="pi pi-shield text-xs"></i> <span>System</span>
          </a>
        </nav>

        <!-- Right: Theme Switcher & Identity Presentation -->
        <div class="flex items-center gap-3">
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
              <!-- Role badges with clear separation -->
              <div class="flex items-center gap-1.5 mt-1">
                <span *ngIf="authService.currentUser()?.isOwner" 
                      class="px-1.5 py-0.5 rounded bg-amber-500/15 text-amber-600 dark:text-amber-400 border border-amber-500/30 font-bold text-[10px] uppercase tracking-wider">
                  OWNER
                </span>
                <span *ngIf="authService.isTechnical()" 
                      class="px-1.5 py-0.5 rounded bg-indigo-500/15 text-indigo-600 dark:text-indigo-400 border border-indigo-500/30 font-semibold text-[10px] uppercase tracking-wider">
                  TECH
                </span>
                <span *ngIf="authService.isEditorial()" 
                      class="px-1.5 py-0.5 rounded bg-emerald-500/15 text-emerald-600 dark:text-emerald-400 border border-emerald-500/30 font-semibold text-[10px] uppercase tracking-wider">
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

      <!-- Main Operational Viewport Container (Full-width, responsive) -->
      <main class="flex-1 w-full p-4 sm:p-6 max-w-full">
        <router-outlet></router-outlet>
      </main>
    </div>
  `
})
export class ShellComponent {
  readonly themeService = inject(ThemeService);
  readonly authService = inject(AuthService);
}
