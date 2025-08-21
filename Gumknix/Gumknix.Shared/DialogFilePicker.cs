using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Gum.Converters;
using Gum.DataTypes;
using Gum.Wireframe;
using Gum.Forms;
using Gum.Forms.Controls;
using MonoGameGum.GueDeriving;
using RenderingLibrary.Graphics;
using nkast.Wasm.FileSystem;

namespace Gumknix
{
    public class DialogFilePicker : BaseDialog
    {
        public delegate void OnFileSelectedDelegate(object sender, List<object> files);
        public event OnFileSelectedDelegate OnFileSelected;

        public enum FilePickerModes
        {
            OpenFile,
            OpenFolder,
            SaveFile
        }

        public FilePickerModes FilePickerMode { get; private set; }

        private ControlsFileViewer _fileViewer;
        private FileSystemItem _selectedFileItem;

        private readonly Panel _panel;
        private readonly TextBox _selectedName;
        private readonly ComboBox _filterTypes;
        private readonly Button _confirmButton;
        private readonly Button _cancelButton;

        public DialogFilePicker(BaseSystemVisual systemVisual, string title, string icon, FilePickerModes filePickerMode, FileSystemItem defaultFileSystemItem,
            string defaultFilename, FileExtensionFilter[] fileExtensionFilters) :
            base(systemVisual, title, icon)
        {
            FilePickerMode = filePickerMode;

            _selectedFileItem = new(defaultFilename);

            _fileViewer = new(this, MainStackPanel);
            _fileViewer.OnFileOpenRequest += (sender, args) =>
            {
                _selectedFileItem = args[0];
                Confirm();
            };

            _panel = new Panel();
            _panel.Dock(Dock.FillHorizontally);
            _panel.Visual.Height = 80;
            _panel.Visual.HeightUnits = DimensionUnitType.Absolute;
            MainStackPanel.AddChild(_panel);

            ColoredRectangleRuntime background = new();
            background.Color = new Color(4, 120, 137);
            background.Dock(Dock.Fill);
            background.Anchor(Anchor.TopLeft);
            _panel.AddChild(background);

            _selectedName = new();
            _selectedName.Placeholder = "";
            _selectedName.Visual.Anchor(Anchor.Left);
            _selectedName.Visual.X = 15;
            _selectedName.Visual.Y = -18;
            _selectedName.Visual.Width = -170;
            _selectedName.Visual.WidthUnits = DimensionUnitType.RelativeToParent;
            _selectedName.Visual.Height = 30;
            _selectedName.MaxNumberOfLines = 1;
            _selectedName.IsReadOnly = FilePickerMode != FilePickerModes.SaveFile;
            _selectedName.TextChanged += (s, e) =>
            {
                _selectedFileItem = new(_selectedName.Text, _selectedFileItem.Parent);
            };
            _panel.AddChild(_selectedName);

            _filterTypes = new();
            _filterTypes.Visual.Anchor(Anchor.Left);
            _filterTypes.Visual.X = 15;
            _filterTypes.Visual.Y = 18;
            _filterTypes.Visual.Width = -170;
            _filterTypes.Visual.WidthUnits = DimensionUnitType.RelativeToParent;
            for (int i = 0; i < fileExtensionFilters.Length; i++)
                _filterTypes.Items.Add(fileExtensionFilters[i]);
            _filterTypes.SelectedIndex = 0;
            _filterTypes.SelectionChanged += (sender, args) => SetExtensionFilters();
            _panel.AddChild(_filterTypes);

            _confirmButton = new();
            _confirmButton.Text = "Confirm";
            _confirmButton.Visual.Anchor(Anchor.Right);
            _confirmButton.Visual.X = -15;
            _confirmButton.Visual.Y = -18;
            _confirmButton.Visual.Click += (sender, args) => Confirm();
            _panel.AddChild(_confirmButton);

            _cancelButton = new Button();
            _cancelButton.Text = "Cancel";
            _cancelButton.Visual.Anchor(Anchor.Right);
            _cancelButton.Visual.X = -15;
            _cancelButton.Visual.Y = 18;
            _cancelButton.Visual.Click += (sender, args) =>
            {
                CloseRequest = true;
                OnFileSelected.Invoke(this, null);
            };
            _panel.AddChild(_cancelButton);

            Window.ResizeMode = Gum.Forms.ResizeMode.CanResize;
            Window.Visual.Width = 800;
            Window.Visual.Height = 400;

            BaseApplet applet = SystemVisual as BaseApplet;
            if (applet != null)
            {
                if ((Window.AbsoluteTop >= (applet.Window.AbsoluteTop - 20)) &&
                    (Window.AbsoluteTop < (applet.Window.AbsoluteTop + 80)))
                {
                    Window.Visual.Y = applet.Window.AbsoluteTop + 80;
                    Window.Visual.YUnits = GeneralUnitType.PixelsFromSmall;
                    Window.Visual.YOrigin = VerticalAlignment.Top;
                }
            }

            SetExtensionFilters();

            _selectedName.Text = (defaultFileSystemItem?.Type == FileSystemItem.Types.File) ? defaultFileSystemItem.Name :
                defaultFilename ?? "";

            if (defaultFileSystemItem != null)
                _fileViewer.ChangeDirectory(defaultFileSystemItem);
        }

        public override void Update()
        {
            _fileViewer.Update(SystemVisual.Layer);

            if ((_fileViewer.SelectedFileSystemItem != null) &&
                ((_fileViewer.SelectedFileSystemItem.Type == FileSystemItem.Types.File) ||
                (FilePickerMode == FilePickerModes.OpenFolder)))
                _selectedName.Text = _fileViewer.SelectedFileSystemItem.Name;

            _confirmButton.Text = (_fileViewer.SelectedFileSystemItem?.Type == FileSystemItem.Types.Directory) ? "Open" : "Confirm";

            base.Update();
        }

        private void Confirm()
        {
            if (_fileViewer.SelectedFileSystemItem != null)
            {
                if ((_fileViewer.SelectedFileSystemItem.Type == FileSystemItem.Types.Directory) &&
                    (FilePickerMode != FilePickerModes.OpenFolder))
                {
                    _fileViewer.ChangeDirectory(_fileViewer.SelectedFileSystemItem);
                    _fileViewer.SelectedFileSystemItem = null;
                    return;
                }
                _selectedFileItem = _fileViewer.SelectedFileSystemItem;
            }
            else
            {
                if ((FilePickerMode == FilePickerModes.SaveFile) && (_fileViewer.SelectedFolder != null))
                {
#if BLAZORGL
                    FileSystemHandle handle = _fileViewer.FolderFileSystemItems.FirstOrDefault(item => _selectedFileItem.Name == item.Name)?.Handle;
                    _selectedFileItem = new FileSystemItem(handle, _fileViewer.SelectedFolder);
#else
                    _selectedFileItem = new FileSystemItem(handle, _fileViewer.SelectedFolder);
#endif
                }
            }

            if ((_selectedFileItem.Name?.Length >= 1) && (_selectedFileItem.Parent != null))
            {
                CloseRequest = true;
                OnFileSelected.Invoke(this, [_selectedFileItem]);
            }
        }

        private void SetExtensionFilters()
        {
            if (_filterTypes.Items.Count == 0)
                return;

            FileExtensionFilter filter = (FileExtensionFilter)_filterTypes.Items[_filterTypes.SelectedIndex];
            _fileViewer.FolderExtensionFilters.Clear();
            for (int i = 0; i < filter.Extensions.Length; i++)
                if (filter.Extensions[i] != "*.*")
                    _fileViewer.FolderExtensionFilters.Add(filter.Extensions[i].Replace("*",""));

            _fileViewer.SortFolderItems();
        }

        protected override void Close()
        {
            _fileViewer.Close();
            base.Close();
        }

        public override FileSystemItem CursorOverFileItem()
        {
            return _fileViewer.CursorOverFileItem();
        }

        public override Task FilesDropped(List<FileSystemItem> fileSystemItems, bool userRequestedCopy, bool userRequestedMove)
        {
            return _fileViewer.FilesDropped(fileSystemItems, userRequestedCopy, userRequestedMove);
        }
    }
}
