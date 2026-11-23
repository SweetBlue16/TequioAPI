// This function supplies the corresponding standalone Postman event.
module.exports = function prepareAuth({ pm, Date, require }) {
const authManifest = {
  "CP-AUT-01 / 01 - Registrar comprador ficticio": {
    "id": "CP-AUT-01",
    "key": "01.1",
    "name": "CP-AUT-01 / 01 - Registrar comprador ficticio",
    "action": "register",
    "persona": "buyer",
    "stage": "prepare",
    "method": "POST",
    "route": "/api/v1/auth/register",
    "statuses": [
      201
    ],
    "requires": []
  },
  "CP-AUT-01 / 02 - Obtener identidad y rol de la cuenta creada": {
    "id": "CP-AUT-01",
    "key": "01.2",
    "name": "CP-AUT-01 / 02 - Obtener identidad y rol de la cuenta creada",
    "action": "login",
    "persona": "buyer",
    "stage": "prepare",
    "method": "POST",
    "route": "/api/v1/auth/login",
    "statuses": [
      200
    ],
    "requires": [
      "01.1"
    ]
  },
  "CP-AUT-01 / 03 - Consultar persistencia del comprador": {
    "id": "CP-AUT-01",
    "key": "01.3",
    "name": "CP-AUT-01 / 03 - Consultar persistencia del comprador",
    "action": "profile",
    "persona": "buyer",
    "stage": "prepare",
    "method": "GET",
    "route": "/api/v1/users/me",
    "statuses": [
      200
    ],
    "requires": [
      "01.2"
    ],
    "verified": false
  },
  "CP-AUT-02 / 01 - Registrar productor ficticio": {
    "id": "CP-AUT-02",
    "key": "02.1",
    "name": "CP-AUT-02 / 01 - Registrar productor ficticio",
    "action": "register",
    "persona": "producer",
    "stage": "prepare",
    "method": "POST",
    "route": "/api/v1/auth/register",
    "statuses": [
      201
    ],
    "requires": []
  },
  "CP-AUT-02 / 02 - Obtener identidad y rol del productor": {
    "id": "CP-AUT-02",
    "key": "02.2",
    "name": "CP-AUT-02 / 02 - Obtener identidad y rol del productor",
    "action": "login",
    "persona": "producer",
    "stage": "prepare",
    "method": "POST",
    "route": "/api/v1/auth/login",
    "statuses": [
      200
    ],
    "requires": [
      "02.1"
    ]
  },
  "CP-AUT-02 / 03 - Consultar persistencia del productor": {
    "id": "CP-AUT-02",
    "key": "02.3",
    "name": "CP-AUT-02 / 03 - Consultar persistencia del productor",
    "action": "profile",
    "persona": "producer",
    "stage": "prepare",
    "method": "GET",
    "route": "/api/v1/users/me",
    "statuses": [
      200
    ],
    "requires": [
      "02.2"
    ],
    "verified": false
  },
  "CP-AUT-03 / 01 - Repetir registro del mismo comprador": {
    "id": "CP-AUT-03",
    "key": "03.1",
    "name": "CP-AUT-03 / 01 - Repetir registro del mismo comprador",
    "action": "duplicate",
    "persona": "buyer",
    "stage": "prepare",
    "method": "POST",
    "route": "/api/v1/auth/register",
    "statuses": [
      400,
      409
    ],
    "requires": [
      "01.3"
    ]
  },
  "CP-AUT-03 / 02 - Iniciar sesión con credenciales originales": {
    "id": "CP-AUT-03",
    "key": "03.2",
    "name": "CP-AUT-03 / 02 - Iniciar sesión con credenciales originales",
    "action": "login",
    "persona": "buyer",
    "stage": "prepare",
    "method": "POST",
    "route": "/api/v1/auth/login",
    "statuses": [
      200
    ],
    "requires": [
      "03.1"
    ]
  },
  "CP-AUT-03 / 03 - Comprobar ID y datos originales sin duplicación observable": {
    "id": "CP-AUT-03",
    "key": "03.3",
    "name": "CP-AUT-03 / 03 - Comprobar ID y datos originales sin duplicación observable",
    "action": "profile",
    "persona": "buyer",
    "stage": "prepare",
    "method": "GET",
    "route": "/api/v1/users/me",
    "statuses": [
      200
    ],
    "requires": [
      "03.2"
    ]
  },
  "CP-AUT-04 / 01 - Rechazar registro de menor de edad": {
    "id": "CP-AUT-04",
    "key": "04.1",
    "name": "CP-AUT-04 / 01 - Rechazar registro de menor de edad",
    "action": "minor",
    "persona": "minor",
    "stage": "prepare",
    "method": "POST",
    "route": "/api/v1/auth/register",
    "statuses": [
      400
    ],
    "requires": []
  },
  "CP-AUT-04 / 02 - Comprobar que no se crearon credenciales utilizables": {
    "id": "CP-AUT-04",
    "key": "04.2",
    "name": "CP-AUT-04 / 02 - Comprobar que no se crearon credenciales utilizables",
    "action": "badLogin",
    "persona": "minor",
    "stage": "prepare",
    "method": "POST",
    "route": "/api/v1/auth/login",
    "statuses": [
      401
    ],
    "requires": [
      "04.1"
    ],
    "useCorrectPassword": true
  },
  "CP-AUT-06 / 01 - Iniciar sesión de la cuenta de prueba pendiente": {
    "id": "CP-AUT-06",
    "key": "06.1",
    "name": "CP-AUT-06 / 01 - Iniciar sesión de la cuenta de prueba pendiente",
    "action": "login",
    "persona": "buyer",
    "stage": "execute",
    "method": "POST",
    "route": "/api/v1/auth/login",
    "statuses": [
      200
    ],
    "requires": [
      "01.3"
    ]
  },
  "CP-AUT-06 / 02 - Confirmar precondición de cuenta pendiente": {
    "id": "CP-AUT-06",
    "key": "06.2",
    "name": "CP-AUT-06 / 02 - Confirmar precondición de cuenta pendiente",
    "action": "profile",
    "persona": "buyer",
    "stage": "execute",
    "method": "GET",
    "route": "/api/v1/users/me",
    "statuses": [
      200
    ],
    "requires": [
      "06.1"
    ],
    "verified": false
  },
  "CP-AUT-06 / 03 - Enviar OTP correcto del buzón": {
    "id": "CP-AUT-06",
    "key": "06.3",
    "name": "CP-AUT-06 / 03 - Enviar OTP correcto del buzón",
    "action": "verify",
    "persona": "buyer",
    "stage": "execute",
    "method": "POST",
    "route": "/api/v1/auth/verify",
    "statuses": [
      200
    ],
    "requires": [
      "06.2"
    ],
    "otp": "verificationOtp"
  },
  "CP-AUT-06 / 04 - Comprobar cuenta verificada mediante GET": {
    "id": "CP-AUT-06",
    "key": "06.4",
    "name": "CP-AUT-06 / 04 - Comprobar cuenta verificada mediante GET",
    "action": "profile",
    "persona": "buyer",
    "stage": "execute",
    "method": "GET",
    "route": "/api/v1/users/me",
    "statuses": [
      200
    ],
    "requires": [
      "06.3"
    ],
    "verified": true
  },
  "CP-AUT-07 / 01 - Preparar cuenta exclusiva para intentos OTP": {
    "id": "CP-AUT-07",
    "key": "07.1",
    "name": "CP-AUT-07 / 01 - Preparar cuenta exclusiva para intentos OTP",
    "action": "register",
    "persona": "attempts",
    "stage": "prepare",
    "method": "POST",
    "route": "/api/v1/auth/register",
    "statuses": [
      201
    ],
    "requires": []
  },
  "CP-AUT-07 / 02 - Obtener sesión de la cuenta de intentos": {
    "id": "CP-AUT-07",
    "key": "07.2",
    "name": "CP-AUT-07 / 02 - Obtener sesión de la cuenta de intentos",
    "action": "login",
    "persona": "attempts",
    "stage": "prepare",
    "method": "POST",
    "route": "/api/v1/auth/login",
    "statuses": [
      200
    ],
    "requires": [
      "07.1"
    ]
  },
  "CP-AUT-07 / 03 - Confirmar que está pendiente de verificación": {
    "id": "CP-AUT-07",
    "key": "07.3",
    "name": "CP-AUT-07 / 03 - Confirmar que está pendiente de verificación",
    "action": "profile",
    "persona": "attempts",
    "stage": "prepare",
    "method": "GET",
    "route": "/api/v1/users/me",
    "statuses": [
      200
    ],
    "requires": [
      "07.2"
    ],
    "verified": false
  },
  "CP-AUT-07 / 04 - Enviar primer OTP incorrecto": {
    "id": "CP-AUT-07",
    "key": "07.4",
    "name": "CP-AUT-07 / 04 - Enviar primer OTP incorrecto",
    "action": "wrongOtp",
    "persona": "attempts",
    "stage": "execute",
    "method": "POST",
    "route": "/api/v1/auth/verify",
    "statuses": [
      400,
      403,
      422
    ],
    "requires": [
      "07.3"
    ]
  },
  "CP-AUT-07 / 05 - Enviar segundo OTP incorrecto": {
    "id": "CP-AUT-07",
    "key": "07.5",
    "name": "CP-AUT-07 / 05 - Enviar segundo OTP incorrecto",
    "action": "wrongOtp",
    "persona": "attempts",
    "stage": "execute",
    "method": "POST",
    "route": "/api/v1/auth/verify",
    "statuses": [
      400,
      403,
      422
    ],
    "requires": [
      "07.4"
    ]
  },
  "CP-AUT-07 / 06 - Exigir bloqueo tras tercer OTP incorrecto": {
    "id": "CP-AUT-07",
    "key": "07.6",
    "name": "CP-AUT-07 / 06 - Exigir bloqueo tras tercer OTP incorrecto",
    "action": "wrongOtp",
    "persona": "attempts",
    "stage": "execute",
    "method": "POST",
    "route": "/api/v1/auth/verify",
    "statuses": [
      403,
      422
    ],
    "requires": [
      "07.5"
    ],
    "planLockout": true
  },
  "CP-AUT-07 / 07 - Comprobar que el código original correcto ya no funciona": {
    "id": "CP-AUT-07",
    "key": "07.7",
    "name": "CP-AUT-07 / 07 - Comprobar que el código original correcto ya no funciona",
    "action": "originalOtp",
    "persona": "attempts",
    "stage": "execute",
    "method": "POST",
    "route": "/api/v1/auth/verify",
    "statuses": [
      403,
      422
    ],
    "requires": [
      "07.6"
    ],
    "planLockout": true
  },
  "CP-AUT-07 / 08 - Renovar sesión para consultar la cuenta": {
    "id": "CP-AUT-07",
    "key": "07.8",
    "name": "CP-AUT-07 / 08 - Renovar sesión para consultar la cuenta",
    "action": "login",
    "persona": "attempts",
    "stage": "execute",
    "method": "POST",
    "route": "/api/v1/auth/login",
    "statuses": [
      200
    ],
    "requires": [
      "07.7"
    ]
  },
  "CP-AUT-07 / 09 - Comprobar que sigue sin verificar": {
    "id": "CP-AUT-07",
    "key": "07.9",
    "name": "CP-AUT-07 / 09 - Comprobar que sigue sin verificar",
    "action": "profile",
    "persona": "attempts",
    "stage": "execute",
    "method": "GET",
    "route": "/api/v1/users/me",
    "statuses": [
      200
    ],
    "requires": [
      "07.8"
    ],
    "verified": false
  },
  "CP-AUT-07 / 10 - Solicitar un nuevo OTP por correo": {
    "id": "CP-AUT-07",
    "key": "07.10",
    "name": "CP-AUT-07 / 10 - Solicitar un nuevo OTP por correo",
    "action": "resend",
    "persona": "attempts",
    "stage": "execute",
    "method": "POST",
    "route": "/api/v1/auth/resend-verification-code",
    "statuses": [
      200
    ],
    "requires": [
      "07.9"
    ]
  },
  "CP-AUT-07 / 11 - Verificar con el nuevo OTP recibido": {
    "id": "CP-AUT-07",
    "key": "07.11",
    "name": "CP-AUT-07 / 11 - Verificar con el nuevo OTP recibido",
    "action": "verify",
    "persona": "attempts",
    "stage": "execute",
    "method": "POST",
    "route": "/api/v1/auth/verify",
    "statuses": [
      200
    ],
    "requires": [
      "07.10"
    ],
    "otp": "authReplacementOtp"
  },
  "CP-AUT-07 / 12 - Comprobar recuperación del flujo con el nuevo código": {
    "id": "CP-AUT-07",
    "key": "07.12",
    "name": "CP-AUT-07 / 12 - Comprobar recuperación del flujo con el nuevo código",
    "action": "profile",
    "persona": "attempts",
    "stage": "execute",
    "method": "GET",
    "route": "/api/v1/users/me",
    "statuses": [
      200
    ],
    "requires": [
      "07.11"
    ],
    "verified": true
  },
  "CP-AUT-08 / 01 - Iniciar sesión del comprador verificado y capturar JWT": {
    "id": "CP-AUT-08",
    "key": "08.1",
    "name": "CP-AUT-08 / 01 - Iniciar sesión del comprador verificado y capturar JWT",
    "action": "login",
    "persona": "buyer",
    "stage": "execute",
    "method": "POST",
    "route": "/api/v1/auth/login",
    "statuses": [
      200
    ],
    "requires": [
      "06.4"
    ],
    "captureCandidate": true
  },
  "CP-AUT-08 / 02 - Contrastar sub y rol con el perfil protegido": {
    "id": "CP-AUT-08",
    "key": "08.2",
    "name": "CP-AUT-08 / 02 - Contrastar sub y rol con el perfil protegido",
    "action": "profile",
    "persona": "buyer",
    "stage": "execute",
    "method": "GET",
    "route": "/api/v1/users/me",
    "statuses": [
      200
    ],
    "requires": [
      "08.1"
    ],
    "verified": true
  },
  "CP-AUT-09 / 01 - Rechazar contraseña incorrecta de cuenta conocida": {
    "id": "CP-AUT-09",
    "key": "09.1",
    "name": "CP-AUT-09 / 01 - Rechazar contraseña incorrecta de cuenta conocida",
    "action": "badLogin",
    "persona": "buyer",
    "stage": "execute",
    "method": "POST",
    "route": "/api/v1/auth/login",
    "statuses": [
      401
    ],
    "requires": [
      "06.4"
    ]
  },
  "CP-AUT-09 / 02 - Comparar respuesta genérica con correo inexistente": {
    "id": "CP-AUT-09",
    "key": "09.2",
    "name": "CP-AUT-09 / 02 - Comparar respuesta genérica con correo inexistente",
    "action": "unknownLogin",
    "persona": "buyer",
    "stage": "execute",
    "method": "POST",
    "route": "/api/v1/auth/login",
    "statuses": [
      401
    ],
    "requires": [
      "09.1"
    ]
  },
  "CP-AUT-11 / 01 - Acreditar JWT legítimo mientras está vigente": {
    "id": "CP-AUT-11",
    "key": "11.1",
    "name": "CP-AUT-11 / 01 - Acreditar JWT legítimo mientras está vigente",
    "action": "attest",
    "persona": "buyer",
    "stage": "execute",
    "method": "GET",
    "route": "/api/v1/users/me",
    "statuses": [
      200
    ],
    "requires": [
      "08.2"
    ]
  },
  "CP-AUT-11 / 02 - Acceder con el mismo JWT después del vencimiento natural": {
    "id": "CP-AUT-11",
    "key": "11.2",
    "name": "CP-AUT-11 / 02 - Acceder con el mismo JWT después del vencimiento natural",
    "action": "expired",
    "persona": "buyer",
    "stage": "execute",
    "method": "GET",
    "route": "/api/v1/users/me",
    "statuses": [
      401
    ],
    "requires": [
      "11.1"
    ]
  },
  "CP-AUT-12 / 01 - Preparar cuenta exclusiva para recuperación": {
    "id": "CP-AUT-12",
    "key": "12.1",
    "name": "CP-AUT-12 / 01 - Preparar cuenta exclusiva para recuperación",
    "action": "register",
    "persona": "recovery",
    "stage": "prepare",
    "method": "POST",
    "route": "/api/v1/auth/register",
    "statuses": [
      201
    ],
    "requires": []
  },
  "CP-AUT-12 / 02 - Iniciar sesión antes de recuperación": {
    "id": "CP-AUT-12",
    "key": "12.2",
    "name": "CP-AUT-12 / 02 - Iniciar sesión antes de recuperación",
    "action": "login",
    "persona": "recovery",
    "stage": "prepare",
    "method": "POST",
    "route": "/api/v1/auth/login",
    "statuses": [
      200
    ],
    "requires": [
      "12.1"
    ]
  },
  "CP-AUT-12 / 03 - Confirmar identidad antes de recuperación": {
    "id": "CP-AUT-12",
    "key": "12.3",
    "name": "CP-AUT-12 / 03 - Confirmar identidad antes de recuperación",
    "action": "profile",
    "persona": "recovery",
    "stage": "prepare",
    "method": "GET",
    "route": "/api/v1/users/me",
    "statuses": [
      200
    ],
    "requires": [
      "12.2"
    ]
  },
  "CP-AUT-12 / 04 - Solicitar OTP de recuperación al buzón": {
    "id": "CP-AUT-12",
    "key": "12.4",
    "name": "CP-AUT-12 / 04 - Solicitar OTP de recuperación al buzón",
    "action": "recover",
    "persona": "recovery",
    "stage": "prepare",
    "method": "POST",
    "route": "/api/v1/auth/recover",
    "statuses": [
      200
    ],
    "requires": [
      "12.3"
    ]
  },
  "CP-AUT-12 / 05 - Restablecer contraseña con OTP correcto": {
    "id": "CP-AUT-12",
    "key": "12.5",
    "name": "CP-AUT-12 / 05 - Restablecer contraseña con OTP correcto",
    "action": "reset",
    "persona": "recovery",
    "stage": "execute",
    "method": "POST",
    "route": "/api/v1/auth/reset-password",
    "statuses": [
      200
    ],
    "requires": [
      "12.4"
    ],
    "otp": "recoveryOtp"
  },
  "CP-AUT-12 / 06 - Acceder con la nueva contraseña": {
    "id": "CP-AUT-12",
    "key": "12.6",
    "name": "CP-AUT-12 / 06 - Acceder con la nueva contraseña",
    "action": "login",
    "persona": "recovery",
    "stage": "execute",
    "method": "POST",
    "route": "/api/v1/auth/login",
    "statuses": [
      200
    ],
    "requires": [
      "12.5"
    ]
  },
  "CP-AUT-12 / 07 - Verificar identidad con la nueva sesión": {
    "id": "CP-AUT-12",
    "key": "12.7",
    "name": "CP-AUT-12 / 07 - Verificar identidad con la nueva sesión",
    "action": "profile",
    "persona": "recovery",
    "stage": "execute",
    "method": "GET",
    "route": "/api/v1/users/me",
    "statuses": [
      200
    ],
    "requires": [
      "12.6"
    ]
  },
  "CP-AUT-12 / 08 - Rechazar la contraseña anterior": {
    "id": "CP-AUT-12",
    "key": "12.8",
    "name": "CP-AUT-12 / 08 - Rechazar la contraseña anterior",
    "action": "oldLogin",
    "persona": "recovery",
    "stage": "execute",
    "method": "POST",
    "route": "/api/v1/auth/login",
    "statuses": [
      401
    ],
    "requires": [
      "12.7"
    ]
  },
  "CP-AUT-12 / 09 - Rechazar reutilización del OTP consumido": {
    "id": "CP-AUT-12",
    "key": "12.9",
    "name": "CP-AUT-12 / 09 - Rechazar reutilización del OTP consumido",
    "action": "reuseOtp",
    "persona": "recovery",
    "stage": "execute",
    "method": "POST",
    "route": "/api/v1/auth/reset-password",
    "statuses": [
      400,
      403,
      422
    ],
    "requires": [
      "12.8"
    ]
  },
  "CP-AUT-12 / 10 - Comprobar que el intento de reutilización no cambió la contraseña": {
    "id": "CP-AUT-12",
    "key": "12.10",
    "name": "CP-AUT-12 / 10 - Comprobar que el intento de reutilización no cambió la contraseña",
    "action": "login",
    "persona": "recovery",
    "stage": "execute",
    "method": "POST",
    "route": "/api/v1/auth/login",
    "statuses": [
      200
    ],
    "requires": [
      "12.9"
    ]
  },
  "CP-AUT-12 / 11 - Confirmar persistencia final de la identidad": {
    "id": "CP-AUT-12",
    "key": "12.11",
    "name": "CP-AUT-12 / 11 - Confirmar persistencia final de la identidad",
    "action": "profile",
    "persona": "recovery",
    "stage": "execute",
    "method": "GET",
    "route": "/api/v1/users/me",
    "statuses": [
      200
    ],
    "requires": [
      "12.10"
    ]
  },
  "CP-AUT-13 / 01 - Solicitar un nuevo OTP de recuperación": {
    "id": "CP-AUT-13",
    "key": "13.1",
    "name": "CP-AUT-13 / 01 - Solicitar un nuevo OTP de recuperación",
    "action": "recover",
    "persona": "recovery",
    "stage": "execute",
    "method": "POST",
    "route": "/api/v1/auth/recover",
    "statuses": [
      200
    ],
    "requires": [
      "12.11"
    ]
  },
  "CP-AUT-13 / 02 - Rechazar código incorrecto o vencido sin cambiar contraseña": {
    "id": "CP-AUT-13",
    "key": "13.2",
    "name": "CP-AUT-13 / 02 - Rechazar código incorrecto o vencido sin cambiar contraseña",
    "action": "invalidRecovery",
    "persona": "recovery",
    "stage": "execute",
    "method": "POST",
    "route": "/api/v1/auth/reset-password",
    "statuses": [
      400,
      403,
      422
    ],
    "requires": [
      "13.1"
    ],
    "otp": "recoveryOtp"
  },
  "CP-AUT-13 / 03 - Acceder con la contraseña vigente anterior al intento": {
    "id": "CP-AUT-13",
    "key": "13.3",
    "name": "CP-AUT-13 / 03 - Acceder con la contraseña vigente anterior al intento",
    "action": "login",
    "persona": "recovery",
    "stage": "execute",
    "method": "POST",
    "route": "/api/v1/auth/login",
    "statuses": [
      200
    ],
    "requires": [
      "13.2"
    ]
  },
  "CP-AUT-13 / 04 - Comprobar identidad conservada": {
    "id": "CP-AUT-13",
    "key": "13.4",
    "name": "CP-AUT-13 / 04 - Comprobar identidad conservada",
    "action": "profile",
    "persona": "recovery",
    "stage": "execute",
    "method": "GET",
    "route": "/api/v1/users/me",
    "statuses": [
      200
    ],
    "requires": [
      "13.3"
    ]
  },
  "CP-AUT-13 / 05 - Rechazar la contraseña alternativa no autorizada": {
    "id": "CP-AUT-13",
    "key": "13.5",
    "name": "CP-AUT-13 / 05 - Rechazar la contraseña alternativa no autorizada",
    "action": "alternateLogin",
    "persona": "recovery",
    "stage": "execute",
    "method": "POST",
    "route": "/api/v1/auth/login",
    "statuses": [
      401
    ],
    "requires": [
      "13.4"
    ]
  }
};
// CP-AUT integration runner. All requests are explicit collection items, never hidden HTTP calls.
const authEnv = pm.environment;
const authMeta = authManifest[pm.info.requestName];
const ag = key => String(authEnv.get(key) || '');
const aset = (key, value) => { authEnv.set(key, String(value)); pm.variables.set(key, String(value)); };
let authState;
try { authState = JSON.parse(ag('_authState') || 'null'); } catch (_) { authState = null; }
let authRun = ag('runId');
if (!authRun) { authRun = pm.variables.replaceIn('{{$guid}}').replace(/-/g, '').toLowerCase(); aset('runId', authRun); }
const authIds = Array.from({length:13}, (_, i) => 'CP-AUT-' + String(i + 1).padStart(2,'0'));
if (!authState || authState.runId !== authRun) {
  authState = {runId:authRun,origin:'',cases:{},steps:{},ids:{},claims:{},verified:{},issued:{},passwordChanged:false,candidate:null};
  authIds.forEach(id => { authState.cases[id] = {status:['CP-AUT-05','CP-AUT-10'].includes(id)?'NO IMPLEMENTADO':'NO EJECUTADO',reason:''}; });
  ['authBuyerJwt','authProducerJwt','authAttemptsJwt','authRecoveryJwt','authExpiredCandidateJwt','expiredJwt','verificationOtp','recoveryOtp','authAttemptsOtp','authReplacementOtp','authOtpConfirmedRunId'].forEach(key => aset(key,''));
}
function authSave() {
  aset('_authState', JSON.stringify(authState));
  aset('authResults', JSON.stringify({runId:authRun,cases:authState.cases}));
  aset('authEvidence', JSON.stringify(Object.values(authState.steps).filter(step => step.done).map(step => ({caseId:step.caseId,step:step.key,at:step.at,method:step.method,path:step.path,httpStatus:step.httpStatus,checks:step.checks,status:step.status,reason:step.reason || ''}))));
}
function authSkip(reason, blocked) {
  if (blocked && authMeta && authState.cases[authMeta.id].status !== 'FALLIDO') authState.cases[authMeta.id] = {status:'BLOQUEADO',reason};
  authSave();
  pm.test.skip((authMeta ? authMeta.id : 'CP-AUT') + ' — ' + (blocked?'BLOQUEADO: ':'OMITIDO: ') + reason, function(){});
  pm.execution.skipRequest();
  throw new Error('CP-AUT request skipped.');
}
const authNeed = key => { if (!ag(key).trim()) authSkip('Configurar ' + key,true); return ag(key); };
if (!authMeta) authSkip('Solicitud no reconocida',true);
const authSelected = ag('authSelectedCases').split(',').map(s=>s.trim()).filter(Boolean);
if (authSelected.some(id=>!authIds.includes(id))) authSkip('authSelectedCases contiene identificadores desconocidos',true);
if (authSelected.length && !authSelected.includes(authMeta.id)) authSkip('Caso fuera de la selección',false);
if (!['prepare','execute'].includes(ag('authStage'))) authSkip('authStage debe ser prepare o execute',true);
if (authMeta.stage !== ag('authStage')) authSkip('Paso de otra etapa',false);
if (!/^[a-f0-9]{32}$/.test(authRun)) authSkip('runId debe ser hexadecimal de 32 caracteres',true);
const authBase = authNeed('baseUrl');
if (!/^https?:\/\/(?:[A-Za-z0-9][A-Za-z0-9.-]*|\[[a-fA-F0-9:]+\])(?::[0-9]{1,5})?$/.test(authBase)) authSkip('baseUrl debe ser un origen sin path ni credenciales',true);
if (ag('developmentEnvironmentConfirmed') !== 'true' || ag('authorizedDevelopmentUrl') !== authBase) authSkip('Falta autorización explícita del origen de desarrollo',true);
if (ag('allowMutatingTests') !== 'true') authSkip('Habilitar allowMutatingTests únicamente con autorización',true);
if (authState.origin && authState.origin !== authBase) authSkip('Una ejecución no puede cambiar de origen',true);
authState.origin = authBase;
if (pm.request.method !== authMeta.method) authSkip('Método HTTP modificado',true);
pm.variables.set('baseUrl', authBase);
if (pm.variables.replaceIn(pm.request.url.toString()) !== authBase + authMeta.route) authSkip('Ruta HTTP modificada',true);
if (authState.steps[authMeta.key]) {
  if (authState.steps[authMeta.key].done) authSkip('Paso ya ejecutado; se conserva su evidencia, no se reenvía',false);
  authSkip('Solicitud previa sin respuesta registrada; resultado indeterminado. No reintentar registros ni OTP a ciegas',true);
}
for (const dependency of authMeta.requires) {
  if (!authState.steps[dependency] || !authState.steps[dependency].flowReady) authSkip('Precondición API pendiente: paso ' + dependency,true);
}
if (ag('testInboxConfirmed') !== 'true' || ag('emailRoutingConfirmed') !== 'true') authSkip('Confirmar buzón controlado y enrutamiento antes de ejecutar',true);
const authEmailRegex = /^[A-Za-z0-9_+.-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}$/;
if (!authEmailRegex.test(authNeed('testInboxAddress'))) authSkip('Buzón de pruebas inválido',true);
if (ag('buyerRoleId') !== '1' || ag('producerRoleId') !== '2') authSkip('Los roles de prueba deben ser Comprador 1 y Productor 2',true);
const authSuffix = {buyer:'',producer:'',minor:'-minor',attempts:'-otp',recovery:'-rec',unknown:'-none'};
function authEmail(persona) {
  const key = persona === 'producer' ? 'producerEmailTemplate' : 'buyerEmailTemplate';
  const template = authNeed(key);
  if (!template.includes('{{runId}}')) authSkip('Las plantillas deben incluir {{runId}}',true);
  const email = template.split('{{runId}}').join(authRun + authSuffix[persona]).toLowerCase();
  if (!authEmailRegex.test(email) || email.length > 80) authSkip('Correo de prueba inválido o superior a 80 caracteres',true);
  return email;
}
const authEmails = Object.fromEntries(Object.keys(authSuffix).map(persona=>[persona,authEmail(persona)]));
if (new Set(Object.values(authEmails)).size !== Object.keys(authEmails).length) authSkip('Las direcciones ficticias deben ser distintas',true);
// Bind the resumed run to the original routing and fictitious credentials.
// The digest is internal and never included in sanitized evidence.
const authBinding=require('crypto-js').SHA256(JSON.stringify([authRun,ag('testInboxAddress'),ag('buyerEmailTemplate'),ag('producerEmailTemplate'),ag('buyerPassword'),ag('producerPassword'),ag('newPassword')])).toString();
if(authState.binding && authState.binding!==authBinding) authSkip('No cambiar buzón, plantillas ni contraseñas durante una ejecución; preparar un runId nuevo',true);
authState.binding=authBinding;
aset('buyerEmail',authEmails.buyer); aset('producerEmail',authEmails.producer);
const authPersona = authMeta.persona;
const authRole = authPersona === 'producer' ? 2 : 1;
const authOriginalPassword = authNeed(authPersona === 'producer' ? 'producerPassword' : 'buyerPassword');
if (authOriginalPassword.length < 8 || authOriginalPassword.length > 64) authSkip('Configurar contraseña ficticia de 8 a 64 caracteres',true);
const authCurrentPassword = authPersona === 'recovery' && authState.passwordChanged ? authNeed('newPassword') : authOriginalPassword;
const authAlternative = 'TEST_Alternative_' + authRun;
if ([authOriginalPassword,authCurrentPassword].includes(authAlternative)) authSkip('La contraseña alternativa de prueba debe diferir de las actuales',true);
const authTokenKey = {buyer:'authBuyerJwt',producer:'authProducerJwt',attempts:'authAttemptsJwt',recovery:'authRecoveryJwt'}[authPersona];
function authClaims(token) {
  try { if (token.split('.').length !== 3) return null; return JSON.parse(require('buffer').Buffer.from(token.split('.')[1].replace(/-/g,'+').replace(/_/g,'/'),'base64').toString('utf8')); } catch (_) { return null; }
}
function authOtp(key) {
  const otp = authNeed(key);
  if (!/^[0-9]{6}$/.test(otp) || ag('authOtpConfirmedRunId') !== authRun) authSkip('Proporcionar OTP real del buzón y confirmar su runId',true);
  return otp;
}
function authFresh(persona) {
  if (!authState.issued[persona] || Date.now() - authState.issued[persona] > 14 * 60 * 1000) authSkip('OTP no acreditado como vigente; renovar mediante un flujo autorizado o iniciar nueva ejecución',true);
}
let authBody;
const action = authMeta.action;
if (['register','duplicate','minor'].includes(action)) {
  const year = new Date().getUTCFullYear();
  authBody = {email:authEmails[authPersona],password:authOriginalPassword,firstName:'TEST_'+authRun+'_'+(action==='duplicate'?'duplicate':authPersona),paternalLastName:'TEST_Apellido',maternalLastName:null,birthDate:action==='minor'?(year-10)+'-01-15T00:00:00Z':'1990-01-02T00:00:00Z',phoneNumber:null,roleId:authRole};
}
if (['login','badLogin','unknownLogin','oldLogin','alternateLogin'].includes(action)) {
  let password = authCurrentPassword;
  if (action==='badLogin' && !authMeta.useCorrectPassword) password=authAlternative;
  if (action==='oldLogin') password=authOriginalPassword;
  if (action==='alternateLogin') password=authAlternative;
  authBody={email:authEmails[action==='unknownLogin'?'unknown':authPersona],password};
  if (action==='login') aset(authTokenKey,'');
}
if (['verify','wrongOtp','originalOtp','resend','recover','reset','reuseOtp','invalidRecovery'].includes(action)) {
  if (!authState.ids[authPersona]) authSkip('Falta identidad real comprobada por GET de la cuenta de prueba',true);
  authBody={email:authEmails[authPersona]};
  if (['verify','wrongOtp','originalOtp','resend'].includes(action) && authState.verified[authPersona] !== false) authSkip('La cuenta debe estar pendiente; no aceptar verify idempotente como prueba',true);
  if (['verify','wrongOtp','originalOtp'].includes(action)) {
    authFresh(authPersona);
    const original = authOtp(authMeta.otp || 'authAttemptsOtp');
    authBody.verificationCode=action==='wrongOtp'?String((Number(original)+1)%1000000).padStart(6,'0'):original;
  }
  if (action==='recover') { aset('recoveryOtp',''); }
  if (action==='resend') { aset('authReplacementOtp',''); }
  if (['reset','reuseOtp','invalidRecovery'].includes(action)) {
    const original=authOtp('recoveryOtp');
    const next=authNeed('newPassword');
    if (next.length<8 || next.length>64 || next===authOriginalPassword || next===authAlternative) authSkip('newPassword debe ser distinta y tener 8 a 64 caracteres',true);
    let code=original;
    if (action==='invalidRecovery') {
      const mode=ag('authRecoveryCodeMode');
      if (!['invalid','expired'].includes(mode)) authSkip('authRecoveryCodeMode debe ser invalid o expired',true);
      const issued=authState.issued.recovery;
      if (!issued) authSkip('Falta solicitud de recuperación real',true);
      if (mode==='expired') {
        if (Date.now()-issued < 16*60*1000) authSkip('Esperar vencimiento natural del OTP; no se altera el reloj ni SQL',true);
      } else {
        authFresh('recovery'); code=String((Number(original)+1)%1000000).padStart(6,'0');
      }
    } else if (action==='reset') authFresh('recovery');
    authBody.code=code;
    authBody.newPassword=action==='reset'?next:authAlternative;
  }
}
if (authMeta.method==='GET') {
  const token=['attest','expired'].includes(action)?authNeed('authExpiredCandidateJwt'):authNeed(authTokenKey);
  const claims=authClaims(token);
  if (!claims || !Number.isSafeInteger(Number(claims.sub)) || Number(claims.sub)<=0 || Number(claims.roleId)!==authRole || !Number.isFinite(claims.exp)) authSkip('JWT sin identidad esperada',true);
  if (!authState.claims[authPersona] || String(claims.sub)!==String(authState.claims[authPersona].sub)) authSkip('JWT no corresponde al login observado en esta ejecución',true);
  if (action==='expired') {
    const proof=authState.candidate;
    const digest=require('crypto-js').SHA256(token).toString();
    const skew=Number(ag('authClockSkewSeconds'));
    if (!proof || !proof.acceptedAt || proof.digest!==digest || proof.origin!==authBase || proof.runId!==authRun) authSkip('Falta evidencia de aceptación del mismo JWT legítimo antes de vencer',true);
    if (!Number.isFinite(skew) || skew<0 || skew>3600) authSkip('Configurar tolerancia temporal válida',true);
    if (Date.now() <= (claims.exp+skew)*1000) authSkip('Esperar expiración natural del JWT y tolerancia del servidor',true);
    aset('expiredJwt',token);
  } else if (claims.exp*1000<=Date.now()) authSkip('La sesión previa venció; continuar exige un nuevo flujo autorizado, no falsificar un token',true);
  pm.variables.set('authRequestJwt',token);
}
if (action==='login' && authMeta.id==='CP-AUT-08' && authState.verified.buyer!==true) authSkip('CP-AUT-08 exige cuenta verificada mediante CP-AUT-06',true);
pm.variables.set('authRequestBody',authBody?JSON.stringify(authBody):'');
pm.variables.set('authStepMeta',JSON.stringify(authMeta));
pm.variables.set('authExpectedEmail',authEmails[authPersona]);
pm.variables.set('authExpectedName','TEST_'+authRun+'_'+authPersona);
pm.variables.set('authCaseSteps',JSON.stringify(Object.values(authManifest).filter(step=>step.id===authMeta.id).map(step=>step.key)));
authState.steps[authMeta.key]={key:authMeta.key,caseId:authMeta.id,method:authMeta.method,path:authMeta.route,at:new Date().toISOString(),done:false,flowReady:false};
authSave();

};
