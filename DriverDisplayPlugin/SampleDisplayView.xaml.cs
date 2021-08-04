namespace DriverDisplayPlugin
{
    public partial class SampleDisplayView
    {
        public SampleDisplayView()
        {
            InitializeComponent();
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            if (this.DataContext is SampleDisplayViewModel vm)
            {
                vm.TraceVisual = this.TraceVisualLayer.Visual;
                vm.CursorVisual = this.CursorVisualLayer.Visual;
            }
        }
    }
}