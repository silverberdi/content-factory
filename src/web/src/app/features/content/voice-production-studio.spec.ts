import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import {
  ApiService,
  GeneratedAssetDto,
  VoiceProductionOverviewDto
} from '../../core/api.service';
import { VoiceProductionStudioComponent } from './voice-production-studio.component';
import { VoiceCandidateCardComponent } from './voice-candidate-card.component';
import { VoiceAudioPlayerComponent } from './voice-audio-player.component';

describe('Voice Production Studio Components', () => {
  const mockCandidate: GeneratedAssetDto = {
    id: 'asset-voice-1',
    contentItemId: 'item-1',
    channelId: 'chan-1',
    storyboardId: 'sb-1',
    storyboardVersionId: 'sbv-1',
    assetRequirementId: 'req-voice-1',
    jobId: 'job-voice-1',
    variantIndex: 1,
    assetType: 'TtsVoiceover',
    mediaType: 'Audio',
    storageProvider: 'MinIO',
    storageKey: 'content-factory/development/channels/chan-1/content/item-1/storyboard/sbv-1/audio/voice/req-voice-1/asset-voice-1.wav',
    contentType: 'audio/wav',
    fileSizeBytes: 204800,
    durationSeconds: 44.8,
    checksumSha256: 'a1b2c3d4e5f6',
    provider: 'Gemini',
    providerModelOrWorkflow: 'gemini-3.8-flash-lite-tts:Kore',
    generationParametersSnapshot: '{"language":"es","profileKey":"ia-simple-es-default"}',
    status: 'PendingReview',
    isSelectedForAssembly: false,
    createdAtUtc: new Date().toISOString(),
    updatedAtUtc: new Date().toISOString(),
    isEligibleForAssembly: false
  };

  const mockOverview: VoiceProductionOverviewDto = {
    contentItemId: 'item-1',
    channelId: 'chan-1',
    scriptId: 'script-1',
    scriptVersionId: 'sv-1',
    storyboardId: 'sb-1',
    storyboardVersionId: 'sbv-1',
    storyboardVersion: 1,
    narrationFingerprint: 'f1e2d3c4',
    scriptEstimatedDurationSeconds: 45.0,
    measuredVoiceDurationSeconds: 44.8,
    timingDeviationSeconds: -0.2,
    timingStatus: 'Normal',
    effectiveVoiceProfile: {
      profileKey: 'ia-simple-es-default',
      profileName: 'IA Simple ES Narrator',
      language: 'es',
      configuredLocale: 'es-ES',
      speakingStyle: 'Explanatory',
      tone: 'Credible, Modern',
      warmth: 'MediumHigh',
      energy: 'Dynamic',
      pace: 'Normal'
    },
    isStoryboardCurrent: true,
    isStoryboardApproved: true,
    isStoryboardStale: false,
    totalRequirementsCount: 1,
    generatedCount: 1,
    approvedCount: 0,
    pendingReviewCount: 1,
    activeJobsCount: 0,
    isEligibleForGeneration: true,
    ineligibilityReason: null,
    requirements: [
      {
        requirement: {
          id: 'req-voice-1',
          assetPlanId: 'plan-1',
          frameOrderIndex: 1,
          assetType: 'TtsVoiceover',
          aspectRatio: '9:16',
          visualPrompt: '',
          negativePrompt: '',
          styleIntent: '',
          motionIntent: '',
          targetDurationSeconds: 45.0,
          voiceIntent: 'Clear Spanish voiceover',
          musicMood: '',
          soundEffectIntent: '',
          subtitleProfile: '',
          overlaySpecification: '',
          createdAtUtc: new Date().toISOString(),
          updatedAtUtc: new Date().toISOString()
        },
        frameOrderIndex: 1,
        scriptSceneName: 'Full Narration',
        estimatedDurationSeconds: 45.0,
        activeJob: null,
        candidates: [mockCandidate],
        selectedCandidate: null,
        timingDeviationSeconds: -0.2,
        timingStatus: 'Normal'
      }
    ],
    scriptScenes: [
      {
        sceneId: 'scene-1',
        orderIndex: 1,
        sceneType: 'Hook',
        narrationText: 'La inteligencia artificial simplifica el trabajo.',
        wordCount: 7,
        estimatedDurationSeconds: 3.0
      }
    ]
  };

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [
        VoiceProductionStudioComponent,
        VoiceCandidateCardComponent,
        VoiceAudioPlayerComponent
      ],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([])
      ]
    }).compileComponents();
  });

  it('VoiceAudioPlayerComponent should format time correctly', () => {
    const fixture = TestBed.createComponent(VoiceAudioPlayerComponent);
    const component = fixture.componentInstance;
    component.audioUrl = 'http://localhost:5000/api/generated-assets/asset-1/audio-stream';
    component.initialDurationSeconds = 45.0;
    fixture.detectChanges();

    expect(component).toBeTruthy();
    expect(component.formatTime(65)).toBe('1:05');
    expect(component.formatTime(0)).toBe('0:00');
    expect(component.formatTime(125)).toBe('2:05');
  });

  it('VoiceAudioPlayerComponent should cycle playback rate', () => {
    const fixture = TestBed.createComponent(VoiceAudioPlayerComponent);
    const component = fixture.componentInstance;
    component.audioUrl = 'http://localhost:5000/api/generated-assets/asset-1/audio-stream';
    fixture.detectChanges();

    expect(component.playbackRate()).toBe(1.0);
    component.cyclePlaybackRate();
    expect(component.playbackRate()).toBe(1.25);
    component.cyclePlaybackRate();
    expect(component.playbackRate()).toBe(1.5);
  });

  it('VoiceCandidateCardComponent should render candidate details and deviation', () => {
    const fixture = TestBed.createComponent(VoiceCandidateCardComponent);
    const component = fixture.componentInstance;
    component.candidate = mockCandidate;
    component.estimatedDurationSeconds = 45.0;
    fixture.detectChanges();

    expect(component).toBeTruthy();
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('Toma #1');
    expect(compiled.textContent).toContain('44.8s');

    const dev = component.getTimingDeviation();
    expect(dev.text).toBe('-0.2s');
    expect(dev.status).toBe('Normal');
  });

  it('VoiceCandidateCardComponent should handle rejection flow and validate reason', () => {
    const fixture = TestBed.createComponent(VoiceCandidateCardComponent);
    const component = fixture.componentInstance;
    component.candidate = mockCandidate;
    fixture.detectChanges();

    let emittedReject: { id: string; reason: string } | null = null;
    component.onReject.subscribe((val) => {
      emittedReject = val;
    });

    component.startReject();
    expect(component.isRejecting()).toBe(true);

    // Confirm with empty reason should not emit
    component.rejectionInput = '   ';
    component.confirmReject();
    expect(emittedReject).toBeNull();

    // Confirm with valid reason emits
    component.rejectionInput = 'Pacing is too fast';
    component.confirmReject();
    expect(emittedReject).not.toBeNull();
    expect((emittedReject as any)?.id).toBe(mockCandidate.id);
    expect((emittedReject as any)?.reason).toBe('Pacing is too fast');
    expect(component.isRejecting()).toBe(false);

    // Cancel rejection resets state
    component.startReject();
    component.rejectionInput = 'Some text';
    component.cancelReject();
    expect(component.isRejecting()).toBe(false);
    expect(component.rejectionInput).toBe('');
  });

  it('VoiceProductionStudioComponent should render telemetry and script scenes', () => {
    const fixture = TestBed.createComponent(VoiceProductionStudioComponent);
    const component = fixture.componentInstance;
    component.contentItemId = 'item-1';
    component.storyboardId = 'sb-1';
    component.overview = mockOverview;
    component.isLoading = false;
    fixture.detectChanges();

    expect(component.overview).toBeDefined();
    expect(component.overview?.effectiveVoiceProfile?.profileName).toBe('IA Simple ES Narrator');
    expect(component.overview?.scriptEstimatedDurationSeconds).toBe(45.0);
    expect(component.overview?.measuredVoiceDurationSeconds).toBe(44.8);

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('Producción de Voz & Locución TTS');
    expect(compiled.textContent).toContain('IA Simple ES Narrator');
    expect(compiled.textContent).toContain('44.8s');
  });
});
