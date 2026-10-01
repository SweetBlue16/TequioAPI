using Tequio.Domain.VerificationCodes;
using Tequio.Infrastructure.Security;
using Xunit;

namespace Tequio.Tests.VerificationCodes;

/// <summary>
/// Tests for <see cref="VerificationCodeGenerator"/>.
/// </summary>
public sealed class VerificationCodeGeneratorTest
{
    /// <summary>
    /// Verifies that production generation always returns six decimal digits in the expected range.
    /// </summary>
    [Fact]
    public void TestGenerateCodeWithProductionGeneratorShouldReturnSixDigitsInRange()
    {
        IVerificationCodeGenerator generator = new VerificationCodeGenerator();

        for (int index = 0; index < 100; index++)
        {
            string code = generator.GenerateCode();

            Assert.Equal(6, code.Length);
            Assert.All(code, character => Assert.True(char.IsAsciiDigit(character)));
            Assert.InRange(int.Parse(code), 0, 999_999);
        }
    }

    /// <summary>
    /// Verifies deterministic zero padding without relying on random outcomes.
    /// </summary>
    [Fact]
    public void TestGenerateCodeWithLeadingZeroValueShouldPreserveSixDigits()
    {
        int capturedMinimum = -1;
        int capturedMaximum = -1;
        var generator = new VerificationCodeGenerator(
            (minimum, maximum) =>
            {
                capturedMinimum = minimum;
                capturedMaximum = maximum;
                return 4_281;
            });

        string code = generator.GenerateCode();

        Assert.Equal("004281", code);
        Assert.Equal(0, capturedMinimum);
        Assert.Equal(1_000_000, capturedMaximum);
    }

    /// <summary>
    /// Verifies both boundaries of the six-digit formatting range.
    /// </summary>
    [Theory]
    [InlineData(0, "000000")]
    [InlineData(999_999, "999999")]
    public void TestGenerateCodeWithBoundaryValueShouldReturnExpectedCode(int value, string expectedCode)
    {
        var generator = new VerificationCodeGenerator((_, _) => value);

        string code = generator.GenerateCode();

        Assert.Equal(expectedCode, code);
    }
}
