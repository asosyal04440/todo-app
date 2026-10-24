export function toLocalDateTimeValue(utcDateTime: string | null): string {
  if (utcDateTime === null) return '';
  const date = new Date(utcDateTime);
  const localDate = new Date(date.getTime() - date.getTimezoneOffset() * 60_000);
  return localDate.toISOString().slice(0, 16);
}

export function toUtcDateTimeValue(localDateTime: string): string | null {
  return localDateTime.length === 0 ? null : new Date(localDateTime).toISOString();
}
