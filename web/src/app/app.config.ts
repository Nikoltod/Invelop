import { ApplicationConfig, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideHttpClient, withFetch } from '@angular/common/http';
import { provideRouter, withComponentInputBinding } from '@angular/router';
import { provideStore, provideState } from '@ngrx/store';
import { provideEffects } from '@ngrx/effects';
import { providePrimeNG } from 'primeng/config';
import { ConfirmationService, MessageService } from 'primeng/api';
import Aura from '@primeuix/themes/aura';

import { routes } from './app.routes';
import { contactsFeature } from './contacts/data-access/contacts.reducer';
import * as contactsEffects from './contacts/data-access/contacts.effects';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(routes, withComponentInputBinding()),
    provideHttpClient(withFetch()),

    provideStore(),
    provideState(contactsFeature),
    provideEffects(contactsEffects),

    providePrimeNG({ theme: { preset: Aura, options: { darkModeSelector: '.dark-mode' } } }),

    // PrimeNG's toast and confirm dialog are driven through these services, so they must be
    // provided above the components that render them in app.html.
    MessageService,
    ConfirmationService,
  ],
};
