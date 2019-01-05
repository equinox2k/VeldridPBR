using System;
using System.IO;
using System.Numerics;
using System.Text;
using PNI.Render3d.Core;

namespace PNI.Rendering.Harmony
{
    public static class ModelWriter
    {
        private static void WriteBytes(Stream stream, byte[] value)
        {
            stream.Write(value, 0, value.Length);
        }

        private static void WriteUint(Stream stream, uint value)
        {
            var buffer = BitConverter.GetBytes(value);
            WriteBytes(stream, buffer);
        }

        private static void WriteFloat(Stream stream, float value)
        {
            var buffer = BitConverter.GetBytes(value);
            WriteBytes(stream, buffer);
        }

        private static void WriteVector2(Stream stream, Vector2 value)
        {
            WriteFloat(stream, value.X);
            WriteFloat(stream, value.Y);
        }

        private static void WriteVector3(Stream stream, Vector3 value)
        {
            WriteFloat(stream, value.X);
            WriteFloat(stream, value.Y);
            WriteFloat(stream, value.Z);
        }

        private static void WriteVector4(Stream stream, Vector4 value)
        {
            WriteFloat(stream, value.X);
            WriteFloat(stream, value.Y);
            WriteFloat(stream, value.Z);
            WriteFloat(stream, value.W);
        }

        private static void WriteVertex(Stream stream, Vertex value)
        {
            WriteFloat(stream, value.PositionX);
            WriteFloat(stream, value.PositionY);
            WriteFloat(stream, value.PositionZ);
            WriteFloat(stream, value.TexCoordX);
            WriteFloat(stream, value.TexCoordY);
            WriteFloat(stream, value.NormalX);
            WriteFloat(stream, value.NormalY);
            WriteFloat(stream, value.NormalZ);
            WriteFloat(stream, value.TangentX);
            WriteFloat(stream, value.TangentY);
            WriteFloat(stream, value.TangentZ);
        }

        private static void WriteString(Stream stream, string value)
        {
            WriteUint(stream, (uint)value.Length);
            WriteBytes(stream, Encoding.UTF8.GetBytes(value));
        }

        public static void Encode(Stream stream, SurfaceGroups surfaceGroups)
        {
            var headerBuffer = new[] { (byte)'B', (byte)'M', (byte)'O', (byte)'D' };
            WriteBytes(stream, headerBuffer);
            WriteUint(stream, 1);
            WriteFloat(stream, surfaceGroups.Scale);
            WriteVector3(stream, surfaceGroups.MinDimension);
            WriteVector3(stream, surfaceGroups.MaxDimension);
            WriteUint(stream, (uint)surfaceGroups.Indices.Length);
            foreach (var index in surfaceGroups.Indices)
            {
                WriteUint(stream, index);
            }
            WriteUint(stream, (uint)surfaceGroups.Vertices.Length);
            foreach (var vertex in surfaceGroups.Vertices)
            {
                WriteVertex(stream, vertex);
            }
            WriteUint(stream, (uint)surfaceGroups.SurfaceGroupInfos.Length);
            foreach (var surfaceGroupInfo in surfaceGroups.SurfaceGroupInfos)
            {
                WriteUint(stream, surfaceGroupInfo.IndexOffset);
                WriteUint(stream, surfaceGroupInfo.IndexCount);
                WriteVector3(stream, surfaceGroupInfo.MinDimension);
                WriteVector3(stream, surfaceGroupInfo.MaxDimension);
                WriteVector4(stream, surfaceGroupInfo.UvDimension);
                WriteString(stream, surfaceGroupInfo.Properties.SurfaceName);
            }
        }
    }
}
