import { createSelector } from '@ngrx/store';

import { contactsAdapter, contactsFeature } from './contacts.reducer';

const { selectAll } = contactsAdapter.getSelectors();

export const {
  selectContactsState,
  selectStatus,
  selectLoadError,
  selectSaving,
  selectSaveError,
  selectFieldErrors,
  selectEditingId,
  selectDeletingId,
} = contactsFeature;

export const selectAllContacts = createSelector(selectContactsState, selectAll);
export const selectContactById = (id: string) =>
  createSelector(selectAllContacts, (contacts) => contacts.find((contact) => contact.id === id));
export const selectContactCount = createSelector(selectAllContacts, (contacts) => contacts.length);

/**
 * The three list states, derived in one place.
 *
 * Computing these here rather than writing `status === 'loaded' && count === 0` in the template
 * means the empty state cannot accidentally flash during the first load - the single most common
 * way a list screen looks broken for a fraction of a second.
 */
export const selectIsLoading = createSelector(selectStatus, (status) => status === 'loading');

export const selectIsEmpty = createSelector(
  selectStatus,
  selectContactCount,
  (status, count) => status === 'loaded' && count === 0,
);

export const selectHasLoadError = createSelector(selectStatus, (status) => status === 'error');
