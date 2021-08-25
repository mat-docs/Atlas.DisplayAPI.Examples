﻿using MAT.Atlas.Api.Core.Presentation;

namespace NumericCompareDisplayPlugin
{
    public sealed class CellViewModel : BindableBase
    {
        private object obj;

        public CellViewModel(object obj)
        {
            this.obj = obj;
        }

        public object Value
        {
            get => this.obj;
            set => SetProperty(ref this.obj, value);
        }
    }
}