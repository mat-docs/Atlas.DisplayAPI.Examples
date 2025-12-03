// <copyright file="TemplateDisplayViewModelBase.cs" company="Motion Applied Ltd.">
// Copyright (c) Motion Applied Ltd.</copyright>

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
    /// <summary>
    ///     Provides a template that helps create displays that track cursor and/or timebase changes properly.
    /// </summary>
    public abstract class TemplateDisplayViewModelBase : DisplayPluginViewModel
    {
        private readonly OperationTracker<ICompositeSession> cursorDataRequestTracker;
        private readonly List<IDisposable> disposables = new List<IDisposable>();
        private readonly OperationTracker<ICompositeSession> timebaseDataRequestTracker;
        private bool isDisplayVisible;

        /// <summary>
        ///     Constructor.
        /// </summary>
        /// <param name="signalBus">Signal bus service.</param>
        /// <param name="dataRequestSignalFactory">Data request signal factory.</param>
        /// <param name="logger">Logger service.</param>
        /// <param name="throttleInterval">Interval to throttle data requests (default 5Hz).</param>
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

            this.timebaseDataRequestTracker = new OperationTracker<ICompositeSession>(
                this.ThrottleInterval,
                this.MakeTimebaseDataRequests);
        }

        /// <summary>
        ///     Data request signal factory.
        /// </summary>
        protected IDataRequestSignalFactory DataRequestSignalFactory { get; }

        /// <summary>
        ///     Items that are automatically disposed.
        /// </summary>
        protected IList<IDisposable> Disposables => this.disposables;

        /// <summary>
        ///     Display parameter service (only available once OnInitialised has been called)
        /// </summary>
        protected IDisplayParameterService DisplayParameterService { get; private set; }

        /// <summary>
        ///     Whether display is currently visible.
        /// </summary>
        [Browsable(false)]
        public bool IsDisplayVisible
        {
            get => this.isDisplayVisible;
            set => this.SetProperty(ref this.isDisplayVisible, value);
        }

        /// <summary>
        ///     Logger service.
        /// </summary>
        protected ILogger Logger { get; }

        /// <summary>
        ///     Signal bus service.
        /// </summary>
        protected ISignalBus SignalBus { get; }

        /// <summary>
        ///     Synchronization context used to send/post work to UI thread.
        /// </summary>
        protected SynchronizationContext SynchronizationContext { get; }

        /// <summary>
        ///     Interval to throttle data requests (default 5Hz).
        /// </summary>
        protected TimeSpan ThrottleInterval { get; }

        /// <inheritdoc />
        public override void OnCanRenderDisplayChanged(bool canRender)
        {
            this.MakeDataRequests(true, true);
        }

        /// <inheritdoc />
        protected override void OnInitialised()
        {
            // Access to Display specific services
            this.DisplayParameterService = this.ServiceContext.DisplayParameterService;

            this.MakeDataRequests(true, true);
        }

        /// <inheritdoc />
        public override void OnParameterAdded(ParameterEventArgs args)
        {
            this.MakeDataRequests(true, true);
        }

        /// <inheritdoc />
        public override void OnParameterRemoved(ParameterEventArgs args)
        {
            this.MakeDataRequests(true, true);
        }

        /// <inheritdoc />
        public override void OnCompositeSessionLoaded(CompositeSessionEventArgs args)
        {
            this.MakeDataRequests(true, true);
        }

        /// <inheritdoc />
        public override void OnCompositeSessionUnLoaded(CompositeSessionUnloadedEventArgs args)
        {
            this.MakeDataRequests(true, true);
        }

        /// <inheritdoc />
        public override void OnCompositeSessionContainerChanged()
        {
            if (this.ActiveCompositeSessionContainer == null)
            {
                return;
            }

            this.MakeDataRequests(true, true);
        }

        /// <inheritdoc />
        public override void OnCursorDataPointChanged(ICompositeSession compositeSession)
        {
            this.MakeDataRequests(compositeSession, true, false);
        }

        /// <inheritdoc />
        public override void OnSessionTimeRangeChanged(ICompositeSession compositeSession)
        {
            this.MakeDataRequests(compositeSession, false, true);
        }

        /// <summary>
        ///     Helper method to execute an action on the UI thread.
        /// </summary>
        /// <param name="callback">Action to execute on the UI thread.</param>
        /// <param name="token">Optional cancellation token.</param>
        /// <returns>Task to await on.</returns>
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

        /// <summary>
        ///     Initiate data requests.
        /// </summary>
        /// <param name="cursorChanged">Initiate a cursor data request.</param>
        /// <param name="timebaseChanged">Initiate a timebase data request.</param>
        protected void MakeDataRequests(bool cursorChanged, bool timebaseChanged)
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

            this.MakeDataRequests(primarySession, cursorChanged, timebaseChanged);
        }

        /// <inheritdoc />
        protected override void OnDisposeManagedResources()
        {
            base.OnDisposeManagedResources();
            this.disposables.ForEach(d => d.Dispose());
        }

        /// <summary>
        ///     Notification method to initiate a data request in response to a cursor change.
        /// </summary>
        /// <param name="compositeSession">Composite session that changed.</param>
        /// <returns>Task to await on.</returns>
        protected virtual Task OnMakeCursorDataRequestsAsync(ICompositeSession compositeSession)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        ///     Notification method to initiate a data request in response to a timebase change.
        /// </summary>
        /// <param name="compositeSession">Composite session that changed.</param>
        /// <returns>Task to await on.</returns>
        protected virtual Task OnMakeTimebaseDataRequestsAsync(ICompositeSession compositeSession)
        {
            return Task.CompletedTask;
        }

        private void MakeDataRequests(ICompositeSession compositeSession, bool cursorChanged, bool timebaseChanged)
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

            if (timebaseChanged)
            {
                this.timebaseDataRequestTracker.Add(compositeSession);
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

        private async void MakeTimebaseDataRequests(ICompositeSession compositeSession)
        {
            try
            {
                await this.OnMakeTimebaseDataRequestsAsync(compositeSession);
            }
            catch (Exception ex)
            {
                this.Logger.Debug(nameof(MakeTimebaseDataRequests), ex);
            }
            finally
            {
                this.timebaseDataRequestTracker.Complete();
            }
        }
    }
}