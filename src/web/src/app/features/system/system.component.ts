import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService, UserInvitationDto, AuditEventDto } from '../../core/api.service';
import { AuthService } from '../../core/auth.service';

@Component({
  selector: 'app-system',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="space-y-4 max-w-full">
      <!-- Header -->
      <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-3 pb-3 border-b border-[var(--app-card-border)]">
        <div>
          <h1 class="text-base sm:text-lg font-bold tracking-tight text-[var(--app-text)]">System & Security Governance</h1>
          <p class="text-xs text-[var(--app-muted)]">User access, role assignments, invitation lifecycle, and immutable audit trail.</p>
        </div>
        <div class="flex items-center gap-1.5 p-1 rounded-xl bg-[var(--app-bg)] border border-[var(--app-card-border)]">
          <button (click)="activeTab = 'access'" 
                  class="px-3.5 py-1.5 rounded-lg text-xs font-semibold transition-all cursor-pointer flex items-center gap-1.5"
                  [ngClass]="activeTab === 'access' ? 'bg-[var(--app-card-bg)] text-blue-600 dark:text-blue-400 shadow-xs' : 'text-[var(--app-muted)] hover:text-blue-500'">
            <i class="pi pi-users text-xs"></i> <span>Identity & Invitations</span>
          </button>
          <button (click)="activeTab = 'audit'" 
                  class="px-3.5 py-1.5 rounded-lg text-xs font-semibold transition-all cursor-pointer flex items-center gap-1.5"
                  [ngClass]="activeTab === 'audit' ? 'bg-[var(--app-card-bg)] text-blue-600 dark:text-blue-400 shadow-xs' : 'text-[var(--app-muted)] hover:text-blue-500'">
            <i class="pi pi-history text-xs"></i> <span>Audit Trail</span>
          </button>
        </div>
      </div>

      <!-- Tab 1: Access & Invitations -->
      <div *ngIf="activeTab === 'access'" class="space-y-4">
        <!-- New Invitation Box -->
        <div *ngIf="authService.isTechnical()" class="rounded-xl border border-[var(--app-card-border)] bg-[var(--app-card-bg)] p-5 shadow-xs">
          <div class="flex items-center gap-2 mb-3">
            <i class="pi pi-user-plus text-blue-500 text-sm"></i>
            <h3 class="font-bold text-xs text-[var(--app-text)] uppercase tracking-wide">Invite Google Identity (Invitation-Only Activation)</h3>
          </div>
          <div class="flex flex-col sm:flex-row items-center gap-3">
            <input type="email" [(ngModel)]="inviteEmail" placeholder="user@gmail.com (Exact Google email)" 
                   class="text-xs px-3.5 py-2 rounded-lg border border-[var(--app-card-border)] bg-[var(--app-bg)] text-[var(--app-text)] flex-1 w-full focus:outline-none focus:border-blue-500" />
            
            <div class="flex items-center gap-4 text-xs">
              <label class="flex items-center gap-1.5 cursor-pointer font-medium">
                <input type="checkbox" [(ngModel)]="inviteTech" class="rounded border-[var(--app-card-border)] text-blue-600" /> Technical
              </label>
              <label class="flex items-center gap-1.5 cursor-pointer font-medium">
                <input type="checkbox" [(ngModel)]="inviteEdit" class="rounded border-[var(--app-card-border)] text-blue-600" /> Editorial
              </label>
            </div>
            
            <button (click)="sendInvitation()" [disabled]="!inviteEmail" 
                    class="px-4 py-2 rounded-lg bg-blue-600 hover:bg-blue-700 text-white text-xs font-semibold transition-all disabled:opacity-50 shrink-0 cursor-pointer shadow-xs">
              Send Invitation
            </button>
          </div>
          <p *ngIf="inviteMsg" class="text-[11px] text-emerald-500 mt-2 font-medium">{{ inviteMsg }}</p>
        </div>

        <!-- Pending Invitations -->
        <div class="rounded-xl border border-[var(--app-card-border)] bg-[var(--app-card-bg)] shadow-xs overflow-hidden">
          <div class="p-4 border-b border-[var(--app-card-border)] flex items-center justify-between">
            <h3 class="font-bold text-xs text-[var(--app-text)] uppercase tracking-wide">Pending Invitations</h3>
            <span class="text-[11px] font-mono text-[var(--app-muted)] bg-[var(--app-bg)] px-2 py-0.5 rounded border border-[var(--app-card-border)]">
              {{ invitations().length }} Active
            </span>
          </div>
          <table class="w-full text-left text-xs border-collapse">
            <thead class="bg-[var(--app-bg)] text-[var(--app-muted)] uppercase text-[10px] tracking-wider border-b border-[var(--app-card-border)]">
              <tr>
                <th class="py-3 px-4 font-bold">Email</th>
                <th class="py-3 px-4 font-bold">Assigned Roles</th>
                <th class="py-3 px-4 font-bold">Status</th>
                <th class="py-3 px-4 font-bold">Expires</th>
                <th class="py-3 px-4 font-bold text-right">Action</th>
              </tr>
            </thead>
            <tbody class="divide-y divide-[var(--app-card-border)]">
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

        <!-- Active Users & Protected Owner -->
        <div class="rounded-xl border border-[var(--app-card-border)] bg-[var(--app-card-bg)] shadow-xs overflow-hidden">
          <div class="p-4 border-b border-[var(--app-card-border)] flex items-center justify-between">
            <h3 class="font-bold text-xs text-[var(--app-text)] uppercase tracking-wide">Active User Accounts</h3>
            <span class="text-[10px] text-amber-600 dark:text-amber-400 font-bold uppercase bg-amber-500/15 border border-amber-500/30 px-2 py-0.5 rounded">
              SYSTEM_OWNER is protected
            </span>
          </div>
          <table class="w-full text-left text-xs border-collapse">
            <thead class="bg-[var(--app-bg)] text-[var(--app-muted)] uppercase text-[10px] tracking-wider border-b border-[var(--app-card-border)]">
              <tr>
                <th class="py-3 px-4 font-bold">User Email</th>
                <th class="py-3 px-4 font-bold">Owner Status</th>
                <th class="py-3 px-4 font-bold">Roles</th>
                <th class="py-3 px-4 font-bold">Account Status</th>
              </tr>
            </thead>
            <tbody class="divide-y divide-[var(--app-card-border)]">
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

      <!-- Tab 2: Audit Trail (Minimal Verification View) -->
      <div *ngIf="activeTab === 'audit'" class="rounded-xl border border-[var(--app-card-border)] bg-[var(--app-card-bg)] shadow-xs overflow-hidden">
        <div class="p-4 border-b border-[var(--app-card-border)] flex items-center justify-between">
          <div>
            <h3 class="font-bold text-xs text-[var(--app-text)] uppercase tracking-wide">Immutable Security & Mutation Audit Trail</h3>
            <p class="text-[11px] text-[var(--app-muted)] mt-0.5">Verification-oriented log of all identity, role, and channel changes.</p>
          </div>
          <button (click)="loadAuditEvents()" class="px-3 py-1.5 rounded-lg border border-[var(--app-card-border)] bg-[var(--app-bg)] text-xs font-semibold text-blue-600 dark:text-blue-400 hover:bg-[var(--app-surface-hover)] transition-all cursor-pointer">
            <i class="pi pi-refresh mr-1"></i> Refresh Trail
          </button>
        </div>
        <div class="overflow-x-auto">
          <table class="w-full text-left text-xs border-collapse font-mono text-[11px]">
            <thead class="bg-[var(--app-bg)] text-[var(--app-muted)] uppercase text-[10px] tracking-wider border-b border-[var(--app-card-border)] font-sans">
              <tr>
                <th class="py-3 px-4 font-bold">Timestamp (UTC)</th>
                <th class="py-3 px-4 font-bold">Action</th>
                <th class="py-3 px-4 font-bold">Target Entity</th>
                <th class="py-3 px-4 font-bold">Actor Identity</th>
                <th class="py-3 px-4 font-bold">Mutation Details</th>
              </tr>
            </thead>
            <tbody class="divide-y divide-[var(--app-card-border)]">
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
