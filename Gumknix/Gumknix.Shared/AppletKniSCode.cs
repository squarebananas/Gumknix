using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Emit;

using Microsoft.Xna.Framework;

using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Forms;
using Gum.Forms.Controls;
using Gum.Forms.DefaultVisuals;
using Gum.Wireframe;
using MonoGameGum;
using MonoGameGum.GueDeriving;
using RenderingLibrary.Graphics;

using MetadataReferenceService.Abstractions.Types;
using MetadataReferenceService.BlazorWasm;

#if BLAZORGL
using nkast.Wasm.FileSystem;
#endif

using Gumknix.KniSBuild;

namespace Gumknix
{
    public class AppletKniSCode : BaseApplet
    {
        public static readonly string DefaultTitle = "KniS Code";
        public static readonly string DefaultIcon = "\uEE71";

#if BLAZORGL
        private ModuleMonaco _monaco { get; set; }

        public ModuleMonaco.LanguageDefinition[] LanguageDefinitions { get; private set; }
#endif

        private ColoredRectangleRuntime _background;
        private Menu _menu;
        private StackPanel _stackPanel;
        private StackPanel _mainToolbar;
        private Button compileButton;
        private ComboBox optimizationLevelComboBox;

        private StackPanel _innerPanel;
        private StackPanel _innerPanelLeft;
        private Splitter _innerPanelSplitter;
        private StackPanel _innerPanelRight;
        private float _innerPanelRightTargetWidth;

        private HTMLViewContainer _textEditorContainer;
        private Splitter _textEditorSplitter;
        private ScrollViewer _outputPanelScrollViewer;
        private KniopadTextBox _outputPanel;
        private float _outputPanelTargetHeight;

        private ListBox _solutionFilesListBox;
        private Splitter _solutionFilesSplitter;
        private ListBox _propertiesListBox;
        private float _propertiesListBoxTargetHeight;

        private Dictionary<string, string> sourceFiles = [];
        private string selectedSourceFilePath;

        private static BlazorWasmMetadataReferenceService _referenceService;

        public OptimizationLevel OptimizationLevel { get; private set; } = OptimizationLevel.Release;

        private Assembly _loadedAssembly;

        public string TempForceLoad; // todo replace

        RectangleRuntime rectangle;

        string generatedCode;

        public AppletKniSCode(Gumknix gumknix, object[] args = null) : base(gumknix, args)
        {
            TempForceLoad = Microsoft.Xna.Framework.Media.VideoSoundtrackType.Dialog.ToString();

            base.Initialize(DefaultTitle, DefaultIcon, 1500, 800);

            Window.Visual.MinWidth = 310;
            Window.Visual.MinHeight = 310;

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
            //menuItemFileNew.Clicked += async (s, e) =>
            //{
            //    if (_unsavedChanges)
            //        await ShowUnsavedChanges();
            //    if (!_unsavedChanges)
            //    {
            //        _textBox.Text = "";
            //        _textBox.CaretIndex = 0;
            //        _fileSystemItem = null;
            //    }
            //};

            MenuItem menuItemFileOpen = new();
            menuItemFileOpen.Header = "Open";
            menuItemFileOpen.Dock(Dock.FillHorizontally);
            menuItemFile.Items.Add(menuItemFileOpen);
            //menuItemFileOpen.Clicked += (s, e) => ShowOpen();

            MenuItem menuItemFileSave = new();
            menuItemFileSave.Header = "Save";
            menuItemFileSave.Dock(Dock.FillHorizontally);
            menuItemFile.Items.Add(menuItemFileSave);
            //menuItemFileSave.Clicked += async (s, e) =>
            //{
            //    if (_fileSystemItem?.Handle == null)
            //        await ShowSave();
            //    else
            //        await WriteFile(_fileSystemItem, _textBox.Text);
            //};

            MenuItem menuItemFileSaveAs = new();
            menuItemFileSaveAs.Header = "Save As";
            menuItemFileSaveAs.Dock(Dock.FillHorizontally);
            //menuItemFileSaveAs.Clicked += (s, e) => ShowSave();
            menuItemFile.Items.Add(menuItemFileSaveAs);

            MenuItem menuItemFileShare = new();
            menuItemFileShare.Header = "Save As";
            menuItemFileShare.Dock(Dock.FillHorizontally);
            menuItemFileShare.Clicked += (s, e) => CreateShareLink();
            menuItemFile.Items.Add(menuItemFileShare);

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
            //menuItemEditCut.Clicked += (s, e) => _textBox.Cut();

            MenuItem menuItemEditCopy = new();
            menuItemEditCopy.Header = "Copy";
            menuItemEditCopy.Dock(Dock.FillHorizontally);
            menuItemEdit.Items.Add(menuItemEditCopy);
            //menuItemEditCopy.Clicked += (s, e) => _textBox.Copy();

            MenuItem menuItemEditPaste = new();
            menuItemEditPaste.Header = "Paste";
            menuItemEditPaste.Dock(Dock.FillHorizontally);
            menuItemEdit.Items.Add(menuItemEditPaste);
            //menuItemEditPaste.Clicked += (s, e) => _textBox.Paste();

            MenuItem menuItemEditSelectAll = new();
            menuItemEditSelectAll.Header = "Select All";
            menuItemEditSelectAll.Dock(Dock.FillHorizontally);
            menuItemEdit.Items.Add(menuItemEditSelectAll);
            //menuItemEditSelectAll.Clicked += (s, e) => _textBox.SelectAll();

            MenuItem menuItemView = new();
            menuItemView.Header = "View";
            _menu.Items.Add(menuItemView);

            MenuItem menuItemViewWordWrap = new();
            menuItemViewWordWrap.Header = "Word Wrap";
            menuItemViewWordWrap.Visual.Width = 220;
            menuItemViewWordWrap.Visual.WidthUnits = DimensionUnitType.Absolute;
            menuItemView.Items.Add(menuItemViewWordWrap);

            MenuItem menuItemViewGenerated = new();
            menuItemViewGenerated.Header = "Generated .cs";
            menuItemViewGenerated.Visual.Width = 220;
            menuItemViewGenerated.Visual.WidthUnits = DimensionUnitType.Absolute;
            menuItemView.Items.Add(menuItemViewGenerated);
            menuItemViewGenerated.Clicked += (s, e) => gumknix.StartApplet(typeof(AppletKniopad), [generatedCode]);

            MenuItem menuItemTemp = new();
            menuItemTemp.Header = "Reflect";
            menuItemTemp.Visual.Width = 220;
            menuItemTemp.Visual.WidthUnits = DimensionUnitType.Absolute;
            menuItemView.Items.Add(menuItemTemp);
            menuItemTemp.Clicked += (s, e) => ObjectInfoPanel(typeof(Microsoft.Xna.Framework.Graphics.BasicEffect));

            _background = new();
            _background.Color = new Color(32, 32, 32);
            _background.Dock(Dock.Fill);
            _background.Anchor(Anchor.TopLeft);
            _background.Height = -(TitleBarHeight + _menu.ActualHeight);
            _background.HeightUnits = DimensionUnitType.RelativeToParent;
            MainStackPanel.Visual.AddChild(_background);

            _stackPanel = new();
            _stackPanel.Orientation = Orientation.Vertical;
            _stackPanel.Visual.Dock(Dock.Fill);
            _stackPanel.Visual.Anchor(Anchor.TopLeft);
            _stackPanel.Visual.HeightUnits = DimensionUnitType.RelativeToParent;
            _stackPanel.Visual.ChildrenLayout = Gum.Managers.ChildrenLayout.TopToBottomStack;
            _background.AddChild(_stackPanel);

            _mainToolbar = new();
            _mainToolbar.Orientation = Orientation.Vertical;
            _mainToolbar.Dock(Dock.FillHorizontally);
            _mainToolbar.Visual.ChildrenLayout = Gum.Managers.ChildrenLayout.LeftToRightStack;
            _stackPanel.AddChild(_mainToolbar);

            compileButton = new();
            compileButton.Text = "Compile";
            compileButton.Visual.X = 5;
            compileButton.Visual.Y = 5;
            compileButton.Click += (s, e) =>
            {
                compileButton.IsEnabled = false;
                Task task = Compile();
                task.ContinueWith(t => { compileButton.IsEnabled = true; });
            };
            _mainToolbar.AddChild(compileButton);

            optimizationLevelComboBox = new();
            optimizationLevelComboBox.Items.Add("Debug");
            optimizationLevelComboBox.Items.Add("Release");
            optimizationLevelComboBox.SelectedIndex = 1;
            _mainToolbar.AddChild(optimizationLevelComboBox);

            _innerPanel = new();
            _innerPanel.Orientation = Orientation.Horizontal;
            _innerPanel.Visual.Dock(Dock.Fill);
            _innerPanel.Visual.Anchor(Anchor.TopLeft);
            _innerPanel.Visual.Height = -_mainToolbar.ActualHeight;
            _innerPanel.Visual.HeightUnits = DimensionUnitType.RelativeToParent;
            _innerPanel.Visual.ChildrenLayout = Gum.Managers.ChildrenLayout.LeftToRightStack;
            _stackPanel.AddChild(_innerPanel);

            _innerPanelLeft = new();
            _innerPanelLeft.Orientation = Orientation.Vertical;
            _innerPanelLeft.Visual.Dock(Dock.FillVertically);
            _innerPanelLeft.Visual.Anchor(Anchor.TopLeft);
            _innerPanelLeft.Visual.Width = 1200;
            _innerPanelLeft.Visual.WidthUnits = DimensionUnitType.Absolute;
            _innerPanelLeft.Visual.MinWidth = 150;
            _innerPanelLeft.Visual.ChildrenLayout = Gum.Managers.ChildrenLayout.TopToBottomStack;
            _innerPanel.AddChild(_innerPanelLeft);

            _textEditorContainer = new(GumknixInstance, Window);
            _textEditorContainer.Dock(Dock.FillHorizontally);
            _textEditorContainer.Anchor(Anchor.TopLeft);
            _textEditorContainer.Height = 550;
            _textEditorContainer.HeightUnits = DimensionUnitType.Absolute;
            _textEditorContainer.MinHeight = 100;
            _innerPanelLeft.AddChild(_textEditorContainer);

            _textEditorSplitter = new();
            _textEditorSplitter.Visual.Dock(Dock.FillHorizontally);
            _textEditorSplitter.Visual.Anchor(Anchor.TopLeft);
            _textEditorSplitter.Visual.Height = 2;
            _innerPanelLeft.AddChild(_textEditorSplitter);
            _textEditorSplitter.Visual.Dragging += (s, e) =>
            {
                _outputPanelTargetHeight = MathF.Max(_outputPanelScrollViewer.ActualHeight, _outputPanelScrollViewer.Visual.MinHeight.Value);
            };

            _outputPanelTargetHeight = 200;

            _outputPanelScrollViewer = new();
            _outputPanelScrollViewer.Visual.Dock(Dock.Fill);
            _outputPanelScrollViewer.Visual.Anchor(Anchor.TopLeft);
            _outputPanelScrollViewer.Visual.Height = _outputPanelTargetHeight;
            _outputPanelScrollViewer.Visual.HeightUnits = DimensionUnitType.Absolute;
            _outputPanelScrollViewer.Visual.MinHeight = 100;
            _outputPanelScrollViewer.Visual.ClipsChildren = true;
            _innerPanelLeft.AddChild(_outputPanelScrollViewer);

            _outputPanel = new();
            _outputPanel.Visual.Dock(Dock.Fill);
            _outputPanel.Visual.Anchor(Anchor.TopLeft);
            _outputPanel.Visual.MinHeight = 200;
            (_outputPanel.Visual as TextBoxVisual).TextInstance.HorizontalAlignment = HorizontalAlignment.Left;
            (_outputPanel.Visual as TextBoxVisual).TextInstance.VerticalAlignment = VerticalAlignment.Top;
            _outputPanel.TextWrapping = TextWrapping.Wrap;
            _outputPanel.Visual.TextOverflowVerticalMode = TextOverflowVerticalMode.SpillOver;
            _outputPanel.IsReadOnly = true;
            _outputPanel.Placeholder = null;
            List<StateSave> states = _outputPanel.Visual.Categories["TextBoxCategory"].States;
            for (int i = 0; i < states.Count; i++)
            {
                if (states[i].Variables.GetVariableSave("Background.Color") != null)
                    states[i].Variables.GetVariableSave("Background.Color").Value = new Color(32, 32, 32);
            }
            _outputPanelScrollViewer.AddChild(_outputPanel);
            _outputPanel.UpdateState();

            _innerPanelSplitter = new();
            _innerPanelSplitter.Visual.Dock(Dock.FillVertically);
            _innerPanelSplitter.Visual.Anchor(Anchor.TopLeft);
            _innerPanelSplitter.Visual.Width = 2;
            _innerPanelSplitter.Visual.WidthUnits = DimensionUnitType.Absolute;
            _innerPanel.AddChild(_innerPanelSplitter);
            _innerPanelSplitter.Visual.Dragging += (s, e) =>
            {
                _innerPanelRightTargetWidth = MathF.Max(_innerPanelRight.ActualWidth, _innerPanelRight.Visual.MinWidth.Value);
            };

            _innerPanelRightTargetWidth = 300;

            _innerPanelRight = new();
            _innerPanelRight.Orientation = Orientation.Vertical;
            _innerPanelRight.Visual.Dock(Dock.FillVertically);
            _innerPanelRight.Visual.Anchor(Anchor.TopLeft);
            _innerPanelRight.Visual.Width = _innerPanelRightTargetWidth;
            _innerPanelRight.Visual.WidthUnits = DimensionUnitType.Absolute;
            _innerPanelRight.Visual.MinWidth = 150;
            _innerPanelRight.Visual.ChildrenLayout = Gum.Managers.ChildrenLayout.TopToBottomStack;
            _innerPanel.AddChild(_innerPanelRight);

            _solutionFilesListBox = new();
            _solutionFilesListBox.Visual.Dock(Dock.FillHorizontally);
            _solutionFilesListBox.Visual.Anchor(Anchor.TopLeft);
            _solutionFilesListBox.Visual.Height = 450;
            _solutionFilesListBox.Visual.HeightUnits = DimensionUnitType.Absolute;
            _solutionFilesListBox.Visual.MinHeight = 100;
            _solutionFilesListBox.ListBoxItemFormsType = typeof(SolutionFileListBoxItem);
            _solutionFilesListBox.ItemClicked += async (s, e) =>
            {
                sourceFiles[selectedSourceFilePath] = _monaco.GetText();

                FileSystemItem fileItem = _solutionFilesListBox.SelectedObject as FileSystemItem;
                selectedSourceFilePath = fileItem.Path;
                if (sourceFiles.ContainsKey(selectedSourceFilePath) == false)
                {
                    FileSystemFileHandle fileSystemFileHandle = fileItem.Handle as FileSystemFileHandle;
                    nkast.Wasm.File.File file = await fileSystemFileHandle.GetFile();
                    string text = await file.Text();
                    sourceFiles[selectedSourceFilePath] = text;
                }

                string languageId = GetLanguageNameFromFileExtension(fileItem.Extension);
                _monaco.SetLanguage(languageId);
                _monaco.SetText(sourceFiles[selectedSourceFilePath]);
            };
            _innerPanelRight.AddChild(_solutionFilesListBox);

            _solutionFilesSplitter = new();
            _solutionFilesSplitter.Visual.Dock(Dock.FillHorizontally);
            _solutionFilesSplitter.Visual.Anchor(Anchor.TopLeft);
            _solutionFilesSplitter.Visual.Height = 2;
            _solutionFilesSplitter.Visual.HeightUnits = DimensionUnitType.Absolute;
            _innerPanelRight.AddChild(_solutionFilesSplitter);
            _solutionFilesSplitter.Visual.Dragging += (s, e) =>
            {
                _outputPanelTargetHeight = MathF.Max(_outputPanelScrollViewer.ActualHeight, _outputPanelScrollViewer.Visual.MinHeight.Value);
            };

            _propertiesListBoxTargetHeight = 250;

            _propertiesListBox = new();
            _propertiesListBox.Visual.Dock(Dock.FillHorizontally);
            _propertiesListBox.Visual.Anchor(Anchor.TopLeft);
            _propertiesListBox.Visual.Height = _propertiesListBoxTargetHeight;
            _propertiesListBox.Visual.HeightUnits = DimensionUnitType.Absolute;
            _propertiesListBox.Visual.MinHeight = 100;
            _innerPanelRight.AddChild(_propertiesListBox);

#if BLAZORGL
            _monaco = ModuleMonaco.Create();
            _textEditorContainer.Create(_monaco.Uid.ToString());

            _monaco.OnScriptLoaded += (s, e) => _monaco.InitializeInstance();
            _monaco.OnInstanceLoaded += (s, e) =>
            {
                LanguageDefinitions = _monaco.GetLanguages();
                for (int i = 0; i < LanguageDefinitions.Length; i++)
                    for (int j = 0; j < LanguageDefinitions[i].Extensions?.Length; j++)
                        Gumknix.ExtensionsDefaultApplets.TryAdd(LanguageDefinitions[i].Extensions[j], new(typeof(AppletKniSCode), DefaultIcon));

                ModuleMonaco.LanguageDefinition xmlLanguageDefinition = LanguageDefinitions.Where(l => l.Id == "xml").First();
                xmlLanguageDefinition.AddExtension(".projitems");
                xmlLanguageDefinition.AddExtension(".shproj");

                if (args == null) // remove this
                {
                    Task.Run(async () =>
                    {
                        try
                        {
                            FileSystemItem fileSystemItem = await GumknixInstance.DesktopStorage.GetChildAsync("MicrophoneEcho");
                            fileSystemItem = await fileSystemItem.GetChildAsync("MicrophoneEchoSample.KNI.DesktopGL");
                            fileSystemItem = await fileSystemItem.GetChildAsync("MicrophoneEchoSample.KNI.DesktopGL.csproj");
                            ReadFile(fileSystemItem);
                        }
                        catch { }
                    });

                }

                if (args?.Length >= 1)
                {
                    FileSystemItem fileSystemItem = args[0] as FileSystemItem;
                    if (fileSystemItem != null)
                        ReadFile(fileSystemItem);
                }
                else
                {
                    string text = "";
                    _monaco.SetText(text);
                }
            };

            _monaco.InitializeLoaderScript();

            _referenceService ??= new(Program.NavigationManager);
#endif
        }

        public override void Update()
        {
            if (MathF.Abs(_innerPanel.ActualWidth -
                (_innerPanelLeft.ActualWidth + _innerPanelRight.ActualWidth + _innerPanelSplitter.ActualWidth)) >= 0.1f)
            {
                float leftWidth = _innerPanel.ActualWidth - _innerPanelRightTargetWidth - _innerPanelSplitter.ActualWidth;
                float rightWidth = _innerPanelRightTargetWidth;

                if (_innerPanelLeft.Visual.MinWidth != null && leftWidth < _innerPanelLeft.Visual.MinWidth)
                {
                    leftWidth = (float)_innerPanelLeft.Visual.MinWidth;
                    rightWidth = _innerPanel.ActualWidth - leftWidth - _innerPanelSplitter.ActualWidth;
                }

                _innerPanelLeft.Width = leftWidth;
                _innerPanelRight.Width = rightWidth;
            }

            if (MathF.Abs(_innerPanelLeft.ActualHeight -
                (_textEditorContainer.GetAbsoluteHeight() + _outputPanelScrollViewer.ActualHeight + _textEditorSplitter.ActualHeight)) >= 0.1f)
            {
                float topHeight = _innerPanelLeft.ActualHeight - _outputPanelTargetHeight - _textEditorSplitter.ActualHeight;
                float bottomHeight = _outputPanelTargetHeight;

                if (_textEditorContainer.MinHeight != null && topHeight < _textEditorContainer.MinHeight)
                {
                    topHeight = (float)_textEditorContainer.MinHeight;
                    bottomHeight = _innerPanelLeft.ActualHeight - topHeight - _textEditorSplitter.ActualHeight;
                }

                _textEditorContainer.Height = topHeight;
                _outputPanelScrollViewer.Height = bottomHeight;
            }

            _outputPanel.Visual.Height = Math.Max(_outputPanelScrollViewer.ActualHeight - 15, _outputPanel.WrappedTextHeight);
            _outputPanel.Visual.HeightUnits = DimensionUnitType.Absolute;


            if (MathF.Abs(_innerPanelRight.ActualHeight -
                (_solutionFilesListBox.ActualHeight + _propertiesListBox.ActualHeight + _solutionFilesSplitter.ActualHeight)) >= 0.1f)
            {
                float topHeight = _innerPanelRight.ActualHeight - _propertiesListBoxTargetHeight - _solutionFilesSplitter.ActualHeight;
                float bottomHeight = _propertiesListBoxTargetHeight;

                if (_solutionFilesListBox.Visual.MinHeight != null && topHeight < _solutionFilesListBox.Visual.MinHeight)
                {
                    topHeight = (float)_solutionFilesListBox.Visual.MinHeight;
                    bottomHeight = _innerPanelRight.ActualHeight - topHeight - _solutionFilesSplitter.ActualHeight;
                }

                _solutionFilesListBox.Visual.Height = topHeight;
                _propertiesListBox.Height = bottomHeight;
            }

            //if (rectangle == null)
            //{
            //    rectangle = new();
            //    rectangle.AddToRoot();
            //}
            //rectangle.Color = Color.Red;
            //rectangle.Anchor(Anchor.TopLeft);
            //rectangle.X = _innerPanel.AbsoluteLeft;
            //rectangle.Y = _innerPanel.AbsoluteTop;
            //rectangle.Width = _innerPanel.ActualWidth;
            //rectangle.WidthUnits = DimensionUnitType.Absolute;
            //rectangle.Height = _innerPanel.ActualHeight;
            //rectangle.HeightUnits = DimensionUnitType.Absolute;

            base.Update();
        }

        public override void PostGumUpdate()
        {
            _textEditorContainer.Update();
        }

        public async Task Compile()
        {
            _loadedAssembly = null;

            string log = "";

            Assembly[] loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies();
            List<(Assembly assembly, string moduleName, string[] namespaces)> allAssemblyNamespaces = [];
            List<(Assembly assembly, string moduleName, Type[] types)> allAssemblyTypes = [];

            for (int i = 0; i < loadedAssemblies.Length; i++)
            {
                Assembly assembly = loadedAssemblies[i];
                assembly.GetReferencedAssemblies();
                string moduleName = assembly.ManifestModule.Name;

                List<string> namespaces = GetNamespacesFromAssembly(assembly);
                allAssemblyNamespaces.Add((assembly, moduleName.Replace(".dll", ""), namespaces.ToArray()));

                Type[] types = assembly.GetTypes();
                allAssemblyTypes.Add((assembly, moduleName.Replace(".dll", ""), types));
            }

            Dictionary<string, (Assembly assembly, string moduleName)> namespaceToModuleLookup = [];
            for (int i = 0; i < allAssemblyNamespaces.Count; i++)
            {
                (Assembly assembly, string moduleName, string[] namespaces) = allAssemblyNamespaces[i];
                for (int j = 0; j < allAssemblyNamespaces[i].namespaces.Length; j++)
                {
                    string namespaceValue = allAssemblyNamespaces[i].namespaces[j];
                    if (namespaceValue == "Gumknix")
                    {
                        AssemblyName[] referencedAssemblies = assembly.GetReferencedAssemblies();
                    }

                    bool addOrReplace = true;
                    if (namespaceToModuleLookup.TryGetValue(namespaceValue, out (Assembly existingAssembly, string existingModuleName) existingEntry))
                    {
                        addOrReplace = false;
                        AssemblyName[] referencedAssemblies = existingEntry.existingAssembly.GetReferencedAssemblies();
                        for (int k = 0; k < referencedAssemblies.Length; k++)
                        {
                            if (assembly.FullName == referencedAssemblies[k].FullName)
                            {
                                addOrReplace = true;
                                break;
                            }
                        }
                    }
                    if (addOrReplace)
                        namespaceToModuleLookup[namespaceValue] = (assembly, moduleName);
                }
            }

            Dictionary<string, (Assembly assembly, string moduleName)> fullTypeToModuleLookup = [];
            for (int i = 0; i < allAssemblyTypes.Count; i++)
            {
                (Assembly assembly, string moduleName, Type[] types) = allAssemblyTypes[i];
                for (int j = 0; j < types.Length; j++)
                {
                    if (fullTypeToModuleLookup.TryAdd(types[j].FullName, (assembly, moduleName)) == false)
                    {
                    }
                }
            }

            string outputAssemblyName = "InMemoryAssembly";
            string[] preprocessorSymbols = ["BLAZORGL"];

            string sourceCode = _monaco.GetText();
            if (sourceFiles.Count == 0)
                selectedSourceFilePath = "Program.cs";
            sourceFiles[selectedSourceFilePath] = sourceCode;

            CSharpParseOptions cSharpParseOptions = CSharpParseOptions.Default;
            cSharpParseOptions.WithLanguageVersion(LanguageVersion.LatestMajor);
            cSharpParseOptions.WithPreprocessorSymbols(preprocessorSymbols);

            List<SyntaxTree> syntaxTrees = [];
            for (int i = 0; i < sourceFiles.Count; i++)
            {
                KeyValuePair<string, string> keyValuePair = sourceFiles.ElementAt(i);
                if (keyValuePair.Key.EndsWith(".cs"))
                {
                    SyntaxTree fileSyntaxTree = ParseSource(keyValuePair.Value, cSharpParseOptions);
                    syntaxTrees.Add(fileSyntaxTree);

                    generatedCode = fileSyntaxTree.ToString();
                }
            }

            HashSet<string> assembliesRequired = [];
            assembliesRequired.Add("System.Private.CoreLib");
            assembliesRequired.Add("System.Runtime");

            if (true) // to do
                assembliesRequired.Add("System.Console");

            for (int i = 0; i < syntaxTrees.Count; i++)
            {
                SyntaxTree syntaxTree = syntaxTrees[i];
                CompilationUnitSyntax root = syntaxTree.GetRoot() as CompilationUnitSyntax;
                for (int j = 0; j < root.Usings.Count; j++)
                {
                    UsingDirectiveSyntax usingDirectiveSyntax = root.Usings[j];
                    string namespaceName = usingDirectiveSyntax.Name.ToString().Replace("global::", "");
                    if (namespaceToModuleLookup.TryGetValue(namespaceName, out (Assembly assembly, string moduleName) entry))
                    {
                        assembliesRequired.Add(entry.moduleName);
                    }
                }

                IEnumerable<SyntaxNode> nodes = root.DescendantNodes();
                foreach (SyntaxNode node in nodes)
                {
                    QualifiedNameSyntax qualifiedNameNode = node?.Parent as QualifiedNameSyntax;
                    if (qualifiedNameNode?.Right == node)
                    {
                        QualifiedNameSyntax rootQualifiedNameNode = qualifiedNameNode;
                        while ((rootQualifiedNameNode?.Parent as QualifiedNameSyntax) != null)
                            rootQualifiedNameNode = rootQualifiedNameNode?.Parent as QualifiedNameSyntax;
                        string fullQualifiedName = rootQualifiedNameNode.ToString();

                        if (fullTypeToModuleLookup.TryGetValue(fullQualifiedName, out (Assembly assembly, string moduleName) entry))
                        {
                            if (assembliesRequired.Contains(entry.moduleName) == false)
                                assembliesRequired.Add(entry.moduleName);
                        }
                    }
                }
            }

            List<MetadataReference> metadataReferences = [];
            List<string> assemblyNames = assembliesRequired.ToList();
            for (int i = 0; i < assemblyNames.Count; i++)
            {
                AssemblyDetails assemblyDetails = new() { Name = assemblyNames[i] };
                MetadataReference metadataReference = null;
                try
                {
                    metadataReference = await _referenceService.CreateAsync(assemblyDetails);
                }
                catch (Exception e)
                {
                    log += e.Message + "\n";
                }
                metadataReferences.Add(metadataReference);
            }
            MetadataReference[] metadataReferencesArray = metadataReferences.ToArray();

            CSharpCompilationOptions compilationOptions = new(
                outputKind: OutputKind.DynamicallyLinkedLibrary,
                reportSuppressedDiagnostics: true,
                metadataImportOptions: MetadataImportOptions.Public,
                allowUnsafe: true,
                optimizationLevel: OptimizationLevel
            );

            CSharpCompilation compilation = CSharpCompilation.Create(outputAssemblyName, syntaxTrees, metadataReferencesArray, compilationOptions);

            using MemoryStream ILMemoryStream = new();
            EmitResult emitResult = compilation.Emit(ILMemoryStream);
            for (int i = 0; i < emitResult.Diagnostics.Length; i++)
            {
                Diagnostic diagnostic = emitResult.Diagnostics[i];
                Console.WriteLine(diagnostic.ToString());
                log += diagnostic.ToString() + "\n";
            }

            byte[] ILBytes = null;
            if (emitResult.Success)
            {
                ILBytes = ILMemoryStream.ToArray();
                _loadedAssembly = Assembly.Load(ILBytes);
            }

            if (_loadedAssembly != null)
            {
                bool entryPointFound = false;
                Type[] types = _loadedAssembly.GetTypes();
                for (int i = 0; i < types.Length; i++)
                {
                    try
                    {
                        Type type = types[i];
                        MethodInfo autoRunMethod = type.GetMethod("GumknixEntryPoint", BindingFlags.Public | BindingFlags.Static);
                        if (autoRunMethod != null)
                        {
                            entryPointFound = true;
                            autoRunMethod.Invoke(null, [GumknixInstance]);
                        }
                    }
                    catch (Exception e)
                    {
                        log += e.Message;
                    }
                }
                if (!entryPointFound)
                {
                    log += "No entry point found in the assembly.";

#if BLAZORGL
                    FileSystemDirectoryHandle rootHandle = GumknixInstance.RootStorage.Handle as FileSystemDirectoryHandle;
                    FileSystemFileHandle tempFileHandle = await rootHandle.GetFileHandle("ILBytes.temp", create: true);
                    FileSystemWritableFileStream writableFileStream = await tempFileHandle.CreateWritable();
                    await writableFileStream.Write(ILBytes);
                    await writableFileStream.Truncate((ulong)ILBytes.LongLength);
                    await writableFileStream.Close();

                    nkast.Wasm.File.File tempFile = await tempFileHandle.GetFile();
                    string tempFileUrl = nkast.Wasm.Url.Url.CreateObjectURL(tempFile);
                    string escapedTempFileUrl = Uri.EscapeDataString(tempFileUrl);

                    string address = Program.NavigationManager.BaseUri + "?ilurl=" + escapedTempFileUrl;
                    GumknixInstance.StartApplet(typeof(AppletGumternetExplorer), [address]);
#endif
                }
            }

            if (ILBytes != null)
            {
                string ILText = GetILTextFromAssembly(ILBytes);
                log += "\n\n" + ILText;
            }

            List<ModuleMonaco.CompletionItemInfo> allTypeInfos = [];
            for (int i = 0; i < assemblyNames.Count; i++)
            {
                for (int j = 0; j < allAssemblyTypes.Count; j++)
                {
                    if (assemblyNames[i] == allAssemblyTypes[j].moduleName)
                    {
                        List<ModuleMonaco.CompletionItemInfo> typeInfos = GetTypeInfos(allAssemblyTypes[j].types);
                        allTypeInfos.AddRange(typeInfos);
                    }
                }
            }
            _monaco.RegisterCompletionItemProvider(allTypeInfos);

            _outputPanel.Text = log;
            _outputPanel.CaretIndex = 0;
        }

        public SyntaxTree ParseSource(string sourceCode, CSharpParseOptions cSharpParseOptions)
        {
            SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(sourceCode, cSharpParseOptions);

            syntaxTree = ConvertTopLevelStatement(syntaxTree, cSharpParseOptions);

            //bool isGameBaseClassUsed = false;
            //IEnumerable<ClassDeclarationSyntax> classDeclarations = root.DescendantNodes().OfType<ClassDeclarationSyntax>();
            //foreach (ClassDeclarationSyntax classDeclaration in classDeclarations)
            //{
            //    if (classDeclaration.BaseList == null)
            //        continue;

            //    foreach (BaseTypeSyntax baseType in classDeclaration.BaseList.Types)
            //    {
            //        string baseTypeName = baseType.Type.ToString();
            //        if (baseTypeName == "Game" || baseTypeName == "Microsoft.Xna.Framework.Game")
            //        {
            //            isGameBaseClassUsed = true;

            //            string newBaseTypeName = ": Microsoft.Xna.Framework.DrawableGameComponent";
            //            int startIndex = classDeclaration.BaseList.Span.Start;
            //            int length = classDeclaration.BaseList.Span.Length;
            //            sourceCode = sourceCode.Remove(startIndex, length);
            //            sourceCode = sourceCode.Insert(startIndex, newBaseTypeName);
            //            int offset = newBaseTypeName.Length - length;

            //            IEnumerable<ConstructorDeclarationSyntax> constructors =
            //                classDeclaration.DescendantNodes().OfType<ConstructorDeclarationSyntax>();
            //            foreach (ConstructorDeclarationSyntax constructor in constructors)
            //            {
            //                if (constructor.ParameterList.Parameters.Count == 0)
            //                {
            //                    string newParameterList = "(Microsoft.Xna.Framework.Game game) : base(game)";
            //                    startIndex = constructor.ParameterList.Span.Start + offset;
            //                    length = constructor.ParameterList.Span.Length;
            //                    sourceCode = sourceCode.Remove(startIndex, length);
            //                    sourceCode = sourceCode.Insert(startIndex, newParameterList);
            //                    offset += newParameterList.Length - length;
            //                    break;
            //                }
            //            }

            //            string newClassCode =
            //                """

            //                public static void GumknixEntryPoint(global::Gumknix.Gumknix gumknix)
            //                {
            //                    Microsoft.Xna.Platform.GameStrategy gameStrategy = gumknix.GameServiceContainer.GetService(
            //                        typeof(Microsoft.Xna.Platform.GameStrategy)) as Microsoft.Xna.Platform.GameStrategy;
            //                    DrawableGameComponent testGame = new 
            //                """;
            //            newClassCode += classDeclaration.Identifier.Text;
            //            newClassCode +=
            //                """
            //                (gameStrategy.Game);
            //                    gumknix.StartApplet(typeof(AppletKniGameComponentRunner), [testGame]);
            //                }
                                
            //                """;

            //            sourceCode = sourceCode.Insert(classDeclaration.Span.End - 1 + offset, newClassCode);
            //            syntaxTree = CSharpSyntaxTree.ParseText(sourceCode, cSharpParseOptions);
            //            break;
            //        }
            //    }
            //}

            return syntaxTree;
        }

        private SyntaxTree ConvertTopLevelStatement(SyntaxTree syntaxTree, CSharpParseOptions cSharpParseOptions)
        {
            CompilationUnitSyntax root = syntaxTree.GetCompilationUnitRoot();
            List<GlobalStatementSyntax> globalStatements = [.. root.Members.OfType<GlobalStatementSyntax>()];

            if (globalStatements.Count == 0)
                return syntaxTree;

            UsingDirectiveSyntax[] extraUsings =
            [
                SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("System.Threading")),
                SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("System.Threading.Tasks")),
                SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("global::Gumknix")),
                SyntaxFactory.UsingDirective(SyntaxFactory.NameEquals("Console"), SyntaxFactory.ParseName("PseudoSystem.Console")),
                SyntaxFactory.UsingDirective(SyntaxFactory.NameEquals("ConsoleKeyInfo"), SyntaxFactory.ParseName("PseudoSystem.ConsoleKeyInfo")),
                SyntaxFactory.UsingDirective(SyntaxFactory.NameEquals("ConsoleCancelEventArgs"), SyntaxFactory.ParseName("PseudoSystem.ConsoleCancelEventArgs"))
            ];

            SyntaxList<UsingDirectiveSyntax> usings = root.Usings;
            SyntaxList<UsingDirectiveSyntax> allUsings = usings.AddRange(extraUsings);

            StringBuilder wrappedSource = new();

            wrappedSource.AppendLine("""
                public static void GumknixEntryPoint(Gumknix.Gumknix gumknix)
                {
                    Console Console = new();
                    AppletConsole applet = gumknix.StartApplet(typeof(AppletConsole), [Console]) as AppletConsole;
                    applet.StartTask(async () =>
                    {
                        try
                        {
                """);

            foreach (GlobalStatementSyntax globalMember in globalStatements)
                wrappedSource.AppendLine(globalMember.ToFullString());

            wrappedSource.AppendLine("""
                        }
                        catch (OperationCanceledException)
                        {
                        }
                        catch(Exception e)
                        {
                            gumknix.StartApplet(typeof(AppletKniopad), [e.Message]);
                        }
                    });
                }
                """);

            List<MemberDeclarationSyntax> nonGlobalMembers = [.. root.Members.Where(member => member is not GlobalStatementSyntax)];
            foreach (MemberDeclarationSyntax nonGlobalMember in nonGlobalMembers)
                wrappedSource.AppendLine(nonGlobalMember.ToFullString());

            SyntaxTree tempTree = CSharpSyntaxTree.ParseText(wrappedSource.ToString(), cSharpParseOptions);
            SyntaxNode tempRoot = tempTree.GetRoot();
            List<MemberDeclarationSyntax> members = tempRoot.DescendantNodes().OfType<MemberDeclarationSyntax>().ToList();

            ClassDeclarationSyntax programClass = SyntaxFactory.ClassDeclaration("GumknixConsoleApplet")
               .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword)))
               .WithMembers(SyntaxFactory.List(members));

            CompilationUnitSyntax newRoot = SyntaxFactory.CompilationUnit()
                .WithUsings(allUsings)
                .WithMembers(SyntaxFactory.SingletonList<MemberDeclarationSyntax>(programClass))
                .NormalizeWhitespace();

            string sourceCode = newRoot.ToFullString();

            if (sourceCode.Contains("Console.Read"))
            {
                ConsoleRewriter consoleReadRewriter = new();
                SyntaxNode rewrittenRoot = consoleReadRewriter.Rewrite(newRoot);
                sourceCode = rewrittenRoot.ToFullString();
            }

            syntaxTree = CSharpSyntaxTree.ParseText(sourceCode, cSharpParseOptions);
            return syntaxTree;
        }

        private void ReadFile(FileSystemItem fileItem)
        {
            try
            {
#if BLAZORGL
                FileSystemFileHandle fileSystemFileHandle = fileItem.Handle as FileSystemFileHandle;
                Task<nkast.Wasm.File.File> file = fileSystemFileHandle.GetFile();
                file.ContinueWith(t =>
                {
                    if (t.IsCompletedSuccessfully && t.Result != null)
                    {
                        nkast.Wasm.File.Blob blob = file.Result;

                        Task<string> textTask = blob.Text();
                        textTask.ContinueWith(async t =>
                        {
                            if (t.IsCompletedSuccessfully && t.Result != null)
                            {
                                string text = textTask.Result;

                                ProjectFile projectFile = null;
                                if (fileItem.Extension == ".csproj")
                                {
                                    projectFile = ProjectFile.Parse(fileItem, text, fileItem.Extension);
                                    Task resolveFilesTask = projectFile.ResolveFiles();
                                    await resolveFilesTask.ContinueWith(async t =>
                                    {
                                        for (int i = 0; i < projectFile.ProjectFiles.Count; i++)
                                        {
                                            FileSystemItem projectChildFileItem = projectFile.ProjectFiles.Values.ElementAt(i);
                                            _solutionFilesListBox.Items.Add(projectChildFileItem);

                                            if (projectChildFileItem.Extension != ".cs")
                                                continue;

                                            FileSystemFileHandle fileSystemFileHandle = projectChildFileItem.Handle as FileSystemFileHandle;
                                            nkast.Wasm.File.File file = await fileSystemFileHandle.GetFile();
                                            string text = await file.Text();
                                            sourceFiles.Add(projectChildFileItem.Path, text);
                                        }

                                        _outputPanel.Text = projectFile.Log.ToString();
                                    });
                                }
                                else if (fileItem.Extension == ".cs")
                                {
                                    sourceFiles.Add(fileItem.Path, text);
                                }

                                selectedSourceFilePath = fileItem.Path;
                                string languageId = GetLanguageNameFromFileExtension(fileItem.Extension);
                                _monaco.SetLanguage(languageId);
                                _monaco.SetText(text);
                            }
                        });
                    }
                });
#endif
            }
            catch (Exception e)
            {
            }
        }

        public ModuleMonaco.LanguageDefinition GetLanguageDefinitionFromFileExtension(string extension)
        {
            for (int i = 0; i < LanguageDefinitions.Length; i++)
            {
                ModuleMonaco.LanguageDefinition language = LanguageDefinitions[i];
                for (int j = 0; j < language.Extensions?.Length; j++)
                    if (extension == language.Extensions[j])
                        return language;
            }
            return null;
        }

        public string GetLanguageNameFromFileExtension(string extension)
        {
            ModuleMonaco.LanguageDefinition language = GetLanguageDefinitionFromFileExtension(extension);
            return language?.Id ?? "plaintext";
        }

        public string GetILTextFromAssembly(byte[] assemblyBytes)
        {
            using MemoryStream stream = new MemoryStream(assemblyBytes);
            using PEReader portableExecutableReader = new PEReader(stream);
            MetadataReader metadataReader = portableExecutableReader.GetMetadataReader();
            StringBuilder stringBuilder = new();

            foreach (TypeDefinitionHandle typeDefinitionHandle in metadataReader.TypeDefinitions)
            {
                TypeDefinition typeDef = metadataReader.GetTypeDefinition(typeDefinitionHandle);
                string typeName = metadataReader.GetString(typeDef.Name);
                stringBuilder.AppendLine($"Type: {typeName}");
                stringBuilder.AppendLine();

                foreach (MethodDefinitionHandle methodDefinitionHandle in typeDef.GetMethods())
                {
                    MethodDefinition methodDefinition = metadataReader.GetMethodDefinition(methodDefinitionHandle);
                    string methodName = metadataReader.GetString(methodDefinition.Name);
                    stringBuilder.AppendLine($"  Method: {methodName}");
                    stringBuilder.AppendLine($"    RelativeVirtualAddress: {methodDefinition.RelativeVirtualAddress}");

                    MethodBodyBlock body = portableExecutableReader.GetMethodBody(methodDefinition.RelativeVirtualAddress);
                    if (body != null)
                    {
                        byte[] ilBytes = body.GetILBytes();
                        stringBuilder.AppendLine($"    IL Bytes: {BitConverter.ToString(ilBytes)}");
                    }
                    else
                    {
                        stringBuilder.AppendLine("    No IL body.");
                    }
                    stringBuilder.AppendLine();
                }
            }

            return stringBuilder.ToString();
        }

        public List<string> GetNamespacesFromAssembly(Assembly assembly)
        {
            List<string> namespaces = [];
            Type[] types = assembly.GetTypes();
            for (int i = 0; i < types.Length; i++)
            {
                string namespaceValue = types[i].Namespace;
                if (string.IsNullOrEmpty(namespaceValue))
                    continue;

                bool found = false;
                for (int j = 0; j < namespaces.Count; j++)
                {
                    if (namespaces[j] == namespaceValue)
                    {
                        found = true;
                        break;
                    }
                }
                if (!found)
                    namespaces.Add(namespaceValue);
            }
            return namespaces;
        }

        public List<ModuleMonaco.CompletionItemInfo> GetTypeInfos(Type[] types)
        {
            List<ModuleMonaco.CompletionItemInfo> typeInfos = new();
            for (int j = 0; j < types.Length; j++)
            {
                Type type = types[j];

                if (type.IsNotPublic)
                    continue;

                ModuleMonaco.CompletionItemKind kind =
                      type.IsClass ? ModuleMonaco.CompletionItemKind.Class
                    : type.IsEnum ? ModuleMonaco.CompletionItemKind.Enum
                    : type.IsInterface ? ModuleMonaco.CompletionItemKind.Interface
                    : type.IsValueType ? ModuleMonaco.CompletionItemKind.Struct
                    : ModuleMonaco.CompletionItemKind.Text;
                ModuleMonaco.CompletionItemInfo completionItemInfo = new()
                {
                    FullName = type.Name,
                    KindEnumValue = (int)kind
                };
                typeInfos.Add(completionItemInfo);
            }
            return typeInfos;
        }

        public void ObjectInfoPanel(Type type)
        {
            Dictionary<string, string> summaries = [];
            System.Xml.Linq.XDocument xml = null;
            List<System.Xml.Linq.XElement> xmlMembers = null;

            Microsoft.Xna.Framework.Content.ContentManager contentManager = new(GumknixInstance.GameServiceContainer, "Content");
            using Stream stream = TitleContainer.OpenStream(Path.Combine("Content", "Xna.Framework.Graphics.xml"));
            if (stream != null)
            {
                using StreamReader reader = new(stream, Encoding.UTF8);
                string xmlContent = reader.ReadToEnd();
                xml = System.Xml.Linq.XDocument.Parse(xmlContent);
                xmlMembers = xml.Descendants("member").ToList();
            }

            Type[] interfaces = type.GetInterfaces();

            bool AddSummary(string name, string searchPrefix, out string summary)
            {
                summary = null;
                for (int j = 0; j < xmlMembers.Count; j++)
                {
                    System.Xml.Linq.XElement member = xmlMembers[j];
                    string nameAttr = member.Attribute("name")?.Value;
                    if (nameAttr != null && nameAttr.StartsWith(searchPrefix) && nameAttr.EndsWith(name))
                    {
                        summary = member.Element("summary")?.Value.Trim();

                        if (summary == null)
                        {
                            if (member.Element("inheritdoc") != null)
                            {
                                for (int k = 0; k < interfaces.Length; k++)
                                {
                                    Type interfaceType = interfaces[k];
                                    if (AddSummary(name, searchPrefix.Substring(0, 2) + interfaceType.FullName, out string inheritSummary))
                                    {
                                        summary = inheritSummary;
                                        break;
                                    }
                                }
                            }
                        }

                        break;
                    }
                }
                return summary != null;
            }

            StringBuilder stringBuilder = new();

            BindingFlags bindingFlags = BindingFlags.Public | /*BindingFlags.NonPublic |*/ BindingFlags.Instance | BindingFlags.Static;

            // Types
            stringBuilder.AppendLine($"Type: {type.FullName}");
            if (type.BaseType != null)
            {
                stringBuilder.AppendLine($"Base Type: {type.BaseType.Name}");
                if (AddSummary(type.BaseType.Name, "T:" + type.BaseType.FullName, out string summary))
                    stringBuilder.AppendLine($"  Summary: {summary}");
            }

            // Attributes
            object[] attributes = type.GetCustomAttributes(false);
            stringBuilder.AppendLine($"Attributes ({attributes.Length}):");
            for (int i = 0; i < attributes.Length; i++)
            {
                stringBuilder.AppendLine($"  {attributes[i].GetType().Name}");
                if (AddSummary(attributes[i].GetType().Name, "T:" + attributes[i].GetType().FullName, out string summary))
                    stringBuilder.AppendLine($"    Summary: {summary}");
            }

            // Interfaces
            //Type[] interfaces = type.GetInterfaces();
            stringBuilder.AppendLine($"Interfaces ({interfaces.Length}):");
            for (int i = 0; i < interfaces.Length; i++)
            {
                stringBuilder.AppendLine($"  {interfaces[i].Name}");
                if (AddSummary(interfaces[i].Name, "T:" + interfaces[i].FullName, out string summary))
                    stringBuilder.AppendLine($"    Summary: {summary}");
            }

            // Fields
            FieldInfo[] fields = type.GetFields(bindingFlags);
            stringBuilder.AppendLine($"Fields ({fields.Length}):");
            for (int i = 0; i < fields.Length; i++)
            {
                stringBuilder.AppendLine($"  {fields[i].FieldType.Name} {fields[i].Name}");
                if (AddSummary(fields[i].Name, "F:" + fields[i].DeclaringType + ".", out string summary))
                    stringBuilder.AppendLine($"    Summary: {summary}");
            }

            // Properties
            PropertyInfo[] properties = type.GetProperties(bindingFlags);
            stringBuilder.AppendLine($"Properties ({properties.Length}):");
            for (int i = 0; i < properties.Length; i++)
            {
                PropertyInfo property = properties[i];
                stringBuilder.AppendLine($"  {property.PropertyType.Name} {property.Name}");
                if (AddSummary(property.Name, "P:" + property.DeclaringType + ".", out string summary))
                    stringBuilder.AppendLine($"    Summary: {summary}");
            }

            // Methods
            MethodInfo[] methods = type.GetMethods(bindingFlags);
            stringBuilder.AppendLine($"Methods ({methods.Length}):");
            for (int i = 0; i < methods.Length; i++)
            {
                MethodInfo methodInfo = methods[i];
                if (methodInfo.IsSpecialName)
                    continue;

                stringBuilder.AppendLine($"  {methodInfo.ReturnType.Name} {methodInfo.Name}({string.Join(", ", methodInfo.GetParameters().Select(p => p.ParameterType.Name + " " + p.Name))})");

                StringBuilder parametersXmlText = new StringBuilder();
                ParameterInfo[] parameterInfos = methodInfo.GetParameters();
                for (int j = 0; j < parameterInfos.Length; j++)
                {
                    parametersXmlText.Append((j == 0) ? "(" : "");
                    Type paramType = parameterInfos[j].ParameterType;
                    string parameterName = paramType.UnderlyingSystemType.ToString();
                    parameterName = ToXmlTypeName(parameterName);

                    string ToXmlTypeName(string typeName)
                    {
                        int genericTickIndex = typeName.IndexOf('`');
                        int openBracketIndex = typeName.IndexOf('[');
                        int closeBracketIndex = typeName.LastIndexOf(']');
                        if (genericTickIndex >= 0 && openBracketIndex > genericTickIndex && closeBracketIndex > openBracketIndex)
                        {
                            string genericType = typeName.Substring(0, genericTickIndex);
                            string innerType = typeName.Substring(openBracketIndex + 1, closeBracketIndex - openBracketIndex - 1);
                            int commaIndex = innerType.IndexOf(',');
                            if (commaIndex > 0)
                                innerType = innerType.Substring(0, commaIndex);
                            return $"{genericType}{{{innerType}}}";
                        }
                        return typeName;
                    }

                    parametersXmlText.Append(parameterName);
                    parametersXmlText.Append((j < (parameterInfos.Length - 1)) ? "," : ")");
                }
                if (AddSummary(methodInfo.Name + parametersXmlText, "M:" + methodInfo.DeclaringType + ".", out string summary))
                    stringBuilder.AppendLine($"    Summary: {summary}");
            }

            // Operators
            stringBuilder.AppendLine("Operators:");
            MethodInfo[] operatorMethods = type.GetMethods(bindingFlags);
            for (int i = 0; i < operatorMethods.Length; i++)
            {
                MethodInfo operatorMethod = operatorMethods[i];
                if (operatorMethod.IsSpecialName && operatorMethod.Name.StartsWith("op_"))
                {
                    string operatorName = operatorMethod.Name switch
                    {
                        "op_Addition" => "operator +",
                        "op_Subtraction" => "operator -",
                        "op_Multiply" => "operator *",
                        "op_Division" => "operator /",
                        "op_Equality" => "operator ==",
                        "op_Inequality" => "operator !=",
                        "op_LessThan" => "operator <",
                        "op_GreaterThan" => "operator >",
                        "op_LessThanOrEqual" => "operator <=",
                        "op_GreaterThanOrEqual" => "operator >=",
                        "op_Implicit" => "implicit operator",
                        "op_Explicit" => "explicit operator",
                        _ => operatorMethod.Name
                    };
                    ParameterInfo[] parameters = operatorMethod.GetParameters();
                    string paramList = string.Join(", ", parameters.Select(p => $"{p.ParameterType.Name} {p.Name}"));
                    stringBuilder.AppendLine($"  {operatorMethod.ReturnType.Name} {operatorName}({paramList})");
                    if (AddSummary(operatorMethod.Name, "M:" + operatorMethod.DeclaringType + ".", out string summary))
                        stringBuilder.AppendLine($"    Summary: {summary}");
                }
            }

            _outputPanel.Text = stringBuilder.ToString();
        }

        public string CreateShareLink()
        {
            string code = _monaco.GetText();
            byte[] codeBytes = Encoding.UTF8.GetBytes(code);
            using MemoryStream memoryStream = new();
            using (GZipStream gzip = new(memoryStream, CompressionLevel.SmallestSize, true))
            {
                gzip.Write(codeBytes, 0, codeBytes.Length);
                gzip.Flush();
            }
            byte[] compressedData = memoryStream.ToArray();
            string base64 = Convert.ToBase64String(compressedData);
            return $"data:application/x-gzip;base64,{base64}";
        }

        protected override void Close()
        {
#if BLAZORGL
            _monaco.Close();
            _textEditorContainer.Remove();
#endif
            base.Close();
        }
    }
}
