import { describe, expect, it } from 'vitest';

import { Contact } from './contact.model';
import { ContactsApiActions, ContactsPageActions } from './contacts.actions';
import { contactsFeature } from './contacts.reducer';

const contact: Contact = {
  id: '00000000-0000-4000-8000-000000000001',
  firstName: 'Ada',
  surname: 'Lovelace',
  dateOfBirth: '1990-05-17',
  address: 'London',
  phoneNumber: '+31201234567',
  iban: 'NL91ABNA0417164300',
};

const initialState = contactsFeature.reducer(undefined, { type: '@ngrx/init' });

describe('contacts reducer', () => {
  it('starts with an idle state', () => {
    expect(initialState.status).toBe('idle');
  });

  it('tracks edit mode on the form page', () => {
    const state = contactsFeature.reducer(
      initialState,
      ContactsPageActions.formOpened({ id: contact.id }),
    );

    expect(state.editingId).toBe(contact.id);
  });

  it('removes a deleted contact from entity state', () => {
    const loaded = contactsFeature.reducer(
      initialState,
      ContactsApiActions.loadSucceeded({ contacts: [contact] }),
    );

    const deleted = contactsFeature.reducer(
      loaded,
      ContactsApiActions.deleteSucceeded({ id: contact.id }),
    );

    expect(Object.keys(deleted.entities)).toEqual([]);
  });
});
