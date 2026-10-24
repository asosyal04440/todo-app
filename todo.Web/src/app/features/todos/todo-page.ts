import { ApiErrors } from '../../shared/errors/api-errors';
import { Component, OnInit, inject } from '@angular/core';
import { AuthService } from '../../core/auth/auth.service';
import { WorkspaceLayout } from '../../core/layout/workspace-layout';
import { WorkspaceHeader } from '../../core/layout/workspace-header';
import { TodoListSidebar } from './components/todo-list-sidebar';
import { TodoListHeader } from './components/todo-list-header';
import { TodoForm } from './components/todo-form';
import { TodoListView } from './components/todo-list-view';
import { TodoFacade } from './todo.facade';

@Component({
  selector: 'app-todo-page',
  imports: [
    ApiErrors,
    WorkspaceLayout,
    WorkspaceHeader,
    TodoListSidebar,
    TodoListHeader,
    TodoForm,
    TodoListView,
  ],
  providers: [TodoFacade],
  templateUrl: './todo-page.html',
  styleUrl: './todo-page.scss',
})
export class TodoPage implements OnInit {
  readonly facade = inject(TodoFacade);
  readonly auth = inject(AuthService);
  ngOnInit(): void {
    void this.facade.loadTodoLists();
  }
}
