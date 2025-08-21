using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Gum.Converters;
using Gum.DataTypes;
using Gum.Forms;
using Gum.Forms.Controls;
using Gum.Wireframe;
using MonoGameGum;
using MonoGameGum.GueDeriving;
using RenderingLibrary;
using RenderingLibrary.Graphics;

#if BLAZORGL
using nkast.Wasm.File;
using nkast.Wasm.FileSystem;
#endif

namespace Gumknix
{
    public class Desktop : BaseSystemVisual
    {
        private Panel _panel;
        private List<List<DesktopIcon>> _iconGrid;
        private Point _gridSpacing;
        private int _gridMaxVisibleX;
        private int _gridMaxVisibleY;

        private ContextMenuFile _contextMenu;

        private TextBox _renameTextBox;
        private bool _renameTextBoxActivated;

        private DesktopIcon _draggingIcon;
        private Vector2 _draggingIconInitialPosition;
        private TextRuntime _draggingIconLogo;
        private Label _draggingFileDetails;

        public FileSystemItem DesktopFileFolder { get; private set; }
        public Point? DropGridSlot { get; set; }

#if BLAZORGL
        private FileSystemObserver _fileSystemObserver;
#endif

        private BaseAppletSettings _savedSettings;

        public Desktop(Gumknix gumknix) : base(gumknix)
        {
            _iconGrid = [];
            _gridSpacing = new Point(100, 130);

            _gridMaxVisibleX = 5;
            _gridMaxVisibleY = 5;

            _panel = new Panel();
            _panel.Name = "Desktop";
            _panel.Dock(Dock.Fill);
            _panel.AddToRoot();
            Layer.Add(_panel.Visual);

            _savedSettings = new();
        }

        public void Initialize()
        {
            DesktopFileFolder = GumknixInstance.DesktopStorage;

            AddDefaultDesktopItems();

#if BLAZORGL
            FileSystemDirectoryHandle directoryHandle = DesktopFileFolder.Handle as FileSystemDirectoryHandle;
            Task<FileSystemHandleArray> entriesHandle = directoryHandle.GetEntries();
            entriesHandle.ContinueWith(t =>
            {
                if (t.IsCompletedSuccessfully && t.Result != null)
                {
                    FileSystemHandleArray fileSystemHandles = entriesHandle.Result;
                    int count = fileSystemHandles.Count;
                    for (int i = 0; i < count; i++)
                    {
                        FileSystemHandle fileSystemHandle = fileSystemHandles[i];
                        FileSystemItem fileSystemItem = new(fileSystemHandle, DesktopFileFolder);
                        DesktopFileFolder.AddChild(fileSystemItem);

                        DesktopIcon fileIcon = new(this, fileSystemItem);
                        AddIcon(fileIcon);
                    }
                }
            });

            _fileSystemObserver = FileSystemObserver.Create();
            _fileSystemObserver.Observe(directoryHandle, false).ContinueWith(t =>
            {
                if (t.IsCompletedSuccessfully)
                {
                    _fileSystemObserver.ChangeRecord += async (s, e) =>
                    {
                        Console.WriteLine($"FileSystemObserver.ChangeRecord: {e.ChangeRecord.Type} " +
                            (e.ChangeRecord.ChangedHandle?.Name ?? e.ChangeRecord.RelativePathComponents?.LastOrDefault()));

                        FileSystemHandle changedHandle = e.ChangeRecord.ChangedHandle;
                        bool found = false;

                        if (changedHandle == null)
                        {
                            if (e.ChangeRecord.Type == FileSystemChangeRecordType.Disappeared)
                            {
                                for (int x = 0; x < _iconGrid.Count && !found; x++)
                                {
                                    for (int y = 0; y < _iconGrid[x].Count && !found; y++)
                                    {
                                        DesktopIcon desktopIcon = _iconGrid[x][y];
                                        FileSystemItem fileSystemItem = desktopIcon?.FileSystemItem;
                                        if (fileSystemItem?.Name == e.ChangeRecord.RelativePathComponents[^1])
                                        {
                                            found = true;
                                            desktopIcon.RemoveFromRoot();
                                            _iconGrid[x][y] = null;
                                            _savedSettings.RemoveSetting($"{fileSystemItem.Type}:{fileSystemItem.Name}");
                                            fileSystemItem.Parent?.Children.Remove(fileSystemItem);
                                        }
                                    }
                                }
                            }
                            return;
                        }

                        for (int i = 0; i < DesktopFileFolder.Children.Count && !found; i++)
                        {
                            FileSystemItem fileSystemItemToCheck = DesktopFileFolder.Children[i];
                            FileSystemHandle fileSystemHandleToCheck = fileSystemItemToCheck.Handle;

                            bool sameEntry = await changedHandle.IsSameEntry(fileSystemHandleToCheck);
                            if (sameEntry)
                            {
                                found = true;
                                if (changedHandle.Kind == FileSystemHandleKind.File)
                                {
                                    FileSystemFileHandle changedHandleFileHandle = changedHandle as FileSystemFileHandle;
                                    File file = await changedHandleFileHandle.GetFile();
                                    if (file != null)
                                    {
                                        fileSystemItemToCheck.SetFromFile(file);

                                        DesktopIcon desktopIcon = fileSystemItemToCheck.DesktopIcon;
                                        desktopIcon.Text = fileSystemItemToCheck.Name;
                                        desktopIcon.IconText.Text = fileSystemItemToCheck.Icon;
                                        desktopIcon.AppletType = fileSystemItemToCheck.GetDefaultAppletInfo().AppletType;
                                    }
                                }
                            }
                        }

                        if (!found)
                        {
                            FileSystemItem fileSystemItem = new(changedHandle, DesktopFileFolder);
                            DesktopFileFolder.AddChild(fileSystemItem);

                            DesktopIcon icon = new(this, fileSystemItem);
                            AddIcon(icon, DropGridSlot);
                            DropGridSlot = null;
                        }
                    };
                }
            });
#endif
        }

        public void AddDefaultDesktopItems()
        {
            DesktopIcon fileCabiKnit = new(this, typeof(AppletFileCabiKnit));
            AddIcon(fileCabiKnit);
            DesktopIcon kniopad = new(this, typeof(AppletKniopad));
            AddIcon(kniopad);
            DesktopIcon moknicoEditor = new(this, typeof(AppletMoknicoEditor));
            AddIcon(moknicoEditor);
        }

        public void Update()
        {
            if (_savedSettings.Settings == null)
            {
                _savedSettings.LoadSettings<int[]>(GumknixInstance, "Desktop");
                return;
            }
            else
            {
                if (_savedSettings.SavePending)
                    _savedSettings.SaveSettings();
            }

#if BLAZORGL
            if ((DesktopFileFolder == null) && (GumknixInstance.DesktopStorage != null))
                Initialize();
#endif

            if (DesktopFileFolder == null)
                return;

            UpdateDialogs();

            _gridMaxVisibleX = (int)(_panel.Visual.GetAbsoluteWidth() / _gridSpacing.X);
            _gridMaxVisibleY = (int)(_panel.Visual.GetAbsoluteHeight() / _gridSpacing.Y);
            GridExpand(new Point(_gridMaxVisibleX, _gridMaxVisibleY));

            Point CursorOverSlot = CursorOverGridSlot();
            DesktopIcon CursorOverIcon = CursorOverDesktopIcon();

            if (GumService.Default.Cursor.SecondaryClick && (_contextMenu == null) &&
                (GumknixInstance.ObjectUnderCursor() == this))
            {
#if BLAZORGL
                _contextMenu = new(this);
                if (CursorOverIcon != null)
                {
                    if (CursorOverIcon.AppletType != null)
                        _contextMenu.AddOptionOpen(CursorOverIcon);
                    if ((CursorOverIcon.IconType == DesktopIcon.IconTypes.Folder) ||
                        (CursorOverIcon.IconType == DesktopIcon.IconTypes.File))
                    {
                        if (CursorOverIcon.IconType == DesktopIcon.IconTypes.File)
                            _contextMenu.AddOptionOpenWith(CursorOverIcon.FileSystemItem);
                        _contextMenu.AddOptionCut(CursorOverIcon.FileSystemItem);
                        _contextMenu.AddOptionCopy(CursorOverIcon.FileSystemItem);
                        _contextMenu.AddOptionPaste(CursorOverIcon.FileSystemItem);
                        if (CursorOverIcon.IconType == DesktopIcon.IconTypes.File)
                            _contextMenu.AddOptionDownload(CursorOverIcon.FileSystemItem);
                        if (CursorOverIcon.FileSystemItem?.Parent != null)
                            _contextMenu.AddOptionDelete(CursorOverIcon.FileSystemItem);
                        _contextMenu.AddOptionRename(CursorOverIcon);
                        _contextMenu.AddOptionProperties(CursorOverIcon.FileSystemItem);
                    }
                }
                else
                {
                    _contextMenu.AddOptionNewFolder(DesktopFileFolder);
                    _contextMenu.AddOptionNewFile(DesktopFileFolder, "Text Document", ".txt");
                    _contextMenu.AddOptionCut(DesktopFileFolder);
                    _contextMenu.AddOptionCopy(DesktopFileFolder);
                    _contextMenu.AddOptionPaste(DesktopFileFolder);
                    _contextMenu.AddOptionProperties(DesktopFileFolder);
                    ContextMenuAddOptionTheme();
                }
                _contextMenu.ShowMenu();
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

            if (_draggingIcon == null && _contextMenu == null && _renameTextBox == null &&
                (GumknixInstance.ObjectUnderCursor() == this))
            {
                if (GumService.Default.Cursor.PrimaryPush && (CursorOverIcon != null))
                {
                    _draggingIcon = CursorOverIcon;
                    _draggingIconInitialPosition = new Vector2(GumService.Default.Cursor.X, GumService.Default.Cursor.Y);
                }
            }

            if (_draggingIcon != null)
            {
                Vector2 cursorPosition = new Vector2(GumService.Default.Cursor.X, GumService.Default.Cursor.Y);

                if ((_draggingIconLogo == null) &&
                    (Vector2.DistanceSquared(cursorPosition, _draggingIconInitialPosition) > 100f))
                {
                    _draggingIconLogo = new TextRuntime();
                    _draggingIconLogo.Text = _draggingIcon.IconText.Text;
                    _draggingIconLogo.Font = "FluentSymbolSet";
                    _draggingIconLogo.FontSize = 48;
                    _draggingIconLogo.XOrigin = HorizontalAlignment.Center;
                    _draggingIconLogo.YOrigin = VerticalAlignment.Bottom;
                    GumknixInstance.TooltipLayer.Add(_draggingIconLogo);

                    _draggingFileDetails = new();
                    _draggingFileDetails.Text = "";
                    _draggingFileDetails.Visual.XOrigin = HorizontalAlignment.Left;
                    _draggingFileDetails.Visual.YOrigin = VerticalAlignment.Top;
                    _draggingFileDetails.Visual.Visible = false;
                    GumknixInstance.TooltipLayer.Add(_draggingFileDetails.Visual);
                }

                FileSystemItem.TransferType transferType = FileSystemItem.TransferType.None;
                if (_draggingIconLogo != null)
                {
                    _draggingIconLogo.X = GumService.Default.Cursor.X;
                    _draggingIconLogo.Y = GumService.Default.Cursor.Y;
                    _draggingFileDetails.X = _draggingIconLogo.X + 10;
                    _draggingFileDetails.Y = _draggingIconLogo.Y + 10;

                        FileSystemItem destinationFolder = GumknixInstance.CursorOverFileItem();
                        if ((_draggingIcon.FileSystemItem != null) && (destinationFolder != null))
                            transferType = _draggingIcon.FileSystemItem.DetermineTransferType(destinationFolder,
                                userRequestedCopy: GumService.Default.Keyboard.IsCtrlDown,
                                userRequestedMove: GumService.Default.Keyboard.IsShiftDown);

                    if (transferType != FileSystemItem.TransferType.None)
                        _draggingFileDetails.Text =
                            $"{(transferType == FileSystemItem.TransferType.Move ? "Move" : "Copy")} to \"{destinationFolder.Path}\"";
                    _draggingFileDetails.Visual.Visible = transferType != FileSystemItem.TransferType.None;
                }

                if (GumService.Default.Cursor.PrimaryDown == false)
                {
                    object objectUnderCursor = GumknixInstance.ObjectUnderCursor();
                    if ((objectUnderCursor == this) && (CursorOverIcon == null) && (transferType == FileSystemItem.TransferType.None))
                    {
                        if ((CursorOverSlot.X >= 0) && (CursorOverSlot.X < _iconGrid.Count) &&
                            (CursorOverSlot.Y >= 0) && (CursorOverSlot.Y < _iconGrid[CursorOverSlot.X].Count))
                        {
                            _iconGrid[_draggingIcon.DesktopGridSlot.X][_draggingIcon.DesktopGridSlot.Y] = null;
                            _iconGrid[CursorOverSlot.X][CursorOverSlot.Y] = _draggingIcon;

                            _draggingIcon.DesktopGridSlot = CursorOverSlot;
                            _draggingIcon.Visual.X = _draggingIcon.DesktopGridSlot.X * _gridSpacing.X;
                            _draggingIcon.Visual.Y = _draggingIcon.DesktopGridSlot.Y * _gridSpacing.Y;

                            SetSavedSettingValue(_draggingIcon);
                        }
                    }
                    else
                    {
                        if (transferType != FileSystemItem.TransferType.None)
                            GumknixInstance.FilesDropped([_draggingIcon.FileSystemItem],
                                GumService.Default.Keyboard.IsCtrlDown, GumService.Default.Keyboard.IsShiftDown);
                    }

                    _draggingIcon = null;
                    if (_draggingIconLogo != null)
                    {
                        GumknixInstance.TooltipLayer.Remove(_draggingIconLogo);
                        GumknixInstance.TooltipLayer.Remove(_draggingFileDetails.Visual);
                        _draggingIconLogo = null;
                        _draggingFileDetails = null;
                    }
                }
            }
        }

        public void AddIcon(DesktopIcon icon, Point? gridSlot = null)
        {
            if (gridSlot == null)
            {
                if (_savedSettings.Settings.TryGetValue($"{icon.IconType}:{icon.Name}", out object value))
                {
                    try
                    {
                        int[] values = value as int[];
                        gridSlot = new Point(values[0], values[1]);
                    }
                    catch (Exception e)
                    {
                    }
                }
            }

            if (gridSlot != null)
                if (gridSlot.Value.X < _iconGrid.Count)
                    if (gridSlot.Value.Y < _iconGrid[gridSlot.Value.X].Count)
                        if (_iconGrid[gridSlot.Value.X][gridSlot.Value.Y] != null)
                            gridSlot = null;

            Point maxGridSlot = new(_gridMaxVisibleX, _gridMaxVisibleY);
            if (gridSlot.HasValue)
            {
                maxGridSlot.X = Math.Max(_gridMaxVisibleX, gridSlot.Value.X);
                maxGridSlot.Y = Math.Max(_gridMaxVisibleY, gridSlot.Value.Y);
            }
            GridExpand(maxGridSlot);

            if (gridSlot == null)
                gridSlot = FindFirstFreeSlot();

            _iconGrid[gridSlot.Value.X][gridSlot.Value.Y] = icon;
            icon.DesktopGridSlot = gridSlot.Value;
            icon.Visual.Anchor(Anchor.TopLeft);
            icon.Visual.X = gridSlot.Value.X * _gridSpacing.X;
            icon.Visual.Y = gridSlot.Value.Y * _gridSpacing.Y;
            _panel.AddChild(icon);

            SetSavedSettingValue(icon);
        }

        public Point CursorOverGridSlot()
        {
            return new(GumService.Default.Cursor.X / _gridSpacing.X,
                GumService.Default.Cursor.Y / _gridSpacing.Y);
        }

        public DesktopIcon CursorOverDesktopIcon()
        {
            Point gridSlot = CursorOverGridSlot();
            if ((gridSlot.X >= 0) && (gridSlot.X < _iconGrid.Count))
                if ((gridSlot.Y >= 0) && (gridSlot.Y < _iconGrid[gridSlot.X].Count))
                    return _iconGrid[gridSlot.X][gridSlot.Y];
            return null;
        }

        public void GridExpand(Point maxGridSlot)
        {
            while (_iconGrid.Count <= maxGridSlot.X)
                _iconGrid.Add([]);
            for (int x = 0; x < _iconGrid.Count; x++)
                while (_iconGrid[x].Count <= maxGridSlot.Y)
                    _iconGrid[x].Add(null);
        }

        public Point FindFirstFreeSlot()
        {
            Point maxGridSlot = new(_iconGrid.Count, _iconGrid[0].Count);
            while (true)
            {
                for (int x = 0; x < _iconGrid.Count; x++)
                    for (int y = 0; y < _iconGrid[x].Count; y++)
                        if (_iconGrid[x][y] == null)
                            return new Point(x, y);

                maxGridSlot.X++;
                GridExpand(maxGridSlot);
            }
        }

        public void RenameTextEntry(DesktopIcon desktopIcon)
        {
            FileSystemItem fileSystemItem = desktopIcon.FileSystemItem;
            TextRuntime textRuntime = desktopIcon.Visual.GetGraphicalUiElementByName("TextInstance") as TextRuntime;
            textRuntime.Visible = false;

            _renameTextBox = new();
            _renameTextBox.Text = fileSystemItem.Name;
            _renameTextBox.Dock(Dock.Fill);
            _renameTextBox.Visual.XOrigin = HorizontalAlignment.Left;
            _renameTextBox.Visual.XUnits = GeneralUnitType.PixelsFromSmall;
            _renameTextBox.Visual.Y = 0;
            _renameTextBox.Visual.YOrigin = VerticalAlignment.Top;
            _renameTextBox.Visual.YUnits = GeneralUnitType.PixelsFromMiddle;
            _renameTextBox.TextWrapping = TextWrapping.Wrap;
            _renameTextBox.Visual.TextOverflowVerticalMode = TextOverflowVerticalMode.SpillOver;
            TextRuntime renameTextRuntime = _renameTextBox.Visual.GetGraphicalUiElementByName("TextInstance") as TextRuntime;
            renameTextRuntime.Dock(Dock.FillHorizontally);
            renameTextRuntime.VerticalAlignment = VerticalAlignment.Top;
            renameTextRuntime.HorizontalAlignment = HorizontalAlignment.Center;
            renameTextRuntime.TextOverflowHorizontalMode = TextOverflowHorizontalMode.EllipsisLetter;
            renameTextRuntime.TextOverflowVerticalMode = TextOverflowVerticalMode.SpillOver;
            desktopIcon.AddChild(_renameTextBox);

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

            _renameTextBox.LostFocus += (s, e) =>
            {
                if ((_renameTextBox.Text.Length >= 1) && (_renameTextBox.Text != fileSystemItem.Name))
                    fileSystemItem.Rename(_renameTextBox.Text);

                desktopIcon.Visual.Children.Remove(_renameTextBox.Visual);
                _renameTextBox.Visual.RemoveFromRoot();
                textRuntime.Visible = true;
                _renameTextBox = null;
            };
        }

        public void ContextMenuAddOptionTheme()
        {
            MenuItem optionTheme = new();
            optionTheme.Header = "Change Theme...";
            _contextMenu.Items.Add(optionTheme);

            SettingsThemes.Theme initialTheme = GumknixInstance.SettingsThemes.CurrentTheme;
            bool themeConfirmed = false;

            for (int i = 0; i < GumknixInstance.SettingsThemes.AllThemes.Count; i++)
            {
                SettingsThemes.Theme theme = GumknixInstance.SettingsThemes.AllThemes[i];
                MenuItem themeItem = new();
                themeItem.Header = theme.Name;
                themeItem.Visual.Width = 220;
                themeItem.Visual.WidthUnits = DimensionUnitType.Absolute;
                themeItem.Visual.RollOn += (s, e) =>
                {
                    GumknixInstance.SettingsThemes.ApplyTheme(theme);
                };
                themeItem.Visual.Click += (s, e) =>
                {
                    themeConfirmed = true;
                    _contextMenu.CloseMenu();
                };
                optionTheme.LostFocus += (s, e) =>
                {
                    if (!themeConfirmed)
                        GumknixInstance.SettingsThemes.ApplyTheme(initialTheme);
                };
                if ((theme == GumknixInstance.SettingsThemes.CurrentTheme) && (optionTheme.Items.Count >= 1))
                    optionTheme.Items.Insert(0, themeItem);
                else
                    optionTheme.Items.Add(themeItem);
            }
        }

        public override void ShowDialog(BaseDialog dialog)
        {
            dialog.Window.RemoveFromRoot();
            dialog.Window.AddToRoot();
            Layer.Add(dialog.Window.Visual);
            Dialogs.Add(dialog);
        }

        public FileSystemItem CursorOverFileItem()
        {
            DesktopIcon cursorOverIcon = CursorOverDesktopIcon();
            return (cursorOverIcon?.IconType == DesktopIcon.IconTypes.Folder) ?
                cursorOverIcon.FileSystemItem : DesktopFileFolder;
        }

        public Task FilesDropped(List<FileSystemItem> fileSystemItems, bool userRequestedCopy, bool userRequestedMove)
        {
            FileSystemItem destinationFolder = CursorOverFileItem();
            DropGridSlot = CursorOverGridSlot();
            Task fileDroppedTask = destinationFolder.FilesDropped(this, fileSystemItems, userRequestedCopy, userRequestedMove);
            return fileDroppedTask;
        }

        public void SetSavedSettingValue(DesktopIcon icon)
        {
            _savedSettings.SetValue($"{icon.IconType}:{icon.Name}", new[] { icon.DesktopGridSlot.X, icon.DesktopGridSlot.Y });
        }
    }
}
