import { ValidatorFn } from '@angular/forms';
import { validLocalDate } from '../../shared/forms/form-validators';

export const TODO_VALIDATION_LIMITS = {
  todoListNameMaximumLength: 80,
  todoTitleMaximumLength: 120,
  todoDescriptionMaximumLength: 1000,
} as const;

// Keep these aligned with Application/Common/Validation/SharedValidationRules.cs.
export const TODO_DUE_DATE_CLOCK_TOLERANCE_MS = 5 * 60_000;
const TODO_DUE_DATE_MAXIMUM_YEARS_AHEAD = 10;

export function todoDueDateValidator(isCreating: () => boolean): ValidatorFn {
  return (control) => {
    const dateError = validLocalDate(control);
    if (dateError || !control.value) return dateError;

    const dueAt = new Date(control.value).getTime();
    const now = Date.now();
    if (isCreating() && dueAt < now - TODO_DUE_DATE_CLOCK_TOLERANCE_MS) return { pastDate: true };

    const maximum = new Date(now);
    const month = maximum.getUTCMonth();
    maximum.setUTCFullYear(maximum.getUTCFullYear() + TODO_DUE_DATE_MAXIMUM_YEARS_AHEAD);
    // Match DateTime.AddYears: February 29 becomes February 28, not March 1.
    if (maximum.getUTCMonth() !== month) maximum.setUTCDate(0);
    return dueAt > maximum.getTime() ? { futureDate: true } : null;
  };
}
