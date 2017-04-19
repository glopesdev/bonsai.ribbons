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
    public class UpdateRibbonTrail : Sink<Matrix4[]>
    {
        public UpdateRibbonTrail()
        {
            Axis = Vector3.UnitY;
        }

        [Description("The name of the mesh.")]
        [Editor("Bonsai.Shaders.Configuration.Design.MeshConfigurationEditor, Bonsai.Shaders.Design", typeof(UITypeEditor))]
        public string MeshName { get; set; }

        [TypeConverter("OpenCV.Net.NumericAggregateConverter, OpenCV.Net")]
        [Description("An optional fixed offset for ribbon trail vertices.")]
        public Vector3 Offset { get; set; }

        [TypeConverter("OpenCV.Net.NumericAggregateConverter, OpenCV.Net")]
        [Description("The axis along which the thickness of the ribbon trail is expressed.")]
        public Vector3 Axis { get; set; }

        [Description("The thickness of the ribbon trail.")]
        public float Width { get; set; }

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

        public IObservable<Matrix4[]> Process(IObservable<Matrix4> source)
        {
            return Observable.Defer(() =>
            {
                var poses = new List<Matrix4>();
                return Process(source.Select(input =>
                {
                    if (!float.IsNaN(input.M11) || poses.Count == 0 || !float.IsNaN(poses[poses.Count - 1].M11))
                    {
                        poses.Add(input);
                    }
                    return poses.ToArray();
                }));
            });
        }

        public override IObservable<Matrix4[]> Process(IObservable<Matrix4[]> source)
        {
            return Observable.Defer(() =>
            {
                Mesh mesh = null;
                Vector4[] vertices = null;
                return source.CombineEither(
                    ShaderManager.WindowSource.Do(window =>
                    {
                        window.Update(() =>
                        {
                            mesh = window.Meshes[MeshName];
                            mesh.DrawMode = PrimitiveType.TriangleStrip;
                            BindVertexAttributes(mesh.VertexBuffer, mesh.VertexArray);
                        });
                    }),
                    (input, window) =>
                    {
                        window.Update(() =>
                        {
                            var offset = Offset;
                            var surfaceDirection = Width * Axis;
                            vertices = new Vector4[input.Length * 2];
                            for (int i = 0; i < input.Length; i++)
                            {
                                Vector3 axis, off;
                                Vector3.TransformVector(ref surfaceDirection, ref input[i], out axis);
                                Vector3.TransformVector(ref offset, ref input[i], out off);
                                AddVector(ref input[i].Row3, ref off, out input[i].Row3);
                                AddVector(ref input[i].Row3, ref axis, out vertices[i * 2 + 0]);
                                SubtractVector(ref input[i].Row3, ref axis, out vertices[i * 2 + 1]);
                            }

                            mesh.VertexCount = VertexHelper.UpdateVertexBuffer(mesh.VertexBuffer, vertices, BufferUsageHint.DynamicDraw);
                        });
                        return input;
                    });
            });
        }
    }
}
