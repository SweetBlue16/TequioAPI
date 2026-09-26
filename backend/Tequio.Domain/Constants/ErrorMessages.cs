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
    }
}
