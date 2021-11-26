using System.IO;

using Microsoft.Web.WebView2.Core;

namespace ShowWebPagePlugin
{
    public partial class SampleDisplayView
    {
        public SampleDisplayView()
        {
            InitializeComponent();
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            if (this.DataContext is SampleDisplayViewModel vm)
            {
                this.InitializeAsync(vm);
            }
        }

        public async void InitializeAsync(SampleDisplayViewModel vm)
        {
            var browserFolder = Path.Combine(Path.GetTempPath(), "ATLAS10_WebBrowser");
            var env = await CoreWebView2Environment.CreateAsync(userDataFolder: browserFolder);
            await webView.EnsureCoreWebView2Async(env);

            if (vm.IsDevToolsOpen)
            {
                webView.CoreWebView2.OpenDevToolsWindow();
            }

            vm.Initialize(this.webView.CoreWebView2);
        }
    }
}