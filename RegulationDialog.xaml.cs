﻿using System;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using System.Threading;
using System.Globalization;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using System.ComponentModel;
using System.IO.Ports;
using RFID.Utility.IClass;
using RFID.Service;
using RFID.Service.IInterface.COM;
using RFID.Service.IInterface.COM.Events;
using RFID.Service.IInterface.NET;
using RFID.Service.IInterface.USB;
using RFID.Utility.VM;
using RFID.Service.IInterface.USB.Events;
using RFID.Service.IInterface.NET.Events;
using System.Resources;
using System.Reflection;
using RFID.Service.IInterface.BLE;
using RFID.Service.IInterface.BLE.Events;

namespace RFID.Utility
{
	public partial class RegulationDialog : Window, IDisposable
    {

        /*enum CommandStatus
        {
            DEFAULT, TESTRUN
        };*/

        //private const int PROC_READ_REGULATION_FAKE     = 0x00;
        private const int PROC_READ_REGULATION			= 0x01;
		private const int PROC_READ_MODE_AND_CHANNEL	= 0x02;
		private const int PROC_READ_FREQ_OFFSET			= 0x03;
		private const int PROC_READ_POWER				= 0x04;
		private const int PROC_READ_FREQ				= 0x06;
		private const int PROC_SET_FREQ_H				= 0x07;
		private const int PROC_SET_FREQ_L				= 0x08;
		private const int PROC_SET_FREQ					= 0x09;
		private const int PROC_SET_MEASURE_FREQ			= 0X0A;
		private const int PROC_SET_RESET				= 0x0B;
		private const int PROC_FREQ_SET_RESET			= 0x0C;
        //private const int PROC_READ_GPIOCONFIG         = 0x0D;
        //private const int PROC_READ_GPIOPINS           = 0x0E;
        //private const int PROC_REBOOT					= 0xFF;

		private readonly DispatcherTimer		RepeatEvent = new DispatcherTimer();
		private readonly DispatcherTimer		ProcessEvent = new DispatcherTimer();
		private Byte[]							ReceiveData;
		private Int32                           PreProcess;
		
		private Boolean							IsSymbol = false;
		private Boolean                         IsAdjust = false, 
												IsPlus = false, 
												IsMinus = false, 
												IsReset = false;
		private Boolean                         IsRunning = false;
		private Boolean                         IsReceiveData = false;
		private Boolean                         IsDateTimeStamp = false;
		private Boolean                         IsSetFrequency = false;
		private ReaderService					_ReaderService = null;
        private ICOM                            _ICOM;
        private INET                            _INet;
        private IUSB                            _IUSB;
        private IBLE                            _IBLE;
        private ICOM.CombineDataEventHandler    combineDataEventHandler;
        private INET.NetTCPDataEventHandler     netTCPDataEventHandler;
        private IUSB.USBDataEventHandler        uSBDataEventHandler;
        private IBLE.BLEDataEventHandler        bLEDataEventHandler;
        private ReaderModule.Version	        _Version;
        private ReaderModule.BaudRate           _BaudRate = ReaderModule.BaudRate.B38400;
        private ReaderService.ConnectType       _ConnectType = ReaderService.ConnectType.DEFAULT;
        private Int32                           IRegulation = 8;
        private CultureInfo						Culture;
		private Int32                           BasebandMode;
        //private CommandStatus                   DoProcess = CommandStatus.DEFAULT;
        private Boolean                         IsReceiveEvent = false;
        private volatile Boolean                IsSendPass = false;
        private String                          ReceiveEventData = String.Empty;
        private Boolean                         IsReceiveFake = false;
        private Thread                          SetThread;
        private RegulationVM                    VM = new RegulationVM();
        private ResourceManager                 stringManager = new ResourceManager("en-US", Assembly.GetExecutingAssembly());
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public ReaderModule.BaudRate GetBaudRate()
        {
            return _BaudRate;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public ReaderService GetService()
        {
            return this._ReaderService;
        }


        public RegulationDialog(ReaderService service, ReaderService.ConnectType type, ReaderModule.Version v, ReaderModule.BaudRate br, CultureInfo selectedCulture) {
			InitializeComponent();

            this._ReaderService = service;
            this._ConnectType = type;
            this._Version = v;
            this.Culture = selectedCulture;
            this._BaudRate = br;

            UICtrlModify(this._Version);
            AreaModify(this._Version);
            PowerModify(this._Version);
			FrequencyStepModify();
            BaudRateModify(this._BaudRate);
            DataContext = VM;

            if (service == null)
                throw new ArgumentNullException(stringManager.GetString("ReaderService is null", CultureInfo.CurrentCulture));

            switch (this._ConnectType)
            {
                case ReaderService.ConnectType.COM:
                    this._ICOM = service.COM;
                    this.combineDataEventHandler = new ICOM.CombineDataEventHandler(DoReceiveDataWork);
                    this._ICOM.CombineDataReceiveEventHandler += combineDataEventHandler;
                    break;
                case ReaderService.ConnectType.NET:
                    this._INet = service.NET;
                    this.netTCPDataEventHandler = new INET.NetTCPDataEventHandler(DoReceiveDataWork);
                    this._INet.NetTCPDataReceiveEventHandler += netTCPDataEventHandler;
                    break;
                case ReaderService.ConnectType.USB:
                    this._IUSB = service.USB;
                    this.uSBDataEventHandler = new IUSB.USBDataEventHandler(DoReceiveDataWork);
                    this._IUSB.USBDataReceiveEvent += uSBDataEventHandler;
                    break;
                case ReaderService.ConnectType.BLE:
                    this._IBLE = service.BLE;
                    this.bLEDataEventHandler = new IBLE.BLEDataEventHandler(DoReceiveDataWork);
                    this._IBLE.BLEDataReceiveEvent += bLEDataEventHandler;
                    break;
            }

        }




        /// <summary>
        /// Add R300S_D306, R300V_D405, R300V_D405 (2017.9.4)
        /// </summary>
        /// <param name="v"></param>
        private void UICtrlModify(ReaderModule.Version v)
        {
            switch (v)
            {
                case ReaderModule.Version.FIR3008:
                case ReaderModule.Version.FIRXXXX:
                    UIUnitControl((this.Culture.IetfLanguageTag == "en-US") ?
                    "The Reader version is not support the advanced setting." :
                    "此版本Reader不支援此進階操作", false);
                    break;
                case ReaderModule.Version.FIR300AC1:
                case ReaderModule.Version.FIR300AC2:
                case ReaderModule.Version.FIR300TD1:
                case ReaderModule.Version.FIR300TD2:
                case ReaderModule.Version.FIA300S:
                    this.GroupBaudRate.IsEnabled = false;
                    EventInit();
                    break;
                case ReaderModule.Version.FIR300AC2C4:
                case ReaderModule.Version.FIR300TD204:
                case ReaderModule.Version.FIR300AC2C5:
                case ReaderModule.Version.FIR300AC2C6:
                case ReaderModule.Version.FIR300TD205:
                case ReaderModule.Version.FIR300TD206:
                case ReaderModule.Version.FIR300TH:
                    this.GroupBaudRate.IsEnabled = false;
                    EventInit();
                    break;
                case ReaderModule.Version.FIR300AC3:
                case ReaderModule.Version.FIR300S:
                case ReaderModule.Version.FIR300SD305:
                case ReaderModule.Version.FIR300SD306:
                case ReaderModule.Version.FIR300AC3C5:
                //case Module.Version.FI_R300V_D405:
                case ReaderModule.Version.FIR300VD406:
                case ReaderModule.Version.FIR300AH:
                case ReaderModule.Version.FIR300SH:
                case ReaderModule.Version.FIR300VH:
                    EventInit();
                    break;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="v"></param>
        private void AreaModify(ReaderModule.Version v)
        {
            ObservableCollection<String> list = new ObservableCollection<String>();
            switch (v)
            {
                case ReaderModule.Version.FIR300TD1:
                case ReaderModule.Version.FIR300TD2:
                case ReaderModule.Version.FIR300AC1:
                case ReaderModule.Version.FIR300AC2:
                case ReaderModule.Version.FIA300S:
                case ReaderModule.Version.FIRXXXX:
                    foreach (AreaItem area in ReaderModule.GetAreaGroups(ReaderModule.Version.FIR300TD1))
                        list.Add(String.Format(CultureInfo.CurrentCulture, "{0}", area.LocationName));
                    break;
                case ReaderModule.Version.FIR300AC2C4:
                case ReaderModule.Version.FIR300TD204:
                case ReaderModule.Version.FIR300S:
                case ReaderModule.Version.FIR300AC3:
                    foreach (AreaItem area in ReaderModule.GetAreaGroups(ReaderModule.Version.FIR300AC3))
                        list.Add(String.Format(CultureInfo.CurrentCulture, "{0}", area.LocationName));
                    break;
                case ReaderModule.Version.FIR300AC2C5:
                case ReaderModule.Version.FIR300AC3C5:
                case ReaderModule.Version.FIR300TD205:
                case ReaderModule.Version.FIR300SD305:
                    foreach (AreaItem area in ReaderModule.GetAreaGroups(ReaderModule.Version.FIR300AC2C5))
                        list.Add(String.Format(CultureInfo.CurrentCulture, "{0}", area.LocationName));
                    break;
                case ReaderModule.Version.FIR300AC2C6:
                case ReaderModule.Version.FIR300SD306:
                //case Module.Version.FI_R300V_D405:
                case ReaderModule.Version.FIR300VD406:
                case ReaderModule.Version.FIR300TD206:
                case ReaderModule.Version.FIR300AH:
                case ReaderModule.Version.FIR300SH:
                case ReaderModule.Version.FIR300VH:
                case ReaderModule.Version.FIR300TH:
                    foreach (AreaItem area in ReaderModule.GetAreaGroups(ReaderModule.Version.FIR300VD406))
                        list.Add(String.Format(CultureInfo.CurrentCulture, "{0}", area.LocationName));
                    break;
            }

            this.ComboBoxArea.ItemsSource = list;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="v"></param>
        private void PowerModify(ReaderModule.Version v)
        {
            ObservableCollection<string> list = new ObservableCollection<string>();
            switch (v)
            {
                case ReaderModule.Version.FIR300TD1:
                case ReaderModule.Version.FIR300TD2:
                case ReaderModule.Version.FIR300TD204:
                case ReaderModule.Version.FIR300TD205:
                case ReaderModule.Version.FIR300TD206:
                case ReaderModule.Version.FIR300TH:
                    foreach (PowerItem power in ReaderModule.GetPowerGroups(ReaderModule.Version.FIR300TD1))
                        list.Add(String.Format(CultureInfo.CurrentCulture, "{0}", power.LocationName));
                    break;
                case ReaderModule.Version.FIR300AC1:
                case ReaderModule.Version.FIR300AC2:
                case ReaderModule.Version.FIR300AC2C4:
                case ReaderModule.Version.FIR300AC3:
                case ReaderModule.Version.FIR300AC2C5:
                case ReaderModule.Version.FIR300AC2C6:
                case ReaderModule.Version.FIR300AC3C5:
                case ReaderModule.Version.FIR300AH:
                    foreach (PowerItem power in ReaderModule.GetPowerGroups(ReaderModule.Version.FIR300AC1))
                        list.Add(String.Format(CultureInfo.CurrentCulture, "{0}", power.LocationName));
                    break;
                case ReaderModule.Version.FIR300S:
                case ReaderModule.Version.FIA300S:
                case ReaderModule.Version.FIR300SD305:
                case ReaderModule.Version.FIR300SD306:
                case ReaderModule.Version.FIR300SH:
                    foreach (PowerItem power in ReaderModule.GetPowerGroups(ReaderModule.Version.FIA300S))
                        list.Add(String.Format(CultureInfo.CurrentCulture, "{0}", power.LocationName));
                    break;
                //case Module.Version.FI_R300V_D405:
                case ReaderModule.Version.FIR300VD406:
                case ReaderModule.Version.FIR300VH:
                    foreach (PowerItem power in ReaderModule.GetPowerGroups(ReaderModule.Version.FIR300VD406))
                        list.Add(String.Format(CultureInfo.CurrentCulture, "{0}", power.LocationName));
                    break;
                case ReaderModule.Version.FIRXXXX:
                    list.Add("== N/A ==");
                    break;
            }

            this.ComboboxPower.ItemsSource = list;
            this.ComboboxPower.SelectedIndex = 0;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="br"></param>
        private void BaudRateModify(ReaderModule.BaudRate br)
        {
            switch (br)
            {
                case ReaderModule.BaudRate.B4800: VM.ComboBoxBaudRateSelectedBaudRate = VM.BaudRate[0]; break;
                case ReaderModule.BaudRate.B9600: VM.ComboBoxBaudRateSelectedBaudRate = VM.BaudRate[1]; break;
                case ReaderModule.BaudRate.B14400: VM.ComboBoxBaudRateSelectedBaudRate = VM.BaudRate[2]; break;
                case ReaderModule.BaudRate.B19200: VM.ComboBoxBaudRateSelectedBaudRate = VM.BaudRate[3]; break;
                case ReaderModule.BaudRate.B38400: VM.ComboBoxBaudRateSelectedBaudRate = VM.BaudRate[4]; break;
                case ReaderModule.BaudRate.B57600: VM.ComboBoxBaudRateSelectedBaudRate = VM.BaudRate[5]; break;
                case ReaderModule.BaudRate.B115200: VM.ComboBoxBaudRateSelectedBaudRate = VM.BaudRate[6]; break;
                case ReaderModule.BaudRate.B230400: VM.ComboBoxBaudRateSelectedBaudRate = VM.BaudRate[7]; break;
            }
        }

        /// <summary>
        /// 
        /// </summary>
		private void FrequencyStepModify()
        {
            ObservableCollection<String> list = new ObservableCollection<String>();
            foreach (String s in DataRepository.GetStepGroups()) list.Add(s);
            this.ComboboxStep.ItemsSource = list;
            this.ComboboxStep.SelectedIndex = 0;
        }

        /// <summary>
        /// 
        /// </summary>
        private void EventInit()
        {
            this.RepeatEvent.Tick += new EventHandler(DoRepeatWork);
            this.RepeatEvent.Interval = TimeSpan.FromMilliseconds(16);
            this.ProcessEvent.Tick += new EventHandler(DoPreProcessWork);
            this.ProcessEvent.Interval = TimeSpan.FromMilliseconds(16);
            this.PreProcess = PROC_READ_REGULATION;// PROC_READ_REGULATION_FAKE;
            this.ProcessEvent.Start();
            UIUnitControl((this.Culture.IetfLanguageTag == "en-US") ?
                "Get the Reader information." :
                "取得Reader資訊..", false);
        }





        #region === Group Setting ===
        /// <summary>
        ///  Modify use J000(reset) to set frequency (2017/4/18)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnButtonSetAreaClick(object sender, RoutedEventArgs e)
        {
            if (ComboBoxFrequency != null && ComboBoxMeasureFrequency != null)
            {
                this.SetThread = new Thread(DoSetAreaWork)
                {
                    IsBackground = true
                };
                this.SetThread.Start();

                
            }
        }

        private void DoSetAreaWork()
        {
            //J000(reset) to set frequency
            IsSetFrequency = true;
            Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() =>
            {
                UIUnitControl((this.Culture.IetfLanguageTag == "en-US") ? "Reset and wait few second." : "重置..請稍後", false);
            }));
            DoSendWork(this._ReaderService.CommandJ("0", "00"), ReaderModule.CommandType.Normal);
            DoReceiveWork();

            ReaderModule.Regulation _regulation = ReaderModule.Regulation.US;
            Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() =>
            {
                UIUnitControl((this.Culture.IetfLanguageTag == "en-US") ? "Set area." : "設定區域", false);
            }));

            switch (VM.ComboBoxAreaSelectedIndex/*this.ComboBoxArea.SelectedIndex*/)
            {
                case 0:
                    _regulation = ReaderModule.Regulation.US;
                    DoSendWork(this._ReaderService.SetRegulation(ReaderModule.Regulation.US), ReaderModule.CommandType.Normal);
                    break;
                case 1:
                    _regulation = ReaderModule.Regulation.TW;
                    DoSendWork(this._ReaderService.SetRegulation(ReaderModule.Regulation.TW), ReaderModule.CommandType.Normal);
                    break;
                case 2:
                    _regulation = ReaderModule.Regulation.CN;
                    DoSendWork(this._ReaderService.SetRegulation(ReaderModule.Regulation.CN), ReaderModule.CommandType.Normal);
                    break;
                case 3:
                    _regulation = ReaderModule.Regulation.CN2;
                    DoSendWork(this._ReaderService.SetRegulation(ReaderModule.Regulation.CN2), ReaderModule.CommandType.Normal);
                    break;
                case 4:
                    _regulation = ReaderModule.Regulation.EU;
                    DoSendWork(this._ReaderService.SetRegulation(ReaderModule.Regulation.EU), ReaderModule.CommandType.Normal);
                    break;
                case 5:
                    _regulation = ReaderModule.Regulation.JP;
                    DoSendWork(this._ReaderService.SetRegulation(ReaderModule.Regulation.JP), ReaderModule.CommandType.Normal);
                    break;
                case 6:
                    _regulation = ReaderModule.Regulation.KR;
                    DoSendWork(this._ReaderService.SetRegulation(ReaderModule.Regulation.KR), ReaderModule.CommandType.Normal);
                    break;
                case 7:
                    _regulation = ReaderModule.Regulation.VN;
                    DoSendWork(this._ReaderService.SetRegulation(ReaderModule.Regulation.VN), ReaderModule.CommandType.Normal);
                    break;
                case 8:
                    _regulation = ReaderModule.Regulation.EU2;
                    DoSendWork(this._ReaderService.SetRegulation(ReaderModule.Regulation.EU2), ReaderModule.CommandType.Normal);
                    break;
                case 9:
                    _regulation = ReaderModule.Regulation.IN;
                    DoSendWork(this._ReaderService.SetRegulation(ReaderModule.Regulation.IN), ReaderModule.CommandType.Normal);
                    break;
            }

            this.ReceiveData = DoReceiveWork();
            if (this.ReceiveData != null && this.ReceiveData.Length == 6)
            {
                String str = Encoding.ASCII.GetString(new Byte[] { ReceiveData[2], ReceiveData[3] });
                System.Reflection.FieldInfo fi = _regulation.GetType().GetField(_regulation.ToString());
                if (fi != null)
                {
                    object[] attrs = fi.GetCustomAttributes(typeof(DescriptionAttribute), true);
                    if (attrs != null && attrs.Length > 0)
                    {
                        String dbr = ((DescriptionAttribute)attrs[0]).Description;
                        if (dbr.Equals(str, StringComparison.CurrentCulture))
                        {
                            Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() =>
                            {
                                UIUnitControl((this.Culture.IetfLanguageTag == "en-US") ? "Set area success" : "設定area成功", true);
                                if (ComboBoxFrequency != null && ComboBoxMeasureFrequency != null)
                                {
                                    ComboBoxSetAreaChanged(VM.ComboBoxAreaSelectedIndex/*this.ComboBoxArea.SelectedIndex*/);
                                    this.IRegulation = VM.ComboBoxAreaSelectedIndex/*this.ComboBoxArea.SelectedIndex*/ + 1;
                                }
                            }));
                            //this.LabelMessage.Text = (this.Culture.IetfLanguageTag == "en-US") ? "Set area success" : "設定area成功";
                            
                        }
                    }
                }
            }
            else
            {
                Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() =>
                {
                    UIUnitControl((this.Culture.IetfLanguageTag == "en-US") ? "Set area no reply or error" : "設定area未回覆或錯誤", true);
                }));
                //this.LabelMessage.Text = (this.Culture.IetfLanguageTag == "en-US") ? "Set area no reply or error" : "設定area未回覆或錯誤";
            }
            
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="area"></param>
        private void SetArea(ReaderModule.Regulation area)
        {
            ObservableCollection<String> items = new ObservableCollection<String>();

            for (Int32 i = 0; i < ReaderModule.RegulationChannel(area); i++)
            {
                Double j = ReaderModule.RegulationFrequency(area, i);
                items.Add(String.Format(CultureInfo.CurrentCulture, "{0}MHz", j.ToString("###.000", CultureInfo.CurrentCulture)));
            }
            items.Add("hopping");

            ComboBoxFrequency.ItemsSource = items;
            ComboBoxFrequency.SelectedIndex = ReaderModule.RegulationMidChannel(area);
            ComboBoxMeasureFrequency.ItemsSource = items;
            ComboBoxMeasureFrequency.SelectedIndex = ReaderModule.RegulationChannel(area);
        }


        /// <summary>
        /// Modify ComboBoxMeasureFrequency.SelectedIndex = hopping & radiobutton to BasebandCarryMode (2017/4/18)
        /// </summary>
        /// <param name="index"></param>
        private void ComboBoxSetAreaChanged(Int32 index)
        {
			ObservableCollection<String> items = new ObservableCollection<String>();

            //radiobutton reset to BasebandCarryMode
            BasebandCarryMode.IsChecked = true;

            switch (index)
            {
				case 0:
                    SetArea(ReaderModule.Regulation.US);
					break;
				case 1:
                    SetArea(ReaderModule.Regulation.TW);
                    break;
				case 2:
                    SetArea(ReaderModule.Regulation.CN);
                    break;
				case 3:
                    SetArea(ReaderModule.Regulation.CN2);
                    break;
				case 4:
                    SetArea(ReaderModule.Regulation.EU);
                    break;
                case 5:
                    SetArea(ReaderModule.Regulation.JP);
                    break;
                case 6:
                    SetArea(ReaderModule.Regulation.KR);
                    break;
                case 7:
                    SetArea(ReaderModule.Regulation.VN);
                    break;
                case 8:
                    SetArea(ReaderModule.Regulation.EU2);
                    break;
                case 9:
                    SetArea(ReaderModule.Regulation.IN);
                    break;
            }
		}

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
		private void OnButtonSetFrequencyClick(object sender, RoutedEventArgs e)
        {
			Int32 idx = this.ComboBoxFrequency.SelectedIndex + 1;
			String str = Format.ByteToHexString((Byte)idx);

			DoSendWork(this._ReaderService.CommandJ("1", str), ReaderModule.CommandType.Normal);
			DoReceiveWork();
		}

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
		private void OnButtonAdjustClick(object sender, RoutedEventArgs e)
        {
			if (!String.IsNullOrEmpty(TextBoxMeasureFrequency.GetLineText(0))) {
				this.IsAdjust = true;
				this.PreProcess = PROC_READ_FREQ;
				this.ProcessEvent.Start();
				UIUnitControl(String.Empty, false);
			}
			else {
				UIUnitControl((this.Culture.IetfLanguageTag == "en-US") ?
							"Enter the measurement frequency.." :
							"請輸入量測頻率", true);
				FocusManager.SetFocusedElement(this, this.TextBoxMeasureFrequency);
			}
		}

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
		private void OnButtonSetFrequencyPlusClick(object sender, RoutedEventArgs e)
        {
			this.IsPlus = true;
			this.PreProcess = PROC_READ_FREQ;
			this.ProcessEvent.Start();
			UIUnitControl(String.Empty, false);
		}

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
		private void OnButtonSetFrequencyMinusClick(object sender, RoutedEventArgs e)
        {
			this.IsMinus = true;
			this.PreProcess = PROC_READ_FREQ;
			this.ProcessEvent.Start();
            UIUnitControl(String.Empty, false);
		}

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
		private void OnButtonSetFrequencyResetClick(object sender, RoutedEventArgs e)
        {
			this.IsReset = true;
			this.PreProcess = PROC_FREQ_SET_RESET;
			this.ProcessEvent.Start();
			UIUnitControl(String.Empty, false);
		}

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
		private void OnComboboxPowerSelectionChanged(object sender, SelectionChangedEventArgs e) { }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnButtonSetPowerClick(object sender, RoutedEventArgs e)
        {
			Int32 idx = ReaderModule.GetPowerGroupsIndex(this._Version, ComboboxPower.SelectedIndex);

			DoSendWork(this._ReaderService.SetPower(Format.ByteToHexString((Byte)idx)), ReaderModule.CommandType.Normal);
			DoReceiveWork();
		}
        #endregion


        #region === Group Measure ===

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnRadioButtonBasebandModeChecked(object sender, RoutedEventArgs e) {
            if (!(sender is RadioButton radioButton)) return;
            this.BasebandMode = Convert.ToInt32(radioButton.Tag.ToString(), CultureInfo.CurrentCulture);
		}
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
		private void OnButtonMeasureSetFrequencyClick(object sender, RoutedEventArgs e)
        {
			Byte[] bs = null;
            Int32 idx = ComboBoxMeasureFrequency.SelectedIndex + 1;

			switch (ComboBoxArea.SelectedIndex)
            {
				case 0:
					if (idx == ReaderModule.RegulationChannel(ReaderModule.Regulation.US) + 1)
                        bs = this._ReaderService.CommandJ("0", "00");
					break;
				case 1:
                case 7:
					if (idx == ReaderModule.RegulationChannel(ReaderModule.Regulation.TW) + 1)
                        bs = this._ReaderService.CommandJ("0", "00");
					break;
				case 2:
				case 3:
					if (idx == ReaderModule.RegulationChannel(ReaderModule.Regulation.CN) + 1)
                        bs = this._ReaderService.CommandJ("0", "00");
					break;
				
				case 4:
                case 5:
                case 8:
                    if (idx == ReaderModule.RegulationChannel(ReaderModule.Regulation.EU) + 1)
                        bs = this._ReaderService.CommandJ("0", "00");
					break;
                case 6:
                    if (idx == ReaderModule.RegulationChannel(ReaderModule.Regulation.KR) + 1)
                        bs = this._ReaderService.CommandJ("0", "00");
                    break;
                case 9:
                    if (idx == ReaderModule.RegulationChannel(ReaderModule.Regulation.IN) + 1)
                        bs = this._ReaderService.CommandJ("0", "00");
                    break;
            }

			if (bs == null) {
				bs = this._ReaderService.CommandJ((this.BasebandMode == 2) ? "2" : "1", Format.ByteToHexString((Byte)idx));
			}
			IsSetFrequency = true;
			DoSendWork(bs, ReaderModule.CommandType.Normal);
			DoReceiveWork();
		}
  
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
		private void OnButtonMeasureRunClick(object sender, RoutedEventArgs e)
        {
			if (!IsSetFrequency)
            {
				UIUnitControl((this.Culture.IetfLanguageTag == "en-US") ? "Run set frequency first.." : "請先設定頻率..", true);
				ButtonMeasureSetFrequency.Focus();
				return;
			}

			if (!this.IsRunning)
            {
				this.IsRunning = true;
				this.ButtonMeasureRun.Content = (this.Culture.IetfLanguageTag == "en-US") ? "Stop" : "停止";
				UIUnitControl((this.Culture.IetfLanguageTag == "en-US") ? "Running Tag test.." : "正在執行Tag測試..", false);
                //DoProcess = CommandStatus.TESTRUN;
                this.RepeatEvent.Start();
            }
			else
            {
                this.IsRunning = false;
                this.ButtonMeasureRun.Content = (this.Culture.IetfLanguageTag == "en-US") ? "Run" : "執行";
                UIUnitControl((this.Culture.IetfLanguageTag == "en-US") ? "Tag test has stopped." : "已停止Tag測試", true);
                this.RepeatEvent.Stop();
            }
		}

        #endregion


        #region === Group Baudrate ===
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnButtonSetBuadRateClick(object sender, RoutedEventArgs e)
        {
            this.SetThread = new Thread(DoSetBuadRateWork)
            {
                IsBackground = true
            };
            this.SetThread.Start();           
        }

        private void DoSetBuadRateWork()
        {
            ReaderModule.BaudRate br = ReaderModule.BaudRate.B9600;
            switch (VM.ComboBoxBaudRateSelectedBaudRate.Tag)
            {
                case "0": br = ReaderModule.BaudRate.B4800; break;
                case "1": br = ReaderModule.BaudRate.B9600; break;
                case "2": br = ReaderModule.BaudRate.B14400; break;
                case "3": br = ReaderModule.BaudRate.B19200; break;
                case "4": br = ReaderModule.BaudRate.B38400; break;
                case "5": br = ReaderModule.BaudRate.B57600; break;
                case "6": br = ReaderModule.BaudRate.B115200; break;
                case "7": br = ReaderModule.BaudRate.B230400; break;
            }
            DoSendWork(this._ReaderService.SetBaudRate(br), ReaderModule.CommandType.Normal);
            this.ReceiveData = DoReceiveWork();
            if (this.ReceiveData != null && this.ReceiveData.Length == 6)
            {
                String str = Encoding.ASCII.GetString(new Byte[] { ReceiveData[2], ReceiveData[3] });
                System.Reflection.FieldInfo fi = _BaudRate.GetType().GetField(_BaudRate.ToString());
                if (fi != null)
                {
                    object[] attrs = fi.GetCustomAttributes(typeof(DescriptionAttribute), true);
                    if (attrs != null && attrs.Length > 0)
                    {
                        String dbr = ((DescriptionAttribute)attrs[0]).Description;
                        if (!dbr.Equals(str, StringComparison.CurrentCulture))
                        {
                            switch (_ConnectType)
                            {
                                default:
                                case ReaderService.ConnectType.DEFAULT:
                                    break;
                                case ReaderService.ConnectType.COM:
                                    Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() =>
                                    {
                                        UIUnitControl((this.Culture.IetfLanguageTag == "en-US") ? "Baud rate changed and reconnect module.." : "Baud rate改變，重新連線中..", false);
                                    }));
                                    String port = this._ICOM.GetSerialPort().PortName;
                                    Int32 ibr = ICOM.GetBaudRate(str);

                                    this._ICOM.Close();
                                    this._ICOM = null;
                                    Thread.Sleep(500);
                                    this._ICOM = new ICOM();
                                    this._ICOM.Open(port, ibr, (Parity)Enum.Parse(typeof(Parity), "None", true), 8, (StopBits)Enum.Parse(typeof(StopBits), "One", true));

                                    if (this._ICOM.IsOpen())
                                    {
                                        Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() =>
                                        {
                                            UIUnitControl((this.Culture.IetfLanguageTag == "en-US") ? 
                                                String.Format(CultureInfo.CurrentCulture, "Utility has reconnected to module, and change baud rate to {0}", ibr) : 
                                                String.Format(CultureInfo.CurrentCulture, "已重新連線, baud rate設定至{0}", ibr), true);
                                        }));
                                        _BaudRate = ICOM.GetBaudRate(ibr);
                                    }
                                    else
                                        Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() =>
                                        {
                                            UIUnitControl((this.Culture.IetfLanguageTag == "en-US") ? "reconnect error" : "連線失敗", false);
                                        }));
                                    break;
                                case ReaderService.ConnectType.USB:
                                    Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() =>
                                    {
                                        UIUnitControl((this.Culture.IetfLanguageTag == "en-US") ? "Baud rate changed and reconnect module.." : "Baud rate改變，重新連線中..", false);
                                        Thread.Sleep(500);
                                        Int32 _ibr = ICOM.GetBaudRate(str);
                                        UIUnitControl((this.Culture.IetfLanguageTag == "en-US") ? 
                                            String.Format(CultureInfo.CurrentCulture, "Utility has reconnected to module, and change baud rate to {0}", _ibr) : 
                                            String.Format(CultureInfo.CurrentCulture, "已重新連線, baud rate設定至{0}", _ibr), true);
                                        _BaudRate = ICOM.GetBaudRate(_ibr);
                                    }));
                                    
                                    break;
                                case ReaderService.ConnectType.NET:
                                    //
                                    Thread.Sleep(200);
                                    break;
                                case ReaderService.ConnectType.BLE:
                                    Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() =>
                                    {
                                        UIUnitControl((this.Culture.IetfLanguageTag == "en-US") ? "Baud rate changed and reconnect module.." : "Baud rate改變，重新連線中..", false);
                                        Thread.Sleep(500);
                                        Int32 _ibr = ICOM.GetBaudRate(str);
                                        UIUnitControl((this.Culture.IetfLanguageTag == "en-US") ?
                                            String.Format(CultureInfo.CurrentCulture, "Utility has reconnected to module, and change baud rate to {0}", _ibr) :
                                            String.Format(CultureInfo.CurrentCulture, "已重新連線, baud rate設定至{0}", _ibr), true);
                                        _BaudRate = ICOM.GetBaudRate(_ibr);
                                    }));

                                    break;
                            }
                            //reconnect

                        }
                    }
                }
            }
        }
        #endregion



        #region === Group Information & Show ===
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnButtonUpdateClick(object sender, RoutedEventArgs e) {
			this.PreProcess = PROC_READ_REGULATION;
			this.mLabelArea.Text = String.Empty;
			this.mLabelFrequncy.Text = String.Empty;
			this.mLabelFrequncyOffset.Text = String.Empty;
			this.mLabelPower.Text = String.Empty;
			this.ProcessEvent.Start();
		}

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
		private void OnListBoxMenuItemClick_Delete(object sender, RoutedEventArgs e) {
			if (this.ListBoxInfo.SelectedIndex == -1) return;
			this.ListBoxInfo.Items.Clear();
		}

        #endregion




        private void OnBorderTitleMouseLeftDown(object sender, MouseButtonEventArgs e) {
			this.DragMove();
		}


		private void OnCloseClick(object sender, RoutedEventArgs e) {
            switch (this._ConnectType)
            {
                case ReaderService.ConnectType.COM:
                    this._ICOM.CombineDataReceiveEventHandler -= combineDataEventHandler;
                    break;
                case ReaderService.ConnectType.NET:
                    this._INet.NetTCPDataReceiveEventHandler -= netTCPDataEventHandler;
                    break;
                case ReaderService.ConnectType.USB:
                    this._IUSB.USBDataReceiveEvent -= uSBDataEventHandler;
                    break;
                case ReaderService.ConnectType.BLE:
                    this._IBLE.BLEDataReceiveEvent -= bLEDataEventHandler;
                    break;
            }
            this.Close();
		}

        /// <summary>
        /// 
        /// </summary>
        /// <param name="str"></param>
        /// <param name="data"></param>
		private void DisplayText(String str, String data)
        {
            Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() => {
                if (this.ListBoxInfo.Items.Count > 1000)
                    this.ListBoxInfo.Items.Clear();
                ListBoxItem itm = new ListBoxItem() {
                    Content = String.Empty
                };
                switch (str)
                {
                    case "TX":
                        this.IsDateTimeStamp = true;
                        itm.Foreground = Brushes.SeaGreen;
                        itm.Content = String.Format(CultureInfo.CurrentCulture, "{0} [{1}] - {2}", DateTime.Now.ToString("yy/MM/dd H:mm:ss.fff", CultureInfo.CurrentCulture), str, Format.ShowCRLF(data));
                        break;
                    case "RX":
                        itm.Foreground = Brushes.DarkRed;
                        if (this.IsDateTimeStamp) {
                            if (this.IsRunning)
                                itm.Content = String.Format(CultureInfo.CurrentCulture, "{0} [{1}] - \n{2}", DateTime.Now.ToString("yy/MM/dd H:mm:ss.fff", CultureInfo.CurrentCulture), str, Format.ShowCRLF(data));
                            else
                                itm.Content = String.Format(CultureInfo.CurrentCulture, "{0} [{1}] - {2}", DateTime.Now.ToString("yy/MM/dd H:mm:ss.fff", CultureInfo.CurrentCulture), str, Format.ShowCRLF(data));
                        }
                        else {
                            if (IsReceiveData)
                                itm.Content = Format.ShowCRLF(data);
                            else
                                itm.Content = String.Format(CultureInfo.CurrentCulture, "{0}  -- {1}", Format.ShowCRLF(data), DateTime.Now.ToString("H:mm:ss.fff", CultureInfo.CurrentCulture));
                        }
                        this.IsDateTimeStamp = false;
                        break;
                }
                this.ListBoxInfo.Items.Add(itm);
                this.ListBoxInfo.ScrollIntoView(this.ListBoxInfo.Items[this.ListBoxInfo.Items.Count - 1]);
            }));

            /*ListBoxItem itm = new ListBoxItem();
			if (str == "TX") {
				this.IsDateTimeStamp = true;
				itm.Foreground = Brushes.SeaGreen;
			}
			else
				itm.Foreground = Brushes.DarkRed;

			if (this.IsDateTimeStamp)
            {
				if (str == "RX") this.IsDateTimeStamp = false;
				if (data == null)
					itm.Content = String.Format("{0} [{1}] - ", DateTime.Now.ToString("yy/MM/dd H:mm:ss.fff"), str);
				else {
                    if (str == "RX")
                    {
                        itm.Content = String.Format("{0} [{1}] - \n{2}", DateTime.Now.ToString("yy/MM/dd H:mm:ss.fff"), str, Format.ShowCRLF(data));
                    }
                    else
                        itm.Content = String.Format("{0} [{1}] - {2}", DateTime.Now.ToString("yy/MM/dd H:mm:ss.fff"), str, Format.ShowCRLF(data));
				}
			}
			else {
				if (data == null)
					itm.Content = String.Format("[{0}] - ", str);
				else
					if (IsReceiveData)
						itm.Content = Format.ShowCRLF(data);
					else
						itm.Content = String.Format("{0}  -- {1}", Format.ShowCRLF(data), DateTime.Now.ToString("H:mm:ss.fff"));
			}

			if (this.ListBoxInfo.Items.Count > 1000)
				this.ListBoxInfo.Items.Clear();

			this.ListBoxInfo.Items.Add(itm);
			this.ListBoxInfo.ScrollIntoView(this.ListBoxInfo.Items[this.ListBoxInfo.Items.Count - 1]);
			itm = null;*/
		}


        /// <summary>
        /// 
        /// </summary>
        /// <param name="s"></param>
        /// <param name="b"></param>
		private void UIUnitControl(String s, Boolean b)
        {
			this.LabelMessage.Text = s;
			this.ButtonUpdate.IsEnabled = b;
            this.ButtonSetArea.IsEnabled = b;
            this.ButtonSetFrequency.IsEnabled = b;
			this.ButtonAdiust.IsEnabled = b;
			this.ButtonSetFrequencyPlus.IsEnabled = b;
			this.ButtonSetFrequencyMinus.IsEnabled = b;
			this.ButtonSetPower.IsEnabled = b;
			this.ButtonMeasureSetFrequency.IsEnabled = b;
			this.ComboBoxArea.IsEnabled = b;
			if (!this.IsRunning)
				this.ButtonMeasureRun.IsEnabled = b;
			this.ButtonSetFrequencyReset.IsEnabled = b;
            this.ButtonSetBuadRate.IsEnabled = b;
            //this.ButtonGetGPIOConfiguration.IsEnabled = b;
            //this.ButtonGetGPIOPort.IsEnabled = b;
            //this.CheckBoxGPIOConfigur10.IsEnabled = b;
            //this.CheckBoxGPIOConfigur11.IsEnabled = b;
            //this.CheckBoxGPIOConfigur14.IsEnabled = b;
            //this.CheckBoxGPIOPin10.IsEnabled = b;
            //this.CheckBoxGPIOPin11.IsEnabled = b;
            //this.CheckBoxGPIOPin14.IsEnabled = b;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
		private bool IsAlphabetic(String s) {
			Regex r = new Regex(@"^[0-9.]+$");
			return r.IsMatch(s);
		}


		private void TextBox_PreviewTextInput(object sender, TextCompositionEventArgs e) {
			if (!IsAlphabetic(e.Text))
				e.Handled = true;
		}


		private void TextBox_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e) {
			if (e.Key == Key.Space)
				e.Handled = true;
		}

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
		private void DoRepeatWork(object sender, EventArgs e) {
            if (!this.IsReceiveData)
            {
                this.IsReceiveData = true;
                DoSendWork(this._ReaderService.CommandU(), ReaderModule.CommandType.Normal);
            }		
		}

        /// <summary>
        /// 
        /// </summary>
        /// <param name="bs"></param>
        /// <param name="t"></param>
		private void DoSendWork(Byte[] bs, ReaderModule.CommandType t) {
			if (bs != null) {
                if (!IsReceiveFake) {
                    if (t == ReaderModule.CommandType.Normal)
                        DisplayText("TX", Format.ShowCRLF(Format.BytesToString(bs)));
                    else
                        DisplayText("TX", Format.BytesToHexString(bs));
                }

                IsReceiveEvent = false;
                switch (_ConnectType)
                {
                    default:
                    case ReaderService.ConnectType.DEFAULT:
                        break;
                    case ReaderService.ConnectType.COM:
                        this._ICOM.Send(bs, t);
                        break;
                    case ReaderService.ConnectType.USB:
                        this._IUSB.Send(bs, t);
                        break;
                    case ReaderService.ConnectType.NET:
                        this._INet.Send(bs, t);
                        break;
                    case ReaderService.ConnectType.BLE:
                        this._IBLE.Send(bs, t);
                        break;
                }
			}
		}


        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private Byte[] DoReceiveWork()
        {
            Byte[] b = null;
            switch (_ConnectType)
            {
                default:
                case ReaderService.ConnectType.DEFAULT:
                    break;
                case ReaderService.ConnectType.COM:
                    b = this._ICOM.Receive();
                    break;
                case ReaderService.ConnectType.USB:
                    b = this._IUSB.Receive();
                    break;
                case ReaderService.ConnectType.NET:
                    b = this._INet.Receive();
                    break;
                case ReaderService.ConnectType.BLE:
                    b = this._IBLE.Receive();
                    break;
            }

            /*Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() => {
                ListBoxItem itm = new ListBoxItem {
                    Foreground = Brushes.DarkRed
                };

                if (t == Module.CommandType.Normal)
					itm.Content = String.Format("{0} [RX] - {1}", DateTime.Now.ToString("yy/MM/dd H:mm:ss.fff"), Format.ShowCRLF(Format.BytesToString(b)));
				else
					itm.Content = String.Format("{0} [RX] - {1}", DateTime.Now.ToString("yy/MM/dd H:mm:ss.fff"), Format.BytesToHexString(b));
				this.ListBoxInfo.Items.Add(itm);
				this.ListBoxInfo.ScrollIntoView(this.ListBoxInfo.Items[this.ListBoxInfo.Items.Count - 1]);
			}));*/
			
			return b;
		}

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
		private void DoPreProcessWork(object sender, EventArgs e)
        {
			Byte[] bs = null;
			String str0;

			switch (this.PreProcess)
            {

                case PROC_READ_REGULATION:
                    if (!IsSendPass)
                    {
                        UIUnitControl((this.Culture.IetfLanguageTag == "en-US") ? "Get frequency information." : "讀取頻率資訊..", false);
                        DoSendWork(this._ReaderService.ReadRegulation(), ReaderModule.CommandType.Normal);
                        IsSendPass = true;
                    }
                    else {
                        if (IsReceiveEvent) {
                            IsReceiveEvent = false;
                            IsSendPass = false;
                            this.ReceiveData = Format.StringToBytes(ReceiveEventData);

                            if (this.ReceiveData == null)
                            {
                                UIUnitControl((this.Culture.IetfLanguageTag == "en-US") ?
                                    "The Reader has no callback message, check and confirm the connecting status, please." :
                                    "沒有任何回覆，請確認Reader是否連上",
                                    false);
                                this.ProcessEvent.Stop();
                            }
                            else
                            {
                                if (this.ReceiveData[1] == 0x4E)
                                {
                                    switch (this.ReceiveData[3])
                                    {
                                        case 0x31: this.mLabelArea.Text = String.Format(CultureInfo.CurrentCulture, "01: US 902~928"); IRegulation = 1; break;
                                        case 0x32: this.mLabelArea.Text = String.Format(CultureInfo.CurrentCulture, "02: TW 922~928"); IRegulation = 2; break;
                                        case 0x33: this.mLabelArea.Text = String.Format(CultureInfo.CurrentCulture, "03: CN 920~925"); IRegulation = 3; break;
                                        case 0x34: this.mLabelArea.Text = String.Format(CultureInfo.CurrentCulture, "04: CN2 840~845"); IRegulation = 4; break;
                                        case 0x35: this.mLabelArea.Text = String.Format(CultureInfo.CurrentCulture, "05: EU 865~868"); IRegulation = 5; break;
                                        case 0x36: this.mLabelArea.Text = String.Format(CultureInfo.CurrentCulture, "06: JP 916~921"); IRegulation = 6; break;
                                        case 0x37: this.mLabelArea.Text = String.Format(CultureInfo.CurrentCulture, "07: KR 917~921"); IRegulation = 7; break;
                                        case 0x38: this.mLabelArea.Text = String.Format(CultureInfo.CurrentCulture, "08: VN 918~923"); IRegulation = 8; break;
                                        case 0x39: this.mLabelArea.Text = String.Format(CultureInfo.CurrentCulture, "09: EU2 916~920"); IRegulation = 9; break;
                                        case 0x41: this.mLabelArea.Text = String.Format(CultureInfo.CurrentCulture, "0A: IN 865~867"); IRegulation = 10; break;
                                    }
                                    this.ComboBoxArea.SelectedIndex = IRegulation - 1;
                                    ComboBoxSetAreaChanged(this.ComboBoxArea.SelectedIndex);
                                    this.PreProcess = PROC_READ_MODE_AND_CHANNEL;
                                }
                                else
                                {
                                    UIUnitControl((this.Culture.IetfLanguageTag == "en-US") ? "Error callback." : "錯誤回覆", true);
                                    this.ProcessEvent.Stop();
                                }
                            }
                        }
                    }
					break;

				case PROC_READ_MODE_AND_CHANNEL:
                    if (!IsSendPass) {
                        UIUnitControl((this.Culture.IetfLanguageTag == "en-US") ? "Get the regulation mode and channel." : "讀取頻率模式和頻道..", false);
                        switch (this._Version)
                        {
                            case ReaderModule.Version.FIR300AC1:
                            case ReaderModule.Version.FIR300TD1:
                                //DoSendWork(this._ReaderService.Command_AA("FF04008702"), Module.CommandType.AA);
                                //this.ReceiveData = DoReceiveWork(Module.CommandType.AA);
                                UIUnitControl((this.Culture.IetfLanguageTag == "en-US") ? "Deprecated AA command." : "取消了AA指令集..", false);
                                this.ProcessEvent.Stop();
                                break;
                            case ReaderModule.Version.FIR300AC2:
                            case ReaderModule.Version.FIR300AC2C4:
                            case ReaderModule.Version.FIR300AC3:
                            case ReaderModule.Version.FIR300TD2:
                            case ReaderModule.Version.FIR300TD204:
                            case ReaderModule.Version.FIR300S:
                            case ReaderModule.Version.FIA300S:
                            case ReaderModule.Version.FIR300AC2C5:
                            case ReaderModule.Version.FIR300AC2C6:
                            case ReaderModule.Version.FIR300AC3C5:
                            case ReaderModule.Version.FIR300TD205:
                            case ReaderModule.Version.FIR300TD206:
                            case ReaderModule.Version.FIR300SD305:
                            case ReaderModule.Version.FIR300SD306:
                            //case Module.Version.FI_R300V_D405:
                            case ReaderModule.Version.FIR300VD406:
                            case ReaderModule.Version.FIR300AH:
                            case ReaderModule.Version.FIR300SH:
                            case ReaderModule.Version.FIR300TH:
                            case ReaderModule.Version.FIR300VH:
                                DoSendWork(this._ReaderService.ReadModeandChannel(), ReaderModule.CommandType.Normal);
                                //bs = DoReceiveWork(Module.CommandType.Normal);
                                //this.ReceiveData = Format.HexStringToBytes(Format.RemoveCRLF(Format.BytesToString(bs)));
                                break;
                        }
                        IsSendPass = true;
                    }
                    else {
                        if (IsReceiveEvent) {
                            IsReceiveEvent = false;
                            IsSendPass = false;
                            this.ReceiveData = Format.HexStringToBytes(Format.RemoveCRLF(ReceiveEventData));

                            if (this.ReceiveData[0] == 0xFF || this.ReceiveData[0] == 0x0)
                                this.mLabelFrequncy.Text = String.Format(CultureInfo.CurrentCulture, "hopping");
                            else
                            {
                                String s = null, m;
                                Double j;
                                Int32 i = (this.ReceiveData[1] > 0) ? this.ReceiveData[1] - 1 : this.ReceiveData[1];

                                switch (this.IRegulation)
                                {
                                    case 1:
                                        j = ReaderModule.RegulationFrequency(ReaderModule.Regulation.US, i);
                                        s = String.Format(CultureInfo.CurrentCulture, "{0}MHz", j.ToString("###.00", CultureInfo.CurrentCulture));
                                        break;
                                    case 2:
                                        j = ReaderModule.RegulationFrequency(ReaderModule.Regulation.TW, i);
                                        s = String.Format(CultureInfo.CurrentCulture, "{0}MHz", j.ToString("###.00", CultureInfo.CurrentCulture));
                                        break;
                                    case 3:
                                        j = ReaderModule.RegulationFrequency(ReaderModule.Regulation.CN, i);
                                        s = String.Format(CultureInfo.CurrentCulture, "{0}MHz", j.ToString("###.000", CultureInfo.CurrentCulture));
                                        break;
                                    case 4:
                                        j = ReaderModule.RegulationFrequency(ReaderModule.Regulation.CN2, i);
                                        s = String.Format(CultureInfo.CurrentCulture, "{0}MHz", j.ToString("###.000", CultureInfo.CurrentCulture));
                                        break;
                                    case 5:
                                        j = ReaderModule.RegulationFrequency(ReaderModule.Regulation.EU, i);
                                        s = String.Format(CultureInfo.CurrentCulture, "{0}MHz", j.ToString("###.00", CultureInfo.CurrentCulture));
                                        break;
                                    case 6:
                                        j = ReaderModule.RegulationFrequency(ReaderModule.Regulation.JP, i);
                                        s = String.Format(CultureInfo.CurrentCulture, "{0}MHz", j.ToString("###.00", CultureInfo.CurrentCulture));
                                        break;
                                    case 7:
                                        j = ReaderModule.RegulationFrequency(ReaderModule.Regulation.KR, i);
                                        s = String.Format(CultureInfo.CurrentCulture, "{0}MHz", j.ToString("###.00", CultureInfo.CurrentCulture));
                                        break;
                                    case 8:
                                        j = ReaderModule.RegulationFrequency(ReaderModule.Regulation.VN, i);
                                        s = String.Format(CultureInfo.CurrentCulture, "{0}MHz", j.ToString("###.00", CultureInfo.CurrentCulture));
                                        break;
                                    case 9:
                                        j = ReaderModule.RegulationFrequency(ReaderModule.Regulation.EU2, i);
                                        s = String.Format(CultureInfo.CurrentCulture, "{0}MHz", j.ToString("###.00", CultureInfo.CurrentCulture));
                                        break;
                                    case 10:
                                        j = ReaderModule.RegulationFrequency(ReaderModule.Regulation.IN, i);
                                        s = String.Format(CultureInfo.CurrentCulture, "{0}MHz", j.ToString("###.00", CultureInfo.CurrentCulture));
                                        break;
                                }

                                if (this.ReceiveData[0] == 0x01)
                                {
                                    m = "Carry";
                                    BasebandCarryMode.IsChecked = true;
                                }
                                else
                                {
                                    m = "RX";
                                    BasebandRXMode.IsChecked = true;
                                }
                                this.mLabelFrequncy.Text = String.Format(CultureInfo.CurrentCulture, "Fix mode, {0} Freq. = {1}", m, s);
                                this.ComboBoxMeasureFrequency.SelectedIndex = i;
                            }

                            this.PreProcess = PROC_READ_FREQ_OFFSET;
                        }
                    }
					break;

				case PROC_READ_FREQ_OFFSET:
                    if (!IsSendPass) {
                        UIUnitControl((this.Culture.IetfLanguageTag == "en-US") ? "Get the Reader frequency offset.." : "讀取Reader頻率Offset..", false);
                        switch (this._Version)
                        {
                            case ReaderModule.Version.FIR300AC1:
                            case ReaderModule.Version.FIR300TD1:
                                //DoSendWork(this._ReaderService.Command_AA("FF04008903"), Module.CommandType.AA);
                                //this.ReceiveData = DoReceiveWork(Module.CommandType.AA);
                                UIUnitControl((this.Culture.IetfLanguageTag == "en-US") ? "Deprecated AA command." : "取消了AA指令集..", false);
                                this.ProcessEvent.Stop();
                                break;
                            case ReaderModule.Version.FIR300AC2:
                            case ReaderModule.Version.FIR300AC2C4:
                            case ReaderModule.Version.FIR300AC3:
                            case ReaderModule.Version.FIR300TD2:
                            case ReaderModule.Version.FIR300TD204:
                            case ReaderModule.Version.FIR300S:
                            case ReaderModule.Version.FIA300S:
                            case ReaderModule.Version.FIR300AC2C5:
                            case ReaderModule.Version.FIR300AC2C6:
                            case ReaderModule.Version.FIR300AC3C5:
                            case ReaderModule.Version.FIR300TD205:
                            case ReaderModule.Version.FIR300TD206:
                            case ReaderModule.Version.FIR300SD305:
                            case ReaderModule.Version.FIR300SD306:
                            //case Module.Version.FI_R300V_D405:
                            case ReaderModule.Version.FIR300VD406:
                            case ReaderModule.Version.FIR300AH:
                            case ReaderModule.Version.FIR300SH:
                            case ReaderModule.Version.FIR300TH:
                            case ReaderModule.Version.FIR300VH:
                                DoSendWork(this._ReaderService.ReadFrequencyOffset(), ReaderModule.CommandType.Normal);
                                //bs = DoReceiveWork(Module.CommandType.Normal);
                                //this.ReceiveData = Format.HexStringToBytes(Format.RemoveCRLF(Format.BytesToString(bs)));
                                break;
                        }
                        IsSendPass = true;
                    }
                    else {

                        if (IsReceiveEvent) {
                            IsReceiveEvent = false;
                            IsSendPass = false;
                            this.ReceiveData = Format.HexStringToBytes(Format.RemoveCRLF(ReceiveEventData));

                            if (this.ReceiveData[0] > 0x01)
                                this.mLabelFrequncyOffset.Text = String.Format(CultureInfo.CurrentCulture, "N/A");
                            else
                            {
                                String strSymbol = (ReceiveData[0] == 0x00) ? "-" : "+";
                                Int32 ii = ((ReceiveData[1] << 8) & 0xFF00) + (ReceiveData[2] & 0xFF);
                                double db = (double)ii * (double)30.5;
                                mLabelFrequncyOffset.Text = String.Format(CultureInfo.CurrentCulture, "{0}{1}Hz", strSymbol, db.ToString(CultureInfo.CurrentCulture));

                            }
                            this.PreProcess = PROC_READ_POWER;
                        }

                    }
					break;

				case PROC_READ_POWER:
                    if (!IsSendPass)
                    {
                        UIUnitControl((this.Culture.IetfLanguageTag == "en-US") ? "Get the Reader power.." : "讀取Reader功率..", false);
                        DoSendWork(this._ReaderService.ReadPower(), ReaderModule.CommandType.Normal);
                        IsSendPass = true;
                    }
                    else {
                        if (IsReceiveEvent) {
                            IsReceiveEvent = false;
                            IsSendPass = false;
                            this.ReceiveData = Format.StringToBytes(ReceiveEventData);

                            if (ReceiveData[0] == 0xFF)
                                this.mLabelPower.Text = String.Format(CultureInfo.CurrentCulture, "N/A");
                            else
                            {
                                if (ReceiveData[1] == 0x4E)
                                {
                                    byte[] b = new byte[] { ReceiveData[2], ReceiveData[3] };
                                    String str = Encoding.ASCII.GetString(b);

                                    switch (this._Version)
                                    {
                                        case ReaderModule.Version.FIR300TD1:
                                        case ReaderModule.Version.FIR300TD2:
                                        case ReaderModule.Version.FIR300TD204:
                                        case ReaderModule.Version.FIR300TD205:
                                        case ReaderModule.Version.FIR300TD206:
                                        case ReaderModule.Version.FIR300TH:
                                            if (Format.HexStringToByte(str) >= 0x1B)
                                            {
                                                this.mLabelPower.Text = String.Format(CultureInfo.CurrentCulture, "25dBm");
                                                this.ComboboxPower.SelectedIndex = 0;
                                            }
                                            else
                                            {
                                                this.mLabelPower.Text = String.Format(CultureInfo.CurrentCulture, "{0}dBm", Format.HexStringToByte(str) - 2);
                                                this.ComboboxPower.SelectedIndex = 25 - (Format.HexStringToByte(str) - 2);
                                            }
                                            break;
                                        case ReaderModule.Version.FIR300AC1:
                                        case ReaderModule.Version.FIR300AC2:
                                        case ReaderModule.Version.FIR300AC2C4:
                                        case ReaderModule.Version.FIR300AC3:
                                        case ReaderModule.Version.FIR300AC2C5:
                                        case ReaderModule.Version.FIR300AC2C6:
                                        case ReaderModule.Version.FIR300AC3C5:
                                        case ReaderModule.Version.FIR300AH:
                                            if (Format.HexStringToByte(str) >= 0x14)
                                            {
                                                this.mLabelPower.Text = String.Format(CultureInfo.CurrentCulture, "18dBm");
                                                this.ComboboxPower.SelectedIndex = 0;
                                            }
                                            else
                                            {
                                                this.mLabelPower.Text = String.Format(CultureInfo.CurrentCulture, "{0}dBm", Format.HexStringToByte(str) - 2);
                                                this.ComboboxPower.SelectedIndex = 18 - (Format.HexStringToByte(str) - 2);
                                            }
                                            break;
                                        case ReaderModule.Version.FIR300S:
                                        case ReaderModule.Version.FIA300S:
                                        case ReaderModule.Version.FIR300SD305:
                                        case ReaderModule.Version.FIR300SD306:
                                        case ReaderModule.Version.FIR300SH:
                                            if (Format.HexStringToByte(str) >= 0x1B)
                                            {
                                                this.mLabelPower.Text = String.Format(CultureInfo.CurrentCulture, "27dBm");
                                                this.ComboboxPower.SelectedIndex = 0;
                                            }
                                            else
                                            {
                                                this.mLabelPower.Text = String.Format(CultureInfo.CurrentCulture, "{0}dBm", Format.HexStringToByte(str));
                                                this.ComboboxPower.SelectedIndex = 27 - Format.HexStringToByte(str);
                                            }
                                            break;
                                        //case Module.Version.FI_R300V_D405:
                                        case ReaderModule.Version.FIR300VD406:
                                        case ReaderModule.Version.FIR300VH:
                                            if (Format.HexStringToByte(str) >= 0x1B)
                                            {
                                                this.mLabelPower.Text = String.Format(CultureInfo.CurrentCulture, "29dBm");
                                                this.ComboboxPower.SelectedIndex = 0;
                                            }
                                            else
                                            {
                                                this.mLabelPower.Text = String.Format(CultureInfo.CurrentCulture, "{0}dBm", Format.HexStringToByte(str) + 2);
                                                this.ComboboxPower.SelectedIndex = 27 - Format.HexStringToByte(str);
                                            }
                                            break;
                                        case ReaderModule.Version.FIRXXXX:
                                            this.mLabelPower.Text = String.Format(CultureInfo.CurrentCulture, "N/A");
                                            break;
                                    }
                                }
                            }

                            this.ProcessEvent.Stop();
                            UIUnitControl("", true);
                        }
                    }   
				
                    break;

                case PROC_READ_FREQ:
				case PROC_FREQ_SET_RESET:
					if (!IsReset)
                    {
						str0 = null;
						Double d0 = 0.0;

						switch (this._Version)
                        {
							case ReaderModule.Version.FIR300AC1:
							case ReaderModule.Version.FIR300TD1:
								DoSendWork(this._ReaderService.CommandAA("FF04008903"), ReaderModule.CommandType.AA);
								this.ReceiveData = DoReceiveWork();
								break;
							case ReaderModule.Version.FIR300AC2:
                            case ReaderModule.Version.FIR300AC2C4:
                            case ReaderModule.Version.FIR300AC3:
                            case ReaderModule.Version.FIR300TD2:
                            case ReaderModule.Version.FIR300TD204:
                            case ReaderModule.Version.FIR300S:
                            case ReaderModule.Version.FIA300S:
                            case ReaderModule.Version.FIR300TD205:
                            case ReaderModule.Version.FIR300TD206:
                            case ReaderModule.Version.FIR300AC2C5:
                            case ReaderModule.Version.FIR300AC2C6:
                            case ReaderModule.Version.FIR300AC3C5:
                            case ReaderModule.Version.FIR300SD305:
                            case ReaderModule.Version.FIR300SD306:
                            //case Module.Version.FI_R300V_D405:
                            case ReaderModule.Version.FIR300VD406:
                            case ReaderModule.Version.FIR300AH:
                            case ReaderModule.Version.FIR300SH:
                            case ReaderModule.Version.FIR300TH:
                            case ReaderModule.Version.FIR300VH:
                                DoSendWork(this._ReaderService.ReadFrequencyOffset(), ReaderModule.CommandType.Normal);
								bs = DoReceiveWork();
								this.ReceiveData = Format.HexStringToBytes(Format.RemoveCRLF(Format.BytesToString(bs)));
								break;
						}
						if (ReceiveData == null) {
							UIUnitControl((this.Culture.IetfLanguageTag == "en-US") ?
								"The Reader has no callback message, check and confirm the connecting status, please." :
								"沒有任何回覆，確認Reader是否連上",
								false);
							this.ProcessEvent.Stop();
							return;
						}

						if (IsPlus | IsMinus | IsAdjust)
                        {
							str0 = (ReceiveData[0] > 0x01) ? "{N/A}" : (ReceiveData[0] == 0x01) ? "+" : "-";
							if (ReceiveData[0] == 0xFF) {
								if (ReceiveData[1] == 0xFF) ReceiveData[1] = 0;
								if (ReceiveData[2] == 0xFF) ReceiveData[2] = 0;
							}
							d0 = ((ReceiveData[1] << 8) + ReceiveData[2]) * 30.5;
						}

						if (IsPlus)
                        {
							Int32 value = Int32.Parse(ComboboxStep.SelectedValue.ToString(), CultureInfo.CurrentCulture);
                            Int32 b = (Int32)((ReceiveData[1] << 8) + ReceiveData[2]);
							if (ReceiveData[0] > 0x0) { value += b; ReceiveData[0] = 0x1; }
							else {
								if (value > b) { value = value - b; ReceiveData[0] = 0x1; }
								else value = b - value;
							}
							ReceiveData[1] = (byte)(value >> 8);
							ReceiveData[2] = (byte)(value & 0xFF);

							String str1 = (ReceiveData[0] > 0) ? "+" : "-";
							Double d1 = ((ReceiveData[1] << 8) + ReceiveData[2]) * 30.5;
							UIUnitControl((this.Culture.IetfLanguageTag == "en-US") ?
								String.Format(CultureInfo.CurrentCulture, "Offset freq. {0}{1}Hz，adjust to {2}{3}Hz，and waiting for reboot.", str0, d0, str1, d1) :
								String.Format(CultureInfo.CurrentCulture, "目前offset頻率 {0}{1}Hz，調整為 {2}{3}Hz，並等候Reader重啟", str0, d0, str1, d1), false);
						}
						else if (IsMinus)
                        {
                            Int32 value = Int32.Parse(ComboboxStep.SelectedValue.ToString(), CultureInfo.CurrentCulture);
                            Int32 b = (Int32)((ReceiveData[1] << 8) + ReceiveData[2]);
							if (ReceiveData[0] > 0x0) {
								if (value > b) { value = value - b; ReceiveData[0] = 0x0; }
								else value = b - value; }
							else { value += b; ReceiveData[0] = 0x0; }
							ReceiveData[1] = (byte)(value >> 8);
							ReceiveData[2] = (byte)(value & 0xFF);

							String str2 = (ReceiveData[0] > 0) ? "+" : "-";
							Double d2 = ((ReceiveData[1] << 8) + ReceiveData[2]) * 30.5;
							UIUnitControl((this.Culture.IetfLanguageTag == "en-US") ?
								String.Format(CultureInfo.CurrentCulture, "Offset freq. {0}{1}Hz，adjust to {2}{3}Hz，and waiting for reboot.", str0, d0, str2, d2) :
								String.Format(CultureInfo.CurrentCulture ,"目前offset頻率 {0}{1}Hz，調整為 {2}{3}Hz，並等候Reader重啟", str0, d0, str2, d2), false);
						}
						else if (IsAdjust)
                        {
							Double fc, fm, fb, tf = 0;
							if (ReceiveData[0] == 0x00)//-
								fm = double.Parse(TextBoxMeasureFrequency.GetLineText(0), CultureInfo.CurrentCulture) * 1000000 + ((ReceiveData[1] << 8) + ReceiveData[2]) * 30.5;
							else
								fm = double.Parse(TextBoxMeasureFrequency.GetLineText(0), CultureInfo.CurrentCulture) * 1000000 - ((ReceiveData[1] << 8) + ReceiveData[2]) * 30.5;
                            Int32 idx = ComboBoxFrequency.SelectedIndex;

							switch (IRegulation)
                            {
								case 1: tf = ReaderModule.RegulationFrequency(ReaderModule.Regulation.US, idx); break;
								case 2: tf = ReaderModule.RegulationFrequency(ReaderModule.Regulation.TW, idx); break;
								case 3: tf = ReaderModule.RegulationFrequency(ReaderModule.Regulation.CN, idx); break;
								case 4: tf = ReaderModule.RegulationFrequency(ReaderModule.Regulation.CN2, idx); break;
								case 5: tf = ReaderModule.RegulationFrequency(ReaderModule.Regulation.EU, idx); break;
                                case 6: tf = ReaderModule.RegulationFrequency(ReaderModule.Regulation.JP, idx); break;
                                case 7: tf = ReaderModule.RegulationFrequency(ReaderModule.Regulation.KR, idx); break;
                                case 8: tf = ReaderModule.RegulationFrequency(ReaderModule.Regulation.VN, idx); break;
                                case 9: tf = ReaderModule.RegulationFrequency(ReaderModule.Regulation.EU2, idx); break;
                                case 10: tf = ReaderModule.RegulationFrequency(ReaderModule.Regulation.IN, idx); break;
                            }
							fc = tf * 1000000;
							fb = fc - fm;
							if (fb <= 0) {
								fb = fm - fc;
								IsSymbol = false;
							}
							else
								IsSymbol = true;

							ReceiveData[1] = (Byte)(((Int32)(fb / 30.5) >> 8) & 0xFF);
							ReceiveData[2] = (Byte)((Int32)(fb / 30.5) & 0xFF);
							//this.PreProcess = PROC_READ_FREQ_CALLBACK;
							//this.ProcessEvent.Start();
							UIUnitControl((this.Culture.IetfLanguageTag == "en-US") ?
                                String.Format(CultureInfo.CurrentCulture, "Adjust freq. to {0}Hz，and waiting for reboot.", fm) :
                                String.Format(CultureInfo.CurrentCulture, "修正頻率至{0}, 並等待Reader重啟", fm),
								false);
						}
					}
					else
                    {
						UIUnitControl((this.Culture.IetfLanguageTag == "en-US") ? "Reset offset frequency, and waiting 2s for reboot." : "重置offset頻率，並等候Reader重啟(2s)", false);
                        //J000(reset) to set frequency (2017/4/18)
                        IsSetFrequency = true;
                        DoSendWork(this._ReaderService.CommandJ("0", "00"), ReaderModule.CommandType.Normal);
                        DoReceiveWork();
                        ComboBoxMeasureFrequency.SelectedIndex = ReaderModule.RegulationChannel(IRegulation);
                    }
					this.PreProcess = PROC_SET_FREQ_H;
					break;

				case PROC_SET_FREQ_H:
					switch (this._Version)
                    {
						case ReaderModule.Version.FIR300AC1:
						case ReaderModule.Version.FIR300TD1:
							str0 = String.Format(CultureInfo.CurrentCulture, "FF05008A{0}", (IsReset) ? "FF" : Format.ByteToHexString(ReceiveData[1]));
							DoSendWork(this._ReaderService.CommandAA(str0), ReaderModule.CommandType.AA);
							DoReceiveWork();
							break;
						case ReaderModule.Version.FIR300AC2:
                        case ReaderModule.Version.FIR300AC2C4:
                        case ReaderModule.Version.FIR300AC3:
                        case ReaderModule.Version.FIR300TD2:
                        case ReaderModule.Version.FIR300TD204:
                        case ReaderModule.Version.FIR300S:
                        case ReaderModule.Version.FIA300S:
                        case ReaderModule.Version.FIR300TD205:
                        case ReaderModule.Version.FIR300TD206:
                        case ReaderModule.Version.FIR300AC2C5:
                        case ReaderModule.Version.FIR300AC2C6:
                        case ReaderModule.Version.FIR300AC3C5:
                        case ReaderModule.Version.FIR300SD305:
                        case ReaderModule.Version.FIR300SD306:
                        //case Module.Version.FI_R300V_D405:
                        case ReaderModule.Version.FIR300VD406:
                        case ReaderModule.Version.FIR300AH:
                        case ReaderModule.Version.FIR300SH:
                        case ReaderModule.Version.FIR300TH:
                        case ReaderModule.Version.FIR300VH:
                            str0 = (IsReset) ? "FF" : Format.ByteToHexString(ReceiveData[1]);
							DoSendWork(this._ReaderService.SetFrequencyAddrH(str0), ReaderModule.CommandType.Normal);
							DoReceiveWork();
							break;
					}			
					this.PreProcess = PROC_SET_FREQ_L;
					break;

				case PROC_SET_FREQ_L:
					switch (this._Version) {
						case ReaderModule.Version.FIR300AC1:
						case ReaderModule.Version.FIR300TD1:
							str0 = string.Format(CultureInfo.CurrentCulture, "FF05008B{0}", (IsReset) ? "FF" : Format.ByteToHexString(ReceiveData[2]));
							DoSendWork(this._ReaderService.CommandAA(str0), ReaderModule.CommandType.AA);
							DoReceiveWork();
							break;
                        case ReaderModule.Version.FIR300AC2:
                        case ReaderModule.Version.FIR300AC2C4:
                        case ReaderModule.Version.FIR300AC3:
                        case ReaderModule.Version.FIR300TD2:
                        case ReaderModule.Version.FIR300TD204:
                        case ReaderModule.Version.FIR300S:
                        case ReaderModule.Version.FIA300S:
                        case ReaderModule.Version.FIR300TD205:
                        case ReaderModule.Version.FIR300TD206:
                        case ReaderModule.Version.FIR300AC2C5:
                        case ReaderModule.Version.FIR300AC2C6:
                        case ReaderModule.Version.FIR300AC3C5:
                        case ReaderModule.Version.FIR300SD305:
                        case ReaderModule.Version.FIR300SD306:
                        //case Module.Version.FI_R300V_D405:
                        case ReaderModule.Version.FIR300VD406:
                        case ReaderModule.Version.FIR300AH:
                        case ReaderModule.Version.FIR300SH:
                        case ReaderModule.Version.FIR300TH:
                        case ReaderModule.Version.FIR300VH:
                            str0 = (IsReset) ? "FF" : Format.ByteToHexString(ReceiveData[2]);
							DoSendWork(this._ReaderService.SetFrequencyAddrL(str0), ReaderModule.CommandType.Normal);
							DoReceiveWork();
							break;
					}			
					
					this.PreProcess = PROC_SET_FREQ;
					break;

				case PROC_SET_FREQ:
					switch (this._Version) {
						case ReaderModule.Version.FIR300AC1:
						case ReaderModule.Version.FIR300TD1:
							str0 = string.Format(CultureInfo.CurrentCulture, "FF060089{0}", (IsReset) ? "FF" :
															(IsPlus | IsMinus) ? Format.ByteToHexString(ReceiveData[0]) :
																IsSymbol ? "01" : "00");	
							this.IsPlus = false;
							this.IsMinus = false;
							this.IsReset = false;
							if (this.IsAdjust) {
								this.TextBoxMeasureFrequency.Text = String.Empty;
								this.IsAdjust = false;
							}
							DoSendWork(this._ReaderService.CommandAA(str0), ReaderModule.CommandType.AA);
							DoReceiveWork();
							break;
                        case ReaderModule.Version.FIR300AC2:
                        case ReaderModule.Version.FIR300AC2C4:
                        case ReaderModule.Version.FIR300AC3:
                        case ReaderModule.Version.FIR300TD2:
                        case ReaderModule.Version.FIR300TD204:
                        case ReaderModule.Version.FIR300S:
                        case ReaderModule.Version.FIA300S:
                        case ReaderModule.Version.FIR300TD205:
                        case ReaderModule.Version.FIR300TD206:
                        case ReaderModule.Version.FIR300AC2C5:
                        case ReaderModule.Version.FIR300AC2C6:
                        case ReaderModule.Version.FIR300AC3C5:
                        case ReaderModule.Version.FIR300SD305:
                        case ReaderModule.Version.FIR300SD306:
                        //case Module.Version.FI_R300V_D405:
                        case ReaderModule.Version.FIR300VD406:
                        case ReaderModule.Version.FIR300AH:
                        case ReaderModule.Version.FIR300SH:
                        case ReaderModule.Version.FIR300TH:
                        case ReaderModule.Version.FIR300VH:
                            str0 = (IsReset) ? "FF" : (IsPlus | IsMinus) ? Format.ByteToHexString(ReceiveData[0]) : IsSymbol ? "01" : "00";
							this.IsPlus = false;
							this.IsMinus = false;
							this.IsReset = false;
							if (this.IsAdjust) {
								this.TextBoxMeasureFrequency.Text = String.Empty;
								this.IsAdjust = false;
							}
							DoSendWork(this._ReaderService.SetFrequency(str0), ReaderModule.CommandType.Normal);
							DoReceiveWork();
							break;
					}		
					this.PreProcess = PROC_SET_RESET;
					break;

				case PROC_SET_MEASURE_FREQ:
					Thread.Sleep(1000);
					OnButtonMeasureSetFrequencyClick(null, null);
					this.ProcessEvent.Stop();
					break;

				case PROC_SET_RESET:
					Thread.Sleep(2000);
					//DoReceiveWork(Module.CommandType.Normal);					
					UIUnitControl((this.Culture.IetfLanguageTag == "en-US") ?
						"The Reader has reboot." : 
						"Reader已重啟完成", 
						true);
					this.ProcessEvent.Stop();
					break;
			}
		}


        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
		private void DoReceiveDataWork(object sender, CombineDataReceiveArgumentEventArgs e)
        {
			String s_crlf = e.Data;
            /*switch (DoProcess)
            {
                case CommandStatus.DEFAULT:
                    break;
                case CommandStatus.TESTRUN:

                    DisplayText("RX", s_crlf);
                    if (s_crlf.Equals(ReaderService.COMMANDU_END))
                    {
                        if(!IsRunning)
                            DoProcess = CommandStatus.DEFAULT;
                        this.IsReceiveData = false;
                    } 
                    break;
            } */
            
            if (this.IsRunning)
            {
                if (s_crlf.Equals(ReaderService.CommandEndU, StringComparison.CurrentCulture)) this.IsReceiveData = false;
                DisplayText("RX", s_crlf);
            }
            else
            {
                ReceiveEventData = s_crlf;
                IsReceiveEvent = true;
                if (!IsReceiveFake)
                    DisplayText("RX", s_crlf);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DoReceiveDataWork(object sender, USBDataReceiveEventArgs e)
        {
            if (e.Status == ReadStatus.Success)
            {
                String s_crlf = e.Data;
                
                if (this.IsRunning)
                {
                    if (s_crlf.Equals(ReaderService.CommandEndU, StringComparison.CurrentCulture)) this.IsReceiveData = false;
                    DisplayText("RX", s_crlf);

                }
                else {
                    ReceiveEventData = s_crlf;
                    IsReceiveEvent = true;
                    if (!IsReceiveFake)
                        DisplayText("RX", s_crlf);
                }
            }
            else if (e.Status == ReadStatus.WaitTimedOut)
            {
            }

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DoReceiveDataWork(object sender, BLEDataReceiveEventArgs e)
        {
            if (e.Status == ReadStatus.Success)
            {
                String s_crlf = e.Data;

                if (this.IsRunning)
                {
                    if (s_crlf.Equals(ReaderService.CommandEndU, StringComparison.CurrentCulture)) this.IsReceiveData = false;
                    DisplayText("RX", s_crlf);

                }
                else
                {
                    ReceiveEventData = s_crlf;
                    IsReceiveEvent = true;
                    if (!IsReceiveFake)
                        DisplayText("RX", s_crlf);
                }
            }
            else if (e.Status == ReadStatus.WaitTimedOut)
            {
            }

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
		private void DoReceiveDataWork(object sender, NETDataReceiveEventArgs e)
        {
            if (e.Status == ReadStatus.Success)
            {
                String s_crlf = e.Data;
                
                if (this.IsRunning)
                {
                    if (s_crlf.Equals(ReaderService.CommandEndU, StringComparison.CurrentCulture)) this.IsReceiveData = false;
                    DisplayText("RX", s_crlf);

                }
                else
                {
                    ReceiveEventData = s_crlf;
                    IsReceiveEvent = true;
                    if (!IsReceiveFake)
                        DisplayText("RX", s_crlf);
                }
            }
            else if (e.Status == ReadStatus.WaitTimedOut)
            {
            }
        }

        private void OnComboBoxAreaDownClosed(object sender, EventArgs e)
        {
            VM.ComboBoxAreaSelectedIndex = (sender as ComboBox).SelectedIndex;
        }

        private void OnComboBoxBaudRateDownClosed(object sender, EventArgs e)
        {
            //VM.ComboBoxBaudRateSelectedIndex = (sender as ComboBox).SelectedIndex;
        }

        #region IDisposable Support
        private bool disposedValue = false; // 偵測多餘的呼叫

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: 處置受控狀態 (受控物件)。
                    if (_ICOM != null) _ICOM.Dispose();
                    if (_INet != null) _INet.Dispose();
                }

                // TODO: 釋放非受控資源 (非受控物件) 並覆寫下方的完成項。
                // TODO: 將大型欄位設為 null。

                disposedValue = true;
            }
        }

        // TODO: 僅當上方的 Dispose(bool disposing) 具有會釋放非受控資源的程式碼時，才覆寫完成項。
        // ~RegulationDialog() {
        //   // 請勿變更這個程式碼。請將清除程式碼放入上方的 Dispose(bool disposing) 中。
        //   Dispose(false);
        // }

        // 加入這個程式碼的目的在正確實作可處置的模式。
        public void Dispose()
        {
            // 請勿變更這個程式碼。請將清除程式碼放入上方的 Dispose(bool disposing) 中。
            Dispose(true);
            // TODO: 如果上方的完成項已被覆寫，即取消下行的註解狀態。
            GC.SuppressFinalize(this);
        }
        #endregion

    }
}
