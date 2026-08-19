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

export interface ContentPipelineSummaryDto {
  totalContentItemsCount: number;
  draftingEvidenceCount: number;
  truthSourceApprovedCount: number;
  ideaSelectedCount?: number;
  underReviewTruthSourcesCount: number;
  pendingEditorialTasksCount: number;
}

export interface DashboardSummaryDto {
  factoryHealth: FactoryHealthDto;
  channels: ChannelDto[];
  attentionItems: AttentionItemDto[];
  discovery?: DiscoverySummaryDto;
  contentPipeline?: ContentPipelineSummaryDto;
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
  title: string;
  summary?: string | null;
  language?: string;
}

export interface TriageCandidateRequest {
  status: 'PendingReview' | 'Promoted' | 'Dismissed';
  dismissalReason?: string | null;
  editorialNotes?: string | null;
}

// --- CF-003 Content & TruthSource Models ---

export type ContentItemStage =
  | 'DraftingEvidence'
  | 'TruthSourceApproved'
  | 'IdeaSelected'
  | 'ScriptDrafted'
  | 'ScriptUnderReview'
  | 'ScriptApproved'
  | 'InProduction'
  | 'Published'
  | 'Archived';

export type ContentItemStatus = 'Active' | 'Archived' | 'Suspended';
export type EvidenceRole = 'PrimaryLead' | 'SupportingEvidence' | 'Counterpoint' | 'StyleReference';
export type EvidenceStatus = 'Captured' | 'CaptureFailed' | 'Excluded';
export type TruthSourceStatus = 'Draft' | 'UnderReview' | 'Approved' | 'Rejected';
export type EditorialTaskType = 'ReviewTruthSource' | 'ReviewIdea' | 'ReviewScript' | 'ReviewRender';
export type EditorialTaskPriority = 'Low' | 'Normal' | 'High' | 'Urgent';
export type EditorialTaskStatus = 'Pending' | 'InProgress' | 'Completed' | 'Dismissed';

export interface VerifiableClaimDto {
  claim: string;
  sourceCitation: string;
  evidenceId: string;
}

export interface ContentItemEvidenceDto {
  id: string;
  contentItemId: string;
  discoveryCandidateId: string | null;
  originUrl: string | null;
  title: string;
  role: EvidenceRole | string;
  status: EvidenceStatus | string;
  rawContent: string | null;
  objectStorageKey: string | null;
  extractedText: string | null;
  contentHash: string | null;
  errorMessage: string | null;
  notes: string | null;
  author: string | null;
  capturedAtUtc: string | null;
  createdAtUtc: string;
  createdByEmail: string;
}

export interface TruthSourceDto {
  id: string;
  contentItemId: string;
  status: TruthSourceStatus | string;
  summary: string;
  keyIdeas: string[];
  verifiableClaims: VerifiableClaimDto[];
  evidenceReferences: string[];
  riskNotes: string;
  doNotSayConstraints: string[];
  possibleAngles: string[];
  localizationNotes: string;
  rejectionReason: string | null;
  rejectedAtUtc: string | null;
  rejectedByEmail: string | null;
  approvedAtUtc: string | null;
  approvedByEmail: string | null;
  version: number;
  createdAtUtc: string;
  createdByEmail: string;
  updatedAtUtc: string;
  updatedByEmail: string;
}

export interface TruthSourceVersionDto {
  id: string;
  truthSourceId: string;
  contentItemId: string;
  versionNumber: number;
  snapshotJson: string;
  supportingEvidenceIds: string[];
  changeSummary: string;
  createdAtUtc: string;
  createdByEmail: string;
}

// --- CF-004 Content Idea Models ---

export type ContentIdeaStatus = 'Proposed' | 'Selected' | 'Dismissed';
export type IdeaFreshnessClass = 'Evergreen' | 'Timely' | 'Breaking';
export type IdeaPriority = 'Low' | 'Normal' | 'High' | 'Urgent';

export interface ContentIdeaDto {
  id: string;
  contentItemId: string;
  truthSourceId: string;
  truthSourceVersionId: string;
  title: string;
  angle: string;
  hookStrategy: string;
  audienceValue: string;
  format: string;
  intendedOutcome: string;
  freshnessClass: string;
  priority: string;
  rationale: string;
  status: ContentIdeaStatus | string;
  dismissalNotes?: string | null;
  selectedAtUtc?: string | null;
  selectedByEmail?: string | null;
  version: number;
  createdAtUtc: string;
  createdByEmail: string;
  updatedAtUtc: string;
  updatedByEmail: string;
}

export interface ContentIdeaVersionDto {
  id: string;
  contentIdeaId: string;
  contentItemId: string;
  truthSourceId: string;
  truthSourceVersionId: string;
  versionNumber: number;
  title: string;
  angle: string;
  hookStrategy: string;
  audienceValue: string;
  format: string;
  intendedOutcome: string;
  freshnessClass: string;
  priority: string;
  rationale: string;
  status: ContentIdeaStatus | string;
  dismissalNotes?: string | null;
  editedByEmail: string;
  editedAtUtc: string;
  changeSummary: string;
}

export interface GenerateIdeasOptions {
  count?: number;
  focusAngleStyle?: string | null;
  targetAudience?: string | null;
}

export interface CreateIdeaRequest {
  title: string;
  angle: string;
  hookStrategy: string;
  audienceValue: string;
  format?: string | null;
  intendedOutcome?: string | null;
  freshnessClass?: string | null;
  priority?: string | null;
  rationale?: string | null;
}

export interface UpdateIdeaRequest {
  title: string;
  angle: string;
  hookStrategy: string;
  audienceValue: string;
  format?: string | null;
  intendedOutcome?: string | null;
  freshnessClass?: string | null;
  priority?: string | null;
  rationale?: string | null;
  changeSummary?: string | null;
  expectedVersion: number;
}

export interface SelectIdeaRequest {
  expectedVersion: number;
}

export interface DismissIdeaRequest {
  notes?: string | null;
  expectedVersion: number;
}

export interface ReopenIdeaRequest {
  expectedVersion: number;
}

export interface ContentItemDto {
  id: string;
  channelId: string;
  channelName?: string;
  title: string;
  slug: string;
  stage: ContentItemStage | string;
  status: ContentItemStatus | string;
  version: number;
  evidenceCount: number;
  truthSourceStatus?: string | null;
  truthSourceVersion?: number | null;
  createdAtUtc: string;
  createdByEmail: string;
  updatedAtUtc: string;
  updatedByEmail: string;
}

export interface ContentItemDetailDto {
  id: string;
  channelId: string;
  channelName?: string;
  title: string;
  slug: string;
  stage: ContentItemStage | string;
  status: ContentItemStatus | string;
  version: number;
  createdAtUtc: string;
  createdByEmail: string;
  updatedAtUtc: string;
  updatedByEmail: string;
  evidences: ContentItemEvidenceDto[];
  truthSource?: TruthSourceDto | null;
}

export interface EditorialTaskDto {
  id: string;
  channelId: string;
  channelName?: string;
  contentItemId: string;
  contentTitle?: string;
  taskType: EditorialTaskType | string;
  priority: EditorialTaskPriority | string;
  status: EditorialTaskStatus | string;
  assignedUserEmail: string | null;
  dueDateUtc: string | null;
  completedAtUtc: string | null;
  completedByEmail: string | null;
  createdAtUtc: string;
  updatedAtUtc: string;
  createdByEmail: string;
}

export interface CreateContentItemRequest {
  channelId: string;
  title: string;
}

export interface UpdateContentItemRequest {
  title?: string;
  status?: string;
  expectedVersion: number;
}

export interface AttachEvidenceRequest {
  discoveryCandidateId?: string | null;
  originUrl?: string | null;
  title: string;
  contentText?: string | null;
  role?: string;
  notes?: string | null;
}

export interface SaveTruthSourceRequest {
  summary: string;
  keyIdeas: string[];
  verifiableClaims: VerifiableClaimDto[];
  evidenceReferences: string[];
  riskNotes: string;
  doNotSayConstraints: string[];
  possibleAngles: string[];
  localizationNotes: string;
  expectedVersion: number;
  changeSummary?: string | null;
}

export interface RejectTruthSourceRequest {
  reason: string;
}

export interface AssignEditorialTaskRequest {
  assignedUserEmail?: string | null;
  priority?: string;
  dueDateUtc?: string | null;
}

export interface InitiateContentFromCandidateRequest {
  titleOverride?: string | null;
}

export interface AttachCandidateToContentRequest {
  contentItemId: string;
  role?: string;
  notes?: string | null;
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
    const params: string[] = [];
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
    const params: string[] = [`limit=${limit}`];
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

  initiateContentFromCandidate(candidateId: string, request: InitiateContentFromCandidateRequest): Observable<ContentItemDto> {
    return this.http.post<ContentItemDto>(`${this.baseUrl}/discovery/candidates/${candidateId}/initiate-content`, request);
  }

  attachCandidateToContent(candidateId: string, request: AttachCandidateToContentRequest): Observable<ContentItemEvidenceDto> {
    return this.http.post<ContentItemEvidenceDto>(`${this.baseUrl}/discovery/candidates/${candidateId}/attach-to-content`, request);
  }

  // --- CF-003 Content Items & Evidence Endpoints ---

  getContentItems(channelId?: string, stage?: string, status?: string, search?: string): Observable<ContentItemDto[]> {
    const params: string[] = [];
    if (channelId) params.push(`channelId=${encodeURIComponent(channelId)}`);
    if (stage) params.push(`stage=${encodeURIComponent(stage)}`);
    if (status) params.push(`status=${encodeURIComponent(status)}`);
    if (search) params.push(`search=${encodeURIComponent(search)}`);
    const query = params.length > 0 ? `?${params.join('&')}` : '';
    return this.http.get<ContentItemDto[]>(`${this.baseUrl}/content-items${query}`);
  }

  getContentItemDetail(id: string): Observable<ContentItemDetailDto> {
    return this.http.get<ContentItemDetailDto>(`${this.baseUrl}/content-items/${id}`);
  }

  createContentItem(request: CreateContentItemRequest): Observable<ContentItemDto> {
    return this.http.post<ContentItemDto>(`${this.baseUrl}/content-items`, request);
  }

  updateContentItem(id: string, request: UpdateContentItemRequest): Observable<ContentItemDto> {
    return this.http.put<ContentItemDto>(`${this.baseUrl}/content-items/${id}`, request);
  }

  attachEvidence(contentItemId: string, request: AttachEvidenceRequest): Observable<ContentItemEvidenceDto> {
    return this.http.post<ContentItemEvidenceDto>(`${this.baseUrl}/content-items/${contentItemId}/evidence`, request);
  }

  detachEvidence(contentItemId: string, evidenceId: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/content-items/${contentItemId}/evidence/${evidenceId}`);
  }

  retryEvidenceCapture(contentItemId: string, evidenceId: string): Observable<ContentItemEvidenceDto> {
    return this.http.post<ContentItemEvidenceDto>(`${this.baseUrl}/content-items/${contentItemId}/evidence/${evidenceId}/retry`, {});
  }

  // --- CF-003 TruthSource Review Studio Endpoints ---

  getTruthSource(contentItemId: string): Observable<TruthSourceDto> {
    return this.http.get<TruthSourceDto>(`${this.baseUrl}/content-items/${contentItemId}/truth-source`);
  }

  generateAiDraft(contentItemId: string): Observable<TruthSourceDto> {
    return this.http.post<TruthSourceDto>(`${this.baseUrl}/content-items/${contentItemId}/truth-source/generate-ai-draft`, {});
  }

  saveTruthSource(contentItemId: string, request: SaveTruthSourceRequest): Observable<TruthSourceDto> {
    return this.http.put<TruthSourceDto>(`${this.baseUrl}/content-items/${contentItemId}/truth-source`, request);
  }

  submitTruthSourceReview(contentItemId: string): Observable<TruthSourceDto> {
    return this.http.post<TruthSourceDto>(`${this.baseUrl}/content-items/${contentItemId}/truth-source/submit-review`, {});
  }

  approveTruthSource(contentItemId: string): Observable<TruthSourceDto> {
    return this.http.post<TruthSourceDto>(`${this.baseUrl}/content-items/${contentItemId}/truth-source/approve`, {});
  }

  rejectTruthSource(contentItemId: string, request: RejectTruthSourceRequest): Observable<TruthSourceDto> {
    return this.http.post<TruthSourceDto>(`${this.baseUrl}/content-items/${contentItemId}/truth-source/reject`, request);
  }

  getTruthSourceVersions(contentItemId: string): Observable<TruthSourceVersionDto[]> {
    return this.http.get<TruthSourceVersionDto[]>(`${this.baseUrl}/content-items/${contentItemId}/truth-source/versions`);
  }

  // --- CF-003 Editorial Tasks Endpoints ---

  getEditorialTasks(channelId?: string, status?: string, priority?: string, assignedEmail?: string): Observable<EditorialTaskDto[]> {
    const params: string[] = [];
    if (channelId) params.push(`channelId=${encodeURIComponent(channelId)}`);
    if (status) params.push(`status=${encodeURIComponent(status)}`);
    if (priority) params.push(`priority=${encodeURIComponent(priority)}`);
    if (assignedEmail) params.push(`assignedEmail=${encodeURIComponent(assignedEmail)}`);
    const query = params.length > 0 ? `?${params.join('&')}` : '';
    return this.http.get<EditorialTaskDto[]>(`${this.baseUrl}/editorial-tasks${query}`);
  }

  assignEditorialTask(taskId: string, request: AssignEditorialTaskRequest): Observable<EditorialTaskDto> {
    return this.http.put<EditorialTaskDto>(`${this.baseUrl}/editorial-tasks/${taskId}/assign`, request);
  }

  updateEditorialTaskStatus(taskId: string, status: string): Observable<EditorialTaskDto> {
    return this.http.put<EditorialTaskDto>(`${this.baseUrl}/editorial-tasks/${taskId}/status`, JSON.stringify(status), {
      headers: { 'Content-Type': 'application/json' }
    });
  }

  // --- CF-004 Content Idea Endpoints ---

  getContentIdeas(contentItemId: string): Observable<ContentIdeaDto[]> {
    return this.http.get<ContentIdeaDto[]>(`${this.baseUrl}/content-items/${contentItemId}/ideas`);
  }

  getIdeaById(contentItemId: string, ideaId: string): Observable<ContentIdeaDto> {
    return this.http.get<ContentIdeaDto>(`${this.baseUrl}/content-items/${contentItemId}/ideas/${ideaId}`);
  }

  generateAiIdeas(contentItemId: string, options: GenerateIdeasOptions = {}): Observable<ContentIdeaDto[]> {
    return this.http.post<ContentIdeaDto[]>(`${this.baseUrl}/content-items/${contentItemId}/ideas/generate`, options);
  }

  createManualIdea(contentItemId: string, request: CreateIdeaRequest): Observable<ContentIdeaDto> {
    return this.http.post<ContentIdeaDto>(`${this.baseUrl}/content-items/${contentItemId}/ideas`, request);
  }

  updateIdea(contentItemId: string, ideaId: string, request: UpdateIdeaRequest): Observable<ContentIdeaDto> {
    return this.http.put<ContentIdeaDto>(`${this.baseUrl}/content-items/${contentItemId}/ideas/${ideaId}`, request);
  }

  selectIdea(contentItemId: string, ideaId: string, request: SelectIdeaRequest): Observable<ContentIdeaDto> {
    return this.http.post<ContentIdeaDto>(`${this.baseUrl}/content-items/${contentItemId}/ideas/${ideaId}/select`, request);
  }

  dismissIdea(contentItemId: string, ideaId: string, request: DismissIdeaRequest): Observable<ContentIdeaDto> {
    return this.http.post<ContentIdeaDto>(`${this.baseUrl}/content-items/${contentItemId}/ideas/${ideaId}/dismiss`, request);
  }

  reopenIdea(contentItemId: string, ideaId: string, request: ReopenIdeaRequest): Observable<ContentIdeaDto> {
    return this.http.post<ContentIdeaDto>(`${this.baseUrl}/content-items/${contentItemId}/ideas/${ideaId}/reopen`, request);
  }

  getIdeaVersions(contentItemId: string, ideaId: string): Observable<ContentIdeaVersionDto[]> {
    return this.http.get<ContentIdeaVersionDto[]>(`${this.baseUrl}/content-items/${contentItemId}/ideas/${ideaId}/versions`);
  }

  // --- CF-012 / CF-013 Script Editorial Pipeline Endpoints ---

  getScript(contentItemId: string): Observable<ScriptDto> {
    return this.http.get<ScriptDto>(`${this.baseUrl}/content-items/${contentItemId}/script`);
  }

  getScriptVersions(contentItemId: string, scriptId?: string): Observable<ScriptVersionDto[]> {
    const query = scriptId ? `?scriptId=${encodeURIComponent(scriptId)}` : '';
    return this.http.get<ScriptVersionDto[]>(`${this.baseUrl}/content-items/${contentItemId}/script/versions${query}`);
  }

  getScriptVersion(contentItemId: string, versionId: string, scriptId?: string): Observable<ScriptVersionDto> {
    const query = scriptId ? `?scriptId=${encodeURIComponent(scriptId)}` : '';
    return this.http.get<ScriptVersionDto>(`${this.baseUrl}/content-items/${contentItemId}/script/versions/${versionId}${query}`);
  }

  createScript(contentItemId: string, request: CreateScriptRequest): Observable<ScriptDto> {
    return this.http.post<ScriptDto>(`${this.baseUrl}/content-items/${contentItemId}/script`, request);
  }

  updateScript(contentItemId: string, scriptId: string, request: UpdateScriptRequest): Observable<ScriptDto> {
    return this.http.put<ScriptDto>(`${this.baseUrl}/content-items/${contentItemId}/script/${scriptId}`, request);
  }

  generateAiScript(contentItemId: string, options?: GenerateScriptOptions): Observable<ScriptDto> {
    return this.http.post<ScriptDto>(`${this.baseUrl}/content-items/${contentItemId}/script/generate`, options || {});
  }

  reviewScript(contentItemId: string, scriptId: string): Observable<ScriptReviewResultDto> {
    return this.http.post<ScriptReviewResultDto>(`${this.baseUrl}/content-items/${contentItemId}/script/${scriptId}/review`, {});
  }

  submitScriptForReview(contentItemId: string, scriptId: string, request: SubmitScriptForReviewRequest): Observable<ScriptDto> {
    return this.http.post<ScriptDto>(`${this.baseUrl}/content-items/${contentItemId}/script/${scriptId}/submit-for-review`, request);
  }

  approveScript(contentItemId: string, scriptId: string, request: ApproveScriptRequest): Observable<ScriptDto> {
    return this.http.post<ScriptDto>(`${this.baseUrl}/content-items/${contentItemId}/script/${scriptId}/approve`, request);
  }

  rejectScript(contentItemId: string, scriptId: string, request: RejectScriptRequest): Observable<ScriptDto> {
    return this.http.post<ScriptDto>(`${this.baseUrl}/content-items/${contentItemId}/script/${scriptId}/reject`, request);
  }

  reopenScript(contentItemId: string, scriptId: string, request: ReopenScriptRequest): Observable<ScriptDto> {
    return this.http.post<ScriptDto>(`${this.baseUrl}/content-items/${contentItemId}/script/${scriptId}/reopen`, request);
  }

  // --- CF-014 / CF-015 Storyboard & Production Planning Endpoints ---

  getStoryboard(contentItemId: string): Observable<StoryboardDto> {
    return this.http.get<StoryboardDto>(`${this.baseUrl}/content-items/${contentItemId}/storyboard`);
  }

  getStoryboardVersions(contentItemId: string, storyboardId?: string): Observable<StoryboardVersionDto[]> {
    const query = storyboardId ? `?storyboardId=${encodeURIComponent(storyboardId)}` : '';
    return this.http.get<StoryboardVersionDto[]>(`${this.baseUrl}/content-items/${contentItemId}/storyboard/versions${query}`);
  }

  getStoryboardVersion(contentItemId: string, versionId: string, storyboardId?: string): Observable<StoryboardVersionDto> {
    const query = storyboardId ? `?storyboardId=${encodeURIComponent(storyboardId)}` : '';
    return this.http.get<StoryboardVersionDto>(`${this.baseUrl}/content-items/${contentItemId}/storyboard/versions/${versionId}${query}`);
  }

  createStoryboard(contentItemId: string, request: CreateStoryboardRequest): Observable<StoryboardDto> {
    return this.http.post<StoryboardDto>(`${this.baseUrl}/content-items/${contentItemId}/storyboard`, request);
  }

  updateStoryboard(contentItemId: string, storyboardId: string, request: UpdateStoryboardRequest): Observable<StoryboardDto> {
    return this.http.put<StoryboardDto>(`${this.baseUrl}/content-items/${contentItemId}/storyboard/${storyboardId}`, request);
  }

  generateAiStoryboard(contentItemId: string, options?: PlanStoryboardOptions): Observable<StoryboardDto> {
    return this.http.post<StoryboardDto>(`${this.baseUrl}/content-items/${contentItemId}/storyboard/generate`, options || {});
  }

  reviewStoryboard(contentItemId: string, storyboardId: string): Observable<StoryboardCritiqueResultDto> {
    return this.http.post<StoryboardCritiqueResultDto>(`${this.baseUrl}/content-items/${contentItemId}/storyboard/${storyboardId}/review`, {});
  }

  submitStoryboardForReview(contentItemId: string, storyboardId: string, request: SubmitStoryboardForReviewRequest): Observable<StoryboardDto> {
    return this.http.post<StoryboardDto>(`${this.baseUrl}/content-items/${contentItemId}/storyboard/${storyboardId}/submit-for-review`, request);
  }

  approveStoryboard(contentItemId: string, storyboardId: string, request: ApproveStoryboardRequest): Observable<StoryboardDto> {
    return this.http.post<StoryboardDto>(`${this.baseUrl}/content-items/${contentItemId}/storyboard/${storyboardId}/approve`, request);
  }

  rejectStoryboard(contentItemId: string, storyboardId: string, request: RejectStoryboardRequest): Observable<StoryboardDto> {
    return this.http.post<StoryboardDto>(`${this.baseUrl}/content-items/${contentItemId}/storyboard/${storyboardId}/reject`, request);
  }

  reopenStoryboard(contentItemId: string, storyboardId: string, request: ReopenStoryboardRequest): Observable<StoryboardDto> {
    return this.http.post<StoryboardDto>(`${this.baseUrl}/content-items/${contentItemId}/storyboard/${storyboardId}/reopen`, request);
  }

  reconcileStoryboard(contentItemId: string, storyboardId: string, request: ReconcileStoryboardRequest): Observable<StoryboardDto> {
    return this.http.post<StoryboardDto>(`${this.baseUrl}/content-items/${contentItemId}/storyboard/${storyboardId}/reconcile`, request);
  }

  getProductionEligibility(contentItemId: string): Observable<ProductionEligibilityDto> {
    return this.http.get<ProductionEligibilityDto>(`${this.baseUrl}/content-items/${contentItemId}/storyboard/production-eligibility`);
  }

  // --- Visual Asset Generation & Production Methods ---

  dispatchVisualGeneration(contentItemId: string, storyboardId: string, request: DispatchVisualGenerationRequest): Observable<JobDto[]> {
    return this.http.post<JobDto[]>(`${this.baseUrl}/content-items/${contentItemId}/storyboards/${storyboardId}/visual-generation`, request);
  }

  getVisualProductionOverview(contentItemId: string, storyboardId: string): Observable<VisualProductionOverviewDto> {
    return this.http.get<VisualProductionOverviewDto>(`${this.baseUrl}/content-items/${contentItemId}/storyboards/${storyboardId}/visual-assets`);
  }

  getJob(jobId: string): Observable<JobDto> {
    return this.http.get<JobDto>(`${this.baseUrl}/jobs/${jobId}`);
  }

  retryJob(jobId: string): Observable<JobDto> {
    return this.http.post<JobDto>(`${this.baseUrl}/jobs/${jobId}/retry`, {});
  }

  reviewCandidate(generatedAssetId: string, request: ReviewGeneratedAssetRequest): Observable<GeneratedAssetDto> {
    return this.http.post<GeneratedAssetDto>(`${this.baseUrl}/generated-assets/${generatedAssetId}/review`, request);
  }

  selectCandidateForAssembly(generatedAssetId: string): Observable<GeneratedAssetDto> {
    return this.http.post<GeneratedAssetDto>(`${this.baseUrl}/generated-assets/${generatedAssetId}/select`, {});
  }

  getGeneratedAssetStreamUrl(generatedAssetId: string): string {
    return `${this.baseUrl}/generated-assets/${generatedAssetId}/stream`;
  }

  getGeneratedAssetThumbnailUrl(generatedAssetId: string): string {
    return `${this.baseUrl}/generated-assets/${generatedAssetId}/thumbnail`;
  }
}

// --- CF-012 / CF-013 Script Domain Interfaces ---

export type ScriptStatus = 'Draft' | 'UnderReview' | 'Approved' | 'Rejected';
export type SceneType = 'Hook' | 'Problem' | 'Insight' | 'Climax' | 'CallToAction';

export interface ScriptSceneEvidenceReferenceDto {
  id: string;
  scriptSceneId: string;
  truthSourceClaimId?: string | null;
  claimStatement: string;
  editorialNote?: string | null;
}

export interface ScriptSceneDto {
  id: string;
  scriptId: string;
  orderIndex: number;
  sceneType: SceneType | string;
  narrationText: string;
  visualPrompt: string;
  estimatedDurationSeconds: number;
  wordCount: number;
  evidenceReferences: ScriptSceneEvidenceReferenceDto[];
}

export interface ScriptDto {
  id: string;
  contentItemId: string;
  channelId: string;
  contentIdeaId: string;
  contentIdeaVersionId: string;
  truthSourceId: string;
  truthSourceVersionId: string;
  title: string;
  targetDurationSeconds: number;
  pacingWpm: number;
  estimatedDurationSeconds: number;
  totalWordCount: number;
  language: string;
  status: ScriptStatus | string;
  rejectionReason?: string | null;
  rejectedAtUtc?: string | null;
  rejectedByEmail?: string | null;
  approvedAtUtc?: string | null;
  approvedByEmail?: string | null;
  submittedForReviewAtUtc?: string | null;
  submittedForReviewByEmail?: string | null;
  isStale: boolean;
  staleReason?: string | null;
  version: number;
  createdAtUtc: string;
  createdByEmail: string;
  updatedAtUtc: string;
  updatedByEmail: string;
  scenes: ScriptSceneDto[];
}

export interface ScriptVersionDto {
  id: string;
  scriptId: string;
  contentItemId: string;
  contentIdeaId: string;
  contentIdeaVersionId: string;
  truthSourceId: string;
  truthSourceVersionId: string;
  versionNumber: number;
  snapshotJson: string;
  changeSummary: string;
  status: string;
  rejectionReason?: string | null;
  pacingWpm: number;
  estimatedDurationSeconds: number;
  totalWordCount: number;
  createdAtUtc: string;
  createdByEmail: string;
}

export interface ScriptReviewDimensionDto {
  dimension: string;
  status: 'Pass' | 'Warning' | 'Critical';
  notes: string;
}

export interface ScriptSceneCritiqueDto {
  orderIndex: number;
  sceneType: string;
  status: 'Pass' | 'Warning' | 'Critical';
  claimFidelityNotes: string;
  retentionNotes?: string | null;
  pacingNotes?: string | null;
  suggestions: string[];
}

export interface ScriptReviewResultDto {
  overallStatus: 'Pass' | 'Warning' | 'Critical';
  factualAlignmentScore: number;
  retentionAnalysis: string;
  pacingAssessment: string;
  doNotSayComplianceNotes: string[];
  dimensions: ScriptReviewDimensionDto[];
  sceneCritiques: ScriptSceneCritiqueDto[];
  actionableRecommendations: string[];
}

export interface SaveScriptSceneRequest {
  id?: string | null;
  orderIndex?: number;
  sceneType?: string;
  narrationText: string;
  visualPrompt: string;
  evidenceReferences?: { id?: string; truthSourceClaimId?: string; claimStatement: string; editorialNote?: string | null }[] | null;
}

export interface CreateScriptRequest {
  title: string;
  targetDurationSeconds?: number | null;
  pacingWpm?: number | null;
  language?: string | null;
  scenes?: SaveScriptSceneRequest[] | null;
}

export interface UpdateScriptRequest {
  title: string;
  targetDurationSeconds?: number | null;
  pacingWpm?: number | null;
  language?: string | null;
  scenes: SaveScriptSceneRequest[];
  changeSummary?: string | null;
  expectedVersion: number;
}

export interface GenerateScriptOptions {
  targetDurationSeconds?: number | null;
  pacingWpm?: number | null;
  customInstructions?: string | null;
  toneStyle?: string | null;
}

export interface SubmitScriptForReviewRequest {
  expectedVersion: number;
}

export interface ApproveScriptRequest {
  expectedVersion: number;
}

export interface RejectScriptRequest {
  reason: string;
  expectedVersion: number;
}

export interface ReopenScriptRequest {
  expectedVersion: number;
}

// --- CF-014 / CF-015 Storyboard & Production Planning Interfaces ---

export type StoryboardStatus = 'Draft' | 'UnderReview' | 'Approved' | 'Rejected';
export type FramingIntent = 'ExtremeCloseUp' | 'CloseUp' | 'MediumShot' | 'WideShot' | 'IsometricUi' | 'MotionGraphic';
export type CameraMotionIntent = 'Static' | 'SlowZoomIn' | 'PanUp' | 'TrackingShot' | 'DynamicGlitch';
export type TransitionIntent = 'Cut' | 'Dissolve' | 'Wipe' | 'ZoomIn' | 'Glitch' | 'PanUp';
export type AssetType = 'AiImage' | 'AiVideo' | 'BRoll' | 'GraphicOverlay' | 'TtsVoiceover' | 'BackgroundMusic' | 'SoundEffect' | 'SubtitleTrack';
export type AssetPlanStatus = 'Planned' | 'ReadyForGeneration';

export interface StoryboardFrameDto {
  id: string;
  storyboardId: string;
  orderIndex: number;
  scriptSceneId: string;
  scriptSceneOrderIndex: number;
  framingIntent: string;
  compositionIntent: string;
  cameraMotionIntent: string;
  subject: string;
  environment: string;
  styleIntent: string;
  visualPrompt: string;
  negativePrompt: string;
  audioCue: string;
  estimatedDurationSeconds: number;
  onScreenText: string;
  transitionIntent: string;
  createdAtUtc: string;
  updatedAtUtc: string;
}

export interface AssetRequirementDto {
  id: string;
  assetPlanId: string;
  frameId?: string | null;
  frameOrderIndex?: number | null;
  assetType: string;
  aspectRatio: string;
  visualPrompt: string;
  negativePrompt: string;
  styleIntent: string;
  motionIntent: string;
  targetDurationSeconds?: number | null;
  voiceIntent: string;
  musicMood: string;
  soundEffectIntent: string;
  subtitleProfile: string;
  overlaySpecification: string;
  createdAtUtc: string;
  updatedAtUtc: string;
}

export interface AssetPlanDto {
  id: string;
  storyboardId: string;
  contentItemId: string;
  status: string;
  version: number;
  createdAtUtc: string;
  updatedAtUtc: string;
  requirements: AssetRequirementDto[];
}

export interface StoryboardDto {
  id: string;
  contentItemId: string;
  channelId: string;
  scriptId: string;
  scriptVersionId: string;
  truthSourceId: string;
  truthSourceVersionId: string;
  isCurrent: boolean;
  supersededAtUtc?: string | null;
  reconciledFromStoryboardId?: string | null;
  title: string;
  targetDurationSeconds: number;
  totalEstimatedDurationSeconds: number;
  status: StoryboardStatus | string;
  rejectionReason?: string | null;
  rejectedAtUtc?: string | null;
  rejectedByEmail?: string | null;
  approvedAtUtc?: string | null;
  approvedByEmail?: string | null;
  submittedForReviewAtUtc?: string | null;
  submittedForReviewByEmail?: string | null;
  isStale: boolean;
  staleReason?: string | null;
  version: number;
  createdAtUtc: string;
  createdByEmail: string;
  updatedAtUtc: string;
  updatedByEmail: string;
  frames: StoryboardFrameDto[];
  assetPlan?: AssetPlanDto | null;
}

export interface StoryboardVersionDto {
  id: string;
  storyboardId: string;
  contentItemId: string;
  scriptId: string;
  scriptVersionId: string;
  truthSourceId: string;
  truthSourceVersionId: string;
  versionNumber: number;
  snapshotJson: string;
  changeSummary: string;
  status: string;
  rejectionReason?: string | null;
  totalEstimatedDurationSeconds: number;
  totalFrameCount: number;
  frameCount?: number;
  assetRequirementCount: number;
  createdAtUtc: string;
  createdByEmail: string;
}

export interface StoryboardReviewDimensionDto {
  dimension: string;
  status: 'Pass' | 'Warning' | 'Critical';
  notes: string;
}

export interface StoryboardFrameCritiqueDto {
  frameIndex?: number;
  orderIndex?: number;
  scriptSceneOrderIndex: number;
  status: 'Pass' | 'Warning' | 'Critical';
  hookVisualNotes?: string | null;
  framingVarietyNotes?: string | null;
  compositionNotes?: string | null;
  timingNotes?: string | null;
  promptFidelityNotes?: string | null;
  visualNarrativeFidelityNotes?: string;
  motionFeasibilityNotes?: string | null;
  timingAlignmentNotes?: string | null;
  suggestions: string[];
}

export interface StoryboardCritiqueResultDto {
  overallStatus: 'Pass' | 'Warning' | 'Critical';
  visualAlignmentScore: number;
  hookVisualAssessment?: string;
  framingDiversityAssessment?: string;
  timingAlignmentAssessment?: string;
  narrativeContinuityAssessment?: string;
  timingPacingAssessment?: string;
  dimensions: StoryboardReviewDimensionDto[];
  frameCritiques: StoryboardFrameCritiqueDto[];
  actionableRecommendations: string[];
}

export interface ProductionEligibilityDto {
  contentItemId?: string;
  storyboardId?: string | null;
  storyboardVersion?: number | null;
  blockerReason?: string | null;
  isEligible: boolean;
  currentStoryboardExists: boolean;
  isApproved: boolean;
  isNotStale: boolean;
  isAssetPlanComplete: boolean;
  isUpstreamLineageCurrent: boolean;
  visualRequirementCount: number;
  audioRequirementCount: number;
  subtitleRequirementCount: number;
  blockerReasons: string[];
  statusSummary: string;
}

export interface SaveStoryboardFrameRequest {
  id?: string | null;
  orderIndex?: number;
  scriptSceneId: string;
  scriptSceneOrderIndex: number;
  framingIntent?: string | null;
  compositionIntent?: string | null;
  cameraMotionIntent?: string | null;
  subject?: string | null;
  environment?: string | null;
  styleIntent?: string | null;
  visualPrompt: string;
  negativePrompt?: string | null;
  audioCue: string;
  estimatedDurationSeconds: number;
  onScreenText?: string | null;
  transitionIntent?: string | null;
}

export interface SaveAssetRequirementRequest {
  id?: string | null;
  frameId?: string | null;
  frameOrderIndex?: number | null;
  assetType?: string | null;
  aspectRatio?: string | null;
  visualPrompt?: string | null;
  negativePrompt?: string | null;
  styleIntent?: string | null;
  motionIntent?: string | null;
  targetDurationSeconds?: number | null;
  voiceIntent?: string | null;
  musicMood?: string | null;
  soundEffectIntent?: string | null;
  subtitleProfile?: string | null;
  overlaySpecification?: string | null;
}

export interface CreateStoryboardRequest {
  title: string;
  targetDurationSeconds?: number | null;
  frames?: SaveStoryboardFrameRequest[] | null;
  assetRequirements?: SaveAssetRequirementRequest[] | null;
}

export interface UpdateStoryboardRequest {
  title: string;
  targetDurationSeconds?: number | null;
  frames: SaveStoryboardFrameRequest[];
  assetRequirements?: SaveAssetRequirementRequest[] | null;
  changeSummary?: string | null;
  expectedVersion: number;
}

export interface PlanStoryboardOptions {
  targetDurationSeconds?: number | null;
  visualStylePreset?: string | null;
  cameraMotionIntensity?: string | null;
  frameDensityMultiplier?: number | null;
}

export interface SubmitStoryboardForReviewRequest {
  expectedVersion: number;
}

export interface ApproveStoryboardRequest {
  expectedVersion: number;
}

export interface RejectStoryboardRequest {
  reason: string;
  expectedVersion: number;
}

export interface ReopenStoryboardRequest {
  expectedVersion: number;
}

export interface ReconcileStoryboardRequest {
  expectedVersion: number;
  reuseFramePlanning?: boolean;
}

// --- Visual Asset Generation & Job Production Interfaces ---

export type JobStatus = 'Queued' | 'Running' | 'Succeeded' | 'FailedRetryable' | 'FailedActionRequired' | 'Cancelled';
export type GeneratedAssetStatus = 'PendingReview' | 'Approved' | 'Rejected';

export interface JobAttemptDto {
  id: string;
  jobId: string;
  attemptNumber: number;
  startedAtUtc: string;
  completedAtUtc?: string | null;
  durationMs: number;
  status: string;
  errorCode?: string | null;
  errorMessage?: string | null;
  estimatedCostUsd?: number | null;
  actualCostUsd?: number | null;
}

export interface JobDto {
  id: string;
  contentItemId: string;
  channelId: string;
  jobType: string;
  capability: string;
  sourceAssetRequirementId?: string | null;
  storyboardId?: string | null;
  storyboardVersionId?: string | null;
  generationRevision: number;
  status: JobStatus | string;
  provider: string;
  modelOrWorkflowIdentifier: string;
  attemptCount: number;
  maxAttempts: number;
  automaticRetriesRemaining: number;
  candidateCount: number;
  startedAtUtc?: string | null;
  completedAtUtc?: string | null;
  durationMs: number;
  estimatedCostUsd?: number | null;
  actualCostUsd?: number | null;
  correlationId: string;
  errorCode?: string | null;
  sanitizedErrorMessage?: string | null;
  isRetryable: boolean;
  createdByEmail: string;
  createdAtUtc: string;
  updatedAtUtc: string;
  attempts: JobAttemptDto[];
}

export interface GeneratedAssetDto {
  id: string;
  contentItemId: string;
  channelId: string;
  storyboardId: string;
  storyboardVersionId: string;
  assetRequirementId: string;
  jobId: string;
  variantIndex: number;
  assetType: string;
  mediaType: string;
  storageProvider: string;
  storageKey: string;
  contentType: string;
  fileSizeBytes: number;
  width?: number | null;
  height?: number | null;
  durationSeconds?: number | null;
  checksumSha256: string;
  provider: string;
  providerModelOrWorkflow: string;
  generationParametersSnapshot: string;
  status: GeneratedAssetStatus | string;
  rejectionReason?: string | null;
  reviewedAtUtc?: string | null;
  reviewedByEmail?: string | null;
  isSelectedForAssembly: boolean;
  createdAtUtc: string;
  updatedAtUtc: string;
  isEligibleForAssembly: boolean;
}

export interface DispatchVisualGenerationRequest {
  assetRequirementId?: string | null;
  candidateCount?: number;
  generationRevision?: number | null;
}

export interface ReviewGeneratedAssetRequest {
  status: string;
  rejectionReason?: string | null;
  expectedStatus?: string | null;
}

export interface VisualRequirementProductionDto {
  requirement: AssetRequirementDto;
  frameOrderIndex: number;
  framingIntent: string;
  scriptSceneName: string;
  estimatedDurationSeconds: number;
  activeJob?: JobDto | null;
  candidates: GeneratedAssetDto[];
  selectedCandidate?: GeneratedAssetDto | null;
}

export interface VisualProductionOverviewDto {
  contentItemId: string;
  channelId: string;
  storyboardId: string;
  storyboardVersionId: string;
  storyboardVersion: number;
  isStoryboardCurrent: boolean;
  isStoryboardApproved: boolean;
  isStoryboardStale: boolean;
  totalRequirementsCount: number;
  generatedCount: number;
  approvedCount: number;
  pendingReviewCount: number;
  activeJobsCount: number;
  isEligibleForGeneration: boolean;
  ineligibilityReason?: string | null;
  requirements: VisualRequirementProductionDto[];
}



