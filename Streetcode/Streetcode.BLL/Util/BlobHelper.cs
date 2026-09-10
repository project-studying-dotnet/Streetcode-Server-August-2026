using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace Streetcode.BLL.Util;

public static class BlobHelper
{
    public static string GetHashedFileName(string name)
    {
        string createdFileName = $"{DateTime.Now}{name}"
            .Replace(" ", "_")
            .Replace(".", "_")
            .Replace(":", "_");

        return ComputeHash(createdFileName);
    }

    public static string ComputeHash(string input)
    {
        using var hash = SHA256.Create();
        byte[] result = hash.ComputeHash(Encoding.UTF8.GetBytes(input));
        return Convert.ToBase64String(result).Replace('/', '_');
    }
}