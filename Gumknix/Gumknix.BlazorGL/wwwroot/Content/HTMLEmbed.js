HTMLEmbed =
{
    AddElement: function (d)
    {
        var uid = Module.HEAP32[(d + 0) >> 2];
        var dc = nkJSObject.GetObject(uid);
        var ht = nkJSObject.ReadString(d + 4);
        var te = dc.createElement("div");
        te.innerHTML = ht;
        var el = te.firstElementChild;
        dc.body.appendChild(el);
        return nkJSObject.RegisterObject(el);
    },
    SetInnerHTML: function (uid, d)
    {
        var el = nkJSObject.GetObject(uid);
        var ht = nkJSObject.ReadString(d + 0);
        el.innerHTML = ht;
    },
    RemoveElement: function (uid)
    {
        var el = nkJSObject.GetObject(uid);
        if (el && el.parentNode)
            el.parentNode.removeChild(el);
    }
};
