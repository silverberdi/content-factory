import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { ContentListComponent } from './content-list.component';
import { ContentDetailComponent } from './content-detail.component';
import { TruthSourceReviewStudioComponent } from './truth-source-review-studio.component';
import { ContentIdeasComponent } from './content-ideas.component';
import { GenerateIdeasModalComponent } from './generate-ideas-modal.component';
import { IdeaEditDrawerComponent } from './idea-edit-drawer.component';
import { IdeaVersionHistoryDrawerComponent } from './idea-version-history-drawer.component';
import { EditorialTasksListComponent } from './editorial-tasks-list.component';
import { AttachEvidenceModalComponent } from './attach-evidence-modal.component';
import { ScriptStudioComponent } from './script-studio.component';
import { ScriptSceneCardComponent } from './script-scene-card.component';
import { GenerateScriptModalComponent } from './generate-script-modal.component';
import { ScriptReviewPanelComponent } from './script-review-panel.component';
import { ScriptVersionHistoryDrawerComponent } from './script-version-history-drawer.component';
import { RejectScriptModalComponent } from './reject-script-modal.component';
import { ApiService, ContentIdeaDto, ContentIdeaVersionDto, ContentItemDetailDto, ScriptDto, ScriptReviewResultDto, ScriptVersionDto, TruthSourceDto } from '../../core/api.service';
import { AuthService } from '../../core/auth.service';

describe('Content & TruthSource Feature Components', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [
        ContentListComponent,
        ContentDetailComponent,
        TruthSourceReviewStudioComponent,
        ContentIdeasComponent,
        GenerateIdeasModalComponent,
        IdeaEditDrawerComponent,
        IdeaVersionHistoryDrawerComponent,
        EditorialTasksListComponent,
        AttachEvidenceModalComponent,
        ScriptStudioComponent,
        ScriptSceneCardComponent,
        GenerateScriptModalComponent,
        ScriptReviewPanelComponent,
        ScriptVersionHistoryDrawerComponent,
        RejectScriptModalComponent
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

  it('should create ContentIdeasComponent and render Idea Matrix with Proposed and Selected ideas', () => {
    const fixture = TestBed.createComponent(ContentIdeasComponent);
    const component = fixture.componentInstance;

    const mockDetail: ContentItemDetailDto = {
      id: '00000000-0000-0000-0000-000000000301',
      channelId: '00000000-0000-0000-0000-000000000010',
      channelName: 'IA Simple ES',
      title: '3 Habilidades Clave en 2026',
      slug: '3-habilidades-clave-2026',
      stage: 'IdeaSelected',
      status: 'Active',
      version: 2,
      createdAtUtc: new Date().toISOString(),
      createdByEmail: 'silverio.bernal@gmail.com',
      updatedAtUtc: new Date().toISOString(),
      updatedByEmail: 'silverio.bernal@gmail.com',
      evidences: [],
      truthSource: {
        id: '00000000-0000-0000-0000-000000000302',
        contentItemId: '00000000-0000-0000-0000-000000000301',
        status: 'Approved',
        summary: 'Resumen factual...',
        keyIdeas: [],
        verifiableClaims: [],
        evidenceReferences: [],
        riskNotes: '',
        doNotSayConstraints: [],
        possibleAngles: [],
        localizationNotes: '',
        rejectionReason: null,
        rejectedAtUtc: null,
        rejectedByEmail: null,
        approvedAtUtc: new Date().toISOString(),
        approvedByEmail: 'silverio.bernal@gmail.com',
        version: 1,
        createdAtUtc: new Date().toISOString(),
        createdByEmail: 'silverio.bernal@gmail.com',
        updatedAtUtc: new Date().toISOString(),
        updatedByEmail: 'silverio.bernal@gmail.com'
      }
    };

    const mockIdeas: ContentIdeaDto[] = [
      {
        id: '00000000-0000-0000-0000-000000000401',
        contentItemId: '00000000-0000-0000-0000-000000000301',
        truthSourceId: '00000000-0000-0000-0000-000000000302',
        truthSourceVersionId: '00000000-0000-0000-0000-000000000303',
        title: '3 Habilidades Clave que la IA No Reemplaza en 2026',
        angle: 'Enfoque contraintuitivo / Empoderamiento profesional',
        hookStrategy: '¿Crees que un prompt te salvará en 2026?',
        audienceValue: 'Aprender pensamiento crítico y auditoría humana',
        format: 'YouTube Short 30-60s',
        intendedOutcome: 'Inspiración / Retención',
        freshnessClass: 'Timely',
        priority: 'High',
        rationale: 'Aprovecha la síntesis factual',
        status: 'Selected',
        dismissalNotes: null,
        selectedAtUtc: new Date().toISOString(),
        selectedByEmail: 'operator@silverman.pro',
        version: 1,
        createdAtUtc: new Date().toISOString(),
        createdByEmail: 'operator@silverman.pro',
        updatedAtUtc: new Date().toISOString(),
        updatedByEmail: 'operator@silverman.pro'
      },
      {
        id: '00000000-0000-0000-0000-000000000402',
        contentItemId: '00000000-0000-0000-0000-000000000301',
        truthSourceId: '00000000-0000-0000-0000-000000000302',
        truthSourceVersionId: '00000000-0000-0000-0000-000000000303',
        title: 'El Error de 1.000€ que Cometen al Delegar Tareas en IA',
        angle: 'Alerta de riesgo operativo en contabilidad',
        hookStrategy: 'Un fallo tonto en una respuesta de IA puede costarte carísimo',
        audienceValue: 'Checklist de 3 pasos para auditar resúmenes',
        format: 'YouTube Short 30-60s',
        intendedOutcome: 'Prevención de errores',
        freshnessClass: 'Evergreen',
        priority: 'Normal',
        rationale: 'Guardrails de precisión',
        status: 'Proposed',
        dismissalNotes: null,
        selectedAtUtc: null,
        selectedByEmail: null,
        version: 1,
        createdAtUtc: new Date().toISOString(),
        createdByEmail: 'operator@silverman.pro',
        updatedAtUtc: new Date().toISOString(),
        updatedByEmail: 'operator@silverman.pro'
      }
    ];

    component.contentItem.set(mockDetail);
    component.ideas.set(mockIdeas);
    component.isLoading.set(false);
    fixture.detectChanges();

    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent).toContain('Matriz de Ideas y Ángulos Creativos');
    expect(el.textContent).toContain('3 Habilidades Clave que la IA No Reemplaza en 2026');
    expect(el.textContent).toContain('El Error de 1.000€ que Cometen al Delegar Tareas en IA');
    expect(el.textContent).toContain('Idea Activa Seleccionada');
    expect(component.selectedIdea()?.id).toBe('00000000-0000-0000-0000-000000000401');
    expect(component.getCountForFilter('PROPOSED')).toBe(1);
    expect(component.getCountForFilter('SELECTED')).toBe(1);
  });

  it('should render GenerateIdeasModalComponent and emit generated ideas', () => {
    const fixture = TestBed.createComponent(GenerateIdeasModalComponent);
    const component = fixture.componentInstance;

    component.isOpen = true;
    component.contentItemId = '00000000-0000-0000-0000-000000000301';
    component.truthSourceVersionNumber = 1;
    fixture.detectChanges();

    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent).toContain('Generar Propuestas de Ideas con IA');
    expect(el.textContent).toContain('DeepSeek Reasoning');
    expect(component.count).toBe(3);

    component.count = 4;
    expect(component.count).toBe(4);
  });

  it('should render IdeaEditDrawerComponent in edit mode and populate form values', () => {
    const fixture = TestBed.createComponent(IdeaEditDrawerComponent);
    const component = fixture.componentInstance;

    const mockIdea: ContentIdeaDto = {
      id: '00000000-0000-0000-0000-000000000401',
      contentItemId: '00000000-0000-0000-0000-000000000301',
      truthSourceId: '00000000-0000-0000-0000-000000000302',
      truthSourceVersionId: '00000000-0000-0000-0000-000000000303',
      title: 'Idea Editable Original',
      angle: 'Ángulo Original',
      hookStrategy: 'Gancho Original',
      audienceValue: 'Valor Original',
      format: 'YouTube Short 30-60s',
      intendedOutcome: 'Outcome',
      freshnessClass: 'Timely',
      priority: 'Normal',
      rationale: 'Rationale',
      status: 'Proposed',
      dismissalNotes: null,
      selectedAtUtc: null,
      selectedByEmail: null,
      version: 2,
      createdAtUtc: new Date().toISOString(),
      createdByEmail: 'operator@silverman.pro',
      updatedAtUtc: new Date().toISOString(),
      updatedByEmail: 'operator@silverman.pro'
    };

    component.isOpen = true;
    component.contentItemId = '00000000-0000-0000-0000-000000000301';
    component.idea = mockIdea;
    component.ngOnChanges({
      isOpen: {
        currentValue: true,
        previousValue: false,
        firstChange: true,
        isFirstChange: () => true
      }
    });
    fixture.detectChanges();

    expect(component.isEditMode).toBe(true);
    expect(component.form.title).toBe('Idea Editable Original');
    expect(component.form.angle).toBe('Ángulo Original');
    expect(component.form.hookStrategy).toBe('Gancho Original');

    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent).toContain('Editar Idea (v2)');
    expect(el.textContent).toContain('Bloqueo Optimista Activo');
  });

  it('should render IdeaVersionHistoryDrawerComponent with version history timeline', () => {
    const fixture = TestBed.createComponent(IdeaVersionHistoryDrawerComponent);
    const component = fixture.componentInstance;

    const mockVersions: ContentIdeaVersionDto[] = [
      {
        id: '00000000-0000-0000-0000-000000000501',
        contentIdeaId: '00000000-0000-0000-0000-000000000401',
        contentItemId: '00000000-0000-0000-0000-000000000301',
        truthSourceId: '00000000-0000-0000-0000-000000000302',
        truthSourceVersionId: '00000000-0000-0000-0000-000000000303',
        versionNumber: 1,
        title: 'Versión 1 de la Idea',
        angle: 'Ángulo V1',
        hookStrategy: 'Gancho V1',
        audienceValue: 'Valor V1',
        format: 'YouTube Short 30-60s',
        intendedOutcome: 'Outcome',
        freshnessClass: 'Timely',
        priority: 'Normal',
        rationale: 'Rationale',
        status: 'Proposed',
        dismissalNotes: null,
        editedByEmail: 'operator@silverman.pro',
        editedAtUtc: new Date().toISOString(),
        changeSummary: 'Creación inicial por IA.'
      }
    ];

    component.isOpen = true;
    component.idea = {
      id: '00000000-0000-0000-0000-000000000401',
      contentItemId: '00000000-0000-0000-0000-000000000301',
      truthSourceId: '00000000-0000-0000-0000-000000000302',
      truthSourceVersionId: '00000000-0000-0000-0000-000000000303',
      title: 'Idea de Prueba',
      angle: 'Ángulo',
      hookStrategy: 'Gancho',
      audienceValue: 'Valor',
      format: 'YouTube Short 30-60s',
      intendedOutcome: 'Outcome',
      freshnessClass: 'Timely',
      priority: 'Normal',
      rationale: 'Rationale',
      status: 'Proposed',
      dismissalNotes: null,
      selectedAtUtc: null,
      selectedByEmail: null,
      version: 1,
      createdAtUtc: new Date().toISOString(),
      createdByEmail: 'operator@silverman.pro',
      updatedAtUtc: new Date().toISOString(),
      updatedByEmail: 'operator@silverman.pro'
    };
    component.versions.set(mockVersions);
    component.isLoading.set(false);
    fixture.detectChanges();

    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent).toContain('Historial de Versiones');
    expect(el.textContent).toContain('Versión 1 de la Idea');
    expect(el.textContent).toContain('Creación inicial por IA.');
  });

  it('should surface HTTP 409 concurrency conflict in IdeaEditDrawerComponent and offer reload action', () => {
    const fixture = TestBed.createComponent(IdeaEditDrawerComponent);
    const component = fixture.componentInstance;

    component.isOpen = true;
    component.concurrencyError.set('La idea fue modificada por otro operador concurrentemente. Por favor recarga los últimos cambios.');
    fixture.detectChanges();

    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent).toContain('Conflicto de Edición Concurrente (HTTP 409)');
    expect(el.textContent).toContain('La idea fue modificada por otro operador concurrentemente');
    expect(el.textContent).toContain('Recargar Versión Más Reciente');
  });

  it('should create ScriptStudioComponent and render live pacing, scenes, and duration metrics', () => {
    const fixture = TestBed.createComponent(ScriptStudioComponent);
    const component = fixture.componentInstance;

    const mockDetail: ContentItemDetailDto = {
      id: '00000000-0000-0000-0000-000000000301',
      channelId: '00000000-0000-0000-0000-000000000010',
      channelName: 'IA Simple ES',
      title: '3 Habilidades Clave en 2026',
      slug: '3-habilidades-clave-2026',
      stage: 'ScriptUnderReview',
      status: 'Active',
      version: 2,
      createdAtUtc: new Date().toISOString(),
      createdByEmail: 'operator@silverman.pro',
      updatedAtUtc: new Date().toISOString(),
      updatedByEmail: 'operator@silverman.pro',
      evidences: []
    };

    const mockScript: ScriptDto = {
      id: '00000000-0000-0000-0000-000000000501',
      contentItemId: '00000000-0000-0000-0000-000000000301',
      channelId: '00000000-0000-0000-0000-000000000010',
      contentIdeaId: '00000000-0000-0000-0000-000000000401',
      contentIdeaVersionId: '00000000-0000-0000-0000-000000000401',
      truthSourceId: '00000000-0000-0000-0000-000000000302',
      truthSourceVersionId: '00000000-0000-0000-0000-000000000303',
      title: '3 Habilidades que la IA NO te Puede Quitar en 2026',
      targetDurationSeconds: 45,
      pacingWpm: 140,
      estimatedDurationSeconds: 43.7,
      totalWordCount: 102,
      language: 'es-ES',
      status: 'UnderReview',
      isStale: false,
      staleReason: null,
      version: 1,
      createdAtUtc: new Date().toISOString(),
      createdByEmail: 'operator@silverman.pro',
      updatedAtUtc: new Date().toISOString(),
      updatedByEmail: 'operator@silverman.pro',
      scenes: [
        {
          id: 'sc-1',
          scriptId: '00000000-0000-0000-0000-000000000501',
          orderIndex: 1,
          sceneType: 'Hook',
          narrationText: '¿Crees que un prompt te salvará el empleo en 2026? Te equivocas: estas 3 habilidades valen 10 veces más.',
          visualPrompt: 'Primer plano directo a cámara',
          estimatedDurationSeconds: 7.7,
          wordCount: 18,
          evidenceReferences: [
            {
              id: 'er-1',
              scriptSceneId: 'sc-1',
              truthSourceClaimId: '00000000-0000-0000-0000-000000000001',
              claimStatement: 'El 68% de las empresas priorizan criterio analítico.',
              editorialNote: 'Gancho inicial respaldado'
            }
          ]
        },
        {
          id: 'sc-2',
          scriptId: '00000000-0000-0000-0000-000000000501',
          orderIndex: 2,
          sceneType: 'Problem',
          narrationText: 'Generar texto en 5 segundos no impresiona a nadie si el resultado contiene alucinaciones.',
          visualPrompt: 'B-roll oficina con marcas de error',
          estimatedDurationSeconds: 6.0,
          wordCount: 14,
          evidenceReferences: []
        }
      ]
    };

    component.contentItemId.set('00000000-0000-0000-0000-000000000301');
    component.contentItem.set(mockDetail);
    component.script.set(mockScript);
    component.scenes.set(mockScript.scenes);
    component.currentPacingWpm = 140;
    component.errorMessage.set(null);
    component.isLoading.set(false);
    fixture.detectChanges();

    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent).toContain('Script Studio');
    expect(el.textContent).toContain('3 Habilidades que la IA NO te Puede Quitar en 2026');
    expect(el.textContent).toContain('UnderReview');
    expect(el.textContent).toContain('140 WPM');
    expect(el.textContent).toContain('33 palabras');
    expect(el.textContent).toContain('Hook');
    expect(el.textContent).toContain('Problem');
    expect(el.textContent).toContain('El 68% de las empresas priorizan criterio analítico');
  });

  it('should render Stale Lineage Warning in ScriptStudioComponent when script is marked stale', () => {
    const fixture = TestBed.createComponent(ScriptStudioComponent);
    const component = fixture.componentInstance;

    const mockDetail: ContentItemDetailDto = {
      id: '00000000-0000-0000-0000-000000000301',
      channelId: '00000000-0000-0000-0000-000000000010',
      channelName: 'IA Simple ES',
      title: 'Stale Piece Test',
      slug: 'stale-piece-test',
      stage: 'ScriptUnderReview',
      status: 'Active',
      version: 2,
      createdAtUtc: new Date().toISOString(),
      createdByEmail: 'operator@silverman.pro',
      updatedAtUtc: new Date().toISOString(),
      updatedByEmail: 'operator@silverman.pro',
      evidences: []
    };

    const staleScript: ScriptDto = {
      id: '00000000-0000-0000-0000-000000000501',
      contentItemId: '00000000-0000-0000-0000-000000000301',
      channelId: '00000000-0000-0000-0000-000000000010',
      contentIdeaId: '00000000-0000-0000-0000-000000000401',
      contentIdeaVersionId: '00000000-0000-0000-0000-000000000401',
      truthSourceId: '00000000-0000-0000-0000-000000000302',
      truthSourceVersionId: '00000000-0000-0000-0000-000000000303',
      title: 'Stale Script Title',
      targetDurationSeconds: 45,
      pacingWpm: 140,
      estimatedDurationSeconds: 45.0,
      totalWordCount: 105,
      language: 'es-ES',
      status: 'UnderReview',
      isStale: true,
      staleReason: 'La idea seleccionada ha cambiado. Reconciliación requerida.',
      version: 1,
      createdAtUtc: new Date().toISOString(),
      createdByEmail: 'operator@silverman.pro',
      updatedAtUtc: new Date().toISOString(),
      updatedByEmail: 'operator@silverman.pro',
      scenes: []
    };

    component.contentItemId.set('00000000-0000-0000-0000-000000000301');
    component.contentItem.set(mockDetail);
    component.script.set(staleScript);
    component.errorMessage.set(null);
    component.isLoading.set(false);
    fixture.detectChanges();

    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent).toContain('Lineage Desactualizado');
    expect(el.textContent).toContain('La idea seleccionada ha cambiado. Reconciliación requerida.');
  });

  it('should render ScriptReviewPanelComponent with advisory findings and governance disclaimer', () => {
    const fixture = TestBed.createComponent(ScriptReviewPanelComponent);
    const component = fixture.componentInstance;

    const mockReview: ScriptReviewResultDto = {
      overallStatus: 'Pass',
      factualAlignmentScore: 0.95,
      retentionAnalysis: 'El gancho inicial en los primeros 3s genera curiosidad sin sensacionalismo.',
      pacingAssessment: 'Duración estimada de 44.5s a 140 WPM. Ritmo equilibrado.',
      doNotSayComplianceNotes: ['Cero infracciones detectadas.'],
      dimensions: [
        { dimension: 'Fidelidad Factual', status: 'Pass', notes: 'Todas las afirmaciones corresponden al TruthSource.' },
        { dimension: 'Ritmo y Duración', status: 'Pass', notes: 'Duración 44.5s contra objetivo 45s.' }
      ],
      sceneCritiques: [
        {
          orderIndex: 1,
          sceneType: 'Hook',
          status: 'Pass',
          claimFidelityNotes: 'Afirmación consistente con estudio de empleo.',
          retentionNotes: 'Gancho directo',
          pacingNotes: '7.7s',
          suggestions: []
        }
      ],
      actionableRecommendations: [
        'Mantener dinamismo visual entre el problema y el insight.'
      ]
    };

    component.reviewResult = mockReview;
    fixture.detectChanges();

    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent).toContain('Auditoría Editorial Consultiva (IA)');
    expect(el.textContent).toContain('Dictamen Consultivo:');
    expect(el.textContent).toContain('95%');
    expect(el.textContent).toContain('El gancho inicial en los primeros 3s genera curiosidad');
    expect(el.textContent).toContain('Fidelidad Factual');
    expect(el.textContent).toContain('Mantener dinamismo visual entre el problema y el insight');
  });

  it('should render RejectScriptModalComponent and require non-empty reason', () => {
    const fixture = TestBed.createComponent(RejectScriptModalComponent);
    const component = fixture.componentInstance;

    component.isOpen = true;
    fixture.detectChanges();

    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent).toContain('Rechazar Guión');
    expect(el.textContent).toContain('Motivo del Rechazo');

    let rejectedReason = '';
    component.rejected.subscribe(r => rejectedReason = r);

    // Empty reason should not emit
    component.reason = '   ';
    component.submit();
    expect(rejectedReason).toBe('');

    // Valid reason emits
    component.reason = 'Gancho excede los 3 segundos.';
    component.submit();
    expect(rejectedReason).toBe('Gancho excede los 3 segundos.');
  });
});

