# Reproducción de la Iteración 1

Abra PowerShell 7. Prepare una base nueva y vacía de SQL Server Express. Instale los scripts 01–19 en orden numérico en esa base mediante copias de los scripts con el contexto SQL de pruebas. El script 01 crea la base y fija su contexto. Cambie solo el identificador `TequioDB` en la copia temporal. Use `sqlcmd -b` para detener la instalación si falla alguna sentencia. Los ensayos de integración modifican la base de laboratorio.

## API, SQL y cobertura

```powershell
Set-Location C:\Projects\TEQUIO\TequioAPI
$evidence = (Resolve-Path 'docs/reports/2026-11-23-iteracion-1-seguimiento').Path
$rerun = Join-Path $env:TEMP ('tequio-repro-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory $rerun
$env:TEQUIO_TEST_SQL = 'Server=.\SQLEXPRESS;Database=TequioPruebasAisladas;Integrated Security=true;TrustServerCertificate=true'

dotnet build backend/Tequio.slnx -c Debug
dotnet test backend/Tequio.slnx -c Debug --no-build --logger 'trx;LogFileName=seguimiento.trx' --collect 'XPlat Code Coverage;Format=opencover' --results-directory "$rerun/dotnet"
dotnet test backend/Tequio.slnx -c Debug --no-build --filter FullyQualifiedName~BatchStockSqlTest --logger 'console;verbosity=detailed'
$openCover = (Get-ChildItem "$rerun/dotnet" -Filter coverage.opencover.xml -Recurse | Where-Object DirectoryName -Match '[a-f0-9]{8}-[a-f0-9]{4}-[a-f0-9]{4}-[a-f0-9]{4}-[a-f0-9]{12}$' | Select-Object -First 1).FullName
python "$evidence/calcular-cobertura-api.py" $openCover "$rerun/api-logic-coverage.json"
reportgenerator "-reports:$openCover" "-targetdir:$rerun/cobertura-api" '-reporttypes:Html;TextSummary'

node postman/validate-auth.cjs
node postman/validate-profiles-catalog.cjs
node postman/validate-delivery-points.cjs
dotnet list backend/Tequio.slnx package --vulnerable --include-transitive
git diff --check
```

`reportgenerator` requiere la herramienta .NET dotnet-reportgenerator-globaltool en PATH. La medición toma cada combinación única de ruta y línea instrumentada de Controllers, Validators y Services. Use Debug para la comparación con la ejecución inicial.

## App y navegación

```powershell
Set-Location C:\Projects\TEQUIO\TequioApp
npm ci --ignore-scripts
npm run lint
npm run build
npm run test:coverage -- --ci --runInBand
npm test -- --ci --runInBand --runTestsByPath src/routes/__tests__/NavigationRejection.test.tsx
npm audit --ignore-scripts
git diff --check
```

## Integración, navegador y rendimiento

Siga el [procedimiento de laboratorio](../../2026-11-23-iteracion-1/reproduccion/README.md) para preparar `$stage`, el buzón privado STARTTLS, las variables de prueba y Newman. Copie los tres runners de esta carpeta a `$stage`. Use el mismo puerto SMTP en el capturador y la API. La ejecución fue en 1026 porque 1025 estaba ocupado. Inicie la API en 5168 y Vite en 5173 con el proxy de desarrollo. Use una base que ya tenga instalados 01–19. Mantenga las autorizaciones y secretos de laboratorio en los procesos locales.

```powershell
Set-Location C:\Projects\TEQUIO\TequioAPI
$api = (Get-Location).Path
Copy-Item "$evidence/reproduccion/*.cjs" $stage
node "$stage/integration.cjs" $api $rerun
node "$stage/newman-development.cjs" $api $rerun $stage
node "$stage/newman-auth-ampliado.cjs" $api $rerun $stage
node "$stage/system.cjs" $rerun
node "$stage/admin-browser.cjs" $rerun
k6 run --summary-export "$rerun/k6.json" docs/reports/2026-11-23-iteracion-1/catalog-load.js
```

El runner de autenticación ampliada registra el HTTP y las aserciones fallidas de 01.2 en su JSON. Para revisar el resultado, abra `newman-auth-ampliado.json` y compruebe el campo `blocked` y el arreglo `assertions`. El runner administrativo necesita el administrador ficticio que crea integración. En el navegador, recorra /login con Tab y envíe el formulario vacío con Enter para observar foco y mensajes.

## Matrices y SonarCloud

Los archivos de conciliación y las instantáneas JSON son las entradas de las matrices. Use Python 3 con el árbol local y el historial de los commits base disponibles. Estos comandos regeneran las matrices de seguimiento en el mismo directorio.

```powershell
Set-Location C:\Projects\TEQUIO\TequioApp
node "$evidence/verify-frontend.cjs" $evidence
Set-Location C:\Projects\TEQUIO\TequioAPI
python "$evidence/actualizar-matrices.py"
python "$evidence/auditar-archivos.py"
```

Para consultar el análisis publicado, use las API de SonarCloud con rama `main`.

```powershell
foreach ($project in 'SweetBlue16_TequioAPI', 'SweetBlue16_TequioApp') {
    $endpoint = 'https://sonarcloud.io/api'
    $analysis = Invoke-RestMethod "$endpoint/project_analyses/search?project=$project&branch=main&ps=1"
    $issues = Invoke-RestMethod "$endpoint/issues/search?componentKeys=$project&branch=main&resolved=false&ps=500"
    [pscustomobject]@{
        Project = $project
        Branch = 'main'
        Revision = $analysis.analyses[0].revision
        OpenIssues = $issues.total
    }
}
```

