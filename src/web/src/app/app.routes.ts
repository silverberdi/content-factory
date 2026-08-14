import { Routes } from '@angular/router';
import { ShellComponent } from './shell/shell.component';
import { DashboardComponent } from './features/dashboard/dashboard.component';
import { ChannelsComponent } from './features/channels/channels.component';
import { SystemComponent } from './features/system/system.component';
import { DiscoveryTriageComponent } from './features/discovery/discovery-triage.component';
import { DiscoverySourcesComponent } from './features/discovery/discovery-sources.component';

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
      { path: 'channels', component: ChannelsComponent },
      { path: 'system', component: SystemComponent }
    ]
  },
  { path: '**', redirectTo: '' }
];
