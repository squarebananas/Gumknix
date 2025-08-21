using System;

#if BLAZORGL
using nkast.Wasm.Dom;
#endif

namespace Gumknix
{
#if BLAZORGL
    public class HTMLEmbed : HTMLElement<HTMLEmbed>
    {
        internal HTMLEmbed(int uid) : base(uid)
        {
        }

        public static HTMLEmbed Create(string html)
        {
            int uid = nkast.Wasm.JSInterop.JSObject.StaticInvokeRetInt<int, string>("HTMLEmbed.AddElement",
                nkast.Wasm.Dom.Window.Current.Document.Uid, html);
            if (uid == -1)
                return null;

            HTMLEmbed embed = HTMLEmbed.FromUid(uid);
            if (embed != null)
                return embed;

            return new HTMLEmbed(uid);
        }

        public void SetInnerHTML(string html)
        {
            Invoke("HTMLEmbed.SetInnerHTML", html);
        }

        public void Remove()
        {
            Invoke("HTMLEmbed.RemoveElement");
        }
    }
#endif
}
