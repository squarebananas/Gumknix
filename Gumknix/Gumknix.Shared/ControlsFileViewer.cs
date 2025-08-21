using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Gum.Converters;
using Gum.DataTypes;
using Gum.Forms.Controls;
using Gum.Wireframe;
using MonoGameGum;
using MonoGameGum.GueDeriving;
using RenderingLibrary;
using RenderingLibrary.Graphics;

#if BLAZORGL
using nkast.Wasm.Dom;
using nkast.Wasm.File;
using nkast.Wasm.FileSystem;
using nkast.Wasm.FileSystemAccess;
using nkast.Wasm.Storage;
#endif

namespace Gumknix
{
    public class ControlsFileViewer
    {
        public delegate void OnFileOpenRequestDelegate(object sender, List<FileSystemItem> files);
        public event OnFileOpenRequestDelegate OnFileOpenRequest;

        public object Parent { get; private set; }
        private BaseSystemVisual _systemVisual => BaseSystemVisual.GetSystemVisual(Parent);
        public Gumknix _gumknix => _systemVisual.GumknixInstance;

        private StackPanel _stackPanel;

        private Panel _navigationPanel;
        private StackPanel _navigationBar;
        private List<Button> _navigationPathParts;
        private List<float> _navigationPathPartsWidths;
        private float _navigationBarLastWidth;

        private ListBox _treeListBox;
        private Splitter _splitter;
        private ListBox _folderListBox;

        private ContextMenuFile _contextMenu;

        private TextBox _renameTextBox;
        private bool _renameTextBoxActivated;

        private FileSystemItem _draggingFileItem;
        private Vector2 _draggingFileInitialPosition;
        private Label _draggingFileIcon;
        private Label _draggingFileDetails;

        public FileSystemItem SelectedTreeItem { get; private set; }
        public List<FileSystemItem> SelectedPath { get; private set; }
        public FileSystemItem SelectedFolder { get; private set; }

        public FileSystemItem SelectedFileSystemItem { get; set; } // todo private
        public List<FileSystemItem> FolderFileSystemItems { get; set; }
        public List<string> FolderExtensionFilters { get; set; }

#if BLAZORGL
        private StorageManager storageManager;
        private FileSystemObserver fileSystemObserver;

        private FileSystemDirectoryHandle AppletUserDataDirectory; // todo
        private FileSystemFileHandle AppletSettingsFile;
#endif

        private static Dictionary<long, string> ConnectedFolders = [];

        public ControlsFileViewer(object parent, StackPanel mainStackPanel)
        {
            Parent = parent;

            _navigationPanel = new();
            _navigationPanel.Visual.Dock(Dock.FillHorizontally);
            _navigationPanel.Visual.Anchor(Anchor.TopLeft);
            _navigationPanel.Visual.Height = 32;
            _navigationPanel.Visual.HeightUnits = DimensionUnitType.Absolute;
            _navigationPanel.Visual.StackSpacing = 0;
            mainStackPanel.AddChild(_navigationPanel);

            ColoredRectangleRuntime navigationBarBackground = new();
            navigationBarBackground.Color = new Color(32, 32, 32);
            navigationBarBackground.Dock(Dock.Fill);
            navigationBarBackground.Anchor(Anchor.TopLeft);
            _navigationPanel.AddChild(navigationBarBackground);

            _navigationBar = new();
            _navigationBar.Orientation = Orientation.Horizontal;
            _navigationBar.Visual.Dock(Dock.FillHorizontally);
            _navigationBar.Visual.Anchor(Anchor.TopLeft);
            _navigationBar.Visual.Height = 32;
            _navigationBar.Visual.HeightUnits = DimensionUnitType.Absolute;
            _navigationBar.Visual.StackSpacing = 0;
            _navigationBar.Visual.WrapsChildren = true;
            _navigationPanel.AddChild(_navigationBar);

            TextRuntime separator = new();
            separator.Text = ">";
            separator.X = 5 + ((_navigationBar.Visual.Children.Count == 0) ? 7 : 0);
            separator.Y = 6;
            _navigationBar.Visual.AddChild(separator);

            _navigationPathParts = [];
            _navigationPathPartsWidths = [];

            SelectedPath = [];

            _stackPanel = new();
            _stackPanel.Orientation = Orientation.Horizontal;
            _stackPanel.Visual.Dock(Dock.Fill);
            _stackPanel.Visual.Anchor(Anchor.TopLeft);
            _stackPanel.Visual.Height -= (Parent is DialogFilePicker) ? 144 : 64;
            mainStackPanel.AddChild(_stackPanel);

            _treeListBox = new();
            _treeListBox.Visual.Dock(Dock.FillVertically);
            _treeListBox.Visual.Anchor(Anchor.TopLeft);
            _treeListBox.Visual.Width = 265;
            _treeListBox.Visual.WidthUnits = DimensionUnitType.Absolute;
            _treeListBox.ListBoxItemFormsType = typeof(FileListBoxItem);
            _treeListBox.ItemClicked += (s, e) =>
            {
                FileSystemItem cursorOverTreeItem = CursorOverListBoxItem(_treeListBox);
                if (cursorOverTreeItem != null)
                    OnTreeItemClick(cursorOverTreeItem);
            };
            _stackPanel.AddChild(_treeListBox);

            AddTreeItem(_gumknix.RootStorage);
            AddTreeItem(new FileSystemItem("Connect To Folder"));

            _splitter = new Splitter();
            _splitter.Dock(Dock.FillVertically);
            _splitter.Width = 5;
            _stackPanel.AddChild(_splitter);

            _folderListBox = new();
            _folderListBox.Visual.Dock(Dock.FillVertically);
            _folderListBox.Visual.Anchor(Anchor.TopLeft);
            _folderListBox.Visual.Width = -270;
            _folderListBox.Visual.WidthUnits = DimensionUnitType.RelativeToParent;
            _folderListBox.ListBoxItemFormsType = typeof(FileListBoxItem);
            _stackPanel.AddChild(_folderListBox);

            FolderFileSystemItems = [];
            FolderExtensionFilters = [];

            LoadSettings();
        }

        public void Update(Layer layer)
        {
            bool cursorOverTreeList = _treeListBox.GetVisual("Background").HasCursorOver(GumService.Default.Cursor.X, GumService.Default.Cursor.Y);
            bool cursorOverFolderList = _folderListBox.GetVisual("Background").HasCursorOver(GumService.Default.Cursor.X, GumService.Default.Cursor.Y);
            FileSystemItem cursorOverTreeItem = CursorOverListBoxItem(_treeListBox);
            FileSystemItem cursorOverFolderItem = CursorOverListBoxItem(_folderListBox);

            float navigationBarWidth = _navigationBar.Visual.GetAbsoluteWidth();
            if (navigationBarWidth != _navigationBarLastWidth)
            {
                float measuredWidth = 0;
                int navigationButtonsShown = 0;
                for (int i = _navigationPathPartsWidths.Count - 1; i >= 0; i--)
                {
                    measuredWidth += _navigationPathPartsWidths[i];
                    if (measuredWidth < (navigationBarWidth - 10))
                        navigationButtonsShown++;
                    else
                        break;
                }
                int navigationFirstButtonIndex = _navigationPathParts.Count - navigationButtonsShown;
                for (int i = 0; i < _navigationPathParts.Count; i++)
                {
                    bool visible = i >= navigationFirstButtonIndex;
                    _navigationBar.Visual.Children[(i * 2) + 0].Visible = visible;
                    _navigationBar.Visual.Children[(i * 2) + 1].Visible = visible;
                    (_navigationBar.Visual.Children[(i * 2) + 0] as TextRuntime).Text =
                        ((navigationFirstButtonIndex == 0) || (i != navigationFirstButtonIndex)) ? ">" : " ...";
                }
                _navigationBarLastWidth = navigationBarWidth;
            }

            if ((GumService.Default.Cursor.PrimaryPush || GumService.Default.Cursor.PrimaryDoubleClick) &&
                (cursorOverFolderItem != null))
            {
                SelectedFileSystemItem = cursorOverFolderItem;
                if (GumService.Default.Cursor.PrimaryDoubleClick)
                    OnFolderItemOpenClick(SelectedFileSystemItem);
            }

            if (GumService.Default.Cursor.SecondaryClick && (_contextMenu == null) &&
                (_gumknix.ObjectUnderCursor() == Parent))
            {
#if BLAZORGL
                if (cursorOverTreeList)
                {
                    FileSystemItem highlightedTreeItem = null;
                    for (int i = 0; i < _treeListBox.Items.Count; i++)
                    {
                        FileSystemItem treeItem = _treeListBox.Items[i] as FileSystemItem;
                        if (treeItem.FileListBoxItem.IsHighlighted)
                        {
                            highlightedTreeItem = treeItem;
                            break;
                        }
                    }

                    if (highlightedTreeItem != null)
                        SetContextMenuTreeItem(highlightedTreeItem);
                }
                else if (cursorOverFolderList && (SelectedFolder != null))
                {
                    SelectedFileSystemItem = null;
                    for (int i = 0; i < _folderListBox.Items.Count; i++)
                    {
                        FileSystemItem fileSystemItem = _folderListBox.Items[i] as FileSystemItem;
                        if (fileSystemItem.FileListBoxItem.IsHighlighted)
                        {
                            SelectedFileSystemItem = fileSystemItem;
                            _folderListBox.SelectedIndex = i;
                            break;
                        }
                    }
                    SetContextMenuFolderItem();
                }
#endif
            }
            else if ((_contextMenu != null) &&
                ((GumService.Default.Cursor.PrimaryClick) || (GumService.Default.Cursor.SecondaryClick)))
            {
                bool keepOpen = false;
                for (int i = 0; i < _contextMenu.Items.Count; i++)
                {
                    MenuItem itemToCheck = _contextMenu.Items[i] as MenuItem;
                    if (itemToCheck.Visual.HasCursorOver(GumService.Default.Cursor.X, GumService.Default.Cursor.Y))
                    {
                        if (itemToCheck.Items.Count >= 1)
                        {
                            keepOpen = true;
                            break;
                        }
                    }
                }
                if (!keepOpen)
                    _contextMenu.CloseMenu();
            }
            if (_contextMenu?.IsClosed == true)
                _contextMenu = null;

            if ((_renameTextBox != null) && !_renameTextBoxActivated &&
                GumService.Default.Cursor.PrimaryDown == false)
            {
                _renameTextBox.IsFocused = true;
                _renameTextBoxActivated = true;
            }

            if ((_draggingFileItem == null) && _contextMenu == null && _renameTextBox == null &&
                (_gumknix.ObjectUnderCursor() == Parent))
            {
                if (GumService.Default.Cursor.PrimaryPush && (cursorOverFolderItem != null))
                {
                    _draggingFileItem = cursorOverFolderItem;
                    _draggingFileInitialPosition = new Vector2(GumService.Default.Cursor.X, GumService.Default.Cursor.Y);
                }
            }

            if (_draggingFileItem != null)
            {
                Vector2 cursorPosition = new Vector2(GumService.Default.Cursor.X, GumService.Default.Cursor.Y);
                if ((_draggingFileIcon == null) &&
                    (Vector2.DistanceSquared(cursorPosition, _draggingFileInitialPosition) > 100f))
                {
                    _draggingFileIcon = new();
                    _draggingFileIcon.Text = _draggingFileItem.FileListBoxItem.Icon.Text;
                    _draggingFileIcon.Visual.Font = "FluentSymbolSet";
                    _draggingFileIcon.Visual.FontSize = 48;
                    _draggingFileIcon.Visual.XOrigin = HorizontalAlignment.Center;
                    _draggingFileIcon.Visual.YOrigin = VerticalAlignment.Bottom;
                    _gumknix.TooltipLayer.Add(_draggingFileIcon.Visual);

                    _draggingFileDetails = new();
                    _draggingFileDetails.Text = "";
                    _draggingFileDetails.Visual.XOrigin = HorizontalAlignment.Left;
                    _draggingFileDetails.Visual.YOrigin = VerticalAlignment.Top;
                    _draggingFileDetails.Visual.Visible = false;
                    _gumknix.TooltipLayer.Add(_draggingFileDetails.Visual);
                }

                FileSystemItem.TransferType transferType = FileSystemItem.TransferType.None;
                if (_draggingFileIcon != null)
                {
                    _draggingFileIcon.X = GumService.Default.Cursor.X;
                    _draggingFileIcon.Y = GumService.Default.Cursor.Y;
                    _draggingFileDetails.X = _draggingFileIcon.X + 10;
                    _draggingFileDetails.Y = _draggingFileIcon.Y + 10;

                    FileSystemItem destinationFolder = _gumknix.CursorOverFileItem();

                    if (destinationFolder != null)
                        transferType = _draggingFileItem.DetermineTransferType(destinationFolder,
                            userRequestedCopy: GumService.Default.Keyboard.IsCtrlDown,
                            userRequestedMove: GumService.Default.Keyboard.IsShiftDown);

                    if (transferType != FileSystemItem.TransferType.None)
                        _draggingFileDetails.Text =
                            $"{(transferType == FileSystemItem.TransferType.Move ? "Move" : "Copy")} to \"{destinationFolder.Path}\"";
                    _draggingFileDetails.Visual.Visible = transferType != FileSystemItem.TransferType.None;
                }

                if (GumService.Default.Cursor.PrimaryDown == false)
                {
                    if (transferType != FileSystemItem.TransferType.None)
                        _gumknix.FilesDropped([_draggingFileItem],
                            GumService.Default.Keyboard.IsCtrlDown, GumService.Default.Keyboard.IsShiftDown);

                    _draggingFileItem = null;
                    if (_draggingFileIcon != null)
                    {
                        _gumknix.TooltipLayer.Remove(_draggingFileIcon.Visual);
                        _gumknix.TooltipLayer.Remove(_draggingFileDetails.Visual);
                        _draggingFileIcon = null;
                        _draggingFileDetails = null;
                    }
                }
            }
        }

        private void AddTreeItem(FileSystemItem fileSystemItem)
        {
            _treeListBox.Items.Add(fileSystemItem);
        }

        private void AddFolderItem(FileSystemItem fileSystemItem)
        {
            FolderFileSystemItems.Add(fileSystemItem);
            _folderListBox.Items.Add(fileSystemItem);

#if BLAZORGL
            SelectedFolder.AddChild(fileSystemItem);

            fileSystemItem.OnFileChanged += (s) =>
            {
                fileSystemItem.FileListBoxItem.UpdateToObject(fileSystemItem);
            };
#endif
        }

        public void OnTreeItemClick(FileSystemItem selectedItem)
        {
            FolderFileSystemItems.Clear();
            _folderListBox.Items.Clear();

            if (selectedItem != null)
            {
#if BLAZORGL
                Task<FileSystemDirectoryHandle> directoryHandleTask;
                long reconnectId = selectedItem.ReconnectId ?? DateTime.UtcNow.Ticks;

                try
                {
                    if (selectedItem.OriginPrivateFileSystem)
                    {
                        storageManager = StorageManager.FromNavigator();
                        directoryHandleTask = storageManager.GetDirectory();
                    }
                    else
                    {
                        DirectoryPickerOptions options = new()
                        {
                            Mode = FileSystemPermissionMode.ReadWrite,
                            StartInWellKnownDirectory = WellKnownDirectory.Music,
                            Id = $"FileCabiKnit-{reconnectId}"
                        };
                        directoryHandleTask = nkast.Wasm.Dom.Window.Current.ShowDirectoryPickerAsync(options);
                    }

                    directoryHandleTask.ContinueWith(t =>
                    {
                        if (t.IsCompletedSuccessfully && t.Result != null)
                        {
                            FileSystemDirectoryHandle selectedDirectory = t.Result;
                            if ((selectedItem.OriginPrivateFileSystem == false) && (ConnectedFolders.ContainsKey(reconnectId) == false))
                            {
                                ConnectedFolders.Add(reconnectId, selectedDirectory.Name);
                                RefreshConnectedFolders();
                                for (int i = 0; i < _treeListBox.Items.Count; i++)
                                {
                                    if ((_treeListBox.Items[i] as FileSystemItem).ReconnectId == reconnectId)
                                    {
                                        selectedItem = _treeListBox.Items[i] as FileSystemItem;
                                        break;
                                    }
                                }
                                SaveSettings();
                            }

                            if (selectedItem.Handle == null)
                                selectedItem = new(selectedDirectory, selectedItem.Parent);

                            SelectedTreeItem = selectedItem;
                            ChangeDirectory(selectedItem);
                        }
                    });
                }
                catch (Exception e)
                {
                }
#endif
            }
        }

        public void OnFolderItemOpenClick(FileSystemItem fileSystemItem)
        {
            if (fileSystemItem.Type == FileSystemItem.Types.Directory)
                ChangeDirectory(fileSystemItem);
            else if (OnFileOpenRequest != null)
                OnFileOpenRequest.Invoke(this, [fileSystemItem]);
            else
                _gumknix.StartApplet(fileSystemItem.GetDefaultAppletInfo().AppletType, [fileSystemItem]);
        }

        public void ChangeDirectory(FileSystemItem fileSystemItem)
        {
            FolderFileSystemItems.Clear();
            _folderListBox.Items.Clear();
            SelectedFileSystemItem = null;

#if BLAZORGL
            if (fileSystemObserver != null)
            {
                fileSystemObserver.Disconnect();
                fileSystemObserver = null;
            }

            SelectedFolder = (fileSystemItem.Type == FileSystemItem.Types.Directory) ?
                fileSystemItem : fileSystemItem.Parent;

            SelectedPath.Clear();
            SelectedPath.Add(SelectedFolder);
            while (SelectedPath[^1].Parent != null)
                SelectedPath.Add(SelectedPath[^1].Parent);
            SelectedPath.Reverse();

            for (int i = 0; i < _treeListBox.Items.Count; i++)
            {
                FileSystemItem treeItem = _treeListBox.Items[i] as FileSystemItem;
                if (treeItem == SelectedPath[0])
                {
                    SelectedTreeItem = treeItem;
                    SelectedTreeItem.FileListBoxItem.IsSelected = true;
                    break;
                }
            }

            _navigationBar.Visual.Children.Clear();
            _navigationPathParts.Clear();
            _navigationPathPartsWidths.Clear();

            for (int i = 0; i < SelectedPath.Count; i++)
            {
                FileSystemItem pathPart = SelectedPath[i];

                TextRuntime separator = new();
                separator.Text = ">";
                separator.X = 5 + ((_navigationBar.Visual.Children.Count == 0) ? 7 : 0);
                separator.Y = 6;
                _navigationBar.Visual.AddChild(separator);

                Button button = new();
                button.Text = pathPart.Name;
                if (((pathPart.Handle == SelectedTreeItem?.Handle) && (SelectedTreeItem.OriginPrivateFileSystem)) ||
                    (fileSystemItem.OriginPrivateFileSystem && (i == 0)))
                    button.Text = "Browser Storage";
                button.X = 5;
                button.Y = 3;
                button.Visual.Width = 10;
                button.Visual.WidthUnits = DimensionUnitType.RelativeToChildren;
                button.Visual.Height = 26;
                button.Visual.HeightUnits = DimensionUnitType.Absolute;
                button.Click += (s, e) => ChangeDirectory(pathPart);

                TextRuntime buttonText = button.Visual.GetGraphicalUiElementByName("TextInstance") as TextRuntime;
                buttonText.Dock(Dock.SizeToChildren);
                buttonText.MaxNumberOfLines = 1;

                _navigationPathParts.Add(button);
                _navigationBar.Visual.AddChild(button);

                float width = button.Visual.AbsoluteRight -
                    ((_navigationPathParts.Count >= 2) ? _navigationPathParts[^2].Visual.AbsoluteRight : _navigationBar.AbsoluteLeft);
                _navigationPathPartsWidths.Add(width);
            }

            FileSystemDirectoryHandle SelectedDirectoryHandle = SelectedFolder.Handle as FileSystemDirectoryHandle;
            Task<FileSystemHandleArray> entriesHandle = SelectedDirectoryHandle.GetEntries();
            entriesHandle.ContinueWith(t =>
            {
                if (t.IsCompletedSuccessfully && t.Result != null)
                {
                    FileSystemHandleArray fileSystemHandles = entriesHandle.Result;
                    int count = fileSystemHandles.Count;
                    for (int i = 0; i < count; i++)
                    {
                        FileSystemHandle fileSystemHandle = fileSystemHandles[i];
                        FileSystemItem fileSystemItem = new(fileSystemHandle, SelectedFolder);
                        AddFolderItem(fileSystemItem);
                    }
                    SortFolderItems();
                }
            });

            fileSystemObserver = FileSystemObserver.Create();
            fileSystemObserver.Observe(SelectedDirectoryHandle, false).ContinueWith(t =>
            {
                if (t.IsCompletedSuccessfully)
                {
                    fileSystemObserver.ChangeRecord += async (s, e) =>
                    {
                        Console.WriteLine($"FileSystemObserver.ChangeRecord: {e.ChangeRecord.Type} " +
                            (e.ChangeRecord.ChangedHandle?.Name ?? e.ChangeRecord.RelativePathComponents?.LastOrDefault()));

                        FileSystemHandle changedHandle = e.ChangeRecord.ChangedHandle;
                        bool found = false;

                        if (changedHandle == null)
                        {
                            if (e.ChangeRecord.Type == FileSystemChangeRecordType.Disappeared)
                            {
                                for (int i = 0; i < FolderFileSystemItems.Count && !found; i++)
                                {
                                    FileSystemItem fileSystemItemToCheck = FolderFileSystemItems[i];
                                    if (fileSystemItemToCheck.Name == e.ChangeRecord.RelativePathComponents[^1])
                                    {
                                        found = true;
                                        FolderFileSystemItems.RemoveAt(i);
                                        _folderListBox.Items.Remove(fileSystemItemToCheck);
                                        fileSystemItemToCheck.Parent?.Children.Remove(fileSystemItemToCheck);
                                    }
                                }
                            }
                            return;
                        }

                        for (int i = 0; i < FolderFileSystemItems.Count && !found; i++)
                        {
                            FileSystemItem fileSystemItemToCheck = FolderFileSystemItems[i];
                            FileSystemHandle fileSystemHandleToCheck = fileSystemItemToCheck.Handle;

                            bool sameEntry = await changedHandle.IsSameEntry(fileSystemHandleToCheck);
                            if (sameEntry)
                            {
                                found = true;
                                if (changedHandle.Kind == FileSystemHandleKind.File)
                                {
                                    FileSystemFileHandle changedHandleFileHandle = e.ChangeRecord.ChangedHandle as FileSystemFileHandle;
                                    File file = await changedHandleFileHandle.GetFile();
                                    if (file != null)
                                        fileSystemItemToCheck.SetFromFile(file);
                                }
                            }
                        }

                        if (!found)
                        {
                            FileSystemItem fileSystemItem = new(changedHandle, SelectedFolder);
                            AddFolderItem(fileSystemItem);
                            SortFolderItems();
                        }
                    };
                }
            });
#endif
        }

        public void SortFolderItems()
        {
            if (FolderFileSystemItems.Count == 0)
                return;

            var sorted = FolderFileSystemItems
                .Where(item => ((FolderExtensionFilters.Count == 0) ||
                (item.Type == FileSystemItem.Types.Directory) ||
                (FolderExtensionFilters.Any(ext => item.Name.EndsWith(ext, StringComparison.OrdinalIgnoreCase)))))
                .OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
                .OrderBy(i => (int)i.Type)
                .ToList();

            _folderListBox.Items.Clear();
            for (int i = 0; i < sorted.Count; i++)
                _folderListBox.Items.Add(sorted[i]);
        }

        private void RefreshConnectedFolders()
        {
            for (int i = 0; i < ConnectedFolders.Keys.Count; i++)
            {
                long reconnectId = ConnectedFolders.Keys.ElementAt(i);

                bool contains = false;
                for (int j = 0; j < _treeListBox.Items.Count; j++)
                {
                    if ((_treeListBox.Items[j] as FileSystemItem).ReconnectId == reconnectId)
                    {
                        contains = true;
                        break;
                    }
                }
                if (contains)
                    continue;

                string folderName = ConnectedFolders[reconnectId];
                FileSystemItem fileSystemItem = new(folderName, null) { ReconnectId = reconnectId };
                AddTreeItem(fileSystemItem);
            }
        }

        private void SetContextMenuTreeItem(FileSystemItem highlightedTreeItem)
        {
            _contextMenu = new(_systemVisual);
            if (highlightedTreeItem != null)
            {
                _contextMenu.AddOptionOpen(this, highlightedTreeItem, treeItem: true);
                if (highlightedTreeItem.ReconnectId != null)
                    _contextMenu.AddOptionForgetShortcut(this, highlightedTreeItem);
#if BLAZORGL
                if ((highlightedTreeItem.OriginPrivateFileSystem) || (highlightedTreeItem.ReconnectId != null))
                    _contextMenu.AddOptionProperties(highlightedTreeItem);
#endif
            }
            _contextMenu.ShowMenu();
        }

        private void SetContextMenuFolderItem()
        {
            _contextMenu = new(_systemVisual);
            if (SelectedFileSystemItem != null)
            {
                if (SelectedFileSystemItem.GetDefaultAppletInfo().AppletType != null)
                    _contextMenu.AddOptionOpen(this, SelectedFileSystemItem, folderItem: true);
                if (SelectedFileSystemItem.Type == FileSystemItem.Types.File)
                    _contextMenu.AddOptionOpenWith(SelectedFileSystemItem);
                _contextMenu.AddOptionCut(SelectedFileSystemItem);
                _contextMenu.AddOptionCopy(SelectedFileSystemItem);
                _contextMenu.AddOptionPaste(SelectedFileSystemItem);
                if (SelectedFileSystemItem.Type == FileSystemItem.Types.File)
                    _contextMenu.AddOptionDownload(SelectedFileSystemItem);
                if (SelectedFileSystemItem.Parent != null)
                    _contextMenu.AddOptionDelete(SelectedFileSystemItem);
                _contextMenu.AddOptionRename(this, SelectedFileSystemItem);
                _contextMenu.AddOptionProperties(SelectedFileSystemItem);
            }
            else
            {
                _contextMenu.AddOptionNewFolder(SelectedFolder);
                _contextMenu.AddOptionNewFile(SelectedFolder, "Text Document", ".txt");
                _contextMenu.AddOptionPaste(SelectedFolder);
                _contextMenu.AddOptionProperties(SelectedFolder);
            }
            _contextMenu.ShowMenu();
        }

        public void RenameTextEntry(FileSystemItem fileSystemItem)
        {
            TextRuntime textRuntime = fileSystemItem.FileListBoxItem.Visual.GetGraphicalUiElementByName("TextInstance") as TextRuntime;
            textRuntime.Visible = false;

            _renameTextBox = new TextBox();
            _renameTextBox.Text = fileSystemItem.Name;
            _renameTextBox.Dock(Dock.Fill);
            _renameTextBox.Visual.X = 21;
            _renameTextBox.Visual.XOrigin = HorizontalAlignment.Left;
            _renameTextBox.Visual.XUnits = GeneralUnitType.PixelsFromSmall;
            _renameTextBox.Visual.Y = textRuntime.Y;
            _renameTextBox.Visual.GetGraphicalUiElementByName("Background").Visible = false;
            SelectedFileSystemItem.FileListBoxItem.AddChild(_renameTextBox);

            int dotIndex = _renameTextBox.Text.LastIndexOf('.');
            if (dotIndex >= 0)
                _renameTextBox.CaretIndex = dotIndex;
            else
                _renameTextBox.CaretIndex = _renameTextBox.Text.Length;

            _renameTextBox.KeyDown += (s, e) =>
            {
                if ((e.Key == Keys.Enter) || (e.Key == Keys.Escape))
                    _renameTextBox.IsFocused = false;
            };

            _renameTextBox.LostFocus += async (s, e) =>
            {
                if ((_renameTextBox.Text.Length >= 1) && (_renameTextBox.Text != fileSystemItem.Name))
                {
#if BLAZORGL
                    switch (fileSystemItem.Type)
                    {
                        case FileSystemItem.Types.Directory:
                            await SelectedFileSystemItem.CopyDirectory(SelectedFolder, newName: _renameTextBox.Text, preserveOriginal: false);
                            break;
                        case FileSystemItem.Types.File:
                            await SelectedFileSystemItem.CopyFile(SelectedFolder, newName: _renameTextBox.Text, preserveOriginal: false);
                            break;
                    }
#endif
                }

                SelectedFileSystemItem.FileListBoxItem.Visual.Children.Remove(_renameTextBox.Visual);
                _renameTextBox.Visual.RemoveFromRoot();
                textRuntime.Visible = true;
                _renameTextBox = null;
                _renameTextBoxActivated = false;
            };
        }

        private FileSystemItem CursorOverListBoxItem(ListBox listBox)
        {
            if (listBox.GetVisual("Background").HasCursorOver(GumService.Default.Cursor.X, GumService.Default.Cursor.Y))
            {
                for (int i = 0; i < listBox.Items.Count; i++)
                {
                    FileSystemItem fileSystemItem = listBox.Items[i] as FileSystemItem;
                    if (fileSystemItem.FileListBoxItem.IsHighlighted)
                        return fileSystemItem;
                }
            }
            return null;
        }

        public FileSystemItem CursorOverFileItem()
        {
            FileSystemItem cursorOverTreeItem = CursorOverListBoxItem(_treeListBox);
            FileSystemItem cursorOverFolderItem = CursorOverListBoxItem(_folderListBox);
            bool cursorOverFolderList = _folderListBox.GetVisual("Background").HasCursorOver(GumService.Default.Cursor.X, GumService.Default.Cursor.Y);

            if ((cursorOverTreeItem?.Type == FileSystemItem.Types.Directory) && cursorOverTreeItem.OriginPrivateFileSystem)
                return cursorOverTreeItem;
            else if (cursorOverFolderItem?.Type == FileSystemItem.Types.Directory)
                return cursorOverFolderItem;
            else if (cursorOverFolderList)
                return SelectedFolder;
            return null;
        }

        public Task FilesDropped(List<FileSystemItem> fileSystemItems, bool userRequestedCopy, bool userRequestedMove)
        {
            FileSystemItem destinationFolder = CursorOverFileItem();
            return destinationFolder.FilesDropped(_systemVisual, fileSystemItems, userRequestedCopy, userRequestedMove);
        }

        public void ForgetShortcut(FileSystemItem fileSystemItem)
        {
            if (fileSystemItem.ReconnectId != null)
                ConnectedFolders.Remove(fileSystemItem.ReconnectId.Value);
            _treeListBox.Items.Remove(fileSystemItem);
            SaveSettings();
        }

        public void Close()
        {
#if BLAZORGL
            if (fileSystemObserver != null)
                fileSystemObserver.Disconnect();
#endif
        }

        private void LoadSettings()
        {
#if BLAZORGL
            FileSystemDirectoryHandle appletUserDataStorageHandle = _gumknix.AppletUserDataStorage.Handle as FileSystemDirectoryHandle;
            Task<FileSystemDirectoryHandle> directoryHandleTask = appletUserDataStorageHandle.GetDirectoryHandle("File CabiKnit", true);
            directoryHandleTask.ContinueWith(t =>
            {
                if (t.IsCompletedSuccessfully && t.Result != null)
                {
                    AppletUserDataDirectory = t.Result;
                    Task<FileSystemFileHandle> AppletSettingsFileTask = AppletUserDataDirectory.GetFileHandle("Settings.json", true);
                    AppletSettingsFileTask.ContinueWith(t =>
                    {
                        if (t.IsCompletedSuccessfully && t.Result != null)
                        {
                            AppletSettingsFile = t.Result;
                            Task<File> fileTask = AppletSettingsFile.GetFile();
                            fileTask.ContinueWith(t =>
                            {
                                if (t.IsCompletedSuccessfully && t.Result != null)
                                {
                                    File file = t.Result;
                                    Task<string> fileText = file.Text();
                                    fileText.ContinueWith(t =>
                                    {
                                        if (t.IsCompletedSuccessfully && t.Result != null && t.Result?.Length != 0)
                                        {
                                            try
                                            {
                                                Dictionary<long, string> loadedData =
                                                System.Text.Json.JsonSerializer.Deserialize<Dictionary<long, string>>(t.Result);
                                                if (loadedData != null)
                                                {
                                                    ConnectedFolders = loadedData;
                                                    RefreshConnectedFolders();
                                                }
                                            }
                                            catch
                                            {
                                            }
                                        }
                                    });
                                }
                            });
                        }
                    });
                }
            });
#endif
        }

        private void SaveSettings()
        {
#if BLAZORGL
            if (AppletSettingsFile == null)
                return;

            Task<FileSystemWritableFileStream> writableFileStreamTask = AppletSettingsFile.CreateWritable();
            writableFileStreamTask.ContinueWith(t =>
            {
                if (t.IsCompletedSuccessfully && t.Result != null)
                {
                    FileSystemWritableFileStream writableFileStream = t.Result;
                    string jsonSettings = System.Text.Json.JsonSerializer.Serialize(ConnectedFolders);
                    byte[] data = Encoding.UTF8.GetBytes(jsonSettings);
                    Task<bool> writeTask = writableFileStream.Write(data);
                    writeTask.ContinueWith(t =>
                    {
                        if (t.IsCompletedSuccessfully)
                        {
                            writableFileStream.Truncate((ulong)data.LongLength).ContinueWith(t =>
                            {
                                if (t.IsCompletedSuccessfully)
                                    writableFileStream.Close();
                            });
                        }
                    });
                }
            });
#endif
        }
    }
}
