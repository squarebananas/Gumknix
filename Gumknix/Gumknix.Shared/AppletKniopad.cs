using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Forms;
using Gum.Forms.Controls;
using Gum.Wireframe;
using MonoGameGum.GueDeriving;
using RenderingLibrary;
using RenderingLibrary.Graphics;

#if BLAZORGL
using nkast.Wasm.Dom;
using nkast.Wasm.File;
using nkast.Wasm.FileSystem;
#endif

namespace Gumknix
{
    public class AppletKniopad : BaseApplet
    {
        public static readonly string DefaultTitle = "Kniopad";
        public static readonly string DefaultIcon = "\uE88E";

        private Menu _menu;

        private ScrollViewer _textBoxScrollViewer;
        private KniopadTextBox _textBox;

        private FileSystemItem _fileSystemItem;
        private bool _unsavedChanges;

        public AppletKniopad(Gumknix gumknix, object[] args = null) : base(gumknix, args)
        {
            base.Initialize(DefaultTitle, DefaultIcon);

            _menu = new();
            MainStackPanel.Visual.AddChild(_menu.Visual);

            MenuItem menuItemFile = new();
            menuItemFile.Header = "File";
            _menu.Items.Add(menuItemFile);

            MenuItem menuItemFileNew = new();
            menuItemFileNew.Header = "New";
            menuItemFileNew.Visual.Width = 220;
            menuItemFileNew.Visual.WidthUnits = DimensionUnitType.Absolute;
            menuItemFile.Items.Add(menuItemFileNew);
            menuItemFileNew.Clicked += async (s, e) =>
            {
                if (_unsavedChanges)
                    await ShowUnsavedChanges();
                if (!_unsavedChanges)
                {
                    _textBox.Text = "";
                    _textBox.CaretIndex = 0;
                    _fileSystemItem = null;
                }
            };

            MenuItem menuItemFileOpen = new();
            menuItemFileOpen.Header = "Open";
            menuItemFileOpen.Dock(Dock.FillHorizontally);
            menuItemFile.Items.Add(menuItemFileOpen);
            menuItemFileOpen.Clicked += (s, e) => ShowOpen();

            MenuItem menuItemFileSave = new();
            menuItemFileSave.Header = "Save";
            menuItemFileSave.Dock(Dock.FillHorizontally);
            menuItemFile.Items.Add(menuItemFileSave);
            menuItemFileSave.Clicked += async (s, e) =>
            {
                if (_fileSystemItem?.Handle == null)
                    await ShowSave();
                else
                    await WriteFile(_fileSystemItem, _textBox.Text);
            };

            MenuItem menuItemFileSaveAs = new();
            menuItemFileSaveAs.Header = "Save As";
            menuItemFileSaveAs.Dock(Dock.FillHorizontally);
            menuItemFileSaveAs.Clicked += (s, e) => ShowSave();
            menuItemFile.Items.Add(menuItemFileSaveAs);

            MenuItem menuItemFilePrint = new();
            menuItemFilePrint.Header = "Print";
            menuItemFilePrint.Visual.IsEnabled = false;
            menuItemFilePrint.Dock(Dock.FillHorizontally);
            (menuItemFilePrint.Visual.GetGraphicalUiElementByName("TextInstance") as TextRuntime).Color = Color.Gray;
            menuItemFile.Items.Add(menuItemFilePrint);

            MenuItem menuItemFileExit = new();
            menuItemFileExit.Header = "Exit";
            menuItemFileExit.Dock(Dock.FillHorizontally);
            menuItemFile.Items.Add(menuItemFileExit);
            menuItemFileExit.Clicked += (s, e) => CloseRequest = true;

            MenuItem menuItemEdit = new();
            menuItemEdit.Header = "Edit";
            _menu.Items.Add(menuItemEdit);

            MenuItem menuItemEditCut = new();
            menuItemEditCut.Header = "Cut";
            menuItemEditCut.Visual.Width = 220;
            menuItemEditCut.Visual.WidthUnits = DimensionUnitType.Absolute;
            menuItemEdit.Items.Add(menuItemEditCut);
            menuItemEditCut.Clicked += (s, e) => _textBox.Cut();

            MenuItem menuItemEditCopy = new();
            menuItemEditCopy.Header = "Copy";
            menuItemEditCopy.Dock(Dock.FillHorizontally);
            menuItemEdit.Items.Add(menuItemEditCopy);
            menuItemEditCopy.Clicked += (s, e) => _textBox.Copy();

            MenuItem menuItemEditPaste = new();
            menuItemEditPaste.Header = "Paste";
            menuItemEditPaste.Dock(Dock.FillHorizontally);
            menuItemEdit.Items.Add(menuItemEditPaste);
            menuItemEditPaste.Clicked += (s, e) => _textBox.Paste();

            MenuItem menuItemView = new();
            menuItemView.Header = "View";
            _menu.Items.Add(menuItemView);

            MenuItem menuItemViewWordWrap = new();
            menuItemViewWordWrap.Header = "Word Wrap";
            menuItemViewWordWrap.Visual.Width = 220;
            menuItemViewWordWrap.Visual.WidthUnits = DimensionUnitType.Absolute;
            menuItemView.Items.Add(menuItemViewWordWrap);
            menuItemViewWordWrap.Clicked += (s, e) =>
            { _textBox.TextWrapping = (_textBox.TextWrapping == TextWrapping.NoWrap) ? TextWrapping.Wrap : TextWrapping.NoWrap; };

            _textBoxScrollViewer = new();
            _textBoxScrollViewer.Visual.Dock(Dock.Fill);
            _textBoxScrollViewer.Visual.Anchor(Anchor.TopLeft);
            _textBoxScrollViewer.Visual.Height -= 59;
            _textBoxScrollViewer.Visual.ClipsChildren = true;
            MainStackPanel.AddChild(_textBoxScrollViewer);

            _textBox = new KniopadTextBox();
            _textBox.Visual.Dock(Dock.FillHorizontally);
            _textBox.Visual.Anchor(Anchor.TopLeft);
            _textBox.Visual.HeightUnits = DimensionUnitType.Absolute;
            _textBox.TextWrapping = TextWrapping.Wrap;
            _textBox.Visual.TextOverflowVerticalMode = TextOverflowVerticalMode.SpillOver;
            _textBox.Placeholder = null;
            _textBox.AcceptsReturn = true;
            _textBox.AcceptsTab = true;
            _textBox.Text = "";
            List<StateSave> states = _textBox.Visual.Categories["TextBoxCategory"].States;
            for (int i = 0; i < states.Count; i++)
            {
                if (states[i].Variables.GetVariableSave("Background.Color") != null)
                    states[i].Variables.GetVariableSave("Background.Color").Value = new Color(32, 32, 32);
            }
            _textBoxScrollViewer.AddChild(_textBox);
            _textBox.UpdateState();
            _textBox.TextChanged += (s, e) => _unsavedChanges = true;

            if (args?.Length >= 1)
            {
                FileSystemItem fileSystemItem = args[0] as FileSystemItem;
                if (fileSystemItem != null)
                {
                    Task readTask = ReadFile(fileSystemItem);
                }
            }
        }

        public override void Update()
        {
            _textBox.Visual.Height = Math.Max(Window.ActualHeight - 63, _textBox.WrappedTextHeight);
            _textBox.Visual.HeightUnits = DimensionUnitType.Absolute;
            
            base.Update();
        }

        private Task<bool> ShowOpen()
        {
            TaskCompletionSource<bool> taskCompletionSource = new();

            DialogFilePicker filePicker = new(this, "Open File", "\uEE71", DialogFilePicker.FilePickerModes.OpenFile, _fileSystemItem, null,
                [new FileExtensionFilter("Show text documents", ["*.txt"]),
                 new FileExtensionFilter("Show all files", ["*.*"])]);
            filePicker.OnFileSelected += async (s, files) =>
            {
                bool success = false;
                try
                {
                    if (files?.Count == 1)
                    {
                        FileSystemItem file = files[0] as FileSystemItem;
                        if (file?.Type == FileSystemItem.Types.File)
                        {
                            if (_unsavedChanges)
                                 await ShowUnsavedChanges();
                            if (!_unsavedChanges)
                                success = await ReadFile(file);
                        }
                    }
                }
                catch (Exception exception)
                {
                    taskCompletionSource.TrySetException(exception);
                }
                taskCompletionSource.TrySetResult(success);
            };
            ShowDialog(filePicker);

            return taskCompletionSource.Task;
        }

        private Task<bool> ShowSave()
        {
            TaskCompletionSource<bool> taskCompletionSource = new();

            string defaultFilename = _fileSystemItem?.GetAvailableName("Untitled", ".txt") ?? "Untitled.txt";
            DialogFilePicker filePicker = new(this, "Save File", "\uEE71", DialogFilePicker.FilePickerModes.SaveFile, _fileSystemItem, defaultFilename,
                [new FileExtensionFilter("Show text documents", ["*.txt"]),
                 new FileExtensionFilter("Show all files", ["*.*"])]);
            filePicker.OnFileSelected += async (sender, files) =>
            {
                bool success = false;
                try
                {
                    if (files?.Count == 1)
                    {
                        FileSystemItem file = files[0] as FileSystemItem;
                        if (file?.Type == FileSystemItem.Types.File)
                        {
                            if (file.Handle != null)
                            {
                                bool result = await FileSystemItem.ShowOverwriteFile(this, file);
                                if (result)
                                {
                                    success = await WriteFile(file, _textBox.Text);
                                    _unsavedChanges = false;
                                }

                            }
                            else
                            {
                                success = await WriteFile(file, _textBox.Text);
                            }
                        }
                    }
                }
                catch(Exception exception)
                {
                    taskCompletionSource.TrySetException(exception);
                }
                taskCompletionSource.TrySetResult(success);
            };
            ShowDialog(filePicker);

            return taskCompletionSource.Task;
        }

        private Task<bool> ShowUnsavedChanges()
        {
            TaskCompletionSource<bool> taskCompletionSource = new();

            DialogMultiChoice multiChoice = new DialogMultiChoice(this, "Do you want to save your work?", "\uEE71",
               "Changes have been made which have yet to be saved.",
               ["Save", "Don't Save", "Cancel"]);
            multiChoice.OnChoiceSelected += async (s, choiceSelected) =>
            {
                bool success = false;
                switch (choiceSelected)
                {
                    case 0: // Save
                        if (_fileSystemItem?.Handle == null)
                            success = await ShowSave();
                        else
                            success = await WriteFile(_fileSystemItem, _textBox.Text);
                        if (success)
                            _unsavedChanges = false;
                        break;
                    case 1: // Don't Save
                        _unsavedChanges = false;
                        break;
                    default: // Cancel
                        break;
                }
                taskCompletionSource.TrySetResult(success);
            };
            ShowDialog(multiChoice);

            return taskCompletionSource.Task;
        }

        private async Task<bool> ReadFile(FileSystemItem fileItem)
        {
            _fileSystemItem = fileItem;
            _textBox.Text = "";

#if BLAZORGL
            FileSystemFileHandle fileSystemFileHandle = _fileSystemItem.Handle as FileSystemFileHandle;
            Blob blob = await fileSystemFileHandle.GetFile();
            if (blob != null)
            {
                string text = await blob.Text();
                if (text != null)
                {
                    _textBox.Text = text;
                    _unsavedChanges = false;
                    return true;
                }
            }
#endif

            return false;
        }

        private async Task<bool> WriteFile(FileSystemItem fileItem, string text)
        {
            _fileSystemItem = fileItem;

#if BLAZORGL
            FileSystemFileHandle fileSystemFileHandle = _fileSystemItem.Handle as FileSystemFileHandle;
            if (fileSystemFileHandle == null)
            {
                FileSystemDirectoryHandle directoryHandle = _fileSystemItem.Parent.Handle as FileSystemDirectoryHandle;
                FileSystemFileHandle newFileHandle = await directoryHandle.GetFileHandle(_fileSystemItem.Name, true);
                if (newFileHandle != null)
                {
                    fileSystemFileHandle = newFileHandle;
                    _fileSystemItem = new FileSystemItem(newFileHandle, _fileSystemItem.Parent);
                }
            }
            if (fileSystemFileHandle == null)
                return false;

            FileSystemWritableFileStream writableFileStream = await fileSystemFileHandle.CreateWritable();
            if (writableFileStream != null)
            {
                byte[] data = UTF8Encoding.UTF8.GetBytes(text);
                bool writeSuccess = await writableFileStream.Write(data);
                if (writeSuccess)
                {
                    await writableFileStream.Truncate((ulong)data.LongLength);
                    await writableFileStream.Close();
                    return true;
                }
            }
#endif

            return false;
        }

        protected override void Close()
        {
            if (_unsavedChanges)
            {
                CloseRequest = false;
                Task<bool> showUnsavedChangesTask = ShowUnsavedChanges();
                showUnsavedChangesTask.ContinueWith(t =>
                {
                    if (!_unsavedChanges)
                        CloseRequest = true;
                });
                return;
            }

            base.Close();
        }
    }

    internal class KniopadTextBox : TextBox
    {
        internal void Cut() => base.HandleCut();
        internal void Copy() => base.HandleCopy();
        internal void Paste() => base.HandlePaste();
        internal float WrappedTextHeight => coreTextObject.WrappedTextHeight;
    }
}
