import { Injectable } from '@angular/core';

interface GoogleCredentialResponse {
  credential: string;
}

interface GoogleIdentityApi {
  accounts: {
    id: {
      initialize(options: {
        client_id: string;
        callback: (response: GoogleCredentialResponse) => void;
      }): void;
      renderButton(
        parent: HTMLElement,
        options: {
          type?: 'standard' | 'icon';
          theme?: 'outline' | 'filled_blue' | 'filled_black';
          size?: 'large' | 'medium' | 'small';
          text?: 'signin_with' | 'signup_with' | 'continue_with';
          shape?: 'rectangular' | 'pill';
          width?: number;
          locale?: string;
        },
      ): void;
    };
  };
}

declare global {
  interface Window {
    google?: GoogleIdentityApi;
  }
}

const GOOGLE_SCRIPT_URL = 'https://accounts.google.com/gsi/client';

@Injectable({
  providedIn: 'root',
})
export class GoogleIdentityService {
  private scriptLoading: Promise<GoogleIdentityApi> | null = null;

  async renderButton(
    container: HTMLElement,
    clientId: string,
    onCredential: (idToken: string) => void,
  ): Promise<void> {
    const google = await this.loadScript();

    google.accounts.id.initialize({
      client_id: clientId,
      callback: (response) => onCredential(response.credential),
    });

    google.accounts.id.renderButton(container, {
      type: 'standard',
      theme: 'outline',
      size: 'large',
      text: 'continue_with',
      shape: 'rectangular',
      width: Math.min(container.clientWidth || 320, 400),
      locale: 'tr',
    });
  }

  private loadScript(): Promise<GoogleIdentityApi> {
    if (this.scriptLoading !== null) {
      return this.scriptLoading;
    }

    this.scriptLoading = new Promise<GoogleIdentityApi>((resolve, reject) => {
      if (window.google !== undefined) {
        resolve(window.google);
        return;
      }

      const script = document.createElement('script');
      script.src = GOOGLE_SCRIPT_URL;
      script.async = true;
      script.defer = true;
      script.onload = () => {
        if (window.google !== undefined) {
          resolve(window.google);
        } else {
          reject(new Error('Google Identity Services yüklenemedi.'));
        }
      };
      script.onerror = () => {
        this.scriptLoading = null;
        reject(new Error('Google Identity Services yüklenemedi.'));
      };

      document.head.appendChild(script);
    });

    return this.scriptLoading;
  }
}
