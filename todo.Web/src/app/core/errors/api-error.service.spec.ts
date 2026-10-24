import { HttpErrorResponse } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { FormControl, FormGroup } from '@angular/forms';
import { NzMessageService } from 'ng-zorro-antd/message';
import { ApiErrorService } from './api-error.service';
import { ApiError } from './api-error';

describe('API error normalization', () => {
  const notify = vi.fn();
  beforeEach(() => {
    notify.mockReset();
    TestBed.configureTestingModule({
      providers: [{ provide: NzMessageService, useValue: { error: notify } }],
    });
  });
  it.each([0, 401, 403, 404, 429, 500, 503])(
    'uses a safe common message for status %s',
    (status) => {
      const error = TestBed.inject(ApiErrorService).normalize(
        new HttpErrorResponse({ status, error: { detail: 'raw technical detail' } }),
      );
      expect(error.status).toBe(status);
      expect(error.message).not.toContain('raw technical detail');
      expect(error.message.length).toBeGreaterThan(10);
    },
  );
  it('keeps validation arrays and ignores malformed ProblemDetails values', () => {
    const error = TestBed.inject(ApiErrorService).normalize(
      new HttpErrorResponse({
        status: 400,
        error: {
          detail: 10,
          title: {},
          errors: { Title: ['Başlık geçersiz', 1], Email: 'wrong shape', Name: [] },
        },
      }),
    );
    expect(error.fieldErrors['Title']).toEqual(['Başlık geçersiz']);
    expect(error.fieldErrors['Email']).toBeUndefined();
    expect(error.message).toContain('400');
  });
  it('maps case-insensitive field names and aliases, and drops server errors on value edits', () => {
    const form = new FormGroup({ title: new FormControl('old'), dueAtLocal: new FormControl('') });
    const error = new ApiError(400, 'invalid', null, {
      Title: ['Geçersiz başlık'],
      DueAtUtc: ['Geçersiz tarih'],
      Unknown: ['Diğer hata'],
    });
    TestBed.inject(ApiErrorService).applyFieldErrors(form, error, { dueatutc: 'dueAtLocal' });
    expect(form.controls.title.errors?.['server']).toBe('Geçersiz başlık');
    expect(form.controls.title.touched).toBe(true);
    expect(form.controls.dueAtLocal.errors?.['server']).toBe('Geçersiz tarih');
    form.controls.title.setValue('new');
    expect(form.controls.title.errors).toBeNull();
  });
  it('reports the same normalized failure once', () => {
    const service = TestBed.inject(ApiErrorService);
    const error = service.normalize(new HttpErrorResponse({ status: 503 }));
    service.report(error);
    service.report(error);
    expect(notify).toHaveBeenCalledOnce();
  });
});
