import { Routes } from '@angular/router';

export const TODO_ROUTES: Routes = [
  { path: '', loadComponent: () => import('./todo-page').then((module) => module.TodoPage) },
];
