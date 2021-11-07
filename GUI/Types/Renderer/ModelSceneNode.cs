using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using OpenTK.Graphics.OpenGL;
using ValveResourceFormat;
using ValveResourceFormat.ResourceTypes;
using ValveResourceFormat.ResourceTypes.ModelAnimation;
using ValveResourceFormat.Serialization;

using System.IO;

namespace GUI.Types.Renderer
{
    internal class ModelSceneNode : SceneNode, IRenderableMeshCollection
    {
        private Model Model { get; }
        public Vector4 Tint
        {
            get
            {
                if (meshRenderers.Count > 0)
                {
                    return meshRenderers[0].Tint;
                }

                return Vector4.One;
            }
            set
            {
                foreach (var renderer in meshRenderers)
                {
                    renderer.Tint = value;
                }
            }
        }

        public AnimationController AnimationController { get; } = new();
        public IEnumerable<RenderableMesh> RenderableMeshes => activeMeshRenderers;

        private readonly List<RenderableMesh> meshRenderers = new List<RenderableMesh>();
        private readonly List<Animation> animations = new List<Animation>();
        private Dictionary<string, string> skinMaterials;

        private Animation activeAnimation;
        private int[] animationTextures;
        private Skeleton[] skeletons;

        private ICollection<string> activeMeshGroups = new HashSet<string>();
        private ICollection<RenderableMesh> activeMeshRenderers = new HashSet<RenderableMesh>();

        public ModelSceneNode(Scene scene, Model model, string skin = null, bool loadAnimations = true)
            : base(scene)
        {
            Model = model;

            if (skin != null)
            {
                SetSkin(skin);
            }

            LoadMeshes();
            UpdateBoundingBox();

            // Load required resources
            if (loadAnimations)
            {
                LoadSkeletons();
                LoadAnimations();
            }
        }


        private bool richar_export_done = true; // est init a false par defaut
        private uint richar_updateCount = 0;

        public override void Update(Scene.UpdateContext context)
        {
            richar_updateCount++;


            // version de mon exporter.  a incrémenter si beosin
            // 101 : les animations etaient a 10 FPS
            // 102 : les animations sont maintenant a 60 FPS
            // 103 : correction d'un probleme de comptage de frame
            const int versionExportRichard = 103;


            if (activeAnimation == null)
            {
                // apres qq update, on export l'amation si besoin
                if (richar_updateCount == 10)
                {
                    // le 4ieme argument est le nom de l'animation a exporter
                    string[] args = Environment.GetCommandLineArgs();
                    if (args.Length >= 4)
                    {
                        string animName = args[3];
                        bool successSetAnim = SetAnimation(animName);
                        int aaa = 0;

                        if (successSetAnim)
                        {
                            richar_export_done = false; // on active l'export
                        }
                        else
                        {
                            string nameStrAnimationForFile = animName;
                            nameStrAnimationForFile = nameStrAnimationForFile.Replace(" ", "_");
                            //nameStrAnimationForFile = nameStrAnimationForFile.Replace("@", "_");
                            nameStrAnimationForFile = nameStrAnimationForFile.Replace("\\", "_");
                            nameStrAnimationForFile = nameStrAnimationForFile.Replace("/", "_");
                            nameStrAnimationForFile = nameStrAnimationForFile.Replace(":", "_");
                            nameStrAnimationForFile = nameStrAnimationForFile.Replace("*", "_");
                            nameStrAnimationForFile = nameStrAnimationForFile.Replace("?", "_");
                            nameStrAnimationForFile = nameStrAnimationForFile.Replace("\"", "_");
                            nameStrAnimationForFile = nameStrAnimationForFile.Replace("<", "_");
                            nameStrAnimationForFile = nameStrAnimationForFile.Replace(">", "_");
                            nameStrAnimationForFile = nameStrAnimationForFile.Replace("|", "_");

                            string richard_model_name = Model.Data.GetProperty<string>("m_name");
                            System.IO.BinaryWriter richard_writer2 = new System.IO.BinaryWriter(System.IO.File.Open(richard_model_name + ".anim." + nameStrAnimationForFile, System.IO.FileMode.Create));
                            richard_writer2.Write("BAD_ANIM");
                            richard_writer2.Write((UInt32)versionExportRichard);
                            richard_writer2.Close();

                            System.Windows.Forms.Application.Exit(); // richard , je FERME l'application

                        }
                    }
                }


                return;
            }

            AnimationController.Update(context.Timestep);



            float richa_dureeAnimation_seconde = 0.0f;
            System.IO.BinaryWriter richard_writer = null;


            // c'est ici que je regle le FPS avec lequel je veux exporter l'animation
            // 60 je pense que c'est pas mal. ca devrait faire des animations de bonne qualité
            // a noter que si l'animation de base est en 30 FPS et que je fais un export a 60, les frame intermediaires devraient etre
            // correctement interopollées.
            const int richa_fps = 60;  

            if (!richar_export_done)
            {
                string nameStrAnimationForFile = activeAnimation.Name;
                nameStrAnimationForFile = nameStrAnimationForFile.Replace(" ", "_");
                //nameStrAnimationForFile = nameStrAnimationForFile.Replace("@", "_");
                nameStrAnimationForFile = nameStrAnimationForFile.Replace("\\", "_");
                nameStrAnimationForFile = nameStrAnimationForFile.Replace("/", "_");
                nameStrAnimationForFile = nameStrAnimationForFile.Replace(":", "_");
                nameStrAnimationForFile = nameStrAnimationForFile.Replace("*", "_");
                nameStrAnimationForFile = nameStrAnimationForFile.Replace("?", "_");
                nameStrAnimationForFile = nameStrAnimationForFile.Replace("\"", "_");
                nameStrAnimationForFile = nameStrAnimationForFile.Replace("<", "_");
                nameStrAnimationForFile = nameStrAnimationForFile.Replace(">", "_");
                nameStrAnimationForFile = nameStrAnimationForFile.Replace("|", "_");

                string richard_model_name = Model.Data.GetProperty<string>("m_name");
                string fullnammee = richard_model_name + ".anim." + nameStrAnimationForFile;

                string pathhhhh = System.IO.Path.GetDirectoryName(richard_model_name);
                System.IO.Directory.CreateDirectory(pathhhhh); // creer folder si jamais il n'existe pas

                richard_writer = new System.IO.BinaryWriter(System.IO.File.Open(fullnammee, System.IO.FileMode.Create));
       


                richard_writer.Write("UPDATE_SKEL_BEG");


                richard_writer.Write((UInt32)versionExportRichard);

                // a la frame 0 (premiere frame)     il a fait 0 * 1/FPS secondes
                // a la frame 1                      il a fait 1 * 1/FPS secondes
                // a la frame X                      il a fait X * 1/FPS secondes
                richa_dureeAnimation_seconde = ((float)activeAnimation.FrameCount-1.0f) / activeAnimation.Fps;

                richard_writer.Write((UInt32)skeletons.Length); // number of skeleton
                richard_writer.Write((UInt32)activeAnimation.FrameCount);
                richard_writer.Write(activeAnimation.Fps); // FPS de l'animation originale
                richard_writer.Write(richa_fps); // FPS de MON export

                // ecrire un end-terminated-string (  pas la meme chose que  richard_writer.Write(string)  qui va ecrire  taille+string.  mais taille je comprends pas bien comment c'est geré   )
                for (int ic = 0; ic < activeAnimation.Name.Length; ic++)
                    richard_writer.Write(activeAnimation.Name[ic]);
                richard_writer.Write((Byte)0);

                //time = 0;
                AnimationController.Time = 0;
            }

            
            for (int frameCount = 0; ; frameCount++)
            {

                if (!richar_export_done)
                {
                    richard_writer.Write("TIME_BEG");
                    richard_writer.Write(AnimationController.Time); // time of the current animation

                }


                for (var i = 0; i < skeletons.Length; i++)
                {
                    var skeleton = skeletons[i];
                    var animationTexture = animationTextures[i];

                    if (!richar_export_done)
                    {
                        richard_writer.Write((UInt32)skeleton.AnimationTextureSize);
                    }

                    // Update animation matrices
                    var animationMatrices = new float[skeleton.AnimationTextureSize * 16];
                    for (var j = 0; j < skeleton.AnimationTextureSize; j++)
                    {
                        // Default to identity matrices
                        animationMatrices[j * 16] = 1.0f;
                        animationMatrices[(j * 16) + 5] = 1.0f;
                        animationMatrices[(j * 16) + 10] = 1.0f;
                        animationMatrices[(j * 16) + 15] = 1.0f;
                    }

                    animationMatrices = activeAnimation.GetAnimationMatricesAsArray(AnimationController.Time, skeleton);


                    if (!richar_export_done)
                    {
                        // si je comprends bien, la texture fait  4*AnimationTextureSize  pxl.
                        // 1 pxl = 4 float.
                        // on a donc  AnimationTextureSize*16 floats.
                        for (var kk = 0; kk < skeleton.AnimationTextureSize * 16; kk++)
                        {
                            richard_writer.Write(animationMatrices[kk]);
                        }
                    }

                    // Update animation texture
                    GL.BindTexture(TextureTarget.Texture2D, animationTexture);
                    GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba32f, 4, skeleton.AnimationTextureSize, 0, PixelFormat.Rgba, PixelType.Float, animationMatrices);
                    GL.BindTexture(TextureTarget.Texture2D, 0);
                }


                if (!richar_export_done)
                {

                    AnimationController.Time += 1.0f / (float)richa_fps;


                    // on s'arrete quand le temps qu'on vient de save a dépassé la durée de l'anim
                    // je pense qu'il faut s'arreter dès que le time depasse, pour eviter d'expoter une frame "fausse"
                    if (!richar_export_done)
                    {
                        if (AnimationController.Time > richa_dureeAnimation_seconde)
                            break;
                    }

                }
                else
                {
                    break;
                }
            }

            if (!richar_export_done)
            {
                richard_writer.Write("UPDATE_SKEL_END");
                richard_writer.Close();
                richar_export_done = true;
          
               System.Windows.Forms.Application.Exit();  // richard , je FERME l'application

            }
            




        }

        public override void Render(Scene.RenderContext context)
        {
            // This node does not render itself; it uses the batching system via IRenderableMeshCollection
        }

        public override IEnumerable<string> GetSupportedRenderModes()
            => meshRenderers.SelectMany(renderer => renderer.GetSupportedRenderModes()).Distinct();

        public override void SetRenderMode(string renderMode)
        {
            foreach (var renderer in meshRenderers)
            {
                renderer.SetRenderMode(renderMode);
            }
        }

        public void SetSkin(string skin)
        {
            var materialGroups = Model.Data.GetArray<IKeyValueCollection>("m_materialGroups");
            string[] defaultMaterials = null;

            foreach (var materialGroup in materialGroups)
            {
                // "The first item needs to match the default materials on the model"
                if (defaultMaterials == null)
                {
                    defaultMaterials = materialGroup.GetArray<string>("m_materials");
                }

                if (materialGroup.GetProperty<string>("m_name") == skin)
                {
                    var materials = materialGroup.GetArray<string>("m_materials");

                    skinMaterials = new Dictionary<string, string>();

                    for (var i = 0; i < defaultMaterials.Length; i++)
                    {
                        skinMaterials[defaultMaterials[i]] = materials[i];
                    }

                    break;
                }
            }

            if (meshRenderers.Count > 0)
            {
                foreach (var mesh in meshRenderers)
                {
                    mesh.SetSkin(skinMaterials);
                }
            }
        }

        public static  string ReverseString(string srtVarable)
        {
            return new string(srtVarable.Reverse().ToArray());
        }


        private void LoadMeshes()
        {
            bool modeExport = false;
            string[] args = Environment.GetCommandLineArgs();

            // le 4ieme argument est pour exporter une animation, exemple "snark_idle_sleeping".
            // s'il est present, je pense que c'est mieux de ne pas exporter le model.
            // comme ca, un run de VRF.exe =  1 seul export.
            // ca evite la confusion
            if (args.Length == 3)
            {
                modeExport = true ;
            }

            // "models/props_combine/combine_lockers/combine_locker01.vmdl"
            // "models/props/alyx_hideout/bug_jar01a.vmdl"
            string richard_model_name = Model.Data.GetProperty<string>("m_name");

            if (richard_model_name == "models/props/alyx_hideout/bug_jar01a.vmdl")
            {
                int a = 0;
            }

            if (richard_model_name == "models/creatures/snark/snark.vmdl")
            {
                int a = 0;
            }
            if (richard_model_name == "models/props/coffee_box.vmdl")
            {
                int a = 0;
            }

            System.IO.BinaryWriter richard_writer = null;
            if (modeExport)
            {
                
                

                // remove file name
                string richard_filename = "";
                string richard_directory = "";
                int step = 0;
                for (int i = richard_model_name.Length-1; ; i-- )
                {

                    if ( i == -1 )
                    {
                        break;
                    }

                    if (step == 0 && richard_model_name[i] == '/')
                    {
                        step++;
                    }
                    if (step==0)
                        richard_filename += richard_model_name[i];
                    else
                        richard_directory += richard_model_name[i];
                }

                richard_filename = ReverseString(richard_filename);
                richard_directory = ReverseString(richard_directory);

                Directory.CreateDirectory(richard_directory);

                

            

                // richard  -  write vertex buffer
                richard_writer = new System.IO.BinaryWriter(System.IO.File.Open(richard_model_name + ".ri", System.IO.FileMode.Create));

                richard_writer.Write("MESH_BEG");
                richard_writer.Write((UInt32)7); // version de mon exporter.  a incrémenter si beosin

            }


            // Get embedded meshes
            foreach (var embeddedMesh in Model.GetEmbeddedMeshesAndLoD().Where(m => (m.LoDMask & 1) != 0))
            {
                
                meshRenderers.Add(new RenderableMesh(richard_writer, embeddedMesh.Mesh, Scene.GuiContext, skinMaterials));
            }

            // Load referred meshes from file (only load meshes with LoD 1)
            var referredMeshesAndLoDs = Model.GetReferenceMeshNamesAndLoD();
            foreach (var refMesh in referredMeshesAndLoDs.Where(m => (m.LoDMask & 1) != 0))
            {
                var newResource = Scene.GuiContext.LoadFileByAnyMeansNecessary(refMesh.MeshName + "_c");
                if (newResource == null)
                {
                    continue;
                }

                meshRenderers.Add(new RenderableMesh(richard_writer, new Mesh(newResource), Scene.GuiContext, skinMaterials));

            }

            // Set active meshes to default
            SetActiveMeshGroups(Model.GetDefaultMeshGroups());

            // s'il y a que 3 arguments ( sous entendu, s'il y a pas le 4ieme argument qui defini le nom de l'animation a exporter )
            // alors on peut quitter maintenant
            if (args.Length == 3)
            {
                richard_writer.Write("MESH_END");
                richard_writer.Close();

                // richard , je FERME l'application
                System.Windows.Forms.Application.Exit();
            }

        }

        private void LoadSkeletons()
        {
            skeletons = meshRenderers.Select((_, i) => Model.GetSkeleton(i)).ToArray();
        }

        private void SetupAnimationTextures()
        {
            if (animationTextures == default)
            {
                // Create animation texture for each mesh
                animationTextures = new int[meshRenderers.Count];
                for (var i = 0; i < meshRenderers.Count; i++)
                {
                    var animationTexture = GL.GenTexture();
                    GL.BindTexture(TextureTarget.Texture2D, animationTexture);
                    // Set clamping to edges
                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
                    // Set nearest-neighbor sampling since we don't want to interpolate matrix rows
                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMinFilter.Nearest);
                    //Unbind texture again
                    GL.BindTexture(TextureTarget.Texture2D, 0);

                    animationTextures[i] = animationTexture;
                }
            }
        }

        private void LoadAnimations()
        {
            var animGroupPaths = Model.GetReferencedAnimationGroupNames();
            var emebeddedAnims = Model.GetEmbeddedAnimations();

            if (!animGroupPaths.Any() && !emebeddedAnims.Any())
            {
                return;
            }

            SetupAnimationTextures();

            // Load animations from referenced animation groups
            foreach (var animGroupPath in animGroupPaths)
            {
                var animGroup = Scene.GuiContext.LoadFileByAnyMeansNecessary(animGroupPath + "_c");
                if (animGroup != default)
                {
                    animations.AddRange(AnimationGroupLoader.LoadAnimationGroup(animGroup, Scene.GuiContext));
                }
            }

            // Get embedded animations
            animations.AddRange(emebeddedAnims);
        }

        public void LoadAnimation(string animationName)
        {
            var animGroupPaths = Model.GetReferencedAnimationGroupNames();
            var embeddedAnims = Model.GetEmbeddedAnimations();

            if (!animGroupPaths.Any() && !embeddedAnims.Any())
            {
                return;
            }

            if (skeletons == default)
            {
                LoadSkeletons();
                SetupAnimationTextures();
            }

            // Get embedded animations
            var embeddedAnim = embeddedAnims.FirstOrDefault(a => a.Name == animationName);
            if (embeddedAnim != default)
            {
                animations.Add(embeddedAnim);
                return;
            }

            // Load animations from referenced animation groups
            foreach (var animGroupPath in animGroupPaths)
            {
                var animGroup = Scene.GuiContext.LoadFileByAnyMeansNecessary(animGroupPath + "_c");
                var foundAnimations = AnimationGroupLoader.TryLoadSingleAnimationFileFromGroup(animGroup, animationName, Scene.GuiContext);
                if (foundAnimations != default)
                {
                    animations.AddRange(foundAnimations);
                    return;
                }
            }
        }

        public IEnumerable<string> GetSupportedAnimationNames()
            => animations.Select(a => a.Name);

        public bool SetAnimation(string animationName)
        {
            activeAnimation = animations.FirstOrDefault(a => a.Name == animationName);
            AnimationController.SetAnimation(activeAnimation);

            if (activeAnimation != default)
            {
                for (var i = 0; i < meshRenderers.Count; i++)
                {
                    meshRenderers[i].SetAnimationTexture(animationTextures[i], skeletons[i].AnimationTextureSize);
                }
                return true; // fail set anim
            }
            else
            {
                foreach (var renderer in meshRenderers)
                {
                    renderer.SetAnimationTexture(null, 0);
                }
                return false; // fail set anim
            }
        }

        public IEnumerable<string> GetMeshGroups()
            => Model.GetMeshGroups();

        public ICollection<string> GetActiveMeshGroups()
            => activeMeshGroups;

        public void SetActiveMeshGroups(IEnumerable<string> meshGroups)
        {
            activeMeshGroups = new HashSet<string>(GetMeshGroups().Intersect(meshGroups));

            var groups = GetMeshGroups();
            if (groups.Count() > 1)
            {
                activeMeshRenderers.Clear();
                foreach (var group in activeMeshGroups)
                {
                    var meshMask = Model.GetActiveMeshMaskForGroup(group).ToArray();
                    for (var meshIndex = 0; meshIndex < meshRenderers.Count; meshIndex++)
                    {
                        if (meshMask[meshIndex] && !activeMeshRenderers.Contains(meshRenderers[meshIndex]))
                        {
                            activeMeshRenderers.Add(meshRenderers[meshIndex]);
                        }
                    }
                }
            }
            else
            {
                activeMeshRenderers = new HashSet<RenderableMesh>(meshRenderers);
            }
        }

        private void UpdateBoundingBox()
        {
            bool first = true;
            foreach (var mesh in meshRenderers)
            {
                LocalBoundingBox = first ? mesh.BoundingBox : BoundingBox.Union(mesh.BoundingBox);
                first = false;
            }
        }
    }
}
