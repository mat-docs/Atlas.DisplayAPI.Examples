using System;
using System.Windows;
using System.Windows.Media;

namespace DisplayPluginLibrary
{
    public interface IVisual
    {
        Size Extents { get; }

        void Draw(Action<DrawingContext> drawAction);
    }

    public sealed class VisualLayer : FrameworkElement
    {
        private readonly ThisVisual thisVisual;

        public VisualLayer()
        {
            this.thisVisual = new ThisVisual(this);
            this.AddVisualChild(thisVisual);
            this.AddLogicalChild(thisVisual);
        }

        public IVisual Visual => this.thisVisual;

        protected override int VisualChildrenCount { get; } = 1;

        protected override Visual GetVisualChild(int index) => thisVisual;

        private sealed class ThisVisual : DrawingVisual, IVisual
        {
            private readonly VisualLayer visualLayer;

            public ThisVisual(VisualLayer visualLayer)
            {
                this.visualLayer = visualLayer;
            }

            public Size Extents => new Size(visualLayer.ActualWidth, visualLayer.ActualHeight);

            public void Draw(Action<DrawingContext> drawAction)
            {
                using (var drawingContext = this.RenderOpen())
                {
                    drawAction(drawingContext);
                }
            }
        }
    }
}