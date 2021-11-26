using System;
using System.ComponentModel;

using MAT.Atlas.Api.Core.Diagnostics;
using MAT.Atlas.Client.Presentation.Displays;

using Microsoft.Web.WebView2.Core;

namespace ShowWebPagePlugin
{
    public sealed class SampleDisplayViewModel : DisplayPluginViewModel
    {
        private readonly ILogger logger;
        private CoreWebView2 coreWebView2;
        private bool firstLoad = true;
        private bool isVisible;
        private bool isDevToolsOpen;
        private string url;

        public SampleDisplayViewModel(ILogger logger)
        {
            this.logger = logger;
        }

        public bool IsDevToolsOpen
        {
            get => this.isDevToolsOpen;
            set
            {
                if (this.SetProperty(ref this.isDevToolsOpen, value) &&
                    this.isDevToolsOpen)
                {
                    this.coreWebView2.OpenDevToolsWindow();
                }
            }
        }

        [Browsable(false)]
        public bool IsVisible
        {
            get => this.isVisible;
            set => this.SetProperty(ref this.isVisible, value);
        }

        public string Url
        {
            get => this.url = this.ReadProperty("https://www.mclaren.com/applied/");
            set
            {
                if (this.SetProperty(ref this.url, value))
                {
                    this.SaveProperty(value);
                }
            }
        }

        public void Initialize(CoreWebView2 coreWebView2)
        {
            this.coreWebView2 = coreWebView2;
            this.coreWebView2.NavigationCompleted += this.OnNavigationCompleted;
            this.NavigateToUrl();
        }

        private void NavigateToUrl()
        {
            try
            {
                this.coreWebView2.Navigate(this.Url);
            }
            catch (Exception ex)
            {
                this.logger.Error($"Navigation to {this.url} failed", ex);
            }
        }


        private void OnNavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            if (this.firstLoad)
            {
                this.firstLoad = false;
                this.IsVisible = true;
            }
        }
    }
}