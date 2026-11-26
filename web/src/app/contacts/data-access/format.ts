/** `"NL91ABNA0417164300"` -> `"NL91 ABNA 0417 1643 00"`, the way a bank statement prints it. */
export function formatIban(iban: string): string {
  return iban.replace(/(.{4})/g, '$1 ').trim();
}
