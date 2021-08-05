using System.Collections;
using System.Windows.Media;

using MAT.Atlas.Api.Core.Presentation;

namespace DriverDisplayPlugin
{
    public class LedViewModel : BindableBase
    {
        private readonly Color offColor;
        private readonly Color onColor;
        private readonly int bitIndex;
        private Color color;
        private double shiftY;

        public LedViewModel(Color offColor, Color onColor, int bitIndex, double shiftY)
        {
            this.offColor = offColor;
            this.onColor = onColor;
            this.bitIndex = bitIndex;
            this.shiftY = shiftY;

            this.color = offColor;
        }

        public Color Color
        {
            get => this.color;
            set => SetProperty(ref this.color, value);
        }

        public double ShiftY
        {
            get => this.shiftY;
            set => SetProperty(ref this.shiftY, value);
        }

        public void UpdateColor(BitArray bits) => this.Color = bits[bitIndex] ? this.onColor : this.offColor;
    }
}