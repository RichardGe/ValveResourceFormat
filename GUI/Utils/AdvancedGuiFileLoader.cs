//#define DEBUG_FILE_LOAD

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SteamDatabase.ValvePak;
using ValveKeyValue;
using ValveResourceFormat;
using ValveResourceFormat.IO;

namespace GUI.Utils
{
    public class AdvancedGuiFileLoader : IFileLoader
    {
        private static readonly Dictionary<string, Package> CachedPackages = new Dictionary<string, Package>();
        private readonly List<Package> CurrentGamePackages = new List<Package>();
        private readonly Dictionary<string, Resource> CachedResources = new Dictionary<string, Resource>();
        private readonly VrfGuiContext GuiContext;
        private bool GamePackagesScanned;

        public AdvancedGuiFileLoader(VrfGuiContext guiContext)
        {
            GuiContext = guiContext;
        }

        public void ClearCache()
        {
            foreach (var resource in CachedResources.Values)
            {
                resource.Dispose();
            }

            CachedResources.Clear();
        }

        public Resource LoadFile(string file)
        {
            // TODO: Might conflict where same file name is available in different paths
            if (CachedResources.TryGetValue(file, out var resource) && resource.Reader != null)
            {
                return resource;
            }

            resource = new Resource
            {
                FileName = file,
            };

            var entry = GuiContext.CurrentPackage?.FindEntry(file);

            if (entry != null)
            {
#if DEBUG_FILE_LOAD
                Console.WriteLine($"Loaded \"{file}\" from current vpk");
#endif

                GuiContext.CurrentPackage.ReadEntry(entry, out var output, false);
                resource.Read(new MemoryStream(output));
                CachedResources[file] = resource;

                return resource;
            }

            if (GuiContext.ParentFileLoader != null)
            {
                return GuiContext.ParentFileLoader.LoadFile(file);
            }

            if (!GamePackagesScanned)
            {
                GamePackagesScanned = true;
                FindAndLoadSearchPaths();
            }

            var paths = Settings.Config.GameSearchPaths.ToList();
            var packages = CurrentGamePackages.ToList();

            foreach (var searchPath in paths.Where(searchPath => searchPath.EndsWith(".vpk")).ToList())
            {
                paths.Remove(searchPath);

                if (!CachedPackages.TryGetValue(searchPath, out var package))
                {
                    Console.WriteLine($"Preloading vpk \"{searchPath}\"");

                    package = new Package();
                    package.Read(searchPath);
                    CachedPackages[searchPath] = package;
                }

                packages.Add(package);
            }

            if (GuiContext.CurrentPackage != null && GuiContext.CurrentPackage.Entries.ContainsKey("vpk"))
            {
                foreach (var searchPath in GuiContext.CurrentPackage.Entries["vpk"])
                {
                    if (!CachedPackages.TryGetValue(searchPath.GetFileName(), out var package))
                    {
                        Console.WriteLine($"Preloading vpk from parent vpk \"{searchPath}\"");

                        GuiContext.CurrentPackage.ReadEntry(searchPath, out var vpk, false);
                        var ms = new MemoryStream(vpk);
                        package = new Package();
                        package.SetFileName(searchPath.GetFileName());
                        package.Read(ms);
                        CachedPackages[searchPath.GetFileName()] = package;
                    }

                    packages.Add(package);
                }
            }

            foreach (var package in packages)
            {
                entry = package?.FindEntry(file);

                if (entry != null)
                {
#if DEBUG_FILE_LOAD
                    Console.WriteLine($"Loaded \"{file}\" from preloaded vpk \"{package.FileName}\"");
#endif

                    package.ReadEntry(entry, out var output, false);
                    resource.Read(new MemoryStream(output));
                    CachedResources[file] = resource;

                    return resource;
                }
            }

            var path = FindResourcePath(paths, file, GuiContext.FileName);

            if (path == null)
            {
                Console.Error.WriteLine($"Failed to load \"{file}\". Did you configure VPK paths in settings correctly?");

                return null;
            }

            resource.Read(path);
            CachedResources[file] = resource;

            return resource;
        }

        private void FindAndLoadSearchPaths()
        {
            var gameinfoPath = GetCurrentGameInfoPath();

            if (gameinfoPath == null)
            {
                return;
            }

            var folders = new List<string>();
            var rootFolder = Path.GetDirectoryName(Path.GetDirectoryName(gameinfoPath));
            KVObject gameInfo;

            using (var stream = new FileStream(gameinfoPath, FileMode.Open, FileAccess.Read))
            {
                try
                {
                    gameInfo = KVSerializer.Create(KVSerializationFormat.KeyValues1Text).Deserialize(stream);
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine(e);
                    return;
                }
            }

            Console.WriteLine($"Found \"{gameInfo["game"]}\" from \"{gameinfoPath}\"");

            foreach (var searchPath in (IEnumerable<KVObject>)gameInfo["FileSystem"]["SearchPaths"])
            {
                if (searchPath.Name != "Game")
                {
                    continue;
                }

                folders.Add(Path.Combine(rootFolder, searchPath.Value.ToString()));
            }

            foreach (var folder in folders)
            {
                // Scan for vpks in folder, same logic as in source engine
                for (var i = 1; i < 99; i++)
                {
                    var vpk = Path.Combine(folder, $"pak{i:D2}_dir.vpk");

                    if (!File.Exists(vpk))
                    {
                        break;
                    }

                    if (GuiContext.FileName == vpk)
                    {
#if DEBUG_FILE_LOAD
                        Console.WriteLine($"VPK \"{vpk}\" is the same we just opened, skipping");
#endif
                        continue;
                    }

                    if (Settings.Config.GameSearchPaths.Contains(vpk))
                    {
#if DEBUG_FILE_LOAD
                        Console.WriteLine($"VPK \"{vpk}\" is already user-defined, skipping");
#endif
                        continue;
                    }

                    Console.WriteLine($"Preloading vpk \"{vpk}\"");

                    var package = new Package();
                    package.Read(vpk);
                    CurrentGamePackages.Add(package);
                }
            }
        }

        private string GetCurrentGameInfoPath()
        {
            var directory = GuiContext.FileName;
            var i = 10;

            while (i-- > 0)
            {
                directory = Path.GetDirectoryName(directory);

#if DEBUG_FILE_LOAD
                Console.WriteLine($"Scanning \"{directory}\"");
#endif

                if (directory == null || Path.GetFileName(directory) == "steamapps")
                {
                    return null;
                }

                var gameinfoPath = Path.Combine(directory, "gameinfo.gi");

                if (File.Exists(gameinfoPath))
                {
                    return gameinfoPath;
                }
            }

            return null;
        }

        private static string FindResourcePath(IList<string> paths, string file, string currentFullPath = null)
        {
            if (currentFullPath != null)
            {
                paths = paths.OrderByDescending(x => currentFullPath.StartsWith(x, StringComparison.Ordinal)).ToList();
            }

            foreach (var searchPath in paths)
            {
                var path = Path.Combine(searchPath, file);
                path = Path.GetFullPath(path);

                if (File.Exists(path))
                {
#if DEBUG_FILE_LOAD
                    Console.WriteLine($"Loaded \"{file}\" from disk: \"{path}\"");
#endif

                    return path;
                }
            }

            return null;
        }
    }
}
