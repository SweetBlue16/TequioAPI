# Dictamen técnico de Iteración 1

**Rechazada para cierre técnico integral.** Los recorridos documentados de identidad, perfil, catálogo y puntos de entrega pasaron localmente. Quedan escenarios críticos sin ejecutar, cobertura API inferior a 80 % y confirmación de SonarCloud pendiente para los cambios locales.

Ejecución del 23 de noviembre de 2026 en Windows, zona America/Mexico_City, .NET SDK 10.0.401, Node 24.13.1, npm 11.8.0 y SQL Server Express. Se usaron bases aisladas `TequioIteracion1_20261123` para integración y `TequioIteracion1_Install_20261123` para instalación y regresión. La API escuchó en `localhost:5168`, Vite en `localhost:5173` y el buzón STARTTLS de pruebas en loopback. Las evidencias del 16 de noviembre y la configuración local del usuario se conservaron. Las cuentas y los registros creados durante esta ejecución son ficticios.

## Actividades

| Actividad | Estado | Evidencia |
| --- | --- | --- |
| FIX TequioAPI | Parcial. Se corrigieron excepciones, diagnóstico, origen de clave JWT, autorización y respuestas de conflicto del catálogo, cobertura CI, estilo de migraciones y validadores Postman. Quedan ocurrencias del validador dinámico y revisión exhaustiva de estándar. | [Inventario API](inventario-TequioAPI.tsv), [Sonar API](sonar-TequioAPI-trazabilidad.tsv) |
| FIX TequioApp | F01–F22 intervenidos y trazados. Restan comprobaciones por rama y confirmación en el analizador. | [F01–F22](f01-f22.tsv), [inventario App](inventario-TequioApp.tsv), [Sonar App](sonar-TequioApp-trazabilidad.tsv) |
| TESTS 6.2.1 | Ejecutada parcialmente y documentada. Dictamen integral rechazado por los criterios del plan. | [Plan y escenarios actualizados](Plan_y_Escenarios_de_Pruebas_Tequio_Iteracion_1.docx) |

## Ejecuciones por suite

| Suite | Resultado obtenido | Evidencia |
| --- | --- | --- |
| Línea base | 151 xUnit con SQL, 99 Jest. Lint inicial con 5 errores. | [xUnit base](backend-sql-baseline.trx), [Jest base](jest-baseline.json) |
| xUnit y SQL | 155 de 155. Incluye reglas RN-07, protección administrativa, consumo de intentos y concurrencia de alta duplicada. | [TRX](backend-verificado/xunit.trx), [salida](backend-verificado.log) |
| Instalación y actualización SQL | Scripts 01–18 en base nueva. Reejecución de 15–18 en la base de integración con datos. | [Instalación](sql-install.log) |
| Frontend | Lint y build con salida 0. Jest 125 de 125 en 17 suites. Vite avisa sobre el tamaño del paquete principal. | [Build](frontend-build-final.log), [Jest](jest-final.json) |
| Newman | CP-PTO con 47 solicitudes y 83 aserciones, 0 fallos. Reglas de rol, alta, actualización, baja lógica y rechazo RN-07. | [Exportación saneada](newman-cp-pto.json) |
| API real complementaria | Alta con OTP, autorización de Admin, agotamiento y reenvío, persistencia de perfil y catálogo, y respuestas de autorización. Los casos ejecutados están enumerados en el JSON. | [HTTP](integration-http.json), [primera detección 400](integration-http-first.json) |
| Sistema Playwright | Registro público de comprador, rechazo previo a OTP, código recibido por SMTP local, login y exploración del catálogo con productos visibles de la API. Admin también ejecutó alta, edición y regreso al listado con mensajes de éxito. | [Comprador](playwright.json), [Admin](playwright-admin.json), [traza pública](playwright-registro-publico.zip) |
| Accesibilidad | Comprobación automatizada de nombres, foco y teclado en Chromium. El botón de ingreso obtuvo contraste 8.01:1. Pendiente revisión manual exhaustiva. | [Playwright](playwright.json), [contraste](playwright-contraste.json), [Jest](jest-final.json) |
| Rendimiento | GET de catálogo con 50 usuarios virtuales durante 20 s, 3 productos base en total y 1 activo en la categoría consultada. 3,939 solicitudes, p95 12.17 ms y 0 % de errores. Cumple los umbrales ordinarios de 800 ms y 2 % para esta operación. | [k6](k6.json), [volumen](k6-data.txt) |

Los validadores Postman fuera de línea registraron 21 comprobaciones de autenticación, 26 de perfiles y catálogo, y validación estructural de 47 solicitudes CP-PTO. La suite principal de Postman contiene fases interactivas, entre ellas caducidad natural del JWT y carga en almacenamiento externo. La evidencia HTTP complementaria no sustituye la ejecución completa de esa colección.

## Cobertura

| Métrica | Resultado | Evidencia |
| --- | --- | --- |
| Coverlet global | 29.4 % de líneas y 22.0 % de ramas | [HTML](backend-verificado/coverage-html/index.html) |
| Controladores, validadores y servicios | 333 de 663 líneas instrumentadas, 50.23 % frente a 80 % del plan | [Cálculo](api-logic-coverage.json) |
| Jest | 43.85 % de líneas, 44.61 % de ramas, 44.21 % de sentencias | [HTML](coverage-frontend/index.html) |
| Requisitos | Evidencia parcial para identidad, perfil, catálogo y puntos. Pendientes escenarios de lotes, apartados, cierre, proveedores externos y matriz completa. | [Plan](Plan_y_Escenarios_de_Pruebas_Tequio_Iteracion_1.docx) |

## SonarCloud por revisión

| Proyecto | Rama | Último commit analizado | Quality Gate | Abiertos |
| --- | --- | --- | --- | --- |
| SweetBlue16_TequioAPI | main | `d57f966371bb42fbe04daaaf07735f1119696ee9` | OK | 91 |
| SweetBlue16_TequioApp | main | `7d132327f7656f5090229e2567098fd1848c522f` | ERROR, seguridad de código nuevo | 102 |

Estos resultados corresponden a los commits base. La consulta posterior devolvió las mismas revisiones y resultados. Los 22 hallazgos iniciales de App constaban abiertos en esa línea base. El árbol de trabajo queda local, sin commit ni push, por lo que el escáner remoto aún no ha validado las correcciones ni la importación de cobertura. Los JSON `SweetBlue16_*` conservan la respuesta de análisis, Quality Gate, issues y hotspots de la consulta. [API en SonarCloud](https://sonarcloud.io/project/overview?id=SweetBlue16_TequioAPI) y [App en SonarCloud](https://sonarcloud.io/project/overview?id=SweetBlue16_TequioApp).

Las dos definiciones YAML y los 47 archivos CSS existentes se pudieron analizar localmente. La auditoría NuGet final no reportó dependencias vulnerables en los orígenes configurados. [Validación estática](static-css-workflows.json) y [NuGet](nuget-audit.log).

El análisis base indexó `.sql` como PL/SQL. Conserva sus hallazgos visibles. La validación de sintaxis y comportamiento de T-SQL se ejecutó con SQL Server y xUnit. Falta confirmar disponibilidad y asignación del perfil T-SQL en SonarCloud antes de atribuirle cobertura de reglas específicas del dialecto.

## Defectos y bloqueos

| ID | Severidad | Ubicación | Estado y evidencia |
| --- | --- | --- | --- |
| D-01 | Mayor | Autenticación API | Login previo a OTP corregido. Los roles públicos ya requieren verificación, comprobado en xUnit y Playwright. |
| D-02 | Mayor | Autorización API de productos | 400 inicial para comprador. Reejecución 403 con el rol de productor exigido por el controlador. |
| D-03 | Mayor | Baja de producto | El conflicto documentado se asigna a HTTP 409. Las comprobaciones actuales de conflicto y estado se registran en el JSON HTTP. |
| D-04 | Mayor | Cobertura API | 50.23 % en lógica del alcance medido frente a 80 % del plan. Pendiente. |
| D-05 | Bloqueante para el cierre | Lotes y apartados | Pendientes publicación, cierre y concurrencia de apartados por falta de flujo ejecutable en los repositorios. La concurrencia de puntos no cubre sobreventa. |
| D-06 | Mayor | Integraciones | Carga real de imagen y pruebas de mapa con proveedor quedan pendientes. Se usó un buzón real de laboratorio sin salida a terceros. |
| D-08 | Mayor | Ruta pública del catálogo | El enlace regresaba al inicio en el commit base. Se añadió una vista de lectura por categoría. Chromium confirmó el GET 200 y el nombre de un producto del resultado visible en la página. |
| D-07 | Mayor | SonarCloud | Quedan validadores Postman que ejecutan scripts de la colección para sus simulaciones. Su refactorización y la confirmación remota por commit siguen pendientes. |

El Word disponible en Descargas identifica las secciones como «1.7 Matriz de trazabilidad» y «4.2 Plantilla de ejecución». La copia editable conserva esa numeración, los títulos, columnas e IDs del original. Corresponden a las referencias 7.1.7 y 7.4.2 de la solicitud.

## Comandos reproducibles

API, desde `C:\Projects\TEQUIO\TequioAPI`, con una base SQL aislada que tenga 01–18 instalados:

```powershell
$env:TEQUIO_TEST_SQL = 'Server=.\SQLEXPRESS;Database=TequioIteracion1_Install_20261123;Integrated Security=true;TrustServerCertificate=true'
dotnet build backend/Tequio.slnx
dotnet test backend/Tequio.slnx --no-build --collect 'XPlat Code Coverage' --logger trx --results-directory TestResults -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=opencover
node postman/validate-auth.cjs
node postman/validate-profiles-catalog.cjs
node postman/validate-delivery-points.cjs
```

Para arrancar la API, configure `TEQUIO_JWT_SIGNING_KEY` desde el gestor de secretos del entorno o `Jwt:Key` en user-secrets para Development. La clave debe contener al menos 32 bytes generados criptográficamente y será la misma para firma y validación. Un archivo appsettings local ya no se usa como fuente de firma. Use solo cuentas, almacenamiento y correo autorizados para pruebas.

App, desde `C:\Projects\TEQUIO\TequioApp`:

```powershell
npm ci --ignore-scripts
npm run lint
npm run build
npm run test:coverage -- --ci --runInBand
```

La instalación limpia no requirió ejecutar lifecycle scripts. Los binarios nativos se resolvieron a través de las dependencias opcionales del lockfile. Workflow y acciones quedaron fijados a SHA. El escaneo .NET recibe OpenCover y el de App LCOV. Las ejecuciones sin los secretos de GitHub conservan las pruebas en PR de forks; el análisis del proyecto debe ejecutarse en un contexto con el secreto.

Para reproducir la carga use `k6 run --summary-export k6.json catalog-load.js` desde esta carpeta, con la API de desarrollo conectada. La colección original del runner está en [Tequio.CP-PTO](../../../postman/Tequio.CP-PTO.postman_collection.json). Siga el [procedimiento del entorno de desarrollo](../../../docs/PUNTOS_ENTREGA.md) para preparar los tokens de prueba y el fixture RN-07. Los reportes compartibles omiten encabezados, cuerpos, credenciales, JWT y OTP.

Los [scripts de reproducción](reproduccion/README.md) documentan el buzón STARTTLS, Chromium y el runner de datos ficticios.

Referencias de CI: [acción oficial y tag 8.3.0](https://github.com/SonarSource/sonarqube-scan-action/releases/tag/v8.3.0), [cobertura .NET y OpenCover](https://docs.sonarsource.com/sonarqube-server/analyzing-source-code/test-coverage/dotnet-test-coverage).

La auditoría npm inicial registró 18 alertas transitivas de desarrollo a partir de glob y minimatch. Se fijaron versiones corregidas y se regeneró el lockfile. La auditoría posterior registró 0 vulnerabilidades. [Auditoría inicial](npm-audit-inicial.json) y [Auditoría npm](npm-audit.json).
