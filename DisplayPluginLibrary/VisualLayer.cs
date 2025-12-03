// <copyright file="VisualLayer.cs" company="Motion Applied Ltd.">
// Copyright (c) Motion Applied Ltd.</copyright>

using System;
using System.Windows;
using System.Windows.Media;

namespace DisplayPluginLibrary
{
    /// <summary>
    ///     Interface to draw graphics using DrawingContext class.
    /// </summary>
    public interface IVisual
    {
        /// <summary>
        ///     Extents of area where graphics are drawn.
        /// </summary>
        Size Extents { get; }

        /// <summary>
        ///     Draw graphics.
        /// </summary>
        /// <param name="drawAction">Action executed to draw graphics using DrawingContext.</param>
        void Draw(Action<DrawingContext> drawAction);
    }

    /// <summary>
    ///     XAML element to draw graphics using DrawingContext class.
    /// </summary>
    public sealed class VisualLayer : FrameworkElement
    {
        private readonly ThisVisual thisVisual;

        /// <summary>
        ///     Constructor.
        /// </summary>
        public VisualLayer()
        {
            this.thisVisual = new ThisVisual(this);
            this.AddVisualChild(thisVisual);
            this.AddLogicalChild(thisVisual);
        }

        /// <summary>
        ///     Access to interface to draw graphics.
        /// </summary>
        public IVisual Visual => this.thisVisual;

        /// <inheritdoc />
        protected override int VisualChildrenCount { get; } = 1;

        /// <inheritdoc />
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