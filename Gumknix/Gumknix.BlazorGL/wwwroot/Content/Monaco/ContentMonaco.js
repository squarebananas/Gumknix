window.contentMonaco =
{
    Create: function ()
    {
        var instance =
        {
            editor: null
        };
        return nkJSObject.RegisterObject(instance);
    },
    InitializeLoaderScript: function (uid)
    {
        var instance = nkJSObject.GetObject(uid);
        if (!window.monacoLoaderLoaded)
        {
            var loaderScript = document.createElement('script');
            loaderScript.src = "https://cdn.jsdelivr.net/npm/monaco-editor@0.52.2/min/vs/loader.js";
            loaderScript.onload = function ()
            {
                window.monacoLoaderLoaded = true;
                DotNet.invokeMethod('Gumknix', 'JsMonacoScriptLoaded', instance.nkUid);
            };
            document.head.appendChild(loaderScript);
        }
        else
        {
            DotNet.invokeMethod('Gumknix', 'JsMonacoScriptLoaded', instance.nkUid);
        }
    },
    InitializeInstance: function (uid)
    {
        var instance = nkJSObject.GetObject(uid);
        require.config({ paths: { 'vs': 'https://cdn.jsdelivr.net/npm/monaco-editor@0.52.2/min/vs' } });
        require(['vs/editor/editor.main'], function ()
        {
            instance.editor = monaco.editor.create(document.getElementById('editorUid' + instance.nkUid),
                {
                    language: 'csharp',
                    theme: 'vs-dark',
                    automaticLayout: true,
                });
            DotNet.invokeMethod('Gumknix', 'JsMonacoInstanceLoaded', uid);
        });
    },
    SetText: function (uid, d)
    {
        var instance = nkJSObject.GetObject(uid);
        var ed = instance.editor;
        var t = nkJSObject.ReadString(d + 0);
        ed.setValue(t);
    },
    Close: function (uid)
    {
        var instance = nkJSObject.GetObject(uid);
        var ed = instance.editor;
        if (ed)
            ed.dispose();
    }
};
