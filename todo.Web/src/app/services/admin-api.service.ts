import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { resolveApiUrl } from '../core/api-url';
import { CurrentUser } from '../models/auth.models';

@Injectable({
  providedIn: 'root',
})
export class AdminApiService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = resolveApiUrl();

  getUsers(): Observable<CurrentUser[]> {
    return this.http.get<CurrentUser[]>(`${this.apiUrl}/admin/users`);
  }
}
