// <copyright file="Plugin.cs" company="McLaren Applied Technologies Ltd.">
// Copyright (c) McLaren Applied Technologies Ltd.</copyright>

using MAT.Atlas.Api;
using MAT.Atlas.Api.Plugins;
using MAT.Atlas.Client.Platform.Plugins;
using MAT.Atlas.Client.Presentation.Plugins;

namespace MAT.Atlas.Plugins.Samples.HelloWorld
{
    [DisplayPlugin(
        View = typeof(HelloWorldView),
        ViewModel = typeof(HelloWorldViewModel),
        IconUri = IconResourcePath)]
    internal class Plugin : DisplayPlugin
    {
        private const string IconResourcePath =
            "pack://application:,,,/MAT.Atlas.Plugins.Samples.HelloWorld;component/Resources/Images/icon.png";

        public Plugin(
            IPluginVersion pluginVersion,
            IAssemblyInfoProvider assemblyInfoProvider,
            IPluginRegistration pluginRegistration)
            : base(pluginVersion, assemblyInfoProvider, pluginRegistration)
        {
        }
    }
}