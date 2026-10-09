namespace Tequio.Api.Validators
{
    public static class NumberValidator
    {
        public static void ValidateGreaterOrEqualToZero(int number, string argumentDescriptor)
        {
            if (number < 0)
            {
                throw new ArgumentException($"{argumentDescriptor} has to be equal or greater than zero");
            }
        }
        
        public static void ValidateGreaterOrEqualToOne(int number, string argumentDescriptor)
        {
            if (number < 1)
            {
                throw new ArgumentException($"{argumentDescriptor} has to be equal or greater than zero");
            }
        }
    }
}