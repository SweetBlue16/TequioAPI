# [TESTS] 6.2 — Autenticación: CP-AUT-01 a CP-AUT-13

Fecha: 28 de octubre de 2026. Base auditada: `main`, `f701003`.

## Resultado de esta actividad

**No se ejecutaron solicitudes HTTP contra TEQUIO ni operaciones SQL.** El
environment versionado tiene `baseUrl` y secretos vacíos,
`developmentEnvironmentConfirmed=false` y `allowMutatingTests=false`. No se
proporcionó una URL concreta autorizada ni los OTP de un buzón controlado.
No se buscaron credenciales en archivos privados ni se iniciaron servicios.

Se amplió la colección existente: **16 peticiones originales conservadas sin
cambiar su definición, 49 nuevas, 65 en total**. La carpeta nueva contiene los
13 identificadores exactos; CP-AUT-05 y CP-AUT-10 se documentan sin solicitudes
porque la funcionalidad requerida no existe. No se inventan rutas ni se acepta
404 como éxito. Se reutiliza el mismo environment: 49 variables anteriores más
15 nuevas, **64 en total**, con secretos iniciales vacíos.

El documento original del plan no está en el repositorio. Se utilizaron las
especificaciones completas del encargo como referencia de 6.3.1 y las condiciones
de seguridad indicadas para 6.1.9; no se omitió ningún caso por esa ausencia.

## Matriz de los trece escenarios

En esta tabla `auth/` significa `/api/v1/auth/` y `users/me` significa
`/api/v1/users/me`. Las solicitudes utilizan `{{baseUrl}}`.
Los estados reflejan **ejecución real**, no resultados de fixtures del validador.

| Caso | Ruta y flujo preparado | Resultado esperado y postcondiciones | Observado en esta actividad | Estado | Trazabilidad |
| --- | --- | --- | --- | --- | --- |
| CP-AUT-01 — Registrar comprador | POST auth/register → POST auth/login → GET users/me | 201, rol 1 en JWT, ID real, email y nombres TEST_ persistidos; el registro no fabrica un ID | Sin solicitudes reales; flujo preparado, 3 pasos | NO EJECUTADO | CU01, RF-01 |
| CP-AUT-02 — Registrar productor | POST auth/register → POST auth/login → GET users/me | 201, rol 2 en JWT, acceso protegido y perfil persistido con identidad correspondiente | Sin solicitudes reales; flujo preparado, 3 pasos | NO EJECUTADO | CU01, RF-01 |
| CP-AUT-03 — Correo duplicado | POST auth/register con email existente y nombre diferente → login original → GET users/me | 409 o validación equivalente 400 con mensaje exacto de duplicado; credenciales, ID y nombres originales conservados | Código fuente traduce 50002 a 400; no es evidencia HTTP. 3 pasos preparados | NO EJECUTADO | CU01, RF-01 |
| CP-AUT-04 — Restricción de edad | POST auth/register con nacimiento diez años atrás → POST auth/login | 400 por edad; 401 al intentar usar esas credenciales | SQL rechaza edad <18; admite exactamente 18. Sin ejecución; 2 pasos preparados | NO EJECUTADO | CU01, RN-08 |
| CP-AUT-05 — Registrar administrador | No hay ruta específica para registro autorizado por Administrador Maestro ni código de autorización | 201 y código de autorización mediante el flujo administrativo requerido | Ausencia comprobada en controladores y servicios; no se llama al registro público con roles administrativos | NO IMPLEMENTADO | CU02, RF-01 |
| CP-AUT-06 — Completar verificación | login → GET pendiente → POST auth/verify → GET verificada | OTP correcto real: 200 e isVerified=true; no basta el verify idempotente de una cuenta ya verificada | Falta OTP y entorno autorizado. 4 pasos preparados, sin ejecutar | NO EJECUTADO | CU03, RN-11 |
| CP-AUT-07 — Límite de intentos OTP | registro/login/GET de cuenta exclusiva → verify incorrecto ×3 → verify original → login/GET pendiente → resend-verification-code → verify nuevo → GET verificada | Invalidar el original tras tres intentos; exigir 403/422 al agotar intentos, mantener cuenta pendiente y permitir verificar con el nuevo OTP | SQL implementa 3 intentos, pero mapper/middleware producirían 400. La expectativa 403/422 se conserva; no se declara un fallo HTTP sin ejecutarlo. 12 pasos preparados | NO EJECUTADO | CU03, RN-11 |
| CP-AUT-08 — Inicio de sesión válido | POST auth/login → GET users/me | 200, JWT vigente con sub y rol 1, identidad contrastada por API; secreto guardado sin imprimir | Falta comprador verificado real de CP-AUT-06. 2 pasos preparados | NO EJECUTADO | CU04, RF-01 |
| CP-AUT-09 — Credenciales inválidas | POST auth/login con contraseña incorrecta → login con email inexistente único | Ambos 401, mismo mensaje genérico, sin token ni distinción entre correo y contraseña | Sin solicitudes reales; 2 pasos preparados | NO EJECUTADO | CU04, RF-01 |
| CP-AUT-10 — Administrador Maestro con 2FA | Existe POST auth/login, pero no desafío ni confirmación de segundo factor | Exigir 2FA antes de entregar JWT | Servicio devuelve JWT directamente para cualquier rol autenticado; no se solicitan cuentas administrativas reales ni se simula 2FA | NO IMPLEMENTADO | CU04, RN-09 |
| CP-AUT-11 — Token vencido | GET users/me con JWT aún vigente → el mismo GET con exactamente ese JWT tras vencer | Primera respuesta 200 que acredita procedencia; después 401 sin escrituras, sin alterar payload ni firma | Falta JWT real previamente aceptado y espera natural. 2 pasos preparados | NO EJECUTADO | CU04, RNF-05 |
| CP-AUT-12 — Recuperación válida | registro/login/GET → recover → reset-password → login nuevo/GET → login anterior → reutilizar OTP → login nuevo/GET | Recuperación exitosa; nueva contraseña funciona, anterior devuelve 401, OTP consumido es rechazado y la identidad se conserva | Falta OTP real de recuperación y entorno autorizado. 11 pasos preparados | NO EJECUTADO | CU05, RF-10 |
| CP-AUT-13 — Código de recuperación incorrecto o vencido | recover nuevo → reset-password inválido o vencido → login vigente/GET → login alternativo | Rechazo 400/403/422 por la causa prevista; contraseña vigente antes del intento conservada; alternativa rechazada 401 | Sin ejecución. Dos variantes seleccionables, 5 pasos; una ejecución registra una variante, no acredita la otra | NO EJECUTADO | CU05, RF-10 |

Totales actuales por **escenario**:

| APROBADO | FALLIDO | BLOQUEADO | NO IMPLEMENTADO | NO EJECUTADO |
| ---: | ---: | ---: | ---: | ---: |
| 0 | 0 | 0 | 2 | 11 |

Los once flujos se dejan NO EJECUTADOS porque no se han enviado sus solicitudes.
La columna de precondiciones y las instrucciones siguientes describen los bloqueos
que habrá que resolver. No se atribuye una avería SMTP sin haberla observado.

## Ejecutar mediante Postman Collection Runner

### Preparar autorización, datos y buzón

1. Importar los dos JSON existentes y seleccionar su environment. Utilizar un
   runtime actual compatible con `pm.execution.skipRequest()` y `pm.test.skip()`.
2. Confirmar con el responsable el **origen concreto** de desarrollo autorizado.
   Configurar `baseUrl` y `authorizedDevelopmentUrl` con ese origen exacto,
   sin `/api/v1` ni barra final, y `developmentEnvironmentConfirmed=true`.
   Configurar `allowMutatingTests=true` solo con esa autorización. Nunca producción.
3. Usar un buzón dedicado o capturador SMTP autorizado, configurar
   `testInboxAddress`, `buyerEmailTemplate`, `producerEmailTemplate`,
   `testInboxConfirmed=true` y `emailRoutingConfirmed=true`, como en README.
   Ambas plantillas deben incluir `{{runId}}`, producir correos distintos y no
   superar 80 caracteres. Comprobar aliases/plus-addressing antes de utilizarlos.
   No enviar correos a terceros ni asumir que example.test recibe mensajes.
4. Introducir contraseñas **ficticias** distintas en buyerPassword,
   producerPassword y newPassword (8–64 caracteres). No utilizar cuentas personales.
5. Para una ejecución nueva, vaciar `runId`. El primer paso genera un GUID sin
   guiones y descarta los tokens/OTP internos de la ejecución anterior. No ejecutar
   antes las plantillas de registro de 6.0 con ese mismo runId: los correos de la
   suite deben estar libres. Una repetición completa usa un runId nuevo; una
   continuación conserva runId y `_authState`.

Los nombres son `TEST_<runId>_<persona>`. Se generan cuentas buyer y producer con
las plantillas normales y cuentas separadas para intentos OTP (`<runId>-otp`) y
recuperación (`<runId>-rec`). El menor y el email inexistente usan sufijos distintos
y solo se utilizan en sus pruebas negativas. La sustitución de sufijos debe estar
cubierta por el enrutamiento autorizado del buzón. No se reutilizan usuarios ajenos.

### Primera etapa: prepare

1. Establecer `authStage=prepare` y dejar `authSelectedCases` vacío para todos los
   casos, o escribir una lista separada por comas, por ejemplo
   `CP-AUT-01,CP-AUT-02,CP-AUT-03,CP-AUT-04,CP-AUT-07,CP-AUT-12`.
2. En Collection Runner seleccionar **solo** la carpeta
   `05 - Autenticación CP-AUT-01 a CP-AUT-13`, conservar el orden, una iteración y
   habilitar la persistencia local de los cambios de variables. No correr toda la
   colección ni usar varias iteraciones con el mismo runId.
3. Esta etapa registra hasta cuatro cuentas de pruebas y comprueba los registros
   y la restricción de edad; deja pendientes las verificaciones y solicita la
   recuperación de la cuenta separada. Crea códigos reales por SMTP.
4. Leer el buzón autorizado fuera de Postman y suministrar:
   - `verificationOtp`: OTP de registro de buyer, para CP-AUT-06.
   - `authAttemptsOtp`: OTP original de la cuenta con sufijo `-otp`, para CP-AUT-07.
   - `recoveryOtp`: OTP **de recuperación** de la cuenta con sufijo `-rec`, para CP-AUT-12.
   - `authOtpConfirmedRunId`: copiar el runId actual para confirmar que los códigos
     se obtuvieron del mensaje y destinatario correctos de esa ejecución.
5. Introducir los códigos como texto de seis dígitos conservando ceros iniciales.
   No extraerlos de SQL, logs ni endpoints ocultos. No se generan códigos correctos
   por script. Los códigos incorrectos de CP-AUT-07/13 se derivan del real sumando
   uno módulo 1 000 000, por lo que son distintos del código proporcionado.

El servicio da 15 minutos a los OTP. La suite usa un margen de 14 minutos para
flujos que exigen vigencia. No mezclar mensajes ni dejar pasar horas entre
prepare y execute. Una precondición vencida queda bloqueada, no aprobada.

### Segunda etapa: execute y puntos de continuación

1. Cambiar `authStage=execute`, conservar runId y estado, y ejecutar la misma
   carpeta con una iteración. Dejar la selección vacía permite cubrir todos los
   casos con precondiciones satisfechas; una selección parcial debe incluir o
   haber completado sus dependencias.
2. CP-AUT-06 exige GET pendiente antes de enviar OTP y GET verificada después.
   CP-AUT-08 depende de esa verificación, no del login permisivo actual de una
   cuenta pendiente. El JWT se guarda como secreto sin imprimirlo.
3. CP-AUT-07 envía tres errores y luego el original correcto. Los tres errores
   genéricos por sí solos no aprueban nada. La suite comprueba la cuenta pendiente,
   solicita otro código y se detiene sin enviar la verificación siguiente mientras
   falte `authReplacementOtp`. Copiar ese **nuevo** OTP recibido y volver a ejecutar
   con `authSelectedCases=CP-AUT-07`. Los pasos ya respondidos no se repiten.
   Con el backend actual se prevé FALLIDO por 400 frente a 403/422; el flujo puede
   continuar recabando evidencia del nuevo código sin ocultar ese fallo.
4. CP-AUT-12 realiza todas las comprobaciones de contraseña y reutilización de OTP.
   Usa una cuenta distinta, por lo que no cambia la contraseña de buyer o producer.
   La contraseña vigente de recuperación pasa a newPassword tras reset 200. Se
   conserva el OTP hasta probar su inutilización y se vacía al terminar el caso.
5. CP-AUT-13 solicita un OTP nuevo, vacía el anterior y queda pendiente de introducir
   el recibido. Proporcionar `recoveryOtp` del mensaje nuevo y volver a ejecutar
   `authSelectedCases=CP-AUT-13`. `authRecoveryCodeMode=invalid` es el valor inicial:
   se envía un código distinto y se comprueba que la contraseña vigente se conserva.
   La contraseña anterior **a este intento** es newPassword, porque CP-AUT-12 ya
   cambió la contraseña de esa cuenta.
6. Para la variante `expired`, elegir el modo **antes** de ejecutar el intento 13.2,
   copiar el código real y esperar al menos 16 minutos desde la respuesta a recover,
   sin reenviar ni sustituir el OTP. El rechazo debe llevar el mensaje específico
   de expiración; un error genérico no demuestra un código vencido. Para medir
   ambas variantes usar dos ejecuciones nuevas; un paso ya respondido no se reenvía.
7. CP-AUT-11 conserva el JWT emitido por CP-AUT-08 y lo acredita con un GET 200
   mientras está vigente. Después queda bloqueado hasta el vencimiento natural
   más la tolerancia temporal. La API emite tokens de dos horas; Program no cambia
   el [ClockSkew predeterminado de 300 segundos](https://learn.microsoft.com/en-us/dotnet/api/microsoft.identitymodel.tokens.tokenvalidationparameters.defaultclockskew?view=msal-web-dotnet-latest).
   Conservar el environment local y repetir `authSelectedCases=CP-AUT-11` tras la
   espera. Se exige el mismo hash SHA-256, origen, runId e identidad que la aceptación
   previa. `expiredJwt` se rellena con ese token, no con uno malformado o fabricado.
   No se espera dos horas automáticamente ni se altera el reloj de servidor/cliente.

El Runner continúa con los otros pasos/casos cuando encuentra un punto sin OTP;
no abre una ventana para solicitarlo ni espera activamente. La continuación es
una nueva ejecución del Runner con el **mismo estado local** y el caso seleccionado.
Los pasos ya respondidos se omiten conservando su resultado y fecha originales.
Un paso omitido no aporta nueva evidencia ni convierte el caso en aprobado.

La configuración de buzón, plantillas y las tres contraseñas ficticias se vincula
a la ejecución mediante una huella interna; no cambiarlas al reanudar. Esto evita
aprobar el rechazo de una contraseña “anterior” diferente de la realmente usada.
Si deben cambiar, preparar una ejecución nueva. La huella no aparece en la
evidencia pública y no sustituye las comprobaciones HTTP de credenciales.

Un timeout/cancelación entre envío y respuesta deja el paso como indeterminado.
La suite no reenvía automáticamente un registro ni un intento OTP cuyo resultado
se desconoce. No editar `_authState` para forzar un resultado: revisar con el
responsable y preparar una ejecución nueva si no se puede recuperar con seguridad.

### Variables nuevas y evidencia

| Variable | Valor inicial / finalidad |
| --- | --- |
| `authStage` | `prepare`; después `execute` |
| `authSelectedCases` | Vacío = todos; lista de identificadores exactos separados por comas |
| `authOtpConfirmedRunId` | Vacío; confirmación de procedencia de los códigos del buzón |
| `authAttemptsOtp`, `authReplacementOtp` | Vacíos, secret; código original y sustituto de la cuenta de intentos |
| `authRecoveryCodeMode` | `invalid` o `expired`; no equivale a probar ambas variantes |
| `authClockSkewSeconds` | `300`; ajustar solo si la configuración real autorizada lo cambia |
| `authBuyerJwt`, `authProducerJwt`, `authAttemptsJwt`, `authRecoveryJwt` | Vacíos, secret; tokens reales de los respectivos login válidos |
| `authExpiredCandidateJwt` | Vacío, secret; token conservado de CP-AUT-08 para acreditar y probar expiración |
| `_authState` | Vacío; pasos enviados/respondidos, IDs reales, claims no secretos, tiempos y dependencias. No contiene contraseñas, OTP ni JWT completos |
| `authResults` | Vacío; matriz por escenario con runId, estado y motivo |
| `authEvidence` | Vacío; fecha, caso, paso, método, ruta, estado HTTP, comprobaciones booleanas y resultado; sin cuerpos ni secretos |

No se almacenan IDs inventados: buyerUserId y producerUserId vienen de GET reales.
Un JWT se captura únicamente de login 200 con formato, sub, rol y exp coherentes;
su uso en GET prueba que el servidor acepta su firma. No se utiliza la clave JWT
en Postman. CP-AUT-02 prueba el rol y acceso autenticado; no constituye por sí solo
una auditoría de autorización de todos los recursos de productor.

Leer `authResults` para el resultado por caso y `authEvidence` para los pasos.
Las aserciones positivas individuales no equivalen a casos aprobados: APROBADO
solo se asigna cuando **todos los pasos obligatorios** tienen respuesta real y
cumplen sus comprobaciones. Si falta un OTP/espera se marca BLOQUEADO; si una
aserción real falla se conserva FALLIDO, incluso si después se completa parte
del flujo. CP-AUT-05/10 siempre permanecen NO IMPLEMENTADOS.

Conservar únicamente esas evidencias saneadas para actualizar esta matriz tras
una ejecución autorizada. Postman puede guardar cuerpos, Authorization y respuestas
en su historial propio: no compartir el environment activo, consola, capturas ni
informes sin sanear. Las pruebas no imprimen contraseñas, OTP ni JWT. El archivo
exportable versionado siempre mantiene esos valores vacíos.

## Hallazgos y discrepancias

Estos hallazgos proceden de **inspección estática**, no de respuestas HTTP observadas
en esta actividad. Los códigos indicados son los que produciría la ruta según el
código; donde no existe ruta se escribe N/A. Cada reproducción requiere primero
autorización y cuentas de pruebas; no se ejecutó contra la instancia compartida.

| Registro / caso | Requisito esperado | Comportamiento del código / ruta y estado previsto | Reproducción autorizada / evidencia | Severidad propuesta / recomendación |
| --- | --- | --- | --- | --- |
| AUT-D01 / CP-AUT-04 | “Más de 18 años” | SQL rechaza solo `<18`, por lo que **exactamente 18 se admite**. POST auth/register: 201 si SMTP funciona; un fallo posterior de correo puede ser 503. El mensaje de error también dice “mayor de 18” | Usar una cuenta nueva con cumpleaños exactamente 18 años antes de la fecha SQL y otra con un día menos. No ejecutado. Evidencia: [USP_CreateUser](../database/scripts/02_USP_CreateUser.sql), condición `IF @age < 18`, y ErrorMessages.AgeRestriction | Media por ambigüedad; acordar si el requisito es >=18 o >18 y alinear requisito y mensaje. No se cambió la regla |
| AUT-D02 / CP-AUT-05 | Alta administrativa protegida por Administrador Maestro y código de autorización | No existe flujo específico ni generación del código de autorización. Ruta y estado: N/A | Inventariar [AuthController](../backend/Tequio.Api/Controllers/AuthController.cs) y [IAuthService](../backend/Tequio.Domain/Services/IAuthService.cs). No probar una URL inventada | Alta; implementar el caso de uso con autorización y código, sin reutilizar un registro público de roles administrativos |
| AUT-D03 / CP-AUT-10 | Segundo factor antes del JWT de Administrador Maestro | POST auth/login llama AuthenticateUserAsync y devuelve JWT directamente, sin comprobar rol para 2FA. Estado previsto 200 con credenciales válidas | Inspección de AuthenticateUserAsync y [JwtProvider](../backend/Tequio.Domain/Services/IJwtProvider.cs). No se usaron credenciales administrativas | Alta; diseñar desafío 2FA y emitir JWT solo tras completarlo |
| AUT-D04 / CP-AUT-07 | Tres errores invalidan OTP y el plan exige 403/422 | SQL reduce RemainingAttempts de 3 a 0 y rechaza después el correcto, pero 50003 y 50005 se mapean a ArgumentException y terminan en **400**, con el mismo mensaje genérico | Ejecutar los 12 pasos preparados con OTP original y nuevo del buzón. Evidencia estática: [validador OTP](../database/scripts/05_USP_ValidateVerificationCode.sql), [mapper](../backend/Tequio.Infrastructure/Helpers/SqlExceptionMapper.cs) y [middleware](../backend/Tequio.Api/Middlewares/GlobalExceptionMiddleware.cs). Sin evidencia HTTP todavía | Media; acordar contrato y diferenciar agotamiento de intentos. Mantener la expectativa 403/422 hasta revisión del plan; no convertir los tres 400 en APROBADO |
| AUT-D05 / CP-AUT-06, CP-AUT-08 | Examinar restricción de acceso de cuentas sin verificar | AuthenticateUserAsync consulta hash/rol y emite JWT sin comprobar IsVerified. POST auth/login: 200 previsto para cuenta pendiente con contraseña correcta | Los setup de CP-AUT-01/02/07/12 registran el perfil pendiente y su sesión, sin confundirlo con CP-AUT-08. Evidencia: AuthenticateUserAsync y GetUserAuthDataAsync. Sin ejecución real | Alta si RN-11 exige bloquear el acceso; confirmar política y validar estado antes de emitir JWT. No añadir una regla ficticia a las pruebas |
| AUT-D06 / CP-AUT-01/02/06/07/12/13 | El registro y las operaciones dependientes de OTP deben informar un fallo SMTP sin aparentar éxito | RegisterUserAsync persiste usuario y código **antes** del envío; EmailDeliveryException produce 503 con mensaje saneado. Un 503 no implica que el correo quede libre ni que no se haya consumido cuota | En un entorno aislado/autorizado se podría interrumpir SMTP después de persistir; **no se indujo un fallo en el entorno compartido**. Evidencia: orden de RegisterUserAsync, EmailService y middleware | Media; documentar estado parcial, permitir recuperación/reenvío y considerar outbox. La suite marca BLOQUEADO POR SMTP solo ante el 503 específico, no reintenta el registro |
| AUT-D07 / CP-AUT-05, CP-AUT-10 | Los roles administrativos no deben poder elegirse en registro público | Registro es anónimo, RoleId solo tiene Required, repositorio transmite cualquier entero y SQL no limita a 1/2. La FK admite roles 3/4 del seed. Riesgo de alta privilegiada por la ruta pública; 201/503 previstos, **no observado por HTTP** | Evidencia: AuthController, RegisterRequestDto, CreateUserAsync, USP_CreateUser y seed de roles. No se enviaron roles 3/4 ni se creó un administrador | Crítica propuesta; restringir roles públicos en servidor y separar el flujo administrativo. Requiere revisión de seguridad; la suite no explota ese riesgo |
| AUT-D08 / CP-AUT-12 | Consumo del OTP y cambio de contraseña consistentes ante fallo | ResetPasswordAsync valida/consume el OTP y después actualiza contraseña en otra llamada, sin transacción que englobe ambas. Si falla la actualización puede quedar OTP consumido y contraseña anterior vigente. 500 previsto por fallo interno, no observado | Evidencia: ResetPasswordAsync y repositorios de código/contraseña. No se alteró SQL ni se indujeron fallos de persistencia en la instancia compartida | Media; considerar atomicidad para consumo y actualización; validar posteriormente en entorno controlado |

También se revisó expiración: OTP dura 15 minutos, la validación comprueba fecha
y pone intentos a cero al detectar vencimiento. Los tipos AccountVerification y
PasswordRecovery son distintos. El código correcto se consume al usarlo y el
tipo recuperación no marca la cuenta como verificada. CP-AUT-12/13 mantienen
comprobaciones para demostrarlo con API; no se da por aprobado por leer el SQL.
`VerifyAccountAsync` retorna 200 sin validar OTP si IsVerified ya era true, de ahí
la precondición GET pendiente de CP-AUT-06/07.

No existe consulta pública de usuarios por email ni listado administrativo para
contar duplicados. CP-AUT-03 comprueba rechazo, identidad, nombres y credenciales
invariantes por API; la unicidad de Email aparece además en el DDL, sin ejecutar
una consulta SQL ni afirmar un recuento de filas medido.

## Verificación de los artefactos

Validación reproducible sin ejecutar la API:

```powershell
node postman/validate-auth.cjs
```

El validador utiliza Node y respuestas **sintéticas solo en memoria** para probar
la lógica de los scripts, nunca como evidencia de integración. Con el esquema
oficial v2.1 descargado previamente también se ejecutó la opción `--schema` con
la ruta local de ese archivo, aprovechando Ajv del Newman 6.2.2 existente. No se
instalaron dependencias. Se aprobaron **22 comprobaciones offline**, incluyendo:

- JSON y sintaxis JavaScript válidos; 13 carpetas y 65 solicitudes.
- Comparación exacta de las primeras cinco carpetas con las 16 peticiones de
  `f701003`, y deserialización del environment/colección con el SDK de Postman.
- Validación contra el esquema oficial Collection v2.1.
- Referencias de variables definidas, secretos exportados vacíos, y cancelación
  de las 65 solicitudes con los valores exportados sin configurar.
- Bloqueos por origen, autorización, buzón, SMTP, OTP ausente y peticiones de
  resultado indeterminado; ninguna repetición automática de registros o intentos.
- Conservación del fallo esperado del plan para CP-AUT-07 al simular un 400,
  reanudación sin repetir los tres intentos, postcondiciones de recuperación y
  procedencia del token antes de la prueba de expiración.
- Ausencia de secretos en authEvidence y separación de estados por escenario.

**Solicitudes HTTP reales: 0. Operaciones SQL: 0. Escenarios de integración
aprobados: 0.** No se ejecutó Postman Runner/Newman contra la API, no se midió
cobertura y no se presenta la validación estructural como un resultado CP-AUT.

Al finalizar una ejecución futura, conservar evidencia saneada, actualizar esta
matriz con estados reales y volver a `allowMutatingTests=false`. No borrar
usuarios ni tablas para repetir: usar un runId nuevo. No reiniciar contenedores,
recrear bases ni consultar SQL para aprobar casos. Esta actividad solo modifica
artefactos Postman y documentación; no hace commit ni push.
