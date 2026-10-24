import { ApiError } from '../../core/errors/api-error';
import { ApiErrorService } from '../../core/errors/api-error.service';
import { ApiErrors } from '../../shared/errors/api-errors';
import { DatePipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzTagModule } from 'ng-zorro-antd/tag';
import { CurrentUser } from '../../core/auth/auth.models';
import { AdminApiService } from './admin-api.service';
import { AuthService } from '../../core/auth/auth.service';
import { DEFAULT_PAGE_SIZE } from '../../shared/pagination/paged-result';

@Component({
  selector: 'app-admin-users-page',
  imports: [ApiErrors, DatePipe, RouterLink, NzButtonModule, NzTagModule],
  templateUrl: './admin-users-page.html',
  styleUrl: './admin-users-page.scss',
})
export class AdminUsersPage implements OnInit {
  private readonly adminApi = inject(AdminApiService);
  protected readonly auth = inject(AuthService);

  readonly users = signal<CurrentUser[]>([]);
  readonly isLoading = signal(true);
  private readonly errors = inject(ApiErrorService);
  readonly error = signal<ApiError | null>(null);
  readonly pageNumber = signal(1);
  readonly totalPages = signal(0);
  readonly totalCount = signal(0);

  ngOnInit(): void {
    void this.loadUsers();
  }

  async changePage(pageNumber: number): Promise<void> {
    if (this.isLoading() || pageNumber < 1 || pageNumber > this.totalPages()) return;
    await this.loadUsers(pageNumber);
  }

  private async loadUsers(pageNumber = 1): Promise<void> {
    this.isLoading.set(true);
    this.error.set(null);
    try {
      const page = await firstValueFrom(this.adminApi.getUsers(pageNumber, DEFAULT_PAGE_SIZE));
      if (pageNumber > Math.max(1, page.totalPages)) {
        await this.loadUsers(Math.max(1, page.totalPages));
        return;
      }
      this.users.set(page.items);
      this.pageNumber.set(page.pageNumber);
      this.totalPages.set(page.totalPages);
      this.totalCount.set(page.totalCount);
    } catch (error: unknown) {
      const normalized = this.errors.normalize(error);
      this.error.set(
        normalized.status === 403
          ? new ApiError(403, 'Bu sayfayı görmek için Admin yetkisi gerekiyor.')
          : normalized,
      );
    } finally {
      this.isLoading.set(false);
    }
  }
}
