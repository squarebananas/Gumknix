using System;
using System.Collections.Generic;
using MonoGameGum;
using RenderingLibrary.Graphics;

namespace Gumknix
{
    public class BaseSystemVisual
    {
        public Gumknix GumknixInstance { get; init; }

        public Layer Layer { get; set; }
        public List<BaseDialog> Dialogs { get; private set; }

        public BaseSystemVisual(Gumknix gumknix)
        {
            GumknixInstance = gumknix;

            Layer = new();
            Layer.LayerCameraSettings ??= new LayerCameraSettings() { Zoom = 1f };
            GumService.Default.Renderer.AddLayer(Layer);

            Dialogs = [];
        }

        public static BaseSystemVisual GetSystemVisual(object obj)
        {
            if (obj is BaseSystemVisual systemVisual)
                return systemVisual;
            if (obj is BaseDialog dialog)
                return dialog.SystemVisual;
            return null;
        }

        public virtual void ShowDialog(BaseDialog dialog) { }

        protected virtual void UpdateDialogs()
        {
            for (int i = 0; i < Dialogs.Count; i++)
            {
                BaseDialog dialog = Dialogs[i];
                dialog.Update();
                if (dialog.IsClosed)
                {
                    Dialogs.RemoveAt(i);
                    i--;
                }
            }
        }
    }
}
