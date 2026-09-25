import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { Customer, Equipment } from './work-order.model';

/**
 * Reads the lists somebody needs before they can raise a job.
 *
 * Kept apart from the work order service: these are the things a job is raised
 * against, not the jobs themselves, and nothing here writes.
 */
@Injectable({ providedIn: 'root' })
export class CatalogueService {
  private readonly http = inject(HttpClient);

  findCustomers(name: string, take = 10): Observable<Customer[]> {
    const params = new HttpParams().set('name', name).set('take', take);

    return this.http.get<Customer[]>('/api/customers', { params });
  }

  equipmentOf(customerId: string): Observable<Equipment[]> {
    return this.http.get<Equipment[]>(`/api/customers/${customerId}/equipment`);
  }
}
