import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { EditorialStudioComponent } from './editorial-studio.component';
import { ApiService } from '../../core/api.service';

describe('EditorialStudioComponent', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [EditorialStudioComponent],
      providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()]
    }).compileComponents();
  });

  it('renders an empty, honest workspace and uses the shared page primitives', () => {
    const fixture = TestBed.createComponent(EditorialStudioComponent);
    const http = TestBed.inject(HttpTestingController);
    fixture.detectChanges();
    http.expectOne(req => req.url.includes('/channels')).flush([]);
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('Editorial Studio');
    expect(fixture.nativeElement.textContent).toContain('Introduce una URL');
    expect(fixture.nativeElement.querySelector('app-page-header')).toBeTruthy();
    http.verify();
  });

  it('only presents original text as verbatim when the backend verifies it', () => {
    const component = TestBed.createComponent(EditorialStudioComponent).componentInstance;
    expect(component.isVerifiedVerbatim({
      id: '1', title: 'Finding', editorialValue: 'Paráfrasis', evidenceId: 'e',
      contentType: 'Verbatim', originalText: 'Texto', spanishTranslation: null,
      speaker: null, timestamp: null, reviewStatus: 'Verified'
    })).toBe(true);
    expect(component.isVerifiedVerbatim({
      id: '2', title: 'Finding', editorialValue: 'Paráfrasis', evidenceId: 'e',
      contentType: 'Verbatim', originalText: 'Texto', spanishTranslation: null,
      speaker: 'Unknown', timestamp: '00:01', reviewStatus: 'NeedsReview'
    })).toBe(false);
  });

  it('keeps idea generation behind the approved TruthSource state', () => {
    const fixture = TestBed.createComponent(EditorialStudioComponent);
    const component = fixture.componentInstance;
    expect(component.truthSource()).toBeNull();
    expect(component.ideas()).toEqual([]);
  });
});
