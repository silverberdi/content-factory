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

export interface DiscoverySummaryDto {
  pendingCandidatesCount: number;
  promotedCandidatesCount: number;
  dismissedCandidatesCount: number;
  activeSourcesCount: number;
  pausedSourcesCount: number;
  errorSourcesCount: number;
}

export interface DashboardSummaryDto {
  factoryHealth: FactoryHealthDto;
  channels: ChannelDto[];
  attentionItems: AttentionItemDto[];
  discovery?: DiscoverySummaryDto;
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

export interface DiscoverySourceDto {
  id: string;
  channelId: string;
  channelName?: string;
  name: string;
  originUrl: string;
  sourceType: string;
  language: string;
  pollingIntervalMinutes: number;
  status: 'Active' | 'Paused' | 'Error';
  lastSyncAtUtc: string | null;
  nextSyncAtUtc: string | null;
  failureCount: number;
  lastErrorMessage: string | null;
  createdAtUtc: string;
  updatedAtUtc: string;
}

export interface CreateDiscoverySourceRequest {
  channelId: string;
  name: string;
  originUrl: string;
  sourceType?: string;
  language?: string;
  pollingIntervalMinutes?: number;
}

export interface UpdateDiscoverySourceRequest {
  name: string;
  originUrl: string;
  sourceType?: string;
  language?: string;
  pollingIntervalMinutes?: number;
  status?: string;
}

export interface DiscoveryCandidateDto {
  id: string;
  channelId: string;
  channelName?: string;
  discoverySourceId: string | null;
  sourceName: string | null;
  externalUrl: string | null;
  normalizedUrl: string | null;
  title: string;
  summary: string | null;
  rawContent: string | null;
  language: string;
  author: string | null;
  discoveredAtUtc: string;
  status: 'PendingReview' | 'Promoted' | 'Dismissed';
  originType: 'Automated' | 'Manual';
  submitterEmail: string | null;
  dismissalReason: string | null;
  editorialNotes: string | null;
  promotedAtUtc: string | null;
  promotedByEmail: string | null;
  createdAtUtc: string;
}

export interface QuickSubmitCandidateRequest {
  channelId: string;
  externalUrl?: string | null;
  title?: string;
  summary?: string | null;
  language?: string;
}

export interface TriageCandidateRequest {
  status: 'PendingReview' | 'Promoted' | 'Dismissed';
  dismissalReason?: string | null;
  editorialNotes?: string | null;
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

  // --- Discovery Endpoints ---

  getDiscoverySources(channelId?: string, status?: string): Observable<DiscoverySourceDto[]> {
    let params: string[] = [];
    if (channelId) params.push(`channelId=${encodeURIComponent(channelId)}`);
    if (status) params.push(`status=${encodeURIComponent(status)}`);
    const query = params.length > 0 ? `?${params.join('&')}` : '';
    return this.http.get<DiscoverySourceDto[]>(`${this.baseUrl}/discovery/sources${query}`);
  }

  getDiscoverySourceById(id: string): Observable<DiscoverySourceDto> {
    return this.http.get<DiscoverySourceDto>(`${this.baseUrl}/discovery/sources/${id}`);
  }

  createDiscoverySource(request: CreateDiscoverySourceRequest): Observable<DiscoverySourceDto> {
    return this.http.post<DiscoverySourceDto>(`${this.baseUrl}/discovery/sources`, request);
  }

  updateDiscoverySource(id: string, request: UpdateDiscoverySourceRequest): Observable<DiscoverySourceDto> {
    return this.http.put<DiscoverySourceDto>(`${this.baseUrl}/discovery/sources/${id}`, request);
  }

  deleteDiscoverySource(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/discovery/sources/${id}`);
  }

  syncDiscoverySource(id: string): Observable<{ synced: boolean; newItemsCount: number }> {
    return this.http.post<{ synced: boolean; newItemsCount: number }>(`${this.baseUrl}/discovery/sources/${id}/sync`, {});
  }

  getDiscoveryCandidates(
    channelId?: string,
    status?: string,
    sourceId?: string,
    search?: string,
    limit: number = 100
  ): Observable<DiscoveryCandidateDto[]> {
    let params: string[] = [`limit=${limit}`];
    if (channelId) params.push(`channelId=${encodeURIComponent(channelId)}`);
    if (status) params.push(`status=${encodeURIComponent(status)}`);
    if (sourceId) params.push(`sourceId=${encodeURIComponent(sourceId)}`);
    if (search) params.push(`search=${encodeURIComponent(search)}`);
    return this.http.get<DiscoveryCandidateDto[]>(`${this.baseUrl}/discovery/candidates?${params.join('&')}`);
  }

  getDiscoveryCandidateById(id: string): Observable<DiscoveryCandidateDto> {
    return this.http.get<DiscoveryCandidateDto>(`${this.baseUrl}/discovery/candidates/${id}`);
  }

  quickSubmitCandidate(request: QuickSubmitCandidateRequest): Observable<DiscoveryCandidateDto> {
    return this.http.post<DiscoveryCandidateDto>(`${this.baseUrl}/discovery/candidates/manual`, request);
  }

  triageCandidate(id: string, request: TriageCandidateRequest): Observable<DiscoveryCandidateDto> {
    return this.http.post<DiscoveryCandidateDto>(`${this.baseUrl}/discovery/candidates/${id}/triage`, request);
  }

  getDiscoverySummary(channelId?: string): Observable<DiscoverySummaryDto> {
    const query = channelId ? `?channelId=${encodeURIComponent(channelId)}` : '';
    return this.http.get<DiscoverySummaryDto>(`${this.baseUrl}/discovery/summary${query}`);
  }
}
