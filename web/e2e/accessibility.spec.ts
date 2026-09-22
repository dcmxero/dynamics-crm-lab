import AxeBuilder from '@axe-core/playwright';
import { expect, Page, test } from '@playwright/test';

/**
 * Runs axe against each screen.
 *
 * This catches the mechanical faults - a control with no accessible name, a
 * heading order that skips a level, colours that do not carry enough contrast.
 * It does not prove a screen is usable with a screen reader; nothing automated
 * does. It does stop the obvious regressions from landing.
 */

const JOB_ID = '3fa85f64-5717-4562-b3fc-2c963f66afa6';

const detail = {
  id: JOB_ID,
  number: 'WO-20260901-ABCDEF',
  customerId: '11111111-1111-1111-1111-111111111111',
  equipmentId: '22222222-2222-2222-2222-222222222222',
  technicianId: null,
  status: 'New',
  resolution: null,
  totalPrice: 90,
  currency: 'EUR',
  lines: [
    {
      id: '33333333-3333-3333-3333-333333333333',
      description: 'Technician labour',
      quantity: 2,
      unitPrice: 45,
      lineTotal: 90,
    },
  ],
};

async function scan(page: Page): Promise<void> {
  const results = await new AxeBuilder({ page })
    .withTags(['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa'])
    .analyze();

  expect(results.violations).toEqual([]);
}

test('the list has no accessibility violations', async ({ page }) => {
  await page.route('**/api/work-orders?*', (route) =>
    route.fulfill({
      json: {
        items: [
          {
            id: JOB_ID,
            number: detail.number,
            status: 'New',
            technicianId: null,
            totalPrice: 90,
            currency: 'EUR',
            lineCount: 1,
          },
        ],
        nextCursor: null,
      },
      status: 200,
    }),
  );

  await page.goto('/work-orders');
  await page.getByRole('link', { name: detail.number }).waitFor();

  await scan(page);
});

test('the detail has no accessibility violations', async ({ page }) => {
  await page.route(`**/api/work-orders/${JOB_ID}`, (route) =>
    route.fulfill({ json: detail, status: 200 }),
  );

  await page.goto(`/work-orders/${JOB_ID}`);
  await page.getByRole('heading', { name: detail.number }).waitFor();

  await scan(page);
});

test('the form has no accessibility violations', async ({ page }) => {
  await page.goto('/work-orders/new');
  await page.getByLabel('Customer').waitFor();

  await scan(page);
});
