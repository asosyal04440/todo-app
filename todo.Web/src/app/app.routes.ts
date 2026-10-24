import { Routes } from '@angular/router';
import { adminGuard, authGuard, guestGuard } from './core/auth/auth.guards';

export const routes: Routes = [
  {
    path: 'login',
    loadChildren: () => import('./features/auth/auth.routes').then((module) => module.AUTH_ROUTES),
    canActivate: [guestGuard],
    title: 'Giriş | Todo',
  },
  {
    path: 'admin/users',
    loadChildren: () =>
      import('./features/admin/admin.routes').then((module) => module.ADMIN_ROUTES),
    canActivate: [adminGuard],
    title: 'Kullanıcılar | Todo',
  },
  {
    path: '',
    loadChildren: () =>
      import('./features/todos/todos.routes').then((module) => module.TODO_ROUTES),
    canActivate: [authGuard],
    pathMatch: 'full',
    title: 'Todo | Gününü düzenle',
  },
  { path: '**', redirectTo: '' },
];
