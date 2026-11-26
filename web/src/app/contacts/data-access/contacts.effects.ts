import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { Actions, createEffect, ofType } from '@ngrx/effects';
import { MessageService } from 'primeng/api';
import { catchError, concatMap, map, of, switchMap, tap } from 'rxjs';

import { ContactsApiService } from './contacts-api.service';
import { ContactsApiActions, ContactsPageActions } from './contacts.actions';
import { describeFailure } from './server-errors';

export const loadContacts = createEffect(
  (actions$ = inject(Actions), api = inject(ContactsApiService)) =>
    actions$.pipe(
      ofType(ContactsPageActions.opened, ContactsPageActions.retried),
      switchMap(() =>
        api.getAll().pipe(
          map((contacts) => ContactsApiActions.loadSucceeded({ contacts })),
          catchError((error) =>
            of(ContactsApiActions.loadFailed({ message: describeFailure(error).message })),
          ),
        ),
      ),
    ),
  { functional: true },
);

export const getContact = createEffect(
  (actions$ = inject(Actions), api = inject(ContactsApiService)) =>
    actions$.pipe(
      ofType(ContactsPageActions.formOpened),
      switchMap(({ id }) =>
        id
          ? api.get(id).pipe(
              map((contact) => ContactsApiActions.getSucceeded({ contact })),
              catchError((error) =>
                of(ContactsApiActions.getFailed({ message: describeFailure(error).message })),
              ),
            )
          : of(),
      ),
    ),
  { functional: true },
);

export const createContact = createEffect(
  (actions$ = inject(Actions), api = inject(ContactsApiService)) =>
    actions$.pipe(
      ofType(ContactsPageActions.createSubmitted),
      // concatMap, not switchMap: a double-submit must not silently cancel the first request
      // after the server has already accepted it. Here a dropped write would lose a contact.
      concatMap(({ request }) =>
        api.create(request).pipe(
          map((contact) => ContactsApiActions.createSucceeded({ contact })),
          catchError((error) => of(ContactsApiActions.createFailed(describeFailure(error)))),
        ),
      ),
    ),
  { functional: true },
);

export const updateContact = createEffect(
  (actions$ = inject(Actions), api = inject(ContactsApiService)) =>
    actions$.pipe(
      ofType(ContactsPageActions.updateSubmitted),
      concatMap(({ id, request }) =>
        api.update(id, request).pipe(
          map((contact) => ContactsApiActions.updateSucceeded({ contact })),
          catchError((error) => of(ContactsApiActions.updateFailed(describeFailure(error)))),
        ),
      ),
    ),
  { functional: true },
);

export const deleteContact = createEffect(
  (actions$ = inject(Actions), api = inject(ContactsApiService)) =>
    actions$.pipe(
      ofType(ContactsPageActions.deleteRequested),
      concatMap(({ id }) =>
        api.delete(id).pipe(
          map(() => ContactsApiActions.deleteSucceeded({ id })),
          catchError((error) =>
            of(ContactsApiActions.deleteFailed({ message: describeFailure(error).message })),
          ),
        ),
      ),
    ),
  { functional: true },
);

/**
 * Navigation and toasts: the side effects that are not state changes.
 *
 * `dispatch: false` because nothing new happens in the store as a result - and an effect that
 * both navigates and dispatches is the usual way a redirect loop gets built by accident.
 */
export const navigateAfterWrite = createEffect(
  (actions$ = inject(Actions), router = inject(Router), messages = inject(MessageService)) =>
    actions$.pipe(
      ofType(ContactsApiActions.createSucceeded, ContactsApiActions.updateSucceeded),
      tap(({ contact }) => {
        messages.add({
          severity: 'success',
          summary: 'Contact saved',
          detail: `${contact.firstName} ${contact.surname} was saved.`,
        });

        void router.navigate(['/contacts']);
      }),
    ),
  { functional: true, dispatch: false },
);

export const notifyAfterDelete = createEffect(
  (actions$ = inject(Actions), messages = inject(MessageService)) =>
    actions$.pipe(
      ofType(ContactsApiActions.deleteSucceeded),
      tap(() =>
        messages.add({
          severity: 'success',
          summary: 'Contact deleted',
          detail: 'The contact was removed.',
        }),
      ),
    ),
  { functional: true, dispatch: false },
);
