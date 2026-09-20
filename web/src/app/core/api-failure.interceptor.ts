import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { catchError, throwError } from 'rxjs';
import { ApiFailure } from './work-order.model';

/**
 * Turns every failed call into one shape the rest of the app can rely on.
 *
 * The API answers with problem details, but a request can also fail before it
 * ever reaches the API. Without this, every component would be reaching into
 * `error.error.detail` and guessing what is there.
 */
export const apiFailureInterceptor: HttpInterceptorFn = (request, next) =>
  next(request).pipe(catchError((error: unknown) => throwError(() => toFailure(error))));

function toFailure(error: unknown): ApiFailure {
  if (!(error instanceof HttpErrorResponse)) {
    return { message: 'Something went wrong.', rule: false, status: 0 };
  }

  // Status 0 means the request never got an answer: the API is down, or the
  // browser blocked it. Saying so is more useful than "unknown error".
  if (error.status === 0) {
    return {
      message: 'The service did not respond. Check that the API is running.',
      rule: false,
      status: 0,
    };
  }

  return {
    message: detailOf(error) ?? `The request failed (${error.status}).`,
    rule: error.status === 422,
    status: error.status,
  };
}

function detailOf(error: HttpErrorResponse): string | null {
  const body: unknown = error.error;

  if (body && typeof body === 'object') {
    const problem = body as { detail?: unknown; title?: unknown };

    if (typeof problem.detail === 'string' && problem.detail.length > 0) {
      return problem.detail;
    }

    if (typeof problem.title === 'string' && problem.title.length > 0) {
      return problem.title;
    }
  }

  return null;
}
