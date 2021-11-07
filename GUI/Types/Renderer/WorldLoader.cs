using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using GUI.Utils;
using ValveResourceFormat.ResourceTypes;
using ValveResourceFormat.Utils;

namespace GUI.Types.Renderer
{
    internal class WorldLoader
    {
        private readonly World world;
        private readonly VrfGuiContext guiContext;

        // Contains metadata that can't be captured by manipulating the scene itself. Returned from Load().
        public class LoadResult
        {
            public HashSet<string> DefaultEnabledLayers { get; } = new HashSet<string>();

            public IDictionary<string, Matrix4x4> CameraMatrices { get; } = new Dictionary<string, Matrix4x4>();

            public Vector3? GlobalLightPosition { get; set; }

            public World Skybox { get; set; }
            public float SkyboxScale { get; set; } = 1.0f;
            public Vector3 SkyboxOrigin { get; set; } = Vector3.Zero;
        }

        public WorldLoader(VrfGuiContext vrfGuiContext, World world)
        {
            this.world = world;
            guiContext = vrfGuiContext;
        }

        public LoadResult Load(Scene scene)
        {
            var result = new LoadResult();

            // Output is World_t we need to iterate m_worldNodes inside it.
            var worldNodes = world.GetWorldNodeNames();
            foreach (var worldNode in worldNodes)
            {
                if (worldNode != null)
                {
                    var newResource = guiContext.LoadFileByAnyMeansNecessary(worldNode + ".vwnod_c");
                    if (newResource == null)
                    {
                        throw new Exception("WTF");
                    }

                    var subloader = new WorldNodeLoader(guiContext, (WorldNode)newResource.DataBlock);
                    subloader.Load(scene);
                }
            }





            System.IO.BinaryWriter richard_writer = null;
            // COMMENTER OU DECOMMENTER CETTE LIGNE SI JE VEUX ACTIVER OU PAS L'EXPORT
            // ca va creer le fichier ici:
            // H:\MYDOC\installs&tools\VRF\git\per_frame_exporter\ValveResourceFormat\GUI\bin\Debug
           ////// richard_writer = new System.IO.BinaryWriter(System.IO.File.Open("_RICHA_MAP_LIGHT.bin", System.IO.FileMode.Create));

            if (richard_writer != null)
            {
                richard_writer.Write("LIGHT_BEG_ALL");
                richard_writer.Write((UInt32)812); // version de mon exporter.  a incrémenter si beosin
            }

            // RICHARD:  chargement de la liste de Enities dans toute la MAP
            //           cette liste a l'air de parfaitement correspondre a la liste des enitites listées
            //           par mon script LUA.
            //           donc a priori je n'ai pas besoin de ces info, puisque mon script va les donner
            //
            //    donc en gros il faudrait la comment-out quand j'export un map ?.. je crois
            //    la j'enleve le comment-out que j'avais fait ce cette partie, car j'ai besoin de charger les lights
            //    du coup j'ai rajouté un IF,  qui se declanche que si je veux exporter les lights ... dans le doute ...
            //
            if (richard_writer != null)
            {

                foreach (var lumpName in world.GetEntityLumpNames())
                {
                    if (lumpName == null)
                    {
                        return result;
                    }

                    var newResource = guiContext.LoadFileByAnyMeansNecessary(lumpName + "_c");

                    if (newResource == null)
                    {
                        return result;
                    }

                    var entityLump = (EntityLump)newResource.DataBlock;
                    LoadEntitiesFromLump(richard_writer, scene, result, entityLump, "world_layer_base"); // TODO
                }


            }


            if (richard_writer != null)
            {
                richard_writer.Write("LIGHT_END_ALL");
                richard_writer.Close();
                int aaaa = 0;
            }




            return result;
        }

        private void LoadEntitiesFromLump(System.IO.BinaryWriter richard_writer, Scene scene, LoadResult result, EntityLump entityLump, string layerName = null)
        {
            var childEntities = entityLump.GetChildEntityNames();


            int light_spot__count = 0;
            int light_environment__count = 0;



            foreach (var childEntityName in childEntities)
            {
                var newResource = guiContext.LoadFileByAnyMeansNecessary(childEntityName + "_c");

                if (newResource == null)
                {
                    continue;
                }

                var childLump = (EntityLump)newResource.DataBlock;
                var childName = childLump.Data.GetProperty<string>("m_name");

                LoadEntitiesFromLump(richard_writer,  scene, result, childLump, childName);
            }

            var worldEntities = entityLump.GetEntities();



            foreach (var entity in worldEntities)
            {
                var classname = entity.GetProperty<string>("classname");

                if (classname == "info_world_layer")
                {
                    var spawnflags = entity.GetProperty<uint>("spawnflags");
                    var layername = entity.GetProperty<string>("layername");

                    // Visible on spawn flag
                    if ((spawnflags & 1) == 1)
                    {
                        result.DefaultEnabledLayers.Add(layername);
                    }

                    continue;
                }
                else if (classname == "skybox_reference")
                {
                    var worldgroupid = entity.GetProperty<string>("worldgroupid");
                    var targetmapname = entity.GetProperty<string>("targetmapname");

                    var skyboxWorldPath = $"maps/{Path.GetFileNameWithoutExtension(targetmapname)}/world.vwrld_c";
                    var skyboxPackage = guiContext.LoadFileByAnyMeansNecessary(skyboxWorldPath);

                    if (skyboxPackage != null)
                    {
                        result.Skybox = (World)skyboxPackage.DataBlock;
                    }
                }



                //var name_richa1 = entity.GetProperty<string>("name");
                //var name_richa2 = entity.GetProperty<string>("m_name");
                //var name_richa3 = entity.GetProperty<string>("m_szVariableName");

                // me permet d'avoir le nom de l'entity, exemple: "[PR#]prop_dogfood"
                // enlever le  [PR#]  pour avoir le nom donné in game
                var name_richa4 = entity.GetProperty<string>(0x4137af6b);



                var scale = entity.GetProperty<string>("scales");
                var position = entity.GetProperty<string>("origin");
                var angles = entity.GetProperty<string>("angles");
                var model = entity.GetProperty<string>("model");
                var skin = entity.GetProperty<string>("skin");
                var particle = entity.GetProperty<string>("effect_name");
                //var animation = entity.GetProperty<string>("defaultanim");
                string animation = null;


                /*

               {
                  // exemple de liste de properties pour une   light_spot  créée dans Hammer
                  // c'est assez facile a extraire, juste avec un simple copier/coller

                  angles = "-27.9287 352.686 15.324"
                  scales = "1 1 1"
                  "Transform Locked" = "0"
                  "Force Hidden" = "0"
                  "Editor Only" = "0"
                  targetname = ""
                  vscripts = ""
                  parentname = ""
                  parentAttachmentName = ""
                  "local.origin" = ""
                  "local.angles" = ""
                  "local.scales" = ""
                  useLocalOffset = "0"
                  enabled = "1"
                  color = "255 255 255"
                  brightness = "1.0"
                  range = "512"
                  castshadows = "1"
                  nearclipplane = "1"
                  Style = "0"
                  pattern = ""
                  fademindist = "-250"
                  fademaxdist = "1250"
                  rendertocubemaps = "1"
                  priority = "0"
                  lightgroup = ""
                  bouncescale = "1.0"
                  renderdiffuse = "1"
                  renderspecular = "1"
                  rendertransmissive = "1"
                  directlight = "1"
                  indirectlight = "1"
                  attenuation1 = "0.0"
                  attenuation2 = "1.0"
                  lightsourceradius = "2.0"
                  clientSideEntity = "0"
                  lightcookie = ""
                  falloff = "1"
                  innerconeangle = "45"
                  outerconeangle = "60"
                  shadowfademindist = "-250"
                  shadowfademaxdist = "1000"
                  shadowtexturewidth = "0"
                  shadowtextureheight = "0"
                  pvs_modify_entity = "0"
                  fogcontributionstrength = "1"
                  fog_lighting = "0"
                  baked_light_indexing = "1"
                  origin = "112.674 15.252 101.016"
              }





                


                ///////////   light_environment ///////
                angles = "0 0 0"
                scales = "1 1 1"
                "Transform Locked" = "0"
                "Force Hidden" = "0"
                "Editor Only" = "0"
                targetname = ""
                vscripts = ""
                parentname = ""
                parentAttachmentName = ""
                "local.origin" = ""
                "local.angles" = ""
                "local.scales" = ""
                useLocalOffset = "0"
                enabled = "1"
                color = "255 255 255"
                brightness = "1.0"
                range = "512"
                castshadows = "1"
                nearclipplane = "1"
                Style = "0"
                pattern = ""
                fademindist = "-250"
                fademaxdist = "1250"
                rendertocubemaps = "1"
                priority = "0"
                lightgroup = ""
                bouncescale = "1.0"
                renderdiffuse = "1"
                renderspecular = "1"
                rendertransmissive = "1"
                directlight = "1"
                indirectlight = "1"
                angulardiameter = "1.0"
                numcascades = "3"
                shadowcascadedistance0 = "0.0"
                shadowcascadedistance1 = "0.0"
                shadowcascadedistance2 = "0.0"
                shadowcascaderesolution0 = "0"
                shadowcascaderesolution1 = "0"
                shadowcascaderesolution2 = "0"
                skycolor = "255 255 255"
                skyintensity = "1.0"
                lower_hemisphere_is_black = "1"
                skytexture = ""
                skytexturescale = "1.0"
                skyambientbounce = "147 147 147"
                sunlightminbrightness = "32"
                ambient_occlusion = "0"
                max_occlusion_distance = "16.0"
                fully_occluded_fraction = "1.0"
                occlusion_exponent = "1.0"
                ambient_color = "0 0 0"
                fogcontributionstrength = "1"
                fog_lighting = "1"
                baked_light_indexing = "1"
                origin = "158.325 46.9555 15.6513"








               */



                if (
                    classname == "light"
                       || classname == "light_spot"    // <--- present dans la map 1 de Alyx  (il y en a en gros 32)
                    || classname == "light_dynamic"  // pas utilisé
                    || classname == "env_projectedtexture" // pas utilisé
                    || classname == "point_spotlight"  // pas utilisé
                   || classname == "light_environment"  // <--- present dans la map 1 de Alyx  (il y en a 1 seule je crois)
                    || classname == "light_directional"  // pas utilisé
                    )
                {


                    if (classname == "light_spot")
                    {
                        light_spot__count++;
                    }
                    else if (classname == "light_environment")
                    {
                        light_environment__count++;
                    }
                    else
                    {
                        int a = 0;

                    }

                    var XXXXXXX_not_exist = entity.GetProperty<string>("XXXXXXX_not_exist");

                    var brightness1 = entity.GetProperty<float>("brightness");
                    var color = entity.GetProperty<byte[]>("color");

                    var Force_Hidden1 = entity.GetProperty<string>("Force Hidden");
                    var Force_Hidden2 = entity.GetProperty<string>("\"Force Hidden\"");
                    var Force_Hidden3 = entity.GetProperty<string>("force hidden");
                    var Force_Hidden4 = entity.GetProperty<string>("\"force hidden\"");

                    var innerconeangle = entity.GetProperty<float>("innerconeangle");
                    var outerconeangle = entity.GetProperty<float>("outerconeangle");

                    var range = entity.GetProperty<float>("range"); // Range =   Distance range for light.  0=infinite

                    var fademindist = entity.GetProperty<float>("fademindist"); // Distance at which the light starts to fade 
                    var fademaxdist = entity.GetProperty<float>("fademaxdist"); // Maximum distance at which the light is visible (0 = don't fade out).




                    if (richard_writer != null)
                    {
                        richard_writer.Write("LIGHT_BEG");

                        richard_writer.Write(classname);
                        richard_writer.Write((float)brightness1);
                        richard_writer.Write((byte)color[0]);
                        richard_writer.Write((byte)color[1]);
                        richard_writer.Write((byte)color[2]);
                        richard_writer.Write((byte)color[3]);
                        richard_writer.Write((float)innerconeangle);
                        richard_writer.Write((float)outerconeangle);
                        richard_writer.Write((float)range);
                        richard_writer.Write((float)fademindist);
                        richard_writer.Write((float)fademaxdist);


                        // end-terminated-string
                        for (int ic = 0; ic < scale.Length; ic++)
                            richard_writer.Write(scale[ic]);
                        richard_writer.Write((Byte)0);


                        // end-terminated-string
                        for (int ic = 0; ic < position.Length; ic++)
                            richard_writer.Write(position[ic]);
                        richard_writer.Write((Byte)0);


                        // end-terminated-string
                        for (int ic = 0; ic < angles.Length; ic++)
                            richard_writer.Write(angles[ic]);
                        richard_writer.Write((Byte)0);



                        richard_writer.Write("LIGHT_END");




                    }




                    int aaa = 0;
                }


                if (scale == null || position == null || angles == null)
                {
                    continue;
                }

                var isGlobalLight = classname == "env_global_light";
                var isCamera =
                    classname == "sky_camera" ||
                    classname == "point_devshot_camera" ||
                    classname == "point_camera";
                var isTrigger =
                    classname.Contains("trigger") ||
                    classname == "post_processing_volume";

                var positionVector = EntityTransformHelper.ParseVector(position);

                var transformationMatrix = EntityTransformHelper.CalculateTransformationMatrix(entity);

                if (classname == "sky_camera")
                {
                    result.SkyboxScale = entity.GetProperty<ulong>("scale");
                    result.SkyboxOrigin = positionVector;
                }

                if (particle != null)
                {
                    var particleResource = guiContext.LoadFileByAnyMeansNecessary(particle + "_c");

                    if (particleResource != null)
                    {
                        var particleSystem = (ParticleSystem)particleResource.DataBlock;
                        var origin = new Vector3(positionVector.X, positionVector.Y, positionVector.Z);

                        try
                        {
                            var particleNode = new ParticleSceneNode(scene, particleSystem)
                            {
                                Transform = Matrix4x4.CreateTranslation(origin),
                                LayerName = layerName,
                            };
                            scene.Add(particleNode, true);
                        }
                        catch (Exception e)
                        {
                            Console.Error.WriteLine($"Failed to setup particle '{particle}': {e.Message}");
                        }
                    }

                    continue;
                }

                if (isCamera)
                {
                    var name = entity.GetProperty<string>("targetname") ?? string.Empty;
                    var cameraName = string.IsNullOrEmpty(name)
                        ? classname
                        : name;

                    result.CameraMatrices.Add(cameraName, transformationMatrix);

                    continue;
                }
                else if (isGlobalLight)
                {
                    result.GlobalLightPosition = positionVector;

                    continue;
                }
                else if (model == null)
                {
                    continue;
                }

                var objColor = Vector4.One;

                // Parse colour if present
                var colour = entity.GetProperty("rendercolor");

                // HL Alyx has an entity that puts rendercolor as a string instead of color255
                // TODO: Make an enum for these types
                if (colour != default && colour.Type == 0x09)
                {
                    var colourBytes = (byte[])colour.Data;
                    objColor.X = colourBytes[0] / 255.0f;
                    objColor.Y = colourBytes[1] / 255.0f;
                    objColor.Z = colourBytes[2] / 255.0f;
                    objColor.W = colourBytes[3] / 255.0f;
                }

                var newEntity = guiContext.LoadFileByAnyMeansNecessary(model + "_c");

                if (newEntity == null)
                {
                    var errorModelResource = guiContext.LoadFileByAnyMeansNecessary("models/dev/error.vmdl_c");

                    if (errorModelResource != null)
                    {
                        var errorModel = new ModelSceneNode(scene, (Model)errorModelResource.DataBlock, skin, false)
                        {
                            Transform = transformationMatrix,
                            LayerName = layerName,
                        };
                        scene.Add(errorModel, false);
                    }
                    else
                    {
                        Console.WriteLine("Unable to load error.vmdl_c. Did you add \"core/pak_001.dir\" to your game paths?");
                    }

                    continue;
                }

                var newModel = (Model)newEntity.DataBlock;

                var modelNode = new ModelSceneNode(scene, newModel, skin, false)
                {
                    Transform = transformationMatrix,
                    Tint = objColor,
                    LayerName = layerName,
                };

                if (animation != default)
                {
                    modelNode.LoadAnimation(animation); // Load only this animation
                    modelNode.SetAnimation(animation);
                }

                var bodyHash = EntityLumpKeyLookup.Get("body");
                if (entity.Properties.ContainsKey(bodyHash))
                {
                    var groups = modelNode.GetMeshGroups();
                    var body = entity.Properties[bodyHash].Data;
                    int bodyGroup = -1;

                    if (body is ulong bodyGroupLong)
                    {
                        bodyGroup = (int)bodyGroupLong;
                    }
                    else if (body is string bodyGroupString)
                    {
                        if (!int.TryParse(bodyGroupString, out bodyGroup))
                        {
                            bodyGroup = -1;
                        }
                    }

                    modelNode.SetActiveMeshGroups(groups.Skip(bodyGroup).Take(1));
                }

                scene.Add(modelNode, false);

                var phys = newModel.GetEmbeddedPhys();
                if (phys == null)
                {
                    var refPhysicsPaths = newModel.GetReferencedPhysNames();
                    if (refPhysicsPaths.Any())
                    {
                        var newResource = guiContext.LoadFileByAnyMeansNecessary(refPhysicsPaths.First() + "_c");
                        if (newResource != null)
                        {
                            phys = (PhysAggregateData)newResource.DataBlock;
                        }
                    }
                }

                if (phys != null)
                {
                    var physSceneNode = new PhysSceneNode(scene, phys)
                    {
                        Transform = transformationMatrix,
                        IsTrigger = isTrigger,
                        LayerName = layerName
                    };
                    scene.Add(physSceneNode, false);
                }
            }




        }
    }
}
