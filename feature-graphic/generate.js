const puppeteer = require('puppeteer');
const fs = require('fs');
const path = require('path');

const logoPath = path.join(__dirname, '..', 'src', 'Paraki.Frontend', 'wwwroot', 'icon-192.png');
const logoBase64 = fs.readFileSync(logoPath).toString('base64');

const html = `<!DOCTYPE html>
<html>
<head>
<meta charset="UTF-8">
<link href="https://fonts.googleapis.com/css2?family=Inter:wght@400;600;700;800&display=swap" rel="stylesheet">
<style>
  * { margin: 0; padding: 0; box-sizing: border-box; }
  body {
    width: 1024px;
    height: 500px;
    overflow: hidden;
    background: #0E0E0E;
    font-family: 'Inter', sans-serif;
    display: flex;
    align-items: center;
    justify-content: center;
    position: relative;
  }

  /* Background glow blobs */
  .blob {
    position: absolute;
    border-radius: 50%;
    filter: blur(80px);
    opacity: 0.18;
  }
  .blob-1 {
    width: 420px; height: 420px;
    background: #E85870;
    top: -100px; left: -80px;
  }
  .blob-2 {
    width: 320px; height: 320px;
    background: #E85870;
    bottom: -80px; right: 80px;
    opacity: 0.10;
  }
  .blob-3 {
    width: 200px; height: 200px;
    background: #ffffff;
    top: 60px; right: 200px;
    opacity: 0.03;
  }

  /* Grid lines decoration */
  .grid {
    position: absolute;
    inset: 0;
    background-image:
      linear-gradient(rgba(255,255,255,0.03) 1px, transparent 1px),
      linear-gradient(90deg, rgba(255,255,255,0.03) 1px, transparent 1px);
    background-size: 48px 48px;
  }

  /* Content */
  .content {
    position: relative;
    z-index: 10;
    display: flex;
    align-items: center;
    gap: 56px;
    padding: 0 80px;
  }

  /* Logo */
  .logo-wrap {
    width: 120px;
    height: 120px;
    border-radius: 28px;
    overflow: hidden;
    flex-shrink: 0;
    box-shadow: 0 20px 60px rgba(232, 88, 112, 0.35), 0 4px 16px rgba(0,0,0,0.5);
    border: 1px solid rgba(255,255,255,0.08);
  }
  .logo-wrap img {
    width: 100%;
    height: 100%;
    object-fit: cover;
  }

  /* Divider */
  .divider {
    width: 1px;
    height: 120px;
    background: linear-gradient(to bottom, transparent, rgba(255,255,255,0.15), transparent);
    flex-shrink: 0;
  }

  /* Text */
  .text {
    display: flex;
    flex-direction: column;
    gap: 12px;
  }
  .app-name {
    font-size: 72px;
    font-weight: 800;
    color: #F0F0F0;
    letter-spacing: -0.04em;
    line-height: 1;
  }
  .app-name span {
    color: #E85870;
  }
  .tagline {
    font-size: 19px;
    font-weight: 400;
    color: rgba(240,240,240,0.55);
    line-height: 1.4;
    letter-spacing: -0.01em;
    max-width: 400px;
  }

  /* Pills */
  .pills {
    display: flex;
    gap: 8px;
    margin-top: 6px;
    flex-wrap: wrap;
  }
  .pill {
    font-size: 11px;
    font-weight: 600;
    padding: 5px 12px;
    border-radius: 999px;
    background: rgba(232, 88, 112, 0.12);
    border: 1px solid rgba(232, 88, 112, 0.25);
    color: #E85870;
    letter-spacing: 0.03em;
  }
</style>
</head>
<body>
  <div class="blob blob-1"></div>
  <div class="blob blob-2"></div>
  <div class="blob blob-3"></div>
  <div class="grid"></div>

  <div class="content">
    <div class="logo-wrap">
      <img src="data:image/png;base64,${logoBase64}" alt="Paraki">
    </div>

    <div class="divider"></div>

    <div class="text">
      <div class="app-name">Par<span>aki</span></div>
      <div class="tagline">Descubra e avalie bicicletários e vagas para micromobilidade perto de você.</div>
      <div class="pills">
        <span class="pill">Bicicletas</span>
        <span class="pill">Patinetes</span>
        <span class="pill">Scooters</span>
        <span class="pill">Monociclos</span>
      </div>
    </div>
  </div>
</body>
</html>`;

(async () => {
  const browser = await puppeteer.launch({ args: ['--no-sandbox'] });
  const page = await browser.newPage();
  await page.setViewport({ width: 1024, height: 500, deviceScaleFactor: 2 });
  await page.setContent(html, { waitUntil: 'networkidle0' });
  await page.screenshot({
    path: path.join(__dirname, 'feature-graphic.png'),
    clip: { x: 0, y: 0, width: 1024, height: 500 }
  });
  await browser.close();
  console.log('feature-graphic.png gerado com sucesso!');
})();
