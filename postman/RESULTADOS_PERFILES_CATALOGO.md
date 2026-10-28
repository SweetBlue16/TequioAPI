# [TESTS] 6.2 — Perfiles, multimedia y catálogo

Fecha: 28 de octubre de 2026. Rama auditada: main. Commit inicial: `8bc3846`.
La base actual ya incluye la actividad de autenticación posterior a `f701003`:
65 peticiones y 64 variables. Se conservaron todas, incluida la carpeta CP-AUT.

## Resultado y límites de ejecución

Se añadieron **39 peticiones** en la carpeta `06 - Perfiles, multimedia y catálogo`:
**104 peticiones totales**, 11 escenarios nuevos y 24 escenarios identificados en
total contando los 13 CP-AUT anteriores. El mismo environment tiene 81 variables:
las 64 previas intactas más 17 nuevas. No hay colección ni environment alternativos.

**No se ejecutó integración real.** baseUrl está vacío, los secretos están vacíos,
developmentEnvironmentConfirmed y allowMutatingTests son false. No hay una URL
concreta autorizada, sesiones reales, aprobación de Cloudinary ni preparación
aprobada de lotes. No se inició la API ni se consultó SQL Server; no se leyeron
credenciales privadas ni se subieron archivos al proveedor.

Se revisaron controladores, DTO, servicios, repositorios, middleware, mapper SQL,
CloudinaryStorageService y procedimientos 07–13 como código fuente, sin ejecutarlos.
El documento original del plan no está en el repositorio. El encargo identifica
RN-10 y RN-12 pero **no suministra números CU/RF para estos once casos**. La matriz
mantiene trazabilidad nominal y marca esos identificadores pendientes de cotejo;
no asigna números inventados ni reutiliza CU/RF de autenticación.

## Matriz de escenarios

`me` significa `/api/v1/users/me`; `image`, `/api/v1/files/image`;
`products`, `/api/v1/products`. Todas las rutas anteponen `{{baseUrl}}`.
Evidencia actual **E0**: inspección de código y validación offline, sin respuesta
HTTP real. `pcEvidence` estará vacío en el archivo exportable hasta ejecutar.

| ID | Caso de uso / requisito funcional | Regla | Endpoint, precondición y datos de entrada | Esperado y postcondición | Observado / estado actual | Evidencia / defecto |
| --- | --- | --- | --- | --- | --- | --- |
| CP-PER-01 | Actualizar perfil propio; CU/RF numérico pendiente | No se suministró número | GET me del comprador y productor controlado → PUT me del comprador con nombres, localidad y biografía TEST_ + runId → dos GET posteriores. JWT válidos y dos IDs distintos del mismo runId | 200; comparar todos los campos enviados, ID/email invariantes, y segundo perfil sin cambios | Flujo de 5 pasos preparado; **NO EJECUTADO** | E0; sin defecto confirmado por HTTP |
| CP-PER-02 | Rechazar datos de perfil inválidos; CU/RF numérico pendiente | Validación real del DTO; formato del plan pendiente | GET → PUT con FirstName vacío y otro cambio que no debe persistir → GET → PUT con PhoneNumber de 21 caracteres → GET | 400, mensaje del campo real y perfil completo sin cambios. Esto no prueba formato de email/teléfono | 5 pasos para el alcance implementado. El requisito de formato no puede cumplirse: **BLOQUEADO** | E0; PC-D01 |
| CP-MUL-01 | Cargar y asociar imagen; CU/RF numérico pendiente | RN-12 | GET me → POST image multipart `file`, PNG ficticio válido de 131 bytes → PUT me/profile-picture con URL real → GET. Consentimiento externo y fixtures verificados | 200; URL HTTPS del origen/prefijo autorizado, sin segmentos de ruta ascendentes; foto asociada e identidad/otros campos conservados | 4 pasos preparados, sin carga real: **NO EJECUTADO** | E0; alcance de URL referenciada, no disponibilidad del CDN |
| CP-MUL-02 | Rechazar formato no admitido; CU/RF numérico pendiente | RN-12 | GET me → POST image con texto .txt inofensivo → GET | **400**, mensaje de error y ninguna URL de éxito; perfil sin cambios. 200 o 500 hacen fallar el caso | 3 pasos preparados. No hay filtro explícito de extensión/MIME en servidor; respuesta del proveedor no medida: **NO EJECUTADO** | E0; PC-D02 |
| CP-MUL-03 | Aplicar límite de carga; CU/RF numérico pendiente | RN-12 | GET → POST PNG pequeño → POST PNG de 5 242 880 bytes → POST PNG de 5 242 881 bytes → GET | Dentro y exactamente en el límite: 200 con URL válida. Excesivo: 413 o validación equivalente 400, sin URL. Registrar status exacto y comprobar perfil sin cambios | 5 pasos preparados. Dominio usa 5 MiB; no se enviaron archivos: **NO EJECUTADO** | E0; PC-D03 |
| CP-MUL-04 | Gestionar nombres con rutas/caracteres especiales; CU/RF numérico pendiente | RN-12 | GET → dos POST image con multipart explícito y nombres `../../TEST_<runId>.txt` y separadores/caracteres especiales; contenido ASCII inofensivo → GET. Consentimiento externo | Registrar contrato/URL y perfil sin cambios. Debe demostrarse que no se escribió fuera del destino; 200 por sí solo no lo prueba | 4 pasos observacionales; no hay API de inventario/normalización física del proveedor. **BLOQUEADO** | E0; PC-D04 |
| CP-CAT-01 | Registrar producto propio; CU/RF numérico pendiente | No se suministró número | GET me productor → POST products con categoryId existente, TEST_<runId>_created, descripción TEST_, kg, imageUrl=null permitido por DTO → GET products/producers/{id} paginado | 201 y productId real; comprobar propietario, nombre, categoría, descripción, unidad, foto e IsActive mediante API | 3 peticiones, GET repetible por páginas; **NO EJECUTADO** | E0 |
| CP-CAT-02 | Editar descripción/foto de producto propio; CU/RF numérico pendiente | No se suministró número | No existe endpoint de edición. Sin solicitud inventada | 200 y cambios persistidos por operación de edición | Ausencia confirmada por inventario de controladores/servicios: **NO IMPLEMENTADO** | E0; PC-D05 |
| CP-CAT-03 | Desactivar producto propio; CU/RF numérico pendiente | Baja lógica, sin adjudicar número adicional | GET me → exploración activa previa → PATCH products/{id}/deactive con ID creado/observado en este runId y confirmación adicional → exploración activa posterior completa | 200; producto ausente en **todas** las páginas activas y demás productos visibles sin cambios. No implica consultar historial inactivo | 4 peticiones, GET repetibles por páginas; **NO EJECUTADO** | E0; limitación PC-L01 |
| CP-CAT-04 | Impedir baja con lote; CU/RF numérico pendiente | **RN-10** | Producto dedicado creado por API; lote ficticio preparado mediante mecanismo de desarrollo aprobado, sin SQL manual. GET activo → PATCH products/{id}/deactive → GET activo | **409 Conflict**, producto activo y sin cambios; asociación al lote conservada | 6 peticiones por etapas, deshabilitadas por defecto. No hay endpoint/mecanismo aprobado de lotes ni consulta de asociación: **BLOQUEADO** | E0; **PC-D06: 50017 → InvalidOperationException → 500** |
| CP-CAT-05 | Impedir edición de producto ajeno entre productores controlados; CU/RF numérico pendiente | Autorización, número no suministrado | No hay endpoint de edición. No se prueban usuarios reales ni se sustituye con desactivación | 403 y producto del otro productor sin cambios | Falta la operación requerida: **NO IMPLEMENTADO** | E0; PC-D05 |

Totales **de los once escenarios de esta entrega**:

| APROBADO | FALLIDO | BLOQUEADO | NO IMPLEMENTADO | NO EJECUTADO |
| ---: | ---: | ---: | ---: | ---: |
| 0 | 0 | 3 | 2 | 6 |

Los bloqueos de PER-02, MUL-04 y CAT-04 son de alcance/precondición conocidos,
no fallos HTTP observados. No se clasifica anticipadamente MUL-02 o CAT-04 como
FALLIDO por leer código. Cuando se ejecuten, cualquier aserción HTTP fallida
quedará FALLIDA aunque las consultas posteriores sí permitan reunir evidencia.

## Importar y preparar cuentas de prueba

1. Importar los dos JSON existentes y seleccionar su environment. Reimportar las
   actualizaciones sobre la colección existente; no crear una copia independiente.
2. Configurar baseUrl y authorizedDevelopmentUrl con el origen de desarrollo
   **expresamente autorizado**, sin path ni barra final. Confirmar
   developmentEnvironmentConfirmed=true y allowMutatingTests=true solo con esa
   autorización. Nunca producción.
3. Preparar buyer y producer ficticios del mismo runId por las peticiones ya
   existentes. La carpeta CP-AUT en etapa prepare obtiene sesiones y perfiles
   de las dos cuentas (ver RESULTADOS_AUTENTICACION.md); no es necesario repetir
   registros de las carpetas originales bajo el mismo runId.
4. Conservar buyerUserId y producerUserId capturados por GET, plantillas de correo,
   flags de buzón autorizado y tokens con procedencia. La carpeta nueva acepta:
   - authBuyerJwt/authProducerJwt junto con `_authState` del mismo origen, runId e ID;
   - o el JWT de 6.0 con `_jwt*` y `_profile*` acreditados para la cuenta activa.
   PER-01 necesita dos sesiones distintas disponibles para comprobar el usuario
   observador; se recomienda usar los tokens separados de autenticación.
5. No pegar un token arbitrario ni escribir IDs manualmente para saltar controles.
   El script compara sub, rol, vigencia, procedencia e identidad; el GET inicial
   debe devolver exactamente el email generado y un nombre TEST_<runId> de esa cuenta.
   Las escrituras usan únicamente la identidad autenticada, nunca un userId en URL.
6. Establecer **pcSelectedCase** con un identificador exacto, por ejemplo CP-PER-01.
   Su valor inicial vacío deshabilita todos los casos nuevos. En Runner seleccionar
   **solo la carpeta 06**, conservar orden, una iteración y persistir variables
   localmente. No correr toda la colección de 104 peticiones.

Ejecutar perfiles después de preparar las cuentas y los casos de autenticación
que dependan del nombre original. PER-01 cambia deliberadamente datos de la cuenta
ficticia; no volver a suponer el nombre inicial del registro. No tocar perfiles de
compañeros. El perfil observador es otra cuenta ficticia controlada, no una cuenta real.
Esa comparación acredita el alcance observado; no equivale a consultar todos los
usuarios de la base, para lo que no hay endpoint ni se permite usar SQL.

## Fixtures y cargas externas

No se versionan binarios. El generador de Node produce PNG RGBA de un píxel blanco,
con CRC válidos y un chunk auxiliar de relleno para conservar un archivo PNG válido
en el tamaño exacto. Nunca crea ejecutables ni contenido activo.

Desde la raíz del repositorio, sustituir RUN_ID por el runId de las cuentas:

```powershell
node postman/create-fixtures.cjs generate RUN_ID
node postman/create-fixtures.cjs verify RUN_ID
```

El directorio `postman/.fixtures/RUN_ID/` queda ignorado por Git y contiene:

| Fixture | Bytes | Uso |
| --- | ---: | --- |
| TEST_<runId>_small.png | 131 | Imagen válida dentro del límite |
| TEST_<runId>_exact_5MiB.png | 5 242 880 | Límite exacto implementado |
| TEST_<runId>_over_5MiB.png | 5 242 881 | Exceso mínimo |
| TEST_<runId>_rejected.txt | 82 | Texto plano inofensivo |

Copiar el contenido completo de manifest.json a **pcFixtureManifest** y confirmar
pcFixturesConfirmedRunId con el runId actual. En cada POST multipart de MUL-01/02/03,
seleccionar manualmente en Body > form-data > file el fixture indicado. La entrada
se exporta con src=null; no hay rutas personales versionadas. Postman añade el
Content-Type multipart y su boundary. No agregar ese encabezado manualmente.

El script valida runId, patrón del nombre y tamaño declarado, exige un hash con
formato SHA-256 y el basename exacto seleccionado. El hash real del archivo lo
compara el comando verify, no el script de Postman. **El sandbox no lee los bytes del
archivo local**; por eso el operador debe ejecutar verify inmediatamente antes
de seleccionar/enviar y no reemplazar el archivo después. No se presenta una
declaración de tamaño como una medición independiente de bytes transmitidos.

Antes de cualquier caso multimedia, aprobar separadamente consumo del proveedor:

- pcAllowExternalUploads=true.
- pcExternalUploadConfirmedRunId=runId actual.
- pcStorageOrigin: origen HTTPS del almacenamiento de **pruebas autorizado**.
- pcStoragePathPrefix: prefijo público de esa cuenta, terminado en `/`, por ejemplo
  `/<cloudName-de-pruebas>/image/upload/` para la configuración Cloudinary actual.

La URL devuelta debe tener ese origen/prefijo, la carpeta tequio-profiles y no
incluir query, fragmento ni segmentos ascendentes. No se descargan archivos del
CDN desde scripts. MUL-01 conserva la URL real en uploadedImageUrl, sus variables
de procedencia y el estado del caso, y verifica su asociación mediante GET me.

MUL-04 usa multipart ASCII explícito para conservar nombres de ruta que una
interfaz de selección podría normalizar. El contenido es texto inofensivo y la
extensión .txt. Un rechazo puede deberse al contenido, no a la ruta: **no permite
aislar ni demostrar sanitización**. Una URL aparente dentro del prefijo tampoco
demuestra dónde escribió físicamente el proveedor. Los tests registran ese alcance
y mantienen BLOQUEADO el objetivo de seguridad; un error 500 observado ahí es solo
una observación de contrato, nunca aprobación de confinamiento.

Estas cargas pueden dejar activos de prueba sin asociación. No existe endpoint
de eliminación/inventario y la suite no realiza limpiezas externas. Requieren
permiso y cuotas acordadas, y no se ejecutan automáticamente por defecto.

## Catálogo y RN-10

Configurar categoryId con una categoría existente aprobada para pruebas; no hay
endpoint de listado de categorías. El seed es una referencia estática, no un
script para ejecutar. El producto usa imageUrl=null porque el DTO lo permite;
registrarlo no obliga a consumir Cloudinary.

1. Ejecutar CP-CAT-01 como Productor ficticio. Solo un 201 con productId entero
   positivo permite guardar el ID. El GET siguiente comprueba el payload completo.
2. Para CP-CAT-03, conservar ese producto, su propietario y runId. Copiar el runId
   actual a **confirmDeactivationRunId**, el control existente de un solo uso.
   Además se conservan las variables existentes productId, _createdProductId,
   _productRunId, _productOwnerId y _productObservedRunId, derivadas de respuestas
   reales y revalidadas en la exploración previa. Cambiar productId a otro valor
   bloquea el PATCH; no basta introducir un ID manual.
3. Las consultas HTTP usan pageIndex de base 1 y pageSize=100. El DTO de respuesta
   usa índice de base 0. La petición GET se repite mediante setNextRequest **sobre
   sí misma** hasta completar itemCount, sin sendRequest ocultos. Solo se repiten
   lecturas, nunca POST/PUT/PATCH. La coherencia del recuento y la ausencia de IDs
   repetidos se verifican por página.
4. La ausencia se aprueba solo tras recorrer todas las páginas y comparar todos
   los demás productos activos con la exploración previa. pcMaxPages=50 impone
   un límite; si se alcanza queda BLOQUEADO. No confundir un envío individual en
   Send con un Runner que sí sigue la paginación.

**CP-CAT-04 permanece BLOQUEADO.** No hay controlador de lotes ni un mecanismo
aprobado de preparación en el repositorio. No se consulta ni modifica SQL para
fabricar esa precondición. Se dejan solicitudes existentes de producto y baja
listas para una futura preparación autorizada, con dos etapas deshabilitadas:

- pcBatchStage=prepare-product, pcBatchPreparationApprovedRunId=runId y
  pcBatchPreparationMechanism con referencia a una aprobación real del equipo:
  consulta del Productor, creación por API de TEST_<runId>_batch y acreditación
  del producto activo. Es otro producto distinto al desactivado por CAT-03.
- Solo cuando exista el mecanismo autorizado (sin SQL manual de esta actividad),
  preparar el lote sobre ese ID ficticio. pcBatchStage=probe exige además
  pcBatchLinkedConfirmedRunId=runId, pcBatchState igual a Draft/Active/Confirmed,
  pcBatchEvidenceRef con evidencia real y confirmDeactivationRunId=runId.
  No rellenar esos campos para fingir una precondición que no existe.
- El probe conserva **409** como esperado, luego consulta el producto. Si recibe
  500 falla y aun así permite el GET de seguimiento para observar que sigue activo.
  Incluso con 409 y producto activo no hay API para probar que la asociación al
  lote siguió intacta: el caso completo permanece BLOQUEADO hasta contar con esa
  observabilidad. Las declaraciones del operador no sustituyen esa postcondición.

Los estados Draft, Active y Confirmed provienen del SQL actual. Las consultas de
productos filtran IsActive=1; **no existe endpoint de historial o consulta individual
de inactivos**. CP-CAT-03 acredita exclusión de activos, no el registro histórico
interno ni la ausencia de borrado físico por una consulta de historial inexistente.

## Variables nuevas, estado y evidencia

| Variables | Inicial / finalidad |
| --- | --- |
| pcSelectedCase | Vacío: selección explícita de un caso; no hay modo automático de todos |
| pcAllowExternalUploads, pcExternalUploadConfirmedRunId | false/vacío: consentimiento separado del proveedor |
| pcFixtureManifest, pcFixturesConfirmedRunId | Vacíos: manifiesto verificado y confirmación de fixtures locales |
| pcStorageOrigin, pcStoragePathPrefix | Vacíos: destino público de almacenamiento de pruebas autorizado |
| pcMaxPages | 50: límite de exploración, máximo configurable 1000 |
| pcBatchStage | disabled: deshabilita preparación/probe de lotes por defecto |
| pcBatchPreparationApprovedRunId, pcBatchPreparationMechanism | Vacíos: autorización real del mecanismo de preparación |
| pcBatchLinkedConfirmedRunId, pcBatchState, pcBatchEvidenceRef | Vacíos: referencia manual a una precondición de lote legítima; no acredita su postcondición |
| _pcState | Vacío: perfiles ficticios, payload esperado, productos capturados, exploraciones y pasos; no contiene JWT/OTP/contraseñas |
| pcResults, pcEvidence | Vacíos: resultados por caso y HTTP/checks saneados por paso/página |

No editar variables internas para saltar controles. Los JWT permanecen en los
campos secretos existentes; los locales pcRequestJwt/pcRequestBody se construyen
durante la solicitud. Los secretos exportables conservan sus valores vacíos.

Al reanudar, los pasos ya respondidos se omiten y conservan fecha y resultado.
Una escritura enviada sin respuesta registrada queda indeterminada y no se
reintenta a ciegas. Las exploraciones GET pueden continuar; un caso incompleto o
una sola página nunca lo aprueban. Si cambian cuentas/origen o almacenamiento
entre cargas, se bloquea la ejecución. No duplicar productos para sortear el estado.

pcEvidence registra método, ruta resuelta, status real, fecha, página y checks
booleanos. No guarda cuerpos, tokens, contraseñas, OTP ni contenido de archivos.
Un paso observacional aprobado no equivale al caso aprobado: PER-02/MUL-04/CAT-04
conservan sus límites, y cualquier fallo real previo se mantiene FALLIDO.
No compartir el environment activo, historial de Postman o informes crudos que
puedan incluir Authorization. Actualizar esta matriz únicamente con evidencia
real saneada, nunca con fixtures del validador offline.

## Defectos y diferencias documentadas

Todos los hallazgos siguientes son **estáticos**, salvo la ausencia de endpoints
confirmada en el inventario. No se atribuye un status HTTP medido a esta actividad.

| ID / casos | Evidencia de código y diferencia | Estado previsto / reproducción autorizada | Severidad propuesta y recomendación |
| --- | --- | --- | --- |
| PC-D01 / PER-02 | UpdateUserProfileDto no contiene Email y PhoneNumber solo tiene StringLength(20). No hay formato telefónico ni EmailAddress en ese contrato | Los casos reales Required/StringLength devolverían 400; no satisfacen el requisito distinto de formato. No se fabrica un JSON Email ni se aprueba por otro campo | Media: aclarar el contrato funcional o implementar formatos mediante cambio separado autorizado |
| PC-D02 / MUL-02, RN-12 | FilesController solo comprueba null/longitud 0; FileService solo comprueba tamaño. CloudinaryStorageService usa ImageUploadParams, sin filtro explícito de MIME/extensión del servidor | Un .txt podría terminar rechazado por el proveedor y traducido a 500, o ser aceptado según contenido/proveedor. Ni uno ni otro sería el 400 del plan. La prueba real está pendiente | Media: validación de tipo/contenido en servidor y contrato de error controlado; no confiar en frontend ni en un error genérico del proveedor |
| PC-D03 / MUL-03, RN-12 | MaxFileSize=5*1024*1024=5 242 880 bytes. RN-12 dice 5 MB y el error también imprime 5MB; 5 MB decimal serían 5 000 000 bytes | Domain permite exactamente 5 MiB y rechaza un byte más con ArgumentException →400. El plan admite ese equivalente o 413. La diferencia es 242 880 bytes; sin medición HTTP todavía | Media por ambigüedad: acordar MB/MiB y alinear documentación/mensajes, sin cambiar esta regla durante pruebas |
| PC-D04 / MUL-04, RN-12 | La API pasa file.FileName a Cloudinary FileDescription; configura Folder=tequio-profiles, UseFilename=true, UniqueFilename=true, Overwrite=false. No expone nombre normalizado, inventario ni destino físico | URL aparentemente válida o HTTP 200 no demuestra ausencia de escritura fuera del destino. Texto rechazado tampoco demuestra normalización del nombre | Alta propuesta para verificar el riesgo; revisión de proveedor/normalización y evidencia de confinamiento en entorno aprobado. No afirmar que exista explotación demostrada |
| PC-D05 / CAT-02 y CAT-05 | ProductController/IProductService solo exponen crear, desactivar y consultar. No existe operación de edición | Ruta de edición y status real: N/A; no inventar PUT/PATCH ni llamar a una ruta para considerar 404 éxito | Alta funcional: implementar edición/propiedad mediante cambio separado; después usar dos productores ficticios para la prueba ajena |
| **PC-D06 / CAT-04, RN-10** | USP_DeactivateBaseProduct rechaza lote Draft/Active/Confirmed con SQL **50017**; ProductRepository llama SqlExceptionMapper; mapper crea **InvalidOperationException(ProductHasActiveBatches)**; GlobalExceptionMiddleware la captura y devuelve **500**, sustituyendo el detalle por DatabaseError | El contrato del caso exige **409**. Cadena confirmada por lectura de archivos; reproducción HTTP pendiente del lote legítimo. La aserción 409 no se modifica, y 500 quedará FALLIDO | Alta: mapear el conflicto de negocio de forma diferenciada y devolver 409 sin convertir otros fallos internos en conflictos; aprobar la corrección por separado |
| PC-L01 / CAT-03, CAT-04 | GET products/producers y category filtran solo activos; no hay historial inactivo ni consulta de lote/asociación | No se puede acreditar historial o integridad del vínculo por una API que no lo expone; no usar SQL como aserción | Limitación de observabilidad: añadir consultas autorizadas o acordar evidencia complementaria antes de declarar esos criterios cumplidos |
| PC-L02 / RN-12 | Solo existe POST files/image; no hay endpoint de video de 20 MB | No se crea una ruta de video ni un duodécimo escenario ficticio | Alcance funcional pendiente; planificar video y sus límites por separado |

Fuentes: [UserController](../backend/Tequio.Api/Controllers/UserController.cs),
[FilesController](../backend/Tequio.Api/Controllers/FilesController.cs),
[ProductController](../backend/Tequio.Api/Controllers/ProductController.cs),
[DTO de perfil](../backend/Tequio.Domain/Dtos/UpdateUserProfileDto.cs),
[FileService](../backend/Tequio.Domain/Services/IFileService.cs),
[almacenamiento](../backend/Tequio.Infrastructure/Services/CloudinaryStorageService.cs),
[baja SQL](../database/scripts/13_USP_DeactivateBaseProduct.sql),
[mapper](../backend/Tequio.Infrastructure/Helpers/SqlExceptionMapper.cs) y
[middleware](../backend/Tequio.Api/Middlewares/GlobalExceptionMiddleware.cs).

## Validaciones realizadas

```powershell
node postman/validate-auth.cjs
node postman/validate-profiles-catalog.cjs
node postman/create-fixtures.cjs self-test RUN_ID
```

En esta actividad los dos validadores también se ejecutaron con `--schema RUTA`
al JSON oficial v2.1 descargado anteriormente. Se utilizaron SDK/Ajv del Newman
6.2.2 existente, sin instalar dependencias. La opción con esquema aprobó:

- **22 comprobaciones offline de regresión de autenticación**, ahora acotadas a
  sus 65 solicitudes conservadas; su lógica y escenarios no se reescribieron.
- **27 comprobaciones offline de perfiles/catálogo**: JSON/esquema, sintaxis,
  importabilidad mediante SDK, variables, 11 carpetas y preservación exacta de
  solicitudes/variables previas; guardas, observador, rechazos sin cambios,
  recursos externos, fixtures válidos y límites, MIME aceptado que falla,
  paginación de varias páginas, confirmación de baja, 500 que falla frente a 409
  y postcondición de lote que no puede aprobarse sin observabilidad.
- El generador comprobó firmas PNG, CRC y píxel blanco en memoria. **No se
  escribieron binarios grandes**, no se subieron archivos y no se contactó un CDN.
- Los 104 requests quedan cancelados/omitidos con el environment exportado sin
  configurar. La validación usa respuestas sintéticas solo para verificar scripts;
  no son evidencia ni aprobación de integración.
- `git diff --check` sin errores. Los únicos cambios están en postman/.

El número de checks con SDK/esquema puede diferir si esas herramientas opcionales
no existen al ejecutar el comando simple; el resumen indica qué validaciones se
efectuaron. No se instala nada automáticamente ni se declara validación de esquema
si no se proporcionó el archivo oficial.

**HTTP real: 0. Operaciones SQL: 0. Escenarios de integración aprobados: 0.**
No se ejecutaron builds o tests de producción porque no se modificó su código.
Sin cobertura medida, commits ni pushes. Al finalizar una sesión autorizada,
desactivar allowMutatingTests y pcAllowExternalUploads y vaciar confirmaciones.
No eliminar usuarios/productos ni reinicializar la base para repetir pruebas.
