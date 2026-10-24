export class ApiError extends Error {
  notified = false;
  constructor(
    readonly status: number,
    message: string,
    readonly problemType: string | null = null,
    readonly fieldErrors: Record<string, string[]> = {},
  ) {
    super(message);
    this.name = 'ApiError';
  }
}
