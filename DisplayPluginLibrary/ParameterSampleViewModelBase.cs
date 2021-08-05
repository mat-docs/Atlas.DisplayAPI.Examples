using System;

using MAT.Atlas.Api.Core.Presentation;
using MAT.Atlas.Api.Core.Signals;
using MAT.Atlas.Client.Platform.Data.Signals;
using MAT.Atlas.Client.Platform.Parameters;

namespace DisplayPluginLibrary
{
    public abstract class ParameterSampleViewModelBase : BindableBase
    {
        private bool isValidValue;
        private string name;
        private double value;

        public IDisplayParameter DisplayParameter { get; private set; }

        internal OperationTracker<SampleRequestSignal> OperationTracker { get; private set; }

        public bool IsValidValue
        {
            get => this.isValidValue;
            set => this.SetProperty(ref this.isValidValue, value);
        }

        public string Name
        {
            get => this.name;
            set => this.SetProperty(ref this.name, value);
        }

        public double Value
        {
            get => this.value;
            set
            {
                if (!isValidValue)
                {
                    this.isValidValue = true;
                    this.value = value;
                    this.OnPropertyChanged();
                    this.OnValueChanged(null);
                }
                else
                {
                    var oldValue = this.value;
                    if (this.SetProperty(ref this.value, value))
                    {
                        this.OnValueChanged(oldValue);
                    }
                }
            }
        }

        protected virtual void OnValueChanged(double? oldValue)
        {
        }

        internal void Init(ISignalBus signalBus, TimeSpan throttleInterval)
        {
            this.OperationTracker = new OperationTracker<SampleRequestSignal>(throttleInterval, signalBus.Send);
        }

        internal void Update(IDisplayParameter displayParameter)
        {
            if (ReferenceEquals(this.DisplayParameter, displayParameter))
            {
                return;
            }

            if (this.DisplayParameter != null)
            {
                this.IsValidValue = false;
                this.OperationTracker.Abort();
            }

            this.DisplayParameter = displayParameter;
            this.Name = displayParameter?.Name;
        }
    }
}