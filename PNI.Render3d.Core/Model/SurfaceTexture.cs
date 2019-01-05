namespace PNI.Rendering.Harmony
{
    public class SurfaceTexture
    {
        public byte[] ImageData { get; }

        public int Width { get; }

        public int Height { get; }

        public SurfaceTexture(byte[] imageData, int width, int height)
        {
            ImageData = imageData;
            Width = width;
            Height = height;
        }
    }
}
