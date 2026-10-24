import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { AdminApiService } from './admin-api.service';
import { AuthService } from '../../core/auth/auth.service';
import { AdminUsersPage } from './admin-users-page';

describe('AdminUsersPage pagination', () => {
  const getUsers = vi.fn();
  const user = { id: 1, email: 'admin@example.com', displayName: 'Admin', role: 'Admin',
    hasPassword: false, hasGoogleLogin: true, createdAtUtc: '2026-10-13T10:00:00Z' };

  beforeEach(async () => {
    getUsers.mockReset();
    getUsers.mockImplementation((pageNumber: number, pageSize: number) => of({
      items: [user], pageNumber, pageSize, totalCount: 21, totalPages: 2,
    }));
    await TestBed.configureTestingModule({
      imports: [AdminUsersPage],
      providers: [provideRouter([]),
        { provide: AdminApiService, useValue: { getUsers } },
        { provide: AuthService, useValue: { currentUser: signal(user) } },
      ],
    }).compileComponents();
  });

  it('shows the total and requests the next page from its button', async () => {
    const fixture = TestBed.createComponent(AdminUsersPage);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('21 kullanıcı');
    const next = fixture.nativeElement.querySelector('[aria-label="Kullanıcı sayfaları"] button:last-child') as HTMLButtonElement;
    next.click();
    await fixture.whenStable();
    expect(getUsers).toHaveBeenLastCalledWith(2, 20);
    expect(fixture.componentInstance.pageNumber()).toBe(2);
  });
});
