import { ApiError } from '../../../core/errors/api-error';
import { ApiErrorService } from '../../../core/errors/api-error.service';
import {
  Component,
  inject,
  EventEmitter,
  Input,
  OnChanges,
  Output,
  SimpleChanges,
} from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { requiredTrimmed } from '../../../shared/forms/form-validators';
import { FormFieldError } from '../../../shared/forms/form-field-error';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzInputModule } from 'ng-zorro-antd/input';
import { TodoList } from '../todo.models';
import { TODO_VALIDATION_LIMITS } from '../todo-validation';

@Component({
  selector: 'app-todo-list-sidebar',
  imports: [ReactiveFormsModule, FormFieldError, NzButtonModule, NzInputModule],
  templateUrl: './todo-list-sidebar.html',
  styleUrl: './todo-list-sidebar.scss',
})
export class TodoListSidebar implements OnChanges {
  @Input() lists: TodoList[] = [];
  @Input() selectedId: number | null = null;
  @Input() totalCount = 0;
  @Input() pageNumber = 1;
  @Input() totalPages = 0;
  @Input() loading = false;
  @Input() saving = false;
  @Input() apiError: ApiError | null = null;
  @Input() busy = false;
  @Input() apiAvailable = false;
  @Input() creationVersion = 0;
  @Output() readonly createRequested = new EventEmitter<string>();
  @Output() readonly selectRequested = new EventEmitter<TodoList>();
  @Output() readonly pageRequested = new EventEmitter<number>();
  readonly validationLimits = TODO_VALIDATION_LIMITS;
  readonly form = inject(FormBuilder).nonNullable.group({
    name: [
      '',
      [requiredTrimmed, Validators.maxLength(TODO_VALIDATION_LIMITS.todoListNameMaximumLength)],
    ],
  });

  private readonly errors = inject(ApiErrorService);
  ngOnChanges(changes: SimpleChanges): void {
    if (changes['creationVersion']) this.form.reset();
    if (changes['apiError'] && this.apiError)
      this.errors.applyFieldErrors(this.form, this.apiError);
  }
  submit(): void {
    if (this.saving || this.busy) return;
    this.form.markAllAsTouched();
    if (this.form.valid) this.createRequested.emit(this.form.controls.name.value.trim());
  }
}
