using System;
using System.Collections.Generic;
using Microsoft.JSInterop;
using nkast.Wasm.JSInterop;

namespace Gumknix
{
    public class ModuleMonaco : CachedJSObject<ModuleMonaco>
    {
        public delegate void OnLogDelegate(object sender, string message);

        public event EventHandler<EventArgs> OnScriptLoaded;
        public event EventHandler<EventArgs> OnInstanceLoaded;

        public class LanguageDefinition
        {
            public string Id { get; set; }
            public string[] Extensions { get; set; }
        }

        public class CompletionItemInfo
        {
            public string FullName { get; set; }
            public int KindEnumValue { get; set; }
        }

        public enum CompletionItemKind
        {
            Method = 0,
            Function = 1,
            Constructor = 2,
            Field = 3,
            Variable = 4,
            Class = 5,
            Struct = 6,
            Interface = 7,
            Module = 8,
            Property = 9,
            Event = 10,
            Operator = 11,
            Unit = 12,
            Value = 13,
            Constant = 14,
            Enum = 15,
            EnumMember = 16,
            Keyword = 17,
            Text = 18,
            Color = 19,
            File = 20,
            Reference = 21,
            Customcolor = 22,
            Folder = 23,
            TypeParameter = 24,
            User = 25,
            Issue = 26,
            Tool = 27,
            Snippet = 28
        }

        internal ModuleMonaco(int uid) : base(uid)
        {
        }

        public static ModuleMonaco Create()
        {
            int uid = JSObject.StaticInvokeRetInt("contentMonaco.Create");
            if (uid == -1)
                return null;

            ModuleMonaco monaco = ModuleMonaco.FromUid(uid);
            if (monaco != null)
                return monaco;

            return new ModuleMonaco(uid);
        }

        public void InitializeLoaderScript()
        {
            Invoke("contentMonaco.InitializeLoaderScript");
        }

        public void InitializeInstance()
        {
            Invoke("contentMonaco.InitializeInstance");
        }

        public string GetText()
        {
            return InvokeRetString("contentMonaco.GetText");
        }

        public void SetText(string text)
        {
            Invoke("contentMonaco.SetText", text);
        }

        public LanguageDefinition[] GetLanguages()
        {
            string jsonString = InvokeRetString("contentMonaco.GetLanguages");
            LanguageDefinition[] languages = System.Text.Json.JsonSerializer.Deserialize<LanguageDefinition[]>(jsonString);
            return languages;
        }

        public void SetLanguage(string language)
        {
            Invoke("contentMonaco.SetLanguage", language);
        }

        public void RegisterCompletionItemProvider(List<CompletionItemInfo> typeInfos)
        {
            string jsonString = System.Text.Json.JsonSerializer.Serialize(typeInfos);
            Invoke("contentMonaco.RegisterCompletionItemProvider", jsonString);
        }

        public void Close()
        {
            Invoke("contentMonaco.Close");
        }

        [JSInvokable]
        public static void JsMonacoScriptLoaded(int uid)
        {
            ModuleMonaco monaco = ModuleMonaco.FromUid(uid);
            if (monaco == null)
                return;

            var handler = monaco.OnScriptLoaded;
            if (handler != null)
                handler(monaco, EventArgs.Empty);
        }

        [JSInvokable]
        public static void JsMonacoInstanceLoaded(int uid)
        {
            ModuleMonaco monaco = ModuleMonaco.FromUid(uid);
            if (monaco == null)
                return;

            var handler = monaco.OnInstanceLoaded;
            if (handler != null)
                handler(monaco, EventArgs.Empty);
        }
    }
}
