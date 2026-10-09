import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { resolveApiUrl } from '../core/api-url';
import { TodoItem, TodoList, TodoListRequest, TodoRequest } from '../models/todo.models';

@Injectable({
  providedIn: 'root',
})
export class TodoApiService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = resolveApiUrl();

  getTodoLists(): Observable<TodoList[]> {
    return this.http.get<TodoList[]>(`${this.apiUrl}/todo-lists`);
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

  getTodos(todoListId: number): Observable<TodoItem[]> {
    return this.http.get<TodoItem[]>(`${this.apiUrl}/todo-lists/${todoListId}/todos`);
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
    return this.http.delete<void>(
      `${this.apiUrl}/todo-lists/${todoListId}/todos/${todoId}`,
    );
  }
}
