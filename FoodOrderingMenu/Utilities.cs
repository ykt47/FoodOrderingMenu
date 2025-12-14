using System;
using System.Security.Cryptography;
using System.Text;

public static class Utilities
{
    public static string HashPassword(string password)
    {
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(password ?? ""));
        return Convert.ToHexString(bytes);
    }
}
