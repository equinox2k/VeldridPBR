using System;
using System.Collections.Generic;
using System.Numerics;

namespace VeldridNSViewExample
{
    public class Mesh 
    {
  
        public ushort[] Indices { get; set; }
        public Vertex[] Vertices { get; set; }

        public Mesh(int startX, int startY, int width, int height, int cols, int rows)
        {
            var vertices = new List<Vertex>();
            var indices = new List<ushort>();
            for (var tempY = 0; tempY < rows; tempY++)
            {
                var y = startY + tempY;
                for (var tempX = 0; tempX < cols; tempX++)
                {
                    var x = startX + tempX;
                    var vertex = new Vertex
                    {
                        PositionX = x / (cols - 1.0f) * width - width / 2.0f,
                        PositionY = y / (rows - 1.0f) * height - height / 2.0f,
                        PositionZ = 0,
                        TexCoordX = x / (cols - 1.0f),
                        TexCoordY = 1.0f - y / (rows - 1.0f),
                        NormalX = 0.0f,
                        NormalY = 0.0f,
                        NormalZ = 1.0f,
                        TangentX = 1.0f,
                        TangentY = 0.0f,
                        TangentZ = 0.0f
                    };

                    vertices.Add(vertex);
                    if (x >= cols - 1 || y >= rows - 1)
                    {
                        continue;
                    }
                    indices.Add((ushort)(y * cols + x + 1));
                    indices.Add((ushort)((y + 1) * cols + x + 1));
                    indices.Add((ushort)(y * cols + x));
                    indices.Add((ushort)((y + 1) * cols + x + 1));
                    indices.Add((ushort)((y + 1) * cols + x));
                    indices.Add((ushort)(y * cols + x));
                }
            }
            Vertices = vertices.ToArray();
            Indices = indices.ToArray();
        }
    }
}
