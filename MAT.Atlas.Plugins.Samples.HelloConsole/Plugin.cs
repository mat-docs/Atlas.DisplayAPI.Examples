// <copyright file="Plugin.cs" company="McLaren Applied Technologies Ltd.">
// Copyright (c) McLaren Applied Technologies Ltd.</copyright>

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