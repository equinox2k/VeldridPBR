using System;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using PNI.Render3d.Core.Helpers;

namespace PNI.Rendering.Harmony
{
    public static class Extensions
    {

        public static string GetHash(this Vector3 vector)
        {
            var hashCodes = new StringBuilder();
            hashCodes.Append(vector.X.GetHashCode().ToString("X"));
            hashCodes.Append("_");
            hashCodes.Append(vector.Y.GetHashCode().ToString("X"));
            hashCodes.Append("_");
            hashCodes.Append(vector.Z.GetHashCode().ToString("X"));
            return HashHelper.ComputeStringHash(hashCodes.ToString());
        }

        public static bool Equals(this Vector3 vector, Vector3 other)
        {
            return vector.GetHash().Equals(other.GetHash());
        }

        public static string GetHash(this Vector2 vector)
        {
            var hashCodes = new StringBuilder();
            hashCodes.Append(vector.X.GetHashCode().ToString("X"));
            hashCodes.Append("_");
            hashCodes.Append(vector.Y.GetHashCode().ToString("X"));
            return HashHelper.ComputeStringHash(hashCodes.ToString());
        }

        public static bool Equals(this Vector2 vector, Vector2 other)
        {
            return vector.GetHash().Equals(other.GetHash());
        }

        public static float Clamp(this float value, float min, float max)
        {
            return Math.Min(Math.Max(value, min), max);
        }

        public static int Clamp(this int value, int min, int max)
        {
            return Math.Min(Math.Max(value, min), max);
        }
      
    }
}
