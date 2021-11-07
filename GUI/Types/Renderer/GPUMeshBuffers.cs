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
                GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(vbib.VertexBuffers[i].ElementCount * vbib.VertexBuffers[i].ElementSizeInBytes), vbib.VertexBuffers[i].Data, BufferUsageHint.StaticDraw);





                if (richard_writer != null)
                {
                    richard_writer.Write("VB_BEG");

                    //richard_writer.Write((UInt32)vbib.VertexBuffers[i].Count);
                    richard_writer.Write((UInt32)vbib.VertexBuffers[i].ElementCount);

                    //richard_writer.Write((UInt32)vbib.VertexBuffers[i].Size);
                    richard_writer.Write((UInt32)vbib.VertexBuffers[i].ElementSizeInBytes);

                    //richard_writer.Write((UInt32)vbib.VertexBuffers[i].Attributes.Count);
                    richard_writer.Write((UInt32)vbib.VertexBuffers[i].InputLayoutFields.Count);

                    for (int aa = 0; aa < vbib.VertexBuffers[i].InputLayoutFields.Count; aa++)
                    {
                        richard_writer.Write(vbib.VertexBuffers[i].InputLayoutFields[aa].SemanticName);
                        richard_writer.Write(vbib.VertexBuffers[i].InputLayoutFields[aa].Offset);
                        richard_writer.Write((UInt32)vbib.VertexBuffers[i].InputLayoutFields[aa].Format);
                    }

                    richard_writer.Write(vbib.VertexBuffers[i].Data);
                    richard_writer.Write("VB_END");
                    richard_writer.Flush();
                }




                GL.GetBufferParameter(BufferTarget.ArrayBuffer, BufferParameterName.BufferSize, out VertexBuffers[i].Size);
            }

            for (var i = 0; i < vbib.IndexBuffers.Count; i++)
            {
                IndexBuffers[i].Handle = (uint)GL.GenBuffer();
                GL.BindBuffer(BufferTarget.ElementArrayBuffer, IndexBuffers[i].Handle);
                GL.BufferData(BufferTarget.ElementArrayBuffer, (IntPtr)(vbib.IndexBuffers[i].ElementCount * vbib.IndexBuffers[i].ElementSizeInBytes), vbib.IndexBuffers[i].Data, BufferUsageHint.StaticDraw);

                if (richard_writer != null)
                {
                    richard_writer.Write("ID_BEG");
                    richard_writer.Write((UInt32)vbib.IndexBuffers[i].ElementCount);
                    richard_writer.Write((UInt32)vbib.IndexBuffers[i].ElementSizeInBytes);
                    richard_writer.Write(vbib.IndexBuffers[i].Data);
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
