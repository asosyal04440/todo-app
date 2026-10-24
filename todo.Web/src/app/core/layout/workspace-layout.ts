import { Component } from '@angular/core';

@Component({
  selector: 'app-workspace-layout',
  template: `<div class="page-background">
    <div class="workspace">
      <ng-content select="[workspaceSidebar]" />
      <main class="main-content"><ng-content select="[workspaceMain]" /></main>
    </div>
  </div>`,
  styleUrl: './workspace-layout.scss',
})
export class WorkspaceLayout {}
