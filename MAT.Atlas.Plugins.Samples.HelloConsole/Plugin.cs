// <copyright file="Plugin.cs" company="McLaren Applied Technologies Ltd.">
// Copyright (c) McLaren Applied Technologies Ltd.</copyright>

using MAT.Atlas.Api;
using MAT.Atlas.Api.Plugins;
using MAT.Atlas.Client.Platform.Plugins;
using MAT.Atlas.Client.Presentation.Plugins;
using MAT.Atlas.Plugins.Samples.HelloConsole.ViewModels;
using MAT.Atlas.Plugins.Samples.HelloConsole.Views;

namespace MAT.Atlas.Plugins.Samples.HelloConsole
{
    [DisplayPlugin(
        View = typeof(HelloConsoleDisplayView),
        ViewModel = typeof(HelloConsoleDisplayViewModel),
        IconUri = IconResourcePath)]
    internal class Plugin : DisplayPlugin
    {
        private const string IconResourcePath = "pack://application:,,,/MAT.Atlas.Plugins.Samples.HelloConsole;component/Resources/Images/console.png";

        public Plugin(
            IPluginVersion pluginVersion,
            IAssemblyInfoProvider assemblyInfoProvider,
            IPluginRegistration pluginRegistration)
            : base(pluginVersion, assemblyInfoProvider, pluginRegistration)
        {
        }
    }
}