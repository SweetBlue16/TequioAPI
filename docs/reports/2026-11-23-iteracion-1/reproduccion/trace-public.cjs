const { chromium } = require('./browser/node_modules/playwright');
const path = require('node:path');

(async () => {
  const browser = await chromium.launch();
  try {
    const context = await browser.newContext({ reducedMotion: 'reduce' });
    await context.tracing.start({ screenshots: true, snapshots: true, sources: false });
    const page = await context.newPage();
    await page.goto('http://localhost:5173/registro');
    await page.getByRole('button', { name: 'Registrarme como Comprador' }).click();
    await page.getByLabel('Nombre*', { exact: true }).waitFor();
    await context.tracing.stop({ path: path.join(process.argv[2], 'playwright-registro-publico.zip') });
    await context.close();
  } finally {
    await browser.close();
  }
})().catch((error) => {
  console.error(error.message);
  process.exitCode = 1;
});
