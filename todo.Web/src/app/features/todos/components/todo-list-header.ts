import { ApiError } from '../../../core/errors/api-error';
import { ApiErrorService } from '../../../core/errors/api-error.service';
import {
  Component,
  inject,
  computed,
  input,
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
import { NzPopconfirmModule } from 'ng-zorro-antd/popconfirm';
import { TodoItem, TodoList } from '../todo.models';
import { TODO_VALIDATION_LIMITS } from '../todo-validation';

@Component({
  selector: 'app-todo-list-header',
  imports: [ReactiveFormsModule, FormFieldError, NzButtonModule, NzInputModule, NzPopconfirmModule],
  templateUrl: './todo-list-header.html',
  styleUrl: './todo-list-header.scss',
})
export class TodoListHeader implements OnChanges {
  @Input({ required: true }) list!: TodoList;
  readonly todos = input<TodoItem[]>([]);
  @Input() totalCount = 0;
  @Input() renaming = false;
  @Input() saving = false;
  @Input() busy = false;
  @Input() apiError: ApiError | null = null;
  @Output() readonly renameStarted = new EventEmitter<void>();
  @Output() readonly renameCancelled = new EventEmitter<void>();
  @Output() readonly renameRequested = new EventEmitter<string>();
  @Output() readonly deleteRequested = new EventEmitter<void>();
  readonly validationLimits = TODO_VALIDATION_LIMITS;
  readonly form = inject(FormBuilder).nonNullable.group({
    name: [
      '',
      [requiredTrimmed, Validators.maxLength(TODO_VALIDATION_LIMITS.todoListNameMaximumLength)],
    ],
  });

  private readonly errors = inject(ApiErrorService);
  ngOnChanges(changes: SimpleChanges): void {
    if (changes['list'] || changes['renaming']?.currentValue === true)
      this.form.reset({ name: this.list.name });
    if (changes['apiError'] && this.apiError)
      this.errors.applyFieldErrors(this.form, this.apiError);
  }

  readonly completedCount = computed(() => this.todos().filter((todo) => todo.isCompleted).length);
  submit(): void {
    if (this.saving) return;
    this.form.markAllAsTouched();
    if (this.form.valid) this.renameRequested.emit(this.form.controls.name.value.trim());
  }
}
