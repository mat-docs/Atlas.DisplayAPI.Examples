using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;

using MAT.Atlas.Client.Platform.Parameters;
using MAT.OCS.Core;

using MathNet.Numerics;

namespace DriverDisplayPlugin
{
    public class Trace
    {
        private readonly double displayMin;
        private readonly double displayRange;
        private readonly List<TraceLine> lines = new List<TraceLine>();

        public Trace(
            IDisplayParameterContainer displayParameterContainer,
            IDisplayParameter displayParameter,
            ParameterValues values)
        {
            this.displayMin = Math.Min(
                displayParameter.SessionParameter.Minimum,
                displayParameter.SessionParameter.Maximum);
            var displayMax = Math.Max(
                displayParameter.SessionParameter.Minimum,
                displayParameter.SessionParameter.Maximum);
            this.displayRange = displayMax - this.displayMin;
            this.PointCount = values.SampleCount;
            var color = displayParameterContainer.Color;
            this.TraceColor = Color.FromArgb(color.A, color.R, color.G, color.B);
            ProcessValues(values);
        }

        public int PointCount { get; }

        public Color TraceColor { get; }

        public IEnumerable<(Point, Point)> GetLines(Size extents)
        {
            foreach (var line in this.lines)
            {
                var startPoint = new Point(
                    this.CalculateWindowX(line.Start.X, extents.Width),
                    this.CalculateWindowY(line.Start.Y, extents.Height));
                var endPoint = new Point(
                    this.CalculateWindowX(line.End.X, extents.Width),
                    this.CalculateWindowY(line.End.Y, extents.Height));

                yield return (startPoint, endPoint);
            }
        }

        private double CalculateWindowX(int x, double width)
        {
            var ratio = x / (double)this.PointCount;
            return ratio * width;
        }

        private double CalculateWindowY(double y, double height)
        {
            var zeroedValue = y - this.displayMin;
            var ratio = zeroedValue / displayRange;
            return height - (ratio * height);
        }

        private void ProcessValues(ParameterValues values)
        {
            TracePoint? startPoint = null;
            var isPreviousLineHorizontal = false;

            for (var x = 0; x < this.PointCount; ++x)
            {
                var isSample = values.DataStatus[x].HasFlag(DataStatusType.Sample);
                var isInterpolated = values.DataStatus[x].HasFlag(DataStatusType.Interpolated);
                var minValue = values.DataMin[x];
                var maxValue = values.DataMax[x];
                var isHole = (!isSample && !isInterpolated) || double.IsNaN(minValue) || double.IsNaN(maxValue);
                if (isHole)
                {
                    CloseExistingHorizontalLine(startPoint, ref isPreviousLineHorizontal, x - 1);
                    startPoint = null;
                    continue;
                }

                this.AddData(x, minValue, maxValue, ref startPoint, ref isPreviousLineHorizontal);
            }

            CloseExistingHorizontalLine(startPoint, ref isPreviousLineHorizontal, this.PointCount - 1);
        }

        private void AddData(int x, double minY, double maxY, ref TracePoint? startPoint, ref bool isPreviousLineHorizontal)
        {
            var isPoint = minY.AlmostEqual(maxY);

            if (!startPoint.HasValue)
            {
                if (isPoint)
                {
                    // Start of new horizontal line
                    startPoint = new TracePoint(x, minY);
                    isPreviousLineHorizontal = true;
                }
                else
                {
                    // Add vertical line between min and max
                    this.lines.Add(GetVerticalLine(x, minY, maxY));
                    startPoint = default(TracePoint);
                }

                return;
            }

            if (isPreviousLineHorizontal &&
                isPoint &&
                startPoint.Value.Y.AlmostEqual(minY))
            {
                return;
            }

            if (isPoint)
            {
                if (!isPreviousLineHorizontal)
                {
                    // Extend the end of the previous vertical line to point if necessary
                    var lastLine = this.lines.Last();
                    if (lastLine.End.Y < minY)
                    {
                        this.lines[this.lines.Count - 1] = GetVerticalLine(lastLine.End.X, lastLine.End.Y, minY);
                    }
                    else if (lastLine.Start.Y > minY)
                    {
                        this.lines[this.lines.Count - 1] = GetVerticalLine(lastLine.End.X, minY, lastLine.Start.Y);
                    }
                }
                else
                {
                    // Add step to this point
                    AddHorizontalLine(startPoint.Value, x);
                    var lastLine = this.lines.Last();
                    this.lines.Add(GetVerticalLine(x, lastLine.End.Y, minY));
                }

                // Start of new horizontal line
                startPoint = new TracePoint(x, minY);
                isPreviousLineHorizontal = true;
            }
            else
            {
                CloseExistingHorizontalLine(startPoint, ref isPreviousLineHorizontal, x);
                startPoint = default(TracePoint);

                // Extend min/max to the end of previous vertical line if necessary
                var lastLine = this.lines.Last();
                if (lastLine.End.Y < minY)
                {
                    minY = lastLine.End.Y;
                }

                if (lastLine.Start.Y > maxY)
                {
                    maxY = lastLine.Start.Y;
                }

                // Add vertical line between min and max
                this.lines.Add(GetVerticalLine(x, minY, maxY));
            }
        }

        private void AddHorizontalLine(TracePoint start, int x)
        {
            if (x > start.X)
            {
                this.lines.Add(new TraceLine(start, new TracePoint(x, start.Y)));
            }
        }

        private static TraceLine GetVerticalLine(int x, double minY, double maxY)
        {
            if (minY > maxY)
            {
                var temp = minY;
                minY = maxY;
                maxY = temp;
            }

            return new TraceLine(new TracePoint(x, minY), new TracePoint(x, maxY));
        }

        private void CloseExistingHorizontalLine(TracePoint? startPoint, ref bool isPreviousLineHorizontal, int x)
        {
            if (startPoint.HasValue &&
                isPreviousLineHorizontal)
            {
                this.AddHorizontalLine(startPoint.Value, x);
                isPreviousLineHorizontal = false;
            }
        }

        private struct TracePoint
        {
            public TracePoint(int x, double y)
            {
                this.X = x;
                this.Y = y;
            }

            public int X { get; }

            public double Y { get; }

            public override string ToString()
            {
                return $"{X}, {Y}";
            }
        }

        private struct TraceLine
        {
            public TraceLine(TracePoint start, TracePoint end)
            {
                Start = start;
                End = end;
            }

            public TracePoint Start { get; }

            public TracePoint End { get; }

            public override string ToString()
            {
                return $"({Start}) => ({End})";
            }
        }
    }
}