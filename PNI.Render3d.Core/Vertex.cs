using System.Numerics;
using System.Text;
using PNI.Rendering.Harmony.Helpers;

namespace PNI.Rendering.Harmony
{
    public struct Vertex
    {
        public const uint SizeInBytes = 44;

        public float PositionX;
        public float PositionY;
        public float PositionZ;

        public float TexCoordX;
        public float TexCoordY;

        public float NormalX;
        public float NormalY;
        public float NormalZ;

        public float TangentX;
        public float TangentY;
        public float TangentZ;

        public Vertex(Vector3 position, Vector2 texCoord, Vector3 normal, Vector3 tangent)
        {
            PositionX = position.X;
            PositionY = position.Y;
            PositionZ = position.Z;
            TexCoordX = texCoord.X;
            TexCoordY = texCoord.Y;
            NormalX = normal.X;
            NormalY = normal.Y;
            NormalZ = normal.Z;
            TangentX = tangent.X;
            TangentY = tangent.Y;
            TangentZ = tangent.Z;
        }

        public string GetHash()
        {
            var hashCodes = new StringBuilder();
            hashCodes.Append(PositionX.GetHashCode().ToString("X"));
            hashCodes.Append("_");
            hashCodes.Append(PositionY.GetHashCode().ToString("X"));
            hashCodes.Append("_");
            hashCodes.Append(PositionZ.GetHashCode().ToString("X"));
            hashCodes.Append("_");
            hashCodes.Append(TexCoordX.GetHashCode().ToString("X"));
            hashCodes.Append("_");
            hashCodes.Append(TexCoordY.GetHashCode().ToString("X"));
            hashCodes.Append("_");
            hashCodes.Append(NormalX.GetHashCode().ToString("X"));
            hashCodes.Append("_");
            hashCodes.Append(NormalY.GetHashCode().ToString("X"));
            hashCodes.Append("_");
            hashCodes.Append(NormalZ.GetHashCode().ToString("X"));
            hashCodes.Append("_");
            hashCodes.Append(TangentX.GetHashCode().ToString("X"));
            hashCodes.Append("_");
            hashCodes.Append(TangentY.GetHashCode().ToString("X"));
            hashCodes.Append("_");
            hashCodes.Append(TangentZ.GetHashCode().ToString("X"));
            return HashHelper.ComputeStringHash(hashCodes.ToString());
        }

        public bool Equals(Vertex other)
        {
            return GetHash().Equals(other.GetHash());
        }
    }
}
