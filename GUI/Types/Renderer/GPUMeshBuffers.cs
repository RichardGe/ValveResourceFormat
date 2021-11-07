using System;
using System.Linq;
using OpenTK.Graphics.OpenGL;
using ValveResourceFormat.Blocks;

namespace GUI.Types.Renderer
{
    public class GPUMeshBuffers
    {
        public struct Buffer
        {
#pragma warning disable CA1051 // Do not declare visible instance fields
            public uint Handle;
            public long Size;
#pragma warning restore CA1051 // Do not declare visible instance fields
        }

        public Buffer[] VertexBuffers { get; private set; }
        public Buffer[] IndexBuffers { get; private set; }

        public GPUMeshBuffers(VBIB vbib, System.IO.BinaryWriter richard_writer)
        {
            VertexBuffers = new Buffer[vbib.VertexBuffers.Count];
            IndexBuffers = new Buffer[vbib.IndexBuffers.Count];


            if (richard_writer != null)
            {
                richard_writer.Write("SUBMESH_BEG");
                richard_writer.Write(vbib.VertexBuffers.Count);
                richard_writer.Write(vbib.IndexBuffers.Count);
            }

            for (var i = 0; i < vbib.VertexBuffers.Count; i++)
            {
                VertexBuffers[i].Handle = (uint)GL.GenBuffer();
                GL.BindBuffer(BufferTarget.ArrayBuffer, VertexBuffers[i].Handle);
                GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(vbib.VertexBuffers[i].Count * vbib.VertexBuffers[i].Size), vbib.VertexBuffers[i].Buffer, BufferUsageHint.StaticDraw);




                {


                    byte b0 = vbib.VertexBuffers[i].Buffer[20*0+16+0];
                    byte b1 = vbib.VertexBuffers[i].Buffer[20 * 0 + 16 + 1];
                    byte b2 = vbib.VertexBuffers[i].Buffer[20 * 0 + 16 + 2];
                    byte b3 = vbib.VertexBuffers[i].Buffer[20 * 0 + 16 + 3];

                    byte b4 = vbib.VertexBuffers[i].Buffer[20 * 1 + 16 + 0];
                    byte b5 = vbib.VertexBuffers[i].Buffer[20 * 1 + 16 + 1];
                    byte b6 = vbib.VertexBuffers[i].Buffer[20 * 1 + 16 + 2];
                    byte b7 = vbib.VertexBuffers[i].Buffer[20 * 1 + 16 + 3];

                    int a = 12;


                }



                if (richard_writer != null)
                {
                    richard_writer.Write("VB_BEG");
                    richard_writer.Write((UInt32)vbib.VertexBuffers[i].Count);
                    richard_writer.Write((UInt32)vbib.VertexBuffers[i].Size);
                    richard_writer.Write((UInt32)vbib.VertexBuffers[i].Attributes.Count);
                    for (int aa = 0; aa < vbib.VertexBuffers[i].Attributes.Count; aa++)
                    {
                        richard_writer.Write(vbib.VertexBuffers[i].Attributes[aa].Name);
                        richard_writer.Write(vbib.VertexBuffers[i].Attributes[aa].Offset);
                        richard_writer.Write((UInt32)vbib.VertexBuffers[i].Attributes[aa].Type);
                    }

                    richard_writer.Write(vbib.VertexBuffers[i].Buffer);
                    richard_writer.Write("VB_END");
                    richard_writer.Flush();
                }




                GL.GetBufferParameter(BufferTarget.ArrayBuffer, BufferParameterName.BufferSize, out VertexBuffers[i].Size);
            }

            for (var i = 0; i < vbib.IndexBuffers.Count; i++)
            {
                IndexBuffers[i].Handle = (uint)GL.GenBuffer();
                GL.BindBuffer(BufferTarget.ElementArrayBuffer, IndexBuffers[i].Handle);
                GL.BufferData(BufferTarget.ElementArrayBuffer, (IntPtr)(vbib.IndexBuffers[i].Count * vbib.IndexBuffers[i].Size), vbib.IndexBuffers[i].Buffer, BufferUsageHint.StaticDraw);

                if (richard_writer != null)
                {
                    richard_writer.Write("ID_BEG");
                    richard_writer.Write((UInt32)vbib.IndexBuffers[i].Count);
                    richard_writer.Write((UInt32)vbib.IndexBuffers[i].Size);
                    richard_writer.Write(vbib.IndexBuffers[i].Buffer);
                    richard_writer.Write("ID_END");
                    richard_writer.Flush();
                }


                GL.GetBufferParameter(BufferTarget.ElementArrayBuffer, BufferParameterName.BufferSize, out IndexBuffers[i].Size);
            }

            if (richard_writer != null)
            {
                richard_writer.Write("SUBMESH_END");
            }


        }
    }
}
