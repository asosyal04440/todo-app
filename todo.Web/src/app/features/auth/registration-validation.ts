export const AUTH_VALIDATION_LIMITS = {
  emailMaximumLength: 256,
  displayNameMaximumLength: 100,
  passwordMinimumLength: 8,
  passwordMaximumLength: 128,
} as const;

export function isRegistrationPasswordValid(password: string): boolean {
  if (
    password.length < AUTH_VALIDATION_LIMITS.passwordMinimumLength ||
    password.length > AUTH_VALIDATION_LIMITS.passwordMaximumLength
  ) {
    return false;
  }

  const characters = password.split('');
  const hasLetter = characters.some((character) => /\p{L}/u.test(character));
  const hasDigit = characters.some((character) => /\p{Nd}/u.test(character));

  return hasLetter && hasDigit;
}
