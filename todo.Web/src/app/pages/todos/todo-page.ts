import { DatePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectorRef, Component, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzCheckboxModule } from 'ng-zorro-antd/checkbox';
import { NzInputModule } from 'ng-zorro-antd/input';
import { NzMessageService } from 'ng-zorro-antd/message';
import { NzPopconfirmModule } from 'ng-zorro-antd/popconfirm';
import { NzSelectModule } from 'ng-zorro-antd/select';
import { NzTagModule } from 'ng-zorro-antd/tag';
import { TodoItem, TodoList, TodoPriority, TodoRequest } from '../../models/todo.models';
import { AuthService } from '../../services/auth.service';
import { TodoApiService } from '../../services/todo-api.service';

const CONCURRENCY_CONFLICT_PROBLEM_TYPE = 'urn:todo:problem:concurrency-conflict';

@Component({
  selector: 'app-todo-page',
  imports: [
    DatePipe,
    RouterLink,
    FormsModule,
    NzButtonModule,
    NzCheckboxModule,
    NzInputModule,
    NzPopconfirmModule,
    NzSelectModule,
    NzTagModule,
  ],
  templateUrl: './todo-page.html',
  styleUrl: './todo-page.scss',
})
export class TodoPage implements OnInit {
  private readonly todoApi = inject(TodoApiService);
  protected readonly auth = inject(AuthService);
  private readonly messages = inject(NzMessageService);
  private readonly changeDetector = inject(ChangeDetectorRef);

  todoLists: TodoList[] = [];
  todos: TodoItem[] = [];
  selectedList: TodoList | null = null;

  isLoadingLists = false;
  isLoadingTodos = false;
  isSavingList = false;
  isSavingTodo = false;
  isApiAvailable = false;

  newListName = '';
  isRenamingList = false;
  renamedListName = '';

  isCreatingTodo = false;
  newTodoTitle = '';
  newTodoDescription = '';
  newTodoDueAtLocal = '';
  newTodoPriority: TodoPriority = 'Normal';

  editingTodoId: number | null = null;
  editedTodoTitle = '';
  editedTodoDescription = '';
  editedTodoDueAtLocal = '';
  editedTodoPriority: TodoPriority = 'Normal';

  ngOnInit(): void {
    void this.loadTodoLists();
  }

  logout(): void {
    void this.auth.logout();
  }

  async loadTodoLists(preferredListId?: number): Promise<void> {
    this.isLoadingLists = true;

    try {
      this.todoLists = await firstValueFrom(this.todoApi.getTodoLists());
      this.isApiAvailable = true;

      if (this.todoLists.length === 0) {
        this.selectedList = null;
        this.todos = [];
        return;
      }

      const currentListId = preferredListId ?? this.selectedList?.id;
      const matchingList = this.todoLists.find((list) => list.id === currentListId);
      const listToSelect = matchingList ?? this.todoLists[0];

      if (listToSelect !== undefined) {
        this.selectedList = listToSelect;
        await this.loadTodos(listToSelect.id);
      }
    } catch (error: unknown) {
      this.showError(error);
    } finally {
      this.isLoadingLists = false;
      this.changeDetector.markForCheck();
    }
  }

  async selectList(todoList: TodoList): Promise<void> {
    this.selectedList = todoList;
    this.isRenamingList = false;
    this.isCreatingTodo = false;
    this.editingTodoId = null;
    await this.loadTodos(todoList.id);
  }

  async createList(): Promise<void> {
    const name = this.newListName.trim();

    if (name.length === 0) {
      this.messages.warning('Liste adı boş olamaz.');
      return;
    }

    this.isSavingList = true;

    try {
      const createdList = await firstValueFrom(
        this.todoApi.createTodoList({ name }),
      );

      this.newListName = '';
      this.messages.success('Liste oluşturuldu.');
      await this.loadTodoLists(createdList.id);
    } catch (error: unknown) {
      this.showError(error, 'Bu isimde bir listen zaten var.');
    } finally {
      this.isSavingList = false;
      this.changeDetector.markForCheck();
    }
  }

  startRenamingList(): void {
    if (this.selectedList === null) {
      return;
    }

    this.renamedListName = this.selectedList.name;
    this.isRenamingList = true;
  }

  async saveListName(): Promise<void> {
    if (this.selectedList === null) {
      return;
    }

    const todoListId = this.selectedList.id;
    const name = this.renamedListName.trim();

    if (name.length === 0) {
      this.messages.warning('Liste adı boş olamaz.');
      return;
    }

    this.isSavingList = true;

    try {
      const renamedList = await firstValueFrom(
        this.todoApi.renameTodoList(todoListId, { name }),
      );

      this.selectedList = renamedList;
      this.todoLists = this.todoLists.map((list) =>
        list.id === renamedList.id ? renamedList : list,
      );
      this.isRenamingList = false;
      this.messages.success('Liste adı güncellendi.');
    } catch (error: unknown) {
      this.showError(error, 'Bu isimde bir listen zaten var.');
    } finally {
      this.isSavingList = false;
      this.changeDetector.markForCheck();
    }
  }

  async deleteList(): Promise<void> {
    if (this.selectedList === null) {
      return;
    }

    const deletedListId = this.selectedList.id;

    try {
      await firstValueFrom(this.todoApi.deleteTodoList(deletedListId));
      this.messages.success('Liste ve içindeki görevler silindi.');
      await this.loadTodoLists();
    } catch (error: unknown) {
      this.showError(error);
    } finally {
      this.changeDetector.markForCheck();
    }
  }

  async loadTodos(todoListId: number): Promise<void> {
    this.isLoadingTodos = true;

    try {
      this.todos = await firstValueFrom(this.todoApi.getTodos(todoListId));
    } catch (error: unknown) {
      this.todos = [];
      this.showError(error);
    } finally {
      this.isLoadingTodos = false;
      this.changeDetector.markForCheck();
    }
  }

  async createTodo(): Promise<void> {
    if (this.selectedList === null) {
      return;
    }

    const todoListId = this.selectedList.id;
    const title = this.newTodoTitle.trim();

    if (title.length === 0) {
      this.messages.warning('Görev başlığı boş olamaz.');
      return;
    }

    this.isSavingTodo = true;

    try {
      const request = this.createTodoRequest(
        title,
        this.newTodoDescription,
        this.newTodoDueAtLocal,
        this.newTodoPriority,
      );
      const createdTodo = await firstValueFrom(
        this.todoApi.createTodo(todoListId, request),
      );

      this.todos = [...this.todos, createdTodo];
      this.newTodoTitle = '';
      this.newTodoDescription = '';
      this.newTodoDueAtLocal = '';
      this.newTodoPriority = 'Normal';
      this.isCreatingTodo = false;
      this.messages.success('Görev oluşturuldu.');
    } catch (error: unknown) {
      this.showError(error, 'Bu listede aynı isimli bir görev zaten var.');
    } finally {
      this.isSavingTodo = false;
      this.changeDetector.markForCheck();
    }
  }

  startEditingTodo(todo: TodoItem): void {
    this.editingTodoId = todo.id;
    this.editedTodoTitle = todo.title;
    this.editedTodoDescription = todo.description ?? '';
    this.editedTodoDueAtLocal = this.toLocalDateTimeValue(todo.dueAtUtc);
    this.editedTodoPriority = todo.priority;
  }

  cancelEditingTodo(): void {
    this.editingTodoId = null;
  }

  async saveTodo(todo: TodoItem): Promise<void> {
    if (this.selectedList === null) {
      return;
    }

    const todoListId = this.selectedList.id;
    const title = this.editedTodoTitle.trim();

    if (title.length === 0) {
      this.messages.warning('Görev başlığı boş olamaz.');
      return;
    }

    this.isSavingTodo = true;

    try {
      const request = this.createTodoRequest(
        title,
        this.editedTodoDescription,
        this.editedTodoDueAtLocal,
        this.editedTodoPriority,
      );
      const updatedTodo = await firstValueFrom(
        this.todoApi.updateTodo(todoListId, todo.id, request),
      );

      this.replaceTodo(updatedTodo);
      this.editingTodoId = null;
      this.messages.success('Görev güncellendi.');
    } catch (error: unknown) {
      this.showError(error, 'Bu listede aynı isimli bir görev zaten var.');
    } finally {
      this.isSavingTodo = false;
      this.changeDetector.markForCheck();
    }
  }

  async setTodoCompletion(todo: TodoItem, isCompleted: boolean): Promise<void> {
    if (this.selectedList === null) {
      return;
    }

    const todoListId = this.selectedList.id;

    try {
      const updatedTodo = await firstValueFrom(
        this.todoApi.setTodoCompletionStatus(
          todoListId,
          todo.id,
          isCompleted,
        ),
      );

      this.replaceTodo(updatedTodo);
    } catch (error: unknown) {
      this.showError(error);
    } finally {
      this.changeDetector.markForCheck();
    }
  }

  async deleteTodo(todo: TodoItem): Promise<void> {
    if (this.selectedList === null) {
      return;
    }

    const todoListId = this.selectedList.id;

    try {
      await firstValueFrom(this.todoApi.deleteTodo(todoListId, todo.id));
      this.todos = this.todos.filter((existingTodo) => existingTodo.id !== todo.id);
      this.messages.success('Görev silindi.');
    } catch (error: unknown) {
      this.showError(error);
    } finally {
      this.changeDetector.markForCheck();
    }
  }

  completedTodoCount(): number {
    return this.todos.filter((todo) => todo.isCompleted).length;
  }

  priorityLabel(priority: TodoPriority): string {
    if (priority === 'High') {
      return 'Yüksek';
    }

    if (priority === 'Low') {
      return 'Düşük';
    }

    return 'Normal';
  }

  priorityColor(priority: TodoPriority): string {
    if (priority === 'High') {
      return 'red';
    }

    if (priority === 'Low') {
      return 'blue';
    }

    return 'gold';
  }

  private createTodoRequest(
    title: string,
    description: string,
    dueAtLocal: string,
    priority: TodoPriority,
  ): TodoRequest {
    return {
      title,
      description: description.trim().length > 0 ? description.trim() : null,
      dueAtUtc: this.toUtcDateTimeValue(dueAtLocal),
      priority,
    };
  }

  minimumDueAtLocal(): string {
    return this.toLocalDateTimeValue(new Date().toISOString());
  }

  private toUtcDateTimeValue(localDateTime: string): string | null {
    if (localDateTime.length === 0) {
      return null;
    }

    return new Date(localDateTime).toISOString();
  }

  private toLocalDateTimeValue(utcDateTime: string | null): string {
    if (utcDateTime === null) {
      return '';
    }

    const date = new Date(utcDateTime);
    const localOffsetInMilliseconds = date.getTimezoneOffset() * 60_000;
    const localDate = new Date(date.getTime() - localOffsetInMilliseconds);

    return localDate.toISOString().slice(0, 16);
  }

  private replaceTodo(updatedTodo: TodoItem): void {
    this.todos = this.todos.map((todo) =>
      todo.id === updatedTodo.id ? updatedTodo : todo,
    );
  }

  private showError(error: unknown, conflictMessage?: string): void {
    let message = 'İstek tamamlanamadı.';

    if (error instanceof HttpErrorResponse) {
      const problem = error.error as { detail?: string; title?: string; type?: string } | null;
      message = problem?.detail ?? problem?.title ?? `Sunucu yanıtı: ${error.status}`;

      if (error.status === 409 && problem?.type === CONCURRENCY_CONFLICT_PROBLEM_TYPE) {
        this.messages.warning('Bu kayıt başka bir sekmede ya da cihazda değiştirildi veya silindi. Liste yenilendi.');
        void this.loadTodoLists();
        return;
      }

      if (error.status === 409 && conflictMessage !== undefined) {
        message = conflictMessage;
      }

      if (error.status === 503) {
        message = 'Sunucu şu anda veritabanına ulaşamıyor. Biraz sonra tekrar dene.';
      }

      if (error.status >= 500 && error.status !== 503) {
        message = 'Beklenmeyen bir hata oluştu. Sorun devam ederse yöneticine bildir.';
      }

      if (error.status === 401) {
        return;
      }

      if (error.status === 0) {
        this.isApiAvailable = false;
        message = 'API sunucusuna bağlanılamadı. HerSeyiBaslat.bat ile uygulamayı aç.';
      }
    }

    this.messages.error(message);
  }
}
