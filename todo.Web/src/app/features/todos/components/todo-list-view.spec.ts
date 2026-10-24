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
});
