import { Injectable } from '@angular/core';

import { environment } from '../../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class AppConfigService {
  readonly appName = 'Portal Nacional de Gobernanza Musical';
  readonly apiBaseUrl = environment.apiBaseUrl;
}
