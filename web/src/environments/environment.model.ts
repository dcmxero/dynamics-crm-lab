/** Where the client signs in and what it asks permission for. */
export interface SignInSettings {
  /** The application registration this client is. */
  readonly clientId: string;
  /** The tenant that issues the tokens. */
  readonly authority: string;
  /** Where the tenant sends the browser back to. */
  readonly redirectUri: string;
  /** The permission the API publishes, as it appears in a token request. */
  readonly apiScope: string;
}

/** What differs between the ways this client is built. */
export interface Environment {
  /**
   * The tenant to sign in against, or `null` to run without signing in.
   *
   * Only the end-to-end build leaves it out: that suite serves the client
   * against a stubbed API with no tenant behind either of them. Nothing is
   * weakened by it, because the guard below is about not showing people a page
   * that cannot load. What may actually be read and written is decided by the
   * API, which refuses an unauthenticated request whatever the client believes.
   */
  readonly signIn: SignInSettings | null;
}
