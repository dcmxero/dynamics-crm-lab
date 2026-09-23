import { Component, inject, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatToolbarModule } from '@angular/material/toolbar';
import { RouterLink, RouterOutlet } from '@angular/router';
import { MsalService } from '@azure/msal-angular';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, RouterLink, MatToolbarModule, MatButtonModule, MatIconModule],
  templateUrl: './app.html',
  styleUrl: './app.scss',
})
export class App {
  // Absent in the end-to-end build, which runs without a tenant to sign in
  // against, so the toolbar simply shows nobody.
  private readonly msal = inject(MsalService, { optional: true });

  readonly signedInAs = signal(this.msal?.instance.getActiveAccount()?.name ?? null);

  signOut(): void {
    this.msal?.logoutRedirect();
  }
}
