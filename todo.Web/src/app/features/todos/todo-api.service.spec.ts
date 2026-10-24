import { API_URL } from '../../core/config/api-url.token';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { TodoApiService } from './todo-api.service';

describe('TodoApiService', () => {
  let service: TodoApiService;
  let httpTestingController: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        { provide: API_URL, useValue: 'http://localhost:7070/api' },
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
      'http://localhost:7070/api/todo-lists?pageNumber=1&pageSize=20',
    );

    expect(request.request.method).toBe('GET');

    const page = { items: [], totalCount: 0, pageNumber: 1, pageSize: 20, totalPages: 0 };
    request.flush(page);

    expect(receivedLists).toEqual(page);
  });

  it('sends a POST request with the new list name', () => {
    const newList = { name: 'İş' };

    service.createTodoList(newList).subscribe();

    const request = httpTestingController.expectOne('http://localhost:7070/api/todo-lists');

    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual(newList);

    request.flush({
      id: 1,
      name: 'İş',
      createdAtUtc: '2026-09-26T15:00:00Z',
    });
  });
});

describe('TodoApiService configured environment', () => {
  it('uses the injected API URL rather than a built-in localhost address', () => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: API_URL, useValue: 'https://test-api.example.com/api' },
      ],
    });
    TestBed.inject(TodoApiService).getTodoLists().subscribe();
    const http = TestBed.inject(HttpTestingController);
    http
      .expectOne('https://test-api.example.com/api/todo-lists?pageNumber=1&pageSize=20')
      .flush({ items: [], totalCount: 0, pageNumber: 1, pageSize: 20, totalPages: 0 });
    http.verify();
  });
});
