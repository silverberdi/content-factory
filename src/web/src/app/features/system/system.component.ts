import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService, UserInvitationDto, AuditEventDto } from '../../core/api.service';
import { AuthService } from '../../core/auth.service';
import { PageHeaderComponent } from '../../shared/layout/page-header.component';

@Component({
  selector: 'app-system',
  standalone: true,
  imports: [CommonModule, FormsModule, PageHeaderComponent],
  host: { class: 'block w-full' },
  template: `
    <div class="cf-page-container space-y-4 text-xs">
      
      <!-- Canonical Page Header -->
      <app-page-header 
        title="Gobernanza y Seguridad del Sistema" 
        subtitle="Acceso de usuarios, roles de identidad, ciclo de invitaciones y registro de auditoría inmutable">
        <div actions class="flex items-center gap-1.5 p-1 rounded-xl bg-[var(--app-bg)] border border-[var(--app-card-border)]">
          <button (click)="activeTab = 'access'" 
                  class="px-3 py-1 rounded-lg text-xs font-semibold transition-all cursor-pointer flex items-center gap-1.5"
                  [ngClass]="activeTab === 'access' ? 'bg-[var(--app-card-bg)] text-blue-600 dark:text-blue-400 shadow-2xs' : 'text-[var(--app-muted)] hover:text-blue-500'">
            <i class="pi pi-users text-xs"></i> <span>Identidad e Invitaciones</span>
          </button>
          <button (click)="activeTab = 'audit'" 
                  class="px-3 py-1 rounded-lg text-xs font-semibold transition-all cursor-pointer flex items-center gap-1.5"
                  [ngClass]="activeTab === 'audit' ? 'bg-[var(--app-card-bg)] text-blue-600 dark:text-blue-400 shadow-2xs' : 'text-[var(--app-muted)] hover:text-blue-500'">
            <i class="pi pi-history text-xs"></i> <span>Registro de Auditoría</span>
          </button>
        </div>
      </app-page-header>

      <!-- Tab 1: Access & Invitations -->
      <div *ngIf="activeTab === 'access'" class="space-y-4">
        <!-- New Invitation Box -->
        <div *ngIf="authService.isTechnical()" class="cf-card p-5 shadow-xs">
          <div class="flex items-center gap-2 mb-3">
            <i class="pi pi-user-plus text-blue-500 text-sm"></i>
            <h3 class="font-bold text-xs text-[var(--app-text)] uppercase tracking-wide">Invite Google Identity (Invitation-Only Activation)</h3>
          </div>
          <div class="flex flex-col sm:flex-row items-center gap-3">
            <input type="email" [(ngModel)]="inviteEmail" placeholder="user@gmail.com (Exact Google email)" 
                   class="text-xs px-3.5 py-2 rounded-lg border border-[var(--app-card-border)] bg-[var(--app-bg)] text-[var(--app-text)] flex-1 w-full focus:outline-hidden focus:border-blue-500" />
            
            <div class="flex items-center gap-4 text-xs">
              <label class="flex items-center gap-1.5 cursor-pointer font-medium">
                <input type="checkbox" [(ngModel)]="inviteTech" class="rounded border-[var(--app-card-border)] text-blue-600" /> Technical
              </label>
              <label class="flex items-center gap-1.5 cursor-pointer font-medium">
                <input type="checkbox" [(ngModel)]="inviteEdit" class="rounded border-[var(--app-card-border)] text-blue-600" /> Editorial
              </label>
            </div>
            
            <button (click)="sendInvitation()" [disabled]="!inviteEmail" 
                    class="cf-btn-primary disabled:opacity-50 shrink-0">
              Send Invitation
            </button>
          </div>
          <p *ngIf="inviteMsg" class="text-[11px] text-emerald-500 mt-2 font-medium">{{ inviteMsg }}</p>
        </div>

        <!-- Pending Invitations -->
        <div class="cf-card overflow-hidden">
          <div class="p-4 border-b border-[var(--app-card-border)] flex items-center justify-between">
            <h3 class="font-bold text-xs text-[var(--app-text)] uppercase tracking-wide">Pending Invitations</h3>
            <span class="text-[11px] font-mono text-[var(--app-muted)] bg-[var(--app-bg)] px-2 py-0.5 rounded border border-[var(--app-card-border)]">
              {{ invitations().length }} Active
            </span>
          </div>
          <div class="overflow-x-auto">
            <table class="cf-table">
              <thead>
                <tr>
                  <th>Email</th>
                  <th>Assigned Roles</th>
                  <th>Status</th>
                  <th>Expires</th>
                  <th class="text-right">Action</th>
                </tr>
              </thead>
              <tbody>
                <tr *ngFor="let inv of invitations()" class="hover:bg-[var(--app-surface-hover)] transition-colors">
                  <td class="py-3 px-4 font-semibold text-[var(--app-text)]">{{ inv.email }}</td>
                  <td class="py-3 px-4">
                    <div class="flex items-center gap-1">
                      <span *ngFor="let r of inv.roles" class="px-2 py-0.5 rounded bg-blue-500/15 border border-blue-500/30 text-blue-600 dark:text-blue-400 text-[10px] font-semibold">
                        {{ r }}
                      </span>
                    </div>
                  </td>
                  <td class="py-3 px-4">
                    <span class="px-2 py-0.5 rounded bg-amber-500/15 border border-amber-500/30 text-amber-600 dark:text-amber-400 text-[10px] font-bold uppercase">
                      Pending
                    </span>
                  </td>
                  <td class="py-3 px-4 text-[var(--app-muted)] text-[11px] font-mono">{{ inv.expiresAtUtc | date:'yyyy-MM-dd HH:mm' }}</td>
                  <td class="py-3 px-4 text-right">
                    <button (click)="revokeInvitation(inv.id)" class="text-rose-500 text-xs font-semibold hover:underline cursor-pointer">Revoke</button>
                  </td>
                </tr>
                <tr *ngIf="invitations().length === 0">
                  <td colspan="5" class="py-6 text-center text-[var(--app-muted)]">No pending invitations.</td>
                </tr>
              </tbody>
            </table>
          </div>
        </div>

        <!-- Active Users & Protected Owner -->
        <div class="cf-card overflow-hidden">
          <div class="p-4 border-b border-[var(--app-card-border)] flex items-center justify-between">
            <h3 class="font-bold text-xs text-[var(--app-text)] uppercase tracking-wide">Active User Accounts</h3>
            <span class="text-[10px] text-amber-600 dark:text-amber-400 font-bold uppercase bg-amber-500/15 border border-amber-500/30 px-2 py-0.5 rounded">
              SYSTEM_OWNER is protected
            </span>
          </div>
          <div class="overflow-x-auto">
            <table class="cf-table">
              <thead>
                <tr>
                  <th>User Email</th>
                  <th>Owner Status</th>
                  <th>Roles</th>
                  <th>Account Status</th>
                </tr>
              </thead>
              <tbody>
                <tr *ngFor="let u of users()" class="hover:bg-[var(--app-surface-hover)] transition-colors">
                  <td class="py-3 px-4 font-semibold text-[var(--app-text)]">{{ u.email }}</td>
                  <td class="py-3 px-4">
                    <span *ngIf="u.isOwner" class="px-2 py-0.5 rounded bg-amber-500/15 border border-amber-500/30 text-amber-600 dark:text-amber-400 font-bold text-[10px] uppercase">
                      SYSTEM_OWNER
                    </span>
                    <span *ngIf="!u.isOwner" class="text-[var(--app-muted)] text-[11px]">Standard User</span>
                  </td>
                  <td class="py-3 px-4">
                    <div class="flex items-center gap-1">
                      <span *ngFor="let r of u.roles" class="px-2 py-0.5 rounded bg-indigo-500/15 border border-indigo-500/30 text-indigo-600 dark:text-indigo-400 text-[10px] font-semibold">
                        {{ r }}
                      </span>
                    </div>
                  </td>
                  <td class="py-3 px-4">
                    <span class="px-2 py-0.5 rounded text-[10px] font-bold border"
                          [ngClass]="u.isActive ? 'bg-emerald-500/15 text-emerald-600 dark:text-emerald-400 border-emerald-500/30' : 'bg-rose-500/15 text-rose-600 border-rose-500/30'">
                      {{ u.isActive ? 'ACTIVE' : 'DISABLED' }}
                    </span>
                  </td>
                </tr>
              </tbody>
            </table>
          </div>
        </div>
      </div>

      <!-- Tab 2: Audit Trail (Minimal Verification View) -->
      <div *ngIf="activeTab === 'audit'" class="cf-card overflow-hidden">
        <div class="p-4 border-b border-[var(--app-card-border)] flex items-center justify-between">
          <div>
            <h3 class="font-bold text-xs text-[var(--app-text)] uppercase tracking-wide">Immutable Security & Mutation Audit Trail</h3>
            <p class="text-[11px] text-[var(--app-muted)] mt-0.5">Verification-oriented log of all identity, role, and channel changes.</p>
          </div>
          <button (click)="loadAuditEvents()" class="cf-btn-secondary">
            <i class="pi pi-refresh mr-1"></i> Refresh Trail
          </button>
        </div>
        <div class="overflow-x-auto">
          <table class="cf-table font-mono text-[11px]">
            <thead>
              <tr>
                <th>Timestamp (UTC)</th>
                <th>Action</th>
                <th>Target Entity</th>
                <th>Actor Identity</th>
                <th>Mutation Details</th>
              </tr>
            </thead>
            <tbody>
              <tr *ngFor="let ev of auditEvents()" class="hover:bg-[var(--app-surface-hover)] transition-colors">
                <td class="py-3 px-4 text-[var(--app-muted)] whitespace-nowrap">{{ ev.timestampUtc | date:'yyyy-MM-dd HH:mm:ss' }}</td>
                <td class="py-3 px-4 font-bold text-blue-600 dark:text-blue-400">{{ ev.action }}</td>
                <td class="py-3 px-4 text-[var(--app-text)]">{{ ev.targetType }}:{{ ev.targetId | slice:0:8 }}</td>
                <td class="py-3 px-4 text-[var(--app-text)]">{{ ev.actorEmail }}</td>
                <td class="py-3 px-4 text-[var(--app-muted)] max-w-sm truncate">{{ ev.detailsJson || '-' }}</td>
              </tr>
              <tr *ngIf="auditEvents().length === 0">
                <td colspan="5" class="py-8 text-center text-[var(--app-muted)] font-sans">No audit events recorded yet.</td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>
    </div>
  `
})
export class SystemComponent implements OnInit {
  private readonly api = inject(ApiService);
  readonly authService = inject(AuthService);

  activeTab: 'access' | 'audit' = 'access';

  readonly users = signal<any[]>([]);
  readonly invitations = signal<UserInvitationDto[]>([]);
  readonly auditEvents = signal<AuditEventDto[]>([]);

  inviteEmail: string = '';
  inviteTech: boolean = false;
  inviteEdit: boolean = true;
  inviteMsg: string | null = null;

  ngOnInit(): void {
    this.loadData();
  }

  loadData(): void {
    this.api.getUsers().subscribe(u => this.users.set(u));
    this.api.getInvitations().subscribe(i => this.invitations.set(i));
    this.loadAuditEvents();
  }

  loadAuditEvents(): void {
    this.api.getRecentAuditEvents().subscribe(a => this.auditEvents.set(a));
  }

  sendInvitation(): void {
    if (!this.inviteEmail) return;
    const roles: string[] = [];
    if (this.inviteTech) roles.push('TECHNICAL');
    if (this.inviteEdit) roles.push('EDITORIAL');

    this.api.createInvitation({ email: this.inviteEmail, roles }).subscribe({
      next: () => {
        this.inviteMsg = `Invitation issued to ${this.inviteEmail}`;
        this.inviteEmail = '';
        this.loadData();
        setTimeout(() => this.inviteMsg = null, 4000);
      },
      error: err => {
        this.inviteMsg = `Error: ${err.error?.error || 'Failed to invite'}`;
      }
    });
  }

  revokeInvitation(id: string): void {
    this.api.revokeInvitation(id).subscribe(() => this.loadData());
  }
}
