using System;
using System.Numerics;
using GUI.Utils;
using ValveResourceFormat;
using ValveResourceFormat.ResourceTypes;
using ValveResourceFormat.Serialization;

namespace GUI.Types.Renderer
{
    internal class WorldNodeLoader
    {
        private readonly WorldNode node;
        private readonly VrfGuiContext guiContext;

        public WorldNodeLoader(VrfGuiContext vrfGuiContext, WorldNode node)
        {
            this.node = node;
            guiContext = vrfGuiContext;
        }

        public void Load(Scene scene)
        {
            var data = node.Data;

            string[] worldLayers;

            if (data.ContainsKey("m_layerNames"))
            {
                worldLayers = data.GetArray<string>("m_layerNames");
            }
            else
            {
                worldLayers = Array.Empty<string>();
            }

            var sceneObjectLayerIndices = data.ContainsKey("m_sceneObjectLayerIndices") ? data.GetIntegerArray("m_sceneObjectLayerIndices") : null;
            var sceneObjects = data.GetArray("m_sceneObjects");
            var i = 0;


            int nb_MeshSceneNode = 0;

            // a chaque fois que j'ouvre une map, ca va recreer un nouveau BIN - et erase l'ancien
            // ca va etre exporté ici :
            // H:\MYDOC\installs&tools\VRF\git\per_frame_exporter\ValveResourceFormat\GUI\bin\Debug/
            System.IO.BinaryWriter richard_writer = null;


            // COMMENTER OU DECOMMENTER CETTE LIGNE SI JE VEUX ACTIVER OU PAS L'EXPORT
            /// richard_writer = new System.IO.BinaryWriter(System.IO.File.Open("_RICHA_MAP.bin", System.IO.FileMode.Create));


            if (richard_writer != null)
            {
                richard_writer.Write("MAP_BEG");
                richard_writer.Write((UInt32)305); // version de mon exporter.  a incrémenter si beosin
            }

            // Output is WorldNode_t we need to iterate m_sceneObjects inside it
            foreach (var sceneObject in sceneObjects)
            {
                var layerIndex = sceneObjectLayerIndices?[i++] ?? -1;

                // sceneObject is SceneObject_t
                var renderableModel = sceneObject.GetProperty<string>("m_renderableModel");
                var matrix = sceneObject.GetArray("m_vTransform").ToMatrix4x4();

                var tintColorWrongVector = sceneObject.GetSubCollection("m_vTintColor").ToVector4();

                Vector4 tintColor;
                if (tintColorWrongVector.W == 0)
                {
                    // Ignoring tintColor, it will fuck things up.
                    tintColor = Vector4.One;
                }
                else
                {
                    tintColor = new Vector4(tintColorWrongVector.X, tintColorWrongVector.Y, tintColorWrongVector.Z, tintColorWrongVector.W);
                }

                if (renderableModel != null)
                {
                    var newResource = guiContext.LoadFileByAnyMeansNecessary(renderableModel + "_c");

                    if (newResource == null)
                    {
                        continue;
                    }


                    if (richard_writer != null)
                    {
                        richard_writer.Write("MODEL_BEG");

                        string richard_model_name = ((Model)newResource.DataBlock).Data.GetProperty<string>("m_name");

                        // ecrire un end-terminated-string (  pas la meme chose que  richard_writer.Write(string)  qui va ecrire  taille+string.  mais taille je comprends pas bien comment c'est geré   )
                        for (int ic = 0; ic < richard_model_name.Length; ic++)
                            richard_writer.Write(richard_model_name[ic]);
                        richard_writer.Write((Byte)0);

                        // layer name
                        for (int ic = 0; ic < worldLayers[layerIndex].Length; ic++)
                            richard_writer.Write(worldLayers[layerIndex][ic]);
                        richard_writer.Write((Byte)0);

                        //tint
                        richard_writer.Write(tintColor.X);
                        richard_writer.Write(tintColor.Y);
                        richard_writer.Write(tintColor.Z);
                        richard_writer.Write(tintColor.W);

                        richard_writer.Write(matrix.M11);
                        richard_writer.Write(matrix.M12);
                        richard_writer.Write(matrix.M13);
                        richard_writer.Write(matrix.M14);
                        richard_writer.Write(matrix.M21);
                        richard_writer.Write(matrix.M22);
                        richard_writer.Write(matrix.M23);
                        richard_writer.Write(matrix.M24);
                        richard_writer.Write(matrix.M31);
                        richard_writer.Write(matrix.M32);
                        richard_writer.Write(matrix.M33);
                        richard_writer.Write(matrix.M34);
                        richard_writer.Write(matrix.M41);
                        richard_writer.Write(matrix.M42);
                        richard_writer.Write(matrix.M43);
                        richard_writer.Write(matrix.M44);
                    }


                    var modelNode = new ModelSceneNode(scene, (Model)newResource.DataBlock, null, false)
                    {
                        Transform = matrix,
                        Tint = tintColor,
                        LayerName = worldLayers[layerIndex],
                    };

                    if (richard_writer != null)
                    {
                        richard_writer.Write("MODEL_END");
                    }

                    scene.Add(modelNode, false);
                    

                }


                var renderable = sceneObject.GetProperty<string>("m_renderable");

                // ce cas la, j'ai l'impression qu'il n'arrive jamais, je vais l'ignorer
                if (renderable != null)
                {
                    
                    var newResource = guiContext.LoadFileByAnyMeansNecessary(renderable + "_c");

                    if (newResource == null)
                    {
                        continue;
                    }

                    var meshNode = new MeshSceneNode(scene, new Mesh(newResource))
                    {
                        Transform = matrix,
                        Tint = tintColor,
                        LayerName = worldLayers[layerIndex],
                    };

                    scene.Add(meshNode, false);
                    
                    nb_MeshSceneNode++;
                }



            } // for each object in the scene

            if (richard_writer != null)
            {
                richard_writer.Write("MAP_END");
                richard_writer.Close();
            }

            int attttt = 0;


        }
    }
}
