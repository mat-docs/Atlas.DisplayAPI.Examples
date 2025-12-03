// <copyright file="Plugin.cs" company="Motion Applied Ltd.">
// Copyright (c) Motion Applied Ltd.</copyright>

using MAT.Atlas.Client.Presentation.Plugins;
using MAT.Atlas.Plugins.Samples.HelloConsole.ViewModels;
using MAT.Atlas.Plugins.Samples.HelloConsole.Views;

namespace MAT.Atlas.Plugins.Samples.HelloConsole
{
    [DisplayPlugin(
        View = typeof(HelloConsoleDisplayView),
        ViewModel = typeof(HelloConsoleDisplayViewModel),
        IconUri = "Resources/Images/console.png")]
    internal class Plugin : DisplayPlugin<Plugin>
    {
    }
}