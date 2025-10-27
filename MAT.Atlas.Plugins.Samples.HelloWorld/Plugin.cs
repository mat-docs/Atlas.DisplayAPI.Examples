using MAT.Atlas.Client.Presentation.Plugins;

namespace MAT.Atlas.Plugins.Samples.HelloWorld
{
    [DisplayPlugin(
        View = typeof(HelloWorldView),
        ViewModel = typeof(HelloWorldViewModel),
        IconUri = "Resources/Images/icon.png")]
    internal class Plugin : DisplayPlugin<Plugin>
    {
    }
}