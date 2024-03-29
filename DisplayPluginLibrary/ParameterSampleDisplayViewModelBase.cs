﻿using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

using MAT.Atlas.Api.Core.Diagnostics;
using MAT.Atlas.Api.Core.Signals;
using MAT.Atlas.Client.Platform.Data;
using MAT.Atlas.Client.Platform.Data.Signals;
using MAT.Atlas.Client.Platform.Sessions;
using MAT.OCS.Core;

namespace DisplayPluginLibrary
{
    /// <summary>
    ///     Provides automatic retrieval of display parameter sample values.
    /// </summary>
    /// <typeparam name="TParameterViewModel">Parameter View Model.</typeparam>
    public abstract class ParameterSampleDisplayViewModelBase<TParameterViewModel> : TemplateDisplayViewModelBase
        where TParameterViewModel : ParameterSampleViewModelBase
    {
        /// <inheritdoc />
        protected ParameterSampleDisplayViewModelBase(
            ISignalBus signalBus,
            IDataRequestSignalFactory dataRequestSignalFactory,
            ILogger logger,
            TimeSpan? throttleInterval = null) :
             base(signalBus, dataRequestSignalFactory, logger, throttleInterval)
        {
            this.Disposables.Add(this.SignalBus.Subscribe<SampleResultSignal>(this.HandleCursorDataRequests, s => s.SourceId == this.ScopeIdentity.Guid));
        }

        /// <summary>
        ///     Parameter view models.
        /// </summary>
        [Browsable(false)]
        public ObservableCollection<TParameterViewModel> Parameters { get; } = new ObservableCollection<TParameterViewModel>();

        /// <summary>
        ///     Create a parameter view model.
        /// </summary>
        /// <returns>Parameter view model.</returns>
        protected TParameterViewModel CreateParameterViewModel()
        {
            var parameterViewModel = this.OnCreateParameterViewModel();
            parameterViewModel.Init(this.SignalBus, this.ThrottleInterval);
            return parameterViewModel;
        }

        /// <summary>
        ///     Derived class supplied parameter view model Factory method.
        /// </summary>
        /// <returns>Parameter view model.</returns>
        protected abstract TParameterViewModel OnCreateParameterViewModel();

        /// <inheritdoc />
        protected sealed override async Task OnMakeCursorDataRequestsAsync(ICompositeSession compositeSession)
        {
            try
            {
                await this.ExecuteOnUiAsync(() => this.UpdateParameters(compositeSession.Key, compositeSession.CursorPoint));
            }
            catch (Exception ex)
            {
                this.Logger.Trace("Error updating parameters", ex);
            }
        }

        /// <summary>
        ///     Notification method called once parameters have been updated.
        /// </summary>
        protected virtual void OnUpdateParameters()
        {
        }

        private async void HandleCursorDataRequests(SampleResultSignal signal)
        {
            try
            {
                await this.ExecuteOnUiAsync(() => { UpdateParameterValue(signal.Data); });
            }
            catch (Exception ex)
            {
                this.Logger.Trace("Error updating parameter value", ex);
            }
        }

        private void UpdateParameters(CompositeSessionKey compositeSessionKey, long cursorPoint)
        {
            var existingParameters = this.Parameters.ToDictionary(p => p.DisplayParameter, p => p);

            var i = 0;
            foreach (var primaryDisplayParameter in this.DisplayParameterService.PrimaryParameters.Take(this.Parameters.Count))
            {
                if (!ReferenceEquals(this.Parameters[i].DisplayParameter, primaryDisplayParameter))
                {
                    if (existingParameters.TryGetValue(primaryDisplayParameter, out var existingParameter))
                    {
                        this.Parameters[i] = existingParameter;
                    }
                    else
                    {
                        this.Parameters[i] = this.CreateParameterViewModel();
                    }
                }

                ++i;
            }

            while (this.Parameters.Count > this.DisplayParameterService.PrimaryParameters.Count)
            {
                this.Parameters.Last().Update(null);
                this.Parameters.RemoveAt(this.Parameters.Count - 1);
            }

            while (this.Parameters.Count < this.DisplayParameterService.PrimaryParameters.Count)
            {
                this.Parameters.Add(this.CreateParameterViewModel());
            }

            i = 0;
            foreach (var primaryDisplayParameter in this.DisplayParameterService.PrimaryParameters)
            {
                var parameter = this.Parameters[i++];
                parameter.Update(primaryDisplayParameter);

                var signal = this.DataRequestSignalFactory.CreateSampleRequestSignal(
                    this.ScopeIdentity.Guid,
                    parameter.DisplayParameter.InstanceIdentifier,
                    compositeSessionKey,
                    parameter.DisplayParameter,
                    cursorPoint + 1,
                    1,
                    SampleDirection.Previous);

                parameter.OperationTracker.Add(signal);
            }

            this.OnUpdateParameters();
        }

        private void UpdateParameterValue(IResult data)
        {
            ParameterSampleViewModelBase parameterSampleViewModel = null;
            try
            {
                parameterSampleViewModel = this.Parameters.FirstOrDefault(p => p.DisplayParameter.InstanceIdentifier == data.Request.RequestId);
                if (parameterSampleViewModel == null)
                {
                    return;
                }

                if (data.ParameterValues.SampleCount != 1 ||
                    !data.ParameterValues.DataStatus[0].HasFlag(DataStatusType.Sample))
                {
                    return;
                }

                var value = data.ParameterValues.Data[0];
                if (double.IsNaN(value))
                {
                    return;
                }

                parameterSampleViewModel.Value = value;
            }
            finally
            {
                parameterSampleViewModel?.OperationTracker.Complete();
            }
        }
    }
}