import { type Page } from '@playwright/test';
import { apiUrl, expect, test } from './fixtures';

interface TodoListResponse {
  id: number;
  name: string;
}

interface TodoResponse {
  id: number;
  todoListId: number;
  title: string;
  description: string | null;
  dueAtUtc: string | null;
  priority: 'Low' | 'Normal' | 'High';
  isCompleted: boolean;
}

let createdListId: number | undefined;

test.beforeEach(async ({ page, session }) => {
  createdListId = undefined;

  expect(session.accessToken).toBeTruthy();

  await page.goto('/');
  await expect(page.locator('.connection-state')).toHaveText('bağlı');
  await expect(page.locator('.muted-message').filter({ hasText: 'Henüz liste yok' })).toBeVisible();
});

test.afterEach(async ({ request }) => {
  if (createdListId === undefined) {
    return;
  }

  const response = await request.delete(`${apiUrl}/todo-lists/${createdListId}`);
  expect([204, 404]).toContain(response.status());
});

test('a task can be created, loaded again, edited, completed, and deleted through the UI', async ({
  page,
  request,
}) => {
  const list = await createList(page, `E2E görev akışı ${Date.now()}`);
  createdListId = list.id;

  const getListResponse = await request.get(`${apiUrl}/todo-lists/${list.id}`);
  expect(getListResponse.status()).toBe(200);
  expect((await getListResponse.json()).name).toBe(list.name);

  const originalTitle = `E2E görev ${Date.now()}`;
  const originalDescription = 'Bu görev tarayıcıdan API ve SQL Server üzerinden kaydedildi.';
  const originalDueAt = toLocalDateTimeInput(new Date(Date.now() + 24 * 60 * 60 * 1000));

  await page.getByRole('button', { name: /Yeni görev/ }).click();
  await page.getByLabel('Görev adı').fill(originalTitle);
  await page.getByLabel('Açıklama').fill(originalDescription);
  await page.getByLabel('Bitiş tarihi').fill(originalDueAt);

  const createTodoResponsePromise = page.waitForResponse((response) =>
    response.url() === `${apiUrl}/todo-lists/${list.id}/todos` &&
    response.request().method() === 'POST',
  );

  await page.getByRole('button', { name: 'Görevi oluştur' }).click();

  const createTodoResponse = await createTodoResponsePromise;
  expect(createTodoResponse.status()).toBe(201);

  const createdTodo = (await createTodoResponse.json()) as TodoResponse;
  expect(createdTodo.title).toBe(originalTitle);
  expect(createdTodo.description).toBe(originalDescription);
  expect(createdTodo.dueAtUtc).not.toBeNull();
  expect(createdTodo.todoListId).toBe(list.id);
  expect(createdTodo.priority).toBe('Normal');
  expect(createdTodo.isCompleted).toBe(false);

  let taskCard = page.locator('.todo-card').filter({ hasText: originalTitle });
  await expect(taskCard).toContainText(originalDescription);

  const getTodoResponse = await request.get(
    `${apiUrl}/todo-lists/${list.id}/todos/${createdTodo.id}`,
  );
  expect(getTodoResponse.status()).toBe(200);
  expect((await getTodoResponse.json()).id).toBe(createdTodo.id);

  await page.reload();
  const listButton = page.locator('.list-navigation-item').filter({ hasText: list.name });
  await listButton.click();
  taskCard = page.locator('.todo-card').filter({ hasText: originalTitle });
  await expect(taskCard).toContainText(originalDescription);

  await taskCard.getByRole('button', { name: 'Düzenle' }).click();

  const updatedTitle = `${originalTitle} güncellendi`;
  const updatedDescription = 'Düzenlenen görev SQL Server üzerinden tekrar yüklendi.';
  const updatedDueAt = toLocalDateTimeInput(new Date(Date.now() + 2 * 24 * 60 * 60 * 1000));
  const editCard = page.locator('.todo-edit-card');

  await editCard.getByRole('textbox', { name: 'Görev adı' }).fill(updatedTitle);
  await editCard.getByRole('textbox', { name: 'Açıklama' }).fill(updatedDescription);
  await editCard.getByRole('textbox', { name: 'Bitiş tarihi' }).fill(updatedDueAt);
  const priorityInput = editCard.locator('.ant-select-selection-search-input');
  await priorityInput.click({ force: true });
  await priorityInput.press('ArrowDown');
  const highPriorityOption = page.getByText('Yüksek', { exact: true }).last();
  await expect(highPriorityOption).toBeVisible();
  await highPriorityOption.click();

  const updateTodoResponsePromise = page.waitForResponse((response) =>
    response.url() === `${apiUrl}/todo-lists/${list.id}/todos/${createdTodo.id}` &&
    response.request().method() === 'PUT',
  );

  await editCard.getByRole('button', { name: 'Değişiklikleri kaydet' }).click();

  const updateTodoResponse = await updateTodoResponsePromise;
  expect(updateTodoResponse.status()).toBe(200);

  const updatedTodo = (await updateTodoResponse.json()) as TodoResponse;
  expect(updatedTodo.title).toBe(updatedTitle);
  expect(updatedTodo.description).toBe(updatedDescription);
  expect(updatedTodo.dueAtUtc).not.toBeNull();
  expect(updatedTodo.priority).toBe('High');

  taskCard = page.locator('.todo-card').filter({ hasText: updatedTitle });
  await expect(taskCard).toContainText('Yüksek öncelik');

  const completionResponsePromise = page.waitForResponse((response) =>
    response.url() === `${apiUrl}/todo-lists/${list.id}/todos/${createdTodo.id}/completion` &&
    response.request().method() === 'PATCH',
  );

  await taskCard.locator('.completion-control').click();
  await expect(taskCard.getByRole('checkbox', { name: `${updatedTitle} tamamlandı` })).toBeChecked();

  const completionResponse = await completionResponsePromise;
  expect(completionResponse.status()).toBe(200);
  expect((await completionResponse.json()).isCompleted).toBe(true);

  const persistedTodoResponse = await request.get(
    `${apiUrl}/todo-lists/${list.id}/todos/${createdTodo.id}`,
  );
  expect((await persistedTodoResponse.json()).isCompleted).toBe(true);

  const incompleteResponsePromise = page.waitForResponse((response) =>
    response.url() === `${apiUrl}/todo-lists/${list.id}/todos/${createdTodo.id}/completion` &&
    response.request().method() === 'PATCH',
  );

  await taskCard.locator('.completion-control').click();
  await expect(taskCard.getByRole('checkbox', { name: `${updatedTitle} tamamlandı` })).not.toBeChecked();

  const incompleteResponse = await incompleteResponsePromise;
  expect(incompleteResponse.status()).toBe(200);
  expect((await incompleteResponse.json()).isCompleted).toBe(false);

  const incompleteTodoResponse = await request.get(
    `${apiUrl}/todo-lists/${list.id}/todos/${createdTodo.id}`,
  );
  expect((await incompleteTodoResponse.json()).isCompleted).toBe(false);

  const deleteTodoResponsePromise = page.waitForResponse((response) =>
    response.url() === `${apiUrl}/todo-lists/${list.id}/todos/${createdTodo.id}` &&
    response.request().method() === 'DELETE',
  );

  await taskCard.getByRole('button', { name: 'Sil' }).click();
  await expect(page.getByText('Bu görev silinsin mi?')).toBeVisible();
  await confirmPopconfirm(page);

  const deleteTodoResponse = await deleteTodoResponsePromise;
  expect(deleteTodoResponse.status()).toBe(204);
  await expect(page.getByText('Bu listede henüz görev yok')).toBeVisible();

  const deletedTodoResponse = await request.get(
    `${apiUrl}/todo-lists/${list.id}/todos/${createdTodo.id}`,
  );
  expect(deletedTodoResponse.status()).toBe(404);
});

test('a list can be renamed and deleting it removes its tasks', async ({ page, request }) => {
  const list = await createList(page, `E2E liste akışı ${Date.now()}`);
  createdListId = list.id;

  await page.getByRole('button', { name: 'Yeniden adlandır' }).click();

  const renamedListName = `${list.name} yeniden adlandırıldı`;
  const renamedListInput = page.locator('input[name="renamedListName"]');
  await renamedListInput.fill(renamedListName);
  await expect(renamedListInput).toHaveValue(renamedListName);

  const renameListResponsePromise = page.waitForResponse((response) =>
    response.url() === `${apiUrl}/todo-lists/${list.id}` &&
    response.request().method() === 'PUT',
  );

  await page.getByRole('button', { name: 'Kaydet' }).click();

  const renameListResponse = await renameListResponsePromise;
  expect(renameListResponse.status()).toBe(200);
  expect(renameListResponse.request().postDataJSON()).toEqual({ name: renamedListName });
  expect((await renameListResponse.json()).name).toBe(renamedListName);
  const renamedListButton = page
    .locator('.list-navigation-item')
    .filter({ hasText: renamedListName });
  await expect(renamedListButton).toBeVisible();

  const todo = await createTodo(page, list.id, `Silme testi görevi ${Date.now()}`);
  expect(todo.todoListId).toBe(list.id);

  await page.getByRole('button', { name: 'Listeyi sil' }).click();
  await expect(page.getByText('Bu liste ve içindeki bütün görevler silinsin mi?')).toBeVisible();

  const deleteListResponsePromise = page.waitForResponse((response) =>
    response.url() === `${apiUrl}/todo-lists/${list.id}` &&
    response.request().method() === 'DELETE',
  );

  await confirmPopconfirm(page);

  const deleteListResponse = await deleteListResponsePromise;
  expect(deleteListResponse.status()).toBe(204);
  await expect(renamedListButton).toHaveCount(0);

  const deletedListResponse = await request.get(`${apiUrl}/todo-lists/${list.id}`);
  expect(deletedListResponse.status()).toBe(404);

  const deletedListTodosResponse = await request.get(`${apiUrl}/todo-lists/${list.id}/todos`);
  expect(deletedListTodosResponse.status()).toBe(404);

  createdListId = undefined;
});

test('the UI and real API reject invalid data and missing resources', async ({ page, request }) => {
  await page.getByLabel('Yeni liste adı').fill('   ');
  await page.getByRole('button', { name: 'Liste oluştur' }).click();
  await expect(page.getByText('Liste adı boş olamaz.')).toBeVisible();

  const invalidListResponse = await request.post(`${apiUrl}/todo-lists`, {
    data: { name: '   ' },
  });

  expect(invalidListResponse.status()).toBe(400);
  const problem = (await invalidListResponse.json()) as {
    errors?: Record<string, string[]>;
  };
  expect(problem.errors).toBeDefined();
  expect(Object.keys(problem.errors ?? {}).some((field) => field.toLowerCase() === 'name')).toBe(
    true,
  );

  const createListResponse = await request.post(`${apiUrl}/todo-lists`, {
    data: { name: `E2E domain exception ${Date.now()}` },
  });
  expect(createListResponse.status()).toBe(201);
  const createdList = (await createListResponse.json()) as TodoListResponse;
  createdListId = createdList.id;

  const invalidDueDateResponse = await request.post(
    `${apiUrl}/todo-lists/${createdList.id}/todos`,
    {
      data: {
        title: 'UTC doğrulama testi',
        description: null,
        dueAtUtc: '2030-01-01T12:00:00',
        priority: 'Normal',
      },
    },
  );

  expect(invalidDueDateResponse.status()).toBe(400);
  const domainProblem = (await invalidDueDateResponse.json()) as {
    title: string;
    detail: string;
  };
  expect(domainProblem.title).toBe('The request violates a domain rule.');
  expect(domainProblem.detail).toContain('due date must be expressed in UTC');

  const missingListResponse = await request.get(`${apiUrl}/todo-lists/2147483647`);
  expect(missingListResponse.status()).toBe(404);
});

test('duplicate names are rejected in the UI and by the API', async ({ page, request }) => {
  const list = await createList(page, `E2E tekrar ${Date.now()}`);
  const firstTodo = await createTodo(page, list.id, 'Raporu teslim et');
  expect(firstTodo.title).toBe('Raporu teslim et');

  await page.getByRole('button', { name: /Yeni görev/ }).click();
  await page.getByLabel('Görev adı').fill('  RAPORU teslim et ');
  const duplicateTodoResponsePromise = page.waitForResponse((response) =>
    response.url() === `${apiUrl}/todo-lists/${list.id}/todos` &&
    response.request().method() === 'POST',
  );
  await page.getByRole('button', { name: 'Görevi oluştur' }).click();
  expect((await duplicateTodoResponsePromise).status()).toBe(409);
  await expect(page.getByText('Bu listede aynı isimli bir görev zaten var.')).toBeVisible();

  const duplicateListResponse = await request.post(`${apiUrl}/todo-lists`, {
    data: { name: list.name.toUpperCase() },
  });
  expect(duplicateListResponse.status()).toBe(409);
  const duplicateProblem = (await duplicateListResponse.json()) as { title: string; traceId: string };
  expect(duplicateProblem.title).toBe('The name is already in use.');
  expect(duplicateProblem.traceId).toBeTruthy();

  const invalidTodoResponse = await request.post(`${apiUrl}/todo-lists/${list.id}/todos`, {
    data: { title: '', description: 'a'.repeat(1001), priority: 'Normal' },
  });
  expect(invalidTodoResponse.status()).toBe(400);
  const validationProblem = (await invalidTodoResponse.json()) as {
    errors: Record<string, string[]>;
  };
  expect(Object.keys(validationProblem.errors).sort()).toEqual(['Description', 'Title']);
  expect(validationProblem.errors['Title']?.[0]).toContain('Görev adı');
});

async function createList(page: Page, name: string): Promise<TodoListResponse> {
  await page.getByLabel('Yeni liste adı').fill(name);

  const responsePromise = page.waitForResponse((response) =>
    response.url() === `${apiUrl}/todo-lists` && response.request().method() === 'POST',
  );

  await page.getByRole('button', { name: 'Liste oluştur' }).click();

  const response = await responsePromise;
  expect(response.status()).toBe(201);

  const list = (await response.json()) as TodoListResponse;
  createdListId = list.id;
  await expect(page.locator('.list-navigation-item').filter({ hasText: name })).toBeVisible();

  return list;
}

async function createTodo(page: Page, listId: number, title: string): Promise<TodoResponse> {
  await page.getByRole('button', { name: /Yeni görev/ }).click();
  await page.getByLabel('Görev adı').fill(title);
  await page.getByLabel('Açıklama').fill('Silme zincirini doğrulamak için oluşturuldu.');

  const responsePromise = page.waitForResponse((response) =>
    response.url() === `${apiUrl}/todo-lists/${listId}/todos` &&
    response.request().method() === 'POST',
  );

  await page.getByRole('button', { name: 'Görevi oluştur' }).click();

  const response = await responsePromise;
  expect(response.status()).toBe(201);

  return (await response.json()) as TodoResponse;
}

async function confirmPopconfirm(page: Page): Promise<void> {
  const confirmButton = page.locator('.ant-popover-buttons button.ant-btn-primary').last();
  await expect(confirmButton).toBeVisible();
  await confirmButton.click();
}

function toLocalDateTimeInput(date: Date): string {
  const localDate = new Date(date.getTime() - date.getTimezoneOffset() * 60_000);
  return localDate.toISOString().slice(0, 16);
}
