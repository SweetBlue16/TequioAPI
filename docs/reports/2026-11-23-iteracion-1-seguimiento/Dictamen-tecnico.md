# Dictamen técnico de seguimiento de la Iteración 1

**Rechazado para el cierre integral del MVP.** La cobertura de lógica API superó el criterio del 80 %. Los dos listados de SonarCloud tienen evidencia local por ocurrencia. Siguen abiertos los recorridos completos de lotes, compra y cierre, las pruebas con proveedores autorizados y la confirmación remota de los cambios locales.

La ejecución del 23 de noviembre de 2026 usó SQL Server Express con una base aislada, API Development, Chromium, Jest y un buzón STARTTLS local con cuentas ficticias. Los repositorios quedan en `main` para revisión previa al push. El [dictamen inicial](../2026-11-23-iteracion-1/Dictamen-tecnico.md) y sus evidencias se conservaron. El plan editable mantiene la numeración original 1.7 y 4.2, correspondientes a 7.1.7 y 7.4.2 de la actividad.

## Conciliación de SonarCloud

| Proyecto | Rama | Commit analizado | Quality Gate base | Ocurrencias TXT | Corregidas aquí y verificadas | Ya corregidas y verificadas | Pendientes locales de ese listado | Adicionales abiertas en remoto | Antecedentes cerrados |
| --- | --- | --- | --- | ---: | ---: | ---: | ---: | ---: | ---: |
| TequioAPI | main | `d57f966371bb42fbe04daaaf07735f1119696ee9` | OK | 91 | 8 | 83 | 0 | 0 | 0 observados |
| TequioApp | main | `7d132327f7656f5090229e2567098fd1848c522f` | ERROR | 97 | 2 | 95 | 0 | 0 | 5 |
| Total | | | | **188** | **10** | **178** | **0** | **0** | **5** |

Los dos TXT eran legibles. Las 188 marcas Open se cruzaron con la misma rama y revisión por archivo, línea y mensaje, y de allí se obtuvo una clave SonarCloud distinta por ocurrencia. F01–F22 quedaron vinculados por clave a 22 de las 97 filas de App. Las matrices registran regla, línea base, cambio comprobado, prueba local y estado remoto en columnas separadas. La comprobación local combina inspección de la transformación concreta con pruebas de los flujos, compilador, linters y validadores de cada repositorio. [API](sonar-TequioAPI-trazabilidad.tsv) · [App](sonar-TequioApp-trazabilidad.tsv) · [F01–F22](f01-f22.tsv).

El **102** inicial de App procedía del total de la consulta sin filtrar por resolución. En el mismo análisis, `resolved=false` devuelve 97 y `resolved=true` devuelve cinco. La resolución remota anterior fue FIXED para una ocurrencia de complejidad en el asistente de puntos y REMOVED para cuatro avisos CSS. Las cinco claves son disjuntas de los TXT. [Sin filtro](SweetBlue16_TequioApp-all.json) · [Abiertos](SweetBlue16_TequioApp-issues.json) · [Cerrados](sonar-antecedentes-remotos-TequioApp.tsv).

El análisis remoto consta únicamente para los commits base. Los cambios locales carecen de commit analizado. Los [análisis API](SweetBlue16_TequioAPI-analyses.json) y [App](SweetBlue16_TequioApp-analyses.json) conservan las revisiones. El cierre de cada clave y la importación de cobertura quedan pendientes del análisis de un commit que contenga los cambios. Se conservaron las reglas y los umbrales.

## Verificaciones de esta ejecución

| Verificación | Resultado | Evidencia |
| --- | --- | --- |
| Compilación API | 0 errores y 0 advertencias | [Debug](backend-build.log) |
| xUnit con SQL Server | 173 aprobadas, 0 omitidas, en Debug y Release | [Debug](xunit-debug.log) · [Release](xunit-final.log) |
| Cobertura API en lógica | 628 de 690 líneas, **91.01 %** frente a 50.23 % inicial y 80 % exigido. Mismo alcance y configuración Debug. | [Cálculo](api-logic-coverage.json) · [HTML Coverlet](cobertura-api-debug/index.html) |
| Cobertura API global | 41.5 % de líneas y 31.2 % de ramas | [Resumen](cobertura-api-debug/Summary.txt) |
| App | Build y lint aprobados. 129 Jest en 18 suites, 65.22 % de líneas y 50.46 % de ramas | [Build](frontend-build.log) · [Lint](lint.log) · [Jest](jest.log) · [HTML Jest](cobertura-app/index.html) |
| Rechazo de navegación | Cuatro pruebas de aviso visible F12, F13, F14 y F19 | [Jest](navigation-jest.log) |
| HTTP y navegador | 32 pasos HTTP, incluidos OTP, recuperación, catálogo y autorización. Flujos de comprador y administración en Chromium | [HTTP](integration-http.json) · [Comprador](playwright.json) · [Admin](playwright-admin.json) |
| Newman CP-PTO | 47 solicitudes y 83 aserciones, incluidas regla RN-07 y preservación de asociación | [Newman](newman-cp-pto.json) |
| Newman Tequio Development | 26 solicitudes y 103 aserciones aprobadas. Pasaron CP-PER-01, CP-CAT-01 y CP-CAT-03. CP-PER-02 quedó parcial | [Newman](newman-development.json) |
| Newman autenticación ampliada | Pasaron 04.1, 04.2 y 01.1. 01.2 devolvió el 401 de cuenta sin verificar. Conserva 2 aserciones fallidas. | [Newman](newman-auth-ampliado.json) |
| Validadores Postman | 21 comprobaciones de autenticación, 26 de perfiles y catálogo y estructura completa de CP-PTO | [Auth](postman-auth.log) · [Perfiles y catálogo](postman-profiles-catalog.log) · [Puntos](postman-delivery-points.log) |
| k6 catálogo | 3,950 solicitudes, 50 usuarios virtuales, p95 de 11.54 ms y 0 errores | [k6](k6.json) |
| Auditoría de paquetes | 0 vulnerabilidades notificadas en npm y NuGet | [npm](npm-audit.json) · [NuGet](nuget-audit.log) |

**Postman.** Los validadores offline invocan funciones explícitas y comparan los cuerpos con los eventos exportados. Los chequeos sintácticos usan `node --check`. Newman detectó que el script de perfiles podía redeclarar un identificador del sandbox. Se dio alcance de bloque a las aserciones de las dos carpetas ampliadas y se repitió la ejecución HTTP. Los validadores offline verifican también que las 65 solicitudes históricas y sus aserciones se conservaron. [Colisión reproducida](newman-development-colision.json).

**JWT.** La firma y la validación comparten el mismo objeto de clave. El host obtiene el material del entorno o de user-secrets en Development. Se revisaron el proveedor, la configuración, las pruebas y las referencias de código y documentación. El `appsettings.Development.json` ignorado conserva una entrada local heredada fuera de la ruta de firma. Se verificaron longitud mínima, emisión y uso de tokens con cuentas de laboratorio. [Rutas examinadas](jwt-source-paths.txt).

**T-SQL.** El árbol remoto clasificó como `plsql` los 20 archivos SQL de la base analizada. Los scripts 01–18 se instalaron en la base aislada y se aplicaron los cambios de 13 y 19. Se comprobó la migración de puntos, la compatibilidad con sus consumidores y el rechazo de baja de un producto o punto asociado a lotes. Para el nuevo apartado SQL, 10 de 50 compradores concurrentes consiguieron una de 10 unidades. Los 40 restantes recibieron conflicto. El stock terminó en cero y el reintento con la misma referencia conservó una sola orden. p95 SQL de 92.98 ms. [Lenguaje remoto](SweetBlue16_TequioAPI-files.json) · [Instalación](sql-setup.log) · [Prueba](batch-stock.log).

**Accesibilidad.** Recorrido manual con Tab, foco visible y mensajes del formulario de acceso. Las pruebas de Chromium verificaron además el modal de términos y el panel de puntos. Los fallos de navegación se comprobaron con el router montado y alertas visibles. [Revisión manual](accesibilidad-teclado.md).

## Pendientes del MVP

| Archivo o área | Impedimento observado | Validación que falta |
| --- | --- | --- |
| `TequioAPI/database/scripts/19_BatchStockReservation.sql` y capas web de ambos repositorios | La publicación y el apartado sólo están conectados a SQL Server. El estado Pending aún requiere coordinación con autorización de pago, vencimiento, reversión y cierre. | Exponer y verificar los flujos de publicación y compra, pago rechazado, expiración, cierre con y sin meta, comprobantes y entrega. Repetir RN-03 a través de API y medir CP-REN-02. |
| `TequioAPI/postman/Tequio.postman_collection.json`, grupo de autenticación 05 | La secuencia ampliada espera emitir JWT antes de verificar OTP y bloquea casos dependientes. La prueba de campo reejecutada obtuvo 401. El contrato histórico de agotamiento 403/422 difiere del 400 de la API. CP-AUT-05 y 10 carecen de pasos. | Reordenar las precondiciones sin habilitar acceso prematuro. Resolver la expectativa de códigos de agotamiento. Completar y ejecutar toda la carpeta, incluidos el token genuinamente vencido y OTP vencido. |
| `TequioAPI/postman/Tequio.postman_collection.json`, grupo 06 y `UpdateUserProfileDto.cs` | Newman confirmó los rechazos 400 por obligatoriedad y longitud y que el perfil quedó intacto. Queda por acordar el formato de teléfono de perfil. La prueba de UI existente acepta hasta 20 caracteres y el correo es de solo lectura. | Verificar CP-PER-02 completo y los casos pendientes de archivos y catálogo. |
| `CloudinaryStorageService.cs`, selección de mapas de TequioApp y futura integración de pago | El entorno disponible tuvo SMTP local, almacenamiento simulado y ninguna cuenta sandbox autorizada de los proveedores externos. | Habilitar credenciales aisladas y ejecutar cargas persistidas con limpieza, mapa real con alternativa por dirección, pagos de prueba aprobados y rechazados y correo externo cuando corresponda. |
| `TequioApp/src/pages` y `src/components` | Falta evaluación manual del resto de pantallas y lector de pantalla. En el formulario vacío el foco queda en el botón después del rechazo. | Completar recorridos con teclado y lector por rol, confirmando anuncio, foco y retorno, zoom y contraste. |
| Inventarios de código propio | Se aplicaron controles por archivo y se revisaron los puntos del listado Sonar de ambos repositorios. La revisión semántica manual integral de rutas ajenas a esos listados quedó abierta. | Revisar las rutas con marca manual pendiente y verificar la aplicación del estándar en componentes, scripts, estilos, mocks y pruebas restantes. [API](auditoria-archivos-TequioAPI.tsv) · [App](auditoria-archivos-TequioApp.tsv) |
| SonarCloud, ambos workflows | El último análisis remoto corresponde al commit base de cada repositorio. La disponibilidad de perfil T-SQL no se confirmó. | Analizar el commit que se revise y publique, cotejar claves y cobertura importada, y comprobar el perfil del dialecto. |

Los [comandos PowerShell de reproducción](reproduccion/README.md) incluyen compilación, pruebas, generación de matrices, cobertura, SQL y los runners Newman.

