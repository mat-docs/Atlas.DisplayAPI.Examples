using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

using DisplayPluginLibrary;

using MAT.Atlas.Api.Core.Diagnostics;
using MAT.Atlas.Api.Core.Signals;
using MAT.Atlas.Client.Platform.Data;
using MAT.Atlas.Client.Platform.Data.Signals;
using MAT.Atlas.Client.Platform.Parameters;
using MAT.Atlas.Client.Platform.Sessions;
using MAT.Atlas.Client.Presentation.Plugins;

namespace DriverDisplayPlugin
{
    [DisplayPluginSettings(ParametersMaxCount = 100)]
    public class SampleDisplayViewModel : TemplateDisplayViewModelBase
    {
        private static readonly Color BlueOff = Color.FromArgb(255, 0, 0, 40);
        private static readonly Color BlueOn = Color.FromArgb(255, 0, 0, 255);
        private static readonly Color GreenOff = Color.FromArgb(255, 0, 40, 0);
        private static readonly Color GreenOn = Color.FromArgb(255, 0, 255, 0);
        private static readonly Color RedOff = Color.FromArgb(255, 40, 0, 0);
        private static readonly Color RedOn = Color.FromArgb(255, 255, 0, 0);
        private static readonly Color YellowOff = Color.FromArgb(255, 40, 40, 0);
        private static readonly Color YellowOn = Color.FromArgb(255, 255, 255, 0);
        private readonly OperationTracker<(DataRequestSignal Signal, IDisplayParameterContainer ParameterContainer)> dataRequestTracker;
        private readonly OperationTracker<SampleRequestSignal> sampleRequestTracker;
        private readonly OperationTracker<Trace> redrawTraceRequestTracker;
        private readonly OperationTracker<Cursor> redrawCursorRequestTracker;
        private int dataRequestSampleCount;

        public SampleDisplayViewModel(
            ISignalBus signalBus,
            IDataRequestSignalFactory dataRequestSignalFactory,
            ILogger logger)
        : base(signalBus, dataRequestSignalFactory, logger)
        {
            this.Disposables.Add(signalBus.Subscribe<SampleResultSignal>(this.HandleSampleResultSignal, r => r.SourceId == this.ScopeIdentity.Guid));
            this.Disposables.Add(signalBus.Subscribe<DataResultSignal>(this.HandleDataResultSignal, r => r.SourceId == this.ScopeIdentity.Guid));

            this.dataRequestTracker = new OperationTracker<(DataRequestSignal Signal, IDisplayParameterContainer)>(
                ThrottleInterval,
                operation => signalBus.Send(operation.Signal));

            this.sampleRequestTracker = new OperationTracker<SampleRequestSignal>(
                ThrottleInterval,
                signalBus.Send);

            this.redrawTraceRequestTracker = new OperationTracker<Trace>(
                ThrottleInterval,
                async trace => await this.ExecuteOnUiAsync(() => this.Redraw(trace)));

            this.redrawCursorRequestTracker = new OperationTracker<Cursor>(
                ThrottleInterval,
                async cursor => await this.ExecuteOnUiAsync(() => this.Redraw(cursor)));
        }

        [Browsable(false)]
        public List<LedViewModel> TopShiftLights { get; } = new List<LedViewModel>()
        {
            new LedViewModel(GreenOff, GreenOn, 14, 15),
            new LedViewModel(GreenOff, GreenOn, 13, 10),
            new LedViewModel(GreenOff, GreenOn, 12, 5),
            new LedViewModel(GreenOff, GreenOn, 11, 0),
            new LedViewModel(GreenOff, GreenOn, 10, -5),
            new LedViewModel(RedOff, RedOn, 9, -10),
            new LedViewModel(RedOff, RedOn, 8, -10),
            new LedViewModel(RedOff, RedOn, 7, -10),
            new LedViewModel(RedOff, RedOn, 6, -10),
            new LedViewModel(RedOff, RedOn, 5, -10),
            new LedViewModel(BlueOff, BlueOn, 4, -5),
            new LedViewModel(BlueOff, BlueOn, 3, 0),
            new LedViewModel(BlueOff, BlueOn, 2, 5),
            new LedViewModel(BlueOff, BlueOn, 1, 10),
            new LedViewModel(BlueOff, BlueOn, 0, 15)
        };

        [Browsable(false)]
        public List<LedViewModel> LeftShiftLights { get; } = new List<LedViewModel>()
        {
            new LedViewModel(YellowOff, YellowOn, 20, 0),
            new LedViewModel(RedOff, RedOn, 18, 0),
            new LedViewModel(BlueOff, BlueOn, 16, 0)
        };

        [Browsable(false)]
        public List<LedViewModel> RightShiftLights { get; } = new List<LedViewModel>()
        {
            new LedViewModel(YellowOff, YellowOn, 19, 0),
            new LedViewModel(RedOff, RedOn, 17, 0),
            new LedViewModel(BlueOff, BlueOn, 15, 0)
        };

        public int DataRequestSampleCount
        {
            get => this.dataRequestSampleCount = this.ReadProperty(1000);
            set
            {
                if (this.SetProperty(ref this.dataRequestSampleCount, value))
                {
                    this.SaveProperty(value);
                    this.MakeDataRequests(false, true);
                }
            }
        }

        [Browsable(false)]
        public IVisual CursorVisual { get; set; }

        [Browsable(false)]
        public IVisual TraceVisual { get; set; }

        protected override Task OnMakeCursorDataRequestsAsync(ICompositeSession compositeSession)
        {
            if (this.DisplayParameterService.PrimaryParameters.Count < 1)
            {
                return Task.CompletedTask;
            }

            var signal = this.DataRequestSignalFactory.CreateSampleRequestSignal(
                this.ScopeIdentity.Guid,
                compositeSession.Key,
                this.DisplayParameterService.PrimaryParameters.FirstOrDefault(),
                compositeSession.CursorPoint + 1,
                1,
                SampleDirection.Previous);

            this.sampleRequestTracker.Add(signal);

            var cursor = new Cursor(compositeSession.TimebaseRange, compositeSession.CursorPoint);
            this.redrawCursorRequestTracker.Add(cursor);
            return Task.CompletedTask;
        }

        protected override Task OnMakeTimelineDataRequestsAsync(ICompositeSession compositeSession)
        {
            if (this.DisplayParameterService.PrimaryParameters.Count < 2)
            {
                return Task.CompletedTask;
            }

            // Second parameter is the trace line
            var signal = this.DataRequestSignalFactory.CreateDataRequestSignal(
                this.ScopeIdentity.Guid,
                this.DisplayParameterService.PrimaryParameters.Skip(1).FirstOrDefault(),
                compositeSession.TimebaseRange,
                this.DataRequestSampleCount,
                SampleMode.MaximumToMinimum);

            this.dataRequestTracker.Add((signal, this.DisplayParameterService.ParameterContainers.Skip(1).FirstOrDefault()));
            return Task.CompletedTask;
        }

        private void HandleSampleResultSignal(SampleResultSignal signal)
        {
            var result = signal.Data;

            var parameterValues = result.ParameterValues;
            parameterValues.Lock();

            try
            {
                if (parameterValues.SampleCount == 1)
                {
                    var shiftLightsValue = (uint) parameterValues.Data[0];
                    var bits = new BitArray(BitConverter.GetBytes(shiftLightsValue));
                    this.TopShiftLights.ForEach(sl => sl.UpdateColor(bits));
                    this.LeftShiftLights.ForEach(sl => sl.UpdateColor(bits));
                    this.RightShiftLights.ForEach(sl => sl.UpdateColor(bits));
                }
            }
            finally
            {
                parameterValues.Unlock();
                this.sampleRequestTracker.Complete();
            }
        }

        private void HandleDataResultSignal(DataResultSignal signal)
        {
            var request = signal.Data.Request;
            var result = signal.Data;

            var parameterValues = result.ParameterValues;
            parameterValues.Lock();

            try
            {
                if (parameterValues.SampleCount > 0 &&
                    this.dataRequestTracker.GetCurrent(out var currentOperation))
                {
                    var trace = new Trace(currentOperation.ParameterContainer, request.Parameter, parameterValues);
                    this.redrawTraceRequestTracker.Add(trace);
                }
            }
            finally
            {
                parameterValues.Unlock();
                this.dataRequestTracker.Complete();
            }
        }

        private void Redraw(Trace trace)
        {
            try
            {
                var extents = this.TraceVisual.Extents;
                if (!this.IsDisplayVisible || extents.Width == 0 || extents.Height == 0)
                {
                    this.TraceVisual.Draw(delegate { });
                    return;
                }

                this.TraceVisual.Draw(
                    dc =>
                    {
                        dc.DrawRectangle(
                            Brushes.Transparent,
                            new Pen(Brushes.White, 1),
                            new Rect(new Point(0, 0), extents));

                        var tracePen = new Pen(new SolidColorBrush(trace.TraceColor), extents.Width / trace.PointCount);
                        foreach (var (start, end) in trace.GetLines(extents))
                        {
                            dc.DrawLine(tracePen, start, end);
                        }
                    });
            }
            finally
            {
                redrawTraceRequestTracker.Complete();
            }
        }

        private void Redraw(Cursor cursor)
        {
            try
            {
                var extents = this.CursorVisual.Extents;
                if (!this.IsDisplayVisible ||
                    extents.Width == 0 ||
                    extents.Height == 0 ||
                    !cursor.GetCursorLine(extents, out var cursorLine))
                {
                    this.CursorVisual.Draw(delegate { });
                    return;
                }

                this.CursorVisual.Draw(
                    dc =>
                    {
                        var cursorPen = new Pen(Brushes.White, 1);
                        dc.DrawLine(cursorPen, cursorLine.Item1, cursorLine.Item2);
                    });
            }
            finally
            {
                redrawCursorRequestTracker.Complete();
            }
        }
    }
}