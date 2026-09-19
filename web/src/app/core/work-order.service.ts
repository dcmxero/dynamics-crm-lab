import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import {
  RaiseWorkOrderRequest,
  WorkOrder,
  WorkOrderAssigned,
  WorkOrderClosed,
  WorkOrderCreated,
  WorkOrderStatus,
  WorkOrderSummary,
} from './work-order.model';

/**
 * The single place that knows the API routes.
 *
 * Components ask for work orders; none of them assembles a URL, which is what
 * keeps a route change to one file.
 */
@Injectable({ providedIn: 'root' })
export class WorkOrderService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api/work-orders';

  list(status: WorkOrderStatus, take = 50): Observable<WorkOrderSummary[]> {
    const params = new HttpParams().set('status', status).set('take', take);

    return this.http.get<WorkOrderSummary[]>(this.baseUrl, { params });
  }

  get(id: string): Observable<WorkOrder> {
    return this.http.get<WorkOrder>(`${this.baseUrl}/${id}`);
  }

  raise(request: RaiseWorkOrderRequest): Observable<WorkOrderCreated> {
    return this.http.post<WorkOrderCreated>(this.baseUrl, request);
  }

  assign(id: string, technicianId: string | null): Observable<WorkOrderAssigned> {
    return this.http.post<WorkOrderAssigned>(`${this.baseUrl}/${id}/assignment`, {
      technicianId,
    });
  }

  close(id: string, resolution: string): Observable<WorkOrderClosed> {
    return this.http.post<WorkOrderClosed>(`${this.baseUrl}/${id}/closure`, {
      resolution,
    });
  }
}
