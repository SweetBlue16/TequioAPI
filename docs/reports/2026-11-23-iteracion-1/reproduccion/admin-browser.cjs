const { chromium } = require('./browser/node_modules/playwright');
const { execFileSync } = require('node:child_process');
const fs = require('node:fs/promises');
const path = require('node:path');
const assert = require('node:assert/strict');

const output = process.argv[2];
const host = 'http://localhost:5173';
const evidence = [];
const email = execFileSync('sqlcmd', [
  '-S', '.\\SQLEXPRESS', '-E', '-C', '-b', '-d', process.env.TEQUIO_TEST_DATABASE, '-h', '-1', '-W', '-Q',
  "SET NOCOUNT ON; SELECT TOP (1) Email FROM dbo.[User] WHERE RoleId = 3 AND IsVerified = 1 AND Email LIKE 'test-admin-%@tequio.invalid' ORDER BY Id DESC;",
], { encoding: 'utf8' }).trim();
assert.match(email, /^test-admin-[a-z0-9]+@tequio\.invalid$/);

async function step(name, callback) {
  try {
    await callback();
    evidence.push({ name, status: 'passed' });
  } catch (error) {
    evidence.push({ name, status: 'failed', error: String(error.message).replaceAll(email, '[redacted]').replaceAll(process.env.TEQUIO_TEST_PASSWORD, '[redacted]') });
    throw error;
  }
}

async function run() {
  const browser = await chromium.launch();
  const page = await browser.newPage({ reducedMotion: 'reduce' });
  page.setDefaultTimeout(7000);
  try {
    await step('Verified Admin sees the delivery points panel', async () => {
      await page.goto(host + '/login');
      await page.getByLabel('Correo electrónico o número de teléfono').fill(email);
      await page.getByLabel('Contraseña', { exact: true }).fill(process.env.TEQUIO_TEST_PASSWORD);
      await page.locator('form button[type="submit"]').click();
      await page.waitForURL((url) => url.pathname !== '/login');
      await page.goto(host + '/admin/puntos-entrega');
      await page.getByRole('list', { name: 'Listado de puntos de entrega autorizados' }).waitFor();
    });
    await step('A native button opens the point and is independent of its secondary controls', async () => {
      const list = page.getByRole('list', { name: 'Listado de puntos de entrega autorizados' });
      const item = list.getByRole('listitem').first();
      const details = item.getByRole('button', { name: /Ver detalles/ });
      await details.focus();
      await page.keyboard.press('Enter');
      const back = page.getByRole('button', { name: 'Regresar' });
      await back.waitFor();
      await back.click();
      await item.getByRole('button', { name: /Editar/ }).click();
      await page.waitForURL('**/admin/puntos-entrega/edicion/*');
      await page.getByRole('heading', { name: 'Editar punto de entrega' }).waitFor();
    });
    await step('ComboBox uses column layout and missing coordinates block each branch', async () => {
      await page.getByRole('button', { name: 'Continuar' }).click();
      const municipality = page.getByRole('combobox', { name: /municipio/i });
      const direction = await municipality.evaluate((select) =>
        getComputedStyle(select.closest('select').parentElement.parentElement).flexDirection);
      assert.equal(direction, 'column');
      const latitude = page.getByLabel('Latitud*');
      const longitude = page.getByLabel('Longitud*');
      const stepButton = page.getByRole('button', { name: 'Continuar' });
      await page.getByLabel('Calle*', { exact: true }).fill('Enriquez');
      await page.getByLabel('Colonia*', { exact: true }).fill('Centro');
      await page.getByLabel('Número*', { exact: true }).fill('1');
      await page.getByLabel('C.P.*', { exact: true }).fill('91000');
      await municipality.selectOption('Xalapa');
      await latitude.fill('19.53');
      await longitude.fill('-96.92');
      const savedLatitude = await latitude.inputValue();
      const savedLongitude = await longitude.inputValue();
      await latitude.fill('');
      await stepButton.click();
      await latitude.waitFor();
      await page.getByText('El marcador debe ubicarse dentro de Xalapa o municipios cercanos.').waitFor();
      assert.equal(await latitude.inputValue(), '');
      await latitude.fill(savedLatitude);
      await longitude.fill('');
      await stepButton.click();
      await longitude.waitFor();
      await page.getByText('El marcador debe ubicarse dentro de Xalapa o municipios cercanos.').waitFor();
      assert.equal(await longitude.inputValue(), '');
      await longitude.fill(savedLongitude);
      await stepButton.click();
      await page.getByRole('heading', { name: 'Resumen edición punto' }).waitFor();
    });
    await step('Saving changes returns to a consistent list', async () => {
      const result = page.waitForResponse((response) =>
        response.url().includes('/delivery-points/') && response.request().method() === 'PUT');
      await page.getByRole('button', { name: 'Guardar cambios' }).click();
      assert.equal((await result).status(), 200);
      await page.getByRole('status').filter({ hasText: 'Cambios guardados con éxito.' }).waitFor();
      await page.waitForURL('**/admin/puntos-entrega');
      await page.getByRole('list', { name: 'Listado de puntos de entrega autorizados' }).waitFor();
    });
    await step('The invalid edit ID redirects to the list', async () => {
      await page.goto(host + '/admin/puntos-entrega/edicion/invalido');
      await page.waitForURL('**/admin/puntos-entrega');
    });
    await step('Register a new point and read its success notification on the list', async () => {
      const name = 'TEST_UI_' + Date.now().toString(36);
      await page.getByRole('button', { name: 'Agregar punto de entrega' }).click();
      await page.waitForURL('**/admin/puntos-entrega/nuevo');
      await page.getByLabel('Nombre del punto*', { exact: true }).fill(name);
      await page.getByRole('button', { name: 'Continuar' }).click();
      await page.getByLabel('Calle*', { exact: true }).fill('Enriquez');
      await page.getByLabel('Colonia*', { exact: true }).fill('Centro');
      await page.getByLabel('Número*', { exact: true }).fill('1');
      await page.getByLabel('C.P.*', { exact: true }).fill('91000');
      await page.getByRole('combobox', { name: /municipio/i }).selectOption('Xalapa');
      await page.getByLabel('Latitud*').fill('19.53');
      await page.getByLabel('Longitud*').fill('-96.92');
      await page.getByRole('button', { name: 'Continuar' }).click();
      const result = page.waitForResponse((response) =>
        response.url().endsWith('/delivery-points') && response.request().method() === 'POST');
      await page.getByRole('button', { name: 'Confirmar registro' }).click();
      assert.equal((await result).status(), 201);
      await page.waitForURL('**/admin/puntos-entrega');
      await page.getByRole('status').filter({ hasText: 'Punto de entrega registrado con éxito.' }).waitFor();
      await page.getByRole('button', { name: 'Ver detalles de ' + name }).waitFor();
    });
  } finally {
    await browser.close();
    await fs.writeFile(path.join(output, 'playwright-admin.json'), JSON.stringify(evidence, null, 2));
  }
}

run().catch(() => { process.exitCode = 1; });





