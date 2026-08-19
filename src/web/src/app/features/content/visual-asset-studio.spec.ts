import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { of } from 'rxjs';
import {
  ApiService,
  GeneratedAssetDto,
  JobDto,
  StoryboardDto,
  VisualProductionOverviewDto
} from '../../core/api.service';
import { VisualAssetStudioComponent } from './visual-asset-studio.component';
import { VisualCandidateCardComponent } from './visual-candidate-card.component';
import { VisualCandidatePreviewModalComponent } from './visual-candidate-preview-modal.component';
import { JobDiagnosticsDrawerComponent } from './job-diagnostics-drawer.component';

describe('Visual Asset Studio Components', () => {
  let apiService: ApiService;

  const mockStoryboard: StoryboardDto = {
    id: 'sb-1',
    contentItemId: 'item-1',
    channelId: 'chan-1',
    scriptId: 'script-1',
    scriptVersionId: 'sv-1',
    truthSourceId: 'ts-1',
    truthSourceVersionId: 'tsv-1',
    isCurrent: true,
    title: 'Test Storyboard',
    targetDurationSeconds: 45,
    totalEstimatedDurationSeconds: 45,
    status: 'Approved',
    isStale: false,
    version: 1,
    createdAtUtc: new Date().toISOString(),
    createdByEmail: 'editor@silverman.pro',
    updatedAtUtc: new Date().toISOString(),
    updatedByEmail: 'editor@silverman.pro',
    frames: []
  };

  const mockCandidate: GeneratedAssetDto = {
    id: 'asset-1',
    contentItemId: 'item-1',
    channelId: 'chan-1',
    storyboardId: 'sb-1',
    storyboardVersionId: 'sbv-1',
    assetRequirementId: 'req-1',
    jobId: 'job-1',
    variantIndex: 1,
    assetType: 'AiImage',
    mediaType: 'Image',
    storageProvider: 'MinIO',
    storageKey: 'content-factory/development/channels/chan-1/content/item-1/storyboard/sbv-1/visual/req-1/asset-1.png',
    contentType: 'image/png',
    fileSizeBytes: 102400,
    width: 1080,
    height: 1920,
    checksumSha256: 'a1b2c3d4e5',
    provider: 'Comfy',
    providerModelOrWorkflow: 'flux_schnell_vertical_9x16',
    generationParametersSnapshot: '{"prompt":"Test visual"}',
    status: 'PendingReview',
    isSelectedForAssembly: false,
    createdAtUtc: new Date().toISOString(),
    updatedAtUtc: new Date().toISOString(),
    isEligibleForAssembly: false
  };

  const mockJob: JobDto = {
    id: 'job-1',
    contentItemId: 'item-1',
    channelId: 'chan-1',
    jobType: 'generate_visual_asset',
    capability: 'generate_visual_asset',
    storyboardId: 'sb-1',
    storyboardVersionId: 'sbv-1',
    generationRevision: 1,
    status: 'FailedActionRequired',
    provider: 'Comfy',
    modelOrWorkflowIdentifier: 'flux_schnell_vertical_9x16',
    attemptCount: 1,
    maxAttempts: 3,
    automaticRetriesRemaining: 2,
    candidateCount: 1,
    durationMs: 2500,
    estimatedCostUsd: 0.005,
    actualCostUsd: null,
    correlationId: 'corr-1',
    errorCode: 'COMFY_CONNECTION_FAILED',
    sanitizedErrorMessage: 'Connection to Comfy server failed.',
    isRetryable: false,
    createdByEmail: 'editor@silverman.pro',
    createdAtUtc: new Date().toISOString(),
    updatedAtUtc: new Date().toISOString(),
    attempts: [
      {
        id: 'att-1',
        jobId: 'job-1',
        attemptNumber: 1,
        startedAtUtc: new Date().toISOString(),
        durationMs: 2500,
        status: 'FailedActionRequired',
        errorCode: 'COMFY_CONNECTION_FAILED',
        errorMessage: 'Connection refused.'
      }
    ]
  };

  const mockOverview: VisualProductionOverviewDto = {
    contentItemId: 'item-1',
    channelId: 'chan-1',
    storyboardId: 'sb-1',
    storyboardVersionId: 'sbv-1',
    storyboardVersion: 1,
    isStoryboardCurrent: true,
    isStoryboardApproved: true,
    isStoryboardStale: false,
    totalRequirementsCount: 1,
    generatedCount: 1,
    approvedCount: 0,
    pendingReviewCount: 1,
    activeJobsCount: 0,
    isEligibleForGeneration: true,
    requirements: [
      {
        requirement: {
          id: 'req-1',
          assetPlanId: 'ap-1',
          frameId: 'frame-1',
          frameOrderIndex: 1,
          assetType: 'AiImage',
          aspectRatio: '9:16',
          visualPrompt: 'Futuristic AI chip on neon motherboard',
          negativePrompt: 'blurry',
          styleIntent: 'Cyberpunk',
          motionIntent: 'Static',
          voiceIntent: '',
          musicMood: '',
          soundEffectIntent: '',
          subtitleProfile: '',
          overlaySpecification: '',
          createdAtUtc: new Date().toISOString(),
          updatedAtUtc: new Date().toISOString()
        },
        frameOrderIndex: 1,
        framingIntent: 'CloseUp',
        scriptSceneName: 'Scene 1',
        estimatedDurationSeconds: 4.0,
        activeJob: null,
        candidates: [mockCandidate],
        selectedCandidate: null
      }
    ]
  };

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [
        VisualAssetStudioComponent,
        VisualCandidateCardComponent,
        VisualCandidatePreviewModalComponent,
        JobDiagnosticsDrawerComponent
      ],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        ApiService
      ]
    }).compileComponents();

    apiService = TestBed.inject(ApiService);
  });

  it('VisualAssetStudioComponent should load and render production overview', () => {
    const fixture = TestBed.createComponent(VisualAssetStudioComponent);
    const comp = fixture.componentInstance;
    comp.contentItemId = 'item-1';
    comp.storyboard = mockStoryboard;
    comp.overview = mockOverview;
    comp.isLoading = false;

    fixture.detectChanges();

    expect(comp.overview).toBeDefined();
    expect(comp.overview?.totalRequirementsCount).toBe(1);

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('Visual Production Execution');
    expect(compiled.textContent).toContain('Futuristic AI chip on neon motherboard');
  });

  it('VisualCandidateCardComponent should render candidate details', () => {
    const fixture = TestBed.createComponent(VisualCandidateCardComponent);
    const comp = fixture.componentInstance;
    comp.candidate = mockCandidate;

    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('#1');
    expect(compiled.textContent).toContain('Comfy');
    expect(compiled.textContent).toContain('Pending Review');
    expect(compiled.textContent).toContain('1080x1920');
  });

  it('VisualCandidatePreviewModalComponent should validate rejection reason and emit events', () => {
    const fixture = TestBed.createComponent(VisualCandidatePreviewModalComponent);
    const comp = fixture.componentInstance;
    comp.visible = true;
    comp.activeCandidate = mockCandidate;
    comp.allCandidates = [mockCandidate];

    let emittedReject: { candidate: GeneratedAssetDto; reason: string } | null = null;
    comp.onReject.subscribe((val: { candidate: GeneratedAssetDto; reason: string }) => {
      emittedReject = val;
    });

    fixture.detectChanges();

    // Rejection without text should not emit
    comp.isRejecting = true;
    comp.rejectionReason = '   ';
    comp.submitRejection();
    expect(emittedReject).toBeNull();

    // Rejection with text emits
    comp.rejectionReason = 'Visual style is too dark';
    comp.submitRejection();
    expect(emittedReject).not.toBeNull();
    expect((emittedReject as any)?.reason).toBe('Visual style is too dark');
  });

  it('JobDiagnosticsDrawerComponent should render error details', () => {
    const fixture = TestBed.createComponent(JobDiagnosticsDrawerComponent);
    const comp = fixture.componentInstance;
    comp.visible = true;
    comp.job = mockJob;

    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('Technical Job Execution Diagnostics');
    expect(compiled.textContent).toContain('COMFY_CONNECTION_FAILED');
    expect(compiled.textContent).toContain('Connection to Comfy server failed.');
    expect(compiled.textContent).toContain('Attempt 1 of 3');
  });
});
