import { ApiError } from '../../../core/errors/api-error';
import { DatePipe } from '@angular/common';
import { Component, EventEmitter, Input, Output } from '@angular/core';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzPopconfirmModule } from 'ng-zorro-antd/popconfirm';
import { NzTagModule } from 'ng-zorro-antd/tag';
import { TodoItem, TodoPriority, TodoRequest } from '../todo.models';
import { TodoForm } from './todo-form';

@Component({
  selector: 'app-todo-row',
  imports: [DatePipe, NzButtonModule, NzPopconfirmModule, NzTagModule, TodoForm],
  templateUrl: './todo-row.html',
  styleUrl: './todo-row.scss',
})
export class TodoRow {
  @Input({ required: true }) todo!: TodoItem;
  @Input() editing = false;
  @Input() saving = false;
  @Input() apiError: ApiError | null = null;
  @Output() readonly editRequested = new EventEmitter<TodoItem>();
  @Output() readonly editCancelled = new EventEmitter<void>();
  @Output() readonly updateRequested = new EventEmitter<{ todo: TodoItem; request: TodoRequest }>();
  @Output() readonly completionRequested = new EventEmitter<{
    todo: TodoItem;
    isCompleted: boolean;
  }>();
  @Output() readonly deleteRequested = new EventEmitter<TodoItem>();

  changeCompletion(event: Event): void {
    const checkbox = event.target as HTMLInputElement;
    this.completionRequested.emit({ todo: this.todo, isCompleted: checkbox.checked });
    checkbox.checked = this.todo.isCompleted;
  }

  priorityLabel(priority: TodoPriority): string {
    return { Low: 'Düşük', Normal: 'Normal', High: 'Yüksek' }[priority];
  }

  priorityColor(priority: TodoPriority): string {
    return { Low: 'blue', Normal: 'gold', High: 'red' }[priority];
  }
}
