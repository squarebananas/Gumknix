using System;
using global::Gumknix;
using Gum.Forms;
using Gum.Forms.Controls;
using Gum.Wireframe;
using MonoGameGum;

public class TestClass
{
    public static void GumknixEntryPoint(Gumknix.Gumknix gumknix)
    {
        AddButton();
    }

    public static void AddButton()
    {
        Button button = new();
        gumknix.GumRoot.Children.Add(button.Visual);
        button.Click += (s, e) => AddButton();
    }
}
