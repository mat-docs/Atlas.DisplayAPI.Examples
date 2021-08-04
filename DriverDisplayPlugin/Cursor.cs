using System.Windows;

using MAT.Atlas.Api.Core.Data;

namespace DriverDisplayPlugin
{
    public class Cursor
    {
        private readonly TimeRange timebase;
        private readonly long cursorValue;

        public Cursor(TimeRange timebase, long cursorValue)
        {
            this.timebase = timebase;
            this.cursorValue = cursorValue;
        }

        public bool GetCursorLine(Size extents, out (Point, Point) line)
        {
            if (!this.timebase.IsWithinRange(cursorValue))
            {
                line = default;
                return false;
            }

            var offset = this.cursorValue - this.timebase.Start;
            var extent = this.timebase.Length;
            var ratio = offset / (double)extent;
            var x = ratio * extents.Width;
            line = (new Point(x, 0), new Point(x, extents.Height));
            return true;
        }
    }
}
