import { HttpErrorResponse } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { NzMessageService } from 'ng-zorro-antd/message';
import { of, Subject, throwError } from 'rxjs';
import { PagedResult } from '../../shared/pagination/paged-result';
import { TodoItem } from './todo.models';
import { CurrentUser } from '../../core/auth/auth.models';
import { TodoApiService } from './todo-api.service';
import { TodoFacade } from './todo.facade';

describe('TodoFacade', () => {
  const signedInUser: CurrentUser = {
    id: 1,
    email: 'ali@example.com',
    displayName: 'Ali',
    role: 'User',
    hasPassword: true,
    hasGoogleLogin: false,
    createdAtUtc: '2026-10-06T10:00:00Z',
  };
  const showErrorMessage = vi.fn();
  const showWarningMessage = vi.fn();
  const createTodoList = vi.fn();
  const deleteTodo = vi.fn();
  const getTodoLists = vi.fn();
  const getTodos = vi.fn();
  const createTodo = vi.fn();
  const updateTodo = vi.fn();
  const renameTodoList = vi.fn();
  const setTodoCompletionStatus = vi.fn();
  const emptyPage = { items: [], totalCount: 0, pageNumber: 1, pageSize: 20, totalPages: 0 };

  beforeEach(async () => {
    showErrorMessage.mockClear();
    showWarningMessage.mockClear();
    createTodoList.mockReset();
    deleteTodo.mockReset();
    getTodoLists.mockReset();
    getTodoLists.mockReturnValue(of(emptyPage));
    getTodos.mockReset();
    getTodos.mockReturnValue(of(emptyPage));
    createTodo.mockReset();
    updateTodo.mockReset();
    renameTodoList.mockReset();
    setTodoCompletionStatus.mockReset();

    await TestBed.configureTestingModule({
      providers: [
        TodoFacade,
        {
          provide: TodoApiService,
          useValue: {
            getTodoLists,
            getTodos,
            createTodoList,
            deleteTodo,
            createTodo,
            updateTodo,
            renameTodoList,
            setTodoCompletionStatus,
          },
        },
        {
          provide: NzMessageService,
          useValue: {
            success: vi.fn(),
            warning: showWarningMessage,
            error: showErrorMessage,
          },
        },
      ],
    }).compileComponents();
  });

  const list = { id: 5, name: 'Ev', createdAtUtc: signedInUser.createdAtUtc };
  const todo: TodoItem = {
    id: 9,
    todoListId: 5,
    title: 'Süt al',
    description: null,
    isCompleted: false,
    priority: 'Normal',
    dueAtUtc: null,
    createdAtUtc: signedInUser.createdAtUtc,
  };
  const request = {
    title: '  Ekmek al  ',
    description: 'Fırından',
    dueAtUtc: null,
    priority: 'High' as const,
  };

  it('cancels A immediately on selection B and clears A rows while B loads', async () => {
    const a = new Subject<PagedResult<TodoItem>>();
    const b = new Subject<PagedResult<TodoItem>>();
    getTodos.mockReturnValueOnce(a).mockReturnValueOnce(b);
    const facade = TestBed.inject(TodoFacade);
    facade.todos.set([todo]);
    const oldLoad = facade.selectList(list);
    const newLoad = facade.selectList({ ...list, id: 6, name: 'İş' });
    expect(a.observed).toBe(false);
    expect(b.observed).toBe(true);
    expect(facade.todos()).toEqual([]);
    expect(facade.isLoadingTodos()).toBe(true);
    await oldLoad;
    const bTodo = { ...todo, id: 10, todoListId: 6, title: 'B görevi' };
    b.next({ ...emptyPage, items: [bTodo], totalCount: 1, totalPages: 1 });
    await newLoad;
    a.next({ ...emptyPage, items: [todo], totalCount: 1, totalPages: 1 });
    expect(facade.todos()).toEqual([bTodo]);
    expect(facade.selectedList()?.id).toBe(6);
    expect(facade.isLoadingTodos()).toBe(false);
  });

  it('continues listening to selections after a request error', async () => {
    getTodos
      .mockReturnValueOnce(throwError(() => new HttpErrorResponse({ status: 503 })))
      .mockReturnValueOnce(of({ ...emptyPage, items: [todo], totalCount: 1, totalPages: 1 }));
    const facade = TestBed.inject(TodoFacade);
    await facade.selectList(list);
    expect(facade.todoLoadState()).toBe('error');
    await facade.selectList(list);
    expect(facade.todos()).toEqual([todo]);
    expect(facade.todoLoadState()).toBe('loaded');
  });

  it('unsubscribes pending reads and settles their promise when the scope is destroyed', async () => {
    const response = new Subject<PagedResult<TodoItem>>();
    getTodos.mockReturnValue(response);
    const facade = TestBed.inject(TodoFacade);
    const pending = facade.loadTodos(5);
    expect(response.observed).toBe(true);
    TestBed.resetTestingModule();
    await pending;
    expect(response.observed).toBe(false);
    await facade.loadTodos(6);
    expect(getTodos).toHaveBeenCalledOnce();
  });

  it('does not apply an old completion response to another selected list', async () => {
    const response = new Subject<TodoItem>();
    setTodoCompletionStatus.mockReturnValue(response);
    const facade = TestBed.inject(TodoFacade);
    facade.selectedList.set(list);
    facade.todos.set([todo]);
    const pending = facade.setTodoCompletion(todo, true);
    const other = { ...todo, id: 10, todoListId: 6 };
    facade.selectedList.set({ ...list, id: 6 });
    facade.todos.set([other]);
    response.next({ ...todo, isCompleted: true });
    await pending;
    expect(facade.todos()).toEqual([other]);
  });

  it('creates a task in the selected list and closes its form only after success', async () => {
    const facade = TestBed.inject(TodoFacade);
    facade.selectedList.set(list);
    facade.isCreatingTodo.set(true);
    facade.todoTotalCount.set(20);
    createTodo.mockReturnValue(of({ ...todo, title: 'Ekmek al' }));
    getTodos.mockReturnValue(of({ ...emptyPage, pageNumber: 2, totalCount: 21, totalPages: 2 }));
    await facade.createTodo(request);
    expect(createTodo).toHaveBeenCalledWith(5, { ...request, title: 'Ekmek al' });
    expect(getTodos).toHaveBeenCalledWith(5, 2, 20);
    expect(facade.isCreatingTodo()).toBe(false);
    expect(facade.isSavingTodo()).toBe(false);
  });

  it('keeps a failed task form open so the component can retain its draft', async () => {
    const facade = TestBed.inject(TodoFacade);
    facade.selectedList.set(list);
    facade.isCreatingTodo.set(true);
    createTodo.mockReturnValue(throwError(() => new HttpErrorResponse({ status: 409 })));
    await facade.createTodo(request);
    expect(facade.isCreatingTodo()).toBe(true);
    expect(facade.isSavingTodo()).toBe(false);
    expect(facade.error()?.message).toBe('Bu listede aynı isimli bir görev zaten var.');
    expect(facade.errorTarget()).toBe('todo-create');
  });

  it('replaces the saved task and ends editing without reloading every record', async () => {
    const facade = TestBed.inject(TodoFacade);
    facade.selectedList.set(list);
    facade.todos.set([todo]);
    facade.editingTodoId.set(todo.id);
    const updated = { ...todo, title: 'Ekmek al', priority: 'High' as const };
    updateTodo.mockReturnValue(of(updated));
    await facade.saveTodo(todo, request);
    expect(updateTodo).toHaveBeenCalledWith(5, 9, { ...request, title: 'Ekmek al' });
    expect(facade.todos()).toEqual([updated]);
    expect(facade.editingTodoId()).toBeNull();
    expect(getTodos).not.toHaveBeenCalled();
  });

  it('updates completion from the row event payload', async () => {
    const facade = TestBed.inject(TodoFacade);
    facade.selectedList.set(list);
    facade.todos.set([todo]);
    setTodoCompletionStatus.mockReturnValue(of({ ...todo, isCompleted: true }));
    await facade.setTodoCompletion(todo, true);
    expect(setTodoCompletionStatus).toHaveBeenCalledWith(5, 9, true);
    expect(facade.todos()[0].isCompleted).toBe(true);
  });

  it('renames both the selected list and its sidebar entry', async () => {
    const facade = TestBed.inject(TodoFacade);
    facade.selectedList.set(list);
    facade.todoLists.set([list]);
    facade.isRenamingList.set(true);
    const renamed = { ...list, name: 'İş' };
    renameTodoList.mockReturnValue(of(renamed));
    await facade.saveListName('  İş  ');
    expect(renameTodoList).toHaveBeenCalledWith(5, { name: 'İş' });
    expect(facade.todoLists()).toEqual([renamed]);
    expect(facade.selectedList()).toEqual(renamed);
    expect(facade.isRenamingList()).toBe(false);
  });

  it('resets the sidebar draft only after successfully creating a list', async () => {
    const facade = TestBed.inject(TodoFacade);
    createTodoList.mockReturnValue(throwError(() => new HttpErrorResponse({ status: 409 })));
    await facade.createList('Ev');
    expect(facade.listCreationVersion()).toBe(0);
    createTodoList.mockReturnValue(of(list));
    getTodoLists.mockReturnValue(of({ ...emptyPage, items: [list], totalCount: 1, totalPages: 1 }));
    await facade.createList('Ev');
    expect(facade.listCreationVersion()).toBe(1);
    expect(facade.selectedList()?.id).toBe(5);
  });

  it('explains a duplicate list name in Turkish', async () => {
    createTodoList.mockReturnValue(
      throwError(
        () =>
          new HttpErrorResponse({
            status: 409,
            error: { title: 'The name is already in use.' },
          }),
      ),
    );
    const facade = TestBed.inject(TodoFacade);
    await facade.loadTodoLists();

    await facade.createList('Ev');

    expect(facade.error()?.message).toBe('Bu isimde bir listen zaten var.');
    expect(showErrorMessage).not.toHaveBeenCalled();
  });

  it('reloads the lists when another tab already changed or deleted the task', async () => {
    deleteTodo.mockReturnValue(
      throwError(
        () =>
          new HttpErrorResponse({
            status: 409,
            error: { type: 'urn:todo:problem:concurrency-conflict' },
          }),
      ),
    );
    const facade = TestBed.inject(TodoFacade);
    await facade.loadTodoLists();
    getTodoLists.mockClear();

    facade.selectedList.set({ id: 5, name: 'Ev', createdAtUtc: '2026-10-06T10:00:00Z' });
    await facade.deleteTodo({
      id: 9,
      todoListId: 5,
      title: 'Süt al',
      description: null,
      isCompleted: false,
      createdAtUtc: '2026-10-06T10:00:00Z',
      dueAtUtc: null,
      priority: 'Normal',
    });

    expect(showWarningMessage).toHaveBeenCalledWith(
      'Bu kayıt başka bir sekmede ya da cihazda değiştirildi veya silindi. Liste yenilendi.',
    );
    expect(showErrorMessage).not.toHaveBeenCalled();
    expect(getTodoLists).toHaveBeenCalledOnce();
  });

  it('moves between list and task pages without loading every record', async () => {
    const list = { id: 1, name: 'İş', createdAtUtc: signedInUser.createdAtUtc };
    const secondList = { ...list, id: 21, name: 'Ev' };
    getTodoLists.mockReturnValue(
      of({ ...emptyPage, items: [list], totalCount: 21, totalPages: 2 }),
    );
    getTodos.mockImplementation((_id: number, pageNumber: number) =>
      of({ ...emptyPage, pageNumber, totalCount: 21, totalPages: 2 }),
    );
    const facade = TestBed.inject(TodoFacade);
    await facade.loadTodoLists();
    await vi.waitFor(() => expect(facade.todoTotalPages()).toBe(2));
    await facade.changeTodoPage(2);
    expect(getTodos).toHaveBeenLastCalledWith(1, 2, 20);
    expect(facade.todoPageNumber()).toBe(2);

    getTodoLists.mockReturnValueOnce(
      of({ ...emptyPage, items: [secondList], totalCount: 21, totalPages: 2, pageNumber: 2 }),
    );
    await facade.changeListPage(2);
    expect(getTodoLists).toHaveBeenLastCalledWith(2, 20);
    expect(getTodos).toHaveBeenLastCalledWith(21, 1, 20);
    expect(facade.selectedList()?.id).toBe(21);
  });

  it('returns to the previous task page when deletion empties the last page', async () => {
    const facade = TestBed.inject(TodoFacade);
    facade.selectedList.set({ id: 5, name: 'Ev', createdAtUtc: signedInUser.createdAtUtc });
    facade.todoPageNumber.set(2);
    deleteTodo.mockReturnValue(of(undefined));
    getTodos
      .mockReturnValueOnce(of({ ...emptyPage, pageNumber: 2, totalCount: 20, totalPages: 1 }))
      .mockReturnValueOnce(of({ ...emptyPage, totalCount: 20, totalPages: 1 }));
    await facade.deleteTodo({
      id: 21,
      todoListId: 5,
      title: 'Son görev',
      description: null,
      isCompleted: false,
      createdAtUtc: signedInUser.createdAtUtc,
      dueAtUtc: null,
      priority: 'Normal',
    });
    expect(getTodos).toHaveBeenNthCalledWith(1, 5, 2, 20);
    expect(getTodos).toHaveBeenNthCalledWith(2, 5, 1, 20);
    expect(facade.todoPageNumber()).toBe(1);
    expect(facade.isLoadingTodos()).toBe(false);
  });

  it('does not replace a newer task page with a slower response', async () => {
    const first = new Subject<PagedResult<TodoItem>>();
    const second = new Subject<PagedResult<TodoItem>>();
    getTodos.mockReturnValueOnce(first).mockReturnValueOnce(second);
    const facade = TestBed.inject(TodoFacade);
    const oldLoad = facade.loadTodos(1, 1);
    const newLoad = facade.loadTodos(1, 2);
    second.next({ ...emptyPage, pageNumber: 2, totalCount: 40, totalPages: 2 });
    await newLoad;
    first.next({ ...emptyPage, pageNumber: 1, totalCount: 40, totalPages: 2 });
    await oldLoad;
    expect(facade.todoPageNumber()).toBe(2);
  });
});
