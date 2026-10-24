import { HttpErrorResponse } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { FormGroup } from '@angular/forms';
import { NzMessageService } from 'ng-zorro-antd/message';
import { ApiError } from './api-error';

@Injectable({ providedIn: 'root' })
export class ApiErrorService {
  private readonly messages = inject(NzMessageService);

  normalize(error: unknown): ApiError {
    if (error instanceof ApiError) return error;
    if (!(error instanceof HttpErrorResponse))
      return new ApiError(-1, 'İstek tamamlanamadı. Lütfen tekrar dene.');
    const body: Record<string, unknown> =
      error.error && typeof error.error === 'object' && !Array.isArray(error.error)
        ? error.error
        : {};
    const fieldErrors: Record<string, string[]> = Object.create(null);
    if (body['errors'] && typeof body['errors'] === 'object' && !Array.isArray(body['errors'])) {
      for (const [key, value] of Object.entries(body['errors'])) {
        if (Array.isArray(value)) {
          const messages = value.filter(
            (item): item is string => typeof item === 'string' && item.length > 0,
          );
          if (messages.length) fieldErrors[key] = messages;
        }
      }
    }
    const detail = typeof body['detail'] === 'string' ? body['detail'] : null;
    const title = typeof body['title'] === 'string' ? body['title'] : null;
    const standard: Record<number, string> = {
      0: 'API sunucusuna bağlanılamadı. Bağlantını ve uygulamanın açık olduğunu kontrol et.',
      401: 'Oturumun doğrulanamadı. Tekrar giriş yap.',
      403: 'Bu işlem için yetkin bulunmuyor.',
      404: 'İstenen kayıt bulunamadı.',
      429: 'Çok fazla deneme yapıldı. Bir dakika bekleyip tekrar dene.',
      503: 'Sunucu şu anda veritabanına ulaşamıyor. Biraz sonra tekrar dene.',
    };
    const message =
      standard[error.status] ??
      (error.status >= 500
        ? 'Beklenmeyen bir hata oluştu. Sorun devam ederse yöneticine bildir.'
        : (detail ?? title ?? `İstek tamamlanamadı (${error.status}).`));
    return new ApiError(
      error.status,
      message,
      typeof body['type'] === 'string' ? body['type'] : null,
      fieldErrors,
    );
  }

  report(error: unknown, override?: string): ApiError {
    const normalized = this.normalize(error);
    if (!normalized.notified) {
      this.messages.error(override ?? normalized.message);
      normalized.notified = true;
    }
    return normalized;
  }

  applyFieldErrors(form: FormGroup, error: ApiError, aliases: Record<string, string> = {}): void {
    const controls = Object.keys(form.controls);
    for (const [field, messages] of Object.entries(error.fieldErrors)) {
      const key = field.split('.').at(-1)!.toLowerCase();
      const target = aliases[key] ?? controls.find((name) => name.toLowerCase() === key);
      if (target && controls.includes(target)) {
        const control = form.controls[target];
        control.setErrors({ ...control.errors, server: messages.join(' ') });
        control.markAsTouched();
      }
    }
  }
}
