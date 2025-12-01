// <copyright file="SampleDisplayViewModel.cs" company="Motion Applied Ltd.">
// Copyright (c) Motion Applied Ltd.</copyright>

using DisplayPluginLibrary;

using MAT.Atlas.Api.Core.Diagnostics;
using MAT.Atlas.Api.Core.Signals;
using MAT.Atlas.Client.Platform.Data;
using MAT.Atlas.Client.Presentation.Plugins;

namespace SimpleGaugePlugin
{
    [DisplayPluginSettings(ParametersMaxCount = 100)]
    public sealed class SampleDisplayViewModel : ParameterSampleDisplayViewModelBase<ParameterViewModel>
    {
        public SampleDisplayViewModel(
            ISignalBus signalBus,
            IDataRequestSignalFactory dataRequestSignalFactory,
            ILogger logger) :
            base(signalBus, dataRequestSignalFactory, logger)
        {
        }

        protected override ParameterViewModel OnCreateParameterViewModel() => new ParameterViewModel();
    }
}