﻿using DisplayPluginLibrary;

namespace SimpleGaugePlugin
{
    public sealed class ParameterViewModel : ParameterSampleViewModelBase
    {
        private string description;
        private double displayMaximum;
        private double displayMinimum;

        public string Description
        {
            get => this.description;
            set => this.SetProperty(ref description, value);
        }

        public double DisplayMaximum
        {
            get => this.displayMaximum;
            set => this.SetProperty(ref displayMaximum, value);
        }

        public double DisplayMinimum
        {
            get => this.displayMinimum;
            set => this.SetProperty(ref displayMinimum, value);
        }

        protected override void OnUpdate()
        {
            this.DisplayMinimum = this.DisplayParameter.SessionParameter.Minimum;
            this.DisplayMaximum = this.DisplayParameter.SessionParameter.Maximum;
        }

        protected override void OnValueChanged(double? oldValue)
        {
            this.Description = $"{this.Name}\r{this.Value}";
        }
    }
}