/**
 * A contact exactly as the API returns it.
 *
 * `dateOfBirth` is a plain `yyyy-MM-dd` string, never a `Date`. See `date-only.ts` for why that
 * distinction is load-bearing rather than fussy.
 */
export interface Contact {
  readonly id: string;
  readonly firstName: string;
  readonly surname: string;
  readonly dateOfBirth: string;
  readonly address: string;
  readonly phoneNumber: string;
  readonly iban: string;
}

export type CreateContactRequest = Omit<Contact, 'id'>;

/** The `errors` dictionary of an RFC 9457 response: field name -> messages. */
export type FieldErrors = Readonly<Record<string, readonly string[]>>;
