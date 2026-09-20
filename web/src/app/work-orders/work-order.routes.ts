import { Routes } from '@angular/router';

export const workOrderRoutes: Routes = [
  {
    path: '',
    loadComponent: () => import('./work-order-list').then((m) => m.WorkOrderList),
    title: 'Work orders',
  },
  {
    path: 'new',
    loadComponent: () => import('./raise-work-order').then((m) => m.RaiseWorkOrder),
    title: 'Raise a work order',
  },
  {
    path: ':id',
    loadComponent: () => import('./work-order-detail').then((m) => m.WorkOrderDetail),
    title: 'Work order',
  },
];
