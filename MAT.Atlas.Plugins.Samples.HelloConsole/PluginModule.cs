// <copyright file="PluginModule.cs" company="McLaren Applied Technologies Ltd.">
// Copyright (c) McLaren Applied Technologies Ltd.</copyright>

using Autofac;
using Autofac.Core;
using MAT.Atlas.Api.Presentation.Plugins;
using System.ComponentModel.Composition;
using MAT.Atlas.Plugins.Samples.HelloConsole.ViewModels;
using MAT.Atlas.Plugins.Samples.HelloConsole.Views;

namespace MAT.Atlas.Plugins.Samples.HelloConsole
{
    [Export(typeof(IModule))]
    public class PluginModule : Module
    {
        //Add dependencies to the DI container
        protected override void Load(ContainerBuilder builder)
        {
            // register the plugin
            builder.RegisterType<Plugin>().As<IAtlasDisplayPlugin>();

            // register the display view and view model
            builder.RegisterType<HelloConsoleDisplayView>();
            builder.RegisterType<HelloConsoleDisplayViewModel>();

            this.RegisterMyServices(builder);
        }

        private void RegisterMyServices(ContainerBuilder builder)
        {
            //register custom dependencies
            //e.g. builder.RegisterType<MyClass>().As<IMyInterface>();
        }
    }
}