using System.Security.Cryptography;
using System.Text;

namespace ChoyERP.Application.Common.Security;

public static class TokenHasher
{
    /// <summary>
    /// Los tokens de renovación son opacos: en la base solo se guarda su hash SHA-256.
    /// </summary>
    public static string Sha256(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
}
