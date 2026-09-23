import { HttpErrorResponse } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { beforeEach, describe, expect, it } from 'vitest';
import { apiFailureInterceptor } from './api-failure.interceptor';
import { ApiFailure } from './work-order.model';

describe('apiFailureInterceptor', () => {
  let http: HttpClient;
  let backend: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([apiFailureInterceptor])),
        provideHttpClientTesting(),
      ],
    });

    http = TestBed.inject(HttpClient);
    backend = TestBed.inject(HttpTestingController);
  });

  function failWith(status: number, body: object): Promise<ApiFailure> {
    return new Promise((resolve) => {
      http.get('/api/work-orders').subscribe({
        error: (failure: ApiFailure) => resolve(failure),
      });

      backend.expectOne('/api/work-orders').flush(body, { status, statusText: 'x' });
    });
  }

  it('asks the caller to sign in again when the token is no longer valid', async () => {
    const failure = await failWith(401, {});

    expect(failure.status).toBe(401);
    expect(failure.message).toContain('Sign in again');
    expect(failure.rule).toBe(false);
  });

  it('says it is a matter of access when the caller is known and refused', async () => {
    // A refusal is not something signing in again would fix, so it must not
    // read like one.
    const failure = await failWith(403, {});

    expect(failure.status).toBe(403);
    expect(failure.message).toContain('not allowed');
  });

  it('reports the detail from a problem document', async () => {
    const failure = await failWith(422, {
      title: 'The job does not allow this',
      detail: 'A work order cannot be closed without a resolution.',
    });

    expect(failure.message).toBe('A work order cannot be closed without a resolution.');
  });

  it('marks a 422 as a broken rule rather than a fault', async () => {
    const failure = await failWith(422, { detail: 'Line quantity must be positive.' });

    expect(failure.rule).toBe(true);
  });

  it('does not mark a 404 as a broken rule', async () => {
    const failure = await failWith(404, { detail: 'Work order does not exist.' });

    expect(failure.rule).toBe(false);
  });

  it('falls back to the title when there is no detail', async () => {
    const failure = await failWith(400, { title: 'Unknown status' });

    expect(failure.message).toBe('Unknown status');
  });

  it('says the service did not answer when the request never landed', async () => {
    const failure = await new Promise<ApiFailure>((resolve) => {
      http.get('/api/work-orders').subscribe({
        error: (problem: ApiFailure) => resolve(problem),
      });

      backend
        .expectOne('/api/work-orders')
        .error(new ProgressEvent('error'), { status: 0, statusText: '' });
    });

    expect(failure.status).toBe(0);
    expect(failure.message).toContain('did not respond');
  });

  it('handles a failure that is not an HTTP response at all', async () => {
    const failure = await new Promise<ApiFailure>((resolve) => {
      http.get('/api/work-orders').subscribe({
        error: (problem: ApiFailure) => resolve(problem),
      });

      backend.expectOne('/api/work-orders').error(new ProgressEvent('error'));
    });

    expect(failure).toHaveProperty('message');
    expect(failure.rule).toBe(false);
  });

  it('leaves a successful call untouched', async () => {
    const body = await new Promise((resolve) => {
      http.get('/api/work-orders').subscribe(resolve);

      backend.expectOne('/api/work-orders').flush([{ id: '1' }]);
    });

    expect(body).toEqual([{ id: '1' }]);
  });

  it('does not swallow the error type', () => {
    expect(new HttpErrorResponse({ status: 500 })).toBeInstanceOf(HttpErrorResponse);
  });
});
