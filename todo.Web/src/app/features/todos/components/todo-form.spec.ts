import { TestBed } from '@angular/core/testing';
import { ApiErrorService } from '../../../core/errors/api-error.service';
import { toLocalDateTimeValue } from '../todo-date-time';
import { TodoForm } from './todo-form';

describe('TodoForm due dates', () => {
  const now = Date.parse('2026-10-24T12:00:00Z');
  let component: TodoForm;

  beforeEach(() => {
    vi.spyOn(Date, 'now').mockReturnValue(now);
    TestBed.configureTestingModule({
      providers: [{ provide: ApiErrorService, useValue: { applyFieldErrors: vi.fn() } }],
    });
    component = TestBed.runInInjectionContext(() => new TodoForm());
    component.form.controls.title.setValue('  Tarih kontrolü  ');
    vi.spyOn(component.submitted, 'emit');
  });

  afterEach(() => vi.restoreAllMocks());

  function setDueAt(utc: string): void {
    component.form.controls.dueAtLocal.setValue(toLocalDateTimeValue(utc));
  }

  it('blocks a new task with a past date before emitting an API request', () => {
    setDueAt('2026-10-23T12:00:00Z');
    component.submit();
    expect(component.submitted.emit).not.toHaveBeenCalled();
    expect(component.form.controls.dueAtLocal.hasError('pastDate')).toBe(true);
    expect(component.form.controls.dueAtLocal.touched).toBe(true);
  });

  it('blocks a date more than ten years ahead', () => {
    setDueAt('2037-10-24T12:00:00Z');
    component.submit();
    expect(component.submitted.emit).not.toHaveBeenCalled();
    expect(component.form.controls.dueAtLocal.hasError('futureDate')).toBe(true);
  });

  it('allows a task without a due date', () => {
    component.submit();
    expect(component.submitted.emit).toHaveBeenCalledWith({
      title: 'Tarih kontrolü',
      description: null,
      dueAtUtc: null,
      priority: 'Normal',
    });
  });

  it('allows editing an existing overdue task without changing its date', () => {
    component.todo = {
      id: 1,
      todoListId: 1,
      title: 'Geciken görev',
      description: null,
      isCompleted: false,
      createdAtUtc: '2026-10-01T12:00:00Z',
      dueAtUtc: '2026-10-23T12:00:00Z',
      priority: 'Normal',
    };
    setDueAt(component.todo.dueAtUtc!);
    component.submit();
    expect(component.submitted.emit).toHaveBeenCalled();
    expect(component.minimumDueAtLocal()).toBe('');
  });

  it('allows the same five-minute clock tolerance as the API', () => {
    setDueAt('2026-10-24T11:57:00Z');
    component.submit();
    expect(component.submitted.emit).toHaveBeenCalled();
  });

  it('rechecks a date on submission after the form has been left open', () => {
    setDueAt('2026-10-24T12:00:00Z');
    expect(component.form.valid).toBe(true);
    vi.mocked(Date.now).mockReturnValue(now + 6 * 60_000);
    component.submit();
    expect(component.submitted.emit).not.toHaveBeenCalled();
    expect(component.form.controls.dueAtLocal.hasError('pastDate')).toBe(true);
  });

  it('can submit after correcting an invalid date', () => {
    setDueAt('2026-10-23T12:00:00Z');
    component.submit();
    setDueAt('2026-10-25T12:00:00Z');
    component.submit();
    expect(component.submitted.emit).toHaveBeenCalledTimes(1);
    expect(component.submitted.emit).toHaveBeenCalledWith(
      expect.objectContaining({
        dueAtUtc: '2026-10-25T12:00:00.000Z',
      }),
    );
  });

  it('uses the API calendar boundary when ten years ends in a non-leap year', () => {
    vi.mocked(Date.now).mockReturnValue(Date.parse('2028-02-29T12:00:00Z'));
    setDueAt('2038-02-28T12:01:00Z');
    component.submit();
    expect(component.submitted.emit).not.toHaveBeenCalled();
    expect(component.form.controls.dueAtLocal.hasError('futureDate')).toBe(true);
  });
});
