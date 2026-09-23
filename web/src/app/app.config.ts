import {
  provideHttpClient,
  withFetch,
  withInterceptors,
  withInterceptorsFromDi,
} from '@angular/common/http';
import { ApplicationConfig, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideRouter, withComponentInputBinding } from '@angular/router';
import { apiFailureInterceptor } from './core/api-failure.interceptor';
import { provideSignIn } from './core/sign-in';
import { routes } from './app.routes';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    // withComponentInputBinding lets a route parameter arrive as a component
    // input, so a detail page does not have to subscribe to the route itself.
    provideRouter(routes, withComponentInputBinding()),
    ...provideSignIn(),
    // The token is attached by a class based interceptor from the sign-in
    // library, so both kinds have to be taken from the container.
    provideHttpClient(
      withFetch(),
      withInterceptorsFromDi(),
      withInterceptors([apiFailureInterceptor]),
    ),
  ],
};
