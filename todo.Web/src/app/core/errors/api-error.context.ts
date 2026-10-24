import { HttpContextToken } from '@angular/common/http';

export const SILENT_API_ERROR = new HttpContextToken<boolean>(() => false);
