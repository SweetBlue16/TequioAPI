const { chromium } = require('./browser/node_modules/playwright');
const fs = require('node:fs/promises');
const path = require('node:path');

function luminance(color) {
  const channels = color.match(/\d+(?:\.\d+)?/g).slice(0, 3).map((value) => Number(value) / 255);
  const [r, g, b] = channels.map((value) => value <= 0.04045 ? value / 12.92 : ((value + 0.055) / 1.055) ** 2.4);
  return 0.2126 * r + 0.7152 * g + 0.0722 * b;
}

(async () => {
  const browser = await chromium.launch();
  try {
    const page = await browser.newPage();
    await page.goto('http://localhost:5173/login');
    const submit = page.locator('form button[type="submit"]');
    await submit.waitFor();
    const colors = await submit.evaluate((element) => {
      const { color, backgroundColor } = getComputedStyle(element);
      return { text: color, background: backgroundColor };
    });
    const levels = [luminance(colors.text), luminance(colors.background)].sort((a, b) => b - a);
    const ratio = (levels[0] + 0.05) / (levels[1] + 0.05);
    await fs.writeFile(path.join(process.argv[2], 'playwright-contraste.json'), JSON.stringify({
      page: '/login', control: 'Iniciar sesión', colors, ratio, expectedMinimum: 4.5, passed: ratio >= 4.5,
    }, null, 2));
    if (ratio < 4.5) process.exitCode = 1;
  } finally {
    await browser.close();
  }
})().catch(() => { process.exitCode = 1; });
