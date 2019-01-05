using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Runtime.Serialization.Json;
using System.Security.Cryptography;
using System.Text;

namespace PNI.Rendering.Harmony
{
    public static class Exporter
    {

        private static string CreateMd5Name(string filePath)
        {
            var fileData = File.ReadAllBytes(filePath);
            using (var md5 = MD5.Create())
            {
                var hashBuffer = md5.ComputeHash(fileData);
                var stringBuilder = new StringBuilder(hashBuffer.Length * 2);
                foreach (var value in hashBuffer)
                {
                    stringBuilder.AppendFormat("{0:x2}", value);
                }
                return $"{stringBuilder}{Path.GetExtension(filePath)}";
            }
        }

        public static void Export(Stream stream, string fbxPath, SurfaceGroups surfaceGroups)
        {
            var filenames = new HashSet<string>();

            using (var zipArchive = new ZipArchive(stream, ZipArchiveMode.Create, true))
            {
                zipArchive.CreateEntryFromFile(fbxPath, "object.fbx");

                var modelEntry = zipArchive.CreateEntry("object.bmodel");
                using (var streamWriter = new BinaryWriter(modelEntry.Open()))
                using (var memoryStream = new MemoryStream())
                {
                    ModelWriter.Encode(memoryStream, surfaceGroups);
                    var buffer = memoryStream.ToArray();
                    streamWriter.Write(buffer, 0, buffer.Length);
                }

                var propertiesArray = new List<SurfacePropertiesRoot>();
                foreach (var surfaceGroupInfo in surfaceGroups.SurfaceGroupInfos)
                {
                    var properties = new SurfacePropertiesRoot(surfaceGroupInfo.Properties);
                    if (!string.IsNullOrEmpty(properties.Diffuse.File))
                    {
                        var filePath = properties.Diffuse.File;
                        var md5Name = CreateMd5Name(filePath);
                        if (!filenames.Contains(md5Name))
                        {
                            filenames.Add(md5Name);
                            zipArchive.CreateEntryFromFile(filePath, md5Name);
                        }
                        properties.Diffuse.File = md5Name;
                    }
                    if (!string.IsNullOrEmpty(properties.Bumpmap.File))
                    {
                        var filePath = properties.Bumpmap.File;
                        var md5Name = CreateMd5Name(filePath);
                        if (!filenames.Contains(md5Name))
                        {
                            filenames.Add(md5Name);
                            zipArchive.CreateEntryFromFile(filePath, md5Name);
                        }
                        properties.Bumpmap.File = md5Name;
                    }
                    if (!string.IsNullOrEmpty(properties.Effect.File))
                    {
                        var filePath = properties.Effect.File;
                        var md5Name = CreateMd5Name(filePath);
                        if (!filenames.Contains(md5Name))
                        {
                            filenames.Add(md5Name);
                            zipArchive.CreateEntryFromFile(filePath, md5Name);
                        }
                        properties.Effect.File = md5Name;
                    }
                    for (var i = 0; i < properties.Options.Count; i++)
                    {                                            
                        if (!string.IsNullOrEmpty(properties.Options[i].Diffuse.File))
                        {
                            var filePath = properties.Options[i].Diffuse.File;
                            var md5Name = CreateMd5Name(filePath);
                            if (!filenames.Contains(md5Name))
                            {
                                filenames.Add(md5Name);
                                zipArchive.CreateEntryFromFile(filePath, md5Name);
                            }
                            properties.Options[i].Diffuse.File = md5Name;
                        }
                        if (!string.IsNullOrEmpty(properties.Options[i].Bumpmap.File))
                        {
                            var filePath = properties.Options[i].Bumpmap.File;
                            var md5Name = CreateMd5Name(filePath);
                            if (!filenames.Contains(md5Name))
                            {
                                filenames.Add(md5Name);
                                zipArchive.CreateEntryFromFile(filePath, md5Name);
                            }
                            properties.Options[i].Bumpmap.File = md5Name;
                        }
                        if (!string.IsNullOrEmpty(properties.Options[i].Effect.File))
                        {
                            var filePath = properties.Options[i].Effect.File;
                            var md5Name = CreateMd5Name(filePath);
                            if (!filenames.Contains(md5Name))
                            {
                                filenames.Add(md5Name);
                                zipArchive.CreateEntryFromFile(filePath, md5Name);
                            }
                            properties.Options[i].Effect.File = md5Name;
                        }
                    }
                    propertiesArray.Add(properties);                    
                }

                var surfaceEntry = zipArchive.CreateEntry("surfaces.json");
                using (var streamWriter = new BinaryWriter(surfaceEntry.Open()))
                {
                    var serializer = new DataContractJsonSerializer(typeof(SurfacePropertiesRoot[]));
                    using (var tempStream = new MemoryStream())
                    {
                        serializer.WriteObject(tempStream, propertiesArray.ToArray());
                        var jsonBuffer = tempStream.ToArray();
                        streamWriter.Write(jsonBuffer, 0, jsonBuffer.Length);
                    }
                }
            }
        }
    }
}
