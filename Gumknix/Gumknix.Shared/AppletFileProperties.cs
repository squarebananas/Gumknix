using System;
using System.Threading.Tasks;
using Gum.Forms;
using Gum.Forms.Controls;
using Gum.Wireframe;
using MonoGameGum.GueDeriving;

namespace Gumknix
{
    public class AppletFileProperties : BaseApplet
    {
        public static readonly string DefaultTitle = "File Properties";
        public static readonly string DefaultIcon = "";

        public FileSystemItem FileSystemItem { get; private set; }
        public FileSystemItem.FileStats FileStats { get; private set; }

        private Label _size;
        private Label _lastModified;
        private Label _contains;

        private Task _getAllChildrenTask;
        private bool _updating;

        public AppletFileProperties(Gumknix gumknix, object[] args = null) : base(gumknix, args)
        {
            FileSystemItem = args[0] as FileSystemItem;

#if BLAZORGL
            FileStats = new();
            FileStats.Add(FileSystemItem);
            if (FileSystemItem.Type == FileSystemItem.Types.Directory)
            {
                _updating = true;
                _getAllChildrenTask = FileSystemItem.GetAllChildrenAsync(true, addToFileStats: FileStats);
            }
#endif

            Initialize($"{FileSystemItem.Name} Properties", FileSystemItem.Icon, resizeMode: ResizeMode.NoResize);

            ColoredRectangleRuntime background = new();
            background.Color = Gum.Forms.DefaultVisuals.Styling.ActiveStyle.Colors.LightGray;
            background.Dock(Dock.Fill);
            background.Anchor(Anchor.TopLeft);
            Window.Visual.Children.Insert(1, background);

            float leftOffset = 25;
            float valueOffset = 125;
            float lineGap = 15;

            Label nameLabel = new();
            nameLabel.Text = "Filename:";
            nameLabel.X = leftOffset;
            nameLabel.Y = lineGap + 5;
            MainStackPanel.AddChild(nameLabel);

            Label name = new();
            name.Text = FileSystemItem.Name;
            name.X = valueOffset;
            nameLabel.AddChild(name);

            Label pathLabel = new();
            pathLabel.Text = "Path:";
            pathLabel.X = leftOffset;
            pathLabel.Y = lineGap;
            MainStackPanel.AddChild(pathLabel);

            Label path = new();
            path.Text = FileSystemItem.Path;
            path.X = valueOffset;
            pathLabel.AddChild(path);

            Label sizeLabel = new();
            sizeLabel.Text = "Size:";
            sizeLabel.X = leftOffset;
            sizeLabel.Y = lineGap;
            MainStackPanel.AddChild(sizeLabel);

            _size = new();
            _size.Text = "";
            _size.X = valueOffset;
            sizeLabel.AddChild(_size);

            Label lastModifiedLabel = new();
            lastModifiedLabel.Text = "Last Modified:";
            lastModifiedLabel.X = leftOffset;
            lastModifiedLabel.Y = lineGap;
            MainStackPanel.AddChild(lastModifiedLabel);

            _lastModified = new();
            _lastModified.Text = "";
            _lastModified.X = valueOffset;
            lastModifiedLabel.AddChild(_lastModified);

            if (FileSystemItem.Type == FileSystemItem.Types.Directory)
            {
                Label containsLabel = new();
                containsLabel.Text = "Contains:";
                containsLabel.X = leftOffset;
                containsLabel.Y = lineGap;
                MainStackPanel.AddChild(containsLabel);

                _contains = new();
                _contains.Text = "";
                _contains.X = valueOffset;
                containsLabel.AddChild(_contains);
            }

            SetTextValues();

            Button okButton = new();
            okButton.Text = "OK";
            okButton.X = leftOffset;
            okButton.Y = 25;
            okButton.Click += (s, e) => CloseRequest = true;
            MainStackPanel.AddChild(okButton);

            Window.Width = 600;
            Window.Height = 300;
        }

        public override void Update()
        {
            if (FileStats.ValuesChanged == true)
            {
                SetTextValues();
                FileStats.ValuesChanged = false;
            }

            if (_updating && _getAllChildrenTask.IsCompletedSuccessfully)
            {
                _updating = false;
                SetTextValues();
            }

            base.Update();
        }

        private void SetTextValues()
        {
            long bytes = FileStats.TotalSize;
            _size.Text = $"{FileSystemItem.GetMetricSize(bytes)} ({bytes:N0} byte{((bytes != 1) ? "s" : "")})";
            if (_updating)
                _size.Text = "Calculating - " + _size.Text;

            DateTime date = FileStats.LastModified;
            _lastModified.Text = (date != DateTime.MinValue) ? date.ToLocalTime().ToString() : "-";
            if (_updating)
                _lastModified.Text = $"Calculating{((date != DateTime.MinValue) ? $" - {_lastModified.Text}" : "")}";

            if (FileSystemItem.Type == FileSystemItem.Types.Directory)
            {
                int files = FileStats.FileCount;
                int folders = FileStats.DirectoryCount;
                _contains.Text = $"{files:N0} File{((files != 1) ? "s" : "")}, ";
                _contains.Text += $"{folders:N0} Folder{((folders != 1) ? "s" : "")}";
                if (_updating)
                    _contains.Text = "Calculating - " + _contains.Text;
            }
        }
    }
}
