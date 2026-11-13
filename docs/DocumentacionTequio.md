# DocumentacionTequio.md

## 1. Actores del Sistema
*   **Comprador:** Usuario final que revisa el catálogo, aparta paquetes y simula el pago.
*   **Productor:** Crea campañas de lotes, gestiona su catálogo, valida entregas y revisa métricas.
*   **Administrador:** Gestiona puntos de entrega y monitorea transacciones globales.
*   **Administrador maestro:** Registra formalmente a otros administradores.

## 2. Casos de Uso (Catálogo Completo)
*   **CU-01 Registrar usuario:** Comprador/productor crea su cuenta, validando correo y enviando código de verificación.
*   **CU-02 Registrar administrador:** El administrador maestro registra a un nuevo administrador y autoriza con código.
*   **CU-03 Completar verificación de cuenta:** Ingreso de código OTP para activar cuenta nueva.
*   **CU-04 Autenticar usuario:** Inicio de sesión que genera un JWT. El administrador maestro requiere 2FA (código OTP).
*   **CU-05 Recuperar contraseña:** Flujo con código OTP por correo para restablecer credenciales.
*   **CU-06 Gestionar perfil de usuario:** Actualización de datos de contacto y biografía.
*   **CU-07 Cargar archivos multimedia:** Subida de imágenes/videos validando tamaño y extensión.
*   **CU-08 Gestionar catálogo de productos:** El Productor da de alta o baja productos base (nombre, descripción, imagen, categoría).
*   **CU-09 Gestionar puntos de entrega autorizados:** El Administrador registra lugares físicos con geolocalización.
*   **CU-10 Publicar lote de compra:** El Productor asocia un producto base a una campaña, define paquetes cerrados, meta mínima, fecha límite y punto de recolección.
*   **CU-11 Explorar catálogo activo:** El Comprador filtra y busca lotes vigentes.
*   **CU-12 Comprar / Apartar lotes:** El Comprador selecciona paquetes de un lote activo. El sistema aplica control de concurrencia y procesa un pago simulado obteniendo un token.
*   **CU-13 Validar entrega física de lotes:** El Productor ingresa el código del comprador en el punto físico para marcar los paquetes como entregados.
*   **CU-14 Consultar dashboard:** Productores ven métricas de ventas y exportan manifiestos; Administradores ven transacciones globales.

## 3. Reglas de Negocio
*   **RN-01 Estructura de Venta:** La venta a granel está prohibida; los productos se comercializan en paquetes cerrados.
*   **RN-02 Asignación de Entrega:** Solo el productor elige el punto de recolección físico al crear el lote.
*   **RN-03 Control de Concurrencia:** Obligatorio en BD durante el apartado para evitar sobrepasar inventario.
*   **RN-04 Cierre Automático:** El sistema confirma o cancela lotes automáticamente en su fecha límite.
*   **RN-05 Comprobantes:** El recibo PDF se genera solo si el lote está confirmado y el pago simulado fue exitoso.
*   **RN-06 Puntos de Entrega:** Solo el administrador los gestiona; requieren ubicación geográfica válida.
*   **RN-07 Baja de Puntos:** No se pueden eliminar puntos asociados a lotes activos o confirmados.
*   **RN-08 Mayoría de Edad:** Solo mayores de 18 años pueden registrarse.
*   **RN-09 2FA Administrador Maestro:** Requiere validación de OTP en cada inicio de sesión.
*   **RN-10 Baja de Productos:** No se pueden dar de baja productos involucrados en lotes activos o en entregas pendientes.
*   **RN-11 Expiración de Códigos:** Códigos OTP expiran en 15 minutos y tienen máximo 3 intentos fallidos.
*   **RN-12 Restricciones Multimedia:** Imágenes hasta 5MB, videos hasta 20MB, solo formatos comunes.

## 4. Restricciones Técnicas
*   **RS01 Simulación Financiera:** Prohibido guardar datos de tarjetas; procesar en entorno de pruebas (Stripe Sandbox).
*   **RS02 Logística Inmutable:** No hay cálculo de rutas ni envíos a domicilio; entregas solo en puntos fijos.
*   **RS03 Backend Exclusivo en BD:** Toda lógica de datos y validaciones de concurrencia viven en Procedimientos Almacenados en SQL Server.
*   **RS04 Notificaciones Sin Estado:** Prohibido WebSockets; usar correos asíncronos para notificar estados.

## 5. Modelo de Datos (Esquema SQL)
*   **Rol:** `Id`, `Nombre`.
*   **Usuario:** `Id`, `RolId`, `Correo`, `PasswordHash`, `Nombre`, `ApellidoPaterno`, `ApellidoMaterno`, `FechaNacimiento`, `Telefono`, `FotoPerfilUrl`, `Localidad`, `Biografia`, `CuentaVerificada`, `FechaRegistro`.
*   **CodigoVerificacion:** `Id`, `UsuarioId`, `Codigo`, `Tipo`, `FechaCreacion`, `FechaExpiracion`, `IntentosRestantes`.
*   **CategoriaProducto:** `Id`, `CategoriaPadreId`, `Nombre`.
*   **ProductoBase:** `Id`, `ProductorId`, `CategoriaId`, `Nombre`, `DescripcionBreve`, `ImagenUrl`, `UnidadMedicion`, `EstadoActivo`.
*   **PuntoEntrega:** `Id`, `Nombre`, `Calle`, `Numero`, `Ciudad`, `CodigoPostal`, `Colonia`, `Referencias`, `HorarioAtencion`, `Latitud`, `Longitud`, `EstadoActivo`.
*   **Lote:** `Id`, `ProductoBaseId`, `ProductorId`, `PuntoEntregaId`, `NombreLote`, `Descripcion`, `MetaMinima`, `FechaLimite`, `Estado`, `FechaPublicacion`.
*   **Paquete:** `Id`, `LoteId`, `NombrePaquete`, `Contenido`, `Precio`, `CantidadDisponible`.
*   **Apartado:** `Id`, `CompradorId`, `LoteId`, `FechaTransaccion`, `TotalPagado`, `EstadoPago`, `ReferenciaPago`, `CodigoEntrega`, `EstadoEntrega`, `FechaValidacionEntrega`.
*   **DetalleApartado:** `Id`, `ApartadoId`, `PaqueteId`, `Cantidad`, `PrecioUnitario`.

## 6. Contratos de API REST
*   **Autenticación:**
    *   `POST /api/v1/auth/register` (Payload: correo, password, nombre, apellidos, telefono, rolId) -> 201 Created.
    *   `POST /api/v1/auth/verify` (Payload: correo, codigo) -> 200 OK.
    *   `POST /api/v1/auth/login` (Payload: correo, password) -> 200 OK (Retorna JWT).
    *   `POST /api/v1/auth/recover` / `PUT /api/v1/auth/reset-password` / `POST /api/v1/auth/admin/register`.
*   **Usuarios y Archivos:**
    *   `GET /api/v1/users/me` / `PUT /api/v1/users/me` (Requieren JWT).
    *   `POST /api/v1/files/upload` (multipart/form-data) -> Retorna URL.
*   **Catálogo (Productos Base):**
    *   `GET /api/v1/products` / `POST /api/v1/products` / `PATCH /api/v1/products/{id}/deactive`.
*   **Logística (Puntos de Entrega):**
    *   `GET /api/v1/delivery-points` / `POST /api/v1/delivery-points` / `PUT /api/v1/delivery-points/{id}` / `PATCH /api/v1/delivery-points/{id}/deactive`.
*   **Lotes y Campañas:**
    *   `POST /api/v1/batches` (Incluye sub-arreglo de `paquetes`).
    *   `GET /api/v1/batches` (Admite params `search` y `categoriaId`).
    *   `GET /api/v1/batches/{id}`.
*   **Transacciones y Entregas:**
    *   `POST /api/v1/orders` (Payload: loteId, paymentToken, arreglo de paquetesSeleccionados) -> 201 Created con `codigoEntrega`.
    *   `GET /api/v1/orders/pending` / `POST /api/v1/orders/validate-delivery` (recibe codigo) / `PATCH /api/v1/orders/{id}/confirm-delivery` / `GET /api/v1/orders/{id}/receipt` (PDF).
*   **Dashboards:**
    *   `GET /api/v1/metrics/producer` / `GET /api/v1/metrics/admin` / `GET /api/v1/batches/{id}/manifest` (Retorna Excel).