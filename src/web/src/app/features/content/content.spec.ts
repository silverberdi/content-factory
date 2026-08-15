import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { ContentListComponent } from './content-list.component';
import { ContentDetailComponent } from './content-detail.component';
import { TruthSourceReviewStudioComponent } from './truth-source-review-studio.component';
import { EditorialTasksListComponent } from './editorial-tasks-list.component';
import { AttachEvidenceModalComponent } from './attach-evidence-modal.component';
import { ApiService, ContentItemDetailDto, TruthSourceDto } from '../../core/api.service';
import { AuthService } from '../../core/auth.service';

describe('Content & TruthSource Feature Components', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [
        ContentListComponent,
        ContentDetailComponent,
        TruthSourceReviewStudioComponent,
        EditorialTasksListComponent,
        AttachEvidenceModalComponent
      ],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        ApiService,
        AuthService
      ]
    }).compileComponents();
  });

  it('should create ContentListComponent', () => {
    const fixture = TestBed.createComponent(ContentListComponent);
    const component = fixture.componentInstance;
    expect(component).toBeTruthy();
  });

  it('should create ContentDetailComponent and render newly created item with empty TruthSource and Evidences', () => {
    const fixture = TestBed.createComponent(ContentDetailComponent);
    const component = fixture.componentInstance;

    const newItem: ContentItemDetailDto = {
      id: 'c2b63168-c862-45f9-ac4e-eafd5e5930bd',
      channelId: '00000000-0000-0000-0000-000000000010',
      channelName: 'IA Simple ES',
      title: '3 Casos de Uso Reales de IA en Logística 2026',
      slug: '3-casos-de-uso-reales-de-ia-en-logistica-2026',
      stage: 'DraftingEvidence',
      status: 'Active',
      version: 1,
      createdAtUtc: new Date().toISOString(),
      createdByEmail: 'silverio.bernal@gmail.com',
      updatedAtUtc: new Date().toISOString(),
      updatedByEmail: 'silverio.bernal@gmail.com',
      evidences: [],
      truthSource: null
    };

    component.item.set(newItem);
    component.isLoading.set(false);
    component.errorMessage.set(null);
    fixture.detectChanges();

    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent).toContain('3 Casos de Uso Reales de IA en Logística 2026');
    expect(el.textContent).toContain('DraftingEvidence');
    expect(el.textContent).toContain('IA Simple ES');
    expect(el.textContent).toContain('No hay evidencias adjuntas a esta pieza');
    expect(el.textContent).toContain('Aún no se ha sintetizado el TruthSource');
  });

  it('should disable Generate AI Draft button in ContentDetailComponent when only CaptureFailed evidence is present', () => {
    const fixture = TestBed.createComponent(ContentDetailComponent);
    const component = fixture.componentInstance;

    const itemWithFailedEvidence: ContentItemDetailDto = {
      id: 'c2b63168-c862-45f9-ac4e-eafd5e5930bd',
      channelId: '00000000-0000-0000-0000-000000000010',
      channelName: 'IA Simple ES',
      title: 'Failed Evidence Test Piece',
      slug: 'failed-evidence-test-piece',
      stage: 'DraftingEvidence',
      status: 'Active',
      version: 1,
      createdAtUtc: new Date().toISOString(),
      createdByEmail: 'silverio.bernal@gmail.com',
      updatedAtUtc: new Date().toISOString(),
      updatedByEmail: 'silverio.bernal@gmail.com',
      evidences: [
        {
          id: '00000000-0000-0000-0000-000000000311',
          contentItemId: 'c2b63168-c862-45f9-ac4e-eafd5e5930bd',
          discoveryCandidateId: '00000000-0000-0000-0000-000000000001',
          originUrl: 'https://www.genbeta.com/dead-link',
          title: 'Genbeta Failed Link',
          role: 'PrimaryLead',
          status: 'CaptureFailed',
          rawContent: null,
          objectStorageKey: null,
          extractedText: null,
          contentHash: null,
          errorMessage: 'HTTP 404 Not Found',
          notes: null,
          author: null,
          capturedAtUtc: null,
          createdAtUtc: new Date().toISOString(),
          createdByEmail: 'silverio.bernal@gmail.com'
        }
      ],
      truthSource: null
    };

    component.item.set(itemWithFailedEvidence);
    component.isLoading.set(false);
    fixture.detectChanges();

    expect(component.hasUsableEvidence).toBe(false);

    const el = fixture.nativeElement as HTMLElement;
    const generateBtn = el.querySelector('button[disabled]') as HTMLButtonElement;
    expect(generateBtn).toBeTruthy();
    expect(generateBtn.textContent).toContain('Generar Borrador con IA');
    expect(el.textContent).toContain('Requiere al menos 1 evidencia capturada con éxito.');
  });

  it('should enable Generate AI Draft button in ContentDetailComponent when at least one Captured evidence is present', () => {
    const fixture = TestBed.createComponent(ContentDetailComponent);
    const component = fixture.componentInstance;

    const itemWithCapturedEvidence: ContentItemDetailDto = {
      id: 'c2b63168-c862-45f9-ac4e-eafd5e5930bd',
      channelId: '00000000-0000-0000-0000-000000000010',
      channelName: 'IA Simple ES',
      title: 'Captured Evidence Test Piece',
      slug: 'captured-evidence-test-piece',
      stage: 'DraftingEvidence',
      status: 'Active',
      version: 1,
      createdAtUtc: new Date().toISOString(),
      createdByEmail: 'silverio.bernal@gmail.com',
      updatedAtUtc: new Date().toISOString(),
      updatedByEmail: 'silverio.bernal@gmail.com',
      evidences: [
        {
          id: '00000000-0000-0000-0000-000000000311',
          contentItemId: 'c2b63168-c862-45f9-ac4e-eafd5e5930bd',
          discoveryCandidateId: null,
          originUrl: null,
          title: 'Manual Context Note',
          role: 'PrimaryLead',
          status: 'Captured',
          rawContent: 'Nota con hechos verificables...',
          objectStorageKey: null,
          extractedText: 'Nota con hechos verificables...',
          contentHash: 'a1b2c3d4e5f6',
          errorMessage: null,
          notes: null,
          author: null,
          capturedAtUtc: new Date().toISOString(),
          createdAtUtc: new Date().toISOString(),
          createdByEmail: 'silverio.bernal@gmail.com'
        }
      ],
      truthSource: null
    };

    component.item.set(itemWithCapturedEvidence);
    component.isLoading.set(false);
    fixture.detectChanges();

    expect(component.hasUsableEvidence).toBe(true);

    const el = fixture.nativeElement as HTMLElement;
    const buttons = Array.from(el.querySelectorAll('button'));
    const generateBtn = buttons.find(b => b.textContent?.includes('Generar Borrador con IA'));
    expect(generateBtn).toBeTruthy();
    expect(generateBtn?.disabled).toBe(false);
    expect(el.textContent).not.toContain('Requiere al menos 1 evidencia capturada con éxito.');
  });

  it('should render error and retry option in ContentDetailComponent on failure', () => {
    const fixture = TestBed.createComponent(ContentDetailComponent);
    const component = fixture.componentInstance;

    fixture.detectChanges();

    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent).toContain('Identificador de pieza de contenido inválido o ausente.');
    expect(el.textContent).toContain('Reintentar');
    expect(el.textContent).toContain('Volver al Workspace');
  });

  it('should create EditorialTasksListComponent', () => {
    const fixture = TestBed.createComponent(EditorialTasksListComponent);
    const component = fixture.componentInstance;
    expect(component).toBeTruthy();
  });

  it('should create and render AttachEvidenceModalComponent', () => {
    const fixture = TestBed.createComponent(AttachEvidenceModalComponent);
    const component = fixture.componentInstance;

    component.isOpen = true;
    component.contentItemId = '00000000-0000-0000-0000-000000000301';
    fixture.detectChanges();

    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent).toContain('Adjuntar Evidencia de Origen');
    expect(el.textContent).toContain('Enlace Web / URL');

    let closed = false;
    component.closed.subscribe(() => (closed = true));
    component.close();
    expect(closed).toBe(true);
  });

  it('should render TruthSourceReviewStudioComponent for a newly created item without TruthSource', () => {
    const fixture = TestBed.createComponent(TruthSourceReviewStudioComponent);
    const component = fixture.componentInstance;

    const newItem: ContentItemDetailDto = {
      id: 'c2b63168-c862-45f9-ac4e-eafd5e5930bd',
      channelId: '00000000-0000-0000-0000-000000000010',
      channelName: 'IA Simple ES',
      title: '3 Casos de Uso Reales de IA en Logística 2026',
      slug: '3-casos-de-uso-reales-de-ia-en-logistica-2026',
      stage: 'DraftingEvidence',
      status: 'Active',
      version: 1,
      createdAtUtc: new Date().toISOString(),
      createdByEmail: 'silverio.bernal@gmail.com',
      updatedAtUtc: new Date().toISOString(),
      updatedByEmail: 'silverio.bernal@gmail.com',
      evidences: [],
      truthSource: null
    };

    component.contentItem.set(newItem);
    component.truthSource.set(null);
    component.isLoading.set(false);
    component.errorMessage.set(null);
    fixture.detectChanges();

    expect(component.hasUsableEvidence).toBe(false);

    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent).toContain('TruthSource Review Studio');
    expect(el.textContent).toContain('3 Casos de Uso Reales de IA en Logística 2026');
    expect(el.textContent).toContain('Borrador de TruthSource no generado');
    expect(el.textContent).toContain('Sintetizar con IA');
    expect(el.textContent).toContain('Requiere al menos 1 evidencia capturada con éxito.');
  });

  it('should render TruthSourceReviewStudioComponent with side-by-side layout and data', () => {
    const fixture = TestBed.createComponent(TruthSourceReviewStudioComponent);
    const component = fixture.componentInstance;

    const mockTruthSource: TruthSourceDto = {
      id: '00000000-0000-0000-0000-000000000321',
      contentItemId: '00000000-0000-0000-0000-000000000301',
      status: 'Approved',
      summary: 'Resumen factual verificado de prueba.',
      keyIdeas: ['Idea clave 1', 'Idea clave 2'],
      verifiableClaims: [
        {
          claim: 'El 68% de las empresas evalúan pensamiento crítico.',
          sourceCitation: 'El País 2026',
          evidenceId: '00000000-0000-0000-0000-000000000311'
        }
      ],
      evidenceReferences: ['00000000-0000-0000-0000-000000000311'],
      riskNotes: 'Evitar sensacionalismo.',
      doNotSayConstraints: ['No prometer fórmulas mágicas.'],
      possibleAngles: ['Ángulo 1'],
      localizationNotes: 'Español neutro.',
      rejectionReason: null,
      rejectedAtUtc: null,
      rejectedByEmail: null,
      approvedAtUtc: new Date().toISOString(),
      approvedByEmail: 'silverio.bernal@gmail.com',
      version: 2,
      createdAtUtc: new Date().toISOString(),
      createdByEmail: 'silverio.bernal@gmail.com',
      updatedAtUtc: new Date().toISOString(),
      updatedByEmail: 'silverio.bernal@gmail.com'
    };

    const mockDetail: ContentItemDetailDto = {
      id: '00000000-0000-0000-0000-000000000301',
      channelId: '00000000-0000-0000-0000-000000000010',
      channelName: 'IA Simple ES',
      title: '3 Habilidades Clave en 2026',
      slug: '3-habilidades-clave-2026',
      stage: 'TruthSourceApproved',
      status: 'Active',
      version: 2,
      createdAtUtc: new Date().toISOString(),
      createdByEmail: 'silverio.bernal@gmail.com',
      updatedAtUtc: new Date().toISOString(),
      updatedByEmail: 'silverio.bernal@gmail.com',
      evidences: [
        {
          id: '00000000-0000-0000-0000-000000000311',
          contentItemId: '00000000-0000-0000-0000-000000000301',
          discoveryCandidateId: null,
          originUrl: 'https://elpais.com/empleo-ia',
          title: 'El impacto de la IA en empleo',
          role: 'PrimaryLead',
          status: 'Captured',
          rawContent: 'Texto extraído de prueba...',
          objectStorageKey: null,
          extractedText: 'Texto extraído de prueba...',
          contentHash: 'a1b2c3d4e5f6',
          errorMessage: null,
          notes: null,
          author: null,
          capturedAtUtc: new Date().toISOString(),
          createdAtUtc: new Date().toISOString(),
          createdByEmail: 'silverio.bernal@gmail.com'
        }
      ],
      truthSource: mockTruthSource
    };

    component.contentItem.set(mockDetail);
    component.truthSource.set(mockTruthSource);
    component.isLoading.set(false);
    fixture.detectChanges();

    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent).toContain('TruthSource Review Studio');
    expect(el.textContent).toContain('3 Habilidades Clave en 2026');
    expect(el.textContent).toContain('Evidencias Capturadas');
    expect(el.textContent).toContain('Resumen factual verificado de prueba');
    expect(el.textContent).toContain('Idea clave 1');
    expect(el.textContent).toContain('El 68% de las empresas evalúan pensamiento crítico');
    expect(el.textContent).toContain('No prometer fórmulas mágicas');
  });
});
