using System.Collections.Generic;
using System.Runtime.Serialization;

namespace PNI.Rendering.Harmony.Model
{
    [DataContract]
    public class SurfacePropertiesRoot : SurfaceProperties
    {
        private bool _disposed;

        [DataMember]
        public string SurfaceName { get; set; }

        [DataMember]
        public List<SurfacePropertiesOption> Options { get; set; }

        public SurfacePropertiesRoot()
        {
            Options = new List<SurfacePropertiesOption>();
        }

        public SurfacePropertiesRoot(SurfacePropertiesRoot surfacePropertiesRoot) : base(surfacePropertiesRoot)
        {
            SurfaceName = surfacePropertiesRoot.SurfaceName;
            Options = new List<SurfacePropertiesOption>();
            foreach (var option in surfacePropertiesRoot.Options)
            {
                Options.Add(new SurfacePropertiesOption(option));
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }
            if (disposing)
            {
                foreach (var option in Options)
                {
                    option.Dispose();
                }
            }
            _disposed = true;
            base.Dispose(disposing);
        }
    }
}
