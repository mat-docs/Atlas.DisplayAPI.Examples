using System;
using System.Collections.Generic;

using MAT.Atlas.Api.Core.Presentation;
using MAT.Atlas.Api.Core.Signals;
using MAT.Atlas.Client.Platform.Data.Signals;
using MAT.Atlas.Client.Platform.Parameters;

namespace DisplayPluginLibrary
{
    /// <summary>
    ///     Base class for Parameter View Model.
    /// </summary>
    public abstract class ParameterSampleViewModelBase : BindableBase
    {
        private bool isValidValue;
        private string name;
        private double value;

        /// <summary>
        ///     Display parameter properties (Note: only valid after OnUpdate has been called).
        /// </summary>
        public IDisplayParameter DisplayParameter { get; private set; }

        internal OperationTracker<SampleRequestSignal> OperationTracker { get; private set; }

        /// <summary>
        ///     Has a value been assigned.
        /// </summary>
        public bool IsValidValue
        {
            get => this.isValidValue;
            set => this.SetProperty(ref this.isValidValue, value);
        }

        /// <summary>
        ///     Display parameter name.
        /// </summary>
        public string Name
        {
            get => this.name;
            set => this.SetProperty(ref this.name, value);
        }

        /// <summary>
        ///     Current value.
        /// </summary>
        public double Value
        {
            get => this.value;
            set
            {
                var oldValue = default(double?);

                if (this.IsValidValue)
                {
                    if (EqualityComparer<double>.Default.Equals(this.value, value))
                    {
                        return;
                    }

                    oldValue = this.value;
                }

                this.IsValidValue = this.OnValueChanged(oldValue, value);
                if (this.IsValidValue)
                {
                    this.value = value;
                    this.OnPropertyChanged();
                }
            }
        }

        /// <summary>
        ///     Notification method called when new parameter value has been assigned.
        /// </summary>
        /// <param name="oldValue">Optional previous value.</param>
        /// <param name="newValue">New value.</param>
        /// <returns>Whether value is valid.</returns>
        protected virtual bool OnValueChanged(double? oldValue, double newValue)
        {
            return true;
        }

        /// <summary>
        ///     Notification method called after parameter have been updated.
        /// </summary>
        protected virtual void OnUpdate()
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

            this.Name = displayParameter?.Name ?? string.Empty;

            if (this.DisplayParameter != null)
            {
                this.OnUpdate();
            }
        }
    }
}