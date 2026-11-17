# Puntos físicos de entrega — 5.11, 5.12, 5.13 y 6.2.1

## Cierre actualizado — 16 de noviembre de 2026

Las seis pruebas manuales fueron **realizadas y aprobadas por el usuario**, según su confirmación explícita
en este chat. El agente registra el resultado; no se atribuye su ejecución ni una inspección visual propia.
No se amplían los casos informados con subcasos que el usuario no mencionó.

| Prueba manual | Responsable | Resultado |
|---|---|---|
| Persistencia al recargar | Usuario | Aprobado |
| Edición | Usuario | Aprobado |
| Mapa | Usuario | Aprobado |
| Rechazo de duplicados | Usuario | Aprobado |
| Desactivación | Usuario | Aprobado |
| Acceso desde el menú | Usuario | Aprobado |

Registro de la declaración: [manual-user_2026-11-16.json](../postman/evidence/manual-user_2026-11-16.json).
La desactivación informada no se presenta como una nueva prueba manual de RN-07; su cobertura continúa
respaldada por SQL y CP-PTO automatizados, incluidos los lotes Active, Confirmed y Vigente.

### Estado de las cuatro actividades

| Actividad | Estado al cierre | Evidencia |
|---|---|---|
| 5.11 — SQL | Implementada, instalada y verificada | Scripts 15–17, corrección de autorización 18; 16 pruebas SQL dentro de la suite completa (9 de puntos y 7 de autorización) |
| 5.12 — API C# | Implementada y verificada | CRUD persistido real, RN-06/RN-07, autorización por OTP, CP-PTO y revisión manual del usuario |
| 5.13 — xUnit/Jest | Implementada y verificada | 151 xUnit aprobadas, 0 omitidas; 21 pruebas frontend afectadas en 6 suites aprobadas; corrida completa previa de 99 Jest conservada |
| 6.2.1 — CP-PTO | Implementada y verificada nuevamente | 47 solicitudes y 83 aserciones aprobadas con los cambios actuales de JWT; API real en puerto 5169 y SQL aislado; limpieza aprobada |

### Reportes conservados de esta ejecución

- [Resumen estructurado](reports/2026-11-16-manual-closeout/summary.json).
- [Reporte xUnit completo TRX](reports/2026-11-16-manual-closeout/backend.trx) y [salida de ejecución](reports/2026-11-16-manual-closeout/backend.log).
- [Ejecución CP-PTO y comprobaciones adicionales](reports/2026-11-16-manual-closeout/cp-pto.json), sin tokens ni contraseñas.
- Frontend: `TequioApp/docs/reports/2026-11-16-manual-closeout/frontend.json` y `frontend.log`, con los 21 casos afectados de Navbar, login, rutas protegidas, decodificación y gestión del token.
- Lint frontend: `lint-affected.log`, `lint-global.log` y `lint-status.json` en esa misma carpeta.

La prueba CP-PTO se repitió sin sustituir la conexión de la instancia normal ni tocar los datos creados por
el usuario: usó `TequioDeliveryTest_20261116_180106` y otro proceso de API temporal. Sus datos TEST_ se
limpiaron lógicamente conservando asociaciones; el proceso temporal se detuvo. La API normal y su proxy
continuaron respondiendo 200. Las compilaciones previas siguen aprobadas; en este cierre solo se modificaron
documentación y reportes, sin repetir compilación innecesariamente.

### Pendientes reales

**No quedan bloqueos funcionales ni verificaciones pendientes dentro de 5.11, 5.12, 5.13 y 6.2.1.**
La configuración local y la autorización Admin están resueltas; las pruebas manuales del mapa y del CRUD
ya están informadas y aprobadas por el propietario.

Fuera del cierre funcional permanecen cinco errores preexistentes de ESLint global (reconfirmados en esta
corrida: jest.setup, referencias de tipos en pruebas de login/registro, ROLE_ID y escape en validador).
El lint de los archivos de autorización/navegación afectados pasó. También permanecen los avisos previos
de NuGet sobre System.Security.Cryptography.Pkcs 10.0.0 y los avisos Vite de bundle/configuración, que no
se corrigieron en esta tarea. Sus resultados no se cuentan como aprobados ni se ocultan.

No se hicieron commits ni push. Se conservaron SMTP, secretos y avances existentes.

## Auditoría inicial y decisiones (histórico)

Auditoría realizada el 16 de noviembre de 2026. Ambos repositorios comenzaron sin cambios locales.
Se conservaron las pantallas de administración, el asistente, sus estilos, Mapbox y los flujos previos.

- Backend .NET 10: API → servicios de dominio → repositorios ADO.NET/EF Core → procedimientos SQL Server.
  Se reutilizan DI, JWT y `{error}` / `{mensaje}`. Los scripts numerados se instalan manualmente;
  no se encontró un sistema de migraciones automático. xUnit y Moq ya estaban disponibles.
- Existían `DeliveryPoint`, `Batch.DeliveryPointId` y `Batch.ProducerId`, sin procedimientos ni endpoints
  de puntos. El productor elige del catálogo compartido mediante su lote; no se inventó una relación
  de propiedad directa ni se concedió gestión al Productor.
- RN-06 reserva gestión a `Admin` (rol 3). El JWT existente usa `Buyer`, `Producer`, `Admin`, `MasterAdmin`.
  El Administrador maestro conserva el aprovisionamiento actual, su OTP y el mecanismo de autorización
  de administradores. No se amplió el registro público ni se concedió implícitamente RN-06 a MasterAdmin.
- Frontend React 19 / TypeScript / Vite: ya existían `/admin/puntos-entrega`, alta, edición, detalle,
  selección Mapbox, geocodificación regional, Toast, Jest y Testing Library. El servicio usaba un mock
  y `/admin/*` no estaba protegido. Ahora consume el cliente HTTP autenticado y exige rol 3.
- `decodeSession` esperaba `sub/roleId` mientras que el proveedor JWT emite las URI de claims de .NET:
  ahora acepta también esos claims, preservando el formato anterior. La API continúa validando la firma.
- Configuración: conexión, JWT, Cloudinary y SMTP presentes en configuración local/secretos, sin mostrar
  valores ni modificar archivos de configuración o secretos. La conexión efectiva señala `127.0.0.1`,
  base `TequioDB`; en la auditoría inicial no respondió (SqlException 2) y Docker estaba detenido.
  En la auditoría inicial no había MasterAdmin configurado. Esa situación ya se resolvió: Docker/SQL
  responden, se instalaron 15–18 y las cuentas MasterAdmin/Admin quedaron configuradas y autorizadas
  mediante el flujo existente, sin modificar SMTP ni publicar sus valores.
- Frontend: `VITE_API_BASE_URL=/api/v1`, proxy existente hacia `http://localhost:5168`.
  Inicialmente no había `VITE_MAPBOX_ACCESS_TOKEN` local; el propietario ya lo configuró. El mapa
  ofrece un estado alternativo cuando falta el token y se pueden
  introducir coordenadas por teclado. `.env.example` no contiene secretos y `.env.local` sigue excluido.
  No se probaron proveedores externos de correo, imágenes ni mapas.

**Duplicado (decisión explícita propuesta para esta entrega):** mismo municipio y mismo nombre tras
quitar espacios exteriores, ignorando mayúsculas y acentos (`Latin1_General_100_CI_AI`). No había definición
previa documentada. Incluye puntos inactivos; la dirección y las coordenadas no sustituyen esa identidad.
El índice único protege altas y actualizaciones concurrentes. Si negocio cambia la definición, se debe
migrar el índice y ajustar CP-PTO; no es una regla histórica encontrada en el proyecto.

**RN-07 — fuente comprobada:** en la respuesta del usuario al formulario de aclaración de este mismo chat
se recibió literalmente: «Que no se pueda desactivar ningun punto con lotes activos, confimados o vigentes».
Esa respuesta posterior precisó el requisito inicial de bloquear cualquier asociación; no provino de un
archivo ni fue una excepción inferida. Se conserva el bloqueo de activos, confirmados o vigentes. El procedimiento previo de productos usa `Draft`, `Active`, `Confirmed`;
no existe un catálogo de estados de lote ni su CRUD todavía. Se consideran terminales únicamente
`Cancelled`, `Canceled`, `Cancelado`, `Completed`, `Finalizado`, `Entregado` (sin distinguir mayúsculas/acentos).
Cualquier otro estado, incluidos borradores y estados desconocidos, sigue vigente y bloquea la baja.
No se usa únicamente la fecha límite para liberar un punto: un lote confirmado puede seguir pendiente.
El rechazo devuelve **409** y conserva registro, estado y asociaciones. No hay borrado físico.

El catálogo inicial de municipios reutiliza los nueve nombres del frontend regional. La FK verifica la
existencia y correspondencia de `MunicipalityId` y `City`; **no verifica pertenencia geográfica**.
El encuadre regional de Mapbox es una ayuda de presentación existente, no una prueba territorial.
La API y SQL aceptan rangos geográficos completos y cero, sin inventar validaciones de polígonos municipales.

## Cambios por actividad y archivos principales

| Actividad | Entrega |
|---|---|
| 5.11 | `database/scripts/15_DeliveryPointSchema.sql`, `16_DeliveryPointProcedures.sql`, `17_DeliveryPointStatus.sql`: catálogo, migración incremental, FK, rangos, obligatoriedad, longitudes, unicidad y procedimientos |
| 5.12 | `DeliveryPointController.cs`, `SaveDeliveryPointDto.cs`, `IDeliveryPointService.cs`, `IDeliveryPointRepository.cs`, `DeliveryPointRepository.cs`: CRUD lógico por capas; conexión del servicio HTTP del frontend y errores en Toast |
| 5.13 | `backend/Tequio.Tests/DeliveryPoints/`: validación, HTTP/JWT real con repositorio simulado y pruebas SQL optativas; Jest en `MapboxPicker/__tests__` y `DeliveryPointWizard/__tests__` |
| 6.2.1 | `postman/Tequio.CP-PTO.postman_collection.json`, entorno de ejemplo, fixture SQL separado, limpieza lógica y validador estructural |

`Program.cs` registra las dependencias y conserva la política JWT; localiza errores de enlace de JSON
solo en este controlador. El middleware añade la excepción de conflicto del módulo. `DataSeeder.SeedAdminAsync`
es virtual para sustituir únicamente el bootstrap en las pruebas HTTP, sin cambiar su ejecución normal.
El modelo de `Batch` desactiva `OUTPUT` directo de EF para ser compatible con su nuevo trigger.

## Contrato HTTP

Base: `/api/v1/delivery-points`. Consultas compartidas públicas, coherentes con el catálogo accesible
para comprador/productor; gestión exclusivamente `Admin`. Sin token → 401; token válido de otro rol → 403.

| Método / ruta relativa | Permiso | Respuesta |
|---|---|---|
| GET `/` | Pública | 200 `{items,totalCount,pageIndex,pageSize}`; 400 paginación inválida |
| GET `/{id}` | Pública | 200 punto completo; 404 inexistente |
| POST `/` | Admin | 201 `{id,mensaje}` y Location; 400 inválido; 409 duplicado |
| PUT `/{id}` | Admin | 200 `{mensaje}`; 400 inválido; 404 inexistente; 409 duplicado |
| PATCH `/{id}/deactive` | Admin | 200 `{mensaje}`; 404 inexistente; 409 RN-07 |
| PATCH `/{id}/active` | Admin | 200 `{mensaje}`; 404 inexistente |

Se conserva `/deactive`, ya utilizado por el frontend y la API de productos, así como `/active` del frontend.
Los identificadores no positivos se rechazan con 400. El listado usa páginas base 1 y tamaños entre 1 y 100.
PUT no altera `isActive`; los cambios de disponibilidad pasan por PATCH.

Campos obligatorios: `name` (150), `street` (150), `number` (50), `neighborhood` (100), `zipCode` (20),
`city` (100), `businessHours` (200), `latitude`, `longitude`. `references` admite hasta 400 caracteres.
`municipalityId` es opcional para conservar el contrato del formulario: `city` resuelve el municipio existente.
Si se envía un identificador, debe existir y coincidir con `city`. Las coordenadas son `decimal?` en entrada;
SQL conserva `DECIMAL(10,8)` / `DECIMAL(11,8)`, nunca usa cero como ausencia.
La validación se aplica también en actualización; JSON no numérico, no representable o fuera de rango → 400.

Errores SQL del módulo: 50101/50105 → 400; 50104 → 404; 50103 → 409; 2601/2627 → 409 en los
procedimientos de este repositorio. Otros errores siguen el middleware general y no se revelan al cliente.
No se etiqueta cualquier excepción SQL como duplicado.

## Instalación SQL sin pérdida de datos

1. Recuperar acceso a la base configurada y confirmar `SELECT DB_NAME()` (`TequioDB` en esta auditoría).
2. Sobre una base existente con `01`–`14` instalados, ejecutar **15 → 16 → 17** en ese orden, con SSMS
   o `sqlcmd -b -S <servidor> -d TequioDB -i <archivo>` usando la autenticación local existente.
   No poner contraseñas en los scripts. **No volver a ejecutar 01 sobre una base existente.**
3. En una base de pruebas nueva, instalar 01–17. El script 01 es únicamente la creación inicial.
4. 15 usa una transacción y detiene la instalación ante municipios desconocidos, duplicados o datos existentes
   inválidos. Revisarlos explícitamente; no se borran ni se reparan silenciosamente.
5. 16 crea o actualiza alta, listado, detalle y actualización. 17 crea función de vigencia, cambio de estado
   y trigger de lotes. La baja bloquea el padre y el rango de lotes en una transacción; el trigger rechaza
   inserciones, reasignaciones o cambios a un lote vigente sobre un punto inactivo. El índice único evita
   duplicados concurrentes. Un interbloqueo SQL puede abortar una petición, pero no autoriza un estado inválido.

Los scripts son repetibles; no recrean la base ni las tablas existentes. Nunca se ejecutan automáticamente
al iniciar la API. Instalados y vueltos a ejecutar correctamente en `TequioDB`; se verificó la conservación de filas y
checksums de las diez tablas originales. El script 01 no se ejecutó sobre esa base.

## Ejecución local y Postman

- Backend: `dotnet build backend/Tequio.slnx` y `dotnet test backend/Tequio.slnx` desde TequioAPI.
- SQL xUnit: suministrar `TEQUIO_TEST_SQL` mediante entorno local, apuntando **solo a una base aislada**
  con 01–17, y repetir `dotnet test`. **Atención:** el script 14 contiene `USE TequioDB`; al preparar
  una base aislada hay que sustituir ese contexto por el nombre de pruebas antes de ejecutarlo, sin modificar
  el archivo original. Comprobar `SELECT DB_NAME()` antes y después de cada script. Sin ella se informan pruebas omitidas, nunca aprobadas.
  Las pruebas ordinarias usan rollback. Las de RN-07/concurrencia conservan evidencia TEST_ comprometida,
  cancelan sus lotes y desactivan sus propios puntos/productos al terminar; no borran asociaciones.
- Iniciar la API con el perfil Development y la configuración MasterAdmin ya existente, sin cambiar sus
  credenciales: `dotnet run --project backend/Tequio.Api`. No es necesario enviar correos para probar el catálogo
  con usuarios Admin/Productor ya provisionados legítimamente.
- Frontend: desde TequioApp, `npm ci`, `npm run build`, `npm test -- --runInBand`, `npm run dev`.
  Añadir el token Mapbox únicamente a la configuración local si se requiere visualizar el SDK real.

La numeración **CP-PTO-01 a CP-PTO-13 se creó para esta entrega**, ya que no existían casos CP-PTO previos.
Importar la colección y `Tequio.CP-PTO.example.postman_environment.json`; usar sus valores locales para:
`adminToken` (Admin), `nonAdminToken` (Comprador/Productor), `municipalityId` y `municipalityName`.
`baseUrl` incluye `/api/v1`. `missingDeliveryPointId` debe ser un ID confirmado inexistente.

Para RN-07, ejecutar `postman/delivery-points-fixture.sql` solo en la base de pruebas, configurando su runId
hexadecimal, productor TEST_ y producto TEST_ del mismo productor. Usa el procedimiento legítimo del catálogo
y una inserción controlada en la tabla Batch, porque todavía no existe API de lotes.
Copiar `linkedDeliveryPointId` y `linkedBatchId` al entorno local. Sin ellos los pasos RN-07 generan un error
explícito y **no cuentan como aprobados**. No usar puntos reales para preparar la prueba.

En Postman Collection Runner ejecutar la colección completa en orden, una iteración. Genera nombres TEST_
únicos y guarda identificadores, comprueba el duplicado exacto, los permisos en todas las mutaciones,
RN-07 con instantáneas antes/después, baja lógica, coordenadas inválidas, municipio inexistente,
404, edición duplicada y persistencia de cero. No exportar tokens ni el entorno resuelto al repositorio.

Si Newman está instalado:

```text
newman run postman/Tequio.CP-PTO.postman_collection.json -e <entorno-local-con-valores> --bail
node postman/validate-delivery-points.cjs
```

El segundo comando valida estructura y sintaxis JavaScript, **no ejecuta HTTP**. Después del runner,
ejecutar `delivery-points-fixture-cleanup.sql` con los IDs y runId de ese fixture: valida su pertenencia,
rechaza fixtures con compras, cancela únicamente su lote y desactiva el punto, conservando las asociaciones.
Los otros puntos de la ejecución ya terminan desactivados; si el runner se interrumpe, consultar sus IDs y
aplicar PATCH únicamente a esos puntos TEST_ sin lotes vigentes. No limpiar registros reales.

## Resultados de la ejecución anterior (histórico; ver cierre actualizado)

| Comprobación | Resultado |
|---|---|
| Backend build | Aprobado, sin errores; avisos NuGet preexistentes de `System.Security.Cryptography.Pkcs` 10.0.0 |
| xUnit completo | 126 aprobadas, incluidas las 9 SQL; 0 omitidas; 0 fallidas |
| TypeScript / Vite | Aprobado; aviso de tamaño de bundle Mapbox y configuración `__dirname` de Vite |
| Jest completo | 89 aprobadas en 14 suites; 0 fallidas |
| ESLint de archivos nuevos/modificados del módulo | Aprobado |
| ESLint global | Fallido por 5 errores preexistentes en jest.setup, pruebas de login/registro, RegistrationRequest y deliveryPointValidators |
| Colección CP-PTO | 47 solicitudes; 83 aserciones aprobadas contra API/SQL reales; 0 fallos |
| SQL 15–17 / integración real | Aprobado: Docker/SQL disponibles; 15–17 instalados y repetidos; 9 pruebas SQL aprobadas |
| SMTP, Cloudinary, Mapbox real | Configuración preservada; no se consumieron servicios externos; en aquella corrida el mapa visual no se ejecutó; aprobación manual actual registrada arriba |

Las pruebas HTTP unitarias usan middleware/JWT reales con repositorio sustituido. La ejecución posterior
sí utilizó repositorio real, SQL Server y JWT obtenidos mediante login de usuarios TEST_ Admin y Productor.
Las nueve pruebas SQL se ejecutaron en `TequioDeliveryTest_20261116_180106`, distinta de la base configurada.
No se contaron omisiones como aprobaciones. La preparación inicial falló por el `USE TequioDB` del script 14;
se corrigió el contexto aislado y se repitió la ejecución completa: 126/126 aprobadas.

## Evidencia de verificación real

Evidencia sin tokens/contraseñas: [`postman/evidence/puntos-entrega_2026-11-16.json`](../postman/evidence/puntos-entrega_2026-11-16.json).
Contiene los 47 resultados HTTP, las 83 aserciones, nombres/resultados de las nueve pruebas SQL,
comprobaciones complementarias, datos de limpieza y conservación de la base original.

| Escenario | Evidencia real |
|---|---|
| Duplicado exacto y normalizado | POST devuelve 409 para el mismo municipio/nombre, incluso `Café`/`cafe`, espacios exteriores y cambio de mayúsculas; GET conserva el punto |
| Otro municipio | El mismo nombre se acepta en Coatepec con 201; PUT/GET y PATCH inactivo/activo persisten correctamente |
| RN-06 | Productor recibe 403 en POST, PUT y ambos PATCH; sin token reciben 401 |
| RN-07 | PATCH devuelve 409 con lotes Active, Confirmed y Vigente; GET y consulta SQL comprueban punto/asociación intactos |
| Limpieza | Lote propio Cancelled; punto vinculado y demás puntos TEST_ inactivos; asociaciones conservadas; credenciales TEST_ invalidadas |
| Frontend conectado | Vite sirve HTML 200; proxy `/api/v1/delivery-points` devuelve 200 y el mismo JSON persistido que la API |

Se hizo respaldo `COPY_ONLY ... WITH CHECKSUM` antes de instalar:
`/var/opt/mssql/backup/TequioDB_before_delivery_20261116_180048.bak`, dentro del volumen SQL del proyecto.
En esa ejecución aislada, las diez tablas originales mantuvieron sus conteos y checksums. Esa observación
es histórica: el propietario posteriormente creó datos al realizar las pruebas manuales en TequioDB.
La base aislada se conserva como evidencia con sus fixtures desactivados; no se borraron datos reales.
Los procesos temporales de API y Vite se detuvieron al terminar.

## Configuración local completada

MasterAdmin y Mapbox ya están configurados localmente. La cuenta Admin fue autorizada por OTP en el flujo
protegido, tal como documenta [ADMIN_FLOW.md](ADMIN_FLOW.md). SMTP se conservó. No faltan valores locales
para ejecutar las cuatro actividades; no se incluyen credenciales ni tokens en esta documentación.
La revisión manual del mapa fue realizada y aprobada por el usuario; ver el cierre y su atribución.

## Arranque normal después de la configuración local

Se completó localmente la configuración de MasterAdmin y Mapbox, sin publicar sus valores ni modificar SMTP.
La API normal y Vite quedaron ejecutándose con `TequioDB` (no con la base aislada):

- Frontend: `http://127.0.0.1:5173/login`.
- Puntos de entrega: `http://127.0.0.1:5173/admin/puntos-entrega`.
- API: `http://localhost:5168/api/v1/delivery-points`.
- Swagger: `http://localhost:5168/swagger`.

Comprobaciones reales: API, proxy Vite y HTML 200; login y perfil de MasterAdmin 200; estilo Mapbox streets-v12
200 usando el token local, sin imprimirlo. La configuración y los secretos se conservaron. La revisión manual del mapa fue después aprobada por el
propietario, según su confirmación registrada en el cierre.

La cuenta Admin rol 3 ya existe y está autorizada mediante OTP. MasterAdmin conserva rol 4 y no recibe
gestión de puntos (403). Si en el futuro se necesita otra cuenta, el asistente manual de PowerShell 5.1 es:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "C:\Projects\TEQUIO\Create-TequioAdmin.ps1"
```

El asistente captura los datos ocultos, usa el login normal de MasterAdmin, solicita el alta protegida y
pide el código enviado por el SMTP existente al correo de MasterAdmin. No muestra tokens ni contraseñas.
Su ejecución manual solicita el envío del código; el agente no envió correos durante esta configuración.
Si la solicitud ya se registró y solo falta introducir su código, usar el mismo comando con `-AuthorizeOnly`.
No cambiar roles directamente en SQL ni reutilizar la cuenta MasterAdmin como Admin.

Se corrigió un obstáculo del flujo existente: `register-admin` esperaba un claim Email ausente en los JWT
actuales. Ahora obtiene el correo mediante IUserService a partir del identificador autenticado y conserva
Authorize(MasterAdmin), las capas y la autorización por código. Las dos pruebas AuthControllerTest pasan,
incluida la regresión para JWT sin correo. No se modificaron las credenciales ni las reglas del módulo.

Guía de reproducción (los seis casos ya fueron aprobados por el usuario): iniciar sesión con Admin,
abrir puntos de entrega, crear `TEST_VISUAL_...`,
seleccionar Xalapa y una dirección de prueba, comprobar mapa/marcador y selección por clic/arrastre, guardar,
recargar, editar y desactivar el punto sin lotes. Repetir el alta debe presentar conflicto; un punto con lote
vigente debe rechazar la baja. Para revisar RN-07 usar los fixtures de la base aislada documentados arriba,
sin asociar lotes ficticios a datos reales de TequioDB.
