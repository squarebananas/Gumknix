using System;
using System.Threading.Tasks;
using Gum.Converters;
using Gum.DataTypes;
using Gum.Forms.Controls;
using MonoGameGum;

#if BLAZORGL
using nkast.Wasm.File;
using nkast.Wasm.FileSystem;
using nkast.Wasm.Url;
#endif

namespace Gumknix
{
    public class ContextMenuFile : MenuItem
    {
        public BaseSystemVisual SystemVisual;

        public bool IsClosed {get; private set; }

        public ContextMenuFile(BaseSystemVisual systemVisual) : base()
        {
            SystemVisual = systemVisual;
            Header = "";
            X = GumService.Default.Cursor.X;
            Visual.XUnits = GeneralUnitType.PixelsFromSmall;
            Y = GumService.Default.Cursor.Y;
            Visual.YUnits = GeneralUnitType.PixelsFromSmall;
        }

        public void ShowMenu(float width = 180f)
        {
            for (int i = 0; i < Items.Count; i++)
            {
                MenuItem item = Items[i] as MenuItem;
                item.Visual.Width = width;
                item.Visual.WidthUnits = DimensionUnitType.Absolute;
            }

            this.AddToRoot();
            IsFocused = true;
            IsSelected = true;
            Dock(Gum.Wireframe.Dock.SizeToChildren);
        }

        public void AddOptionOpen(ControlsFileViewer fileViewer, FileSystemItem fileSystemItem,
            bool treeItem = false, bool folderItem = false)
        {
            MenuItem optionOpen = new();
            optionOpen.Header = "Open";
            Items.Add(optionOpen);
            optionOpen.Clicked += (s, e) =>
            {
                if (treeItem)
                    fileViewer.OnTreeItemClick(fileSystemItem);
                else if (folderItem)
                    fileViewer.OnFolderItemOpenClick(fileSystemItem);
                CloseMenu();
            };
        }

        public void AddOptionOpen(DesktopIcon desktopIcon)
        {
            MenuItem optionOpen = new();
            optionOpen.Header = "Open";
            Items.Add(optionOpen);
            optionOpen.Clicked += (s, e) =>
            {
                Gumknix gumknix = SystemVisual.GumknixInstance;
                if (desktopIcon.IconType == DesktopIcon.IconTypes.Program)
                    gumknix.StartApplet(desktopIcon.AppletType);
                else
                    gumknix.StartApplet(desktopIcon.FileSystemItem.GetDefaultAppletInfo().AppletType, [desktopIcon.FileSystemItem]);
                CloseMenu();
            };
        }

        public void AddOptionOpenWith(FileSystemItem fileSystemItem)
        {
            MenuItem optionOpenWith = new();
            optionOpenWith.Header = "Open With...";
            Items.Add(optionOpenWith);

            Type defaultAppletType = fileSystemItem.GetDefaultAppletInfo().AppletType;
            for (int i = 0; i < SystemVisual.GumknixInstance.AvailableApplets.Count; i++)
            {
                Type appletType = SystemVisual.GumknixInstance.AvailableApplets[i];
                MenuItem appletItem = new();
                appletItem.Header = BaseApplet.GetDefaultTitle(appletType);
                appletItem.Visual.Width = 220;
                appletItem.Visual.WidthUnits = DimensionUnitType.Absolute;
                appletItem.Clicked += (s, e) =>
                {
                    SystemVisual.GumknixInstance.StartApplet(appletType, [fileSystemItem]);
                    CloseMenu();
                };
                if ((appletType == defaultAppletType) && (optionOpenWith.Items.Count >= 1))
                    optionOpenWith.Items.Insert(0, appletItem);
                else
                    optionOpenWith.Items.Add(appletItem);
            }
        }

        public void AddOptionCut(FileSystemItem fileSystemItem)
        {
            MenuItem optionCut = new();
            optionCut.Header = "Cut";
            Items.Add(optionCut);
            optionCut.Clicked += (s, e) =>
            {
                Gumknix gumknix = SystemVisual.GumknixInstance;
                gumknix.ClipboardItems.Clear();
                gumknix.ClipboardItems.Add(fileSystemItem);
                gumknix.ClipboardIsCut = true;
                CloseMenu();
            };
        }

        public void AddOptionCopy(FileSystemItem fileSystemItem)
        {
            MenuItem optionCopy = new();
            optionCopy.Header = "Copy";
            Items.Add(optionCopy);
            optionCopy.Clicked += (s, e) =>
            {
                Gumknix gumknix = SystemVisual.GumknixInstance;
                gumknix.ClipboardItems.Clear();
                gumknix.ClipboardItems.Add(fileSystemItem);
                gumknix.ClipboardIsCut = false;
                CloseMenu();
            };
        }

        public void AddOptionPaste(FileSystemItem fileSystemItem)
        {
            MenuItem optionPaste = new();
            optionPaste.Header = "Paste";
            optionPaste.IsEnabled = SystemVisual.GumknixInstance.ClipboardItems.Count >= 1;
            Items.Add(optionPaste);
            optionPaste.Clicked += async (s, e) =>
            {
#if BLAZORGL
                Gumknix gumknix = SystemVisual.GumknixInstance;
                if ((SystemVisual == gumknix.Desktop) && (gumknix.Desktop.CursorOverDesktopIcon() == null))
                    gumknix.Desktop.DropGridSlot = gumknix.Desktop.CursorOverGridSlot();

                for (int i = 0; i < gumknix.ClipboardItems.Count; i++)
                {
                    FileSystemItem clipboardItem = gumknix.ClipboardItems[i] as FileSystemItem;
                    if (clipboardItem == null)
                        continue;

                    string newName = clipboardItem.Name;
                    bool alreadyExists = await fileSystemItem.Exists(clipboardItem.Name);
                    if (alreadyExists)
                    {
                        if (gumknix.ClipboardIsCut)
                        {
                            bool overwriteConfirm = await FileSystemItem.ShowOverwriteFile(SystemVisual, clipboardItem);
                            if (!overwriteConfirm)
                                continue;
                        }
                        else
                        {
                            string nameWithoutExtension = System.IO.Path.GetFileNameWithoutExtension(newName);
                            newName = fileSystemItem.GetAvailableName(nameWithoutExtension + " - Copy", clipboardItem.Extension);
                        }
                    }

                    switch (clipboardItem.Type)
                    {
                        case FileSystemItem.Types.Directory:
                            await clipboardItem.CopyDirectory(fileSystemItem, newName, preserveOriginal: gumknix.ClipboardIsCut == false);
                            break;
                        case FileSystemItem.Types.File:
                            await clipboardItem.CopyFile(fileSystemItem, newName, preserveOriginal: gumknix.ClipboardIsCut == false);
                            break;
                    }
                }

                if (gumknix.ClipboardIsCut) // todo if successful
                    gumknix.ClipboardItems.Clear();

                CloseMenu();
#endif
            };
        }

        public void AddOptionDownload(FileSystemItem fileSystemItem)
        {
            MenuItem optionDownload = new();
            optionDownload.Header = "Download";
            Items.Add(optionDownload);
            optionDownload.Clicked += (s, e) =>
            {
#if BLAZORGL
                FileSystemFileHandle fileSystemFileHandle = fileSystemItem.Handle as FileSystemFileHandle;
                fileSystemFileHandle.GetFile().ContinueWith(t =>
                {
                    if (t.IsCompletedSuccessfully && t.Result != null)
                    {
                        nkast.Wasm.File.File file = t.Result;
                        string url = Url.CreateObjectURL(file);
                        Url.DownloadFromURL(url, file.Name);
                    }
                });
#endif
                CloseMenu();
            };
        }

        public void AddOptionRename(DesktopIcon desktopIcon)
        {
            MenuItem optionRename = new();
            optionRename.Header = "Rename";
            Items.Add(optionRename);
            optionRename.Clicked += (s, e) =>
            {
                SystemVisual.GumknixInstance.Desktop.RenameTextEntry(desktopIcon);
                CloseMenu();
            };
        }

        public void AddOptionRename(ControlsFileViewer fileViewer, FileSystemItem fileSystemItem)
        {
            MenuItem optionRename = new();
            optionRename.Header = "Rename";
            Items.Add(optionRename);
            optionRename.Clicked += (s, e) =>
            {
                fileViewer.RenameTextEntry(fileSystemItem);
                CloseMenu();
            };
        }

        public void AddOptionDelete(FileSystemItem fileSystemItem)
        {
            MenuItem optionDelete = new();
            optionDelete.Header = "Delete";
            Items.Add(optionDelete);
            optionDelete.Clicked += (s, e) =>
            {
                Task deleteTask = fileSystemItem.Delete();
                CloseMenu();
            };

            string path = fileSystemItem.Path;
            Gumknix gumknix = SystemVisual.GumknixInstance;

            if ((path == gumknix.RootStorage.Path) ||
                (path == gumknix.DesktopStorage.Path) ||
                (path == gumknix.DocumentsStorage.Path) ||
                (path == gumknix.AppletFilesStorage.Path) ||
                (path == gumknix.AppletUserDataStorage.Path) ||
                (path == gumknix.SystemStorage.Path) ||
                (path == gumknix.RecycleBinStorage.Path))
                optionDelete.IsEnabled = false;
        }

        public void AddOptionForgetShortcut(ControlsFileViewer fileViewer, FileSystemItem fileSystemItem)
        {
            MenuItem optionForgetShortcut = new();
            optionForgetShortcut.Header = "Forget Shortcut";
            Items.Add(optionForgetShortcut);
            optionForgetShortcut.Clicked += (s, e) =>
            {
                fileViewer.ForgetShortcut(fileSystemItem);
                CloseMenu();
            };
        }

        public void AddOptionProperties(FileSystemItem fileSystemItem)
        {
            MenuItem optionProperties = new();
            optionProperties.Header = "Properties";
            Items.Add(optionProperties);
            optionProperties.IsEnabled = fileSystemItem.Handle != null;
            optionProperties.Clicked += (s, e) =>
            {
                SystemVisual.GumknixInstance.StartApplet(typeof(AppletFileProperties), [fileSystemItem]);
                CloseMenu();
            };
        }

        public void AddOptionNewFolder(FileSystemItem fileSystemItem)
        {
            MenuItem optionNewFolder = new();
            optionNewFolder.Header = "New Folder";
            Items.Add(optionNewFolder);
            optionNewFolder.Clicked += (s, e) =>
            {
                string newFolderName = fileSystemItem.GetAvailableName("New Folder", "");

#if BLAZORGL
                FileSystemDirectoryHandle directoryHandle = fileSystemItem.Handle as FileSystemDirectoryHandle;
                Task<FileSystemDirectoryHandle> createTask = directoryHandle.GetDirectoryHandle(newFolderName, true);
#endif

                CloseMenu();
            };
        }

        public void AddOptionNewFile(FileSystemItem fileSystemItem, string description, string extension)
        {
            MenuItem optionNewFile = new();
            optionNewFile.Header = $"New {description}";
            Items.Add(optionNewFile);
            optionNewFile.Clicked += (s, e) =>
            {
                string newFileName = fileSystemItem.GetAvailableName(optionNewFile.Header, extension);

#if BLAZORGL
                FileSystemDirectoryHandle directoryHandle = fileSystemItem.Handle as FileSystemDirectoryHandle;
                Task<FileSystemFileHandle> createTask = directoryHandle.GetFileHandle(newFileName, true);
#endif

                CloseMenu();
            };
        }

        public void CloseMenu()
        {
            HidePopupRecursively();
            for (int i = 0; i < Items.Count; i++)
                (Items[i] as MenuItem).RemoveFromRoot();
            Visual.Children.Clear();
            Visual.RemoveFromRoot();
            this.RemoveFromRoot();
            Close();
            IsClosed = true;
        }
    }
}
