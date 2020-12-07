// <copyright file="HelloConsoleDisplayViewModel.cs" company="McLaren Applied Technologies Ltd.">
// Copyright (c) McLaren Applied Technologies Ltd.</copyright>

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Concurrency;
using System.Threading.Tasks;
using System.Windows.Input;

using MAT.Atlas.Api;
using MAT.Atlas.Api.Presentation;
using MAT.Atlas.Api.Presentation.Commands;
using MAT.Atlas.Api.Signals;
using MAT.Atlas.Client.Platform.Data;
using MAT.Atlas.Client.Platform.Data.Signals;
using MAT.Atlas.Client.Platform.Parameters;
using MAT.Atlas.Client.Platform.Sessions;
using MAT.Atlas.Client.Presentation.Displays;
using MAT.Atlas.Client.Presentation.Plugins;
using MAT.Atlas.Client.Presentation.Services;
using MAT.Atlas.Plugins.Samples.HelloConsole.Helpers;
using MAT.OCS.Core;

namespace MAT.Atlas.Plugins.Samples.HelloConsole.ViewModels
{
    [DisplayPluginSettings(ParametersMaxCount = 2)]
    internal sealed class HelloConsoleDisplayViewModel : DisplayPluginViewModel
    {
        private readonly IDataRequestSignalFactory dataRequestSignalFactory;
        private readonly IDisposable dataRequestSubscription;
        private readonly IDispatcherSchedulerProvider dispatcherSchedulerProvider;
        private readonly IDisposable sampleCompositeRequestSubscription;
        private readonly IDisposable sampleRequestSubscription;
        private readonly ISessionSummaryService sessionSummaryService;
        private readonly ISignalBus signalBus;
        private DelimiterFormat delimiterFormat;
        private IDisplayParameterService displayParameterService;
        private bool disposed;
        private double fontSize;

        private bool isCompositeDataRequest;
        private bool isScrollingPaused;

        private ObservableCollection<string> textLines;

        public HelloConsoleDisplayViewModel(
            ISignalBus signalBus,
            IDispatcherSchedulerProvider dispatcherSchedulerProvider,
            IDataRequestSignalFactory dataRequestSignalFactory,
            ISessionSummaryService sessionSummaryService)
        {
            this.dataRequestSignalFactory = dataRequestSignalFactory;
            this.sessionSummaryService = sessionSummaryService;
            this.signalBus = signalBus;
            this.dispatcherSchedulerProvider = dispatcherSchedulerProvider;

            this.sampleRequestSubscription =
                this.signalBus.Subscribe<SampleResultSignal>(this.HandleSampleResultSignal, r => r.SourceId == this.ScopeIdentity.Guid);
            this.sampleCompositeRequestSubscription =
                this.signalBus.Subscribe<CompositeSampleResultSignal>(this.HandleCompositeSampleResultSignal, r => r.SourceId == this.ScopeIdentity.Guid);

            this.dataRequestSubscription = this.signalBus.Subscribe<DataResultSignal>(this.HandleDataResultSignal, r => r.SourceId == this.ScopeIdentity.Guid);

            this.TextLines = new ObservableCollection<string>();
            this.ClearCommand = new DelegateCommand(this.ExecuteClearCommand);

            this.WriteLine("Add a session and a parameter, then change data range by clicking on the timeline.");
            this.WriteLine("Note: Keyboard shortcuts: CTRL + L, P, or D.");
        }

        [Browsable(false)]
        public ICommand ClearCommand { get; }

        [Browsable(false)]
        public bool IsCompositeDataRequest
        {
            get { return this.isCompositeDataRequest; }
            set { this.SetProperty(ref this.isCompositeDataRequest, value); }
        }

        [Browsable(false)]
        public bool IsScrollingPaused
        {
            get { return this.isScrollingPaused; }
            set { this.SetProperty(ref this.isScrollingPaused, value); }
        }

        [Browsable(false)]
        public ObservableCollection<string> TextLines
        {
            get { return this.textLines; }
            set { this.SetProperty(ref this.textLines, value); }
        }

        //Indicates whether display's page is selected hence can process its logic or not.
        //E.g. Re-rendering or data request can be issued or paused accordingly.
        public override void OnActiveDisplayPageChanged(bool isActive)
        {
            this.WriteLine($"Active Display Page Changed: {isActive}");
        }

        public override void OnCanRenderDisplayChanged(bool canRender)
        {
            this.WriteLine($"Display Render Changed: {canRender}");
        }

        //Called when active compare set changes e.g. User selects Set 1, Set 2, or No Association
        public override void OnCompositeSessionContainerChanged()
        {
        }

        //Session loading completes i.e. loading happens after a session is added
        public override void OnCompositeSessionLoaded(CompositeSessionEventArgs args)
        {
            this.WriteLine($"Composite Session Loaded: {args.CompositeSessionContainer.Name}, {args.CompositeSession.Identifier}");

            this.PrintSessionSummary(args.CompositeSession.Sessions.FirstOrDefault());
        }

        //Session unloading completes
        public override void OnCompositeSessionUnLoaded(CompositeSessionUnloadedEventArgs args)
        {
            var compositeSession =
                this.ActiveCompositeSessionContainer.CompositeSessions.FirstOrDefault(a => a.InstanceIdentifier == args.CompositeSessionInstanceIdentifier);
            this.WriteLine($"Composite Session UnLoaded: {this.ActiveCompositeSessionContainer.Name}, {compositeSession?.Identifier}");
        }

        public override void OnCursorDataPointChanged(ICompositeSession compositeSession)
        {
            this.WriteLine($"Cursor point: {compositeSession.Identifier}, {compositeSession.CursorPoint}");
        }

        public override void OnParameterContainerAdded(IDisplayParameterContainer displayParameterContainer)
        {
            this.WriteLine($"Display Parameter Container Added: {displayParameterContainer.Identifier}, {displayParameterContainer.Parameters.Count}");
        }

        public override void OnParameterAdded(ParameterEventArgs args)
        {
            this.WriteLine($"Parameter Added: {args.Parameter.Identifier}, {args.Parameter.FormattedName}");

            if (!args.Parameter.IsAvailable || !this.CanRetrieveData)
            {
                return;
            }

            var compositeSession = this.ActiveCompositeSessionContainer.CompositeSessions.FirstOrDefault();

            var signal = this.dataRequestSignalFactory.CreateDataRequestSignal(
                this.ScopeIdentity.Guid,
                args.Parameter,
                compositeSession.TimebaseRange,
                50,
                SampleMode.Mean);

            this.signalBus.Send(signal);
        }

        public override void OnParameterRemoved(ParameterEventArgs args)
        {
            this.WriteLine($"Parameter Removed: {args.Parameter.Identifier}, {args.Parameter.FormattedName}");
        }

        //Timeline range changed i.e. user click different 
        public override void OnSessionTimeRangeChanged(ICompositeSession compositeSession)
        {
            this.WriteLine(
                $"{compositeSession.Identifier}, TimebaseRange: ({compositeSession.TimebaseRange.Start},{compositeSession.TimebaseRange.End}), Laps.Count: {compositeSession.Laps.Count}");

            if (!this.displayParameterService.ParameterContainers.Any())
            {
                return;
            }

            if (this.IsCompositeDataRequest)
            {
                //Composite Sample Request
                var signal = this.dataRequestSignalFactory.CreateCompositeSampleRequestSignal(
                    this.ScopeIdentity.Guid,
                    this.ActiveCompositeSessionContainer.Key,
                    this.ServiceContext.DisplayParameterService.ParameterContainers.FirstOrDefault(),
                    compositeSession.TimebaseRange,
                    SampleDirection.Next);

                this.signalBus.Send(signal);
            }
            else
            {
                //Sample Request
                var signal = this.dataRequestSignalFactory.CreateSampleRequestSignal(
                    this.ScopeIdentity.Guid,
                    compositeSession.Key,
                    this.displayParameterService.PrimaryParameters.FirstOrDefault(),
                    compositeSession.CursorPoint - 1, 2, SampleDirection.Previous);//leading edge

                this.signalBus.Send(signal);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (this.disposed == false)
            {
                this.disposed = true;

                if (disposing)
                {
                    //unsubscribe from signal bus
                    this.dataRequestSubscription.Dispose();
                    this.sampleCompositeRequestSubscription.Dispose();
                    this.sampleRequestSubscription.Dispose();
                }
            }

            base.Dispose(disposing);
        }

        protected override void OnInitialised()
        {
            this.displayParameterService = this.ServiceContext.DisplayParameterService;
            
            this.displayParameterService.AddParameterContainer("vCar:Chassis");

            this.WriteLine($"OnInitialised...");
        }

        private void ExecuteClearCommand()
        {
            this.dispatcherSchedulerProvider.BackgroundDispatcher.Schedule(() => this.TextLines.Clear());
        }

        private IList<string> FormatText(ParameterValuesBase result)
        {
            //Cpu Bound
            var list = new List<string>();
            var delimiter = this.DelimiterFormat.GetString();

            for (int i = 0; i < result.Timestamp.Length; i++)
            {
                list.Add($"{result.Timestamp[i]} {delimiter} {result.Data[i]}");
            }

            return list;
        }

        private void HandleCompositeSampleResultSignal(CompositeSampleResultSignal signal)
        {
            var results = signal.Data.Results;

            foreach (var keyValuePair in results)
            {
                var result = keyValuePair.Value;
                var parameterValues = result.ParameterValues;
                parameterValues.Lock();

                //Process Data
                this.WriteLine($"CompositeSessionKey: {keyValuePair.Key}, {parameterValues.Timestamp.Length}");

                parameterValues.Unlock();
            }
        }

        private async void HandleDataResultSignal(DataResultSignal signal)
        {
            try
            {
                signal.Data.ParameterValues.Lock();

                var formattedText = await Task.Run(() => this.FormatText(signal.Data.ParameterValues)).ConfigureAwait(false);

                this.WriteLines(formattedText);
            }
            catch (Exception)
            {
                // Handle exception
            }
            finally
            {
                signal.Data.ParameterValues.Unlock();
            }
        }

        private void HandleSampleResultSignal(SampleResultSignal signal)
        {
            IRequest request = signal.Data.Request;
            IResult result = signal.Data;

            ParameterValues parameterValues = result.ParameterValues;

            parameterValues.Lock();

            //Some Data Processing...
            this.WriteLine($"Parameter: {request.Parameter.Identifier}, {parameterValues.Timestamp.Length}");
            for (int i = 0; i < parameterValues.Timestamp.Length; i++)
            {
                Debug.WriteLine($"{parameterValues.Timestamp[i]}: {parameterValues.Data[i]}");
            }

            parameterValues.Unlock();
        }

        private void PrintSessionSummary(ISession session)
        {
            var sessionSummary = this.sessionSummaryService.GetSessionSummary(session.Key);
            if (sessionSummary == null)
            {
                return;
            }

            this.WriteLine($"Key: {sessionSummary.Key}");
            this.WriteLine($"Identifier: {sessionSummary.Identifier}");
            this.WriteLine($"FileSessionPath: {sessionSummary.FileSessionPath}");
            this.WriteLine($"LapCount: {sessionSummary.LapCount}");
            this.WriteLine($"#Attributes: {sessionSummary.Items.Count}");
        }

        private void WriteLine(string text, bool append = true)
        {
            this.WriteLines(
                new List<string>
                {
                    text
                },
                append);
        }

        private void WriteLines(IList<string> lines, bool append = false)
        {
            if (this.IsScrollingPaused)
            {
                return;
            }

            this.dispatcherSchedulerProvider.BackgroundDispatcher.Schedule(
                () =>
                {
                    if (!append)
                    {
                        this.TextLines.Clear();
                    }

                    foreach (string line in lines)
                    {
                        this.TextLines.Add(line);
                    }
                });
        }

        #region Display Properties

        [Category("Text")]
        [DisplayName("Font Size")]
        [Description("The size of the font.")]
        [Display(Order = 0)]
        public double FontSize
        {
            get
            {
                this.fontSize = this.ReadProperty(12);
                return this.fontSize;
            }
            set
            {
                if (this.SetProperty(ref this.fontSize, value))
                {
                    this.SaveProperty(value);
                }
            }
        }

        [Category("Font/Text")]
        [DisplayName("Delimiter Format")]
        [Description("The delimiter format.")]
        [Display(Order = 1)]
        public DelimiterFormat DelimiterFormat
        {
            get { return this.delimiterFormat; }
            set { this.SetProperty(ref this.delimiterFormat, value); }
        }

        #endregion
    }
}