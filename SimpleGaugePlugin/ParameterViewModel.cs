// <copyright file="ParameterViewModel.cs" company="Motion Applied Ltd.">
// Copyright (c) Motion Applied Ltd.</copyright>

using DisplayPluginLibrary;

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

        protected override bool OnValueChanged(double? oldValue, double newValue)
        {
            this.OnUpdate();
            if (newValue < this.DisplayMinimum || newValue > this.DisplayMaximum)
            {
                return false;
            }

            this.Description = $"{this.Name}\r{this.Value}";
            return true;
        }
    }
}