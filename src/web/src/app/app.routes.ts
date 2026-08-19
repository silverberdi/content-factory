import { Routes } from '@angular/router';
import { ShellComponent } from './shell/shell.component';
import { DashboardComponent } from './features/dashboard/dashboard.component';
import { ChannelsComponent } from './features/channels/channels.component';
import { SystemComponent } from './features/system/system.component';
import { DiscoveryTriageComponent } from './features/discovery/discovery-triage.component';
import { DiscoverySourcesComponent } from './features/discovery/discovery-sources.component';
import { ContentListComponent } from './features/content/content-list.component';
import { ContentDetailComponent } from './features/content/content-detail.component';
import { TruthSourceReviewStudioComponent } from './features/content/truth-source-review-studio.component';
import { ContentIdeasComponent } from './features/content/content-ideas.component';
import { ScriptStudioComponent } from './features/content/script-studio.component';
import { StoryboardStudioComponent } from './features/content/storyboard-studio.component';
import { EditorialTasksListComponent } from './features/content/editorial-tasks-list.component';

export const routes: Routes = [
  {
    path: '',
    component: ShellComponent,
    children: [
      { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
      { path: 'dashboard', component: DashboardComponent },
      { path: 'discovery', redirectTo: 'discovery/triage', pathMatch: 'full' },
      { path: 'discovery/triage', component: DiscoveryTriageComponent },
      { path: 'discovery/sources', component: DiscoverySourcesComponent },
      { path: 'content', redirectTo: 'content/items', pathMatch: 'full' },
      { path: 'content/items', component: ContentListComponent },
      { path: 'content/items/:id', component: ContentDetailComponent },
      { path: 'content/items/:id/truth-source', component: TruthSourceReviewStudioComponent },
      { path: 'content/items/:id/ideas', component: ContentIdeasComponent },
      { path: 'content/items/:id/script', component: ScriptStudioComponent },
      { path: 'content/items/:id/storyboard', component: StoryboardStudioComponent },
      { path: 'editorial/tasks', component: EditorialTasksListComponent },
      { path: 'channels', component: ChannelsComponent },
      { path: 'system', component: SystemComponent }
    ]
  },
  { path: '**', redirectTo: '' }
];
