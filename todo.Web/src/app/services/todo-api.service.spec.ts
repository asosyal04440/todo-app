import { provideHttpClient } from '@angular/common/http';
import {
  HttpTestingController,
  provideHttpClientTesting,
} from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { TodoApiService } from './todo-api.service';

describe('TodoApiService', () => {
  let service: TodoApiService;
  let httpTestingController: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
      ],
    });

    service = TestBed.inject(TodoApiService);
    httpTestingController = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpTestingController.verify();
  });

  it('sends a GET request to retrieve the todo lists', () => {
    let receivedLists: unknown;

    service.getTodoLists().subscribe((lists) => {
      receivedLists = lists;
    });

    const request = httpTestingController.expectOne(
      'http://localhost:7070/api/todo-lists',
    );

    expect(request.request.method).toBe('GET');

    request.flush([]);

    expect(receivedLists).toEqual([]);
  });

  it('sends a POST request with the new list name', () => {
    const newList = { name: 'İş' };

    service.createTodoList(newList).subscribe();

    const request = httpTestingController.expectOne(
      'http://localhost:7070/api/todo-lists',
    );

    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual(newList);

    request.flush({
      id: 1,
      name: 'İş',
      createdAtUtc: '2026-09-26T15:00:00Z',
    });
  });
});
