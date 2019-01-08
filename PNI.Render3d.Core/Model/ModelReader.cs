using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Numerics;
using System.Runtime.Serialization.Json;
using System.Text;
using PNI.Rendering.Harmony;

namespace PNI.Rendering.Harmony.Model
{
    public static class ModelReader
    {
        private static byte[] ReadBytes(Stream stream, uint length)
        {
            var buffer = new byte[length];
            stream.Read(buffer, 0, (int)length);
            return buffer;
        }

        private static uint ReadUint(Stream stream)
        {
            return BitConverter.ToUInt32(ReadBytes(stream, sizeof(uint)), 0);
        }

        private static float ReadFloat(Stream stream)
        {
            return BitConverter.ToSingle(ReadBytes(stream, sizeof(float)), 0);
        }

        private static Vector2 ReadVector2(Stream stream)
        {
            return new Vector2(ReadFloat(stream), ReadFloat(stream));
        }

        private static Vector3 ReadVector3(Stream stream)
        {
            return new Vector3(ReadFloat(stream), ReadFloat(stream), ReadFloat(stream));
        }

        private static Vector4 ReadVector4(Stream stream)
        {
            return new Vector4(ReadFloat(stream), ReadFloat(stream), ReadFloat(stream), ReadFloat(stream));
        }

        private static Vertex ReadVertex(Stream stream)
        {
            return new Vertex(ReadVector3(stream), ReadVector2(stream), ReadVector3(stream), ReadVector3(stream));
        }

        private static string ReadString(Stream stream)
        {
            var length = ReadUint(stream);
            var stringBuffer = ReadBytes(stream, length);
            return Encoding.UTF8.GetString(stringBuffer);
        }

        public static ModelResource GetModelResource(ModelResource[] modelResources, string resourceName)
        {
            foreach (var modelResource in modelResources)
            {
                if (!modelResource.Name.Equals(resourceName, StringComparison.CurrentCultureIgnoreCase))
                {
                    continue;
                }
                return modelResource;
            }
            return null;
        }

        public static bool Unpack(Stream stream, out SurfaceGroups surfaceGroups)
        {
            stream.Position = 0;

            surfaceGroups = null;

            ModelResource bmodelResource = null;
            ModelResource surfacesResource = null;

            var tempModelResources = new List<ModelResource>();

            using (var gzipStream = new GZipStream(stream, CompressionMode.Decompress))
            using (var memoryStream = new MemoryStream())
            {
                gzipStream.CopyTo(memoryStream);
                memoryStream.Position = 0;

                var headerBuffer = ReadBytes(memoryStream, 4);
                if (headerBuffer[0] != (byte)'H' || headerBuffer[1] != (byte)'P' || headerBuffer[2] != (byte)'K' || headerBuffer[3] != (byte)'G')
                {
                    return false;
                }

                while (memoryStream.Position < memoryStream.Length)
                {
                    var name = ReadString(memoryStream);
                    var data = ReadBytes(memoryStream, ReadUint(memoryStream));

                    if (name.Equals("object.bmodel", StringComparison.CurrentCultureIgnoreCase))
                    {
                        bmodelResource = new ModelResource(name, data);
                    }
                    else if (name.Equals("surfaces.json", StringComparison.CurrentCultureIgnoreCase))
                    {
                        surfacesResource = new ModelResource(name, data);
                    }
                    else
                    {
                        tempModelResources.Add(new ModelResource(name, data));
                    }
                }
            }

            if (bmodelResource == null || surfacesResource == null)
            {
                return false;
            }


            using (var bmodelStream = new MemoryStream(bmodelResource.Data))
            using (var surfaceStream = new MemoryStream(surfacesResource.Data))
            {
                var serializer = new DataContractJsonSerializer(typeof(SurfacePropertiesRoot[]));
                var surfacePropertiesRoot = (SurfacePropertiesRoot[])serializer.ReadObject(surfaceStream);
                if (!Decode(bmodelStream, out var tempSurfaceGroups))
                {
                    return false;
                }

                tempSurfaceGroups.ModelResources = tempModelResources.ToArray();

                foreach (var surfaceGroupInfo in tempSurfaceGroups.SurfaceGroupInfos)
                {
                    foreach (var rootProperty in surfacePropertiesRoot)
                    {
                        if (!surfaceGroupInfo.Properties.SurfaceName.Equals(rootProperty.SurfaceName, StringComparison.CurrentCultureIgnoreCase))
                        {
                            continue;
                        }
                        surfaceGroupInfo.Properties = new SurfacePropertiesRoot(rootProperty);
                        break;
                    }
                }

                surfaceGroups = tempSurfaceGroups;

            }

            return true;
        }

        public static bool Decode(Stream stream, out SurfaceGroups surfaceGroups)
        {
            surfaceGroups = null;

            var headerBuffer = ReadBytes(stream, 4);
            if (headerBuffer[0] != (byte) 'B' || headerBuffer[1] != (byte) 'M' || headerBuffer[2] != (byte) 'O' || headerBuffer[3] != (byte) 'D')
            {
                return false;
            }

            if (ReadUint(stream) != 1)
            {
                return false;
            }

            var tempSurfaceGroups = new SurfaceGroups
            {
                Scale = ReadFloat(stream),
                MinDimension = ReadVector3(stream),
                MaxDimension = ReadVector3(stream)
            };

            var indices = new List<uint>();
            var numIndices = ReadUint(stream);
            for (var i = 0; i < numIndices; i++)
            {
                indices.Add(ReadUint(stream));
            }
            tempSurfaceGroups.Indices = indices.ToArray();

            var vertices = new List<Vertex>();
            var numVertices = ReadUint(stream);
            for (var i = 0; i < numVertices; i++)
            {
                vertices.Add(ReadVertex(stream));
            }
            tempSurfaceGroups.Vertices = vertices.ToArray();

            var surfaceGroupInfos = new List<SurfaceGroupInfo>();
            var numSurfaceGroupInfos = ReadUint(stream);
            for (var i = 0; i < numSurfaceGroupInfos; i++)
            {
                var surfaceGroupInfo = new SurfaceGroupInfo
                {
                    IndexOffset = ReadUint(stream),
                    IndexCount = ReadUint(stream),
                    MinDimension = ReadVector3(stream),
                    MaxDimension = ReadVector3(stream),
                    UvDimension = ReadVector4(stream)
                    
                };
                var surfaceName = ReadString(stream);
                surfaceGroupInfo.Properties = new SurfacePropertiesRoot {SurfaceName = surfaceName};                
                surfaceGroupInfos.Add(surfaceGroupInfo);
            }
            tempSurfaceGroups.SurfaceGroupInfos = surfaceGroupInfos.ToArray();

            surfaceGroups = tempSurfaceGroups;
            return true;
        }
    }
}
