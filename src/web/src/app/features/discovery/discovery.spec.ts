import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { DiscoveryTriageComponent } from './discovery-triage.component';
import { DiscoverySourcesComponent } from './discovery-sources.component';
import { CandidatePreviewDrawerComponent } from './candidate-preview-drawer.component';
import { QuickSubmitModalComponent } from './quick-submit-modal.component';
import { ApiService, DiscoveryCandidateDto } from '../../core/api.service';

describe('Discovery Feature Components', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [
        DiscoveryTriageComponent,
        DiscoverySourcesComponent,
        CandidatePreviewDrawerComponent,
        QuickSubmitModalComponent
      ],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        ApiService
      ]
    }).compileComponents();
  });

  it('should create DiscoveryTriageComponent', () => {
    const fixture = TestBed.createComponent(DiscoveryTriageComponent);
    const component = fixture.componentInstance;
    expect(component).toBeTruthy();
    expect(component.selectedStatus).toBe('PendingReview');
  });

  it('should create DiscoverySourcesComponent', () => {
    const fixture = TestBed.createComponent(DiscoverySourcesComponent);
    const component = fixture.componentInstance;
    expect(component).toBeTruthy();
  });

  it('should render CandidatePreviewDrawerComponent when open and emit triage', () => {
    const fixture = TestBed.createComponent(CandidatePreviewDrawerComponent);
    const component = fixture.componentInstance;

    const mockCandidate: DiscoveryCandidateDto = {
      id: '00000000-0000-0000-0000-000000000201',
      channelId: '00000000-0000-0000-0000-000000000010',
      channelName: 'IA Simple ES',
      discoverySourceId: '00000000-0000-0000-0000-000000000101',
      sourceName: 'Xataka IA',
      externalUrl: 'https://www.xataka.com/ia/test-article',
      normalizedUrl: 'https://www.xataka.com/ia/test-article',
      title: 'Modelos de razonamiento en empresas',
      summary: 'Resumen del artículo sobre modelos de razonamiento.',
      rawContent: 'Contenido completo...',
      language: 'es',
      author: 'Redacción',
      discoveredAtUtc: new Date().toISOString(),
      status: 'PendingReview',
      originType: 'Automated',
      submitterEmail: null,
      dismissalReason: null,
      editorialNotes: null,
      promotedAtUtc: null,
      promotedByEmail: null,
      createdAtUtc: new Date().toISOString()
    };

    component.isOpen = true;
    component.candidate = mockCandidate;
    fixture.detectChanges();

    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent).toContain('Modelos de razonamiento en empresas');
    expect(el.textContent).toContain('IA Simple ES');
    expect(el.textContent).toContain('Xataka IA');

    let emittedTriage: any = null;
    component.onTriage.subscribe((t) => (emittedTriage = t));

    component.confirmPromote();
    expect(emittedTriage).toEqual({
      id: mockCandidate.id,
      status: 'Promoted',
      notes: undefined
    });
  });

  it('should initialize QuickSubmitModalComponent with channels and allow closing', () => {
    const fixture = TestBed.createComponent(QuickSubmitModalComponent);
    const component = fixture.componentInstance;

    component.isOpen = true;
    component.channels = [
      {
        id: '00000000-0000-0000-0000-000000000010',
        name: 'IA Simple ES',
        slug: 'ia-simple-es',
        language: 'es',
        niche: 'AI',
        status: 'pilot',
        createdAtUtc: new Date().toISOString(),
        updatedAtUtc: new Date().toISOString()
      }
    ];
    component.defaultChannelId = '00000000-0000-0000-0000-000000000010';
    component.ngOnChanges();
    fixture.detectChanges();

    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent).toContain('Quick Submit');
    expect(el.textContent).toContain('Add a URL or note for discovery');

    let closed = false;
    component.onClose.subscribe(() => (closed = true));
    component.close();
    expect(closed).toBe(true);
  });
});
