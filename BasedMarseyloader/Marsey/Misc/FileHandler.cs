using System.ComponentModel;
using System.IO;
using System.Reflection;
using Marsey.Config;
using Marsey.Game.Resources;
using Marsey.Game.Resources.Dumper.Resource;
using Marsey.PatchAssembly;
using Marsey.Patches;
using Marsey.Stealthsey;
using Marsey.Subversion;

namespace Marsey.Misc;

/// <summary>
/// Handles file operations in the patch folder
/// </summary>
public abstract class FileHandler
{
    /// <summary>
    /// Prepare data about enabled mods to send to the loader
    /// </summary>
public static async Task PrepareMods(string[]? path = null)
{
    path ??= new[] { MarseyVars.MarseyFolder };
    string[] patchPath = new[] { MarseyVars.MarseyPatchFolder };
    string[] resPath = new[] { MarseyVars.MarseyResourceFolder };

    List<MarseyPatch> marseyPatches = Marsyfier.GetMarseyPatches();
    List<SubverterPatch> subverterPatches = Subverter.GetSubverterPatches();
    List<ResourcePack> resourcePacks = ResMan.GetRPacks();

    IPC.Server server = new();

    // Prepare preloading MarseyPatches
    List<string> preloadpaths = marseyPatches
        .Where(p => p is { Enabled: true, Preload: true })
        .Select(p => p.Asmpath)
        .ToList();

    // Send preloading MarseyPatches through named pipe
    string preloadData = string.Join(",", preloadpaths);
    Task preloadTask = server.ReadySend("PreloadMarseyPatchesPipe", preloadData);

    // If we actually do have any - remove them from the marseypatch list
    if (preloadpaths.Count != 0)
    {
        marseyPatches.RemoveAll(p => preloadpaths.Contains(p.Asmpath));
    }

    // Prepare remaining MarseyPatches
    List<string> marseyAsmpaths = marseyPatches.Where(p => p.Enabled).Select(p => p.Asmpath).ToList();
    string marseyData = string.Join(",", marseyAsmpaths);
    Task marseyTask = server.ReadySend("MarseyPatchesPipe", marseyData);

    // Prepare SubverterPatches
    List<string> subverterAsmpaths = subverterPatches.Where(p => p.Enabled).Select(p => p.Asmpath).ToList();
    string subverterData = string.Join(",", subverterAsmpaths);
    Task subverterTask = server.ReadySend("SubverterPatchesPipe", subverterData);

#if DEBUG
    // Prepare ResourcePacks
    List<string> rpackPaths = resourcePacks.Where(rp => rp.Enabled).Select(rp => rp.Dir).ToList();
    string rpackData = string.Join(",", rpackPaths);
    Task resourceTask = server.ReadySend("ResourcePacksPipe", rpackData);

    // Wait for all tasks to complete
    await Task.WhenAll(preloadTask, marseyTask, subverterTask, resourceTask);
#else
    // Wait for all tasks to complete
    await Task.WhenAll(preloadTask, marseyTask, subverterTask);
#endif
}



    /// <summary>
    /// Loads assemblies from a specified folder.
    /// </summary>
    /// <param name="path">folder with patch dll's, set to "Marsey" by default</param>
    /// <param name="pipe">Are we loading from an IPC pipe</param>
    /// <param name="pipename">Name of an IPC pipe to load the patches from</param>
    public static void LoadAssemblies(string[]? path = null, bool pipe = false, string pipename = "MarseyPatchesPipe")
    {
        path ??= new[] { MarseyVars.MarseyPatchFolder };

        if (!pipe)
        {
            PatchListManager.RecheckPatches();
        }

        List<string> files = pipe ? GetFilesFromPipe(pipename) : GetPatches(path);

        foreach (string file in files)
        {
            MarseyLogger.Log(MarseyLogger.LogType.DEBG, $"Loading assembly from {file}");
            LoadExactAssembly(file, pipe);
            //LoadExactAssembly(file, lockup: false); // BREAKS PATCHES
        }
    }

    /// <summary>
    /// Retrieve a list of patch filepaths from pipe
    /// </summary>
    /// <param name="name">Name of the pipe</param>
    public static List<string> GetFilesFromPipe(string name)
    {
        IPC.Client client = new();
        string data = client.ConnRecv(name);

        return string.IsNullOrEmpty(data) ? new List<string>() : data.Split(',').ToList();
    }

#if DEBUG
    public static bool ByteArrayToFile(string fileName, byte[] byteArray)
    {
        try
        {
            using (var fs = new FileStream(fileName, FileMode.Create, FileAccess.Write))
            {
                fs.Write(byteArray, 0, byteArray.Length);
                return true;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Exception caught in process: {0}", ex);
            return false;
        }
    }
#endif
    public static void TryLinuxToDotnet(ref byte[] data)
    {
        // TODO: read from license.txt
        byte[] linux_header = new byte[] {
    0x98, 0x56, 0x21, 0xf6, 0x87, 0x31, 0x8c, 0xf8, 0xe4, 0xb8, 0x0, 0x8f, 0xef, 0xb8, 0xd9, 0x1f,
    0xc1, 0x42, 0x60, 0xfd, 0xf0, 0x37, 0x60, 0x7d, 0x64, 0x2f, 0x54, 0x14, 0xd2, 0xfc, 0xbe, 0x15,
    0xdf, 0xbf, 0x3b, 0xf2, 0xe3, 0x11, 0xe2, 0x70, 0xa4, 0x37, 0x73, 0x41, 0xfd, 0xd8, 0x2, 0x7f,
    0x33, 0x5a, 0xf0, 0x8c, 0x48, 0x55, 0x2d, 0xd4, 0x8d, 0xd4, 0x15, 0x45, 0x85, 0xd8, 0x23, 0x94,
    0x7, 0x61, 0xfd, 0x64, 0xb3, 0xe9, 0x9f, 0x8c, 0x3e, 0xcd, 0x5b, 0xb, 0x2, 0x5, 0x2, 0xd5,
    0x15, 0x4b, 0x82, 0x31, 0x61, 0xbb, 0x41, 0xaa, 0x8c, 0x7d, 0x9f, 0x54, 0x51, 0x54, 0x46, 0x19,
    0x18, 0xa, 0xd4, 0xf8, 0xe6, 0x84, 0xb1, 0xff, 0xf0, 0x1d, 0xed, 0x8, 0x62, 0xb9, 0xe2, 0xca,
    0x80, 0x2e, 0xa9, 0xb6, 0x5a, 0xb8, 0xbd, 0x96, 0xb, 0xf2, 0x5a, 0xe1, 0xd2, 0xe3, 0xa3, 0xb1,
    0xe0, 0x8f, 0xd, 0x8b, 0x23, 0x65, 0xa8, 0xb7, 0x3b, 0xc1, 0x2f, 0xd0, 0x6b, 0xb6, 0x96, 0x9e,
    0x8e, 0xa3, 0xd3, 0x9, 0xf3, 0xda, 0x20, 0x2a, 0x53, 0x87, 0x68, 0x2f, 0xf2, 0xec, 0x59, 0x85,
    0x97, 0x5, 0xbc, 0x47, 0xc9, 0x76, 0x73, 0x9f, 0x23, 0xa2, 0x2, 0x39, 0x69, 0x38, 0xc0, 0x88,
    0xd5, 0x7e, 0x22, 0xfc, 0x31, 0x7c, 0x16, 0xcd, 0xbd, 0x6c, 0x4f, 0x3d, 0x31, 0x83, 0xaa, 0x3a,
    0x3f, 0x1e, 0xa, 0x8e, 0xc9, 0x7a, 0xdb, 0xcc, 0xfb, 0x9a, 0x7d, 0x78, 0xe2, 0x83, 0xcc, 0xd4,
    0x7f, 0x92, 0x79, 0x9d, 0x90, 0x37, 0xb6, 0x7f, 0x6d, 0x9f, 0x60, 0x8a, 0x32, 0x45, 0xed, 0x42,
    0x1b, 0x16, 0xd1, 0x42, 0xa6, 0x14, 0x6a, 0x21, 0xb7, 0x51, 0x6a, 0x4e, 0x65, 0xcd, 0x45, 0x7e,
    0xa9, 0x79, 0x6f, 0x31, 0xc8, 0x51, 0x9d, 0x6f, 0x42, 0xd, 0x63, 0xbf, 0x3f, 0x87, 0x6a, 0xac,
        };
        IEnumerable<Byte> as_magic = data.Take(4); 
        IEnumerable<Byte> linux_magic = new byte[] { 0xDE, 0xAA, 0xFF, 0xEA }.ToArray();
        if (!as_magic.SequenceEqual(linux_magic))
        {
            return;
        }
        data = data.Skip(4).ToArray();
        int hdr_idx = 0;
        for (int i = 0; i<data.Length; i++)
        {
            if (hdr_idx == linux_header.Length)
                hdr_idx = 0;
            data[i] ^= linux_header[hdr_idx];
            hdr_idx++;
        }
    }

    /// <summary>
    /// Loads an assembly from the specified file path and initializes it.
    /// </summary>
    /// <param name="file">The file path of the assembly to load.</param>
    /// <param name="lockup">Load from the dll directly instead of reading from file</param>
    public static void LoadExactAssembly(string file, bool lockup = false)
    {
        Redial.Disable(); // Disable any AssemblyLoad callbacks found

        try
        {
            if (lockup)
            {
                Assembly assembly = Assembly.LoadFrom(file);
                AssemblyInitializer.Initialize(assembly, assembly.Location);
            }
            else
            {
                byte[] assemblyData = File.ReadAllBytes(file);
                //TryLinuxToDotnet(ref assemblyData);
#if DEBUG
                FileHandler.ByteArrayToFile(file + ".dec", assemblyData);
#endif
                Assembly assembly = Assembly.Load(assemblyData);
                AssemblyInitializer.Initialize(assembly, file);
            }
        }
        catch (FileNotFoundException)
        {
            MarseyLogger.Log(MarseyLogger.LogType.DEBG, $"{file} could not be found");
        }
        catch (PatchAssemblyException ex)
        {
            MarseyLogger.Log(MarseyLogger.LogType.FATL, ex.Message);
        }
        catch (Exception ex) // Catch any other exceptions that may occur
        {
            MarseyLogger.Log(MarseyLogger.LogType.FATL, $"An unexpected error occurred while loading {file}: {ex.Message}");
        }
        finally
        {
            Redial.Enable(); // Enable callbacks in case the game needs them
        }
    }

    /// <summary>
    /// Retrieves the file paths of all DLL files in a specified subdirectory
    /// </summary>
    /// <param name="subdir">An array of strings representing the path to the subdirectory</param>
    /// <returns>An array of strings containing the full paths to each DLL file in the specified subdirectory</returns>
    public static List<string> GetPatches(string[] subdir)
    {
        try
        {
            string[] updatedSubdir = subdir.Prepend(Directory.GetCurrentDirectory()).ToArray();
            string path = Path.Combine(updatedSubdir);

            if (Directory.Exists(path))
            {
                return Directory.GetFiles(path, "*.dll").ToList();
            }

            MarseyLogger.Log(MarseyLogger.LogType.DEBG, $"Directory {path} does not exist");
            return [];
        }
        catch (Exception ex)
        {
            MarseyLogger.Log(MarseyLogger.LogType.FATL, $"Failed to find patches: {ex.Message}");
            return [];
        }
    }

    /// <summary>
    /// Saves assembly from stream to file.
    /// </summary>
    public static void SaveAssembly(string path, string name, Stream asmStream)
    {
        Directory.CreateDirectory(path);

        string fullpath = Path.Combine(path, name);

        using FileStream st = new FileStream(fullpath, FileMode.Create, FileAccess.Write);
        asmStream.CopyTo(st);
    }

    /// <see cref="ResDumpPatches"/>
    public static void CheckRenameDirectory(string path)
    {
        // GetParent once shows itself, GetParent twice shows the actual parent

        if (string.IsNullOrEmpty(path) || !Directory.Exists(path)) return;

        string dirName = Path.GetFileName(path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        string? parentDir = Directory.GetParent(Directory.GetParent(path)?.FullName ?? throw new InvalidOperationException())?.FullName;
        MarseyLogger.Log(MarseyLogger.LogType.DEBG, "FileHandler", $"Parentdir is {parentDir}");

        if (string.IsNullOrEmpty(dirName) || string.IsNullOrEmpty(parentDir)) return;

        string newPath = Path.Combine(parentDir, $"{dirName}{DateTime.Now:yyyyMMddHHmmss}");

        if (Directory.Exists(newPath))
        {
            MarseyLogger.Log(MarseyLogger.LogType.ERRO, "FileHandler",
                $"Cannot move directory. Destination {newPath} already exists.");
            return;
        }

        MarseyLogger.Log(MarseyLogger.LogType.DEBG, "FileHandler", $"Trying to move {path} to {newPath}");
        // Completely evil, do not try-catch this - if it fails - it fails and kills everything.
        Directory.Move(path, newPath);
    }

    /// <see cref="ResDumpPatches"/>
    public static void CreateDir(string filePath)
    {
        string? directoryName = Path.GetDirectoryName(filePath);
        if (directoryName != null && !Directory.Exists(directoryName))
        {
            Directory.CreateDirectory(directoryName);
        }
    }

    /// <see cref="ResDumpPatches"/>
    public static void SaveToFile(string filePath, MemoryStream stream)
    {
        using FileStream st = new FileStream(filePath, FileMode.Create, FileAccess.Write);
        MarseyLogger.Log(MarseyLogger.LogType.DEBG, $"Saving to {filePath}");
        stream.WriteTo(st);
    }
}
