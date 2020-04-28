﻿using System;
using System.ComponentModel;


namespace RFID.Utility.VM
{
    public class B04ListViewItem : INotifyPropertyChanged
    {
        //private const Int32 length_ = 4;
        //public Int32 Length { get { return length_; } }

        private String _TagValue = String.Empty;
        public String TagValue
        {
            get { return _TagValue; }
            set
            {
                if (_TagValue != value)
                {
                    _TagValue = value;
                    RaisePropertyChanged("TagValue");
                }
            }
        }


        private String _A1Count = String.Empty;
        public String A1Count
        {
            get { return _A1Count; }
            set
            {
                if (_A1Count != value)
                {
                    _A1Count = value;
                    RaisePropertyChanged("A1Count");
                }
            }
        }

        private String _A2Count = String.Empty;
        public String A2Count
        {
            get { return _A2Count; }
            set
            {
                if (_A2Count != value)
                {
                    _A2Count = value;
                    RaisePropertyChanged("A2Count");
                }
            }
        }

        private String _A3Count = String.Empty;
        public String A3Count
        {
            get { return _A3Count; }
            set
            {
                if (_A3Count != value)
                {
                    _A3Count = value;
                    RaisePropertyChanged("A3Count");
                }
            }
        }

        private String _A4Count = String.Empty;
        public String A4Count
        {
            get { return _A4Count; }
            set
            {
                if (_A4Count != value)
                {
                    _A4Count = value;
                    RaisePropertyChanged("A4Count");
                }
            }
        }

        private String _A5Count = String.Empty;
        public String A5Count
        {
            get { return _A5Count; }
            set
            {
                if (_A5Count != value)
                {
                    _A5Count = value;
                    RaisePropertyChanged("A5Count");
                }
            }
        }

        private String _A6Count = String.Empty;
        public String A6Count
        {
            get { return _A6Count; }
            set
            {
                if (_A6Count != value)
                {
                    _A6Count = value;
                    RaisePropertyChanged("A6Count");
                }
            }
        }

        private String _A7Count = String.Empty;
        public String A7Count
        {
            get { return _A7Count; }
            set
            {
                if (_A7Count != value)
                {
                    _A7Count = value;
                    RaisePropertyChanged("A7Count");
                }
            }
        }

        private String _A8Count = String.Empty;
        public String A8Count
        {
            get { return _A8Count; }
            set
            {
                if (_A8Count != value)
                {
                    _A8Count = value;
                    RaisePropertyChanged("A8Count");
                }
            }
        }

        private String _A1RR = String.Empty;
        public String A1RR
        {
            get { return _A1RR; }
            set
            {
                if (_A1RR != value)
                {
                    _A1RR = value;
                    RaisePropertyChanged("A1RR");
                }
            }
        }

        private String _A2RR = String.Empty;
        public String A2RR
        {
            get { return _A2RR; }
            set
            {
                if (_A2RR != value)
                {
                    _A2RR = value;
                    RaisePropertyChanged("A2RR");
                }
            }
        }


        private String _A3RR = String.Empty;
        public String A3RR
        {
            get { return _A3RR; }
            set
            {
                if (_A3RR != value)
                {
                    _A3RR = value;
                    RaisePropertyChanged("A3RR");
                }
            }
        }

        private String _A4RR = String.Empty;
        public String A4RR
        {
            get { return _A4RR; }
            set
            {
                if (_A4RR != value)
                {
                    _A4RR = value;
                    RaisePropertyChanged("A4RR");
                }
            }
        }

        private String _A5RR = String.Empty;
        public String A5RR
        {
            get { return _A5RR; }
            set
            {
                if (_A5RR != value)
                {
                    _A5RR = value;
                    RaisePropertyChanged("A5RR");
                }
            }
        }

        private String _A6RR = String.Empty;
        public String A6RR
        {
            get { return _A6RR; }
            set
            {
                if (_A6RR != value)
                {
                    _A6RR = value;
                    RaisePropertyChanged("A6RR");
                }
            }
        }

        private String _A7RR = String.Empty;
        public String A7RR
        {
            get { return _A7RR; }
            set
            {
                if (_A7RR != value)
                {
                    _A7RR = value;
                    RaisePropertyChanged("A7RR");
                }
            }
        }

        private String _A8RR = String.Empty;
        public String A8RR
        {
            get { return _A8RR; }
            set
            {
                if (_A8RR != value)
                {
                    _A8RR = value;
                    RaisePropertyChanged("A8RR");
                }
            }
        }


        private String _A1StartT = String.Empty;
        public String A1StartT
        {
            get { return _A1StartT; }
            set
            {
                if (_A1StartT != value)
                {
                    _A1StartT = value;
                    RaisePropertyChanged("A1StartT");
                }
            }
        }

        private String _A1EndT = String.Empty;
        public String A1EndT
        {
            get { return _A1EndT; }
            set
            {
                if (_A1EndT != value)
                {
                    _A1EndT = value;
                    RaisePropertyChanged("A1EndT");
                }
            }
        }

        private String _A2StartT = String.Empty;
        public String A2StartT
        {
            get { return _A2StartT; }
            set
            {
                if (_A2StartT != value)
                {
                    _A2StartT = value;
                    RaisePropertyChanged("A2StartT");
                }
            }
        }

        private String _A2EndT = String.Empty;
        public String A2EndT
        {
            get { return _A2EndT; }
            set
            {
                if (_A2EndT != value)
                {
                    _A2EndT = value;
                    RaisePropertyChanged("A2EndT");
                }
            }
        }


        private String _A3StartT = String.Empty;
        public String A3StartT
        {
            get { return _A3StartT; }
            set
            {
                if (_A3StartT != value)
                {
                    _A3StartT = value;
                    RaisePropertyChanged("A3StartT");
                }
            }
        }

        private String _A3EndT = String.Empty;
        public String A3EndT
        {
            get { return _A3EndT; }
            set
            {
                if (_A3EndT != value)
                {
                    _A3EndT = value;
                    RaisePropertyChanged("A3EndT");
                }
            }
        }


        public event PropertyChangedEventHandler PropertyChanged;
        private void RaisePropertyChanged(String prop)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }
    }
}
