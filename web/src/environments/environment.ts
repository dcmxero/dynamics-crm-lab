import { Environment } from './environment.model';

/**
 * How this client is built when nothing local says otherwise.
 *
 * No tenant, because the registrations belong to whoever set the lab up and a
 * checkout carrying them would send somebody else's browser somewhere it has no
 * business going. A fresh clone therefore runs without signing in, which is
 * enough to see the client work against a stubbed API.
 *
 * To run it against a real environment, copy environment.local.example.ts to
 * environment.local.ts, fill in your own two registrations, and start with
 * `npm run start:tenant`. That file is not committed. The identifiers in it are
 * not secrets, but they are yours.
 */
export const environment: Environment = {
  signIn: null,
};
