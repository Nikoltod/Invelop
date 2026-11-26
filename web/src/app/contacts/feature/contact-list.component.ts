import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { Store } from '@ngrx/store';
import { ConfirmationService } from 'primeng/api';
import { ButtonModule } from 'primeng/button';
import { MessageModule } from 'primeng/message';
import { SkeletonModule } from 'primeng/skeleton';
import { TableModule } from 'primeng/table';

import { Contact } from '../data-access/contact.model';
import { ContactsPageActions } from '../data-access/contacts.actions';
import {
  selectAllContacts,
  selectHasLoadError,
  selectIsEmpty,
  selectIsLoading,
  selectLoadError,
} from '../data-access/contacts.selectors';
import { formatDateOnly } from '../data-access/date-only';
import { formatIban } from '../data-access/format';

@Component({
  selector: 'app-contact-list',
  imports: [TableModule, ButtonModule, MessageModule, SkeletonModule, RouterLink],
  templateUrl: './contact-list.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ContactListComponent {
  private readonly store = inject(Store);
  private readonly confirmations = inject(ConfirmationService);

  protected readonly contacts = this.store.selectSignal(selectAllContacts);
  protected readonly isLoading = this.store.selectSignal(selectIsLoading);
  protected readonly isEmpty = this.store.selectSignal(selectIsEmpty);
  protected readonly hasError = this.store.selectSignal(selectHasLoadError);
  protected readonly loadError = this.store.selectSignal(selectLoadError);

  protected readonly skeletonRows = [1, 2, 3];

  protected readonly formatDateOnly = formatDateOnly;
  protected readonly formatIban = formatIban;

  constructor() {
    // The component announces that the page was opened; the effect decides that this means a
    // fetch. The component never knows there is an HTTP call involved at all.
    this.store.dispatch(ContactsPageActions.opened());
  }

  protected retry(): void {
    this.store.dispatch(ContactsPageActions.retried());
  }

  protected confirmDelete(contact: Contact): void {
    this.confirmations.confirm({
      message: `Delete ${contact.firstName} ${contact.surname}?`,
      header: 'Delete contact',
      icon: 'pi pi-exclamation-triangle',
      acceptLabel: 'Delete',
      rejectLabel: 'Cancel',
      accept: () => this.store.dispatch(ContactsPageActions.deleteRequested({ id: contact.id })),
    });
  }
}
