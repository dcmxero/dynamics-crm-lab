import { Environment } from './environment.model';

export const environment: Environment = {
  signIn: {
    clientId: 'c610cdab-0e52-46bb-b616-6997e7a33ffd',
    authority: 'https://login.microsoftonline.com/1589213a-daaf-496c-b68b-a2ac535a84f5',
    redirectUri: 'http://localhost:4200',
    apiScope: 'api://0d5314ae-13c3-410a-93dd-871796bbbe3e/access_as_user',
  },
};
