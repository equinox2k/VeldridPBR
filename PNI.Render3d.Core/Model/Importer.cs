using System;
using System.IO;
using System.Runtime.Serialization.Json;

namespace PNI.Rendering.Harmony
{
    public static class Importer
    {
        public static bool Import(string filePath, out SurfaceGroups surfaceGroups)
        {
            surfaceGroups = null;

            var bmodelPath = Path.Combine(filePath, "object.bmodel");
            if (!File.Exists(bmodelPath))
            {
                return false;
            }

            var surfacesPath = Path.Combine(filePath, "surfaces.json");
            if (!File.Exists(surfacesPath))
            {
                return false;
            }

            //todo UPDATE FILE PATHS

            using (var bmodelStream = File.OpenRead(bmodelPath))
            using (var surfaceStream = new FileStream(surfacesPath, FileMode.Open))
            {
                var serializer = new DataContractJsonSerializer(typeof(SurfacePropertiesRoot[]));
                var surfacePropertiesRoot = (SurfacePropertiesRoot[])serializer.ReadObject(surfaceStream);
                if (!ModelReader.Decode(bmodelStream, out var tempSurfaceGroups))
                {
                    return false;
                }
                foreach (var surfaceGroupInfo in tempSurfaceGroups.SurfaceGroupInfos)
                {
                    foreach (var rootProperty in surfacePropertiesRoot)
                    {
                        if (!surfaceGroupInfo.Properties.SurfaceName.Equals(rootProperty.SurfaceName, StringComparison.CurrentCultureIgnoreCase))
                        {
                            continue;
                        }
                        if (!string.IsNullOrEmpty(rootProperty.Diffuse.File))
                        {
                            rootProperty.Diffuse.File = Path.Combine(filePath, rootProperty.Diffuse.File);
                        }
                        if (!string.IsNullOrEmpty(rootProperty.Bumpmap.File))
                        {
                            rootProperty.Bumpmap.File = Path.Combine(filePath, rootProperty.Bumpmap.File);
                        }
                        if (!string.IsNullOrEmpty(rootProperty.Effect.File))
                        {
                            rootProperty.Effect.File = Path.Combine(filePath, rootProperty.Effect.File);
                        }
                        foreach (var option in rootProperty.Options)
                        {
                            if (!string.IsNullOrEmpty(option.Diffuse.File))
                            {
                                option.Diffuse.File = Path.Combine(filePath, option.Diffuse.File);
                            }
                            if (!string.IsNullOrEmpty(option.Bumpmap.File))
                            {
                                option.Bumpmap.File = Path.Combine(filePath, option.Bumpmap.File);
                            }
                            if (!string.IsNullOrEmpty(option.Effect.File))
                            {
                                option.Effect.File = Path.Combine(filePath, option.Effect.File);
                            }
                        }
                        surfaceGroupInfo.Properties = new SurfacePropertiesRoot(rootProperty);
                        break;
                    }
                }
                surfaceGroups = tempSurfaceGroups;               
            }
            return true;
        }
    }
}
