import { describe, expect, it } from 'vitest';
import { isRegistrationPasswordValid } from './registration-validation';

describe('registration password policy', () => {
  it.each<[string, boolean]>([
    ['a1aaaaa', false],
    ['a1aaaaaa', true],
    ['a1' + 'a'.repeat(126), true],
    ['a1' + 'a'.repeat(127), false],
    ['', false],
    ['sadeceharf', false],
    ['12345678', false],
    ['Şifre123', true],
    ['Şifre١٢٣', true],
    ['𐐀1234567', false],
  ])('validates %s consistently with the backend: %s', (password, expected) => {
    expect(isRegistrationPasswordValid(password)).toBe(expected);
  });
});
