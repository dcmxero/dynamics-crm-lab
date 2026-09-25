import { Environment } from './environment.model';

export const environment: Environment = {
  signIn: {
    // The SPA registration.
    clientId: '00000000-0000-0000-0000-000000000000',
    authority: 'https://login.microsoftonline.com/00000000-0000-0000-0000-000000000000',
    redirectUri: 'http://localhost:4200',
    // The scope the API registration exposes.
    apiScope: 'api://00000000-0000-0000-0000-000000000000/access_as_user',
  },
};
