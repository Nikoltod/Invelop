import { createEntityAdapter, EntityState } from '@ngrx/entity';
import { createFeature, createReducer, on } from '@ngrx/store';

import { Contact, FieldErrors } from './contact.model';
import { ContactsApiActions, ContactsPageActions } from './contacts.actions';

export type LoadStatus = 'idle' | 'loading' | 'loaded' | 'error';

export interface ContactsState extends EntityState<Contact> {
  readonly status: LoadStatus;
  /** Why the list failed to load. Null whenever `status` is not 'error'. */
  readonly loadError: string | null;
  readonly saving: boolean;
  /** Banner-level message for a failed save. */
  readonly saveError: string | null;
  /** Per-field messages from the server, keyed to the form's control names. */
  readonly fieldErrors: FieldErrors;
  readonly editingId: string | null;
  readonly deletingId: string | null;
}

/**
 * The server already returns contacts sorted by surname, so the adapter keeps insertion order
 * rather than re-sorting. A client-side `sortComparer` here would be a second, slightly
 * different opinion about ordering - and the two would eventually disagree.
 */
export const contactsAdapter = createEntityAdapter<Contact>();

const initialState: ContactsState = contactsAdapter.getInitialState({
  status: 'idle' as LoadStatus,
  loadError: null,
  saving: false,
  saveError: null,
  fieldErrors: {},
  editingId: null,
  deletingId: null,
});

export const contactsFeature = createFeature({
  name: 'contacts',
  reducer: createReducer(
    initialState,

    on(ContactsPageActions.opened, ContactsPageActions.retried, (state) => ({
      ...state,
      status: 'loading' as LoadStatus,
      loadError: null,
    })),

    on(ContactsApiActions.loadSucceeded, (state, { contacts }) =>
      contactsAdapter.setAll(contacts, { ...state, status: 'loaded' as LoadStatus, loadError: null }),
    ),

    on(ContactsApiActions.loadFailed, (state, { message }) => ({
      ...state,
      status: 'error' as LoadStatus,
      loadError: message,
    })),

    on(ContactsPageActions.formOpened, (state, { id }) => ({
      ...state,
      editingId: id,
      saving: false,
      saveError: null,
      fieldErrors: {},
    })),

    on(ContactsPageActions.createSubmitted, ContactsPageActions.updateSubmitted, (state) => ({
      ...state,
      saving: true,
      saveError: null,
      fieldErrors: {},
    })),

    on(ContactsApiActions.createSucceeded, (state, { contact }) =>
      contactsAdapter.addOne(contact, { ...state, saving: false, saveError: null, fieldErrors: {}, editingId: null }),
    ),

    on(ContactsApiActions.createFailed, ContactsApiActions.updateFailed, (state, { message, fieldErrors }) => ({
      ...state,
      saving: false,
      saveError: message,
      fieldErrors,
    })),

    on(ContactsApiActions.updateSucceeded, (state, { contact }) =>
      contactsAdapter.updateOne(
        { id: contact.id, changes: contact },
        { ...state, saving: false, saveError: null, fieldErrors: {}, editingId: null },
      ),
    ),

    on(ContactsPageActions.deleteRequested, (state, { id }) => ({
      ...state,
      deletingId: id,
      saveError: null,
    })),

    on(ContactsApiActions.deleteSucceeded, (state, { id }) =>
      contactsAdapter.removeOne(id, { ...state, deletingId: null, saveError: null }),
    ),

    on(ContactsApiActions.deleteFailed, (state, { message }) => ({
      ...state,
      deletingId: null,
      saveError: message,
    })),

    on(ContactsApiActions.getSucceeded, (state, { contact }) =>
      contactsAdapter.upsertOne(contact, { ...state, saveError: null }),
    ),

    on(ContactsApiActions.getFailed, (state, { message }) => ({
      ...state,
      saveError: message,
    })),
  ),
});
