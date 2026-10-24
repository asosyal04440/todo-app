import { Component, Input } from '@angular/core';

export interface ApiErrorView {
  message: string;
  fieldErrors: Record<string, string[]>;
}

@Component({
  selector: 'app-api-errors',
  template: `@if (error) {
    <div class="error-message" role="alert">
      <p>{{ error.message }}</p>
      @for (message of fieldMessages(); track $index) {
        <p>{{ message }}</p>
      }
    </div>
  }`,
  styles: [
    ':host { display: block; } .error-message { color: #b42318; margin: 12px 0; } p { margin: 4px 0; }',
  ],
})
export class ApiErrors {
  @Input() error: ApiErrorView | null = null;
  fieldMessages(): string[] {
    return Object.values(this.error?.fieldErrors ?? {}).flat();
  }
}
