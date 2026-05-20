using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace QuiLaCarne.Data;

public sealed class DpapiStringConverter : ValueConverter<string, string>
{
    public DpapiStringConverter()
        : base(
            v => Encrypt(v),
            v => Decrypt(v))
    {
    }

    private static string Encrypt(string value)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        var bytes = Encoding.UTF8.GetBytes(value);
        var protectedBytes = ProtectedData.Protect(
            bytes,
            optionalEntropy: null,
            scope: DataProtectionScope.CurrentUser);

        return Convert.ToBase64String(protectedBytes);
    }

    private static string Decrypt(string value)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        var protectedBytes = Convert.FromBase64String(value);
        var bytes = ProtectedData.Unprotect(
            protectedBytes,
            optionalEntropy: null,
            scope: DataProtectionScope.CurrentUser);

        return Encoding.UTF8.GetString(bytes);
    }
}