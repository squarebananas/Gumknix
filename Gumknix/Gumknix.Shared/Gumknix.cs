using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using MonoGameGum;
using RenderingLibrary;
using RenderingLibrary.Graphics;

#if BLAZORGL
using nkast.Wasm.Dom;
using nkast.Wasm.FileSystem;
using nkast.Wasm.HTMLDragAndDrop;
using nkast.Wasm.Storage;
#endif

namespace Gumknix
{
    public class Gumknix
    {
        public GameServiceContainer GameServiceContainer { get; init; }
        public GameTime GameTime { get; set; }

        public Desktop Desktop { get; private set; }
        public TaskBar TaskBar { get; private set; }
        public List<BaseApplet> RunningApplets { get; private set; }
        public BaseApplet FocusedApplet { get; private set; }
        public Layer TooltipLayer { get; private set; }

        public List<Type> AvailableApplets { get; private set; }

        public FileSystemItem RootStorage { get; private set; }
        public FileSystemItem DesktopStorage { get; private set; }
        public FileSystemItem DocumentsStorage { get; private set; }
        public FileSystemItem AppletFilesStorage { get; private set; }
        public FileSystemItem AppletUserDataStorage { get; private set; }
        public FileSystemItem SystemStorage { get; private set; }
        public FileSystemItem RecycleBinStorage { get; private set; }

        public List<object> ClipboardItems { get; set; } = []; // todo replace with Clipboard api
        public bool ClipboardIsCut { get; set; }
        public FileSystemItem LastFileItemDropped { get; private set; }

        public static Dictionary<string, FileSystemItem.DefaultAppletInfo> ExtensionsDefaultApplets { get; private set; }

#if BLAZORGL
        private HTMLElementExtensionDragAndDrop _htmlElementExtDragAndDrop;
#endif

        public SettingsThemes SettingsThemes;

        public EmbeddedMode EmbeddedMode { get; private set; }

        public Gumknix(GameServiceContainer gameServiceContainer)
        {
            GameServiceContainer = gameServiceContainer;

            AvailableApplets = [
                typeof(AppletKniopad),
                typeof(AppletFileCabiKnit),
                typeof(AppletKniSCode),
                typeof(AppletGuminal),
                typeof(AppletGumternetExplorer),
                typeof(AppletGumToolbox)
                ];

            SetupSystemStorage();
            SetupDefaultExtensions();

            Desktop = new Desktop(this);
            TaskBar = new TaskBar(this);
            RunningApplets = [];

            TooltipLayer = new();
            GumService.Default.Renderer.AddLayer(TooltipLayer);

            SetupDragDrop();

            SettingsThemes = new(this);

            EmbeddedMode = new(this);

            Uri uri = new(Program.NavigationManager.Uri);
            string queryString = uri.Query;


        }

        public void UpdatePreGum(GameTime gameTime)
        {
            GameTime = gameTime;

            EmbeddedMode.Update(gameTime);

            Desktop.Update();
            TaskBar.Update();

            for (int i = 0; i < RunningApplets.Count; i++)
            {
                BaseApplet runningApplet = RunningApplets[i];
                runningApplet.Update();

                if (runningApplet.IsClosed)
                {
                    if (FocusedApplet == runningApplet)
                        FocusedApplet = null;

                    RunningApplets.RemoveAt(i);
                    i--;
                }
            }

            object objectUnderCursor = ObjectUnderCursor();
            if (GumService.Default.Cursor.PrimaryPush)
            {
                if (objectUnderCursor is BaseApplet)
                    (objectUnderCursor as BaseApplet).MoveToFrontRequest = true;
                if (objectUnderCursor is BaseDialog)
                {
                    BaseApplet parentApplet = (objectUnderCursor as BaseDialog).SystemVisual as BaseApplet;
                    if (parentApplet != null)
                        parentApplet.MoveToFrontRequest = true;
                }
            }
        }

        public void UpdatePostGum()
        {
            for (int i = 0; i < RunningApplets.Count; i++)
                RunningApplets[i].PostGumUpdate();
        }

        public void Draw(GameTime gameTime)
        {
            GameTime = gameTime;

            for (int i = 0; i < RunningApplets.Count; i++)
                RunningApplets[i].Draw();
        }

        public BaseApplet StartApplet(Type appletType, object[] arguments = null)
        {
            BaseApplet applet = Activator.CreateInstance(appletType, [this, arguments ?? null]) as BaseApplet;
            RunningApplets.Add(applet);
            TaskBar.AddRunningApplet(applet);
            applet.MoveToFront();
            return applet;
        }

        public void FocusApplet(BaseApplet applet)
        {
            FocusedApplet = applet;
        }

        public void UnfocusApplet()
        {
            FocusedApplet = null; // todo next layer in focus
        }

        internal void TooltipsMoveToFront()
        {
            List<IRenderableIpso> renderables = new(TooltipLayer.Renderables);

            for (int i = 0; i < renderables.Count; i++)
                TooltipLayer.Remove(TooltipLayer.Renderables[i]);

            GumService.Default.Renderer.RemoveLayer(TooltipLayer);
            TooltipLayer = new();
            GumService.Default.Renderer.AddLayer(TooltipLayer);

            for (int i = 0; i < renderables.Count; i++)
                TooltipLayer.Add(renderables[i]);
        }

        public object ObjectUnderCursor()
        {
            for (int i = GumService.Default.Renderer.Layers.Count - 1; i >= 0; i--)
            {
                Layer layer = GumService.Default.Renderer.Layers[i];
                for (int j = 0; j < layer.Renderables.Count; j++)
                {
                    IRenderableIpso renderable = layer.Renderables[j];
                    if ((renderable.Visible == false) ||
                        (renderable.HasCursorOver(GumService.Default.Cursor.X, GumService.Default.Cursor.Y) == false))
                        continue;

                    if (layer == TaskBar.Layer)
                        return TaskBar;

                    for (int k = 0; k < RunningApplets.Count; k++)
                    {
                        BaseApplet applet = RunningApplets[k];
                        if (layer == applet.Layer)
                        {
                            for (int l = 0; l < applet.Dialogs.Count; l++)
                            {
                                BaseDialog dialog = applet.Dialogs[l];
                                if (dialog.Window.Visual.HasCursorOver(GumService.Default.Cursor.X, GumService.Default.Cursor.Y))
                                    return dialog;
                            }

                            return applet;
                        }
                    }
                }
            }
            return Desktop;
        }

        public FileSystemItem CursorOverFileItem()
        {
            object objectUnderCursor = ObjectUnderCursor();
            if (objectUnderCursor == Desktop)
                return Desktop.CursorOverFileItem();
            else if (objectUnderCursor is BaseApplet applet)
                return applet.CursorOverFileItem();
            else if (objectUnderCursor is BaseDialog dialog)
                return dialog.CursorOverFileItem();
            return null;
        }

        public void FilesDropped(List<FileSystemItem> fileSystemItems, bool userRequestedCopy, bool userRequestedMove)
        {
            object objectUnderCursor = ObjectUnderCursor();
            if (objectUnderCursor == Desktop)
                Desktop.FilesDropped(fileSystemItems, userRequestedCopy, userRequestedMove);
            else if (objectUnderCursor is BaseApplet applet)
                applet.FilesDropped(fileSystemItems, userRequestedCopy, userRequestedMove);
            else if (objectUnderCursor is BaseDialog dialog)
                dialog.FilesDropped(fileSystemItems, userRequestedCopy, userRequestedMove);
            else
                return;

            LastFileItemDropped = fileSystemItems[0];
        }

        private void SetupSystemStorage()
        {
#if BLAZORGL
            StorageManager storageManager = StorageManager.FromNavigator();
            Task<FileSystemDirectoryHandle> rootStorageTask = storageManager.GetDirectory();
            rootStorageTask.ContinueWith(t =>
            {
                if (t.IsCompletedSuccessfully && t.Result != null)
                {
                    FileSystemDirectoryHandle rootStorageHandle = t.Result;
                    RootStorage = new(rootStorageHandle, parent: null, alias: "Browser Storage")
                    {
                        OriginPrivateFileSystem = true
                    };

                    Task<FileSystemDirectoryHandle> systemStorageTask = rootStorageHandle.GetDirectoryHandle("Gumknix Virtual OS", true);
                    systemStorageTask.ContinueWith(t =>
                    {
                        if (t.IsCompletedSuccessfully && t.Result != null)
                        {
                            FileSystemDirectoryHandle systemStorageHandle = t.Result;
                            SystemStorage = new(systemStorageHandle, RootStorage);

                            Task<FileSystemDirectoryHandle> appletFilesStorageTask = systemStorageHandle.GetDirectoryHandle("Applet Files", true);
                            appletFilesStorageTask.ContinueWith(t =>
                            {
                                if (t.IsCompletedSuccessfully && t.Result != null)
                                {
                                    FileSystemDirectoryHandle appletFilesStorageHandle = t.Result;
                                    AppletFilesStorage = new(appletFilesStorageHandle, SystemStorage);
                                }
                            });

                            Task<FileSystemDirectoryHandle> appletUserDataStorageTask = systemStorageHandle.GetDirectoryHandle("Applet User Data", true);
                            appletUserDataStorageTask.ContinueWith(t =>
                            {
                                if (t.IsCompletedSuccessfully && t.Result != null)
                                {
                                    FileSystemDirectoryHandle appletUserDataStorageHandle = t.Result;
                                    AppletUserDataStorage = new(appletUserDataStorageHandle, SystemStorage);
                                }
                            });
                        }
                    });

                    Task<FileSystemDirectoryHandle> desktopStorageTask = rootStorageHandle.GetDirectoryHandle("Desktop", true);
                    desktopStorageTask.ContinueWith(t =>
                    {
                        if (t.IsCompletedSuccessfully && t.Result != null)
                        {
                            FileSystemDirectoryHandle desktopStorageHandle = t.Result;
                            DesktopStorage = new(desktopStorageHandle, RootStorage);
                        }
                    });


                    Task<FileSystemDirectoryHandle> documentsStorageTask = rootStorageHandle.GetDirectoryHandle("Documents", true);
                    documentsStorageTask.ContinueWith(t =>
                    {
                        if (t.IsCompletedSuccessfully && t.Result != null)
                        {
                            FileSystemDirectoryHandle documentsStorageHandle = t.Result;
                            DocumentsStorage = new(documentsStorageHandle, RootStorage);
                        }
                    });

                    Task<FileSystemDirectoryHandle> recycleBinStorageTask = rootStorageHandle.GetDirectoryHandle("Recycle Bin", true);
                    recycleBinStorageTask.ContinueWith(t =>
                    {
                        if (t.IsCompletedSuccessfully && t.Result != null)
                        {
                            FileSystemDirectoryHandle recycleBinHandle = t.Result;
                            RecycleBinStorage = new(recycleBinHandle, RootStorage);
                        }
                    });
                }
            });
#endif
        }

        private void SetupDefaultExtensions()
        {
            ExtensionsDefaultApplets = new Dictionary<string, FileSystemItem.DefaultAppletInfo>
            {
                { ".txt", new(typeof(AppletKniopad), AppletKniopad.DefaultIcon) },

                { ".c", new(typeof(AppletKniSCode), AppletKniSCode.DefaultIcon) },
                { ".cpp", new(typeof(AppletKniSCode), AppletKniSCode.DefaultIcon) },
                { ".cs", new(typeof(AppletKniSCode), "\uF0DB") },
                { ".css", new(typeof(AppletKniSCode), AppletKniSCode.DefaultIcon) },
                { ".htm", new(typeof(AppletKniSCode), AppletKniSCode.DefaultIcon) },
                { ".html", new(typeof(AppletKniSCode), AppletKniSCode.DefaultIcon) },
                { ".js", new(typeof(AppletKniSCode), AppletKniSCode.DefaultIcon) },
                { ".json", new(typeof(AppletKniSCode), AppletKniSCode.DefaultIcon) },
                { ".md", new(typeof(AppletKniSCode), AppletKniSCode.DefaultIcon) },
                { ".py", new(typeof(AppletKniSCode), AppletKniSCode.DefaultIcon) },
                { ".ts", new(typeof(AppletKniSCode), AppletKniSCode.DefaultIcon) },
                { ".xml", new(typeof(AppletKniSCode), AppletKniSCode.DefaultIcon) }
            };
        }

        private void SetupDragDrop()
        {
#if BLAZORGL
            nkast.Wasm.Canvas.Canvas canvas = Window.Current.Document.GetElementById<nkast.Wasm.Canvas.Canvas>("theCanvas");
            _htmlElementExtDragAndDrop = canvas.GetExtensionDragAndDrop();
            _htmlElementExtDragAndDrop.DragEnter += (s, e) =>
            {
                e.EffectAllowed = DataTransferEffects.None;
                e.DropEffect = DataTransferEffects.None;
            };

            _htmlElementExtDragAndDrop.Drop += (s, e) =>
            {
                DataTransferItemList dti = e.Items;
                DataTransferItem it = dti[0];

                DataTransferItemKind kind = it.Kind;
                string type = it.Type;

                if (kind == DataTransferItemKind.String)
                {
                    it.GetAsString().ContinueWith(t =>
                    {
                        if (t.IsCompletedSuccessfully && t.Result != null)
                        {
                            string text = t.Result;
                        }
                    });
                    return;
                }

                Task<FileSystemHandle> handleTask = it.GetAsFileSystemHandle();
                handleTask.ContinueWith(t =>
                {
                    if (t.IsCompletedSuccessfully && t.Result != null)
                    {
                        FileSystemHandle fileSystemFileHandle = t.Result;
                        if (fileSystemFileHandle == null)
                            return;

                        FileSystemItem fileSystemItem = new(fileSystemFileHandle, null);
                        FilesDropped([fileSystemItem], GumService.Default.Keyboard.IsCtrlDown, GumService.Default.Keyboard.IsShiftDown);
                    }
                });
            };
#endif
        }

        public static Dictionary<string, string> GetParameterDictionary(string queryString)
        {
            Dictionary<string, string> parameters = [];
            if (string.IsNullOrWhiteSpace(queryString))
                return parameters;

            if (queryString.StartsWith("?"))
                queryString = queryString.Substring(1);

            string[] pairs = queryString.Split('&', StringSplitOptions.RemoveEmptyEntries);
            foreach (string pair in pairs)
            {
                string[] keyValuePair = pair.Split('=', 2);
                string key = Uri.UnescapeDataString(keyValuePair[0]);
                string value = keyValuePair.Length == 2 ? Uri.UnescapeDataString(keyValuePair[1]) : string.Empty;
                parameters[key] = value;
            }
            return parameters;
        }
    }
}
