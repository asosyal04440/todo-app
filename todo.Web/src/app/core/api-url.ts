declare global {
  interface Window {
    __TODO_API_URL__?: string;
  }
}

export function resolveApiUrl(): string {
  if (typeof window !== 'undefined' && window.__TODO_API_URL__) {
    return window.__TODO_API_URL__;
  }

  return 'http://localhost:7070/api';
}
