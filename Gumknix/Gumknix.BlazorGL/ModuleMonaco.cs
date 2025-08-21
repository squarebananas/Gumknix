using System;
using System.Threading.Tasks;
using Microsoft.JSInterop;
using nkast.Wasm.JSInterop;
using nkast.Wasm.File;

namespace Gumknix
{
    public class ModuleMonaco : CachedJSObject<ModuleMonaco>
    {
        public delegate void OnLogDelegate(object sender, string message);

        public event EventHandler<EventArgs> OnScriptLoaded;
        public event EventHandler<EventArgs> OnEditorLoaded;

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

        public void Initialize()
        {
            Invoke("contentMonaco.Initialize");
        }

        public void SetText(string text)
        {
            Invoke("contentMonaco.SetText", text);
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
        public static void JsMonacoEditorLoaded(int uid)
        {
            ModuleMonaco monaco = ModuleMonaco.FromUid(uid);
            if (monaco == null)
                return;

            var handler = monaco.OnEditorLoaded;
            if (handler != null)
                handler(monaco, EventArgs.Empty);
        }
    }
}
