using System;
namespace PNI.Rendering.Harmony.Helpers
{
    public static class MathHelper
    {
        public static float DegreesToRadians(float angle)
        {
            return (float)(Math.PI / 180) * angle;
        }
    }
}
