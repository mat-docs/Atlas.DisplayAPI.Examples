using System.ComponentModel.Composition;
using Autofac;
using Autofac.Core;

using MAT.Atlas.Client.Presentation.Plugins;

namespace NumericCompareDisplayPlugin
{
    [Export(typeof(IModule))]
    public class PluginModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            Plugin.Register(builder);
        }

        [DisplayPlugin(
            View = typeof(SampleDisplayView),
            ViewModel = typeof(SampleDisplayViewModel),
            IconUri = "Resources/icon.png")]
        private class Plugin : DisplayPlugin<Plugin>
        {
        }
    }
}