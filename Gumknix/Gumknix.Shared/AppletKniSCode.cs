using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
//using Microsoft.CodeAnalysis.VisualBasic;
//using Microsoft.CodeAnalysis.VisualBasic.Syntax;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.Xna.Framework;
using Gum.DataTypes;
using Gum.Forms;
using Gum.Forms.Controls;
using Gum.Wireframe;
using MonoGameGum;
using MonoGameGum.GueDeriving;

using MetadataReferenceService.Abstractions.Types;
using MetadataReferenceService.BlazorWasm;
using System.IO.Compression;


#if BLAZORGL
using nkast.Wasm.Canvas;
using nkast.Wasm.Dom;
using nkast.Wasm.File;
using nkast.Wasm.FileSystem;
#endif

namespace Gumknix
{
    public class AppletKniSCode : BaseApplet
    {
        public static readonly string DefaultTitle = "KniS Code";
        public static readonly string DefaultIcon = "\uEE71";

#if BLAZORGL
        private ModuleMonaco _monaco;

        private Canvas _canvas;
        private HTMLEmbed _editorHTMLEmbed;
        private HTMLEmbed _svgCutoutHTMLEmbed;

        public ModuleMonaco.LanguageDefinition[] LanguageDefinitions { get; private set; }
#endif

        private ColoredRectangleRuntime _background;
        private Menu _menu;
        private StackPanel _stackPanel;
        private Button compileButton;

        private static BlazorWasmMetadataReferenceService _referenceService;

        private Assembly _loadedAssembly;

        public AppletKniSCode(Gumknix gumknix, object[] args = null) : base(gumknix, args)
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
            //menuItemViewWordWrap.Clicked += (s, e) =>
            //{ _textBox.TextWrapping = (_textBox.TextWrapping == TextWrapping.NoWrap) ? TextWrapping.Wrap : TextWrapping.NoWrap; };

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
            MainStackPanel.Visual.AddChild(_background);

            _stackPanel = new();
            _stackPanel.Orientation = Orientation.Vertical;
            _stackPanel.Visual.Dock(Dock.Fill);
            _stackPanel.Visual.ChildrenLayout = Gum.Managers.ChildrenLayout.TopToBottomStack;
            _background.AddChild(_stackPanel);

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
            _stackPanel.AddChild(compileButton);

#if BLAZORGL
            _monaco = ModuleMonaco.Create();
            _monaco.OnScriptLoaded += (s, e) => _monaco.InitializeInstance();
            _monaco.OnInstanceLoaded += (s, e) =>
            {
                LanguageDefinitions = _monaco.GetLanguages();
                for (int i = 0; i < LanguageDefinitions.Length; i++)
                    for (int j = 0; j < LanguageDefinitions[i].Extensions?.Length; j++)
                        Gumknix.ExtensionsDefaultApplets.TryAdd(LanguageDefinitions[i].Extensions[j], new(typeof(AppletKniSCode), DefaultIcon));

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

            _editorHTMLEmbed = HTMLEmbed.Create(
                $"""
                <div id = "editorUid{_monaco.Uid}" style = "position: absolute; display: none;">
                </div>
                """);

            _svgCutoutHTMLEmbed = HTMLEmbed.Create(
                $"""
                <svg id="svgCutoutUid{_monaco.Uid}" width="0" height="0" style="position:absolute;">
                </svg>
                """);
            
            _monaco.InitializeLoaderScript();

            _referenceService ??= new(Program.NavigationManager);
#endif
        }

        public override void Update()
        {

            base.Update();
        }

        public override void PostGumUpdate()
        {
#if BLAZORGL
            Rectangle area = new(0, 0, (int)Window.ActualWidth, (int)Window.ActualHeight);
            int currentLayerIndex = GumService.Default.Renderer.Layers.IndexOf(Layer);

            List<Rectangle> rectangles = [];
            for (int i = 0; i < GumknixInstance.RunningApplets.Count; i++)
            {
                BaseApplet applet = GumknixInstance.RunningApplets[i];
                if ((applet != this) && applet.Window.IsVisible)
                {
                    int otherLayerIndex = GumService.Default.Renderer.Layers.IndexOf(applet.Layer);
                    if (otherLayerIndex < currentLayerIndex)
                        continue;

                    if (applet != this)
                    {
                        Rectangle windowArea = new(
                            (int)(applet.Window.Visual.AbsoluteLeft - (Window.AbsoluteLeft + 20)),
                            (int)(applet.Window.Visual.AbsoluteTop - (Window.AbsoluteTop + 50)),
                            (int)applet.Window.Visual.GetAbsoluteWidth(),
                            (int)applet.Window.Visual.GetAbsoluteHeight());
                        if (area.Intersects(windowArea))
                            rectangles.Add(windowArea);
                    }

                    for (int j=0; j < applet.Dialogs.Count;j++)
                    {
                        BaseDialog dialog = applet.Dialogs[j];
                        Rectangle dialogArea = new(
                            (int)(dialog.Window.Visual.AbsoluteLeft - (Window.AbsoluteLeft + 20)),
                            (int)(dialog.Window.Visual.AbsoluteTop - (Window.AbsoluteTop + 50)),
                            (int)dialog.Window.Visual.GetAbsoluteWidth(),
                            (int)dialog.Window.Visual.GetAbsoluteHeight());
                        if (area.Intersects(dialogArea))
                            rectangles.Add(dialogArea);
                    }
                }

            }

            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"  <defs>");
            sb.AppendLine($"    <mask id=\"cutoutUid{_monaco.Uid}\">");
            //sb.AppendLine($"      <rect x=\"{0}\" y=\"{0}\" width=\"{1000}\" height=\"{1000}\" fill=\"black\"/>");
            sb.AppendLine($"      <rect x=\"{area.Left}\" y=\"{area.Top}\" width=\"{area.Width}\" height=\"{area.Height}\" fill=\"white\"/>");
            for (int i = 0; i < rectangles.Count; i++)
            {
                Rectangle r = rectangles[i];
                sb.AppendLine($"      <rect x=\"{r.Left}\" y=\"{r.Top}\" width=\"{r.Width}\" height=\"{r.Height}\" fill=\"black\"/>");
            }
            sb.AppendLine($"    </mask>");
            sb.AppendLine($"  </defs>");

            if (Window.IsVisible)
            {
                _canvas ??= nkast.Wasm.Dom.Window.Current.Document.GetElementById<Canvas>("theCanvas");
                DOMRect clientBounds = _canvas.GetBoundingClientRect();

                _svgCutoutHTMLEmbed.SetInnerHTML(sb.ToString());
                _editorHTMLEmbed.Style.SetProperty("display", "block");
                _editorHTMLEmbed.Style.SetProperty("left", $"{clientBounds.Left + Window.AbsoluteLeft + 20}px");
                _editorHTMLEmbed.Style.SetProperty("top", $"{clientBounds.Top + Window.AbsoluteTop + 100}px");
                _editorHTMLEmbed.Style.SetProperty("width", $"{Window.ActualWidth - 100}px");
                _editorHTMLEmbed.Style.SetProperty("height", $"{Window.ActualHeight - 170}px");
                _editorHTMLEmbed.Style.SetProperty("mask", $"url(#cutoutUid{_monaco.Uid})");
                _editorHTMLEmbed.Style.SetProperty("z-index", "9999");
            }
            else
            {
                _editorHTMLEmbed.Style.SetProperty("display", "none");
            }
#endif
        }

        public async Task Compile()
        {
            //if (loadedAssembly != null)
            //{
            //    try
            //    {
            //        AssemblyLoadContext assemblyLoadContext = AssemblyLoadContext.GetLoadContext(loadedAssembly);
            //        assemblyLoadContext?.Unload();
            //        loadedAssembly = null;
            //    }
            //    catch(Exception e)
            //    {
            //    }
            //}
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

            CSharpParseOptions cSharpParseOptions = CSharpParseOptions.Default;
            cSharpParseOptions.WithLanguageVersion(Microsoft.CodeAnalysis.CSharp.LanguageVersion.LatestMajor);
            cSharpParseOptions.WithPreprocessorSymbols(preprocessorSymbols);
            SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(sourceCode, cSharpParseOptions);

            //VisualBasicParseOptions vbParseOptions = VisualBasicParseOptions.Default;
            //vbParseOptions.WithLanguageVersion(Microsoft.CodeAnalysis.VisualBasic.LanguageVersion.Latest);
            //vbParseOptions.WithPreprocessorSymbols(preprocessorSymbols);
            //SyntaxTree syntaxTree = VisualBasicSyntaxTree.ParseText(sourceCode, vbParseOptions);

            CompilationUnitSyntax root = syntaxTree.GetRoot() as CompilationUnitSyntax;
            bool topLevelStatementsFound = false;
            int lastUsingLine = -1;
            int firstNonBlankLine = -1;
            if (root != null)
            {
                for (int i = 0; i < root.Members.Count; i++)
                {
                    if (root.Members[i] is GlobalStatementSyntax)
                    {
                        topLevelStatementsFound = true;
                        break;
                    }
                }

                if (root.Usings.Count >= 1)
                {
                    UsingDirectiveSyntax lastUsing = root.Usings[^1];
                    lastUsingLine = lastUsing.GetLocation().GetLineSpan().EndLinePosition.Line;
                }
            }

            string[] allLines = sourceCode.Split(["\n", "\r\n"], StringSplitOptions.None);
            string usingText = string.Join("\n", allLines, 0, lastUsingLine + 1);

            if (topLevelStatementsFound)
            {
                for (int i = lastUsingLine + 1; i < allLines.Length; i++)
                {
                    if (!string.IsNullOrWhiteSpace(allLines[i]))
                    {
                        firstNonBlankLine = i;
                        break;
                    }
                }

                string indent = "                ";
                string sourceText = string.Join($"\n{indent}", allLines, firstNonBlankLine, allLines.Length - firstNonBlankLine);

                string wrappedSource = $"{usingText}\n\n";
                wrappedSource +=
                    """
                    using System.Threading.Tasks;
                    using global::Gumknix;
                    using Console = PseudoSystem.Console;

                    public class GumknixConsoleApplet
                    {
                        public static void GumknixEntryPoint(Gumknix.Gumknix gumknix)
                        {
                            Console Console = new();
                            AppletConsole applet = gumknix.StartApplet(typeof(AppletConsole), [Console]) as AppletConsole;
                            applet.StartTask(async () =>
                            {
                                try
                                {
                    """;
                wrappedSource += $"\n{indent}{sourceText}\n";
                wrappedSource +=
                    """
                                }
                                catch(Exception e)
                                {
                                    gumknix.StartApplet(typeof(AppletKniopad), [e.Message]);
                                }
                            });
                        }
                    }
                    """;

                syntaxTree = CSharpSyntaxTree.ParseText(wrappedSource, cSharpParseOptions);
            }

            List<SyntaxTree> syntaxTrees = [syntaxTree];


            HashSet<string> assembliesRequired = [];
            assembliesRequired.Add("System.Private.CoreLib");
            assembliesRequired.Add("System.Runtime");

            root = syntaxTree.GetRoot() as CompilationUnitSyntax;
            for (int i = 0; i < root.Usings.Count; i++)
            {
                UsingDirectiveSyntax usingDirectiveSyntax = root.Usings[i];
                string namespaceName = usingDirectiveSyntax.Name.ToString().Replace("global::", "");
                if (namespaceToModuleLookup.TryGetValue(namespaceName, out (Assembly assembly, string moduleName) entry))
                {
                    if (assembliesRequired.Contains(entry.moduleName) == false)
                        assembliesRequired.Add(entry.moduleName);
                }
            }

            List<string> typeNames = new();
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
                optimizationLevel: OptimizationLevel.Release
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
                        entryPointFound = true;
                        Type type = types[i];
                        MethodInfo autoRunMethod = type.GetMethod("GumknixEntryPoint", BindingFlags.Public | BindingFlags.Static);
                        autoRunMethod?.Invoke(null, [GumknixInstance]);
                    }
                    catch (Exception e)
                    {
                        log += e.Message;
                    }
                }
                if (!entryPointFound)
                    log += "No entry point found in the assembly.";
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

            GumknixInstance.StartApplet(typeof(AppletKniopad), [log]);
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
                        textTask.ContinueWith(t =>
                        {
                            if (t.IsCompletedSuccessfully && t.Result != null)
                            {
                                ModuleMonaco.LanguageDefinition language = GetLanguageDefinitionFromFileExtension(fileItem.Extension);
                                _monaco.SetLanguage(language?.Id ?? "plaintext");

                                string text = textTask.Result;
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

        /// <summary>
        /// Displays information about the specified type in the object info panel.
        /// </summary>
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
                                    if (AddSummary(name, searchPrefix.Substring(0,2) + interfaceType.FullName, out string inheritSummary))
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

            GumknixInstance.StartApplet(typeof(AppletKniopad), [stringBuilder.ToString()]);
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
            _svgCutoutHTMLEmbed.Remove();
            _editorHTMLEmbed.Remove();
#endif
            base.Close();
        }
    }
}
