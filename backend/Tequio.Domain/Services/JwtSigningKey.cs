using Microsoft.IdentityModel.Tokens;

namespace Tequio.Domain.Services;

/// <summary>Holds the shared signing key for JWT issuance and validation.</summary>
public sealed class JwtSigningKey
{
    /// <summary>Creates a signing key from secret material supplied by the host.</summary>
    /// <param name="secret">At least 32 bytes of secret key material.</param>
    public JwtSigningKey(byte[] secret)
    {
        ArgumentNullException.ThrowIfNull(secret);
        if (secret.Length < 32)
        {
            throw new ArgumentException("The JWT signing key must contain at least 32 bytes.", nameof(secret));
        }

        Key = new SymmetricSecurityKey(secret);
    }

    /// <summary>Gets the key used by the JWT handler.</summary>
    public SymmetricSecurityKey Key { get; }

    /// <summary>Creates credentials for the configured JWT signing algorithm.</summary>
    public SigningCredentials CreateCredentials()
    {
        return new SigningCredentials(Key, SecurityAlgorithms.HmacSha256);
    }
}
