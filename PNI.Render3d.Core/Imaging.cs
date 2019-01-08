using System;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Primitives;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Advanced;
using SixLabors.Primitives;
using Veldrid;
using Veldrid.ImageSharp;
using System.Numerics;
using System.IO;
using PNI.Rendering.Harmony.Model;

namespace PNI.Rendering.Harmony
{
    public static class Imaging
    {
        public static Image<Rgba32> Resize(Image<Rgba32> original, Size size, Point topLeft, Point bottomRight)
        {
            Image<Rgba32> result = new Image<Rgba32>(size.Width, size.Height);
            var targetSize = new Size(bottomRight.X - topLeft.X, bottomRight.Y - topLeft.Y);
            using (var cloned = original.Clone())
            {
                cloned.Mutate(x => x.Resize(targetSize));
                result.Mutate(x => x.DrawImage(cloned, new Point(topLeft.X, topLeft.Y), 1.0f));
            }
            return result;
        }

        public static Image<Rgba32> Tile(Image<Rgba32> original, Size size)
        {
            Image<Rgba32> result = new Image<Rgba32>(size.Width, size.Height);
            for (var x = 0; x < size.Width; x += original.Width)
            {
                for (var y = 0; y < size.Height; y += original.Height)
                {
                    result.Mutate(m => m.DrawImage(original, new Point(x, y), 1.0f));
                }
            }
            return result;
        }

        public static Image<Rgba32> CreateImage(Size size)
        {
            return new Image<Rgba32>(size.Width, size.Height);
        }

        public static TextureView ImageToTextureView(Image<Rgba32> image, GraphicsDevice graphicsDevice, ResourceFactory resourceFactory)
        {
            var texture = new ImageSharpTexture(image).CreateDeviceTexture(graphicsDevice, resourceFactory);
            return resourceFactory.CreateTextureView(texture);
        }

        private static Image<Rgba32> GetModelResourceImage(SurfaceProperties surfaceProperties, SurfaceTextureProperty surfaceTextureProperty, ModelResource[] modelResources, Image<Rgba32> photo1, Image<Rgba32> photo2)
        {
            if (surfaceTextureProperty.File.Equals("[Photo1]", StringComparison.CurrentCultureIgnoreCase))
            {
                return photo1.Clone();
            }
            if (surfaceTextureProperty.File.Equals("[Photo2]", StringComparison.CurrentCultureIgnoreCase))
            {
                return photo2.Clone();
            }
            foreach (var modelResource in modelResources)
            {
                if (!modelResource.Name.Equals(surfaceTextureProperty.File, StringComparison.CurrentCultureIgnoreCase))
                {
                    continue;
                }
                using (var memoryStream = new MemoryStream(modelResource.Data))
                {
                    return Image.Load(memoryStream);
                }
            }
            throw new Exception("ModelResource not found.");
        }

        public static Image<Rgba32> GetModelTexture(SurfaceProperties surfaceProperties, SurfaceTextureProperty surfaceTextureProperty, Vector4 uvDimension, ModelResource[] modelResources, Image<Rgba32> photo1, Image<Rgba32> photo2)
        {
            using (var image = GetModelResourceImage(surfaceProperties, surfaceTextureProperty, modelResources, photo1, photo2))
            {
                if (surfaceTextureProperty.Tile)
                {
                    return Tile(image, new Size(1024, 1024));
                }
                var topLeftX = (int)Math.Floor(uvDimension.X * 1024);
                var topLeftY = (int)Math.Floor(uvDimension.Y * 1024);
                var bottomRightX = (int)Math.Ceiling(uvDimension.Z * 1024);
                var bottomRightY = (int)Math.Ceiling(uvDimension.W * 1024);
                return Resize(image, new Size(1024, 1024), new Point(topLeftX, topLeftY), new Point(bottomRightX, bottomRightY));
            }
        }
    }
}
