import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { App } from './app';
import { ThemeService } from './core/theme.service';
import { FactoryHealthWidgetComponent } from './features/dashboard/factory-health-widget.component';

describe('App & Core Components', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [App, FactoryHealthWidgetComponent],
      providers: [
        provideRouter([]),
        provideHttpClient(),
        provideHttpClientTesting(),
        ThemeService
      ]
    }).compileComponents();
  });

  it('should create the main app root', () => {
    const fixture = TestBed.createComponent(App);
    const app = fixture.componentInstance;
    expect(app).toBeTruthy();
  });

  it('should toggle theme correctly in ThemeService', () => {
    const themeService = TestBed.inject(ThemeService);
    themeService.setTheme('light');
    expect(themeService.currentTheme()).toBe('light');

    themeService.toggleTheme();
    expect(themeService.currentTheme()).toBe('dark');

    themeService.toggleTheme();
    expect(themeService.currentTheme()).toBe('light');
  });

  it('should render factory health metrics in FactoryHealthWidget', () => {
    const fixture = TestBed.createComponent(FactoryHealthWidgetComponent);
    fixture.componentInstance.health = {
      status: 'healthy',
      activeChannelsCount: 3,
      pilotChannelsCount: 1,
      totalChannelsCount: 4,
      databaseStatus: 'Connected (MySQL/content_factory_dev)',
      backupStatus: 'Not Configured (CF-001 Scope)',
      environment: 'Development'
    };
    fixture.detectChanges();

    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent).toContain('Factory Health');
    expect(el.textContent).toContain('healthy');
    expect(el.textContent).toContain('Total Channels');
    expect(el.textContent).toContain('MySQL/content_factory_dev');
  });
});
