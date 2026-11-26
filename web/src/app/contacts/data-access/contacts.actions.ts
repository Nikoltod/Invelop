import { createActionGroup, emptyProps, props } from '@ngrx/store';

import { Contact, CreateContactRequest, FieldErrors } from './contact.model';

/**
 * Actions are grouped by *source*, not by entity.
 *
 * A page action describes something a user did; an API action describes something that came
 * back. Keeping them apart means the action log reads as a narrative of events that actually
 * happened, rather than a list of commands - which is what makes the devtools timeline worth
 * opening when something goes wrong.
 */
export const ContactsPageActions = createActionGroup({
  source: 'Contacts Page',
  events: {
    Opened: emptyProps(),
    Retried: emptyProps(),
    'Form Opened': props<{ id: string | null }>(),
    'Create Submitted': props<{ request: CreateContactRequest }>(),
    'Update Submitted': props<{ id: string; request: CreateContactRequest }>(),
    'Delete Requested': props<{ id: string }>(),
  },
});

export const ContactsApiActions = createActionGroup({
  source: 'Contacts API',
  events: {
    'Load Succeeded': props<{ contacts: Contact[] }>(),
    'Load Failed': props<{ message: string }>(),
    'Get Succeeded': props<{ contact: Contact }>(),
    'Get Failed': props<{ message: string }>(),
    'Create Succeeded': props<{ contact: Contact }>(),
    'Create Failed': props<{ message: string; fieldErrors: FieldErrors }>(),
    'Update Succeeded': props<{ contact: Contact }>(),
    'Update Failed': props<{ message: string; fieldErrors: FieldErrors }>(),
    'Delete Succeeded': props<{ id: string }>(),
    'Delete Failed': props<{ message: string }>(),
  },
});
