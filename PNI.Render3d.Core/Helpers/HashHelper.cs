using System;
using System.Security.Cryptography;
using System.Text;

namespace PNI.Render3d.Core.Helpers
{
    public static class HashHelper
    {
        public static string ComputeStringHash(string value)
        {
            var hashBytes = new SHA256Managed().ComputeHash(Encoding.UTF8.GetBytes(value));
            var hash = new StringBuilder();
            foreach (var current in hashBytes)
            {
                hash.Append(current.ToString("x2"));
            }
            return hash.ToString();
        }
    }
}
