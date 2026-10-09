import { HttpErrorResponse } from '@angular/common/http';
import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { NzMessageService } from 'ng-zorro-antd/message';
import { of, throwError } from 'rxjs';
import { CurrentUser } from '../../models/auth.models';
import { AuthService } from '../../services/auth.service';
import { TodoApiService } from '../../services/todo-api.service';
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

  beforeEach(async () => {
    isAdmin.set(false);
    logout.mockClear();
    showErrorMessage.mockClear();
    showWarningMessage.mockClear();
    createTodoList.mockReset();
    deleteTodo.mockReset();
    getTodoLists.mockReset();
    getTodoLists.mockReturnValue(of([]));

    await TestBed.configureTestingModule({
      imports: [TodoPage],
      providers: [
        provideRouter([]),
        {
          provide: TodoApiService,
          useValue: {
            getTodoLists,
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
    const fixture = TestBed.createComponent(TodoPage);
    fixture.detectChanges();
    await fixture.whenStable();

    fixture.componentInstance.newListName = 'Ev';
    await fixture.componentInstance.createList();

    expect(showErrorMessage).toHaveBeenCalledWith('Bu isimde bir listen zaten var.');
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
    const fixture = TestBed.createComponent(TodoPage);
    fixture.detectChanges();
    await fixture.whenStable();
    getTodoLists.mockClear();

    fixture.componentInstance.selectedList = { id: 5, name: 'Ev', createdAtUtc: '2026-10-06T10:00:00Z' };
    await fixture.componentInstance.deleteTodo({
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
});
