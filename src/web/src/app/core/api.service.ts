import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface FactoryHealthDto {
  status: string;
  activeChannelsCount: number;
  pilotChannelsCount: number;
  totalChannelsCount: number;
  databaseStatus: string;
  backupStatus: string;
  environment: string;
}

export interface AttentionItemDto {
  id: string;
  severity: 'info' | 'warning' | 'critical';
  title: string;
  description: string;
  actionPath: string | null;
  isRepresentativeDemo: boolean;
  timestampUtc: string;
}

export interface ChannelDto {
  id: string;
  slug: string;
  name: string;
  language: string;
  niche: string;
  status: string;
  createdAtUtc: string;
  updatedAtUtc: string;
}

export interface DashboardSummaryDto {
  factoryHealth: FactoryHealthDto;
  channels: ChannelDto[];
  attentionItems: AttentionItemDto[];
}

export interface CreateChannelRequest {
  name: string;
  slug?: string;
  language: string;
  niche: string;
  status?: string;
}

export interface UpdateChannelRequest {
  name: string;
  language: string;
  niche: string;
  status: string;
}

export interface UserInvitationDto {
  id: string;
  email: string;
  roles: string[];
  status: string;
  expiresAtUtc: string;
  createdAtUtc: string;
}

export interface InviteUserRequest {
  email: string;
  roles: string[];
}

export interface AuditEventDto {
  id: string;
  actorUserId: string | null;
  actorEmail: string;
  action: string;
  targetType: string;
  targetId: string;
  detailsJson: string | null;
  correlationId: string | null;
  timestampUtc: string;
}

@Injectable({
  providedIn: 'root'
})
export class ApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = environment.apiUrl || 'http://localhost:5000/api';

  getDashboardSummary(): Observable<DashboardSummaryDto> {
    return this.http.get<DashboardSummaryDto>(`${this.baseUrl}/dashboard/summary`);
  }

  getChannels(): Observable<ChannelDto[]> {
    return this.http.get<ChannelDto[]>(`${this.baseUrl}/channels`);
  }

  getChannelById(id: string): Observable<ChannelDto> {
    return this.http.get<ChannelDto>(`${this.baseUrl}/channels/${id}`);
  }

  createChannel(request: CreateChannelRequest): Observable<ChannelDto> {
    return this.http.post<ChannelDto>(`${this.baseUrl}/channels`, request);
  }

  updateChannel(id: string, request: UpdateChannelRequest): Observable<ChannelDto> {
    return this.http.put<ChannelDto>(`${this.baseUrl}/channels/${id}`, request);
  }

  deleteChannel(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/channels/${id}`);
  }

  getUsers(): Observable<any[]> {
    return this.http.get<any[]>(`${this.baseUrl}/identity/users`);
  }

  getInvitations(): Observable<UserInvitationDto[]> {
    return this.http.get<UserInvitationDto[]>(`${this.baseUrl}/identity/invitations`);
  }

  createInvitation(request: InviteUserRequest): Observable<UserInvitationDto> {
    return this.http.post<UserInvitationDto>(`${this.baseUrl}/identity/invitations`, request);
  }

  revokeInvitation(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/identity/invitations/${id}`);
  }

  updateUserRoles(id: string, roles: string[]): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/identity/users/${id}/roles`, { roles });
  }

  setUserStatus(id: string, isActive: boolean): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/identity/users/${id}/status`, isActive);
  }

  getRecentAuditEvents(limit: number = 50): Observable<AuditEventDto[]> {
    return this.http.get<AuditEventDto[]>(`${this.baseUrl}/audit/recent?limit=${limit}`);
  }
}
