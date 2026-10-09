namespace Tequio.Domain.Constants
{
    /// <summary>
    /// Centralized catalog for application error messages returned to the frontend.
    /// </summary>
    public static class ErrorMessages
    {
        public const string AgeRestriction = "Debes ser mayor de 18 años para registrarte.";
        public const string EmailAlreadyRegistered = "El correo electrónico ya se encuentra registrado.";
        public const string InvalidCredentials = "El correo o la contraseña son incorrectos.";
        public const string OtpExpired = "El código de verificación ha expirado. Solicita uno nuevo.";
        public const string OtpInvalid = "El código de verificación es incorrecto.";
        public const string DatabaseError = "Ocurrió un error al procesar la solicitud en la base de datos.";
        public const string FileTooLarge = "La imagen supera el límite máximo permitido de 5MB.";
        public const string InvalidUrl = "El formato de la URL no es válido.";
        public const string RequiredFieldMissing = "Este campo es obligatorio.";
        public const string RequiredImageUrl = "La URL de la imagen es obligatoria.";
        public const string RecordNotFound = "No se encontró el registro solicitado.";

        public const string NegativePageIndex = "El índice de página no puede ser negativo.";
        public const string InvalidPageSize = "El tamaño de página debe ser mayor a cero.";
        public const string PageSizeExceeded = "El tamaño de página no puede exceder los 100 productos.";

        public const string ProducerNotFoundOrUnauthorized = "El usuario especificado no existe o no tiene permisos de productor.";
        public const string CategoryNotFound = "La categoría especificada no existe.";
        public const string DuplicateProductName = "Ya existe un producto activo con el mismo nombre para este productor.";

        public const string ProductNotFoundOrForbidden = "El producto no existe o no pertenece al productor especificado.";
        public const string ProductAlreadyDeactivated = "El producto ya se encuentra desactivado.";
        public const string ProductHasActiveBatches = "No se puede dar de baja el producto porque tiene lotes activos o confirmados.";
    }
}
