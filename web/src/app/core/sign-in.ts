import { HTTP_INTERCEPTORS } from '@angular/common/http';
import { EnvironmentProviders, Provider, inject, provideAppInitializer } from '@angular/core';
import { CanActivateFn } from '@angular/router';
import {
  MSAL_GUARD_CONFIG,
  MSAL_INSTANCE,
  MSAL_INTERCEPTOR_CONFIG,
  MsalBroadcastService,
  MsalGuard,
  MsalGuardConfiguration,
  MsalInterceptor,
  MsalInterceptorConfiguration,
  MsalService,
} from '@azure/msal-angular';
import {
  IPublicClientApplication,
  InteractionType,
  PublicClientApplication,
} from '@azure/msal-browser';
import { concatMap, firstValueFrom, tap } from 'rxjs';
import { environment } from '../../environments/environment';

/**
 * Wires up signing in against the tenant.
 *
 * The client never sees a work order without a token: the API refuses the call.
 * Sending people to the tenant first turns that refusal into a sign-in rather
 * than into an empty page and an error.
 */
export function provideSignIn(): (Provider | EnvironmentProviders)[] {
  const settings = environment.signIn;

  if (settings === null) {
    return [];
  }

  const instance: IPublicClientApplication = new PublicClientApplication({
    auth: {
      clientId: settings.clientId,
      authority: settings.authority,
      redirectUri: settings.redirectUri,
    },
    cache: {
      // Kept to the tab. A token in local storage outlives the browser session
      // and every other tab, which is more of it lying about than is needed.
      cacheLocation: 'sessionStorage',
    },
  });

  const guard: MsalGuardConfiguration = {
    interactionType: InteractionType.Redirect,
    authRequest: { scopes: [settings.apiScope] },
  };

  // The token is attached by matching the address being called, so a request to
  // anywhere else never carries it.
  const attachTo = new Map<string, string[]>([['/api/*', [settings.apiScope]]]);

  const interceptor: MsalInterceptorConfiguration = {
    interactionType: InteractionType.Redirect,
    protectedResourceMap: attachTo,
  };

  return [
    { provide: MSAL_INSTANCE, useValue: instance },
    { provide: MSAL_GUARD_CONFIG, useValue: guard },
    { provide: MSAL_INTERCEPTOR_CONFIG, useValue: interceptor },
    { provide: HTTP_INTERCEPTORS, useClass: MsalInterceptor, multi: true },
    MsalService,
    MsalGuard,
    MsalBroadcastService,
    // The tenant sends the browser back with the answer in the address, and it
    // has to be read before anything asks whether somebody is signed in.
    provideAppInitializer(() => {
      const msal = inject(MsalService);

      return firstValueFrom(
        msal.initialize().pipe(
          concatMap(() => msal.handleRedirectObservable()),
          tap((answer) => {
            // Everything afterwards asks "who is signed in", and with more than
            // one account remembered in the tab nothing else settles that.
            const account = answer?.account ?? msal.instance.getAllAccounts()[0];

            if (account !== undefined) {
              msal.instance.setActiveAccount(account);
            }
          }),
        ),
      );
    }),
  ];
}

/**
 * Keeps a route for people who have signed in.
 *
 * Built without a tenant the route is simply open: there is nothing to sign in
 * against, and it is the API that decides what may be read either way.
 */
export const signedInGuard: CanActivateFn = (route, state) =>
  environment.signIn === null ? true : inject(MsalGuard).canActivate(route, state);
