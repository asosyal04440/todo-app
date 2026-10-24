import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { API_URL } from '../../core/config/api-url.token';
import { CurrentUser } from '../../core/auth/auth.models';
import { DEFAULT_PAGE_SIZE, PagedResult } from '../../shared/pagination/paged-result';

@Injectable({
  providedIn: 'root',
})
export class AdminApiService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = inject(API_URL);

  getUsers(pageNumber = 1, pageSize = DEFAULT_PAGE_SIZE): Observable<PagedResult<CurrentUser>> {
    return this.http.get<PagedResult<CurrentUser>>(`${this.apiUrl}/admin/users`, {
      params: { pageNumber, pageSize },
    });
  }
}
