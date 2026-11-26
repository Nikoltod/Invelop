import { expect, Page, test } from '@playwright/test';

/**
 * Synthetic data. The IBAN is a published ISO 13616 documentation sample, not a real account.
 */
const SAMPLE = {
  firstName: 'Grace',
  phoneNumber: '+31 20 765 4321',
  address: 'Prinsengracht 7\n1015 DK Amsterdam',
  iban: 'NL91 ABNA 0417 1643 00',
};

async function pickDateOfBirth(page: Page): Promise<{ iso: string; display: string }> {
  await page.locator('#dateOfBirth').click();

  const panel = page.locator('.p-datepicker-panel');
  await expect(panel, 'the date picker overlay should open on click').toBeVisible();

  const enabledDay = panel
    .locator('table td:not(.p-datepicker-other-month):not(.p-disabled)')
    .locator('span')
    .first();

  await expect(enabledDay, 'an enabled past day should be available in the opening month').toBeVisible();

  const monthName = (
    await panel.locator('[data-pc-section="selectmonth"], .p-datepicker-select-month').first().textContent()
  )?.trim();
  const year = (
    await panel.locator('[data-pc-section="selectyear"], .p-datepicker-select-year').first().textContent()
  )?.trim();
  const day = (await enabledDay.textContent())?.trim();

  await expect(monthName, 'the calendar month should be readable').toBeTruthy();
  await expect(year, 'the calendar year should be readable').toBeTruthy();
  await expect(day, 'the enabled day should be readable').toBeTruthy();

  const monthNumber = new Date(`${monthName} 1, ${year}`).getMonth() + 1;
  const dayNumber = Number(day);
  const yearNumber = Number(year);
  const iso = `${yearNumber}-${String(monthNumber).padStart(2, '0')}-${String(dayNumber).padStart(2, '0')}`;
  const display = `${String(dayNumber).padStart(2, '0')} ${monthName} ${yearNumber}`;

  await enabledDay.click();
  await expect(panel, 'the overlay should close once a day is chosen').toBeHidden();

  return { iso, display };
}

async function fillContactForm(page: Page, surname: string): Promise<{ iso: string; display: string }> {
  await page.getByTestId('firstName').fill(SAMPLE.firstName);
  await page.getByTestId('surname').fill(surname);
  await page.getByTestId('phoneNumber').fill(SAMPLE.phoneNumber);
  await page.getByTestId('address').fill(SAMPLE.address);
  await page.getByTestId('iban').fill(SAMPLE.iban);

  return pickDateOfBirth(page);
}

test.describe('Contacts', () => {
  test('a created contact is still listed after a page reload', async ({ page }) => {
    // Unique per run, so the row assertion cannot pass by matching a different contact.
    const surname = 'HopperReload';

    await page.goto('/contacts');
    await page.getByTestId('add-contact').click();
    await expect(page).toHaveURL(/\/contacts\/new$/);

    const selected = await fillContactForm(page, surname);

    await expect(page.getByTestId('firstName')).toHaveValue(SAMPLE.firstName);
    await expect(page.getByTestId('surname')).toHaveValue(surname);
    await expect(page.locator('#dateOfBirth')).not.toHaveValue('');

    const createResponse = page.waitForResponse(
      (response) =>
        response.url().includes('/api/contacts') &&
        response.request().method() === 'POST',
    );

    await page.getByTestId('save-contact').click();

    const response = await createResponse;
    expect(response.status()).toBe(201);

    const requestBody = response.request().postDataJSON() as { dateOfBirth?: string };
    expect(requestBody.dateOfBirth).toBe(selected.iso);

    // Saving returns to the list.
    await expect(page).toHaveURL(/\/contacts$/);

    const row = page.getByTestId('contact-row').filter({ hasText: surname });
    await expect(row).toBeVisible();

    // The real check: the contact came from the database, not from the store we just updated.
    await page.reload();

    await expect(row).toBeVisible();
    await expect(row).toContainText('NL91 ABNA 0417 1643 00');

    await row.getByTitle('Edit contact').click();
    await expect(page).toHaveURL(/\/contacts\/[^/]+\/edit$/);

    await page.getByTestId('firstName').fill('Ada');
    const updateResponse = page.waitForResponse(
      (response) => response.url().includes('/api/contacts/') && response.request().method() === 'PUT',
    );
    await page.getByTestId('save-contact').click();
    expect((await updateResponse).status()).toBe(200);

    await expect(page).toHaveURL(/\/contacts$/);
    const updatedRow = page.getByTestId('contact-row').filter({ hasText: surname });
    await expect(updatedRow).toContainText('Ada');

    await updatedRow.getByTitle('Delete contact').click();
    const dialog = page.locator('.p-confirmdialog');
    await expect(dialog).toBeVisible();

    const deleteResponse = page.waitForResponse(
      (response) => response.url().includes('/api/contacts/') && response.request().method() === 'DELETE',
    );
    await dialog.getByRole('button', { name: 'Delete' }).click();
    expect((await deleteResponse).status()).toBe(204);

    await expect(updatedRow).toBeHidden();
  });
});
