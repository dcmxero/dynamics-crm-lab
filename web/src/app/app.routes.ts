import { Routes } from '@angular/router';
import { signedInGuard } from './core/sign-in';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'work-orders' },
  {
    path: 'work-orders',
    canActivate: [signedInGuard],
    // Loaded on demand: the shell stays small and each feature ships its own chunk.
    loadChildren: () => import('./work-orders/work-order.routes').then((m) => m.workOrderRoutes),
  },
  { path: '**', redirectTo: 'work-orders' },
];
