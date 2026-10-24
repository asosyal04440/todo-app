import { Routes } from '@angular/router';
import { Type, reflectComponentType } from '@angular/core';
import { routes } from './app.routes';
import { adminGuard, authGuard, guestGuard } from './core/auth/auth.guards';

describe('Lazy feature routes', () => {
  it.each([
    ['login', 'app-login-page', guestGuard],
    ['admin/users', 'app-admin-users-page', adminGuard],
    ['', 'app-todo-page', authGuard],
  ])('loads %s lazily while keeping its guard', async (path, componentName, guard) => {
    const route = routes.find((route) => route.path === path)!;
    expect(route.component).toBeUndefined();
    expect(route.canActivate).toContain(guard);
    const children = (await route.loadChildren!()) as Routes;
    expect(children[0].path).toBe('');
    expect(children[0].component).toBeUndefined();
    const component = await children[0].loadComponent!();
    expect(reflectComponentType(component as Type<unknown>)?.selector).toBe(componentName);
  });
});
