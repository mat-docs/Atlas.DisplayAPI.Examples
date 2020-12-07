// <copyright file="PluginModule.cs" company="McLaren Applied Technologies Ltd.">
// Copyright (c) McLaren Applied Technologies Ltd.</copyright>

using System.ComponentModel.Composition;

using Autofac;
using Autofac.Core;

using MAT.Atlas.Api.Presentation.Plugins;

namespace MAT.Atlas.Plugins.Samples.HelloWorld
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
            builder.RegisterType<HelloWorldView>();
            builder.RegisterType<HelloWorldViewModel>();

            //Register custom dependencies
            //e.g. builder.RegisterType<MyClass>().As<IMyInterface>();
        }
    }
}