import { Routes } from '@angular/router';
import { adminGuard, authGuard, guestGuard } from './guards/auth.guards';
import { AdminUsersPage } from './pages/admin/admin-users-page';
import { LoginPage } from './pages/login/login-page';
import { TodoPage } from './pages/todos/todo-page';

export const routes: Routes = [
  {
    path: 'login',
    component: LoginPage,
    canActivate: [guestGuard],
    title: 'Giriş | Todo',
  },
  {
    path: 'admin/users',
    component: AdminUsersPage,
    canActivate: [adminGuard],
    title: 'Kullanıcılar | Todo',
  },
  {
    path: '',
    component: TodoPage,
    canActivate: [authGuard],
    pathMatch: 'full',
    title: 'Todo | Gününü düzenle',
  },
  { path: '**', redirectTo: '' },
];
