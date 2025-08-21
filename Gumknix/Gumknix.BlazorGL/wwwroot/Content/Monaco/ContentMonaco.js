window.contentMonaco =
{
    Create: function ()
    {
        var instance =
        {
            uid: null,
            elementId: 'editor',
            editor: null,
        };
        instance.uid = nkJSObject.RegisterObject(instance);

        if (!window.monacoLoaderLoaded)
        {
            var loaderScript = document.createElement('script');
            loaderScript.src = "https://cdn.jsdelivr.net/npm/monaco-editor@0.52.2/min/vs/loader.js";
            loaderScript.onload = function ()
            {
                window.monacoLoaderLoaded = true;
                DotNet.invokeMethod('Gumknix', 'JsMonacoScriptLoaded', instance.uid);
            };
            document.head.appendChild(loaderScript);
        }
        else
        {
            DotNet.invokeMethod('Gumknix', 'JsMonacoScriptLoaded', instance.uid);
        }
        return instance.uid;
    },
    Initialize: function (uid)
    {
        var instance = nkJSObject.GetObject(uid);
        require.config({ paths: { 'vs': 'https://cdn.jsdelivr.net/npm/monaco-editor@0.52.2/min/vs' } });
        require(['vs/editor/editor.main'], function ()
        {
            instance.editor = monaco.editor.create(document.getElementById(instance.elementId),
                {
                    language: 'csharp',
                    theme: 'vs-dark',
                });
            DotNet.invokeMethod('Gumknix', 'JsMonacoEditorLoaded', uid);
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
