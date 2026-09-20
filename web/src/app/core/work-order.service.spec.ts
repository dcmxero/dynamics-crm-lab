import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import { WorkOrderService } from './work-order.service';

describe('WorkOrderService', () => {
  let service: WorkOrderService;
  let backend: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });

    service = TestBed.inject(WorkOrderService);
    backend = TestBed.inject(HttpTestingController);
  });

  afterEach(() => backend.verify());

  it('asks for a stage and a page size', () => {
    service.list('InProgress', 25).subscribe();

    const request = backend.expectOne(
      (candidate) => candidate.url === '/api/work-orders' && candidate.method === 'GET',
    );

    expect(request.request.params.get('status')).toBe('InProgress');
    expect(request.request.params.get('take')).toBe('25');
    request.flush([]);
  });

  it('reads one job by its identifier', () => {
    service.get('abc').subscribe();

    backend.expectOne('/api/work-orders/abc').flush({});
  });

  it('posts a new job to the collection', () => {
    service.raise({ customerId: 'c', equipmentId: 'e', lines: [] }).subscribe();

    const request = backend.expectOne('/api/work-orders');

    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({ customerId: 'c', equipmentId: 'e', lines: [] });
    request.flush({});
  });

  it('sends a null technician when nobody is named', () => {
    service.assign('abc', null).subscribe();

    const request = backend.expectOne('/api/work-orders/abc/assignment');

    expect(request.request.body).toEqual({ technicianId: null });
    request.flush({});
  });

  it('sends the resolution when closing', () => {
    service.close('abc', 'Replaced the filter.').subscribe();

    const request = backend.expectOne('/api/work-orders/abc/closure');

    expect(request.request.body).toEqual({ resolution: 'Replaced the filter.' });
    request.flush({});
  });
});
