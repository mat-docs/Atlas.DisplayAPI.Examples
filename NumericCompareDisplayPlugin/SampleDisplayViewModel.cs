// <copyright file="SampleDisplayViewModel.cs" company="Motion Applied Ltd.">
// Copyright (c) Motion Applied Ltd.</copyright>

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

using DisplayPluginLibrary;

using MAT.Atlas.Api.Core.Diagnostics;
using MAT.Atlas.Api.Core.Signals;
using MAT.Atlas.Client.Platform.Data;
using MAT.Atlas.Client.Platform.Data.Signals;
using MAT.Atlas.Client.Platform.Sessions;
using MAT.Atlas.Client.Presentation.Plugins;
using MAT.OCS.Core;

namespace NumericCompareDisplayPlugin
{
    [DisplayPluginSettings(ParametersMaxCount = 100)]
    public sealed class SampleDisplayViewModel : TemplateDisplayViewModelBase
    {
        private readonly object parameterLock = new object();
        private int columnCount;
        private List<CompositeSessionKey> compositeSessionKeys = new List<CompositeSessionKey>();
        private List<Guid> parameterIdentifiers = new List<Guid>();
        private int rowCount;

        public SampleDisplayViewModel(
            ISignalBus signalBus,
            IDataRequestSignalFactory dataRequestSignalFactory,
            ILogger logger) :
            base(signalBus, dataRequestSignalFactory, logger)
        {
            this.Disposables.Add(this.SignalBus.Subscribe<CompositeSampleResultSignal>(this.HandleCompositeSampleResultSignal, r => r.SourceId == this.ScopeIdentity.Guid));
        }

        [Browsable(false)]
        public ObservableCollection<CellViewModel> CellValues { get; } = new ObservableCollection<CellViewModel>();

        [Browsable(false)]
        public int ColumnCount
        {
            get => this.columnCount;
            set => SetProperty(ref this.columnCount, value);
        }

        [Browsable(false)]
        public int RowCount
        {
            get => this.rowCount;
            set => SetProperty(ref this.rowCount, value);
        }

        protected override async Task OnMakeCursorDataRequestsAsync(ICompositeSession compositeSession)
        {
            await this.ExecuteOnUiAsync(this.SyncParameters);

            foreach (var parameterContainer in DisplayParameterService.ParameterContainers)
            {
                var signal = this.DataRequestSignalFactory.CreateCompositeSampleRequestSignal(
                    this.ScopeIdentity.Guid,
                    this.ActiveCompositeSessionContainer.Key,
                    parameterContainer,
                    compositeSession.CursorPoint + 1,
                    1,
                    SampleDirection.Previous);


                this.SignalBus.Send(signal);
            }
        }

        private void HandleCompositeSampleResultSignal(CompositeSampleResultSignal signal)
        {
            lock (parameterLock)
            {
                var request = signal.Data.Request;

                var parameterIndex = this.parameterIdentifiers.IndexOf(request.ParameterContainer.InstanceIdentifier);
                if (parameterIndex < 0)
                {
                    return;
                }

                var result = signal.Data;

                foreach (var kvp in result.Results)
                {
                    var compositeSessionIndex = this.compositeSessionKeys.IndexOf(kvp.Key);
                    if (compositeSessionIndex < 0)
                    {
                        continue;
                    }

                    var parameterValues = kvp.Value.ParameterValues;
                    if (parameterValues.SampleCount == 0)
                    {
                        continue;
                    }

                    parameterValues.Lock();

                    try
                    {
                        var cellIndex = this.GetCellIndex(parameterIndex, compositeSessionIndex);
                        this.CellValues[cellIndex].Value = parameterValues.Data[0];
                    }
                    finally
                    {
                        parameterValues.Unlock();
                    }
                }
            }
        }

        private void SyncParameters()
        {
            lock (this.parameterLock)
            {
                var compositeSessions = this.ActiveCompositeSessionContainer?.CompositeSessions?.ToList();
                var parameterContainers = this.DisplayParameterService.ParameterContainers.ToList();
                if ((compositeSessions?.Count ?? 0) == 0 ||
                    parameterContainers.Count == 0)
                {
                    this.compositeSessionKeys.Clear();
                    this.parameterIdentifiers.Clear();

                    this.RowCount = 0;
                    this.ColumnCount = 0;
                    this.CellValues.Clear();
                    return;
                }

                var newCompositeSessionKeys = compositeSessions.Select(cs => cs.Key).ToList();
                var newParameterIdentifiers = new List<Guid>();
                var newCellValues = new List<CellViewModel>();

                foreach (var parameterContainer in parameterContainers)
                {
                    newParameterIdentifiers.Add(parameterContainer.InstanceIdentifier);

                    newCellValues.Add(new CellViewModel(parameterContainer.Name));

                    var oldParameterIndex = this.parameterIdentifiers.IndexOf(parameterContainer.InstanceIdentifier);
                    foreach (var newCompositeSessionKey in newCompositeSessionKeys)
                    {
                        var oldCompositeSessionIndex = this.compositeSessionKeys.IndexOf(newCompositeSessionKey);
                        if (oldParameterIndex < 0 || oldCompositeSessionIndex < 0)
                        {
                            newCellValues.Add(new CellViewModel(string.Empty));
                            continue;
                        }

                        var cellIndex = GetCellIndex(oldParameterIndex, oldCompositeSessionIndex);
                        var oldParameterValue = this.CellValues[cellIndex];
                        newCellValues.Add(oldParameterValue);
                    }
                }

                this.compositeSessionKeys = newCompositeSessionKeys;
                this.parameterIdentifiers = newParameterIdentifiers;

                this.RowCount = this.parameterIdentifiers.Count;
                this.ColumnCount = this.compositeSessionKeys.Count + 1;
                this.CellValues.Clear();
                foreach (var newCellValue in newCellValues)
                {
                    this.CellValues.Add(newCellValue);
                }
            }

            this.MakeDataRequests(true, false);
        }

        private int GetCellIndex(int parameterIndex, int compositeSessionIndex)
        {
            var parameterValueIndex = parameterIndex * (this.compositeSessionKeys.Count + 1) + 1 + compositeSessionIndex;
            return parameterValueIndex;
        }
    }
}