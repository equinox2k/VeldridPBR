using System;
using System.Numerics;

namespace VeldridNSViewExample
{
    public class Camera
    {

        public float Fov { get; set; } = 45.0f;
        public float Near { get; set; } = 1.0f;
        public float Far { get; set; } = 200.0f;
        public uint Width { get; set; } = 1;
        public uint Height { get; set; } = 1;
        public Vector3 Eye { get; set; } = new Vector3(0, 0, 100);

        public Matrix4x4 ModelMatrix { get; set; } = Matrix4x4.Identity;

        public Matrix4x4 ViewMatrix
        {
            get
            {
                return Matrix4x4.CreateLookAt(Eye, new Vector3(0, 0, 0), new Vector3(0, 1, 0));
            }
        }

        public Matrix4x4 ProjectionMatrix => Matrix4x4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(Fov), Width / (float)Height, Near, Far);

        public Matrix4x4 ModelViewMatrix
        {
            get
            {
                return Matrix4x4.Multiply(ViewMatrix, ModelMatrix);
            }
        }

        public Matrix4x4 InverseProjectionMatrix
        {
            get
            {
                Matrix4x4.Invert(ProjectionMatrix, out var inverseProjectionMatrix);
                return inverseProjectionMatrix;
            }
        }

        public Matrix4x4 NormalMatrix
        {
            get
            {
                Matrix4x4.Invert(ModelViewMatrix, out var inverseModelViewMatrix);
                var result = Matrix4x4.Transpose(inverseModelViewMatrix);
                result.M14 = 0;
                result.M24 = 0;
                result.M34 = 0;
                result.M41 = 0;
                result.M42 = 0;
                result.M43 = 0;
                result.M44 = 0;
                return result;
            }
        }

        public Matrix4x4 InverseModelViewMatrix
        {
            get
            {
                Matrix4x4.Invert(ModelViewMatrix, out var inverseModelViewMatrix);
                inverseModelViewMatrix.M14 = 0;
                inverseModelViewMatrix.M24 = 0;
                inverseModelViewMatrix.M34 = 0;
                inverseModelViewMatrix.M41 = 0;
                inverseModelViewMatrix.M42 = 0;
                inverseModelViewMatrix.M43 = 0;
                inverseModelViewMatrix.M44 = 0;
                return inverseModelViewMatrix;
            }
        }

        public float CalcZoom(float scale)
        {
            var angle = Fov / 2.0f;
            var adjacent = scale / 2.0f / (float)Math.Tan(angle * (Math.PI / 180.0f));
            return adjacent + Near + scale / 2.0f;
        }

    }
}
