export type TodoPriority = 'Low' | 'Normal' | 'High';

export interface TodoList {
  id: number;
  name: string;
  createdAtUtc: string;
}

export interface TodoItem {
  id: number;
  todoListId: number;
  title: string;
  description: string | null;
  isCompleted: boolean;
  createdAtUtc: string;
  dueAtUtc: string | null;
  priority: TodoPriority;
}

export interface TodoListRequest {
  name: string;
}

export interface TodoRequest {
  title: string;
  description: string | null;
  dueAtUtc: string | null;
  priority: TodoPriority;
}
