using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.FileSystemGlobbing;

using nkast.Wasm.File;
using nkast.Wasm.FileSystem;

namespace Gumknix.KniSBuild
{
    class ProjectFile
    {
        public FileSystemItem FileSystemItem;

        public enum SdkTypes
        {
            None,
            DotNetSDK,
            DotNetSDKBlazorWebAssembly
        }

        public SdkTypes SdkType;

        public bool EnableDefaultItems;
        public bool EnableDefaultCompileItems;
        public string TargetFramework;
        public bool Nullable;
        public bool ImplicitUsings;
        public string ProjectGuid;
        public string RootNamespace;
        public string AssemblyName;

        public class Item
        {
            public enum ItemTypes
            {
                Compile,
                Content,
                EmbeddedResource,
                Import,
                PackageReference,
                ProjectReference,
                None
            }
            public ItemTypes ItemType;

            public string Include;
            public string IncludeAssets;
            public string Exclude;
            public string ExcludeAssets;
            public string Update;
            public string Remove;
            public string Label;
            public string Condition;
            public string CopyToOutputDirectory;
            public string Link;

            public string Version;
            public string PrivateAssets;

            public string Project;
        }

        public List<Item> AllItems = [];

        public class SearchPathInfo
        {
            public ProjectFile ProjectFile;
            public string Path;
            public string SearchPath;
            public FileSystemItem RootFileSystemItem;
            public bool GetFile;
            public bool GetFolder;
            public bool GetSubfolders;
            public string Extension;
        }

        public StringBuilder Log = new();

        public Dictionary<string, FileSystemItem> ProjectFiles = [];

        public ProjectFile()
        {
        }

        public static ProjectFile Parse(FileSystemItem fileSystemItem, string xml, string extension)
        {
            ProjectFile projectFile = new()
            {
                FileSystemItem = fileSystemItem
            };

            try
            {
                XDocument xmlDocument = XDocument.Parse(xml);
                XNamespace rootNamespace = xmlDocument.Root.Name.Namespace;

                XName GetRootedXName(string name)
                {
                    if (rootNamespace == null)
                        return name;
                    else
                        return rootNamespace + name;
                }

                IEnumerable<XElement> project = xmlDocument.Descendants(GetRootedXName("Project"));
                XAttribute sdk = project.Select(x => x.Attribute("Sdk")).LastOrDefault();

                projectFile.SdkType = sdk?.Value switch
                {
                    "Microsoft.NET.Sdk" => SdkTypes.DotNetSDK,
                    "Microsoft.NET.Sdk.BlazorWebAssembly" => SdkTypes.DotNetSDKBlazorWebAssembly,
                    _ => SdkTypes.None
                };

                IEnumerable<XElement> propertyGroup = xmlDocument.Descendants(GetRootedXName("PropertyGroup"));

                string enableDefaultItemsString = propertyGroup.Descendants("EnableDefaultItems").LastOrDefault()?.Value;
                projectFile.EnableDefaultItems = enableDefaultItemsString == "false" ? false : true;
                string enableDefaultCompileItemsString = propertyGroup.Descendants("EnableDefaultCompileItems").LastOrDefault()?.Value;
                projectFile.EnableDefaultCompileItems = enableDefaultCompileItemsString == "false" ? false : true;

                XElement targetFramework = propertyGroup.Descendants("TargetFramework").LastOrDefault();
                XElement targetFrameworks = propertyGroup.Descendants("TargetFrameworks").LastOrDefault();
                projectFile.TargetFramework = targetFramework?.Value ?? targetFrameworks?.Value;

                IEnumerable<XElement> itemGroup = xmlDocument.Descendants( GetRootedXName( "ItemGroup"));

                projectFile.AllItems = [];

                IEnumerable<XElement> combinedItems = [];

                string[] itemTypeNames = Enum.GetNames(typeof(Item.ItemTypes));
                for (int i = 0; i < itemTypeNames.Length; i++)
                {
                    IEnumerable<XElement> itemTypeElements = itemGroup.Descendants(GetRootedXName(itemTypeNames[i]));
                    combinedItems = combinedItems.Concat(itemTypeElements);
                }

                IEnumerable<XElement> import = xmlDocument.Descendants(GetRootedXName("Import"));
                combinedItems = combinedItems.Concat(import);

                foreach (XElement item in combinedItems)
                {
                    Item newItem = new();

                    Item.ItemTypes itemType = (Item.ItemTypes)Enum.Parse(typeof(Item.ItemTypes), item.Name.LocalName);
                    newItem.ItemType = itemType;

                    string HandleMSBuildText(string text)
                    {
                        if (string.IsNullOrEmpty(text))
                            return text;
                        text = text.Replace("$(MSBuildThisFileDirectory)", "");
                        return text;
                    }

                    newItem.Include = HandleMSBuildText(item.Attribute("Include")?.Value);
                    newItem.Exclude = HandleMSBuildText(item.Attribute("Exclude")?.Value);
                    newItem.Update = HandleMSBuildText(item.Attribute("Update")?.Value);
                    newItem.Remove = HandleMSBuildText(item.Attribute("Remove")?.Value);
                    newItem.Label = HandleMSBuildText(item.Attribute("Label")?.Value);
                    newItem.Condition = HandleMSBuildText(item.Attribute("Condition")?.Value);
                    newItem.CopyToOutputDirectory = item.Attribute("CopyToOutputDirectory")?.Value;

                    newItem.Link = item.Element("Link")?.Value;

                    newItem.Project = item.Attribute("Project")?.Value;

                    projectFile.AllItems.Add(newItem);
                }
            }
            catch (Exception e)
            {
            }
            return projectFile;
        }

        public async Task ResolveFiles()
        {
            HashSet<SearchPathInfo> searchPaths = [];

            await AddSearchPathsFromItems(searchPaths);

            ProjectFile projectFile = null;
            ProjectFiles = [];
            ProjectFiles.Add(FileSystemItem.Name, FileSystemItem);

            foreach (SearchPathInfo searchPathInfo in searchPaths)
            {
                if (projectFile != searchPathInfo.ProjectFile)
                {
                    projectFile = searchPathInfo.ProjectFile;
                    ProjectFiles.TryAdd(searchPathInfo.ProjectFile.FileSystemItem.Name, searchPathInfo.ProjectFile.FileSystemItem);
                }

                FileSystemItem root = searchPathInfo.RootFileSystemItem;

                if (searchPathInfo.SearchPath == string.Empty)
                    searchPathInfo.SearchPath = root.Path;

                if (searchPathInfo.GetFile)
                {
                    FileSystemItem fileSystemItem = await root.GetChildAsync(searchPathInfo.Path);
                    if (fileSystemItem != null)
                    {
                        string absolutePath = Path.Combine(root.Path, searchPathInfo.Path).Replace("/", "\\");
                        ProjectFiles.TryAdd(absolutePath, fileSystemItem);
                    }
                }
                if (searchPathInfo.GetFolder || searchPathInfo.GetSubfolders)
                {
                    string path = Path.Combine(root.Path, searchPathInfo.Path);

                    List<FileSystemItem> fileSystemItems = await root.GetAllChildrenAsync(searchPathInfo.GetSubfolders);
                    for (int i = 0; i < fileSystemItems.Count; i++)
                    {
                        string itemPath = fileSystemItems[i].Path;
                        if (itemPath.EndsWith(searchPathInfo.Extension) == false)
                            continue;
                        if (itemPath.Contains("\\bin\\"))
                            continue;
                        if (itemPath.Contains("\\obj\\"))
                            continue;
                        ProjectFiles.TryAdd(fileSystemItems[i].Path, fileSystemItems[i]);
                    }
                }
            }

            foreach (KeyValuePair<string, FileSystemItem> file in ProjectFiles)
            {
                Log.AppendLine(file.Value.Path);
            }
        }

        public async Task AddSearchPathsFromItems(HashSet<SearchPathInfo> searchPaths)
        {
            if (EnableDefaultItems || EnableDefaultCompileItems)
            {
                SearchPathInfo defaultCompileFolders = new();
                defaultCompileFolders.ProjectFile = this;
                defaultCompileFolders.Path = defaultCompileFolders.SearchPath = FileSystemItem.Parent.Path;
                defaultCompileFolders.RootFileSystemItem = FileSystemItem.Parent;
                defaultCompileFolders.GetFolder = true;
                defaultCompileFolders.GetSubfolders = true;
                defaultCompileFolders.Extension = ".cs";
                searchPaths.Add(defaultCompileFolders);
            }

            for (int i = 0; i < AllItems.Count; i++)
            {
                Item item = AllItems[i];

                if ((item.ItemType == Item.ItemTypes.Compile) &&
                    (item.Include?.Length >= 1))
                {
                    string path = item.Include;
                    SearchPathInfo pathRequest = GetPathRequest(path, ".cs");
                    pathRequest.ProjectFile = this;
                    pathRequest.RootFileSystemItem = FileSystemItem.Parent;
                    searchPaths.Add(pathRequest);
                }

                if (((item.ItemType == Item.ItemTypes.Import) && (item.Project?.EndsWith(".projitems") == true)) ||
                    ((item.ItemType == Item.ItemTypes.ProjectReference) && (item.Include?.EndsWith(".csproj") == true)))
                {
                    string path = item.Project ?? item.Include;

                    if (path.Contains("kni\\Platforms\\Kni.Platform") ||
                        path.Contains("MonoGame.Framework\\MonoGame.Framework"))
                        continue;

                    ProjectFile sharedProject = await GetSharedProject(item.Project ?? item.Include);
                    await sharedProject.AddSearchPathsFromItems(searchPaths);
                }
            }
        }

        public SearchPathInfo GetPathRequest(string path, string extension)
        {
            path = path.Replace("\\", "/");
            if (path.EndsWith("**/*.*"))
            {
                return new() { Path = path, SearchPath = path[..^5], GetFolder = true, GetSubfolders = true };
            }
            else if (path.EndsWith("*.*"))
            {
                return new() { Path = path, SearchPath = path[..^3], GetFolder = true };
            }
            else if (path.EndsWith($"*{extension}"))
            {
                return new() { Path = path, SearchPath = path[..^(1 + extension.Length)], GetFolder = true };
            }
            else
            {
                return new() { Path = path, SearchPath = Path.GetDirectoryName(path), GetFile = true };
            }
        }

        public async Task<ProjectFile> GetSharedProject(string sharedProjectPath)
        {
            List<string> pathParts = sharedProjectPath.Split('\\').ToList();
            FileSystemItem fileSystemItem = FileSystemItem.Parent;
            for (int i = 0; i < pathParts.Count; i++)
            {
                if (pathParts[i] == "..")
                    fileSystemItem = fileSystemItem.Parent;
                else
                    fileSystemItem = await fileSystemItem.GetChildAsync(pathParts[i]);
            }

#if BLAZORGL
            FileSystemFileHandle fileSystemFileHandle = fileSystemItem.Handle as FileSystemFileHandle;
            Blob blob = await fileSystemFileHandle.GetFile();
            string xml = await blob.Text();

            ProjectFile project = Parse(fileSystemItem, xml, fileSystemItem.Extension);
#endif
            return project;
        }
    }
}
