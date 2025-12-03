// <copyright file="PluginModule.cs" company="Motion Applied Ltd.">
// Copyright (c) Motion Applied Ltd.</copyright>

using System.ComponentModel.Composition;

using Autofac;
using Autofac.Core;
using MAT.Atlas.Client.Presentation.Plugins;

namespace MAT.Atlas.Plugins.Samples.HelloWorld
{
    [Export(typeof(IModule))]
    public class PluginModule : Module
    {
        //Add dependencies to the DI container
        protected override void Load(ContainerBuilder builder)
        {
            DisplayPlugin<Plugin>.Register(builder);

            this.RegisterMyServices(builder);
        }
        private void RegisterMyServices(ContainerBuilder builder)
        {
            //register custom dependencies
            //e.g. builder.RegisterType<MyClass>().As<IMyInterface>();
        }
    }
}