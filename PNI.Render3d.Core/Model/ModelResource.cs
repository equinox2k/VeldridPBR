namespace PNI.Rendering.Harmony.Model
{
    public class ModelResource
    {
        public string Name { get; }
        public byte[] Data { get; }

        public ModelResource(string name, byte[] data)
        {
            Name = name;
            Data = data;
        }
    }
}
