import { TestBed } from '@angular/core/testing';
import { TodoListView } from './todo-list-view';
import { TodoItem } from '../todo.models';

describe('TodoListView derived state', () => {
  it('recomputes completed count when task inputs change', () => {
    const fixture = TestBed.createComponent(TodoListView);
    const todo: TodoItem = {
      id: 1,
      todoListId: 1,
      title: 'Rapor',
      description: null,
      isCompleted: false,
      priority: 'Normal',
      dueAtUtc: null,
      createdAtUtc: '2026-10-13T08:00:00Z',
    };
    fixture.componentRef.setInput('todos', [todo]);
    fixture.detectChanges();
    expect(fixture.componentInstance.completedCount()).toBe(0);
    fixture.componentRef.setInput('todos', [{ ...todo, isCompleted: true }]);
    fixture.detectChanges();
    expect(fixture.componentInstance.completedCount()).toBe(1);
    expect(fixture.nativeElement.querySelector('.task-total').textContent).toContain('1 / 1');
  });

  it('disables only the pending task and restores its controls after completion', () => {
    const fixture = TestBed.createComponent(TodoListView);
    const todo: TodoItem = {
      id: 1,
      todoListId: 1,
      title: 'Rapor',
      description: null,
      isCompleted: false,
      priority: 'Normal',
      dueAtUtc: null,
      createdAtUtc: '2026-10-13T08:00:00Z',
    };
    fixture.componentRef.setInput('todos', [todo, { ...todo, id: 2, title: 'Toplantı' }]);
    fixture.componentRef.setInput('completingIds', new Set([todo.id]));
    fixture.detectChanges();
    const rows = fixture.nativeElement.querySelectorAll('app-todo-row') as NodeListOf<HTMLElement>;
    const first = rows[0].querySelector('input[type="checkbox"]') as HTMLInputElement;
    const second = rows[1].querySelector('input[type="checkbox"]') as HTMLInputElement;
    const completed = vi.fn();
    fixture.componentInstance.completionRequested.subscribe(completed);

    expect(first.disabled).toBe(true);
    expect(second.disabled).toBe(false);
    expect([...rows[0].querySelectorAll('button')].every((button) => button.disabled)).toBe(true);
    expect([...rows[1].querySelectorAll('button')].every((button) => !button.disabled)).toBe(true);
    first.dispatchEvent(new Event('change'));
    expect(completed).not.toHaveBeenCalled();

    fixture.componentRef.setInput('completingIds', new Set());
    fixture.detectChanges();
    expect(first.disabled).toBe(false);
    expect([...rows[0].querySelectorAll('button')].every((button) => !button.disabled)).toBe(true);
  });
});
