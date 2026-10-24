import { Component, Input } from '@angular/core';
import { AbstractControl } from '@angular/forms';

@Component({
  selector: 'app-form-field-error',
  template: `@if (control?.invalid && (control?.dirty || control?.touched)) {
    <span class="field-error" role="alert">{{ message() }}</span>
  }`,
  styles: [
    ':host { display: block; } .field-error { display: block; color: #b42318; font-size: 12px; margin-top: 4px; }',
  ],
})
export class FormFieldError {
  @Input() control: AbstractControl | null = null;
  @Input() messages: Record<string, string> = {};

  message(): string {
    const errors = this.control?.errors ?? {};
    if (typeof errors['server'] === 'string') return errors['server'];
    const key = Object.keys(errors)[0];
    const defaults: Record<string, string> = {
      required: 'Bu alanı boş bırakma.',
      email: 'Geçerli bir e-posta adresi gir.',
      maxlength: `En fazla ${errors['maxlength']?.requiredLength ?? ''} karakter gir.`,
      date: 'Geçerli bir bitiş tarihi gir.',
      pattern: 'Geçerli bir değer seç.',
    };
    return this.messages[key] ?? defaults[key] ?? 'Bu alanı kontrol et.';
  }
}
