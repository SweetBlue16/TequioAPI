# Autorización Admin, correo y navegación

## Diagnóstico y resultado real

La auditoría encontró una sola cuenta Admin pendiente (rol 3, `IsVerified=0`) y un código de
`AdminAuthorization` sin intentos disponibles. Los logs anteriores mostraban un envío aceptado por SMTP;
esto no prueba llegada a bandeja. No se leyeron ni exportaron contraseñas, códigos o tokens.

Había dos errores encadenados: el procedimiento de validación solo activaba cuentas con propósito
`AccountVerification`, por lo que una autorización Admin válida consumía el código sin activar la cuenta;
después se intentaba enviar un supuesto comprobante usando un valor no numérico y vigencia cero a través
del servicio de OTP, que rechazaba ese formato. Se eliminó ese envío incorrecto. No se simuló una
confirmación ni se omitió la autorización.

Se instaló **18_AdminAuthorization.sql**, incremental, después de los scripts existentes, con respaldo
COPY_ONLY previo. La instalación no cambia el estado de verificación de ninguna cuenta.
El procedimiento ahora consume el OTP y activa la cuenta Admin en una única transacción, únicamente si
el código corresponde, sigue vigente y tiene intentos. Los intentos fallidos se guardan antes de devolver error.

Resultado de la continuación real: reenvío HTTP 200 aceptado por SMTP, segundo reenvío inmediato HTTP 429,
misma cuenta e identificador conservados. El propietario introdujo el código localmente mediante el
asistente y confirmó la autorización. La consulta SQL posterior comprobó **rol 3, IsVerified=1, una sola
cuenta Admin, último código AdminAuthorization consumido**. No se marcó verificada manualmente.
Backend y proxy siguen respondiendo 200 contra TequioDB.

## Destinatarios y separación de propósitos

| Propósito | Destino | Qué habilita |
|---|---|---|
| AccountVerification | Correo del comprador/productor que se registra | Su propia cuenta; no autoriza Admin |
| AdminAuthorization | Correo del MasterAdmin autenticado, obtenido desde su perfil | La solicitud pendiente de la cuenta Admin seleccionada |
| PasswordRecovery | Correo del titular | Cambio de contraseña; no autoriza Admin |

La nueva cuenta Admin no debe buscar el código de autorización en su propia bandeja: se envía a MasterAdmin.
La autorización Admin completa su estado de verificación dentro del flujo protegido; no se envía otro OTP
público ni un comprobante con apariencia de código. SMTP, remitente y credenciales locales permanecen intactos.

## Continuación, límites y permisos

- `POST /api/v1/auth/resend-admin-authorization`, cuerpo `{ "email": "<correo Admin existente>" }`:
  solo MasterAdmin; 200 al enviar, 401 sin sesión, 403 para otros roles, 429 si se alcanzó un límite.
- `POST /api/v1/auth/register-admin`: una solicitud para Admin pendiente continúa sobre la misma cuenta;
  no inserta otra, no cambia contraseña ni datos del registro existente. Otros correos duplicados se rechazan.
- `POST /api/v1/auth/authorize-admin`: solo MasterAdmin; valida el propósito AdminAuthorization y activa
  la cuenta de forma atómica en SQL. Un rol distinto de 3 no es un destino de autorización válido.
- Vigencia 15 minutos, 3 intentos de validación. Reenviar un código todavía vigente **conserva los intentos
  restantes**, invalida el anterior y no desbloquea un desafío agotado antes de su vencimiento.
- Al menos 60 segundos entre solicitudes y como máximo 3 emisiones en una ventana de 15 minutos.
  La API devuelve 429 y Retry-After; los intentos rechazados no generan correo ni otro registro de código.
- La generación y validación se serializan por usuario dentro de SQL, incluyendo solicitudes concurrentes.
- El flujo público de verificación/reenvío no autoriza ni emite códigos AccountVerification para roles 3/4.
  El registro público solo permite Comprador/Productor.
- Un Admin pendiente no puede iniciar sesión. Las sesiones Admin antiguas, emitidas antes de esta corrección,
  se rechazan: después de autorizar la cuenta se requiere iniciar sesión nuevamente. Los nuevos JWT Admin
  incluyen una marca firmada de autorización, emitida únicamente después de la comprobación de estado.

El asistente local es compatible con Windows PowerShell 5.1 y captura datos ocultos:

```powershell
# Continuar una solicitud cuyo código ya se recibió:
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "C:\Projects\TEQUIO\Create-TequioAdmin.ps1" -AuthorizeOnly

# Reenviar para la cuenta existente sin pedir de nuevo contraseña/nombre:
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "C:\Projects\TEQUIO\Create-TequioAdmin.ps1" -ResendOnly
```

No enviar códigos por chat. Si la cuenta ya está autorizada no necesita otro reenvío.

## Navegación

Navbar muestra **Panel de administración** solo a rol 3, en escritorio y móvil, hacia `/admin`.
Se conservan los menús anteriores de Comprador, Productor y MasterAdmin. El panel existente contiene
**Puntos de entrega** hacia `/admin/puntos-entrega`. No se agregó una pantalla de alta de administradores.
La barra lateral usa el nombre/avatar de la sesión y su salida borra el token; ya no usa un nombre fijo.
El login dejó de imprimir errores nativos que podían incluir datos de la solicitud.

Para comprobarlo: cerrar la sesión anterior, iniciar sesión con Admin en `http://127.0.0.1:5173/login`,
seguir Panel de administración → Puntos de entrega y recargar para comprobar restauración de sesión.
MasterAdmin conserva su rol 4 y no obtiene acceso al CRUD de puntos.

## Pruebas y evidencia

- Backend: **151 aprobadas, 0 omitidas, 0 fallidas**, con SQL Server aislado configurado.
- SQL: siete casos nuevos de autorización atómica, código de un solo uso, intentos, expiración, reenvío sin
  restablecer intentos, límites temporales, propósito público rechazado y concurrencia; además siguen
  aprobadas las nueve pruebas de puntos de entrega.
- HTTP con la aplicación y JWT reales: endpoint de reenvío 401/403/200 según identidad; 429/Retry-After;
  rechazo de JWT Admin antiguos. Las dependencias de correo y datos se simulan en estas pruebas HTTP.
- Servicio: destinatarios correctos, continuación sin duplicado, fallo SMTP recuperable, ausencia del
  comprobante inválido, rechazo de login Admin pendiente y del bypass público.
- Frontend: **99 aprobadas en 15 suites**. Diez casos nuevos cubren visibilidad por rol, acceso a panel/puntos,
  menú móvil, cierre de sesión, login con claims reales y restauración/protección. Tras retirar el log nativo,
  se repitieron los 13 casos afectados de Login/Navbar, todos aprobados.
- Compilación backend/frontend y lint de archivos de navegación modificados: aprobados. Persisten los
  avisos NuGet/Vite preexistentes documentados en la entrega anterior.

Evidencia sin secretos: [admin-flow_2026-11-16.json](../postman/evidence/admin-flow_2026-11-16.json).
SMTP aceptado y autorización completada por el propietario se distinguen de una inspección de bandeja:
no se accedió a su correo. No hubo cambios de credenciales, commits ni push.


## Revalidación posterior a las pruebas manuales del usuario

El propietario informó aprobadas seis pruebas manuales: persistencia al recargar, edición, mapa,
duplicados, desactivación y acceso desde el menú. Son resultados del usuario, no una revisión visual
ejecutada por el agente. Se repitieron 151 pruebas backend (incluidas SQL, sin omisiones), las 21 pruebas
frontend afectadas y CP-PTO con 47 solicitudes/83 aserciones: todas aprobadas. No se reenviaron códigos
ni se alteró otra vez la cuenta Admin durante este cierre.

Reportes y estado de 5.11, 5.12, 5.13 y 6.2.1: [cierre actualizado](PUNTOS_ENTREGA.md#cierre-actualizado--16-de-noviembre-de-2026).
Solo permanecen los avisos/deuda técnica previos descritos allí, no bloqueos funcionales del módulo.
