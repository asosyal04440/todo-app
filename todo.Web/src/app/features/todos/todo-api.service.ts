import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { API_URL } from '../../core/config/api-url.token';
import { TodoItem, TodoList, TodoListRequest, TodoRequest } from './todo.models';
import { DEFAULT_PAGE_SIZE, PagedResult } from '../../shared/pagination/paged-result';

@Injectable({
  providedIn: 'root',
})
export class TodoApiService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = inject(API_URL);

  getTodoLists(pageNumber = 1, pageSize = DEFAULT_PAGE_SIZE): Observable<PagedResult<TodoList>> {
    return this.http.get<PagedResult<TodoList>>(`${this.apiUrl}/todo-lists`, {
      params: { pageNumber, pageSize },
    });
  }

  createTodoList(request: TodoListRequest): Observable<TodoList> {
    return this.http.post<TodoList>(`${this.apiUrl}/todo-lists`, request);
  }

  renameTodoList(todoListId: number, request: TodoListRequest): Observable<TodoList> {
    return this.http.put<TodoList>(`${this.apiUrl}/todo-lists/${todoListId}`, request);
  }

  deleteTodoList(todoListId: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/todo-lists/${todoListId}`);
  }

  getTodos(
    todoListId: number,
    pageNumber = 1,
    pageSize = DEFAULT_PAGE_SIZE,
  ): Observable<PagedResult<TodoItem>> {
    return this.http.get<PagedResult<TodoItem>>(`${this.apiUrl}/todo-lists/${todoListId}/todos`, {
      params: { pageNumber, pageSize },
    });
  }

  createTodo(todoListId: number, request: TodoRequest): Observable<TodoItem> {
    return this.http.post<TodoItem>(`${this.apiUrl}/todo-lists/${todoListId}/todos`, request);
  }

  updateTodo(todoListId: number, todoId: number, request: TodoRequest): Observable<TodoItem> {
    return this.http.put<TodoItem>(
      `${this.apiUrl}/todo-lists/${todoListId}/todos/${todoId}`,
      request,
    );
  }

  setTodoCompletionStatus(
    todoListId: number,
    todoId: number,
    isCompleted: boolean,
  ): Observable<TodoItem> {
    return this.http.patch<TodoItem>(
      `${this.apiUrl}/todo-lists/${todoListId}/todos/${todoId}/completion`,
      { isCompleted },
    );
  }

  deleteTodo(todoListId: number, todoId: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/todo-lists/${todoListId}/todos/${todoId}`);
  }
}
