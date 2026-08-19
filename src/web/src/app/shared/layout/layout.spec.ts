import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { Component } from '@angular/core';
import { PageHeaderComponent } from './page-header.component';
import { PageToolbarComponent } from './page-toolbar.component';
import { DashboardComponent } from '../../features/dashboard/dashboard.component';
import { DiscoveryTriageComponent } from '../../features/discovery/discovery-triage.component';
import { DiscoverySourcesComponent } from '../../features/discovery/discovery-sources.component';
import { ContentListComponent } from '../../features/content/content-list.component';
import { ContentDetailComponent } from '../../features/content/content-detail.component';
import { TruthSourceReviewStudioComponent } from '../../features/content/truth-source-review-studio.component';
import { ContentIdeasComponent } from '../../features/content/content-ideas.component';
import { ScriptStudioComponent } from '../../features/content/script-studio.component';
import { EditorialTasksListComponent } from '../../features/content/editorial-tasks-list.component';
import { ChannelsComponent } from '../../features/channels/channels.component';
import { SystemComponent } from '../../features/system/system.component';
import { ShellComponent } from '../../shell/shell.component';

@Component({
  standalone: true,
  imports: [PageHeaderComponent, PageToolbarComponent],
  template: `
    <app-page-header title="Test Title" subtitle="Test Subtitle" [badge]="10" badgeSeverity="warn" backLink="/content/items">
      <div meta><span id="test-meta">Meta Tag</span></div>
      <div actions><button id="test-action">Action</button></div>
    </app-page-header>
    <app-page-toolbar>
      <div start><input id="test-search" /></div>
      <div end><button id="test-filter">Filter</button></div>
    </app-page-toolbar>
  `
})
class TestHostComponent {}

describe('Shared Layout Primitives', () => {
  let fixture: ComponentFixture<TestHostComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [TestHostComponent],
      providers: [provideRouter([])]
    }).compileComponents();

    fixture = TestBed.createComponent(TestHostComponent);
    fixture.detectChanges();
  });

  it('should render page header with title, subtitle, badge and projected slots', () => {
    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent).toContain('Test Title');
    expect(el.textContent).toContain('Test Subtitle');
    expect(el.textContent).toContain('10');
    expect(el.querySelector('#test-meta')).toBeTruthy();
    expect(el.querySelector('#test-action')).toBeTruthy();
  });

  it('should render page toolbar with start and end projected slots', () => {
    const el = fixture.nativeElement as HTMLElement;
    expect(el.querySelector('#test-search')).toBeTruthy();
    expect(el.querySelector('#test-filter')).toBeTruthy();
  });
});

describe('Operational Screen Structural Layout Compliance (ui-layout-consistency-pass)', () => {
  const routedComponents = [
    { name: 'DashboardComponent', component: DashboardComponent },
    { name: 'DiscoveryTriageComponent', component: DiscoveryTriageComponent },
    { name: 'DiscoverySourcesComponent', component: DiscoverySourcesComponent },
    { name: 'ContentListComponent', component: ContentListComponent },
    { name: 'ContentDetailComponent', component: ContentDetailComponent },
    { name: 'TruthSourceReviewStudioComponent', component: TruthSourceReviewStudioComponent },
    { name: 'ContentIdeasComponent', component: ContentIdeasComponent },
    { name: 'ScriptStudioComponent', component: ScriptStudioComponent },
    { name: 'EditorialTasksListComponent', component: EditorialTasksListComponent },
    { name: 'ChannelsComponent', component: ChannelsComponent },
    { name: 'SystemComponent', component: SystemComponent }
  ];

  it('should verify ShellComponent does not constrain width of the router outlet viewport', () => {
    const cmp = (ShellComponent as any)?.ɵcmp;
    expect(cmp).toBeDefined();
    const str = JSON.stringify(cmp?.consts || []) + (cmp?.template?.toString() || '');
    expect(str).not.toContain('max-w-7xl');
    expect(str).not.toContain('max-w-6xl');
    expect(str).not.toContain('max-w-5xl');
    expect(str).not.toContain('container mx-auto');
  });

  for (const { name, component } of routedComponents) {
    it(`should verify ${name} template has no page-level max-width or centering constraints`, () => {
      const cmp = (component as any)?.ɵcmp;
      expect(cmp).toBeDefined();
      const str = JSON.stringify(cmp?.consts || []) + (cmp?.template?.toString() || '');
      
      // Top-level / page containers must not use arbitrary desktop max-width containers
      expect(str).not.toContain('max-w-7xl mx-auto');
      expect(str).not.toContain('max-w-6xl mx-auto');
      expect(str).not.toContain('max-w-5xl mx-auto');
      expect(str).not.toContain('max-w-4xl mx-auto');
      expect(str).not.toContain('container mx-auto');
      
      // Must use standard full-width cf-page-container
      expect(str).toContain('cf-page-container');
    });
  }
});

