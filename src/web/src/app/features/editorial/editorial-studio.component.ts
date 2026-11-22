import { CommonModule } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterModule } from '@angular/router';
import {
  ApiService, ChannelDto, ContentIdeaDto, ContentItemDetailDto, EditorialFindingDto,
  EditorialUrlAnalysisDto, ScriptDto, TruthSourceDto
} from '../../core/api.service';
import { PageHeaderComponent } from '../../shared/layout/page-header.component';
import { PageToolbarComponent } from '../../shared/layout/page-toolbar.component';

type StudioState = 'empty' | 'analyzing' | 'ready' | 'unsupported' | 'insufficient' | 'error' | 'permission';

@Component({
  selector: 'app-editorial-studio',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule, PageHeaderComponent, PageToolbarComponent],
  host: { class: 'block w-full' },
  template: `
    <div class="cf-page-container space-y-3 text-xs editorial-studio">
      <app-page-header title="Editorial Studio"
        subtitle="De la fuente pública a evidencia revisable, ideas y guiones persistentes."
        [badge]="stateLabel()" badgeSeverity="info">
        <div meta class="flex items-center gap-2 flex-wrap">
          <span class="studio-chip"><i class="pi pi-shield-check"></i> Backend autoritativo</span>
          <span *ngIf="analysis()?.provider" class="studio-chip studio-chip-info">
            <i class="pi pi-sparkles"></i> {{ analysis()?.provider }}{{ analysis()?.isSimulated ? ' · Mock' : '' }}
          </span>
        </div>
        <div actions class="flex gap-2 flex-wrap">
          <a routerLink="/content/items" class="cf-btn-secondary"><i class="pi pi-folder-open"></i> Workspace</a>
        </div>
      </app-page-header>

      <app-page-toolbar>
        <div start class="flex items-center gap-2 flex-wrap">
          <span class="text-[10px] uppercase tracking-wider font-bold text-[var(--app-muted)]">Flujo independiente</span>
          <span class="text-[10px] text-[var(--app-muted)]">Fuente → evidencia → aprobación → propuesta → guión</span>
        </div>
        <div end class="text-[10px] text-[var(--app-muted)]">Los datos Mock siempre están etiquetados</div>
      </app-page-toolbar>

      <div *ngIf="errorMessage()" role="alert" class="studio-alert studio-alert-danger">
        <i class="pi pi-exclamation-triangle"></i><span>{{ errorMessage() }}</span>
        <button *ngIf="contentItemId()" (click)="loadPersisted()" class="cf-btn-secondary ml-auto">Reintentar</button>
      </div>

      <div class="studio-grid">
        <section class="cf-card studio-panel studio-source" aria-labelledby="source-heading">
          <div class="studio-panel-heading"><div><p class="studio-kicker">01 · Fuente</p><h2 id="source-heading">Analizar origen</h2></div><span class="studio-status" [class.studio-status-active]="state() === 'analyzing'">{{ stateLabel() }}</span></div>
          <form (ngSubmit)="analyze()" class="space-y-3">
            <label class="studio-label" for="channel">Canal</label>
            <select id="channel" name="channel" [(ngModel)]="channelId" class="studio-input" [disabled]="state() === 'analyzing'">
              <option value="">Selecciona un canal</option><option *ngFor="let channel of channels()" [value]="channel.id">{{ channel.name }} · {{ channel.language }}</option>
            </select>
            <label class="studio-label" for="source-url">URL pública</label>
            <input id="source-url" name="url" [(ngModel)]="url" class="studio-input" placeholder="https://…" type="url" required [disabled]="state() === 'analyzing'" />
            <label class="studio-label" for="angle">Ángulo editorial <span class="font-normal text-[var(--app-muted)]">(opcional)</span></label>
            <input id="angle" name="angle" [(ngModel)]="angle" class="studio-input" placeholder="Qué debe aprender la audiencia" [disabled]="state() === 'analyzing'" />
            <button class="cf-btn-primary w-full justify-center" type="submit" [disabled]="!channelId || !url.trim() || state() === 'analyzing'">
              <i class="pi" [ngClass]="state() === 'analyzing' ? 'pi-spin pi-spinner' : 'pi-search'"></i>{{ state() === 'analyzing' ? 'Analizando…' : 'Analizar fuente' }}
            </button>
          </form>
          <div *ngIf="state() === 'empty'" class="studio-empty"><i class="pi pi-compass"></i><p>Introduce una URL para comenzar un estudio trazable.</p></div>
          <div *ngIf="analysis()" class="studio-source-summary"><span class="font-semibold">{{ analysis()?.title || analysis()?.normalizedUrl }}</span><span>{{ analysis()?.textOrigin || 'Origen devuelto por el backend' }}</span><a *ngIf="analysis()?.locator || analysis()?.normalizedUrl" [href]="analysis()?.locator || analysis()?.normalizedUrl" target="_blank" rel="noreferrer">Ver fuente <i class="pi pi-external-link"></i></a></div>
        </section>

        <section class="cf-card studio-panel studio-findings" aria-labelledby="findings-heading">
          <div class="studio-panel-heading"><div><p class="studio-kicker">02 · Evidencia</p><h2 id="findings-heading">Hallazgos revisables</h2></div><span class="studio-count">{{ findings().length }} F-ID</span></div>
          <div *ngIf="state() === 'analyzing'" class="studio-loading"><i class="pi pi-spin pi-spinner"></i> Esperando análisis del backend…</div>
          <div *ngIf="state() === 'unsupported' || state() === 'insufficient'" class="studio-empty studio-empty-warning"><i class="pi pi-info-circle"></i><p>{{ analysis()?.errorMessage || 'La fuente no tiene suficiente evidencia utilizable.' }}</p></div>
          <div *ngIf="findings().length === 0 && state() !== 'analyzing' && state() !== 'unsupported' && state() !== 'insufficient'" class="studio-empty"><i class="pi pi-file-check"></i><p>Los hallazgos aparecen únicamente cuando el análisis devuelve evidencia persistida.</p></div>
          <div *ngIf="findings().length" class="studio-scroll-list">
            <article *ngFor="let finding of findings(); let index = index" class="finding-card">
              <button class="finding-summary" (click)="toggleFinding(finding.id)" [attr.aria-expanded]="expandedFindingId() === finding.id">
                <span class="finding-id">F-{{ (index + 1).toString().padStart(2, '0') }}</span><span class="flex-1 text-left"><strong>{{ finding.title }}</strong><small>{{ finding.editorialValue }}</small></span><i class="pi" [ngClass]="expandedFindingId() === finding.id ? 'pi-chevron-up' : 'pi-chevron-down'"></i>
              </button>
              <div *ngIf="expandedFindingId() === finding.id" class="finding-evidence">
                <div *ngIf="isVerifiedVerbatim(finding)" class="evidence-region"><span class="evidence-label">Verbatim verificado</span><p>“{{ finding.originalText }}”</p><span *ngIf="finding.speaker || finding.timestamp" class="text-[10px] text-[var(--app-muted)]">{{ finding.speaker || '' }}{{ finding.timestamp ? ' · ' + finding.timestamp : '' }}</span></div>
                <div *ngIf="finding.spanishTranslation" class="evidence-region"><span class="evidence-label">Traducción al español</span><p>{{ finding.spanishTranslation }}</p></div>
                <div *ngIf="finding.contentType === 'Paraphrase' || !isVerifiedVerbatim(finding)" class="evidence-region"><span class="evidence-label">Paráfrasis editorial</span><p>{{ finding.editorialValue }}</p></div>
                <div class="flex items-center justify-between gap-2 pt-2 border-t border-[var(--app-card-border)]"><span class="text-[10px] text-[var(--app-muted)]">{{ finding.reviewStatus }} · {{ finding.evidenceId.substring(0, 8) }}</span><a *ngIf="finding.locator" [href]="finding.locator" target="_blank" rel="noreferrer" class="text-blue-600 dark:text-blue-400 font-semibold">Ver localizador <i class="pi pi-external-link"></i></a></div>
              </div>
            </article>
          </div>
        </section>

        <section class="cf-card studio-panel studio-output" aria-labelledby="output-heading">
          <div class="studio-panel-heading"><div><p class="studio-kicker">03 · Producción editorial</p><h2 id="output-heading">TruthSource, ideas y guión</h2></div><span class="studio-status" [class.studio-status-approved]="truthSource()?.status === 'Approved'">{{ truthSource()?.status || 'Pendiente' }}</span></div>
          <div *ngIf="truthSource(); else noTruthSource" class="space-y-3">
            <div class="studio-summary"><span class="studio-label">TruthSource v{{ truthSource()?.version }}</span><p>{{ truthSource()?.summary }}</p><div class="flex flex-wrap gap-1.5"><span *ngFor="let idea of truthSource()?.possibleAngles" class="studio-tag">{{ idea }}</span></div></div>
            <div class="flex gap-2 flex-wrap"><button *ngIf="truthSource()?.status !== 'Approved'" (click)="approveTruthSource()" [disabled]="isBusy()" class="cf-btn-primary"><i class="pi pi-check"></i> Aprobar TruthSource</button><span *ngIf="truthSource()?.status !== 'Approved'" class="text-[10px] text-amber-600 dark:text-amber-400 self-center">Aprobar antes de generar ideas.</span><button *ngIf="truthSource()?.status === 'Approved'" (click)="generateIdeas()" [disabled]="isBusy()" class="cf-btn-primary"><i class="pi pi-sparkles"></i> Generar ideas</button></div>
          </div>
          <ng-template #noTruthSource><div class="studio-empty"><i class="pi pi-lock"></i><p>La generación está bloqueada hasta que el análisis devuelva un TruthSource revisable.</p></div></ng-template>
          <div *ngIf="truthSource()?.status === 'Approved' && ideas().length" class="studio-output-section"><div class="flex items-center justify-between"><span class="studio-label">Propuestas devueltas</span><label class="flex items-center gap-2 text-[10px]">Count <input type="number" min="1" max="10" [(ngModel)]="ideaCount" class="studio-count-input" /></label></div><div class="proposal-list"><article *ngFor="let idea of ideas()" class="proposal-card" [class.proposal-selected]="idea.status === 'Selected'"><div><span class="studio-tag">{{ idea.status }}</span><h3>{{ idea.title }}</h3><p>{{ idea.angle }}</p></div><div class="flex flex-wrap justify-end gap-2"><button (click)="selectIdea(idea)" [disabled]="isBusy() || idea.status === 'Selected'" class="cf-btn-secondary"><i class="pi pi-video"></i>{{ idea.status === 'Selected' ? 'Seleccionada' : 'Crear este video' }}</button><button *ngIf="idea.status === 'Selected' && !script()" (click)="generateScript()" [disabled]="isBusy()" class="cf-btn-primary"><i class="pi" [ngClass]="isBusy() ? 'pi-spin pi-spinner' : 'pi-file-edit'"></i>{{ isBusy() ? 'Generando…' : 'Generar guion' }}</button></div></article></div></div>
          <div *ngIf="script()" class="script-summary"><div class="flex items-center justify-between gap-2"><span class="studio-label">Guión · {{ script()?.status }}</span><a [routerLink]="['/content/items', contentItemId(), 'script']" class="cf-btn-secondary">Abrir Script Studio</a></div><h3>{{ script()?.title }}</h3><p>{{ script()?.totalWordCount }} palabras · ~{{ script()?.estimatedDurationSeconds }}s · v{{ script()?.version }}</p><div *ngIf="script()?.isStale" class="studio-alert studio-alert-warning">Lineage desactualizado: {{ script()?.staleReason }}</div></div>
          <div class="production-plan"><div class="production-preview"><span>9:16</span><small>Preview conceptual</small><i class="pi pi-play-circle"></i></div><div class="space-y-2"><span class="studio-label">Plan de producción</span><p class="text-[11px] text-[var(--app-muted)]">La pantalla planifica el video; no afirma render, descarga ni publicación de un MP4.</p><div class="flex flex-wrap gap-2"><a *ngIf="contentItemId()" [routerLink]="['/content/items', contentItemId(), 'storyboard']" class="cf-btn-secondary"><i class="pi pi-images"></i> Storyboard · Visual · Voice</a></div></div></div>
        </section>
      </div>
    </div>
  `,
  styles: [`
    :host { color: var(--app-text); }
    .studio-grid { display:grid; grid-template-columns:minmax(240px, .8fr) minmax(300px, 1.15fr) minmax(300px, 1fr); gap:12px; align-items:stretch; }
    .studio-panel { min-height:540px; padding:14px; display:flex; flex-direction:column; gap:14px; }
    .studio-panel-heading { display:flex; justify-content:space-between; gap:8px; align-items:flex-start; border-bottom:1px solid var(--app-card-border); padding-bottom:10px; }
    .studio-panel-heading h2 { margin:2px 0 0; font-size:14px; font-weight:800; }
    .studio-kicker,.studio-label,.evidence-label { font-size:10px; font-weight:800; letter-spacing:.08em; text-transform:uppercase; color:var(--app-muted); }
    .studio-input { width:100%; min-height:34px; border:1px solid var(--app-card-border); border-radius:8px; padding:7px 9px; background:var(--app-bg); color:var(--app-text); outline:none; }
    .studio-input:focus { border-color:#3b82f6; box-shadow:0 0 0 2px #3b82f633; }
    .studio-chip,.studio-status,.studio-count,.studio-tag { display:inline-flex; align-items:center; gap:5px; border:1px solid var(--app-card-border); border-radius:999px; padding:3px 8px; font-size:10px; font-weight:700; color:var(--app-muted); }
    .studio-chip-info { color:#2563eb; border-color:#3b82f655; background:#3b82f611; }
    .studio-status-active { color:#2563eb; background:#3b82f611; }.studio-status-approved { color:#059669; background:#10b98112; }
    .studio-alert { display:flex; align-items:center; gap:8px; padding:9px 11px; border-radius:9px; border:1px solid; }.studio-alert-danger { color:#b91c1c; border-color:#ef444455; background:#ef444411; }.studio-alert-warning { color:#a16207; border-color:#f59e0b55; background:#f59e0b11; }
    .studio-empty,.studio-loading { flex:1; min-height:130px; display:flex; flex-direction:column; align-items:center; justify-content:center; gap:8px; text-align:center; color:var(--app-muted); padding:16px; }.studio-empty i,.studio-loading i { font-size:24px; color:#3b82f6; }.studio-empty-warning i { color:#d97706; }
    .studio-source-summary,.studio-summary,.script-summary,.production-plan { border:1px solid var(--app-card-border); border-radius:9px; padding:10px; display:grid; gap:6px; background:var(--app-bg); }.studio-source-summary span:nth-child(2),.studio-source-summary a { font-size:10px; color:var(--app-muted); }.studio-source-summary a { color:#2563eb; }
    .studio-scroll-list { min-height:0; overflow:auto; max-height:440px; display:grid; gap:8px; padding-right:2px; }.finding-card,.proposal-card { border:1px solid var(--app-card-border); border-radius:9px; overflow:hidden; background:var(--app-bg); }.finding-summary { width:100%; border:0; color:var(--app-text); background:transparent; padding:10px; display:flex; align-items:center; gap:9px; cursor:pointer; }.finding-summary strong,.finding-summary small { display:block; }.finding-summary small { color:var(--app-muted); margin-top:3px; }.finding-id { color:#2563eb; font:700 10px ui-monospace; }.finding-evidence { padding:0 10px 10px; display:grid; gap:8px; }.evidence-region { border-left:2px solid #3b82f6; padding-left:8px; }.evidence-region p { margin:3px 0 0; line-height:1.45; }.evidence-label { color:#2563eb; }.studio-output-section { display:grid; gap:8px; }.studio-count-input { width:52px; border:1px solid var(--app-card-border); background:var(--app-bg); color:var(--app-text); border-radius:6px; padding:3px 5px; }.proposal-list { display:grid; gap:8px; max-height:250px; overflow:auto; }.proposal-card { display:flex; align-items:flex-start; justify-content:space-between; gap:10px; padding:10px; }.proposal-card h3 { margin:6px 0 3px; font-weight:800; }.proposal-card p { color:var(--app-muted); margin:0; line-height:1.4; }.proposal-selected { border-color:#10b98188; }.studio-tag { border-radius:5px; padding:2px 6px; color:#2563eb; background:#3b82f611; }.production-plan { grid-template-columns:96px 1fr; margin-top:auto; }.production-preview { min-height:150px; border-radius:8px; background:linear-gradient(160deg,#172554,#0f172a); color:white; display:flex; flex-direction:column; align-items:center; justify-content:center; gap:5px; }.production-preview span { font-size:22px; font-weight:900; }.production-preview small { color:#bfdbfe; }.production-preview i { font-size:20px; margin-top:10px; }
    @media (max-width: 1100px) { .studio-grid { grid-template-columns:1fr 1fr; }.studio-output { grid-column:1 / -1; min-height:unset; }.studio-panel { min-height:420px; } }
    @media (max-width: 700px) { .studio-grid { display:flex; flex-direction:column; }.studio-panel { min-height:unset; }.studio-findings,.studio-output { max-height:none; }.production-plan { grid-template-columns:84px 1fr; }.studio-panel-heading { align-items:center; } }
  `]
})
export class EditorialStudioComponent implements OnInit {
  private readonly api = inject(ApiService);
  private readonly route = inject(ActivatedRoute);
  readonly channels = signal<ChannelDto[]>([]);
  readonly state = signal<StudioState>('empty');
  readonly analysis = signal<EditorialUrlAnalysisDto | null>(null);
  readonly contentItem = signal<ContentItemDetailDto | null>(null);
  readonly truthSource = signal<TruthSourceDto | null>(null);
  readonly ideas = signal<ContentIdeaDto[]>([]);
  readonly script = signal<ScriptDto | null>(null);
  readonly expandedFindingId = signal<string | null>(null);
  readonly errorMessage = signal<string | null>(null);
  readonly isBusy = signal(false);
  channelId = '';
  url = '';
  angle = '';
  ideaCount = 3;
  readonly contentItemId = computed(() => this.analysis()?.contentItemId || this.contentItem()?.id || this.route.snapshot.paramMap.get('contentItemId'));
  readonly findings = computed(() => this.analysis()?.findings ?? []);
  readonly stateLabel = computed(() => ({ empty:'Listo', analyzing:'Analizando', ready:'Evidencia disponible', unsupported:'Fuente no utilizable', insufficient:'Hallazgos insuficientes', error:'Error recuperable', permission:'Sin permiso' }[this.state()]));

  ngOnInit(): void {
    this.api.getChannels().subscribe({ next: channels => { this.channels.set(channels); if (channels[0]) this.channelId = channels[0].id; }, error: () => this.errorMessage.set('No fue posible cargar los canales autorizados.') });
    const id = this.route.snapshot.paramMap.get('contentItemId');
    if (id) this.loadPersisted();
  }

  analyze(): void {
    this.errorMessage.set(null); this.state.set('analyzing');
    this.api.analyzeEditorialUrl({ channelId: this.channelId, url: this.url.trim(), angle: this.angle.trim() || null }).subscribe({
      next: result => { this.analysis.set(result); this.truthSource.set(result.draft ?? null); this.state.set(result.status === 'Completed' ? (result.findings.length ? 'ready' : 'insufficient') : result.status === 'NotAnalyzable' ? 'unsupported' : 'error'); this.loadRelated(result.contentItemId); },
      error: error => { this.state.set(error.status === 401 || error.status === 403 ? 'permission' : 'error'); this.errorMessage.set(error.error?.message || 'El backend no pudo completar el análisis.'); }
    });
  }

  loadPersisted(): void { const id = this.route.snapshot.paramMap.get('contentItemId'); if (!id) return; this.loadRelated(id); this.api.getEditorialUrlAnalysis(id).subscribe({ next: a => { this.analysis.set(a); this.truthSource.set(a.draft ?? null); this.state.set(a.findings.length ? 'ready' : 'insufficient'); }, error: () => this.errorMessage.set('No se pudo reabrir el análisis persistido.') }); }
  private loadRelated(id: string): void { this.api.getContentItemDetail(id).subscribe({ next: item => { this.contentItem.set(item); this.truthSource.set(this.truthSource() ?? item.truthSource ?? null); }, error: () => {} }); this.api.getContentIdeas(id).subscribe({ next: ideas => this.ideas.set(ideas), error: () => {} }); this.api.getScript(id).subscribe({ next: script => this.script.set(script), error: () => {} }); }
  toggleFinding(id: string): void { this.expandedFindingId.set(this.expandedFindingId() === id ? null : id); }
  isVerifiedVerbatim(finding: EditorialFindingDto): boolean { return finding.contentType.toLowerCase() === 'verbatim' && finding.reviewStatus.toLowerCase() === 'verified' && !!finding.originalText; }
  approveTruthSource(): void { const id = this.contentItemId(); if (!id) return; this.isBusy.set(true); this.api.approveTruthSource(id).subscribe({ next: truth => { this.truthSource.set(truth); this.isBusy.set(false); }, error: error => { this.errorMessage.set(error.error?.message || 'La aprobación fue rechazada por el backend.'); this.isBusy.set(false); } }); }
  generateIdeas(): void { const id = this.contentItemId(); if (!id || this.truthSource()?.status !== 'Approved') return; this.isBusy.set(true); this.api.generateAiIdeas(id, { count: Math.max(1, Math.min(10, this.ideaCount)) }).subscribe({ next: ideas => { this.ideas.set(ideas); this.isBusy.set(false); }, error: error => { this.errorMessage.set(error.error?.message || 'No fue posible generar propuestas.'); this.isBusy.set(false); } }); }
  selectIdea(idea: ContentIdeaDto): void { const id = this.contentItemId(); if (!id) return; this.isBusy.set(true); this.api.selectIdea(id, idea.id, { expectedVersion: idea.version }).subscribe({ next: selected => { this.ideas.update(items => items.map(item => item.id === selected.id ? selected : item.status === 'Selected' ? { ...item, status: 'Proposed' } : item)); this.loadRelated(id); this.isBusy.set(false); }, error: error => { this.handleLifecycleError(error, 'No fue posible seleccionar la propuesta.'); } }); }
  generateScript(): void { const id = this.contentItemId(); const selected = this.ideas().find(idea => idea.status === 'Selected'); if (!id || !selected || this.script()) return; this.errorMessage.set(null); this.isBusy.set(true); this.api.generateAiScript(id).subscribe({ next: script => { this.script.set(script); this.isBusy.set(false); }, error: error => { this.handleLifecycleError(error, 'No fue posible generar el guion.'); } }); }
  private handleLifecycleError(error: { status?: number; error?: { message?: string; code?: string } }, fallback: string): void { const detail = error.error?.message || fallback; this.errorMessage.set(error.status === 409 ? `${detail} Vuelve a cargar el estado persistido antes de reintentar.` : detail); this.isBusy.set(false); }
}
