import { Component, EventEmitter, Input, Output } from '@angular/core';
import { RouterLink } from '@angular/router';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { CurrentUser } from '../auth/auth.models';

@Component({
  selector: 'app-workspace-header',
  imports: [RouterLink, NzButtonModule],
  templateUrl: './workspace-header.html',
  styleUrl: './workspace-header.scss',
})
export class WorkspaceHeader {
  @Input() user: CurrentUser | null = null;
  @Input() isAdmin = false;
  @Input() showCreate = false;
  @Input() showRefresh = false;
  @Input() refreshing = false;
  @Input() refreshDisabled = false;
  @Output() readonly createRequested = new EventEmitter<void>();
  @Output() readonly refreshRequested = new EventEmitter<void>();
  @Output() readonly logoutRequested = new EventEmitter<void>();
}
