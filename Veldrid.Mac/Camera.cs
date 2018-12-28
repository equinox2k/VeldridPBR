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

        private float[] ToMat3Array(Matrix4x4 matrix)
        {
            return new[] {
                matrix.M11, matrix.M12, matrix.M13,
                matrix.M21, matrix.M22, matrix.M23,
                matrix.M31, matrix.M32, matrix.M33
            };
        }

        public float[] NormalMatrix
        {
            get
            {
                Matrix4x4.Invert(ModelViewMatrix, out var inverseModelViewMatrix);
                return ToMat3Array(Matrix4x4.Transpose(inverseModelViewMatrix));
            }
        }

        public float[] InverseModelViewMatrix
        {
            get
            {
                Matrix4x4.Invert(ModelViewMatrix, out var inverseModelViewMatrix);
                return ToMat3Array(inverseModelViewMatrix);
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
