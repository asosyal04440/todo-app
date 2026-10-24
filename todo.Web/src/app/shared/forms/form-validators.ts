import { AbstractControl, ValidationErrors, ValidatorFn } from '@angular/forms';

export const requiredTrimmed: ValidatorFn = (control: AbstractControl): ValidationErrors | null =>
  typeof control.value === 'string' && control.value.trim().length > 0 ? null : { required: true };

export const validLocalDate: ValidatorFn = (control: AbstractControl): ValidationErrors | null =>
  !control.value || !Number.isNaN(new Date(control.value).getTime()) ? null : { date: true };
