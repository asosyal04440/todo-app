import { TestBed } from '@angular/core/testing';
import {
  ActivatedRouteSnapshot,
  CanActivateFn,
  Router,
  RouterStateSnapshot,
  UrlTree,
  provideRouter,
} from '@angular/router';
import { AuthService } from './auth.service';
import { adminGuard, authGuard, guestGuard } from './auth.guards';

describe('auth guards', () => {
  let isAuthenticated: boolean;
  let isAdmin: boolean;

  beforeEach(() => {
    isAuthenticated = false;
    isAdmin = false;

    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        {
          provide: AuthService,
          useValue: {
            isAuthenticated: () => isAuthenticated,
            isAdmin: () => isAdmin,
          },
        },
      ],
    });
  });

  function runGuard(guard: CanActivateFn): boolean | string {
    const result = TestBed.runInInjectionContext(() =>
      guard({} as ActivatedRouteSnapshot, {} as RouterStateSnapshot),
    );

    if (result instanceof UrlTree) {
      return TestBed.inject(Router).serializeUrl(result);
    }

    return result as boolean;
  }

  it('sends a signed-out visitor to the login page', () => {
    expect(runGuard(authGuard)).toBe('/login');
  });

  it('lets a signed-in user open the todo page', () => {
    isAuthenticated = true;

    expect(runGuard(authGuard)).toBe(true);
  });

  it('sends a signed-in user away from the login page', () => {
    isAuthenticated = true;

    expect(runGuard(guestGuard)).toBe('/');
  });

  it('keeps a normal user out of the admin page', () => {
    isAuthenticated = true;

    expect(runGuard(adminGuard)).toBe('/');
  });

  it('lets an admin open the admin page', () => {
    isAuthenticated = true;
    isAdmin = true;

    expect(runGuard(adminGuard)).toBe(true);
  });
});
