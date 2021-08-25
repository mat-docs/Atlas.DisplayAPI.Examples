using System.Windows.Controls;
using Microsoft.Xaml.Behaviors;

namespace EnhancedDisplayPlugin.Behaviours
{
    public sealed class ScrollToEnd : Behavior<TextBox>
    {
        protected override void OnAttached()
        {
            base.OnAttached();
            this.AssociatedObject.TextChanged += this.OnTextChanged;
        }

        protected override void OnDetaching()
        {
            this.AssociatedObject.TextChanged -= this.OnTextChanged;
            base.OnDetaching();
        }

        private void OnTextChanged(object sender, TextChangedEventArgs e)
        {
            if (this.AssociatedObject.LineCount > 0)
            {
                this.AssociatedObject.ScrollToLine(this.AssociatedObject.LineCount - 1);
            }
        }
    }
}