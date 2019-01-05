using System;
namespace PNI.Render3d.Core.Helpers
{
    public static class MathHelper
    {
        public static float DegreesToRadians(float angle)
        {
            return (float)(Math.PI / 180) * angle;
        }
    }
}
