import { Routes } from '@angular/router';

export const workOrderRoutes: Routes = [
  {
    path: '',
    loadComponent: () => import('./work-order-list').then((m) => m.WorkOrderList),
    title: 'Work orders',
  },
];
