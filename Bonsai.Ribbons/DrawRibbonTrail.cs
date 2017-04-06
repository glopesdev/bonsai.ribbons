using Bonsai;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using Bonsai.Shaders;
using OpenTK;
using System.ComponentModel;
using System.Drawing.Design;
using OpenTK.Graphics.OpenGL4;

namespace Bonsai.Ribbons
{
    public class DrawRibbonTrail : Sink<Matrix4[]>
    {
        [Description("The name of the shader program.")]
        [Editor("Bonsai.Shaders.Configuration.Design.ShaderConfigurationEditor, Bonsai.Shaders.Design", typeof(UITypeEditor))]
        public string ShaderName { get; set; }

        static void AddVector(ref Vector4 vector, ref Vector3 axis, out Vector4 result)
        {
            result.X = vector.X + axis.X;
            result.Y = vector.Y + axis.Y;
            result.Z = vector.Z + axis.Z;
            result.W = 1;
        }

        static void SubtractVector(ref Vector4 vector, ref Vector3 axis, out Vector4 result)
        {
            result.X = vector.X - axis.X;
            result.Y = vector.Y - axis.Y;
            result.Z = vector.Z - axis.Z;
            result.W = 1;
        }

        static void BindVertexAttributes(int vbo, int vao)
        {
            GL.BindVertexArray(vao);
            GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);

            for (int i = 0; i < 3; i++)
            {
                GL.EnableVertexAttribArray(i);
                GL.VertexAttribPointer(
                    i, 4, VertexAttribPointerType.Float, false,
                    BlittableValueType<Vector4>.Stride,
                    i * 2 * BlittableValueType<Vector4>.Stride);
            }

            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GL.BindVertexArray(0);
        }

        public override IObservable<Matrix4[]> Process(IObservable<Matrix4[]> source)
        {
            return Observable.Defer(() =>
            {
                Mesh mesh = null;
                Vector4[] vertices = null;
                Vector3 surfaceDirection = Vector3.UnitY;
                return source.CombineEither(
                    ShaderManager.ReserveShader(ShaderName).Do(shader =>
                    {
                        mesh = new Mesh();
                        mesh.DrawMode = PrimitiveType.TriangleStrip;
                        BindVertexAttributes(mesh.VertexBuffer, mesh.VertexArray);
                    }),
                    (input, shader) =>
                    {
                        shader.Update(() =>
                        {
                            vertices = new Vector4[input.Length * 2];
                            for (int i = 0; i < input.Length; i++)
                            {
                                Vector3 axis;
                                Vector3.TransformVector(ref surfaceDirection, ref input[i], out axis);
                                AddVector(ref input[i].Row3, ref axis, out vertices[i * 2 + 0]);
                                SubtractVector(ref input[i].Row3, ref axis, out vertices[i * 2 + 1]);
                            }

                            mesh.VertexCount = VertexHelper.UpdateVertexBuffer(mesh.VertexBuffer, vertices, BufferUsageHint.DynamicDraw);
                            mesh.Draw();
                        });
                        return input;
                    }).Finally(() =>
                    {
                        if (mesh != null)
                        {
                            mesh.Dispose();
                        }
                    });
            });
        }
    }
}
