using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Forms;
using Gum.Forms.Controls;
using Gum.Wireframe;
using MonoGameGum;
using RenderingLibrary.Graphics;

#if BLAZORGL
using nkast.Wasm.Dom;
using nkast.Wasm.File;
using nkast.Wasm.FileSystem;
#endif

namespace Gumknix
{
    public class AppletGuminal : BaseApplet
    {
        public static readonly string DefaultTitle = "Guminal";
        public static readonly string DefaultIcon = "\uEE6F";

        //public Menu Menu;

        private ScrollViewer _textBoxScrollViewer;
        private KniopadTextBox _textBox;

        public AppletGuminal(Gumknix gumknix, object[] args = null) : base(gumknix, args)
        {
            base.Initialize(DefaultTitle, DefaultIcon);

            //Menu = new();
            //MainStackPanel.Visual.AddChild(Menu);

            //MenuItem menuItemFile = new();
            //menuItemFile.Header = "File";
            //Menu.Items.Add(menuItemFile);

            _textBoxScrollViewer = new();
            _textBoxScrollViewer.Visual.Dock(Dock.Fill);
            _textBoxScrollViewer.Visual.Anchor(Anchor.TopLeft);
            _textBoxScrollViewer.Visual.Height -= 32;
            _textBoxScrollViewer.Visual.ClipsChildren = true;
            MainStackPanel.AddChild(_textBoxScrollViewer);

            _textBox = new();
            _textBox.Visual.Dock(Dock.FillHorizontally);
            _textBox.Visual.Anchor(Anchor.TopLeft);
            _textBox.Visual.HeightUnits = DimensionUnitType.Absolute;
            _textBox.TextWrapping = TextWrapping.Wrap;
            _textBox.Visual.TextOverflowVerticalMode = TextOverflowVerticalMode.SpillOver;
            _textBox.Placeholder = null;
            _textBox.AcceptsTab = true;
            //_textBox.Text = "";
            _textBox.Text = "ffmpeg -i bgMusicID.wav output.ogg";
            //_textBox.Text = "ffmpeg -i input.webm output.mp4";
            List<StateSave> states = _textBox.Visual.Categories["TextBoxCategory"].States;
            for (int i = 0; i < states.Count; i++)
            {
                if (states[i].Variables.GetVariableSave("Background.Color") != null)
                    states[i].Variables.GetVariableSave("Background.Color").Value = new Color(32, 32, 32);
            }
            _textBoxScrollViewer.AddChild(_textBox);
            _textBox.UpdateState();
        }

        public override void Update()
        {
            _textBox.Visual.Height = Math.Max(Window.ActualHeight - 36, _textBox.WrappedTextHeight);
            _textBox.Visual.HeightUnits = DimensionUnitType.Absolute;

            if (GumService.Default.Keyboard.KeyPushed(Keys.Enter))
            {
                string lastLine = _textBox.Text?.Split(["\r\n", "\n"], StringSplitOptions.None).Last();
                if (lastLine?.Length >= 1)
                {
                    string ffmpegCommand = "ffmpeg";
                    if (lastLine.StartsWith(ffmpegCommand))
                    {
#if BLAZORGL
                        ModuleFFmpeg ffmpeg = ModuleFFmpeg.Create();
                        ffmpeg.OnInitialized += (s, e) =>
                        {
                            string command = lastLine.Substring(ffmpegCommand.Length).Trim();
                            FileSystemFileHandle fileHandle = GumknixInstance.LastFileItemDropped?.Handle as FileSystemFileHandle;
                            if (fileHandle == null)
                                return;

                            Task<File> inputFileTask = fileHandle.GetFile();
                            inputFileTask.ContinueWith(t =>
                            {
                                if (t.IsCompletedSuccessfully)
                                {
                                    File inputFile = t.Result;
                                    ffmpeg.Run(command, inputFile);
                                }
                            });
                        };
                        ffmpeg.OnLog += (sender, message) =>
                        {
                            _textBox.Text += "\n" + message;
                            _textBoxScrollViewer.ScrollToBottom();
                        };
                        ffmpeg.OnComplete += (sender, blobUid) =>
                        {
                            FileSystemDirectoryHandle desktopStorageHandle = GumknixInstance.DesktopStorage.Handle as FileSystemDirectoryHandle;
                            Task<FileSystemFileHandle> fileHandleTask = desktopStorageHandle.GetFileHandle("output.ogg", true);
                            fileHandleTask.ContinueWith(t =>
                            {
                                if (t.IsCompletedSuccessfully)
                                {
                                    FileSystemFileHandle fileSystemFileHandle = t.Result;
                                    Task<FileSystemWritableFileStream> writableFileStream = fileSystemFileHandle.CreateWritable();
                                    writableFileStream.ContinueWith(t =>
                                    {
                                        if (t.IsCompletedSuccessfully && t.Result != null)
                                        {
                                            FileSystemWritableFileStream writableFileStream = t.Result;
                                            Blob blob = new Blob(blobUid);
                                            Task writeTask = writableFileStream.Write(blob);
                                            writeTask.ContinueWith(t =>
                                            {
                                                if (t.IsCompletedSuccessfully)
                                                {
                                                    writableFileStream.Truncate((ulong)blob.Size).ContinueWith(t =>
                                                    {
                                                        if (t.IsCompletedSuccessfully)
                                                        {
                                                            writableFileStream.Close();

                                                            FileSystemItem fileSystemItem = new(fileSystemFileHandle,
                                                                GumknixInstance.DesktopStorage);
                                                            GumknixInstance.DesktopStorage.AddChild(fileSystemItem);

                                                            DesktopIcon desktopIcon = new(GumknixInstance.Desktop, fileSystemItem);
                                                            desktopIcon.Click += (s, e) =>
                                                            {
                                                                if (GumService.Default.Cursor.PrimaryDoubleClick)
                                                                    GumknixInstance.StartApplet(typeof(AppletKniopad), [fileSystemItem]);
                                                            };
                                                            GumknixInstance.Desktop.AddIcon(desktopIcon);
                                                        }
                                                    });
                                                }
                                            });
                                        }
                                    });
                                }
                            });

                            _textBox.Text += "\nComplete";
                            _textBoxScrollViewer.ScrollToBottom();
                        };

                        ffmpeg.Initialize();
#endif
                    }
                }
            }

            base.Update();
        }

        protected override void Close()
        {
            base.Close();
        }
    }
}
