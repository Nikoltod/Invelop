import { ChangeDetectionStrategy, Component, computed, effect, inject } from '@angular/core';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { AbstractControl, NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { Store } from '@ngrx/store';
import { ButtonModule } from 'primeng/button';
import { DatePickerModule } from 'primeng/datepicker';
import { InputTextModule } from 'primeng/inputtext';
import { MessageModule } from 'primeng/message';
import { TextareaModule } from 'primeng/textarea';

import { FieldErrors } from '../data-access/contact.model';
import { ContactsPageActions } from '../data-access/contacts.actions';
import {
  selectContactById,
  selectFieldErrors,
  selectSaveError,
  selectSaving,
} from '../data-access/contacts.selectors';
import { fromDateOnly, toDateOnly } from '../data-access/date-only';

/** Field labels, used to build the client-side messages. */
const LABELS: Record<string, string> = {
  firstName: 'First name',
  surname: 'Surname',
  dateOfBirth: 'Date of birth',
  address: 'Address',
  phoneNumber: 'Phone number',
  iban: 'IBAN',
};

// Mirrors of the server's limits, for immediate feedback only. The server stays authoritative;
// if these ever disagree, the server wins and its message is what the user sees.
const MAX_NAME_LENGTH = 50;
const MAX_ADDRESS_LENGTH = 250;

@Component({
  selector: 'app-contact-form',
  imports: [
    ReactiveFormsModule,
    ButtonModule,
    DatePickerModule,
    InputTextModule,
    MessageModule,
    TextareaModule,
  ],
  templateUrl: './contact-form.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ContactFormComponent {
  private readonly store = inject(Store);
  private readonly router = inject(Router);
  private readonly formBuilder = inject(NonNullableFormBuilder);
  private readonly route = inject(ActivatedRoute);
  protected readonly editingId = this.route.snapshot.paramMap.get('id');

  protected readonly saving = this.store.selectSignal(selectSaving);
  protected readonly saveError = this.store.selectSignal(selectSaveError);
  private readonly fieldErrors = this.store.selectSignal(selectFieldErrors);

  protected readonly form = this.formBuilder.group({
    firstName: ['', [Validators.required, Validators.maxLength(MAX_NAME_LENGTH)]],
    surname: ['', [Validators.required, Validators.maxLength(MAX_NAME_LENGTH)]],
    dateOfBirth: this.formBuilder.control<Date | null>(null, Validators.required),
    address: ['', [Validators.required, Validators.maxLength(MAX_ADDRESS_LENGTH)]],
    phoneNumber: ['', Validators.required],
    iban: ['', Validators.required],
  });

  /** A date of birth cannot be in the future, so the picker will not offer one. */
  protected readonly today = new Date();

  /**
   * The app runs zoneless, so nothing re-renders just because a form control changed. Angular's
   * `AbstractControl.events` stream covers value, status, touched and pristine changes, and
   * turning it into a signal is what lets the computed messages below stay live.
   */
  private readonly formEvents = toSignal(this.form.events, { initialValue: null });

  /** One message per control: server error first, then the client-side rule it broke. */
  protected readonly messages = computed<Record<string, string | null>>(() => {
    this.formEvents();

    return Object.fromEntries(
      Object.keys(this.form.controls).map((name) => [name, this.messageFor(name)]),
    );
  });

  /**
   * Server errors whose key matches no control - including the `""` key ASP.NET uses for
   * object-level problems, and anything a future API version adds. Shown in the banner so a
   * validation error can never be silently swallowed.
   */
  protected readonly unmappedErrors = computed<string[]>(() => {
    const controls = Object.keys(this.form.controls);

    return Object.entries(this.fieldErrors())
      .filter(([field]) => !controls.includes(field))
      .flatMap(([, messages]) => messages);
  });

  constructor() {
    this.store.dispatch(ContactsPageActions.formOpened({ id: this.editingId }));

    if (this.editingId) {
      const contact = this.store.selectSignal(selectContactById(this.editingId));
      effect(() => {
        const value = contact();
        if (value) {
          this.form.patchValue({
            firstName: value.firstName,
            surname: value.surname,
            dateOfBirth: fromDateOnly(value.dateOfBirth),
            address: value.address,
            phoneNumber: value.phoneNumber,
            iban: value.iban,
          });
        }
      });
    }

    effect(() => this.applyServerErrors(this.fieldErrors()));
    this.clearServerErrorsOnEdit();
  }

  protected submit(): void {
    if (this.form.invalid) {
      // Reveal every problem at once rather than one per attempt.
      this.form.markAllAsTouched();
      return;
    }

    const value = this.form.getRawValue();
    const request = {
      firstName: value.firstName,
      surname: value.surname,
      dateOfBirth: toDateOnly(value.dateOfBirth!),
      address: value.address,
      phoneNumber: value.phoneNumber,
      iban: value.iban,
    };

    if (this.editingId) {
      this.store.dispatch(ContactsPageActions.updateSubmitted({ id: this.editingId, request }));
    } else {
      this.store.dispatch(ContactsPageActions.createSubmitted({ request }));
    }
  }

  protected cancel(): void {
    void this.router.navigate(['/contacts']);
  }

  private messageFor(name: string): string | null {
    const control = this.form.get(name);

    if (!control || !control.touched || !control.errors) {
      return null;
    }

    // The server's message is more specific than anything we can say here - it knows that the
    // IBAN's checksum failed, not merely that the field is non-empty.
    if (control.errors['server']) {
      return control.errors['server'] as string;
    }

    if (control.errors['required']) {
      return `${LABELS[name]} is required.`;
    }

    if (control.errors['maxlength']) {
      const { requiredLength } = control.errors['maxlength'] as { requiredLength: number };
      return `${LABELS[name]} must be at most ${requiredLength} characters.`;
    }

    return `${LABELS[name]} is not valid.`;
  }

  private applyServerErrors(fieldErrors: FieldErrors): void {
    for (const name of Object.keys(this.form.controls)) {
      const control = this.form.get(name);

      if (!control) {
        continue;
      }

      const messages = fieldErrors[name];

      if (messages?.length) {
        control.setErrors({ ...(control.errors ?? {}), server: messages[0] });
        control.markAsTouched();
      } else {
        removeServerError(control);
      }
    }
  }

  /**
   * Drops a control's server error as soon as the user edits it.
   *
   * Without this the message from the last submit sticks to a field the user has already fixed,
   * which reads as the form being broken. It has to be keyed to the specific control, too -
   * clearing all server errors on any keystroke would wipe the other five fields' messages the
   * moment the user starts correcting the first one.
   */
  private clearServerErrorsOnEdit(): void {
    for (const name of Object.keys(this.form.controls)) {
      const control = this.form.get(name);

      control?.valueChanges
        .pipe(takeUntilDestroyed())
        .subscribe(() => removeServerError(control));
    }
  }
}

function removeServerError(control: AbstractControl): void {
  if (!control.hasError('server')) {
    return;
  }

  const { server, ...remaining } = control.errors ?? {};
  void server;

  control.setErrors(Object.keys(remaining).length > 0 ? remaining : null);
}
