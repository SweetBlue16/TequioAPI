using System.Globalization;
using System.Security.Cryptography;
using Tequio.Domain.VerificationCodes;

namespace Tequio.Infrastructure.Security;

/// <summary>
/// Generates fixed-length verification codes using a cryptographically secure random-number generator.
/// </summary>
public sealed class VerificationCodeGenerator : IVerificationCodeGenerator
{
    private const int MinimumValue = 0;
    private const int ExclusiveMaximumValue = 1_000_000;
    private readonly Func<int, int, int> _getRandomNumber;

    /// <summary>
    /// Initializes a new instance of the <see cref="VerificationCodeGenerator"/> class.
    /// </summary>
    public VerificationCodeGenerator()
        : this((minimumValue, maximumValue) => RandomNumberGenerator.GetInt32(minimumValue, maximumValue))
    {
    }

    internal VerificationCodeGenerator(Func<int, int, int> getRandomNumber)
    {
        _getRandomNumber = getRandomNumber ?? throw new ArgumentNullException(nameof(getRandomNumber));
    }

    /// <inheritdoc />
    public string GenerateCode()
    {
        int value = _getRandomNumber(MinimumValue, ExclusiveMaximumValue);
        return value.ToString("D6", CultureInfo.InvariantCulture);
    }
}
