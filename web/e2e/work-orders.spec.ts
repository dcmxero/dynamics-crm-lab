import { expect, Page, test } from '@playwright/test';

/**
 * The API is stubbed at the network layer, so these tests check the client:
 * routing, the states a screen goes through, and how a refused action reads.
 * What the server does with the request is covered by the API tests.
 */

const JOB_ID = '3fa85f64-5717-4562-b3fc-2c963f66afa6';

const summary = {
  id: JOB_ID,
  number: 'WO-20260901-ABCDEF',
  status: 'New',
  technicianId: null,
  totalPrice: 120,
  currency: 'EUR',
  lineCount: 2,
};

const detail = {
  ...summary,
  customerId: '11111111-1111-1111-1111-111111111111',
  equipmentId: '22222222-2222-2222-2222-222222222222',
  technicianName: null,
  resolution: null,
  lines: [
    {
      id: '33333333-3333-3333-3333-333333333333',
      description: 'Technician labour',
      quantity: 2,
      unitPrice: 45,
      lineTotal: 90,
    },
    {
      id: '44444444-4444-4444-4444-444444444444',
      description: 'Filter',
      quantity: 1,
      unitPrice: 30,
      lineTotal: 30,
    },
  ],
};

async function stubList(
  page: Page,
  jobs: unknown[],
  nextCursor: string | null = null,
): Promise<void> {
  await page.route('**/api/work-orders?*', (route) =>
    route.fulfill({ json: { items: jobs, nextCursor }, status: 200 }),
  );
}

async function stubDetail(page: Page): Promise<void> {
  await page.route(`**/api/work-orders/${JOB_ID}`, (route) =>
    route.fulfill({ json: detail, status: 200 }),
  );
}

test('lists the jobs that have reached a stage', async ({ page }) => {
  await stubList(page, [summary]);

  await page.goto('/');

  await expect(page.getByRole('heading', { name: 'Work orders' })).toBeVisible();
  await expect(page.getByRole('link', { name: summary.number })).toBeVisible();
});

test('reads the next page when there is one', async ({ page }) => {
  const second = {
    ...summary,
    id: '4fa85f64-5717-4562-b3fc-2c963f66afa6',
    number: 'WO-20260902-BBBBBB',
  };

  await page.route('**/api/work-orders?*', (route) => {
    const asked = new URL(route.request().url()).searchParams.get('cursor');

    return route.fulfill({
      json: asked
        ? { items: [second], nextCursor: null }
        : { items: [summary], nextCursor: 'more' },
      status: 200,
    });
  });

  await page.goto('/work-orders');

  await expect(page.getByRole('link', { name: summary.number })).toBeVisible();

  await page.getByRole('button', { name: 'Load more' }).click();

  // The page that was already read stays; the next one is added to it.
  await expect(page.getByRole('link', { name: summary.number })).toBeVisible();
  await expect(page.getByRole('link', { name: second.number })).toBeVisible();
  await expect(page.getByRole('button', { name: 'Load more' })).toBeHidden();
});

test('says so when nothing has reached the stage', async ({ page }) => {
  await stubList(page, []);

  await page.goto('/work-orders');

  await expect(page.getByText('No work orders have reached this stage.')).toBeVisible();
});

test('offers a retry when the service does not answer', async ({ page }) => {
  await page.route('**/api/work-orders?*', (route) => route.abort('failed'));

  await page.goto('/work-orders');

  await expect(page.getByRole('alert')).toContainText('did not respond');
  await expect(page.getByRole('button', { name: 'Try again' })).toBeVisible();
});

test('opens a job from the list and shows its charges', async ({ page }) => {
  await stubList(page, [summary]);
  await stubDetail(page);

  await page.goto('/work-orders');
  await page.getByRole('link', { name: summary.number }).click();

  await expect(page).toHaveURL(new RegExp(`/work-orders/${JOB_ID}$`));
  await expect(page.getByRole('heading', { name: summary.number })).toBeVisible();
  await expect(page.getByRole('cell', { name: 'Technician labour' })).toBeVisible();
});

test('shows what the job refused rather than an error page', async ({ page }) => {
  await page.route(`**/api/work-orders/${JOB_ID}`, (route) =>
    route.fulfill({ json: { ...detail, status: 'InProgress' }, status: 200 }),
  );
  await page.route(`**/api/work-orders/${JOB_ID}/closure`, (route) =>
    route.fulfill({
      status: 422,
      json: {
        title: 'The job does not allow this',
        detail: 'A closed work order cannot be closed again.',
      },
    }),
  );

  await page.goto(`/work-orders/${JOB_ID}`);
  await page.getByLabel('Resolution').fill('Replaced the compressor seal.');
  await page.getByRole('button', { name: 'Close the job' }).click();

  await expect(page.getByText('A closed work order cannot be closed again.')).toBeVisible();
});

test('offers closing only a job that has been started', async ({ page }) => {
  await stubDetail(page);

  await page.goto(`/work-orders/${JOB_ID}`);

  await expect(page.getByRole('button', { name: 'Close the job' })).toBeDisabled();
});

test('names the technician on the job rather than identifying them', async ({ page }) => {
  await page.route(`**/api/work-orders/${JOB_ID}`, (route) =>
    route.fulfill({
      json: {
        ...detail,
        status: 'Assigned',
        technicianId: '55555555-5555-5555-5555-555555555555',
        technicianName: 'Zuzana Bielikova',
      },
      status: 200,
    }),
  );

  await page.goto(`/work-orders/${JOB_ID}`);

  await expect(page.getByText('Zuzana Bielikova')).toBeVisible();
});

test('offers starting the work only once somebody is on the job', async ({ page }) => {
  await stubDetail(page);

  await page.goto(`/work-orders/${JOB_ID}`);

  await expect(page.getByRole('button', { name: 'Start the work' })).toBeDisabled();
});

test('starts the work on an assigned job', async ({ page }) => {
  await page.route(`**/api/work-orders/${JOB_ID}`, (route) =>
    route.fulfill({
      json: {
        ...detail,
        status: 'Assigned',
        technicianId: '55555555-5555-5555-5555-555555555555',
        technicianName: 'Zuzana Bielikova',
      },
      status: 200,
    }),
  );
  await page.route(`**/api/work-orders/${JOB_ID}/start`, (route) =>
    route.fulfill({
      json: { id: JOB_ID, number: summary.number, status: 'InProgress' },
      status: 200,
    }),
  );

  await page.goto(`/work-orders/${JOB_ID}`);
  await page.getByRole('button', { name: 'Start the work' }).click();

  await expect(page.getByText(`Work started on ${summary.number}.`)).toBeVisible();
});

test('keeps an empty resolution from being submitted', async ({ page }) => {
  await page.route(`**/api/work-orders/${JOB_ID}`, (route) =>
    route.fulfill({ json: { ...detail, status: 'InProgress' }, status: 200 }),
  );

  await page.goto(`/work-orders/${JOB_ID}`);
  await page.getByRole('button', { name: 'Close the job' }).click();

  await expect(page.getByText('A closed job needs an account of the work.')).toBeVisible();
});

test('raises a job and lands on it', async ({ page }) => {
  await stubDetail(page);
  await page.route('**/api/work-orders', (route) => {
    if (route.request().method() !== 'POST') {
      return route.continue();
    }

    return route.fulfill({
      status: 201,
      json: {
        id: JOB_ID,
        number: summary.number,
        status: 'New',
        totalPrice: 90,
        currency: 'EUR',
      },
    });
  });

  await page.goto('/work-orders/new');

  await page.getByLabel('Customer').fill(detail.customerId);
  await page.getByLabel('Equipment').fill(detail.equipmentId);
  await page.getByLabel('Description').fill('Technician labour');
  await page.getByLabel('Quantity').fill('2');
  await page.getByLabel('Unit price').fill('45');

  await page.getByRole('button', { name: 'Raise the job' }).click();

  await expect(page).toHaveURL(new RegExp(`/work-orders/${JOB_ID}$`));
});

test('rejects an identifier that is not one', async ({ page }) => {
  await page.goto('/work-orders/new');

  await page.getByLabel('Customer').fill('not-an-identifier');
  await page.getByRole('button', { name: 'Raise the job' }).click();

  await expect(page.getByText('Enter the identifier of an existing customer.')).toBeVisible();
});
