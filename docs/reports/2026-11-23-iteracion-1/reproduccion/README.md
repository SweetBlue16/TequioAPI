# Reproducción en desarrollo

Use una base SQL Server Express aislada y datos ficticios de nueva creación. Las pruebas dejan sus filas de auditoría en la base de desarrollo.

En la copia temporal de esta carpeta instale Playwright y el capturador SMTP. Use una copia temporal del certificado local de desarrollo en una carpeta privada y elimine la copia de la clave al terminar. Los scripts nunca envían correo fuera de loopback.

```powershell
$stage = Join-Path $env:TEMP ('tequio-lab-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory $stage
Copy-Item .\* $stage
python -m pip install --target "$stage/python" aiosmtpd==1.4.6
npm install --prefix "$stage/browser" --ignore-scripts playwright
& "$stage/browser/node_modules/.bin/playwright.cmd" install chromium
dotnet dev-certs https --check --trust
dotnet dev-certs https --export-path "$stage/smtp.pem" --format Pem --no-password
```

Defina las siguientes variables únicamente en las consolas de desarrollo o en el gestor de secretos local. Comparta sus valores con los procesos que se iniciarán desde esas consolas.

- `TEQUIO_TEST_DATABASE` con el nombre de una base nueva y aislada. Instale los scripts 01–18 sustituyendo el contexto `TequioDB` en copias temporales.
- `TEQUIO_TEST_PASSWORD`, `TEQUIO_TEST_NEW_PASSWORD` y `TEQUIO_TEST_MASTER_PASSWORD` con contraseñas ficticias y distintas. Deben cumplir la política de caracteres de la API.
- `TEQUIO_TEST_SMTP_PASSWORD` con una contraseña de laboratorio compartida por el capturador y la API.

En consolas separadas:

```powershell
python "$stage/tequio_test_inbox.py"
Set-Location C:\Projects\TEQUIO\TequioAPI
& "$stage/run-api.ps1"
Set-Location C:\Projects\TEQUIO\TequioApp
$env:VITE_API_BASE_URL = '/api/v1'
npm run dev -- --host localhost --port 5173 --strictPort
```

Ejecute las pruebas desde otra consola. Newman 6.2.2 debe estar instalado en el prefijo npm del usuario. El directorio indicado en `$evidence` debe existir.

```powershell
node "$stage/system.cjs" $evidence
node "$stage/integration.cjs" 'C:\Projects\TEQUIO\TequioAPI' $evidence
node "$stage/admin-browser.cjs" $evidence
```

Los JSON son proyecciones de los resultados del runner, sin cuerpos, encabezados, tokens ni códigos. Mantenga local el material efímero de la bandeja SMTP.
