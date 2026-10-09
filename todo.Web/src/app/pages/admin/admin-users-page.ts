import { DatePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzTagModule } from 'ng-zorro-antd/tag';
import { CurrentUser } from '../../models/auth.models';
import { AdminApiService } from '../../services/admin-api.service';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-admin-users-page',
  imports: [DatePipe, RouterLink, NzButtonModule, NzTagModule],
  templateUrl: './admin-users-page.html',
  styleUrl: './admin-users-page.scss',
})
export class AdminUsersPage implements OnInit {
  private readonly adminApi = inject(AdminApiService);
  protected readonly auth = inject(AuthService);

  readonly users = signal<CurrentUser[]>([]);
  readonly isLoading = signal(true);
  readonly errorMessage = signal<string | null>(null);

  ngOnInit(): void {
    void this.loadUsers();
  }

  private async loadUsers(): Promise<void> {
    try {
      this.users.set(await firstValueFrom(this.adminApi.getUsers()));
    } catch (error: unknown) {
      if (error instanceof HttpErrorResponse && error.status === 403) {
        this.errorMessage.set('Bu sayfayı görmek için Admin yetkisi gerekiyor.');
      } else {
        this.errorMessage.set('Kullanıcılar yüklenemedi.');
      }
    } finally {
      this.isLoading.set(false);
    }
  }
}
