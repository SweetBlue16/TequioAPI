namespace Tequio.Api.Validators
{
    /// <summary>
    /// Safely attempts conversions and raises ArgumentExceptions if the conversion fails
    /// </summary>
    public static class StringConverter
    {
        public static int ToInt(string number, string argumentDescriptor)
        {
            try
            {
                return int.Parse(number);
            }
            catch (Exception ex) when (ex is ArgumentNullException || ex is FormatException || ex is OverflowException)
            {
                throw new ArgumentException($"The {argumentDescriptor} is not a valid number");
            }
        }
    }
}