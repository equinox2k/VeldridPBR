using System;
using System.Numerics;
using PNI.Rendering.Harmony.Helpers;

namespace PNI.Rendering.Harmony
{
    public class Camera
    {
        public float ModelScale { get; set; } = 1.0f;
        public float Fov { get; set; } = 45.0f;
        public float Near { get; set; } = 1.0f;
        public float Far { get; set; } = 200.0f;
        public uint Width { get; set; } = 1;
        public uint Height { get; set; } = 1;
        public float RotationX { get; set; } = 0;
        public float RotationY { get; set; } = 0;
        public Vector3 Eye { get; set; } = new Vector3(0, 0, 100);

        public Matrix4x4 ModelMatrix
        {
            get
            {
               return Matrix4x4.CreateFromAxisAngle(Vector3.UnitY, MathHelper.DegreesToRadians(RotationY)) * 
                      Matrix4x4.CreateFromAxisAngle(Vector3.UnitX, MathHelper.DegreesToRadians(RotationX));
            }
        }

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

        public float CalcZoom()
        {
            var angle = Fov / 2.0f;
            var adjacent = ModelScale / 2.0f / (float)Math.Tan(angle * (Math.PI / 180.0f));
            return adjacent + Near + ModelScale / 2.0f;
        }

    }
}
