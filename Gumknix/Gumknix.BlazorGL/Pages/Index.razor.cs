using System;
using System.Reflection;
using Microsoft.JSInterop;
using Microsoft.Xna.Framework;

namespace Gumknix.Pages
{
    public partial class Index
    {
        Game _game;
        bool _loadingGameRunner;

        protected override void OnAfterRender(bool firstRender)
        {
            base.OnAfterRender(firstRender);

            if (firstRender)
            {
                JsRuntime.InvokeAsync<object>("initRenderJS", DotNetObjectReference.Create(this));
            }
        }

        [JSInvokable]
        public void TickDotNet()
        {
            if (_game == null && !_loadingGameRunner)
                CheckGameRunner();
            if (_loadingGameRunner)
                return;

            // init game
            if (_game == null)
            {
                _game = new GumknixDemo();
                _game.Run();
            }

            // run gameloop
            _game.Tick();
        }

        public async void CheckGameRunner()
        {
            try
            {
                Uri uri = new(Program.NavigationManager.Uri);

                string queryString = uri.Query;
                if (string.IsNullOrEmpty(queryString))
                    return;

                string ilUrl = System.Web.HttpUtility.ParseQueryString(queryString).Get("ilurl");
                if (string.IsNullOrEmpty(ilUrl))
                    return;

                _loadingGameRunner = true;

                string unescapedIlUrl = Uri.UnescapeDataString(ilUrl);
                using System.Net.Http.HttpClient httpClient = new System.Net.Http.HttpClient();
                byte[] ILBytes = await httpClient.GetByteArrayAsync(unescapedIlUrl);

                Assembly loadedAssembly = Assembly.Load(ILBytes);
                if (loadedAssembly == null)
                    return;

                Type[] types = loadedAssembly.GetTypes();
                for (int i = 0; i < types.Length; i++)
                {
                    try
                    {
                        Type type = types[i];
                        string baseTypeName = type.BaseType?.ToString();
                        if (baseTypeName == "Game" || baseTypeName == "Microsoft.Xna.Framework.Game")
                        {
                            _game = (Game)Activator.CreateInstance(type);
                            _game.Run();
                            _loadingGameRunner = false;
                            break;
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
    }
}
