using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.IO.Ports;
using System.Collections.ObjectModel;
using System.Threading;
using System.Windows.Threading;
using RFID.Service;
using RFID.Service.Interface.COM;
using RFID.Service.Interface.NET;
using System.Net.Sockets;
using RFID.Service.Interface.USB;
using System.Collections.Generic;
using RFID.Utility.VM;
using System.Linq;
using System.Net;

namespace RFID.Utility
{
	/// <summary>
	/// Interaction logic for ConnectDialog.xaml
	/// </summary>
	public partial class ConnectDialog : Window, IDisposable
	{
		private ReaderService	                _ReaderService;
        private ICOM                            _ICOM;
        private INET                            _INet;
        private UdpClient                       _UDPSocket;
        private IUSB                            _IUSB;
        private IEnumerable<IUSB>               _IUSBs;
        private ReaderService.ConnectType       _ConnectType = ReaderService.ConnectType.DEFAULT;
        private Thread			                SearchThread;
        private Thread                          NetSearchThread;
        private static ManualResetEvent         UPDReceiveDone = new ManualResetEvent(false);
        private ObservableCollection<String>    OCUPDReceives = new ObservableCollection<String>();
        private Int32                           IBaudRate = 38400;


        public ConnectDialog()
        {
			InitializeComponent();
            //COM
            this.SearchThread = new Thread(DoSearchCOMWork) {
                IsBackground = true
            };
            this.SearchThread.Start();

            this._ReaderService = new ReaderService();
        }
        

        public ConnectDialog(Module.BaudRate br)
        {
            InitializeComponent();
            //COM
            switch (br)
            {
                case Module.BaudRate.B4800: this.ComboBoxCOMBaudRate.SelectedIndex = 0; break;
                case Module.BaudRate.B9600: this.ComboBoxCOMBaudRate.SelectedIndex = 1; break;
                case Module.BaudRate.B14400: this.ComboBoxCOMBaudRate.SelectedIndex = 2; break;
                case Module.BaudRate.B19200: this.ComboBoxCOMBaudRate.SelectedIndex = 3; break;
                case Module.BaudRate.B38400: this.ComboBoxCOMBaudRate.SelectedIndex = 4; break;
                case Module.BaudRate.B57600: this.ComboBoxCOMBaudRate.SelectedIndex = 5; break;
                case Module.BaudRate.B115200: this.ComboBoxCOMBaudRate.SelectedIndex = 6; break;
                case Module.BaudRate.B230400: this.ComboBoxCOMBaudRate.SelectedIndex = 7; break;
            }
            this.SearchThread = new Thread(DoSearchCOMWork) {
                IsBackground = true
            };
            this.SearchThread.Start();

            this._ReaderService = new ReaderService();
        }

        public ConnectDialog(Module.BaudRate br, IUSB usb)
        {
            InitializeComponent();
            TabControl.SelectedIndex = 1;
            //COM
            switch (br)
            {
                case Module.BaudRate.B4800: this.ComboBoxCOMBaudRate.SelectedIndex = 0; break;
                case Module.BaudRate.B9600: this.ComboBoxCOMBaudRate.SelectedIndex = 1; break;
                case Module.BaudRate.B14400: this.ComboBoxCOMBaudRate.SelectedIndex = 2; break;
                case Module.BaudRate.B19200: this.ComboBoxCOMBaudRate.SelectedIndex = 3; break;
                case Module.BaudRate.B38400: this.ComboBoxCOMBaudRate.SelectedIndex = 4; break;
                case Module.BaudRate.B57600: this.ComboBoxCOMBaudRate.SelectedIndex = 5; break;
                case Module.BaudRate.B115200: this.ComboBoxCOMBaudRate.SelectedIndex = 6; break;
                case Module.BaudRate.B230400: this.ComboBoxCOMBaudRate.SelectedIndex = 7; break;
            }
            this._ReaderService = new ReaderService();
        }


        public ConnectDialog(Module.BaudRate br, INETInfo inet)
        {
            InitializeComponent();
            TabControl.SelectedIndex = 2;
            //COM
            switch (br)
            {
                case Module.BaudRate.B4800: this.ComboBoxCOMBaudRate.SelectedIndex = 0; break;
                case Module.BaudRate.B9600: this.ComboBoxCOMBaudRate.SelectedIndex = 1; break;
                case Module.BaudRate.B14400: this.ComboBoxCOMBaudRate.SelectedIndex = 2; break;
                case Module.BaudRate.B19200: this.ComboBoxCOMBaudRate.SelectedIndex = 3; break;
                case Module.BaudRate.B38400: this.ComboBoxCOMBaudRate.SelectedIndex = 4; break;
                case Module.BaudRate.B57600: this.ComboBoxCOMBaudRate.SelectedIndex = 5; break;
                case Module.BaudRate.B115200: this.ComboBoxCOMBaudRate.SelectedIndex = 6; break;
                case Module.BaudRate.B230400: this.ComboBoxCOMBaudRate.SelectedIndex = 7; break;
            }
            //NET
            TextBoxNetIP.Text = inet.IP;
            TextBoxNetPort.Text = inet.Port;

            this._ReaderService = new ReaderService();
        }

        




        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
		public ReaderService GetService() { return this._ReaderService; }


        /// <summary>
        /// Return interface type
        /// </summary>
        /// <returns></returns>
        public ReaderService.ConnectType GetIType()
        {
            return _ConnectType;
        }

        /// <summary>
        /// Get COM handler
        /// </summary>
        /// <returns></returns>
        public ICOM GetICOM()
        {
            return _ICOM;
        }

        /// <summary>
        /// Get Net handler
        /// </summary>
        /// <returns></returns>
        public INET GetINET()
        {
            return _INet;
        }

        /// <summary>
        /// Get USB handler
        /// </summary>
        /// <returns></returns>
        public IUSB GetIUSB()
        {
            return _IUSB;
        }


        /// <summary>
        /// Close the utility
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnConnectDialogCloseClick(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            if (this._ICOM != null && this._ICOM.IsOpen())
                this._ICOM.Close();
            if (this._INet != null && this._INet.IsConnected())
            {
                this._INet.Close();

                if (this._UDPSocket != null)
                {
                    this._UDPSocket.Close();
                    this._UDPSocket = null;
                }
                OCUPDReceives.Clear();
            }

            if (this._IUSB != null && this._IUSB.IsOpen)
                this._INet.Close();

            this.Close();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
		private void OnConnectBorderMouseLeftDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="str"></param>
		private void ShowOnTBMSG1(String page, String str)
        {
            switch (page)
            {
                case "COM":
                    this.TBMSG1.Text = str;
                    break;
                case "Net":
                    break;
                case "USB":
                    this.USBTBMSG1.Text = str;
                    break;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="str"></param>
		private void ShowOnTBMSG2(String page, String str)
        {
            switch (page)
            {
                case "COM":
                    this.TBMSG2.Text = str;
                    break;
                case "Net":
                    break;
                case "USB":
                    this.USBTBMSG2.Text = str;
                    break;
            }
        }

        private void TabControlSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //The sender is a type of TabControl...

            if (sender is TabControl tc)
            {
                var item = (TabItem)tc.SelectedItem;
                if (item != null)
                    switch (item.Header)
                    {

                        case "COM":
                            this.ButtonCOMEnter.IsEnabled = false;
                            break;
                        case "USB":

                            if (this._IUSB != null)
                            {
                                if (this._IUSB.IsOpen)
                                    this.ButtonUSBEnter.IsEnabled = true;
                                else
                                    this.ButtonUSBEnter.IsEnabled = false;
                            }
                            else
                                this.ButtonUSBEnter.IsEnabled = false;
                            break;
                        case "NET":
                            break;
                        default:
                            break;
                    }
            }
            e.Handled = true;
        }

        #region === COM ===
        /// <summary>
        /// Serach COM device
        /// Add baudrate parameter and modify process (2017/4/20)
        /// </summary>
        private void DoSearchCOMWork() {
            ObservableCollection<String> oc = new ObservableCollection<String>();
            foreach (ICOM.Reader rs in ICOM.GetReaders())
            {
                if (rs != null)
                    oc.Add(String.Format("{0} –{1}", rs.Name, rs.Description));
            }

            Dispatcher.Invoke(DispatcherPriority.Normal, new Action<ObservableCollection<String>>(CollectionData), oc);
		}


		private void DoConnectWork()
        {

			try {
                Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() => {
                    String[] words = (ComboBoxCOMReader.SelectedItem as String).Split(' ');
                    IBaudRate = Int32.Parse(((ComboBoxItem)ComboBoxCOMBaudRate.SelectedItem).Content.ToString());


                    this._ICOM = new ICOM();
                    this._ICOM.Open(words[0], IBaudRate, (Parity)Enum.Parse(typeof(Parity), "None", true), 8, (StopBits)Enum.Parse(typeof(StopBits), "One", true));

                    if (this._ICOM.IsOpen())
                    {
                        this._ICOM.Send(this._ReaderService.Command_V());
                        Byte[] b = this._ICOM.Receive();
                        if (b != null)
                        {
                            string s = Format.RemoveCRLF(Format.BytesToString(b));
                            if (s.Contains("V"))
                            {
                                ShowOnTBMSG2("COM", String.Format("{0}已連接並驗證", words[0]));
                                this.ButtonCOMEnter.IsEnabled = true;
                                this.ButtonCOMConnect.IsEnabled = false;
                            }       
                            else
                            {
                                ShowOnTBMSG2("COM", "開啟" + words[0] + "成功，驗證並非是Reader模組");
                                this._ICOM.Close();
                                this._ICOM = null;
                                ButtonCOMConnect.IsEnabled = true;
                            }
                        }
                        else
                        {
                            ShowOnTBMSG2("COM", "開啟" + words[0] + "成功，驗證未回覆。");
                            this._ICOM.Close();
                            this._ICOM = null;
                            ButtonCOMConnect.IsEnabled = true;
                        }
                    }
                    else
                    {
                        ShowOnTBMSG2("COM", "開啟" + words[0] + "失敗.");
                        this._ICOM.Close();
                        this._ICOM = null;
                        ButtonCOMConnect.IsEnabled = true;
                    }

                    
                    new Thread(DisableMsg).Start();
                }));
			}
			catch (Exception ex)
            {
                Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() => {
                    ShowOnTBMSG1("COM", "嘗試連接失敗");
                    ShowOnTBMSG2("COM", ex.Message);
                    this._ICOM.Close();
                    this._ICOM = null;
                }));
            }
		}

        private void DisableMsg()
        {
            Thread.Sleep(1600);
            Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() => {
                ShowOnTBMSG1("COM", "");
                ShowOnTBMSG2("COM", "");
            }));
        }
       
		private void CollectionData(ObservableCollection<String> s) {
			this.ComboBoxCOMReader.ItemsSource = s;
			this.ComboBoxCOMReader.SelectedIndex = 0;
        }

        /// <summary>
        /// When combobox dropdown is opened which to search COM device
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
		private void OnComboboxCOMReaderDropDownOpened(object sender, EventArgs e) {

            ObservableCollection<string> oc = new ObservableCollection<string>();
            foreach (ICOM.Reader rs in ICOM.GetReaders())
            {
                if (rs != null)
                    oc.Add(string.Format("{0} –{1}", rs.Name, rs.Description));
            }

            if (oc.Count > 0)
                CollectionData(oc);
		}

		private void OnButtonCOMConnectClick(object sender, RoutedEventArgs e)
        {
            ButtonCOMConnect.IsEnabled = false;
            ShowOnTBMSG1("COM", "");
            ShowOnTBMSG2("COM", "");
            this.SearchThread = new Thread(DoConnectWork) {
                IsBackground = true
            };
            this.SearchThread.Start();
		}

		private void OnButtonCOMEnterClick(object sender, RoutedEventArgs e) {
            this._ConnectType = ReaderService.ConnectType.COM;
            this.DialogResult = true;
		}

        #endregion


        #region === USB ===

        /// <summary>
        /// Serach USB device
        /// </summary>
        /// <param name="parameterObject"></param>
        /*private void DoSearchUSBWork()
        {
            Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() => {
                ObservableCollection<String> _oc = new ObservableCollection<String>();

                this._IUSBs = IUSB.Enumerate();
                for (int i = 0; i < this._IUSBs.Count(); i++)
                {
                    String _productName = this._IUSBs.ElementAt(i).ProductName ?? "NULL";
                    _oc.Add($"{i + 1}: {_productName} (VID: 0x{this._IUSBs.ElementAt(i).VendorId}, PID: 0x{this._IUSBs.ElementAt(i).ProductID})");
                }

                if (_oc.Count > 0)
                {
                    this.ComboBoxUSBReader.ItemsSource = _oc;
                    this.ComboBoxUSBReader.SelectedIndex = 0;
                }
            }));
        }*/


        private void OnButtonUSBEnumerateClick(object sender, RoutedEventArgs e)
        {

            this.SearchThread = new Thread(DoUSBEnumerateWork) {
                IsBackground = true
            };
            this.SearchThread.Start();
        }

        private void DoUSBEnumerateWork()
        {
            Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() => {
                ShowOnTBMSG1("USB", "Get HID device..");
            }));

            new Thread(USBDisableMsg1).Start();


            ObservableCollection<String> _oc = new ObservableCollection<String>();
            this._IUSBs = IUSB.Enumerate();

            for (int i = 0; i < this._IUSBs.Count(); i++)
            {
                String _productName = this._IUSBs.ElementAt(i).ProductName ?? "NULL";
                Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() => {
                    ShowOnTBMSG2("USB", $"{i + 1}: {_productName} ");
                }));

                _oc.Add($"{i + 1}: {_productName} (VID: 0x{this._IUSBs.ElementAt(i).VendorId}, PID: 0x{this._IUSBs.ElementAt(i).ProductID})");
            }

            if (_oc.Count > 0)
            {
                Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() => {
                    this.ComboBoxUSBReader.ItemsSource = _oc;
                    this.ComboBoxUSBReader.SelectedIndex = 0;
                }));
            }
            new Thread(USBDisableMsg2).Start();
        }

        private void USBDisableMsg1()
        {
            Thread.Sleep(2000);
            Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() => {
                ShowOnTBMSG1("USB", "");
            }));
        }

        private void USBDisableMsg2()
        {
            Thread.Sleep(500);
            Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() => {
                ShowOnTBMSG2("USB", "");
            }));
        }

        private void OnButtonUSBConnectClick(object sender, RoutedEventArgs e)
        {
            try
            {
                this._IUSB = _IUSBs.ElementAt(ComboBoxUSBReader.SelectedIndex);
                if (this._IUSB != null)
                {
                    this._IUSB.Open();
                    if (this._IUSB.IsOpen)
                    {
                        this.ButtonUSBEnter.IsEnabled = true;
                        this.ButtonUSBConnect.IsEnabled = false;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void OnButtonUSBEnterClick(object sender, RoutedEventArgs e)
        {
            this._ConnectType = ReaderService.ConnectType.USB;
            this.DialogResult = true;
        }
        #endregion




        #region === NET ===
        private void CheckBoxNetManageGroup(object sender, RoutedEventArgs e)
        {
            var cb = sender as CheckBox;
            switch (cb.Content.ToString())
            {
                case "Net Device List:":
                    CheckBoxNetAssign.IsChecked = false;
                    break;
                case "Net Assign:":
                    CheckBoxNetSearch.IsChecked = false;
                    break;
            }
        }

        private void DoSerachNetWork()
        {
            var b = System.Text.Encoding.UTF8.GetBytes("ReaderUtilityIOT");
            IPEndPoint iep = new IPEndPoint(IPAddress.Parse("255.255.255.255"), 8877);

            this._UDPSocket.Send(b, b.Length, iep);
            this._UDPSocket.BeginReceive(new AsyncCallback(OnUDPReceived), _UDPSocket);
            UPDReceiveDone.WaitOne(2000);
        }

        private void OnUDPReceived(IAsyncResult asyn)
        {
            try
            {
                UdpClient uc = (UdpClient)asyn.AsyncState;
                IPEndPoint iep = new IPEndPoint(IPAddress.Any, 0);

                byte[] data = uc.EndReceive(asyn, ref iep);
                if (data.Length > 0)
                {
                    String mac = Format.ByteToHexString(data[0]) + ':' + Format.ByteToHexString(data[1]) + ':' +
                                Format.ByteToHexString(data[2]) + ':' + Format.ByteToHexString(data[3]) + ':' +
                                Format.ByteToHexString(data[4]) + ':' + Format.ByteToHexString(data[5]);

                    Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() => {
                        for (int i = 0; i < ComboBoxNetReader.Items.Count; i++)
                        {
                            var val = ((ComboBoxNetReader.Items[i] as String).Split(';'))[0];
                            if (val.Equals(iep.Address.ToString())) return;
                        }

                        OCUPDReceives.Add(iep.Address.ToString() + "; MAC Address =" + mac);

                        ComboBoxNetReader.ItemsSource = OCUPDReceives;
                        ComboBoxNetReader.SelectedIndex = 0;

                    }));

                }
                UPDReceiveDone.Set();
                UPDReceiveDone.Reset();
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }
        }

        private void OnButtonNetSearchClick(object sender, RoutedEventArgs e)
        {
            OCUPDReceives.Clear();

            if (this._UDPSocket == null)
            {
                try
                {
                    this._UDPSocket = new UdpClient(8878);

                }
                catch (Exception ee)
                {
                    MessageBox.Show(ee.Message);
                }

            }

            this.NetSearchThread = new Thread(DoSerachNetWork) {
                IsBackground = true
            };
            this.NetSearchThread.Start();
        }

        private void OnButtonNetConnectClick(object sender, RoutedEventArgs e)
        {
            if (CheckBoxNetAssign.IsChecked.Value == true)
            {
                try
                {
                    _INet = new INET(TextBoxNetIP.Text, TextBoxNetPort.Text);
                    if (this._INet.IsConnected())
                    {
                        this._ConnectType = ReaderService.ConnectType.NET;
                        this.DialogResult = true;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }

            }
            else if (CheckBoxNetSearch.IsChecked.Value == true)
            {
                String ip = String.Empty;
                try
                {
                    ip = ((ComboBoxNetReader.SelectedValue as String).Split(';'))[0];
                    _INet = new INET(ip, "8800");
                    if (this._INet.IsConnected())
                    {
                        this._ConnectType = ReaderService.ConnectType.NET;

                        if (this._UDPSocket != null)
                        {
                            //this._UDPSocket.Dispose();
                            this._UDPSocket.Close();
                            this._UDPSocket = null;
                        }
                        this.DialogResult = true;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, ip);
                }
            }
        }

        #region IDisposable Support
        private bool disposedValue = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    OnConnectDialogCloseClick(null, null);
                }

                disposedValue = true;
            }
        }


        public void Dispose()
        {

            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion




        #endregion


    }
}
