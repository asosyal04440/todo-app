import { ApiError } from '../../../core/errors/api-error';
import { Component, inject, computed, input, EventEmitter, Input, Output } from '@angular/core';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { TodoItem, TodoRequest } from '../todo.models';
import { TodoRow } from './todo-row';

@Component({
  selector: 'app-todo-list-view',
  imports: [NzButtonModule, TodoRow],
  templateUrl: './todo-list-view.html',
  styleUrl: './todo-list-view.scss',
})
export class TodoListView {
  readonly todos = input<TodoItem[]>([]);
  @Input() completingIds: ReadonlySet<number> = new Set();
  @Input() loading = false;
  @Input() saving = false;
  @Input() apiError: ApiError | null = null;
  @Input() busy = false;
  @Input() editingId: number | null = null;
  @Input() pageNumber = 1;
  @Input() totalPages = 0;
  @Output() readonly createRequested = new EventEmitter<void>();
  @Output() readonly editRequested = new EventEmitter<TodoItem>();
  @Output() readonly editCancelled = new EventEmitter<void>();
  @Output() readonly updateRequested = new EventEmitter<{ todo: TodoItem; request: TodoRequest }>();
  @Output() readonly completionRequested = new EventEmitter<{
    todo: TodoItem;
    isCompleted: boolean;
  }>();
  @Output() readonly deleteRequested = new EventEmitter<TodoItem>();
  @Output() readonly pageRequested = new EventEmitter<number>();

  readonly completedCount = computed(() => this.todos().filter((todo) => todo.isCompleted).length);
}
