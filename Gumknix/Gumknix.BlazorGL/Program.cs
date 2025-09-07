using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Gum.Clipboard;
using TextCopy;

namespace Gumknix
{
    internal class Program
    {
        public static NavigationManager NavigationManager { get; set; }

        private static async Task Main(string[] args)
        {
            var builder = WebAssemblyHostBuilder.CreateDefault(args);
            builder.RootComponents.Add<App>("#app");
            builder.RootComponents.Add<HeadOutlet>("head::after");
            builder.Services.AddScoped(sp => new HttpClient()
            {
                BaseAddress = new Uri(builder.HostEnvironment.BaseAddress)
            });
            builder.Services.InjectClipboard();
            ClipboardImplementation.InjectedClipboard =
                builder.Services.BuildServiceProvider().GetRequiredService<IClipboard>();
            NavigationManager = builder.Services.BuildServiceProvider().GetRequiredService<NavigationManager>();
            await builder.Build().RunAsync();
        }

        public partial class TextCopyBrowserClipboard : ComponentBase
        {
            [Inject]
            public IClipboard Clipboard { get; set; }

            public string Content { get; set; }

            public Task CopyTextToClipboard() =>
                Clipboard.SetTextAsync(Content);

            public async Task ReadTextFromClipboard() =>
                Content = await Clipboard.GetTextAsync();
        }
    }
}
