using My;
using NativeWifi;
using PacketDotNet;
using SharpPcap;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace WifiSolution.WifiWPFClient
{
  public class ClientViewModel : INotifyPropertyChanged
  {
    public ClientViewModel()
    {
      String resource_data = Properties.Resources.Dict;
      String[] rows = resource_data.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
      dict = new MyDictionary(CultureInfo.CurrentUICulture.TwoLetterISOLanguageName, rows);
      account = new WifiAccount("P", ProjectName, dict);
      if (wf.ReadFromRegistry("CEmailRemember").ToString() != "")
        Account.Email = saes.DecryptToString(wf.ReadFromRegistry("CEmailRemember").ToString());
      if (wf.ReadFromRegistry("CPassRemember").ToString() != "")
        Account.Password = saes.DecryptToString(wf.ReadFromRegistry("CPassRemember").ToString());
      if (Account.Email != "") Account.cb_EmailRememberIsChecked = true;
      if (Account.Password != "") Account.cb_PassRememberIsChecked = true;
      Account.cb_AutoLogin = (wf.ReadFromRegistry("AutoLogin").ToString() == "*");
      Account.cb_AutoConnect = (wf.ReadFromRegistry("AutoConnect").ToString() == "*");

      if (!mfn.IsAdministrator())
      {
        wf.ShowMessageBox(dict.GetMessage(10));
        wf.Shutdown();
      }
      if ((Account.Email != "") || (Account.Password != ""))
      {
        Account.tc_RegisterLoginSelectedIndex = 0;
        if ((Account.Email != "") && (Account.Password != "") && (Account.cb_AutoLogin == true))
          LoginCommand();
      }
      wc = new WifiCommon(dict, ProjectName);
    }
    private WifiAccount account;
    public WifiAccount Account
    {
      get { return account; }
      //set { account = value; }
    }
    SimpleAES saes = new SimpleAES();
    private WinFuncs wf = new WinFuncs(ProjectName);
    private WifiCommon wc;
    #region Properties
    private int waitcount = 0;
    private int Waitcount { get { return waitcount; } set { waitcount = value; OnPropertyChanged(nameof(MainGridVisibility)); } }
    public bool MainGridVisibility { get { return Waitcount == 0; } } //  ? Visibility.Visible : Visibility.Hidden; 
    #endregion
    public void ShowWait()
    {
      Waitcount++;
    }
    public void HideWait()
    {
      Waitcount--;
    }

    private void RuninThread(DoWorkEventHandler work, RunWorkerCompletedEventHandler afterThat)
    {
      ShowWait();
      AutoResetEvent _resetEvent = new AutoResetEvent(false);
      BackgroundWorker bw = new BackgroundWorker();
      bw.DoWork += work;
      if (afterThat != null)
        bw.RunWorkerCompleted += afterThat;// delegate (object sender, RunWorkerCompletedEventArgs e) { StartSystem(false); HideWait(); };
      bw.RunWorkerCompleted += delegate (object sender, RunWorkerCompletedEventArgs e) { HideWait(); };
      bw.RunWorkerAsync();
    }
    private void LoginCommand()
    {
      if (!Account.Logged)
        RuninThread(
          delegate (object sender, DoWorkEventArgs e)
          {
            ShowWait();
            Account.Login();
          },
          //new DoWorkEventHandler(Account.Login),
          delegate (object sender, RunWorkerCompletedEventArgs e)
          {
            if ((Account.cb_AutoConnect == true) && (Account.Logged) && (!Connected))
              Connect();
            HideWait();
          }
          ); // delegate (object sender, RunWorkerCompletedEventArgs e) { StartSystem(false); HideWait(); }
      else
        RuninThread(
          delegate (object sender, DoWorkEventArgs e)
          {
            ShowWait();
            Account.Logout();
          },
          //new DoWorkEventHandler(Account.Login),
          delegate (object sender, RunWorkerCompletedEventArgs e)
          {
            Disconnect();
            OnPropertyChanged(nameof(canConnect));
            HideWait();
          }
          ); // delegate (object sender, RunWorkerCompletedEventArgs e) { StartSystem(false); HideWait(); }

    }
    #region Commands
    private RelayCommand _bt_RefreshCommand;
    public RelayCommand bt_RefreshCommand
    {
      get
      {
        if (_bt_RefreshCommand == null)
        {
          _bt_RefreshCommand = new RelayCommand(p => RefreshCommand());
        }
        return _bt_RefreshCommand;
      }
    }

    private void RefreshCommand()
    {
      refreshNetworkListBox();
      Add2Log("Wifi listesi güncellendi");
    }

    private RelayCommand _bt_LoginCommand;
    public RelayCommand bt_LoginCommand
    {
      get
      {
        if (_bt_LoginCommand == null)
        {
          _bt_LoginCommand = new RelayCommand(p => LoginCommand());
        }
        return _bt_LoginCommand;
      }
    }
    private RelayCommand _bt_ConnectCommand;
    public RelayCommand bt_ConnectCommand
    {
      get
      {
        _bt_ConnectCommand = _bt_ConnectCommand ?? new RelayCommand(ConnectCommand); // , canConnect
        return _bt_ConnectCommand;
      }
    }
    public bool canConnect { get { return Account.Logged || Connected; } }
    private bool _connected = false;
    public bool Connected
    {
      get { return _connected; }
      set
      {
        _connected = value;
        OnPropertyChanged(nameof(bt_ConnectContent));
        OnPropertyChanged(nameof(canConnect));
      }
    }
    public string bt_ConnectContent { get { return Connected ? "Disconnect" : "Connect"; } }
    #endregion

    private const string ProjectName = "Wifi";
    string wifiprefix = "";
    private String providerIp = "";
    public String ProviderIp { get { return providerIp; } set { providerIp = value; Account.ProviderIp = providerIp; } }
    private WlanClient.WlanInterface wlanIface = (new WlanClient()).Interfaces[0];
    private string lastWifiName = "";
    int GetSetUsageInterval = 1000;
    List<WifiType> wifis = new List<WifiType>();
    //public static WifiService.Service1Client _WCF;
    public event PropertyChangedEventHandler PropertyChanged;
    private void OnPropertyChanged(string property) { PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property)); }
    List<WifiType> lb_networksItemsSource { get { return wifis.OrderByDescending(x => x.SignalQuality).ToList(); } }
      public string bt_LoginContent { get { return Account.Logged ? "Logout" : "Login"; } }
    //private long _quota = 0;
    private long _loginquota = 0;
    private long _connectionid = 0;
    public long ConnectionID
    {
      get { return _connectionid; }
      set { _connectionid = value; receivedBytes = 0; }
    }
    public long startUsage = 0;
    private string langCode = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
    private ICaptureDevice device;
    private long receivedBytes = 0;
    private MyDictionary dict;
    Thread thrGetSetUsage;
/*
    public WifiService.Service1Client WCF
    {
      get
      {
        if (_WCF == null)
          _WCF = new WifiService.Service1Client();
        return _WCF;
      }
    }
*/
    public class WifiType
    {
      public string SSIDName { get; set; }
      public uint SignalQuality { get; set; }
    }

    public string GetStringForSSID(Wlan.Dot11Ssid ssid)
    {
      return Encoding.ASCII.GetString(ssid.SSID, 0, (int)ssid.SSIDLength);
    }

    private bool inSystem()
    {
      return ((wlanIface.CurrentConnection.isState == Wlan.WlanInterfaceState.Connected)
          && (GetStringForSSID(wlanIface.CurrentConnection.wlanAssociationAttributes.dot11Ssid).StartsWith(wifiprefix))
          && (ProviderIp != "")
          && (wc.GetWebstring("http://" + ProviderIp + "/ping") == "pong"))
      ;
    }

    private string myEvidence(string Mesaj = "")
    {
      if (!Account.SetSecurityCode())
        return "";
      else
        return Account.EmailHash + (Account.SecurityCode + (Account.EmailHash + Account.Password).HashMD5() + Mesaj.HashMD5()).HashMD5() + Mesaj;
    }

    private string CurrentWifi()
    {
      string rtn = "";
      try
      {
        if (wlanIface.CurrentConnection.isState == Wlan.WlanInterfaceState.Connected)
          return GetStringForSSID(wlanIface.CurrentConnection.wlanAssociationAttributes.dot11Ssid);
      }
      catch (Exception)
      {
      }
      return rtn;
    }

    void wlanIface_WlanConnectionNotification(Wlan.WlanNotificationData notifyData, Wlan.WlanConnectionNotificationData connNotifyData)
    {
      ///* tb_Email thread içinden çalıştığı için access violation alıyoruz. Aşmanın yöntemi var ama şimdilik otomatik login i kapatıyorum...
      string _wfname = CurrentWifi();
      if (_wfname != lastWifiName)
      {
        if (lastWifiName != "")
          wifiAfterDisconnect(lastWifiName);
        lastWifiName = _wfname;
        if (_wfname != "")
          wifiAfterConnect(_wfname);
      }
    }

    private void Add2Log(string msg)
    {
      Console.WriteLine(msg);
    }

    private void wifiAfterConnect(string ssid)
    {
      Add2Log("wifiAfterConnect : Connected to " + ssid);
      //form loadda zaten tanımlanıyor... device = (SharpPcap.WinPcap.WinPcapDevice)CaptureDeviceList.Instance.FirstOrDefault(x => x.Name.ToUpper().Contains(wlanIface.InterfaceGuid.ToString().ToUpper()));// devices[devices.Count - 1];
      if (Array.IndexOf(Environment.GetCommandLineArgs(), "testercument") > -1)
        ProviderIp = "192.168.0.1";
      else
      {
        ProviderIp = "";
        if (device != null)
        {
          if (device.Started)
            device.StopCapture();
          device.Close();
        }
        CaptureDeviceList.Instance.Refresh();
        try
        {
          device = CaptureDeviceList.Instance.FirstOrDefault(x => x.Name.Contains(wlanIface.InterfaceGuid.ToString().ToUpper()));
          device.OnPacketArrival += new SharpPcap.PacketArrivalEventHandler(device_OnPacketArrival);
          device.Open(DeviceMode.Normal, 1000);
          var v = ((SharpPcap.WinPcap.WinPcapDevice)device).Addresses.First(x => x.Addr.ipAddress.ToString().Contains("."));
          if (v != null)
          {
            ProviderIp = v.Addr.ipAddress.ToString();
            ProviderIp = ProviderIp.Substring(0, ProviderIp.LastIndexOf('.')) + ".1";
            Add2Log("providerIp : " + ProviderIp);
          }
        }
        catch (Exception e)
        {
          string msg = device?.LastError;
          device = null;
          Add2Log("Yakalama sürücüsü açılamadı (" + msg + "/" + e.Message + ")");
          return; // throw new Exception // sessiz çıkayım istedim...
        }
      }
      //Dispatcher.BeginInvoke(
      //  DispatcherPriority.ContextIdle,
      //  new Action(delegate ()
      //    {
            if ((!Account.Logged) && (Account.cb_AutoLogin == true))
              Account.Login();
      //    }
      //  )
      //);
    }

    private void wifiAfterDisconnect(string ssid)
    {
      Add2Log("wifiAfterDisconnect : Disconnected from " + ssid);
      Disconnect();
    }

    private void disconnectWifi()
    {
      string _cstr = wlanIface.GetProfileXml(wlanIface.CurrentConnection.profileName);
      wlanIface.DeleteProfile(wlanIface.CurrentConnection.profileName);
      wlanIface.SetProfile(Wlan.WlanProfileFlags.AllUser, _cstr, true);
    }

    private void refreshNetworkListBox()
    {
      //      var lll = wlanIface.GetNetworkBssList(); // Bulduğun tüm modemlerin Mac adresini alabiliyorsun
      // Provider uygulaması ile network kartın Mac adresini sabit bir adres yapıp clientlarda buna göre bağlanmayı düşün. Ya da seçimlik yapabilirsin. "Daha kolay bulunmak için bu opsiyona izin verin" (zararı olur mu)
      wlanIface.Scan();
      Wlan.WlanAvailableNetwork[] networks = wlanIface.GetAvailableNetworkList(0);
      networks = networks.Where(x => GetStringForSSID(x.dot11Ssid).StartsWith(wifiprefix)).ToArray();
      foreach (var item in networks)
      {
        string name = GetStringForSSID(item.dot11Ssid);
        if ((name.StartsWith(wifiprefix)) && (wifis.Where(x => x.SSIDName == name).ToList().Count() == 0))
          wifis.Add(new WifiType() { SSIDName = GetStringForSSID(item.dot11Ssid), SignalQuality = item.wlanSignalQuality });
      }
      foreach (var item in wifis)
      {
        if (networks.Where(x => item.SSIDName == GetStringForSSID(x.dot11Ssid)).ToList().Count() == 0)
          item.SignalQuality = 0;//wifis.Remove(item);
      }
      //lb_networksItemsSource = wifis.OrderByDescending(x => x.SignalQuality);
      if ((Account.Logged) && (!Connected) && (wifis.Count > 0))
        connectWifi(wifis.OrderByDescending(x => x.SignalQuality).ToList()[0].SSIDName);
    }

    public bool connectWifi(string ssid)
    {
      string key = "";
      if (ssid == "dilek ercu")
        key = "Erci8185";
      else
        key = "erci1234";
      string mac = BitConverter.ToString(System.Text.Encoding.Default.GetBytes(ssid)).Replace("-", "");
      string profileXml = string.Format("<?xml version=\"1.0\"?><WLANProfile xmlns=\"http://www.microsoft.com/networking/WLAN/profile/v1\"><name>{0}</name><SSIDConfig><SSID><hex>{1}</hex><name>{0}</name></SSID></SSIDConfig><connectionType>ESS</connectionType><connectionMode>manual</connectionMode><MSM><security><authEncryption><authentication>WPA2PSK</authentication><encryption>AES</encryption><useOneX>false</useOneX></authEncryption><sharedKey><keyType>passPhrase</keyType><protected>false</protected><keyMaterial>{2}</keyMaterial></sharedKey><keyIndex>0</keyIndex></security></MSM></WLANProfile>", ssid, mac, key);
      wlanIface.SetProfile(Wlan.WlanProfileFlags.AllUser, profileXml, true);
      try
      {
        wlanIface.Connect(Wlan.WlanConnectionMode.Profile, Wlan.Dot11BssType.Any, ssid);
      }
      catch (Exception err)
      {
        wf.ShowMessageBox(err.Message);
      }
      return true;
    }

    void device_OnPacketArrival(object sender, CaptureEventArgs e)
    {
      string devmac = device.MacAddress.ToString();
      var packet = Packet.ParsePacket(e.Packet.LinkLayerType, e.Packet.Data);
      if (packet is EthernetPacket)
      {
        var eth = ((EthernetPacket)packet);
        string s_mac = eth.SourceHwAddress.ToString();
        string d_mac = eth.DestinationHwAddress.ToString();
        string d_ip = "", s_ip = "";
        IpPacket ip = (IpPacket)packet.Extract(typeof(IpPacket));
        ARPPacket arp = (ARPPacket)packet.Extract(typeof(ARPPacket));
        if (arp != null) return;
        if (ip != null)
        {
          d_ip = ip.DestinationAddress.ToString();
          s_ip = ip.SourceAddress.ToString();
        }
        if ((d_mac != devmac) || (d_mac == "")
          || (!d_ip.StartsWith(ProviderIp.Substring(0, ProviderIp.LastIndexOf('.'))))
          || (d_ip.EndsWith(".255"))) return;
        //        if ((d_mac == devmac) && (s_ip != providerIp))
        {
          receivedBytes += eth.Bytes.Length - eth.Header.Length;
          //Add2Log(eth.ToString());
        }
      }
    }

    private void GetSetUsage()
    {
      long tmpcid = ConnectionID;
      Add2Log("GetSetUsage thread started!");
      bool stop = false;
      while ((Connected) && (!stop) && (tmpcid == ConnectionID))
      {
        long _receivedBytes = receivedBytes;
        if (_loginquota < _receivedBytes)
          _receivedBytes = _loginquota;
        Account.Quota = _loginquota - _receivedBytes;
        //Add2Log(_loginquota.ToString("###,###,##0") + " - " + _receivedBytes.ToString("###,###,##0") + " = " + Quota.ToString("###,###,##0"));
        string result = wc.GetWebstring("http://" + ProviderIp + "/SetUsage?Message=" + Account.Email.HashMD5() + (Account.SecurityCode + (Account.Email.HashMD5() + Account.Password).HashMD5() + (_receivedBytes.ToString()).HashMD5()).HashMD5() + _receivedBytes.ToString());
        //Add2Log(result == null ? "null" : result);
        if (!wc.CheckResult(ref result)) return;
        if (result.Length == 32)
          Account.SecurityCode = result;
        if (Account.Quota == 0)
        {
          Disconnect();
          stop = true;
          //thrGetSetUsage.Abort(); Disconnect içinde var abort
          Add2Log("DISCONNECT DEDIKTEN SONRAKI SATIR");
          return;
        }
        if (!stop)
          Thread.Sleep(GetSetUsageInterval);
      }
    }

    private void ConnectCommand(object obj)
    {
      if (Connected)
        Disconnect();
      else
        Connect();
    }


    public void Disconnect()
    {
      if ((Connected) && CurrentWifi().StartsWith(wifiprefix) && (ConnectionID>0))
        wc.GetWebstring("http://" + ProviderIp + "/Disconnect?ConnectionID=" + ConnectionID.ToString());
      Connected = false;
      ConnectionID = 0;
      if ((thrGetSetUsage != null) && (thrGetSetUsage.IsAlive))
        thrGetSetUsage.Abort();

      if ((device != null) && (device.Started))
        device.StopCapture();
      Add2Log(dict.GetMessage(12));
    }

    private bool Connect()
    {
      if (!Account.Logged)
        Account.Login();
      if (Connected) return true; // AutoConnect seçili ise Login() içinden connect olmuş olabilir. 
      bool rtn = false;
      string result = wc.GetWebstring("http://" + ProviderIp + "/ConnectUS?ClientEvidence=" + myEvidence());
      if (!wc.CheckResult(ref result)) return rtn;
      long tmplong = 0;
      if ((result.Length > 33) && (result.Substring(32, 1) == ";") && (mfn.isValidHexString(result.Substring(0, 32))) && (long.TryParse(result.Substring(33), out tmplong)))
      {
        Account.SecurityCode = result.Substring(0, 32);
        ConnectionID = tmplong;
        Add2Log("Bağlantı sağlandı. ConnectionID : " + ConnectionID.ToString());
        Connected = true;
        if (!device.Started)
          device.StartCapture();
        if (thrGetSetUsage.IsAlive)
          thrGetSetUsage.Abort();
        thrGetSetUsage = new Thread(new ThreadStart(GetSetUsage));
        thrGetSetUsage.IsBackground = true;
        thrGetSetUsage.Start();
        return true;
      }
      else
        Add2Log("Bağlantı sağlanamadı. (" + result + ")");
      return rtn;
    }

    /*
              string wfname = CurrentWifi();
              if (wfname.StartsWith(wifiprefix))// && (!isSysConnected()))
                wifiAfterConnect(wfname);
              wlanIface.WlanConnectionNotification += wlanIface_WlanConnectionNotification;
              bt_Refresh.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
    */

  }
}
