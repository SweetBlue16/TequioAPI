namespace Tequio.Api.Validators
{
    /// <summary>
    /// Safely attempts conversions and raises ArgumentExceptions if the conversion fails
    /// </summary>
    public static class StringConverter
    {
        public static int ToInt(string number, string argumentDescriptor)
        {
            if (int.TryParse(number, out int value))
            {
                return value;
            }

            throw new ArgumentException($"El valor de {argumentDescriptor} no es un número válido.");
        }
    }
}
