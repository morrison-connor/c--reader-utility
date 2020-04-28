using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RFID.Utility.VM
{
    public class RegulationVM : INotifyPropertyChanged
    {
        public ObservableCollection<BaudRate> BaudRate { get; set; }

        public RegulationVM()
        {
            BaudRate = new ObservableCollection<BaudRate>
            {
                new BaudRate { Content="4800", Tag="0" },
                new BaudRate { Content="9600", Tag="1"},
                new BaudRate { Content="14400", Tag="2"},
                new BaudRate { Content="19200",  Tag="3"},
                new BaudRate { Content="38400",  Tag="4"},
                new BaudRate { Content="57600",  Tag="5"},
                new BaudRate { Content="115200",  Tag="6"},
                new BaudRate { Content="230400",  Tag="7"}
            };
        }

        private BaudRate _ComboBoxBaudRateSelectedBaudRate;
        public BaudRate ComboBoxBaudRateSelectedBaudRate
        {
            get { return _ComboBoxBaudRateSelectedBaudRate; }
            set
            {
                if (_ComboBoxBaudRateSelectedBaudRate != value)
                {
                    _ComboBoxBaudRateSelectedBaudRate = value;
                    OnPropertyChanged("ComboBoxBaudRateSelectedBaudRate");
                }
            }
        }


        

        private Int32 _ComboBoxAreaSelectedIndex = 0;
        public Int32 ComboBoxAreaSelectedIndex
        {
            get { return _ComboBoxAreaSelectedIndex; }
            set
            {
                if (_ComboBoxAreaSelectedIndex != value)
                {
                    _ComboBoxAreaSelectedIndex = value;
                    OnPropertyChanged("ComboBoxAreaSelectedIndex");
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(String property)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }
    }
}
