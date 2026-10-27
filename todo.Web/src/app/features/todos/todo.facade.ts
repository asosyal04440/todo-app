import { ApiError } from '../../core/errors/api-error';
import { ApiErrorService } from '../../core/errors/api-error.service';
import { DestroyRef, Injectable, computed, inject, signal } from '@angular/core';
import { NzMessageService } from 'ng-zorro-antd/message';
import {
  EMPTY,
  BehaviorSubject,
  Observable,
  Subject,
  catchError,
  combineLatest,
  defer,
  distinctUntilChanged,
  exhaustMap,
  finalize,
  forkJoin,
  groupBy,
  firstValueFrom,
  map,
  merge,
  mergeMap,
  of,
  switchMap,
  take,
  tap,
  throwError,
} from 'rxjs';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { PagedResult } from '../../shared/pagination/paged-result';
import { DEFAULT_PAGE_SIZE } from '../../shared/pagination/paged-result';
import { TodoItem, TodoList, TodoRequest } from './todo.models';
import { TodoApiService } from './todo-api.service';

type ErrorTarget = 'read' | 'list-create' | 'list-rename' | 'todo-create' | 'todo-edit';

interface TodoReadCommand {
  listId: number;
  pageNumber: number;
  refreshLists: boolean;
  done: () => void;
}

interface WorkspacePage {
  lists: PagedResult<TodoList>;
  selectedList: TodoList | null;
  todos: PagedResult<TodoItem>;
}

const CONCURRENCY_CONFLICT_PROBLEM_TYPE = 'urn:todo:problem:concurrency-conflict';

@Injectable()
export class TodoFacade {
  private readonly todoApi = inject(TodoApiService);
  private readonly destroyRef = inject(DestroyRef);
  readonly errorTarget = signal<ErrorTarget>('read');
  readonly errorTodoId = signal<number | null>(null);
  readonly error = signal<ApiError | null>(null);
  private readonly errors = inject(ApiErrorService);
  private readonly messages = inject(NzMessageService);
  readonly listCreationVersion = signal<number>(0);

  readonly todoLists = signal<TodoList[]>([]);
  readonly todos = signal<TodoItem[]>([]);
  readonly selectedList = signal<TodoList | null>(null);
  readonly pageSize = DEFAULT_PAGE_SIZE;
  readonly listPageNumber = signal<number>(1);
  readonly listTotalCount = signal<number>(0);
  readonly listTotalPages = signal<number>(0);
  readonly todoPageNumber = signal<number>(1);
  readonly todoTotalCount = signal<number>(0);
  readonly todoTotalPages = signal<number>(0);
  private readonly selectedListIds = new BehaviorSubject<number | null>(null);
  private readonly todoPages = new BehaviorSubject<TodoReadCommand | null>(null);
  private readonly todoReloads = new Subject<TodoReadCommand>();
  private activeTodoLoad: { command: TodoReadCommand; promise: Promise<void> } | null = null;
  private readonly completionRequests = new Subject<{
    todoListId: number;
    todoId: number;
    isCompleted: boolean;
  }>();
  readonly completingTodoIds = signal<ReadonlySet<number>>(new Set());
  private destroyed = false;

  readonly listLoadState = signal<'idle' | 'loading' | 'loaded' | 'error'>('idle');
  readonly isLoadingLists = computed(() => this.listLoadState() === 'loading');
  readonly todoLoadState = signal<'idle' | 'loading' | 'loaded' | 'error'>('idle');
  readonly isLoadingTodos = computed(() => this.todoLoadState() === 'loading');
  readonly isSavingList = signal<boolean>(false);
  readonly isSavingTodo = signal<boolean>(false);
  readonly isApiAvailable = signal<boolean>(false);
  readonly isRefreshing = signal(false);
  readonly canRefresh = computed(
    () =>
      !this.isLoadingLists() &&
      !this.isLoadingTodos() &&
      !this.isSavingList() &&
      !this.isSavingTodo() &&
      !this.isCreatingTodo() &&
      !this.isRenamingList() &&
      this.editingTodoId() === null &&
      this.completingTodoIds().size === 0,
  );

  readonly isRenamingList = signal<boolean>(false);

  readonly isCreatingTodo = signal<boolean>(false);

  readonly editingTodoId = signal<number | null>(null);

  constructor() {
    const destroyRef = this.destroyRef;
    destroyRef.onDestroy(() => {
      this.destroyed = true;
    });
    const queryChanges = combineLatest([this.selectedListIds, this.todoPages]).pipe(
      // A page belongs to a list: resetting the list and its page must not fetch an old page.
      map(([listId, page]) => (page?.listId === listId ? page : null)),
      distinctUntilChanged(
        (previous, current) =>
          previous?.listId === current?.listId && previous?.pageNumber === current?.pageNumber,
      ),
    );
    merge(queryChanges, this.todoReloads)
      .pipe(
        switchMap((command) =>
          command === null
            ? EMPTY
            : defer(() => {
                this.error.set(null);
                this.todoLoadState.set('loading');
                if (command.refreshLists) {
                  this.isRefreshing.set(true);
                  this.listLoadState.set('loading');
                  return this.fetchWorkspacePage(command);
                }
                this.todos.set([]);
                this.todoTotalCount.set(0);
                this.todoTotalPages.set(0);
                return this.fetchTodoPage(command.listId, command.pageNumber);
              }).pipe(
                tap((result) => {
                  const page = 'lists' in result ? result.todos : result;
                  if ('lists' in result) {
                    this.applyListPage(result.lists);
                    this.selectedList.set(result.selectedList);
                    this.listLoadState.set('loaded');
                  }
                  this.todos.set(page.items);
                  this.todoPageNumber.set(page.pageNumber);
                  this.todoTotalCount.set(page.totalCount);
                  this.todoTotalPages.set(page.totalPages);
                  this.todoLoadState.set('loaded');
                  this.isApiAvailable.set(true);
                }),
                catchError((error: unknown) => {
                  this.todoLoadState.set('error');
                  if (command.refreshLists) this.listLoadState.set('error');
                  this.showError(error);
                  return EMPTY;
                }),
                finalize(() => {
                  if (this.todoLoadState() === 'loading') this.todoLoadState.set('idle');
                  if (command.refreshLists) {
                    this.isRefreshing.set(false);
                    if (this.listLoadState() === 'loading') this.listLoadState.set('idle');
                  }
                  command.done();
                }),
              ),
        ),
        takeUntilDestroyed(destroyRef),
      )
      .subscribe();

    this.completionRequests
      .pipe(
        groupBy((command) => command.todoId),
        mergeMap((requests) =>
          requests.pipe(
            exhaustMap((command) =>
              defer(() => {
                this.error.set(null);
                this.completingTodoIds.update((ids) => new Set(ids).add(command.todoId));
                return this.todoApi.setTodoCompletionStatus(
                  command.todoListId,
                  command.todoId,
                  command.isCompleted,
                );
              }).pipe(
                take(1),
                tap((updatedTodo) => {
                  if (this.selectedList()?.id === command.todoListId) this.replaceTodo(updatedTodo);
                }),
                catchError((error: unknown) => {
                  if (this.selectedList()?.id === command.todoListId) this.showError(error);
                  return EMPTY;
                }),
                finalize(() => {
                  this.completingTodoIds.update((ids) => {
                    const remaining = new Set(ids);
                    remaining.delete(command.todoId);
                    return remaining;
                  });
                }),
              ),
            ),
          ),
        ),
        takeUntilDestroyed(destroyRef),
      )
      .subscribe();
  }

  private fetchTodoPage(listId: number, pageNumber: number): Observable<PagedResult<TodoItem>> {
    return defer(() => this.todoApi.getTodos(listId, pageNumber, this.pageSize)).pipe(
      take(1),
      switchMap((page) =>
        pageNumber > Math.max(1, page.totalPages)
          ? this.fetchTodoPage(listId, Math.max(1, page.totalPages))
          : of(page),
      ),
    );
  }

  private fetchListPage(pageNumber: number): Observable<PagedResult<TodoList>> {
    return defer(() => this.todoApi.getTodoLists(pageNumber, this.pageSize)).pipe(
      take(1),
      switchMap((page) =>
        pageNumber > Math.max(1, page.totalPages)
          ? this.fetchListPage(Math.max(1, page.totalPages))
          : of(page),
      ),
    );
  }

  private fetchWorkspacePage(command: TodoReadCommand): Observable<WorkspacePage> {
    return forkJoin({
      lists: this.fetchListPage(this.listPageNumber()),
      todos: this.fetchTodoPage(command.listId, command.pageNumber).pipe(
        catchError((error: unknown) =>
          this.errors.normalize(error).status === 404 ? of(null) : throwError(() => error),
        ),
      ),
    }).pipe(
      switchMap(({ lists, todos }) => {
        const selectedList =
          lists.items.find((list) => list.id === command.listId) ?? lists.items[0] ?? null;
        if (selectedList === null) {
          return of({
            lists,
            selectedList,
            todos: {
              items: [],
              pageNumber: 1,
              pageSize: this.pageSize,
              totalCount: 0,
              totalPages: 0,
            },
          });
        }
        if (selectedList.id === command.listId) {
          // A successful list response containing the selected list cannot reconcile a failed task read.
          if (todos === null)
            return throwError(() => new ApiError(404, 'Seçili liste bulunamadı. Yeniden deneyin.'));
          return of({ lists, selectedList, todos });
        }
        return this.fetchTodoPage(selectedList.id, 1).pipe(
          map((page) => ({ lists, selectedList, todos: page })),
        );
      }),
    );
  }

  private applyListPage(page: PagedResult<TodoList>): void {
    this.todoLists.set(page.items);
    this.listPageNumber.set(page.pageNumber);
    this.listTotalCount.set(page.totalCount);
    this.listTotalPages.set(page.totalPages);
    this.isApiAvailable.set(true);
  }

  async refreshWorkspace(): Promise<void> {
    if (this.destroyed || !this.canRefresh()) return;
    const list = this.selectedList();
    if (list === null) {
      await this.loadTodoLists();
      return;
    }
    await this.requestTodos(list.id, this.todoPageNumber(), true, true);
  }

  async loadTodoLists(preferredListId?: number, pageNumber = this.listPageNumber()): Promise<void> {
    if (this.destroyed) return;
    this.error.set(null);
    this.listLoadState.set('loading');

    try {
      const page = await firstValueFrom(
        this.fetchListPage(pageNumber).pipe(takeUntilDestroyed(this.destroyRef)),
      );
      if (this.destroyed) return;
      this.applyListPage(page);

      if (this.todoLists().length === 0) {
        this.selectedListIds.next(null);
        this.todoPages.next(null);
        this.todoLoadState.set('loaded');
        this.selectedList.set(null);
        this.todos.set([]);
        this.todoTotalCount.set(0);
        this.todoTotalPages.set(0);
        this.todoPageNumber.set(1);
        return;
      }

      const currentListId = preferredListId ?? this.selectedList()?.id;
      const matchingList = this.todoLists().find((list) => list.id === currentListId);
      const listToSelect = matchingList ?? this.todoLists()[0];

      if (listToSelect !== undefined) {
        if (this.selectedList()?.id !== listToSelect.id) this.todoPageNumber.set(1);
        this.selectedList.set(listToSelect);
        await this.loadTodos(listToSelect.id);
      }
    } catch (error: unknown) {
      this.listLoadState.set('error');
      this.showError(error);
    } finally {
      if (this.listLoadState() === 'loading') this.listLoadState.set('loaded');
    }
  }

  async selectList(todoList: TodoList): Promise<void> {
    if (this.selectedList()?.id === todoList.id && this.todoLoadState() !== 'error') {
      await (this.activeTodoLoad?.promise ?? Promise.resolve());
      return;
    }
    this.selectedList.set(todoList);
    this.isRenamingList.set(false);
    this.isCreatingTodo.set(false);
    this.editingTodoId.set(null);
    this.todoPageNumber.set(1);
    await this.requestTodos(todoList.id, 1, false);
  }

  async createList(listName: string): Promise<void> {
    const name = listName.trim();

    if (name.length === 0) {
      this.messages.warning('Liste adı boş olamaz.');
      return;
    }

    this.error.set(null);
    this.isSavingList.set(true);

    try {
      const createdList = await firstValueFrom(this.todoApi.createTodoList({ name }));

      this.listCreationVersion.update((value) => value + 1);
      this.messages.success('Liste oluşturuldu.');
      this.listPageNumber.set(Math.floor(this.listTotalCount() / this.pageSize) + 1);
      await this.loadTodoLists(createdList.id);
    } catch (error: unknown) {
      this.showError(error, 'Bu isimde bir listen zaten var.', 'list-create');
    } finally {
      this.isSavingList.set(false);
    }
  }

  async saveListName(listName: string): Promise<void> {
    const selectedList = this.selectedList();
    if (selectedList === null) {
      return;
    }

    const todoListId = selectedList.id;
    const name = listName.trim();

    if (name.length === 0) {
      this.messages.warning('Liste adı boş olamaz.');
      return;
    }

    this.error.set(null);
    this.isSavingList.set(true);

    try {
      const renamedList = await firstValueFrom(this.todoApi.renameTodoList(todoListId, { name }));

      this.selectedList.set(renamedList);
      this.todoLists.set(
        this.todoLists().map((list) => (list.id === renamedList.id ? renamedList : list)),
      );
      this.isRenamingList.set(false);
      this.messages.success('Liste adı güncellendi.');
    } catch (error: unknown) {
      this.showError(error, 'Bu isimde bir listen zaten var.', 'list-rename');
    } finally {
      this.isSavingList.set(false);
    }
  }

  async deleteList(): Promise<void> {
    const selectedList = this.selectedList();
    if (selectedList === null) {
      return;
    }

    const deletedListId = selectedList.id;
    this.error.set(null);
    this.isSavingList.set(true);

    try {
      await firstValueFrom(this.todoApi.deleteTodoList(deletedListId));
      this.messages.success('Liste ve içindeki görevler silindi.');
      await this.loadTodoLists();
    } catch (error: unknown) {
      this.showError(error);
    } finally {
      this.isSavingList.set(false);
    }
  }

  loadTodos(todoListId: number, pageNumber = this.todoPageNumber()): Promise<void> {
    return this.requestTodos(todoListId, pageNumber, true);
  }

  private requestTodos(
    listId: number,
    pageNumber: number,
    force: boolean,
    refreshLists = false,
  ): Promise<void> {
    if (this.destroyed) return Promise.resolve();
    const previous = this.todoPages.value;
    const sameQuery = previous?.listId === listId && previous.pageNumber === pageNumber;
    if (sameQuery && !force && this.todoLoadState() !== 'error')
      return this.activeTodoLoad?.promise ?? Promise.resolve();

    let settle!: () => void;
    const promise = new Promise<void>((resolve) => {
      settle = resolve;
    });
    const command: TodoReadCommand = {
      listId,
      pageNumber,
      refreshLists,
      done: () => {
        if (this.activeTodoLoad?.command === command) this.activeTodoLoad = null;
        settle();
      },
    };
    this.activeTodoLoad = { command, promise };
    if (sameQuery) {
      // Explicit reloads (including CRUD follow-ups and error retries) bypass query deduplication.
      this.todoReloads.next(command);
    } else {
      this.selectedListIds.next(listId);
      this.todoPages.next(command);
    }
    return promise;
  }

  async changeListPage(pageNumber: number): Promise<void> {
    if (
      this.isLoadingLists() ||
      this.isSavingList() ||
      this.isSavingTodo() ||
      pageNumber < 1 ||
      pageNumber > this.listTotalPages()
    )
      return;
    this.editingTodoId.set(null);
    this.isCreatingTodo.set(false);
    this.isRenamingList.set(false);
    await this.loadTodoLists(undefined, pageNumber);
  }

  async changeTodoPage(pageNumber: number): Promise<void> {
    const selectedList = this.selectedList();
    if (
      selectedList === null ||
      this.isLoadingLists() ||
      this.isLoadingTodos() ||
      this.isSavingTodo() ||
      this.isSavingList() ||
      pageNumber < 1 ||
      pageNumber > this.todoTotalPages()
    )
      return;
    this.editingTodoId.set(null);
    await this.requestTodos(selectedList.id, pageNumber, false);
  }

  async createTodo(request: TodoRequest): Promise<void> {
    const selectedList = this.selectedList();
    if (selectedList === null) {
      return;
    }

    const todoListId = selectedList.id;
    const title = request.title.trim();

    if (title.length === 0) {
      this.messages.warning('Görev başlığı boş olamaz.');
      return;
    }

    this.error.set(null);
    this.isSavingTodo.set(true);

    try {
      await firstValueFrom(this.todoApi.createTodo(todoListId, { ...request, title }));

      if (this.selectedList()?.id !== todoListId) return;
      this.todoPageNumber.set(Math.floor(this.todoTotalCount() / this.pageSize) + 1);
      if (this.selectedList()?.id === todoListId) await this.loadTodos(todoListId);
      this.isCreatingTodo.set(false);
      this.messages.success('Görev oluşturuldu.');
    } catch (error: unknown) {
      this.showError(error, 'Bu listede aynı isimli bir görev zaten var.', 'todo-create');
    } finally {
      this.isSavingTodo.set(false);
    }
  }

  async saveTodo(todo: TodoItem, request: TodoRequest): Promise<void> {
    const selectedList = this.selectedList();
    if (selectedList === null) {
      return;
    }

    const todoListId = selectedList.id;
    const title = request.title.trim();

    if (title.length === 0) {
      this.messages.warning('Görev başlığı boş olamaz.');
      return;
    }

    this.error.set(null);
    this.isSavingTodo.set(true);

    try {
      const updatedTodo = await firstValueFrom(
        this.todoApi.updateTodo(todoListId, todo.id, { ...request, title }),
      );

      if (this.selectedList()?.id !== todoListId) return;
      this.replaceTodo(updatedTodo);
      this.editingTodoId.set(null);
      this.messages.success('Görev güncellendi.');
    } catch (error: unknown) {
      this.showError(error, 'Bu listede aynı isimli bir görev zaten var.', 'todo-edit', todo.id);
    } finally {
      this.isSavingTodo.set(false);
    }
  }

  setTodoCompletion(todo: TodoItem, isCompleted: boolean): void {
    const selectedList = this.selectedList();
    if (this.destroyed || selectedList === null || selectedList.id !== todo.todoListId) {
      return;
    }
    this.completionRequests.next({ todoListId: selectedList.id, todoId: todo.id, isCompleted });
  }

  async deleteTodo(todo: TodoItem): Promise<void> {
    const selectedList = this.selectedList();
    if (selectedList === null) {
      return;
    }

    const todoListId = selectedList.id;
    this.error.set(null);
    this.isSavingTodo.set(true);

    try {
      await firstValueFrom(this.todoApi.deleteTodo(todoListId, todo.id));
      if (this.selectedList()?.id === todoListId) await this.loadTodos(todoListId);
      this.messages.success('Görev silindi.');
    } catch (error: unknown) {
      this.showError(error);
    } finally {
      this.isSavingTodo.set(false);
    }
  }

  private replaceTodo(updatedTodo: TodoItem): void {
    this.todos.set(this.todos().map((todo) => (todo.id === updatedTodo.id ? updatedTodo : todo)));
  }

  private showError(
    error: unknown,
    conflictMessage?: string,
    target: ErrorTarget = 'read',
    todoId: number | null = null,
  ): void {
    if (this.destroyed) return;
    const normalized = this.errors.normalize(error);
    if (normalized.status === 0) this.isApiAvailable.set(false);
    if (normalized.status === 401) return;
    if (normalized.status === 409 && normalized.problemType === CONCURRENCY_CONFLICT_PROBLEM_TYPE) {
      this.messages.warning(
        'Bu kayıt başka bir sekmede ya da cihazda değiştirildi veya silindi. Liste yenilendi.',
      );
      void this.loadTodoLists();
      return;
    }
    this.errorTarget.set(target);
    this.errorTodoId.set(todoId);
    const specific = normalized.status === 409 ? conflictMessage : undefined;
    this.error.set(
      specific
        ? new ApiError(normalized.status, specific, normalized.problemType, normalized.fieldErrors)
        : normalized,
    );
  }
}
