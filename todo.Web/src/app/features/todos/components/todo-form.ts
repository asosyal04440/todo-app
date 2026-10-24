import { ApiError } from '../../../core/errors/api-error';
import { ApiErrorService } from '../../../core/errors/api-error.service';
import {
  Component,
  EventEmitter,
  Input,
  OnChanges,
  Output,
  SimpleChanges,
  inject,
} from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { requiredTrimmed } from '../../../shared/forms/form-validators';
import { FormFieldError } from '../../../shared/forms/form-field-error';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzInputModule } from 'ng-zorro-antd/input';
import { NzSelectModule } from 'ng-zorro-antd/select';
import { TodoItem, TodoPriority, TodoRequest } from '../todo.models';
import { toLocalDateTimeValue, toUtcDateTimeValue } from '../todo-date-time';
import {
  TODO_DUE_DATE_CLOCK_TOLERANCE_MS,
  TODO_VALIDATION_LIMITS,
  todoDueDateValidator,
} from '../todo-validation';

@Component({
  selector: 'app-todo-form',
  imports: [ReactiveFormsModule, FormFieldError, NzButtonModule, NzInputModule, NzSelectModule],
  templateUrl: './todo-form.html',
  styleUrl: './todo-form.scss',
})
export class TodoForm implements OnChanges {
  @Input() todo: TodoItem | null = null;
  @Input() saving = false;
  @Input() apiError: ApiError | null = null;
  @Output() readonly submitted = new EventEmitter<TodoRequest>();
  @Output() readonly cancelled = new EventEmitter<void>();
  readonly validationLimits = TODO_VALIDATION_LIMITS;
  private readonly builder = inject(FormBuilder);
  readonly form = this.builder.nonNullable.group({
    title: [
      '',
      [requiredTrimmed, Validators.maxLength(TODO_VALIDATION_LIMITS.todoTitleMaximumLength)],
    ],
    description: ['', Validators.maxLength(TODO_VALIDATION_LIMITS.todoDescriptionMaximumLength)],
    dueAtLocal: ['', todoDueDateValidator(() => this.todo === null)],
    priority: this.builder.nonNullable.control<TodoPriority>(
      'Normal',
      Validators.pattern(/^(Low|Normal|High)$/),
    ),
  });

  private readonly errors = inject(ApiErrorService);
  ngOnChanges(changes: SimpleChanges): void {
    if (!changes['todo']) {
      if (changes['apiError'] && this.apiError)
        this.errors.applyFieldErrors(this.form, this.apiError, { dueatutc: 'dueAtLocal' });
      return;
    }
    this.form.reset({
      title: this.todo?.title ?? '',
      description: this.todo?.description ?? '',
      dueAtLocal: toLocalDateTimeValue(this.todo?.dueAtUtc ?? null),
      priority: this.todo?.priority ?? 'Normal',
    });
  }

  minimumDueAtLocal(): string {
    return this.todo === null
      ? toLocalDateTimeValue(new Date(Date.now() - TODO_DUE_DATE_CLOCK_TOLERANCE_MS).toISOString())
      : '';
  }

  submit(): void {
    if (this.saving) return;
    this.form.controls.dueAtLocal.updateValueAndValidity();
    this.form.markAllAsTouched();
    if (this.form.invalid) return;
    const { title, description, dueAtLocal, priority } = this.form.getRawValue();
    this.submitted.emit({
      title: title.trim(),
      description: description.trim() || null,
      dueAtUtc: toUtcDateTimeValue(dueAtLocal),
      priority,
    });
  }
}
