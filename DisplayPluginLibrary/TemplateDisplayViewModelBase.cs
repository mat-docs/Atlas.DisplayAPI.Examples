using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using MAT.Atlas.Api.Core.Diagnostics;
using MAT.Atlas.Api.Core.Signals;
using MAT.Atlas.Client.Platform.Data;
using MAT.Atlas.Client.Platform.Parameters;
using MAT.Atlas.Client.Platform.Sessions;
using MAT.Atlas.Client.Presentation.Displays;
using MAT.Atlas.Client.Presentation.Services;

namespace DisplayPluginLibrary
{
    public abstract class TemplateDisplayViewModelBase : DisplayPluginViewModel
    {
        private readonly OperationTracker<ICompositeSession> cursorDataRequestTracker;
        private readonly List<IDisposable> disposables = new List<IDisposable>();
        private readonly OperationTracker<ICompositeSession> timelineDataRequestTracker;
        private bool isDisplayVisible;

        protected TemplateDisplayViewModelBase(
            ISignalBus signalBus,
            IDataRequestSignalFactory dataRequestSignalFactory,
            ILogger logger,
            TimeSpan? throttleInterval = null)
        {
            this.SignalBus = signalBus;
            this.DataRequestSignalFactory = dataRequestSignalFactory;
            this.Logger = logger;
            this.ThrottleInterval = throttleInterval ?? TimeSpan.FromMilliseconds(200) /*5Hz*/;

            this.SynchronizationContext = SynchronizationContext.Current;

            this.cursorDataRequestTracker = new OperationTracker<ICompositeSession>(
                this.ThrottleInterval,
                this.MakeCursorDataRequests);

            this.timelineDataRequestTracker = new OperationTracker<ICompositeSession>(
                this.ThrottleInterval,
                this.MakeTimelineDataRequests);
        }

        protected IDataRequestSignalFactory DataRequestSignalFactory { get; }

        protected IList<IDisposable> Disposables => this.disposables;

        protected IDisplayParameterService DisplayParameterService { get; private set; }

        [Browsable(false)]
        public bool IsDisplayVisible
        {
            get => this.isDisplayVisible;
            set => this.SetProperty(ref this.isDisplayVisible, value);
        }

        protected ILogger Logger { get; }

        protected ISignalBus SignalBus { get; }

        protected SynchronizationContext SynchronizationContext { get; }

        protected TimeSpan ThrottleInterval { get; }

        public override void OnCanRenderDisplayChanged(bool canRender)
        {
            this.MakeDataRequests(true, true);
        }

        protected override void OnInitialised()
        {
            // Access to Display specific services
            this.DisplayParameterService = this.ServiceContext.DisplayParameterService;

            this.MakeDataRequests(true, true);
        }

        public override void OnParameterAdded(ParameterEventArgs args)
        {
            this.MakeDataRequests(true, true);
        }

        public override void OnParameterRemoved(ParameterEventArgs args)
        {
            this.MakeDataRequests(true, true);
        }

        public override void OnCompositeSessionLoaded(CompositeSessionEventArgs args)
        {
            this.MakeDataRequests(true, true);
        }

        public override void OnCompositeSessionUnLoaded(CompositeSessionUnloadedEventArgs args)
        {
            this.MakeDataRequests(true, true);
        }

        public override void OnCompositeSessionContainerChanged()
        {
            if (this.ActiveCompositeSessionContainer == null)
            {
                return;
            }

            this.MakeDataRequests(true, true);
        }

        public override void OnCursorDataPointChanged(ICompositeSession compositeSession)
        {
            this.MakeDataRequests(compositeSession, true, false);
        }

        public override void OnSessionTimeRangeChanged(ICompositeSession compositeSession)
        {
            this.MakeDataRequests(compositeSession, false, true);
        }

        protected async Task ExecuteOnUiAsync(Action callback, CancellationToken? token = null)
        {
            if (token?.IsCancellationRequested ?? false)
            {
                return;
            }

            if (this.SynchronizationContext == null ||
                this.SynchronizationContext == SynchronizationContext.Current)
            {
                callback();
                return;
            }

            var tcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);

            var cancellationTokenRegistration = token?.Register(
                () =>
                {
                    tcs.TrySetCanceled();
                });

            this.SynchronizationContext.Post(
                delegate
                {
                    try
                    {
                        callback();
                        tcs.TrySetResult(null);
                    }
                    catch (Exception e)
                    {
                        tcs.TrySetException(e);
                    }
                },
                null);

            try
            {
                await tcs.Task;
            }
            finally
            {
                cancellationTokenRegistration?.Dispose();
            }
        }

        protected void MakeDataRequests(bool cursorChanged, bool timelineChanged)
        {
            this.IsDisplayVisible = this.CanRetrieveData;

            if (!this.IsDisplayVisible ||
                this.ActiveCompositeSessionContainer == null ||
                !this.ActiveCompositeSessionContainer.IsPrimaryCompositeSessionAvailable)
            {
                return;
            }

            var primarySession = this.ActiveCompositeSessionContainer.CompositeSessions.FirstOrDefault(c => c.IsPrimary);
            if (primarySession == null)
            {
                return;
            }

            this.MakeDataRequests(primarySession, cursorChanged, timelineChanged);
        }

        protected override void OnDisposeManagedResources()
        {
            base.OnDisposeManagedResources();
            this.disposables.ForEach(d => d.Dispose());
        }

        protected virtual Task OnMakeCursorDataRequestsAsync(ICompositeSession compositeSession)
        {
            return Task.CompletedTask;
        }

        protected virtual Task OnMakeTimelineDataRequestsAsync(ICompositeSession compositeSession)
        {
            return Task.CompletedTask;
        }

        private void MakeDataRequests(ICompositeSession compositeSession, bool cursorChanged, bool timelineChanged)
        {
            this.IsDisplayVisible = this.CanRetrieveData;

            if (!this.IsDisplayVisible)
            {
                return;
            }

            if (cursorChanged)
            {
                this.cursorDataRequestTracker.Add(compositeSession);
            }

            if (timelineChanged)
            {
                this.timelineDataRequestTracker.Add(compositeSession);
            }
        }

        private async void MakeCursorDataRequests(ICompositeSession compositeSession)
        {
            try
            {
                await this.OnMakeCursorDataRequestsAsync(compositeSession);
            }
            catch (Exception ex)
            {
                this.Logger.Debug(nameof(MakeCursorDataRequests), ex);
            }
            finally
            {
                this.cursorDataRequestTracker.Complete();
            }
        }

        private async void MakeTimelineDataRequests(ICompositeSession compositeSession)
        {
            try
            {
                await this.OnMakeTimelineDataRequestsAsync(compositeSession);
            }
            catch (Exception ex)
            {
                this.Logger.Debug(nameof(MakeTimelineDataRequests), ex);
            }
            finally
            {
                this.timelineDataRequestTracker.Complete();
            }
        }
    }
}