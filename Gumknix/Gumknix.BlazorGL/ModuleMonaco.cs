using System;
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
