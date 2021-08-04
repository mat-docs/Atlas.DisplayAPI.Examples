using System;
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
    public abstract class ParameterSampleDisplayViewModelBase<TParameterViewModel> : TemplateDisplayViewModelBase
        where TParameterViewModel : ParameterSampleViewModelBase
    {
        protected ParameterSampleDisplayViewModelBase(
            ISignalBus signalBus,
            IDataRequestSignalFactory dataRequestSignalFactory,
            ILogger logger,
            TimeSpan? throttleInterval = null) :
             base(signalBus, dataRequestSignalFactory, logger, throttleInterval)
        {
            this.Disposables.Add(this.SignalBus.Subscribe<SampleResultSignal>(this.HandleCursorDataRequests, s => s.SourceId == this.ScopeIdentity.Guid));
        }

        [Browsable(false)]
        public ObservableCollection<TParameterViewModel> Parameters { get; } = new ObservableCollection<TParameterViewModel>();

        protected TParameterViewModel CreateParameterViewModel()
        {
            var parameterViewModel = this.OnCreateParameterViewModel();
            parameterViewModel.Init(this.SignalBus, this.ThrottleInterval);
            return parameterViewModel;
        }

        protected abstract TParameterViewModel OnCreateParameterViewModel();

        protected override Task OnMakeCursorDataRequestsAsync(ICompositeSession compositeSession)
        {
            return this.ExecuteOnUiAsync(() => this.SynchParameters(compositeSession.Key, compositeSession.CursorPoint));
        }

        private async void HandleCursorDataRequests(SampleResultSignal signal)
        {
            await this.ExecuteOnUiAsync(() =>
            {
                ParameterSampleViewModelBase parameterSampleViewModel = null;
                try
                {
                    parameterSampleViewModel = this.Parameters.FirstOrDefault(p => p.DisplayParameter.InstanceIdentifier == signal.Data.Request.RequestId);
                    if (parameterSampleViewModel == null)
                    {
                        return;
                    }

                    if (signal.Data.ParameterValues.SampleCount != 1)
                    {
                        return;
                    }

                    parameterSampleViewModel.Value = signal.Data.ParameterValues.Data[0];
                }
                finally
                {
                    parameterSampleViewModel?.OperationTracker.Complete();
                }
            });
        }

        private void SynchParameters(CompositeSessionKey compositeSessionKey, long cursorPoint)
        {
            var exitingParameters = this.Parameters.ToDictionary(p => p.DisplayParameter, p => p);

            var i = 0;
            foreach (var primaryDisplayParameter in this.DisplayParameterService.PrimaryParameters.Take(this.Parameters.Count))
            {
                if (!ReferenceEquals(this.Parameters[i].DisplayParameter, primaryDisplayParameter))
                {
                    if (exitingParameters.TryGetValue(primaryDisplayParameter, out var existingParameter))
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
        }
    }
}