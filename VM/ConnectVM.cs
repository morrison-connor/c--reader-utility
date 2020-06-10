using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace RFID.Utility.VM
{
    public class ConnectVM : INotifyPropertyChanged
    {
        private readonly object _blestocksLock = new object();
        public ObservableCollection<BLEListViewItem> BLEDeviceUnpairedItemsSource { get; private set; }


        public ConnectVM()
        {
            BLEDeviceUnpairedItemsSource = new ObservableCollection<BLEListViewItem>();
            BindingOperations.EnableCollectionSynchronization(BLEDeviceUnpairedItemsSource, _blestocksLock);
        }


        public void BLEListViewAddNewItem(BLEListViewItem items)
        {
            BLEDeviceUnpairedItemsSource.Add(items);
        }

        public void BLEListViewUpdatedItem(BLEListViewItem Sourceitems, BLEListViewItem Destitems)
        {
            BLEDeviceUnpairedItemsSource.Insert(BLEDeviceUnpairedItemsSource.IndexOf(Sourceitems), Destitems);
            BLEDeviceUnpairedItemsSource.Remove(Sourceitems);
        }

        public void BLEListViewRemoveItem(BLEListViewItem items)
        {
            BLEDeviceUnpairedItemsSource.Remove(items);
        }

        private BLEListViewItem _BLEDeviceUnpairedItemsSelected;
        public BLEListViewItem BLEDeviceUnpairedItemsSelected
        {
            get { return _BLEDeviceUnpairedItemsSelected; }
            set
            {
                if (value != _BLEDeviceUnpairedItemsSelected)
                {
                    _BLEDeviceUnpairedItemsSelected = value;
                    OnPropertyChanged("BLEDeviceUnpairedItemsSelected");
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(String property)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }
    }
}
