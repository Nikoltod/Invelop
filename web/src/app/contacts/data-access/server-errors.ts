import { HttpErrorResponse } from '@angular/common/http';

import { FieldErrors } from './contact.model';

export interface ServerFailure {
  /** Shown in a banner. Always present, so a failure can never render as a blank message. */
  readonly message: string;
  /** Per-field messages, keyed to match the reactive form's control names. */
  readonly fieldErrors: FieldErrors;
}

/**
 * Turns anything the HTTP layer can throw into something the UI can show.
 *
 * The API returns RFC 9457 for every error, so there is exactly one shape to parse. The
 * distinction that matters here is 400-with-`errors` - a validation failure the user can fix by
 * editing a field - versus everything else, which they cannot.
 */
export function describeFailure(error: unknown): ServerFailure {
  if (!(error instanceof HttpErrorResponse)) {
    return { message: 'Something went wrong. Please try again.', fieldErrors: {} };
  }

  if (error.status === 0) {
    return {
      message: 'Could not reach the server. Check that the API is running, then try again.',
      fieldErrors: {},
    };
  }

  if (error.status === 400 && isFieldErrors(error.error?.errors)) {
    return {
      message: 'Please correct the highlighted fields.',
      fieldErrors: error.error.errors,
    };
  }

  if (error.status === 404) {
    return { message: 'That contact no longer exists.', fieldErrors: {} };
  }

  return {
    message: error.error?.title ?? `The server returned an error (${error.status}).`,
    fieldErrors: {},
  };
}

function isFieldErrors(value: unknown): value is FieldErrors {
  return (
    typeof value === 'object' &&
    value !== null &&
    Object.values(value).every((messages) => Array.isArray(messages))
  );
}
