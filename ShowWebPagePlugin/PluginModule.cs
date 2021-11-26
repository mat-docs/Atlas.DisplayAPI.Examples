using System.ComponentModel.Composition;
using Autofac;
using Autofac.Core;
using MAT.Atlas.Client.Presentation.Plugins;

namespace ShowWebPagePlugin
{
    [Export(typeof(IModule))]
    public sealed class PluginModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            Plugin.Register(builder);
        }

        [DisplayPlugin(
            View = typeof(SampleDisplayView),
            ViewModel = typeof(SampleDisplayViewModel),
            IconUri = "Resources/icon.png")]
        private sealed class Plugin : DisplayPlugin<Plugin>
        {
        }
    }
}