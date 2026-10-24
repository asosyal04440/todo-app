import { HttpErrorResponse } from '@angular/common/http';
import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { NzMessageService } from 'ng-zorro-antd/message';
import { of, Subject, throwError } from 'rxjs';
import { PagedResult } from '../../shared/pagination/paged-result';
import { TodoItem } from './todo.models';
import { CurrentUser } from '../../core/auth/auth.models';
import { AuthService } from '../../core/auth/auth.service';
import { TodoApiService } from './todo-api.service';
import { TodoPage } from './todo-page';

describe('TodoPage', () => {
  const signedInUser: CurrentUser = {
    id: 1,
    email: 'ali@example.com',
    displayName: 'Ali',
    role: 'User',
    hasPassword: true,
    hasGoogleLogin: false,
    createdAtUtc: '2026-10-06T10:00:00Z',
  };
  const isAdmin = signal(false);
  const logout = vi.fn();
  const showErrorMessage = vi.fn();
  const showWarningMessage = vi.fn();
  const createTodoList = vi.fn();
  const deleteTodo = vi.fn();
  const getTodoLists = vi.fn();
  const getTodos = vi.fn();
  const emptyPage = { items: [], totalCount: 0, pageNumber: 1, pageSize: 20, totalPages: 0 };

  beforeEach(async () => {
    isAdmin.set(false);
    logout.mockClear();
    showErrorMessage.mockClear();
    showWarningMessage.mockClear();
    createTodoList.mockReset();
    deleteTodo.mockReset();
    getTodoLists.mockReset();
    getTodoLists.mockReturnValue(of(emptyPage));
    getTodos.mockReset();
    getTodos.mockReturnValue(of(emptyPage));

    await TestBed.configureTestingModule({
      imports: [TodoPage],
      providers: [
        provideRouter([]),
        {
          provide: TodoApiService,
          useValue: {
            getTodoLists,
            getTodos,
            createTodoList,
            deleteTodo,
          },
        },
        {
          provide: AuthService,
          useValue: {
            currentUser: signal(signedInUser),
            isAdmin,
            logout,
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

  async function renderPage(): Promise<HTMLElement> {
    const fixture = TestBed.createComponent(TodoPage);

    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    return fixture.nativeElement as HTMLElement;
  }

  it('renders the page title and the empty list message', async () => {
    const page = await renderPage();
    const connectionLabel = page.querySelector('.sidebar-footer span:nth-of-type(2)');
    const connectionState = page.querySelector('.connection-state');

    expect(page.querySelector('h1')?.textContent).toContain('Görevlerin');
    expect(page.textContent).toContain('Henüz liste yok');
    expect(connectionLabel?.textContent).toBe('API');
    expect(connectionState?.textContent).toBe('bağlı');
  });

  it('shows the signed-in user and logs out from the account menu', async () => {
    const page = await renderPage();
    const logoutButton = Array.from(page.querySelectorAll('button')).find(
      (button) => button.textContent?.trim() === 'Çıkış yap',
    );

    expect(page.querySelector('.account-name')?.textContent).toBe('Ali');
    expect(page.textContent).not.toContain('Kullanıcılar');

    logoutButton?.click();

    expect(logout).toHaveBeenCalledOnce();
  });

  it('shows the user management link only to admins', async () => {
    isAdmin.set(true);

    const page = await renderPage();
    const adminLink = page.querySelector('a[href="/admin/users"]');

    expect(adminLink?.textContent?.trim()).toBe('Kullanıcılar');
  });

  it('uses a separate facade for each mounted page', () => {
    const first = TestBed.createComponent(TodoPage);
    const second = TestBed.createComponent(TodoPage);
    first.componentInstance.facade.selectedList.set({
      id: 1,
      name: 'İş',
      createdAtUtc: signedInUser.createdAtUtc,
    });
    expect(second.componentInstance.facade.selectedList()).toBeNull();
    expect(first.componentInstance.facade).not.toBe(second.componentInstance.facade);
  });

  it('renders asynchronous facade results without a manual detectChanges call', async () => {
    const response = new Subject<PagedResult<TodoItem>>();
    getTodoLists.mockReturnValue(
      of({
        ...emptyPage,
        items: [{ id: 5, name: 'Ev', createdAtUtc: signedInUser.createdAtUtc }],
        totalCount: 1,
        totalPages: 1,
      }),
    );
    getTodos.mockReturnValue(response);
    const fixture = TestBed.createComponent(TodoPage);
    fixture.detectChanges();
    await vi.waitFor(() => expect(getTodos).toHaveBeenCalledOnce());
    response.next({
      ...emptyPage,
      items: [
        {
          id: 9,
          todoListId: 5,
          title: 'Süt al',
          description: null,
          priority: 'Normal',
          dueAtUtc: null,
          isCompleted: false,
          createdAtUtc: signedInUser.createdAtUtc,
        },
      ],
      totalCount: 1,
      totalPages: 1,
    });
    await fixture.whenStable();
    await vi.waitFor(() =>
      expect(fixture.nativeElement.querySelector('.todo-copy h3')?.textContent).toBe('Süt al'),
    );
  });
});
