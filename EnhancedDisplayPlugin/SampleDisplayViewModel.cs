// <copyright file="SampleDisplayViewModel.cs" company="Motion Applied Ltd.">
// Copyright (c) Motion Applied Ltd.</copyright>

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

using MAT.Atlas.Api.Core.Signals;
using MAT.Atlas.Client.Platform.Data;
using MAT.Atlas.Client.Platform.Data.Signals;
using MAT.Atlas.Client.Platform.Parameters;
using MAT.Atlas.Client.Platform.Sessions;
using MAT.Atlas.Client.Presentation.Commands;
using MAT.Atlas.Client.Presentation.Displays;
using MAT.Atlas.Client.Presentation.Plugins;
using MAT.Atlas.Client.Presentation.Services;
using MAT.OCS.Core;

namespace EnhancedDisplayPlugin
{
    [DisplayPluginSettings(ParametersMaxCount = 100)]
    public sealed class SampleDisplayViewModel : DisplayPluginViewModel
    {
        private static readonly Guid ValueAtCursorRequest = Guid.NewGuid();
        private static readonly Guid SamplesForTimebaseRequest = Guid.NewGuid();
        private readonly ISignalBus signalBus;
        private readonly IDataRequestSignalFactory dataRequestSignalFactory;
        private readonly ISessionService sessionService;
        private readonly ISessionSummaryService sessionSummaryService;
        private readonly ISessionCursorService sessionCursorService;
        private readonly SynchronizationContext synchronizationContext;
        private readonly List<IDisposable> disposables;
        private readonly List<string> pendingLogMessages = new List<string>();
        private readonly object logLock = new object();
        private readonly DispatcherTimer timer;
        private IDisplayParameterService displayParameterService;
        private string text = "My Second Display";
        private int fontSize;
        private Color textColor;
        private string logText;
        private bool logPropertiesPeriodically;

        public SampleDisplayViewModel(
            ISignalBus signalBus,
            IDataRequestSignalFactory dataRequestSignalFactory,
            ISessionService sessionService,
            ISessionSummaryService sessionSummaryService,
            ISessionCursorService sessionCursorService)
        {
            this.signalBus = signalBus;
            this.dataRequestSignalFactory = dataRequestSignalFactory;
            this.sessionService = sessionService;
            this.sessionSummaryService = sessionSummaryService;
            this.sessionCursorService = sessionCursorService;
            this.synchronizationContext = SynchronizationContext.Current;

            this.disposables = new List<IDisposable>
            {
                this.signalBus.Subscribe<SampleResultSignal>(this.HandleSampleResultSignal, r => r.SourceId == this.ScopeIdentity.Guid)
            };

            this.ClearLogCommand = new DelegateCommand(this.OnClearLog);
            this.LogPropertiesCommand = new DelegateCommand(this.OnLogProperties);
            this.CentreCursorCommand = new DelegateCommand(this.OnCentreCursor);

            this.timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(5),
            };

            this.timer.Tick += (_, __) => this.OnLogProperties();
        }

        [Category("Display")]
        [DisplayName("Display Text")]
        [Description("Used to change the text")]
        public string Text
        {
            get => this.text;
            set => this.SetProperty(ref this.text, value);
        }

        [Category("Appearance")]
        [DisplayName("Font Size")]
        [Description("Used to change the text size")]
        [Display(Order = 1)]
        public int FontSize
        {
            get => this.fontSize = this.ReadProperty(20);
            set
            {
                if (this.SetProperty(ref this.fontSize, value))
                {
                    this.SaveProperty(value);
                }
            }
        }

        [Category("Appearance")]
        [DisplayName("Text Color")]
        [Description("Used to change the text color")]
        [Display(Order = 0)]
        public Color TextColor
        {
            get => this.textColor = this.ReadProperty(Colors.White);
            set
            {
                if (this.SetProperty(ref this.textColor, value))
                {
                    this.SaveProperty(value);
                    this.OnPropertyChanged(nameof(this.TextBrush));
                }
            }
        }

        [Browsable(false)]
        public Brush TextBrush
        {
            get
            {
                var color = this.TextColor;
                return new SolidColorBrush(Color.FromArgb(color.A, color.R, color.G, color.B));
            }
        }

        [Browsable(false)]
        public string LogText
        {
            get => this.logText;
            set => this.SetProperty(ref this.logText, value);
        }

        [Browsable(false)]
        public bool LogPropertiesPeriodically
        {
            get => this.logPropertiesPeriodically;
            set
            {
                if (this.SetProperty(ref this.logPropertiesPeriodically, value))
                {
                    if (this.logPropertiesPeriodically)
                    {
                        this.timer.Start();
                    }
                    else
                    {
                        this.timer.Stop();
                    }
                }
            }
        }

        [Browsable(false)]
        public ICommand ClearLogCommand { get; }

        [Browsable(false)]
        public ICommand LogPropertiesCommand { get; }

        [Browsable(false)]
        public ICommand CentreCursorCommand { get; }

        public override void OnActiveDisplayPageChanged(bool isActive)
        {
            this.Log(nameof(OnActiveDisplayPageChanged));
            this.Log($"   IsActive: {isActive}");
            this.Log($"   CanRetrieveData: {this.CanRetrieveData}");
        }

        public override void OnCanRenderDisplayChanged(bool canRender)
        {
            this.Log(nameof(OnCanRenderDisplayChanged));
            this.Log($"   CanRender: {canRender}");
            this.Log($"   CanRetrieveData: {this.CanRetrieveData}");

            this.MakeDataRequests(true, true);
        }

        protected override void OnInitialised()
        {
            this.Log(nameof(OnInitialised));
            this.Log($"   CanRetrieveData: {this.CanRetrieveData}");

            // Access to Display specific services
            this.displayParameterService = this.ServiceContext.DisplayParameterService;
#if false
            //...Add parameter programmatically...
            this.displayParameterService.AddParameterContainer("vCar:Chassis");
#endif
            this.MakeDataRequests(true, true);
        }

        public override void OnParameterContainerAdded(IDisplayParameterContainer parameterContainer)
        {
            this.Log(nameof(OnParameterContainerAdded));
            this.LogParameterContainer(parameterContainer);
        }

        public override void OnParameterContainerRemoved(Guid instanceIdentifier)
        {
            this.Log(nameof(OnParameterContainerRemoved));
            this.Log($"   InstanceIdentifier: {instanceIdentifier}");
        }

        public override void OnParameterAdded(ParameterEventArgs args)
        {
            this.Log(nameof(OnParameterAdded));
            this.LogParameterContainer(args.ParameterContainer);
            this.LogParameter(args.Parameter);

            this.MakeDataRequests(true, true);
        }

        public override void OnParameterRemoved(ParameterEventArgs args)
        {
            this.Log(nameof(OnParameterRemoved));
            this.Log($"   Parameter.Name: {args.Parameter.Name}");
            this.Log($"   Parameter.Identifier: {args.Parameter.Identifier}");
            this.Log($"   Parameter.InstanceIdentifier: {args.Parameter.InstanceIdentifier}");

            this.MakeDataRequests(true, true);
        }

        public override void OnCompositeSessionLoaded(CompositeSessionEventArgs args)
        {
            this.Log(nameof(OnCompositeSessionLoaded));
            this.LogCompositeSessionContainer(args.CompositeSessionContainer);
            this.LogCompositeSession(args.CompositeSession);

            this.MakeDataRequests(true, true);
        }

        public override void OnCompositeSessionUnLoaded(CompositeSessionUnloadedEventArgs args)
        {
            this.Log(nameof(OnCompositeSessionUnLoaded));
            this.Log($"   CompositeSessionContainerInstanceIdentifier: {args.CompositeSessionContainerInstanceIdentifier}");
            this.Log($"   CompositeSessionInstanceIdentifier: {args.CompositeSessionInstanceIdentifier}");
            this.Log($"   CompositeSessionKey: {args.CompositeSessionKey}");

            this.MakeDataRequests(true, true);
        }

        public override void OnCompositeSessionContainerChanged()
        {
            if (this.ActiveCompositeSessionContainer == null)
            {
                this.Log($"{nameof(OnCompositeSessionContainerChanged)}(empty)");
                return;
            }

            this.Log(nameof(OnCompositeSessionContainerChanged));
            this.LogCompositeSessionContainer(this.ActiveCompositeSessionContainer);

            this.MakeDataRequests(true, true);
        }

        public override void OnCursorDataPointChanged(ICompositeSession compositeSession)
        {
            this.Log(nameof(OnCursorDataPointChanged));
            this.Log($"   CompositeSession.Identifier: {compositeSession.Identifier}");
            this.Log($"   CompositeSession.InstanceIdentifier: {compositeSession.InstanceIdentifier}");
            this.Log($"   CompositeSession.Key: {compositeSession.Key}");
            this.Log($"   CompositeSession.CursorPoint: {FormatNanoseconds(compositeSession.CursorPoint)}");

            this.MakeDataRequests(compositeSession, true, false);
        }

        public override void OnSessionTimeRangeChanged(ICompositeSession compositeSession)
        {
            this.Log(nameof(OnSessionTimeRangeChanged));
            this.Log($"   CompositeSession.Identifier: {compositeSession.Identifier}");
            this.Log($"   CompositeSession.InstanceIdentifier: {compositeSession.InstanceIdentifier}");
            this.Log($"   CompositeSession.Key: {compositeSession.Key}");
            this.Log($"   CompositeSession.TimebaseRange: {FormatNanoseconds(compositeSession.TimebaseRange.Start)} -> {FormatNanoseconds(compositeSession.TimebaseRange.End)}");

            this.MakeDataRequests(compositeSession, false, true);
        }

        protected override void OnDisposeManagedResources()
        {
            base.OnDisposeManagedResources();
            this.disposables.ForEach(d => d.Dispose());
        }

        protected override void OnPropertyChanged(PropertyChangedEventArgs propertyChangedEventArgs)
        {
            var propertyName = propertyChangedEventArgs.PropertyName;
            if (propertyName != nameof(this.LogText))
            {
                this.Log(nameof(OnPropertyChanged));
                this.Log($"   PropertyName: {propertyName}");
            }

            base.OnPropertyChanged(propertyChangedEventArgs);
        }

        private void MakeDataRequests(bool cursorChanged, bool timebaseChanged)
        {
            // NB: this.ActiveCompositeSessionContainer may throw an exception if display is not initialised
            if (!this.CanRetrieveData)
            {
                this.Log($"!{nameof(MakeDataRequests)}({cursorChanged}, {timebaseChanged})");
                this.Log($"    {nameof(this.CanRetrieveData)}=false");
                return;
            }

            if (this.ActiveCompositeSessionContainer == null)
            {
                this.Log($"!{nameof(MakeDataRequests)}({cursorChanged}, {timebaseChanged})");
                this.Log($"    {nameof(this.ActiveCompositeSessionContainer)}=null");
                return;
            }

            if (!this.ActiveCompositeSessionContainer.IsPrimaryCompositeSessionAvailable)
            {
                this.Log($"!{nameof(MakeDataRequests)}({cursorChanged}, {timebaseChanged})");
                this.Log($"    {nameof(this.ActiveCompositeSessionContainer.IsPrimaryCompositeSessionAvailable)}=false");
                return;
            }

            var primarySession =  this.ActiveCompositeSessionContainer.CompositeSessions.FirstOrDefault(c => c.IsPrimary);
            if (primarySession == null)
            {
                this.Log($"!{nameof(MakeDataRequests)}({cursorChanged}, {timebaseChanged})");
                this.Log($"    {nameof(this.ActiveCompositeSessionContainer.CompositeSessions)} no primary session found");
                return;
            }

            this.MakeDataRequests(primarySession, cursorChanged, timebaseChanged);
        }

        private void MakeDataRequests(ICompositeSession compositeSession, bool cursorChanged, bool timebaseChanged)
        {
            // NB: this.ActiveCompositeSessionContainer may throw an exception if display is not initialised
            if (!this.CanRetrieveData)
            {
                this.Log($"!{nameof(MakeDataRequests)}({cursorChanged}, {timebaseChanged})");
                this.Log($"    {nameof(this.CanRetrieveData)}=false");
                return;
            }

            if (!this.displayParameterService.ParameterContainers.Any())
            {
                this.Log($"!{nameof(MakeDataRequests)}({cursorChanged}, {timebaseChanged})");
                this.Log($"    {nameof(this.displayParameterService.ParameterContainers)}=empty");
                return;
            }

            if (!this.displayParameterService.PrimaryParameters.Any())
            {
                this.Log($"!{nameof(MakeDataRequests)}({cursorChanged}, {timebaseChanged})");
                this.Log($"    {nameof(this.displayParameterService.PrimaryParameters)}=empty");
                return;
            }

            this.Log($"{nameof(MakeDataRequests)}({cursorChanged}, {timebaseChanged})");

            if (cursorChanged)
            {
                foreach (var primaryParameter in this.displayParameterService.PrimaryParameters)
                {
                    var signal = this.dataRequestSignalFactory.CreateSampleRequestSignal(
                        this.ScopeIdentity.Guid,
                        ValueAtCursorRequest,
                        compositeSession.Key,
                        primaryParameter,
                        compositeSession.CursorPoint + 1,
                        1,
                        SampleDirection.Previous);

                    this.signalBus.Send(signal);
                }
            }

            if (timebaseChanged)
            {
                foreach (var primaryParameter in this.displayParameterService.PrimaryParameters)
                {
                    var signal = this.dataRequestSignalFactory.CreateSampleRequestSignal(
                        this.ScopeIdentity.Guid,
                        SamplesForTimebaseRequest,
                        compositeSession.Key,
                        primaryParameter,
                        compositeSession.TimebaseRange,
                        SampleDirection.Next);

                    this.signalBus.Send(signal);
                }
            }
        }

        private void HandleSampleResultSignal(SampleResultSignal signal)
        {
            var request = signal.Data.Request;
            var result = signal.Data;

            var parameterValues = result.ParameterValues;
            parameterValues.Lock();

            try
            {
                if (parameterValues.SampleCount == 0)
                {
                    this.Log($"!{nameof(HandleSampleResultSignal)}({(request.RequestId == ValueAtCursorRequest ? nameof(ValueAtCursorRequest) : nameof(SamplesForTimebaseRequest))})");
                    this.Log($"   {nameof(parameterValues.SampleCount)} == 0");
                    return;
                }

                if (request.RequestId == ValueAtCursorRequest)
                {
                    this.Log($"{nameof(HandleSampleResultSignal)}({nameof(ValueAtCursorRequest)})");
                    this.Log($"   Parameter: {request.Parameter.Identifier}, Value: {parameterValues.Data[0]}");
                    return;
                }

                if (request.RequestId == SamplesForTimebaseRequest)
                {
                    var minValue = double.MaxValue;
                    var maxValue = double.MinValue;
                    for (var i = 0; i < parameterValues.SampleCount; ++i)
                    {
                        if (parameterValues.DataStatus[i].HasFlag(DataStatusType.Sample))
                        {
                            minValue = Math.Min(minValue, parameterValues.Data[i]);
                            maxValue = Math.Max(maxValue, parameterValues.Data[i]);
                        }
                    }

                    this.Log($"{nameof(HandleSampleResultSignal)}({nameof(SamplesForTimebaseRequest)})");
                    this.Log($"   Parameter: {request.Parameter.Identifier}, Sample Count: {parameterValues.SampleCount}, Min: {minValue}, Max: {maxValue}");
                }
            }
            finally
            {
                parameterValues.Unlock();
            }
        }

        private void OnLogProperties()
        {
            this.Log(nameof(OnLogProperties));
            this.Log($"   IsSelected: {this.IsSelected}");
            this.Log($"   CanRetrieveData: {this.CanRetrieveData}");
            this.Log($"   ScopeIdentity.Identity: {this.ScopeIdentity.Identity}");

            if (!this.CanRetrieveData)
            {
                return;
            }

            if (this.ActiveCompositeSessionContainer == null)
            {
                this.Log("   ActiveCompositeSessionContainer: <null>");
                return;
            }

            this.LogCompositeSessionContainer(this.ActiveCompositeSessionContainer);

            foreach (var compositeSession in this.ActiveCompositeSessionContainer.CompositeSessions)
            {
                LogCompositeSession(compositeSession);
            }

            var parameterContainers = this.displayParameterService.ParameterContainers;
            this.Log($"   ParameterContainersCount: {parameterContainers.Count}");
            foreach (var parameterContainer in parameterContainers)
            {
                LogParameterContainer(parameterContainer);
                foreach (var parameter in parameterContainer.Parameters)
                {
                    LogParameter(parameter);
                }
            }
        }

        private void LogCompositeSessionContainer(ICompositeSessionContainer compositeSessionContainer)
        {
            this.Log($"   CompositeSessionContainer.Name: {compositeSessionContainer.Name}");
            this.Log($"   CompositeSessionContainer.Key: {compositeSessionContainer.Key}");
            this.Log($"   CompositeSessionContainer.IsPrimaryCompositeSessionAvailable: {compositeSessionContainer.IsPrimaryCompositeSessionAvailable}");
            this.Log($"   CompositeSessionContainer.PanningState: {compositeSessionContainer.PanningState}");
            this.Log($"   CompositeSessionContainer.CompositeSessionsCount: {compositeSessionContainer.CompositeSessions.Count()}");
        }

        private void LogCompositeSession(ICompositeSession compositeSession)
        {
            this.Log($"   CompositeSession.Identifier: {compositeSession.Identifier}");
            this.Log($"   CompositeSession.InstanceIdentifier: {compositeSession.InstanceIdentifier}");
            this.Log($"   CompositeSession.Key: {compositeSession.Key}");
            this.Log($"   CompositeSession.IsPrimary: {compositeSession.IsPrimary}");
            this.Log($"   CompositeSession.State: {compositeSession.State}");
            this.Log($"   CompositeSession.SessionEpoch: {compositeSession.SessionEpoch}");
            this.Log($"   CompositeSession.LapsCount: {compositeSession.Laps.Count}");
            this.Log($"   CompositeSession.Offset: {FormatNanoseconds(compositeSession.Offset)}");
            this.Log($"   CompositeSession.CursorPoint: {FormatNanoseconds(compositeSession.CursorPoint)}");
            this.Log($"   CompositeSession.TimeRange: {FormatNanoseconds(compositeSession.TimeRange.Start)} -> {FormatNanoseconds(compositeSession.TimeRange.End)}");
            this.Log($"   CompositeSession.TimebaseRange: {FormatNanoseconds(compositeSession.TimebaseRange.Start)} -> {FormatNanoseconds(compositeSession.TimebaseRange.End)}");

            var parametersCount = this.sessionService.GetParametersCount(compositeSession.Key);
            this.Log($"   CompositeSession.ParametersCount: {parametersCount}");

            var sessions = compositeSession.Sessions.ToList();
            this.Log($"   CompositeSession.SessionsCount: {sessions.Count}");
            foreach (var session in sessions)
            {
                this.Log($"   Session.Name: {session.Name}");
                this.Log($"   Session.Key: {session.Key}");
                this.Log($"   Session.InstanceIdentifier: {session.InstanceIdentifier}");
                this.Log($"   Session.State: {session.State}");
                this.Log($"   Session.TimeRange: {FormatNanoseconds(session.TimeRange.Start)} -> {FormatNanoseconds(session.TimeRange.End)}");

                var sessionSummary = this.sessionSummaryService.GetSessionSummary(session.Key);
                this.Log($"   Session.Identifier: {sessionSummary.Identifier}");
                this.Log($"   Session.FileSessionPath: {sessionSummary.FileSessionPath}");
                this.Log($"   Session.ConnectionString: {sessionSummary.ConnectionString}");
                this.Log($"   Session.SessionType: {sessionSummary.SessionType}");
                this.Log($"   Session.StartTime: {FormatNanoseconds(sessionSummary.StartTime)}");
                this.Log($"   Session.EndTime: {FormatNanoseconds(sessionSummary.EndTime)}");
                this.Log($"   Session.LapCount: {sessionSummary.LapCount}");
                this.Log($"   Session.ItemsCount: {sessionSummary.Items.Count}");
                foreach (var item in sessionSummary.Items)
                {
                    this.Log($"   Session.Item({item.Key}): {(item.Value?.ToString() ?? "<null>")}");
                }
            }
        }

        private void LogParameterContainer(IDisplayParameterContainer parameterContainer)
        {
            this.Log($"   ParameterContainer.Name: {parameterContainer.Name}");
            this.Log($"   ParameterContainer.Identifier: {parameterContainer.Identifier}");
            this.Log($"   ParameterContainer.InstanceIdentifier: {parameterContainer.InstanceIdentifier}");
            this.Log($"   ParameterContainer.Color: {parameterContainer.Color}");
            this.Log($"   ParameterContainer.ParametersCount: {parameterContainer.Parameters.Count}");
        }

        private void LogParameter(IDisplayParameter parameter)
        {
            this.Log($"   Parameter.Name: {parameter.Name}");
            this.Log($"   Parameter.FormattedName: {parameter.FormattedName}");
            this.Log($"   Parameter.Identifier: {parameter.Identifier}");
            this.Log($"   Parameter.InstanceIdentifier: {parameter.InstanceIdentifier}");
            this.Log($"   Parameter.CompositeSessionKey: {parameter.CompositeSessionKey}");
            this.Log($"   Parameter.IsPrimary: {parameter.IsPrimary}");
            this.Log($"   Parameter.IsAvailable: {parameter.IsAvailable}");
        }

        private void OnCentreCursor()
        {
            if (!this.CanRetrieveData)
            {
                this.Log($"!{nameof(OnCentreCursor)}");
                this.Log($"    {nameof(this.CanRetrieveData)}=false");
                return;
            }

            if (!this.ActiveCompositeSessionContainer.IsPrimaryCompositeSessionAvailable)
            {
                this.Log($"!{nameof(OnCentreCursor)}");
                this.Log($"    {nameof(this.ActiveCompositeSessionContainer.IsPrimaryCompositeSessionAvailable)}=false");
                return;
            }

            var primarySession = this.ActiveCompositeSessionContainer.CompositeSessions.FirstOrDefault(c => c.IsPrimary);
            if (primarySession == null)
            {
                this.Log($"!{nameof(OnCentreCursor)}");
                this.Log($"    {nameof(this.ActiveCompositeSessionContainer.CompositeSessions)} no primary session found");
                return;
            }

            var centreTimestamp = (primarySession.TimebaseRange.Start + primarySession.TimebaseRange.End) / 2;

            this.Log($"{nameof(OnCentreCursor)}({FormatNanoseconds(centreTimestamp)})");

            this.sessionCursorService.MoveCursor(primarySession, centreTimestamp);
        }

        private void OnClearLog()
        {
            this.LogText = string.Empty;
        }

        private void Log(string message)
        {
            LogMessage(
                this.synchronizationContext,
                this.pendingLogMessages,
                this.logLock,
                () => this.LogText,
                v => this.LogText = v,
                message);
        }

        private static void LogMessage(
            SynchronizationContext synchronizationContext,
            ICollection<string> pendingMessages,
            object logLock,
            Func<string> readLog,
            Action<string> writeLog,
            string message)
        {
            bool firstNewMessage;
            lock (logLock)
            {
                firstNewMessage = !pendingMessages.Any();
                pendingMessages.Add(message);
            }

            if (firstNewMessage)
            {
                synchronizationContext.Post(
                    _ =>
                    {
                        lock (logLock)
                        {
                            var stringBuilder = new StringBuilder(readLog());

                            foreach (var pendingMessage in pendingMessages)
                            {
                                stringBuilder.AppendLine(pendingMessage);
                            }
                            pendingMessages.Clear();

                            writeLog(stringBuilder.ToString());
                        }
                    },
                    null);
            }
        }

        public static TimeSpan FromNanoseconds(long value) => TimeSpan.FromTicks(value / 100);

        public static string FormatNanoseconds(long value) => Format(FromNanoseconds(value));

        public static string Format(TimeSpan timeSpan) => timeSpan.ToString(@"hh\:mm\:ss\.fff");
    }
}