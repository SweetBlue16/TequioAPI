const { chromium } = require('./browser/node_modules/playwright');
const assert = require('node:assert/strict');
const fs = require('node:fs/promises');
const path = require('node:path');

const evidence = [];
const pageHost = 'http://localhost:5173';
const apiHost = 'http://localhost:5168/api/v1';
const runId = Date.now().toString(36);
const testPassword = process.env.TEQUIO_TEST_PASSWORD;
const out = process.argv[2];

async function getOtp(address) {
  const file = path.join(__dirname, address.split('@')[0] + '.otp');
  for (let attempt = 0; attempt < 50; attempt += 1) {
    const code = await fs.readFile(file, 'utf8').catch(() => '');
    if (/^\d{6}$/.test(code)) return code;
    await new Promise((resolve) => setTimeout(resolve, 200));
  }
  throw new Error('The isolated SMTP inbox did not receive a verification code.');
}

async function step(name, action) {
  try {
    await action();
    evidence.push({ name, status: 'passed' });
  } catch (error) {
    evidence.push({ name, status: 'failed', error: error.message.replaceAll(testPassword, '[redacted]') });
    throw error;
  }
}

async function run() {
  const browser = await chromium.launch();
  const page = await browser.newPage({ reducedMotion: 'reduce' });
  page.setDefaultTimeout(8000);
  const email = `test-buyer-${runId}@tequio.invalid`;
  try {
    await step('Register buyer through the public form', async () => {
      await page.goto(pageHost + '/registro');
      await page.getByRole('button', { name: 'Registrarme como Comprador' }).click();
      await page.getByLabel('Nombre*', { exact: true }).fill('Ana');
      await page.getByLabel('Apellido paterno*', { exact: true }).fill('Pruebas');
      await page.getByRole('button', { name: 'Continuar' }).click();
      await page.getByLabel('Correo electrónico*', { exact: true }).fill(email);
      await page.getByLabel('Número de teléfono*').fill('2281234567');
      await page.getByLabel('Fecha de nacimiento*').fill('1990-02-10');
      await page.getByRole('button', { name: 'Continuar' }).click();
      await page.getByLabel('Contraseña*', { exact: true }).fill(testPassword);
      await page.getByLabel('Confirmación de contraseña*').fill(testPassword);
      const response = page.waitForResponse((item) => item.url().endsWith('/auth/register'));
      await page.getByRole('button', { name: 'Registrarme', exact: true }).click();
      assert.equal((await response).status(), 201);
      await page.getByRole('heading', { name: 'Verificación de cuenta' }).waitFor();
    });
    await step('Reject valid credentials before email verification', async () => {
      const pending = await page.request.post(apiHost + '/auth/login', {
        data: { email, password: testPassword },
      });
      assert.equal(pending.status(), 401);
      const response = await pending.json();
      assert.equal(response.error, 'Verifica tu correo electrónico antes de iniciar sesión.');
    });
    await step('Verify the one-time code delivered over STARTTLS', async () => {
      const code = await getOtp(email);
      const group = page.getByRole('group', { name: 'Código de verificación*' });
      for (const [index, digit] of [...code].entries()) {
        await group.getByRole('textbox', { name: `Dígito ${index + 1} de 6` }).fill(digit);
      }
      const response = page.waitForResponse((item) => item.url().endsWith('/auth/verify'));
      await page.getByRole('button', { name: 'Verificar cuenta' }).click();
      assert.equal((await response).status(), 200);
      await page.waitForURL('**/login');
    });
    await step('Sign in as verified buyer', async () => {
      await page.getByLabel('Correo electrónico o número de teléfono').fill(email);
      await page.getByLabel('Contraseña', { exact: true }).fill(testPassword);
      const response = page.waitForResponse((item) => item.url().endsWith('/auth/login'));
      await page.locator('form button[type="submit"]').click();
      assert.equal((await response).status(), 200);
      await page.getByRole('link', { name: /Hola, Ana/i }).waitFor();
    });
    await step('Open the catalog and preserve buyer restrictions', async () => {
      const categoryResponse = page.waitForResponse((response) =>
        response.url().includes('/products/category/1?') && response.request().method() === 'GET');
      await page.getByRole('link', { name: 'Categorías' }).click();
      await page.getByRole('heading', { name: 'Catálogo de productos' }).waitFor();
      await page.waitForURL('**/catalogo');
      const response = await categoryResponse;
      assert.equal(response.status(), 200);
      const catalog = await response.json();
      assert.ok(catalog.products.length > 0);
      const list = page.getByRole('list', { name: 'Productos de la categoría' });
      await list.getByRole('heading', { name: catalog.products[0].name, exact: true }).waitFor();
      await page.getByRole('combobox', { name: 'Categoría' }).waitFor();
      assert.equal(await page.getByRole('link', { name: 'Panel de administración' }).count(), 0);
      await page.screenshot({ path: path.join(out, 'catalogo-comprador.png') });
      await page.goto(pageHost + '/admin/puntos-entrega');
      await page.waitForURL('**/perfil');
    });
    await step('Operate the seller terms modal by keyboard with restored focus', async () => {
      await page.goto(pageHost + '/panel-vendedor');
      const trigger = page.getByRole('button', { name: 'Darme de alta como vendedor gratis' });
      await trigger.focus();
      await page.keyboard.press('Enter');
      const dialog = page.getByRole('dialog', { name: 'Lineamientos de venta comunitaria' });
      const consent = dialog.getByRole('checkbox');
      await dialog.waitFor();
      evidence.push({ name: 'Focus after opening', element: await page.evaluate(() => document.activeElement?.outerHTML.slice(0, 130)) });
      const policies = dialog.getByRole('region', { name: 'Políticas y lineamientos para vendedores' });
      assert.equal(await policies.evaluate((element) => element === document.activeElement), true);
      await page.keyboard.press('Tab');
      assert.equal(await consent.evaluate((element) => element === document.activeElement), true, 'The expected element gained DOM focus');
      await dialog.getByRole('heading', { name: 'Lineamientos de venta comunitaria' }).click();
      assert.equal(await dialog.isVisible(), true);
      await page.keyboard.press('Escape');
      await dialog.waitFor({ state: 'hidden' });
      evidence.push({ name: 'Focus after closing', element: await page.evaluate(() => document.activeElement?.outerHTML.slice(0, 130)) });
      assert.equal(await trigger.evaluate((element) => element === document.activeElement), true, 'The expected element gained DOM focus');
      const focusOutline = await trigger.evaluate((element) => getComputedStyle(element).outlineStyle);
      assert.notEqual(focusOutline, 'none');
      await page.screenshot({ path: path.join(out, 'foco-vendedor.png') });
    });
    await step('Check the anonymous protected route redirect', async () => {
      const guest = await browser.newPage();
      await guest.goto(pageHost + '/admin/puntos-entrega');
      await guest.waitForURL('**/login');
      await guest.close();
    });
  } finally {
    await browser.close();
    await fs.writeFile(path.join(out, 'playwright.json'), JSON.stringify(evidence, null, 2));
  }
}

run().catch(() => { process.exitCode = 1; });




