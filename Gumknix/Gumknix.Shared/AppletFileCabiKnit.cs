using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Gumknix
{
    public class AppletFileCabiKnit : BaseApplet
    {
        public static readonly string DefaultTitle = "File CabiKnit";
        public static readonly string DefaultIcon = "\uE066";

        private ControlsFileViewer _fileViewer;

        public AppletFileCabiKnit(Gumknix gumknix, object[] args = null) : base(gumknix, args)
        {
            base.Initialize(DefaultTitle, DefaultIcon);

            _fileViewer = new(this, MainStackPanel);

            if (args?.Length >= 1)
            {
                FileSystemItem fileSystemItem = args[0] as FileSystemItem;
                if (fileSystemItem != null)
                    _fileViewer.ChangeDirectory(fileSystemItem);
            }
        }

        public override void Update()
        {
            _fileViewer.Update(Layer);
#if BLAZORGL
            if (_fileViewer.SelectedTreeItem?.Name != null)
                SetTitle($"{_fileViewer.SelectedTreeItem.Name} - {DefaultTitle}");
#endif
            base.Update();
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
