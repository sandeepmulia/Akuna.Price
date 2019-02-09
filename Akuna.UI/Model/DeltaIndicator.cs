using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Akuna.UI.Model
{
    class DeltaIndicator : INotifyPropertyChanged
    {
        public DeltaIndicator(uint instrumentId, double currentPrice, double previousPrice)
        {
            this.instrumentId = instrumentId;
            this.CurrentPrice = currentPrice;
            this.PreviousPrice = previousPrice;
        }

        private uint _instrumentId;
        public uint instrumentId
        {
            get { return _instrumentId; }
            set
            {
                _instrumentId = value;
                OnPropertyChanged("instrumentId");
            }
        }

        private double _currentPrice = 0.00;
        public double CurrentPrice
        {
            get { return _currentPrice; }
            set
            {
                _currentPrice = value;
                OnPropertyChanged("CurrentPrice");
                OnPropertyChanged("ChangeInPrice");
            }
        }

        private double _previousPrice = 0.00;
        public double PreviousPrice
        {
            get { return _previousPrice; }
            set
            {
                _previousPrice = value;
                OnPropertyChanged("PreviousPrice");
                OnPropertyChanged("ChangeInPrice");
            }
        }

        public double ChangeInPrice
        {
            get
            {
                return CurrentPrice - PreviousPrice;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;

            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}