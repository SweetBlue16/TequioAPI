# TequioAPI

API del sistema web Tequio, encargada de la gestión de ventas crowd-buying por lotes de pequeños productores artesanales. Proyecto final para la experiencia educativa de Desarrollo de Sistemas Web.


Puntos físicos de entrega: [instalación, contrato y verificación](docs/PUNTOS_ENTREGA.md).

Flujo Admin y correo: [corrección, límites y verificación](docs/ADMIN_FLOW.md).

## Clave de firma JWT

Configure `TEQUIO_JWT_SIGNING_KEY` mediante el gestor de secretos del entorno.
En Development también se admite `Jwt:Key` desde .NET User Secrets para el proyecto
`backend/Tequio.Api`. Genere al menos 32 bytes con un generador criptográfico,
codifíquelos para el transporte como texto y conserve la misma clave en las
instancias que firman y validan tokens.

Si el arranque local usaba `appsettings.Development.json` para esta clave,
migra la configuración a User Secrets o a la variable de entorno. Rotar la
clave invalida los JWT anteriores y requiere volver a iniciar sesión.
