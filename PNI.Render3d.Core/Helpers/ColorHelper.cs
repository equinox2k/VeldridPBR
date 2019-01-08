using System;
using System.Linq;

namespace PNI.Rendering.Harmony.Helpers
{
    public static class ColorHelper
    {
        public static float[] HexToFloatArray(string colorString)
        {
            colorString = colorString.StartsWith("#", StringComparison.CurrentCultureIgnoreCase) ? colorString.Substring(1) : colorString;
            return Enumerable.Range(0, colorString.Length).Where(x => x % 2 == 0).Select(x => Convert.ToByte(colorString.Substring(x, 2), 16) / 255.0f).ToArray();
        }

        public static string FloatArrayToHex(float[] floatArray)
        {
            return string.Concat(floatArray.Select(b => ((byte)(b * 255)).ToString("X2")).ToArray()).ToLower();
        }
    }
}
