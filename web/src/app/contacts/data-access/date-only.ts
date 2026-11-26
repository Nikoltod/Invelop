/**
 * Conversions between the API's `yyyy-MM-dd` strings and the `Date` objects PrimeNG's date
 * picker works in.
 *
 * This file exists because `new Date('1990-05-17').toISOString()` is a trap. An ISO date-only
 * string is parsed as UTC midnight, so anyone west of UTC gets 1990-05-16 back, and a date of
 * birth silently shifts a day every time it round-trips. Both functions below stay in local
 * date parts and never touch `toISOString`, which removes the whole class of bug rather than
 * patching the symptom.
 */

/** `Date` (local) -> `"1990-05-17"`. */
export function toDateOnly(date: Date): string {
  const year = date.getFullYear();
  const month = `${date.getMonth() + 1}`.padStart(2, '0');
  const day = `${date.getDate()}`.padStart(2, '0');

  return `${year}-${month}-${day}`;
}

/** `"1990-05-17"` -> `Date` at local midnight. */
export function fromDateOnly(value: string): Date {
  const [year, month, day] = value.split('-').map(Number);

  return new Date(year, month - 1, day);
}

/** `"1990-05-17"` -> `"17 May 1990"` for display, without going through a `Date` at all. */
export function formatDateOnly(value: string): string {
  const [year, month, day] = value.split('-').map(Number);
  const monthNames = [
    'Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun',
    'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec',
  ];

  return `${day} ${monthNames[month - 1]} ${year}`;
}
