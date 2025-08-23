using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MonoGameGum;

#if BLAZORGL
using nkast.Wasm.File;
using nkast.Wasm.FileSystem;
#endif

namespace Gumknix
{
    public class FileSystemItem
    {
        public delegate void OnFileChangedDelegate(object sender);
        public event OnFileChangedDelegate OnFileChanged;

        public string Name { get; private set; } = "";

#if BLAZORGL
        public FileSystemHandle Handle { get; private set; }
#else
        public object Handle { get; private set; }
#endif

        public FileSystemItem Parent { get; private set; }
        public List<FileSystemItem> Children { get; private set; }

        public long Size { get; private set; }
        public DateTime LastModified { get; private set; }
        public bool OriginPrivateFileSystem { get; init; }
        public long? ReconnectId { get; init; }

        public enum Types
        {
            Directory,
            File
        }

        public Types Type
        {
            get
            {
#if BLAZORGL
                if (Handle is FileSystemDirectoryHandle)
                    return Types.Directory;
#endif
                return Types.File;
            }
        }

        public string Icon
        {
            get
            {
                if (Type == Types.Directory)
                    return "\uE651";

                string extension = System.IO.Path.GetExtension(Name)?.ToLowerInvariant();
                if (!string.IsNullOrEmpty(extension) &&
                    Gumknix.ExtensionsDefaultApplets.TryGetValue(extension, out DefaultAppletInfo defaultApplet))
                    return defaultApplet.Icon;

                return "\uE76F";
            }
        }

        public string Path
        {
            get
            {
                string path = Name;
                FileSystemItem item = this;
                while (item.Parent != null)
                {
                    item = item.Parent;
                    path = item.Name + '\\' + path;
                }
                return path;
            }
        }

        public FileSystemItem Root
        {
            get
            {
                FileSystemItem item = this;
                while (item.Parent != null)
                    item = item.Parent;
                return item;
            }
        }

        public string Extension
        {
            get
            {
                return System.IO.Path.GetExtension(Name).ToLowerInvariant();
            }
        }

        public FileListBoxItem FileListBoxItem { get; set; }
        public DesktopIcon DesktopIcon { get; set; }

        public enum TransferType
        {
            None,
            Copy,
            Move
        }

        public class FileStats
        {
            public int FileCount { get; private set; }
            public int DirectoryCount { get; private set; }
            public long TotalSize { get; private set; }
            public DateTime LastModified { get; private set; }
            public bool ValuesChanged { get; set; }

            public FileStats()
            {
            }

            public void Add(FileSystemItem item)
            {
                switch (item.Type)
                {
                    case Types.File:
                        FileCount++;
                        TotalSize += item.Size;
                        if (item.LastModified > LastModified)
                            LastModified = item.LastModified;
                        break;
                    case Types.Directory:
                        DirectoryCount++;
                        break;
                }
                ValuesChanged = true;
            }
        }

        public readonly struct DefaultAppletInfo(Type appletType, string icon)
        {
            public readonly Type AppletType = appletType;
            public readonly string Icon = icon;
        }

        public FileSystemItem(string name, FileSystemItem parent = null)
        {
            Name = name;
            Parent = parent;
            Children = [];
        }


        public FileSystemItem(FileSystemHandle handle, FileSystemItem parent, string alias = null)
        {
            Name = alias ?? handle.Name;
            Handle = handle;

            Parent = parent;
            Children = [];
            OriginPrivateFileSystem = Parent?.OriginPrivateFileSystem ?? false;

#if BLAZORGL

            if (Handle?.Kind == FileSystemHandleKind.File)
            {
                FileSystemFileHandle fileSystemFileHandle = Handle as FileSystemFileHandle;
                Task<File> fileTask = fileSystemFileHandle.GetFile();
                fileTask.ContinueWith(t =>
                {
                    if (t.IsCompletedSuccessfully && t.Result != null)
                    {
                        File file = t.Result;
                        SetFromFile(file);
                    }
                });
            }
#endif
        }

#if BLAZORGL
        public void SetFromFile(File file)
        {
            bool changed = (Name != file.Name) ||
                (LastModified != file.LastModified) ||
                (Size != file.Size);

            Name = file.Name;
            LastModified = file.LastModified;
            Size = file.Size;

            if (changed)
                OnFileChanged?.Invoke(this);
        }
#endif

        public bool Contains(string name)
        {
            for (int i = 0; i < Children.Count; i++)
                if (Children[i].Name == name)
                    return true;
            return false;
        }

        public async Task<bool> Exists(string newName)
        {
#if BLAZORGL
            FileSystemDirectoryHandle directoryHandle = Handle as FileSystemDirectoryHandle;
            FileSystemHandle itemHandle = null;
            try
            {
                itemHandle = await directoryHandle.GetFileHandle(newName, false);
            }
            catch(Exception e)
            {
            }
            return itemHandle != null;
#else
            return false;
#endif
        }

        public async Task<List<FileSystemItem>> GetAllParentsAsync(FileSystemItem root)
        {
            List<FileSystemItem> pathPartsList = [root];

#if BLAZORGL
            FileSystemDirectoryHandle handle = Handle as FileSystemDirectoryHandle;
            FileSystemDirectoryHandle rootHandle = root.Handle as FileSystemDirectoryHandle;
            if ((handle == null) || (rootHandle == null))
                return pathPartsList;

            bool sameDirectory = await handle.IsSameEntry(rootHandle);
            if (sameDirectory)
                return pathPartsList;

            string path = await rootHandle.Resolve(handle);
            string[] pathParts = path.Split('/', StringSplitOptions.None);

            for (int i = 0; i < pathParts.Length; i++)
            {
                string pathPart = pathParts[i];
                FileSystemItem pathPartFileSystemItem = pathPartsList[^1];
                FileSystemItem childItem = pathPartFileSystemItem.Children.FirstOrDefault(child => child.Name == pathPart);
                if (childItem?.Handle != null)
                {
                    pathPartsList.Add(childItem);
                    continue;
                }
                else
                {
                    FileSystemDirectoryHandle parentHandle = pathPartFileSystemItem.Handle as FileSystemDirectoryHandle;
                    FileSystemDirectoryHandle directoryHandle = await parentHandle.GetDirectoryHandle(pathPart, false);
                    FileSystemItem pathItem = new(directoryHandle, pathPartFileSystemItem);
                    pathPartFileSystemItem.AddChild(pathItem);
                    pathPartsList.Add(pathItem);
                }
            }
#endif

            return pathPartsList;
        }

        public async Task<List<FileSystemItem>> GetAllChildrenAsync(bool recursive,
            List<FileSystemItem> addToList = null, FileStats addToFileStats = null)
        {
            List<FileSystemItem> allChildren = addToList ?? [];

#if BLAZORGL
            FileSystemDirectoryHandle handle = Handle as FileSystemDirectoryHandle;
            if (handle == null)
                return allChildren;

            FileSystemHandleArray entries = await handle.GetEntries();
            for (int i = 0; i < entries.Count; i++)
            {
                FileSystemHandle entry = entries[i];
                FileSystemItem childItem = new(entry, this);
                AddChild(childItem);
                allChildren.Add(childItem);

                if (childItem.Type == Types.File)
                {
                    FileSystemFileHandle fileHandle = entry as FileSystemFileHandle;
                    File file = await fileHandle.GetFile();
                    childItem.SetFromFile(file);
                }

                addToFileStats?.Add(childItem);

                if (recursive && (childItem.Type == Types.Directory))
                    await childItem.GetAllChildrenAsync(true, allChildren, addToFileStats);
            }
#endif

            return allChildren;
        }

        public bool AddChild(FileSystemItem child)
        {
            if (Contains(child.Name))
                return false;

            Children.Add(child);
            return true;
        }

        public async Task CopyFile(FileSystemItem destinationDirectory = null, string newName = null, bool preserveOriginal = true)
        {
#if BLAZORGL
            FileSystemDirectoryHandle sourceDirectoryHandle = Parent?.Handle as FileSystemDirectoryHandle;
            FileSystemDirectoryHandle destinationDirectoryHandle = destinationDirectory?.Handle as FileSystemDirectoryHandle;

            newName ??= Name;
            destinationDirectoryHandle ??= sourceDirectoryHandle;

            if ((sourceDirectoryHandle == destinationDirectoryHandle) && (newName == Name))
                return;

            FileSystemFileHandle fileHandle = Handle as FileSystemFileHandle;
            Blob blob = await fileHandle.GetFile();
            FileSystemFileHandle newFileHandle = await destinationDirectoryHandle.GetFileHandle(newName, true);
            FileSystemWritableFileStream writableFileStream = await newFileHandle.CreateWritable();

            await writableFileStream.Write(blob);
            await writableFileStream.Truncate((ulong)blob.Size);
            await writableFileStream.Close();

            if (!preserveOriginal && (sourceDirectoryHandle != null))
                await sourceDirectoryHandle.RemoveEntry(fileHandle.Name, false);
#endif
        }

        public async Task CopyDirectory(FileSystemItem destinationDirectory = null, string newName = null, bool preserveOriginal = true)
        {
#if BLAZORGL
            FileSystemDirectoryHandle handle = Handle as FileSystemDirectoryHandle;
            FileSystemDirectoryHandle destinationDirectoryHandle = destinationDirectory?.Handle as FileSystemDirectoryHandle;

            newName ??= Name;
            destinationDirectoryHandle ??= Parent?.Handle as FileSystemDirectoryHandle;

            bool overwriteSameItem = (destinationDirectoryHandle == Parent?.Handle) && (newName == Name);
            if (overwriteSameItem && preserveOriginal)
                return;

            List<FileSystemItem> allChildren = await GetAllChildrenAsync(true);

            FileSystemDirectoryHandle newDirectoryHandle = await destinationDirectoryHandle.GetDirectoryHandle(newName, true);
            FileSystemItem newDirectoryItem = new(newDirectoryHandle, destinationDirectory);
            destinationDirectory.AddChild(newDirectoryItem);

            Dictionary<string, FileSystemItem> newDirectories = [];
            newDirectories.Add(Path, newDirectoryItem);

            for (int i = 0; i < allChildren.Count; i++)
            {
                FileSystemItem childItem = allChildren[i];
                FileSystemHandle childHandle = allChildren[i].Handle;
                FileSystemItem newParent = newDirectories[childItem.Parent.Path];

                switch (childHandle.Kind)
                {
                    case FileSystemHandleKind.Directory:
                        FileSystemDirectoryHandle newParentHandle = newParent.Handle as FileSystemDirectoryHandle;
                        FileSystemDirectoryHandle newSubdirectoryHandle = await newParentHandle.GetDirectoryHandle(childHandle.Name, true);
                        FileSystemItem newSubdirectoryItem = new(newSubdirectoryHandle, newParent);
                        newParent.AddChild(newSubdirectoryItem);
                        newDirectories.Add(childItem.Path, newSubdirectoryItem);
                        break;
                    case FileSystemHandleKind.File:
                        await childItem.CopyFile(newParent, childHandle.Name, preserveOriginal);
                        break;
                }
            }

            if (!preserveOriginal && !overwriteSameItem)
            {
                FileSystemDirectoryHandle sourceParentHandle = Parent.Handle as FileSystemDirectoryHandle;
                await sourceParentHandle.RemoveEntry(Handle.Name, true);
            }
#endif
        }

        public void Rename(string newName)
        {
#if BLAZORGL
            switch (Type)
            {
                case Types.Directory:
                    Task copyDirectoryTask = CopyDirectory(newName: newName, preserveOriginal: false);
                    break;
                case Types.File:
                    Task copyFileTask = CopyFile(Parent, newName: newName, preserveOriginal: false);
                    break;
            }
#endif
        }

        public string GetAvailableName(string defaultName, string extension)
        {
            string newFileName = $"{defaultName}{extension}";
            int newFileIndex = 1;
            for (int i = 0; i < Children.Count; i++)
            {
                if (Children[i].Name == newFileName)
                {
                    newFileIndex++;
                    newFileName = $"{defaultName} ({newFileIndex}){extension}";
                    i = 0;
                }
            }
            return newFileName;
        }

        public async Task Delete()
        {
#if BLAZORGL
            FileSystemDirectoryHandle parentHandle = Parent?.Handle as FileSystemDirectoryHandle;
            if (parentHandle == null)
                return;

            Parent.Children.Remove(this);

            await parentHandle.RemoveEntry(Name, true);
#endif
        }

        public TransferType DetermineTransferType(FileSystemItem destinationDirectory, bool userRequestedCopy, bool userRequestedMove)
        {
            bool sameRoot = Root.Path == destinationDirectory.Root.Path;
            bool sameFolder = Parent?.Path == destinationDirectory.Path;
            if (Type == Types.Directory)
                sameFolder |= Path == destinationDirectory.Path;

            bool move = sameRoot && !sameFolder;
            bool copy = !sameRoot;
            if (GumService.Default.Keyboard.IsCtrlDown)
            {
                move = false;
                copy = true;
            }
            if (GumService.Default.Keyboard.IsShiftDown && !sameFolder)
            {
                move = true;
                copy = false;
            }

            return move ? TransferType.Move : copy ? TransferType.Copy : TransferType.None;
        }

        public async Task FilesDropped(BaseSystemVisual systemVisual, List<FileSystemItem> fileSystemItems, bool userRequestedCopy, bool userRequestedMove)
        {
#if BLAZORGL
            await GetAllChildrenAsync(false);

            for (int i = 0; i < fileSystemItems.Count; i++)
            {
                FileSystemItem fileSystemItem = fileSystemItems[i];
                string newName = fileSystemItem.Name;

                bool sameRoot = fileSystemItem.Root.Path == Root.Path;
                bool sameFolder = fileSystemItem.Parent?.Path == Path;

                bool move = !sameFolder && (sameRoot || userRequestedMove);
                bool copy = !sameRoot || userRequestedCopy;
                if (copy)
                    move = false;

                bool alreadyExists = Contains(newName);
                if (alreadyExists)
                {
                    if (move)
                    {
                        bool overwriteConfirm = await ShowOverwriteFile(systemVisual, fileSystemItem);
                        if (!overwriteConfirm)
                            continue;
                    }
                    else if (copy)
                    {
                        string nameWithoutExtension = System.IO.Path.GetFileNameWithoutExtension(newName);
                        newName = GetAvailableName(nameWithoutExtension + " - Copy", fileSystemItem.Extension);
                    }
                    else
                    {
                        continue;
                    }
                }

                switch (fileSystemItem.Type)
                {
                    case Types.Directory:
                        await fileSystemItem.CopyDirectory(this, newName, preserveOriginal: !move);
                        break;
                    case Types.File:
                        await fileSystemItem.CopyFile(this, newName, preserveOriginal: !move);
                        break;
                }
            }
#endif
        }

        public static Task<bool> ShowOverwriteFile(BaseSystemVisual systemVisual, FileSystemItem file)
        {
            TaskCompletionSource<bool> taskCompletionSource = new();

            DialogMultiChoice multiChoice = new DialogMultiChoice(systemVisual, "Overwrite existing file?", "\uEE71",
               "The file " + file.Name + " already exists. Are you sure you want to replace it?",
               ["Yes", "No"]);
            multiChoice.OnChoiceSelected += (s, choiceSelected) =>
            {
                bool result = choiceSelected == 0;
                taskCompletionSource.TrySetResult(result);
                multiChoice.CloseRequest = true;
            };
            systemVisual.ShowDialog(multiChoice);

            return taskCompletionSource.Task;
        }

        public DefaultAppletInfo GetDefaultAppletInfo()
        {
            if (Type == Types.Directory)
                return new DefaultAppletInfo(typeof(AppletFileCabiKnit), AppletFileCabiKnit.DefaultIcon);

            string extension = System.IO.Path.GetExtension(Name)?.ToLowerInvariant();
            if (!string.IsNullOrEmpty(extension) &&
                Gumknix.ExtensionsDefaultApplets.TryGetValue(extension, out DefaultAppletInfo defaultAppletInfo))
                return defaultAppletInfo;

            return new();
        }

        public static string GetMetricSize(long size)
        {
            long bytesPerKiloByte = 1024;
            long bytesPerMegabyte = bytesPerKiloByte * bytesPerKiloByte;
            long bytesPerGigabyte = bytesPerMegabyte * bytesPerKiloByte;
            long bytesPerTerabyte = bytesPerGigabyte * bytesPerKiloByte;

            if (size < bytesPerKiloByte)
                return $"{size:N0} B";
            else if (size < bytesPerMegabyte)
                return $"{(size / bytesPerKiloByte):N0} KB";
            else if (size < bytesPerGigabyte)
                return $"{(size / bytesPerMegabyte):N0} MB";
            else if (size < bytesPerTerabyte)
                return $"{(size / bytesPerGigabyte):N0} GB";
            else
                return $"{(size / bytesPerTerabyte):N0} TB";
        }
    }
}
