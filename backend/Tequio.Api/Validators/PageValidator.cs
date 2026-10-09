namespace Tequio.Api.Validators
{
    public static class PageValidator
    {
        private static int _minPageSize = 10;
        private static int _maxPageSize = 100;
        private static int _maxIndex = ((int.MaxValue / _maxPageSize) - _maxPageSize);
        
        public static void ValidatePageArguments(int pageIndex, int pageSize)
        {
            var errors = new List<string>();
            
            if ((pageIndex - 1) < 0)
            {
                errors.Add("Page index starts at 1, therefore it must be greater than zero.");
            }

            if (pageIndex > _maxIndex)
            {
                errors.Add($"Page index cannot be greater than {_maxIndex}");
            }

            if (pageSize < _minPageSize)
            {
                errors.Add($"Page size cannot be less than {_minPageSize}");
            }

            if (pageSize > _maxPageSize)
            {
                errors.Add($"Page size cannot be greater than {_maxPageSize}");
            }

            if (errors.Count > 0)
            {
                throw new ArgumentException(string.Join(Environment.NewLine, errors));
            }
        }
    }
}