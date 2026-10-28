# TEQUIO — Colección base de integración

La colección conserva las 16 plantillas de [TESTS] 6.0 y añade la carpeta
`05 - Autenticación CP-AUT-01 a CP-AUT-13` de [TESTS] 6.2, con 49 solicitudes
nuevas, OTP manual y resultados por escenario. Usar Collection Runner solo sobre
esa carpeta, siguiendo [RESULTADOS_AUTENTICACION.md](RESULTADOS_AUTENTICACION.md).
CP-AUT-05 y CP-AUT-10 permanecen NO IMPLEMENTADOS. Las instrucciones originales
de este README corresponden a las carpetas 00–04; no mezclar sus registros con
los de la suite bajo el mismo runId.

Auditoría del repositorio en `main`, commit `e24d344`: controladores, DTO,
servicios de autenticación/JWT, middleware, validadores de paginación,
launchSettings y scripts SQL **leídos como fuente**, sin ejecutarlos. No había
colecciones previas. El plan con la sección 6.1.9 no está en el repositorio:
la trazabilidad detallada contra ese documento queda pendiente.

## Importar y ejecutar localmente

1. En Postman, **Import > Files**, seleccionar
   `Tequio.postman_collection.json` y
   `Tequio.Development.postman_environment.json`.
2. Seleccionar el environment **TEQUIO - Development (configure locally)**.
   Usar una versión actual que soporte `pm.execution.skipRequest()` en scripts
   previos a la solicitud. No ejecutar con un runtime antiguo que carezca de esa
   función: lanzar una excepción de JavaScript por sí sola no garantiza cancelar
   una solicitud. No hay scripts que instalen paquetes.
3. Con la instancia de desarrollo ya preparada por su responsable, ejecutar desde
   `backend/`:

   ```powershell
   dotnet run --project Tequio.Api/Tequio.Api.csproj --launch-profile http
   ```

   El perfil `https` también existe. Revisar **Now listening on** y configurar el
   origen real. Los perfiles actuales declaran HTTP 5168 y HTTPS 7244; esos
   puertos no son una suposición de la colección y pueden cambiar por configuración.
   `baseUrl` queda vacío en el archivo importable. Debe contener exclusivamente
   `http(s)://host[:puerto]`, sin `/api/v1`, credenciales ni barra final.
4. La configuración privada existente debe resolver
   `ConnectionStrings:TequioDatabase`, `Jwt:Key`, `Jwt:Issuer`, `Jwt:Audience`,
   `Email:*` y `Cloudinary:*` según los servicios que se utilicen. No copiar esos
   valores a la colección. El correo exige STARTTLS, usuario, contraseña,
   remitente y puerto válidos; un capturador debe ser compatible con esa configuración.
   No ejecutar inicializaciones, migraciones, restores, reinicios de contenedores
   ni cambios de configuración de la instancia compartida desde esta actividad.
5. Confirmar con el responsable que la dirección es de **desarrollo autorizado**.
   Copiar exactamente ese origen a `authorizedDevelopmentUrl` y establecer
   `developmentEnvironmentConfirmed=true`. No usar producción. La colección no
   puede demostrar por sí misma qué base de datos hay detrás de una URL.
6. Enviar individualmente **00 - Setup and Diagnostics / Read development OpenAPI
   document**. Genera y conserva un `runId` de 32 caracteres hexadecimales si está
   vacío. Swagger solo existe en Development; un 404 fuera de ese entorno es
   coherente con el código. Esto no es un endpoint de salud ni demuestra conexión
   a SQL Server, SMTP o Cloudinary.

**Estado seguro inicial:** baseUrl y secretos vacíos, confirmación de entorno
false y `allowMutatingTests=false`. Los scripts cancelan solicitudes con
configuración insuficiente y registran una comprobación de seguridad fallida;
un bloqueo no debe contarse como prueba de integración aprobada. Todos los POST,
PUT y PATCH —también login, por política conservadora— requieren habilitar las
mutaciones. No hay DELETE, consultas SQL, limpieza ni llamadas HTTP desde scripts.

## Variables

Configurar los valores solo localmente. Las variables con tipo `secret` se exportan
vacías en el archivo versionado; no exportar ni compartir el environment después
de utilizarlo sin vaciar de nuevo contraseñas, tokens y OTP.

| Variable | Inicial | Uso |
| --- | --- | --- |
| `baseUrl` | vacío | Origen de la API; las rutas agregan `/api/v1` cuando corresponde. |
| `authorizedDevelopmentUrl` | vacío | Confirmación del origen exacto autorizado. |
| `developmentEnvironmentConfirmed` | `false` | Debe ser `true` incluso para lecturas. |
| `allowMutatingTests` | `false` | Debe ser exactamente `true` para POST, PUT y PATCH. |
| `runId` | vacío | Se genera una vez; identifica los datos de la ejecución. |
| `activeAccount` | `buyer` | Elegir `buyer` o `producer`; cada solicitud de autenticación usa esta cuenta. |
| `buyerEmail`, `producerEmail` | vacíos | Generados desde las plantillas; no introducir cuentas personales existentes. |
| `buyerEmailTemplate`, `producerEmailTemplate` | vacíos | Direcciones con el literal `{{runId}}`, distintas y con enrutamiento autorizado. |
| `buyerPassword`, `producerPassword` | vacíos, secret | Contraseñas ficticias configurables para las dos cuentas. Usar 8–64 caracteres. |
| `newPassword` | vacío, secret | Nueva contraseña ficticia de 8–64 caracteres. |
| `buyerUserId`, `producerUserId` | vacíos | Solo se capturan de un GET `/users/me` válido. |
| `buyerRoleId`, `producerRoleId` | `1`, `2` | Valores del seed: Comprador y Productor. Los roles 3 y 4 son Administrador y Administrador maestro; no se usan. |
| `jwt` | vacío, secret | Se captura únicamente de login HTTP 200 con claims esperados. |
| `expiredJwt` | vacío, secret | CP-AUT-11 lo rellena con el mismo JWT real capturado en CP-AUT-08 y aceptado previamente por GET, después de vencer naturalmente. No se fabrica ni se usa un token malformado. |
| `verificationOtp`, `recoveryOtp` | vacíos, secret | Códigos del buzón, conservados como texto de seis dígitos, incluidos ceros iniciales. |
| `testInboxAddress` | vacío | Buzón de pruebas real o dirección atendida por un capturador autorizado. |
| `testInboxConfirmed` | `false` | Confirmación de que el buzón está bajo control del equipo de pruebas. |
| `emailRoutingConfirmed` | `false` | Confirmación de que ambas plantillas llegan exclusivamente a ese buzón/capturador. |
| `pageIndex`, `pageSize` | `1`, `10` | Query HTTP de base 1, tamaños permitidos por el controlador 10–100. |
| `categoryId` | vacío | ID de categoría existente aprobado para pruebas; consultar el seed como referencia, sin ejecutarlo. No hay endpoint de listado de categorías. |
| `uploadedImageUrl` | vacío | URL real devuelta por la carga de imagen, vinculada a cuenta y ejecución. |
| `productId` | vacío | ID real devuelto al crear el producto de esta ejecución. |
| `confirmPromotionRunId` | vacío | Copiar el runId actual para autorizar una promoción; se consume antes de enviar. |
| `confirmDeactivationRunId` | vacío | Copiar el runId actual para autorizar una desactivación; se consume antes de enviar. |

Las 18 variables internas también se exportan vacías:
`_stateRunId`, `_jwtRunId`, `_jwtAccount`, `_jwtUserId`, `_jwtRoleId`,
`_profileRunId`, `_profileAccount`, `_profileUserId`, `_uploadRunId`,
`_uploadAccount`, `_uploadUserId`, `_productRunId`, `_createdProductId`,
`_productOwnerId`, `_productObservedRunId`, `_buyerRegisteredRunId`,
`_producerRegisteredRunId` y `_buyerPromotedRunId`. No editarlas para omitir
los controles de procedencia. Los datos locales `requestBody`, `operation` y
`expectedStatus` se construyen en el script compartido; no faltan variables
de environment para esos nombres. Los cuerpos se construyen con JSON.stringify
para evitar JSON inválido por comillas en los valores.

Al vaciar `runId`, la siguiente solicitud autorizada crea una ejecución nueva y
descarta IDs, OTP, JWT, URLs, confirmaciones e indicadores de la anterior. Conserva
la configuración de buzón, plantillas, contraseñas y permisos; devolver
`allowMutatingTests=false` al terminar. No borra datos del servidor. Para continuar
la misma ejecución se conserva su runId. No modificar runId para reutilizar
usuarios o productos ajenos. Las variables locales de Postman se fijan desde el
environment para evitar sobrescrituras accidentales mediante variables globales
o archivos de datos del Runner.

## Preparar el buzón y datos ficticios

1. Obtener permiso para un buzón dedicado, dominio catch-all o capturador interno
   que reciba las direcciones de prueba. No usar destinatarios de terceros.
2. Configurar `testInboxAddress` con su dirección real. El nombre del proveedor no
   demuestra que acepte aliases: comprobar previamente su documentación o su
   configuración autorizada. Si se elige plus-addressing, confirmar expresamente
   que funciona **antes** de habilitar la colección.
3. Escribir plantillas distintas con `{{runId}}`. Ejemplos de estructura, para
   sustituir por un dominio/buzón realmente controlado:
   `TEST_buyer_{{runId}}@dominio-controlado` y
   `TEST_producer_{{runId}}@dominio-controlado` para catch-all; o
   `buzon+buyer_{{runId}}@dominio-controlado` y
   `buzon+producer_{{runId}}@dominio-controlado` si el proveedor admite aliases.
   Esos ejemplos no son direcciones listas para usar: el dominio real necesita
   formato válido. `example.test` **no se considera un buzón receptor**; solo es
   admisible un dominio de laboratorio si el capturador está configurado para él.
4. Confirmar `testInboxConfirmed=true` y `emailRoutingConfirmed=true`. El script
   exige direcciones distintas, inclusión del runId, formato ASCII compatible con
   EmailAddress y un máximo conservador de 80 caracteres por el DTO de recuperación.
   Los nombres de personas y productos tienen prefijo `TEST_` e incluyen el runId.
5. Elegir `activeAccount`, configurar su contraseña ficticia, y solo entonces
   habilitar `allowMutatingTests=true`. Enviar **Register active test account**
   manualmente. Cambiar activeAccount para registrar la otra cuenta cuando haga falta.
6. Abrir el buzón fuera de la colección y copiar el código del mensaje correcto
   a `verificationOtp`. Enviar **Verify account with mailbox OTP**. No buscar el
   código en SQL ni esperar que la API lo devuelva. Los códigos de verificación y
   recuperación son de propósitos distintos y caducan a los 15 minutos según el servicio.
7. **Resend** y **recover** tienen respuesta genérica 200: no demuestran que exista
   la cuenta ni que haya llegado un correo. Revisar el buzón. Al solicitar uno nuevo
   se vacía el código local correspondiente; introducir después el código recibido.
8. Para recuperar, usar **Request password recovery OTP**, copiar el código a
   `recoveryOtp`, configurar `newPassword` y enviar **Reset password with mailbox
   OTP**. Tras éxito, la contraseña local de la cuenta activa se actualiza a
   newPassword y se borra el JWT local para exigir login de nuevo. No se supone
   que el servidor revoque todos los tokens anteriores.

## Uso manual de las carpetas

Después de login válido, ejecutar **Read my test profile and capture real user
ID**. El perfil debe coincidir con el email generado, el sub del JWT y el prefijo
`TEST_<runId>`. Este GET habilita las escrituras sobre esa cuenta. Cambiar de
activeAccount requiere login y GET de nuevo; no basta pegar un token en `jwt`.
El script decodifica los claims para comprobaciones locales; la validación
criptográfica corresponde a la API, nunca a un secreto de firma en Postman.

Después de actualizar el perfil, repetir el GET y comparar firstName,
paternalLastName, locality y biography con el cuerpo enviado. Tras asociar foto,
comparar profilePictureUrl con uploadedImageUrl mediante ese mismo GET. Después
de verificar OTP, comprobar isVerified por la API. Después de restablecer la
contraseña, comprobar login con la contraseña nueva. Son comprobaciones manuales
de persistencia; no hay aserciones que consulten SQL Server.

Para foto, seleccionar un archivo ficticio local en **Body > form-data > file**
de la petición de carga. No se versiona una ruta del equipo ni una foto personal.
Usar archivo no vacío de hasta 5 MiB y formato admitido por el proveedor; el
servicio de dominio implementa el límite de tamaño, no una lista MIME. Postman
configura el boundary multipart; no agregar Content-Type manualmente.
La URL recibida se puede asociar únicamente con la cuenta que realizó esa carga.

Para catálogo, usar activeAccount=producer, login, GET del perfil, carga de imagen
y categoryId configurado. Crear el producto `TEST_<runId>`. Registrar el ID real
no prueba persistencia: consultar **Read active products from test producer**,
recorriendo páginas si es necesario hasta observar ese producto. La colección
marca la observación solo al encontrar ID, propietario y nombre de la ejecución.
No realiza paginación automática.

Promoción y desactivación son opcionales y requieren confirmación adicional del
runId actual. La promoción aplica solo al comprador de prueba; después requiere
login/GET para renovar los claims de rol. El productor registrado separadamente
conserva su propio email e ID. La desactivación solo admite el ID capturado de la
creación y previamente observado por la API en la misma ejecución y propietario;
no introduce IDs manuales ni toca productos previos. No ejecutar la colección
completa como una automatización de extremo a extremo: requiere intervención
humana para OTP, archivos y confirmaciones.

## Rutas y contratos auditados

Todas las rutas usan `{{baseUrl}}`. El bearer `{{jwt}}` solo se aplica a las rutas
marcadas como protegidas. Los estados de esta tabla corresponden a éxito; una
respuesta diferente hace fallar la comprobación base, sin cambiarla a un éxito
esperado ni inventar datos para continuar.

| Carpeta | Método y ruta | JSON o entrada | Respuesta de éxito | Bearer |
| --- | --- | --- | --- | --- |
| 00 | GET `/swagger/v1/swagger.json` | — | 200, documento OpenAPI | No |
| 01 | POST `/api/v1/auth/register` | email, password, firstName, paternalLastName, maternalLastName, birthDate, phoneNumber, roleId | 201, mensaje; **sin ID** | No |
| 01 | POST `/api/v1/auth/login` | email, password | 200, mensaje y token | No |
| 01 | POST `/api/v1/auth/verify` | email, verificationCode | 200, mensaje | No |
| 01 | POST `/api/v1/auth/resend-verification-code` | email | 200, mensaje genérico | No |
| 01 | POST `/api/v1/auth/recover` | email | 200, mensaje genérico | No |
| 01 | POST `/api/v1/auth/reset-password` | email, **code**, newPassword | 200, mensaje | No |
| 02 | GET `/api/v1/users/me` | — | 200, UserProfileDto con id, email, nombres, fechas, contacto, foto e isVerified | Sí |
| 02 | PUT `/api/v1/users/me` | firstName, paternalLastName, maternalLastName, birthDate, phoneNumber, locality, biography | 200, mensaje | Sí |
| 02 | PUT `/api/v1/users/me/profile-picture` | pictureUrl | 200, mensaje | Sí |
| 02 | PATCH `/api/v1/users/me/role/producer` | Sin cuerpo | 200, mensaje | Sí |
| 03 | POST `/api/v1/files/image` | multipart, clave **file** | 200, mensaje y url | Sí |
| 04 | POST `/api/v1/products` | categoryId, name, shortDescription, measurementUnit, imageUrl | 201, mensaje y productId | Sí |
| 04 | GET `/api/v1/products/producers/{{producerUserId}}?pageIndex={{pageIndex}}&pageSize={{pageSize}}` | Query de base 1 | 200, pageIndex de base 0, pageSize, itemCount, products | No |
| 04 | GET `/api/v1/products/category/{{categoryId}}?pageIndex={{pageIndex}}&pageSize={{pageSize}}` | Query de base 1 | 200, mismo ProductPageDto | No |
| 04 | PATCH `/api/v1/products/{{productId}}/deactive` | Sin cuerpo | 200, mensaje | Sí |

Los cuerpos JSON son visibles en el script compartido y se preparan antes de
enviar. Las fechas son ISO 8601, se usan valores ficticios y los IDs se sustituyen
solo cuando existen. No hay health check, listado de usuarios, edición de
productos, categorías administrativas ni endpoints para consultar OTP.

## Comprobaciones, errores y limitaciones

- El middleware devuelve `{ "error": "..." }`: ArgumentException 400,
  UnauthorizedAccessException 401, KeyNotFoundException 404, EmailDeliveryException
  503 y errores internos/InvalidOperationException 500. Los errores de validación
  automáticos de ApiController pueden ser ProblemDetails con errors. Un rechazo
  JWT puede devolver 401 sin JSON; las plantillas positivas no lo consideran éxito.
- Aunque el controlador documenta 409 para desactivación, el mapper crea
  InvalidOperationException para producto ya desactivado o con lotes, que el
  middleware actual convierte en **500**, no 409. No se modifica producción para
  ocultar esa discrepancia.
- El login actual no comprueba IsVerified. No se afirma que un usuario pendiente
  de OTP sea rechazado al iniciar sesión. La colección conserva el paso manual
  de verificación, sin convertirlo en una regla ficticia de autenticación.
- Los JWT del servicio duran dos horas; un JWT para probar expiración debe ser
  auténtico del mismo entorno, conservado localmente hasta vencer. La validación
  del servidor mantiene la tolerancia temporal predeterminada de la librería.
- Un registro puede persistir al usuario y fallar después enviando correo (503).
  No reintentar ciegamente la creación ni borrar ese usuario: revisar el buzón y
  usar el reenvío autorizado cuando corresponda. Los permisos del entorno local
  no se pueden deducir del documento Swagger.
- Se valida formato JSON, estado HTTP, contratos de respuesta y exposición de
  claves sensibles; las aserciones de secretos trabajan con booleanos para no
  imprimir los valores. El token de login es la única excepción intencional.
  Los nombres de propiedades de contraseñas en el esquema Swagger son metadatos,
  no secretos expuestos. Los scripts no usan console ni incluyen respuestas
  guardadas con credenciales.
- Postman puede conservar cuerpos, encabezados y respuestas en su historial y
  consola propia. No compartirlos ni generar informes con secretos. Marcar una
  variable como secret no elimina automáticamente el contenido de todos los
  exportadores; revisar cualquier exportación. No versionar el environment usado.
- Los GET de productos devuelven solo activos; la ausencia de un ID en una sola
  página no prueba su desactivación. Verificar por consultas API y revisar todas
  las páginas relevantes y el recuento. No se usan consultas SQL como aserciones.
- No hay endpoint para consultar si un usuario tiene rol Productor en el perfil;
  tras promoción, renovar login y observar el claim roleId emitido por la API.
- Esta entrega no comprueba conectividad, entrega real de correo ni persistencia
  de la instancia compartida. No ejecuta solicitudes contra TEQUIO ni SQL Server.

Validación histórica de 6.0: JSON parseable, esquema oficial Collection v2.1,
deserialización por el SDK disponible con Newman, sintaxis de los scripts y
24 comprobaciones locales de seguridad/captura y coincidencia con rutas/DTO,
mediante respuestas simuladas. Las 16 solicitudes quedan bloqueadas con el
environment exportado sin configurar. Se usaron Newman 6.2.2 (su SDK) y Ajv
existentes para deserializar y validar el esquema oficial; no se ejecutó un Runner
contra la API ni se utilizaron las credenciales de la máquina. No se
instalan dependencias. No se presenta esta revisión como una ejecución de los
casos CP-AUT ni como importación manual en la interfaz de Postman.

Para 6.2 se añadieron 15 variables de suite (64 totales), el validador offline
`node postman/validate-auth.cjs` y el informe de autenticación enlazado arriba.
La verificación actual consta de 22 comprobaciones offline con el esquema oficial
y SDK disponibles; no equivale a ejecución de integración. Los estados por caso,
el protocolo de OTP manual, las etapas prepare/execute, la espera de expiración,
las dependencias y el registro de defectos se explican en ese informe.

Referencias oficiales:
[Collection v2.1](https://schema.getpostman.com/json/collection/v2.1.0/collection.json)
y [pm.execution.skipRequest](https://learning.postman.com/latest-v-12/docs/tests-and-scripts/write-scripts/postman-sandbox-reference/pm-execution).

Al terminar una sesión, volver a `allowMutatingTests=false`, vaciar JWT, OTP y
contraseñas locales si ya no son necesarios, y conservar solo evidencia saneada.
No borrar usuarios, tablas, bases de datos ni volúmenes. Sin commits ni pushes.
