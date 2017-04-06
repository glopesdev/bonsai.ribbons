using OpenTK;
using OpenTK.Graphics.OpenGL4;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bonsai.Ribbons
{
    static class VertexHelper
    {
        public static int UpdateVertexBuffer<TVertex>(int vertexBuffer, TVertex[] buffer, BufferUsageHint usage)
            where TVertex : struct
        {
            var bufferSize = buffer.Length * BlittableValueType<TVertex>.Stride;
            GL.BindBuffer(BufferTarget.ArrayBuffer, vertexBuffer);
            GL.BufferData(BufferTarget.ArrayBuffer,
                          new IntPtr(bufferSize),
                          buffer, usage);
            return buffer.Length;
        }
    }
}
