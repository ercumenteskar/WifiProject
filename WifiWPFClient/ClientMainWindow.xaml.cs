using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using NativeWifi;
using System.Net;
using My;
using System.IO;
using System.Threading;
using System.Net.NetworkInformation;
using Microsoft.Win32;
using System.ComponentModel;
using System.Globalization;
using SharpPcap;
using PacketDotNet;
using System.Diagnostics;

namespace WifiWPFClient
{
  /// <summary>
  /// Interaction logic for MainWindow.xaml
  /// </summary>
  public partial class MainWindow : INotifyPropertyChanged
  {
    private const string _projectName = "Wifi";
    private const string _projectregaddr = "Software\\" + _projectName;
    string wifiprefix = "";
    string providerIp = "";
    //private string Password = "e";
    private string SecurityCode = "";
    private WlanClient.WlanInterface wlanIface = (new WlanClient()).Interfaces[0];
    private string email = "";
    public string Email { get { return email; } set { email = value; OnPropertyChanged(nameof(Email)); } }
    private string password = "";
    public string Password
    {
      get { return password; }
      set
      {
        if (password != value) password = value;
        if (GetPasswordBox(tb_Password) != value) SetPasswordBox(tb_Password, value);
      }
    }

    private string regEmail = "";
    private string regPass1 = "";
    private string regPass2 = "";
    public string RegEmail { get { return regEmail; } set { regEmail = value; OnPropertyChanged(nameof(RegEmail)); } }

    public string RegPass1
    {
      get { return regPass1; }
      set
      {
        if (regPass1 != value) regPass1 = value;
        if (GetPasswordBox(tb_RegisterPassword1) != value) SetPasswordBox(tb_RegisterPassword1, value);
      }
    }

    public string RegPass2
    {
      get { return regPass2; }
      set
      {
        if (regPass2 != value) regPass2 = value;
        if (GetPasswordBox(tb_RegisterPassword2) != value) SetPasswordBox(tb_RegisterPassword2, value);
      }
    }

    private bool? cEmailRememberisChecked = false;
    public bool? CEmailRememberisChecked
    {
      get
      {
        return cEmailRememberisChecked;
      }

      set
      {
        cEmailRememberisChecked = value;
        OnPropertyChanged(nameof(cb_CEmailRemember));
      }
    }

    private bool? cPassRememberisChecked = false;
    public bool? CPassRememberisChecked
    {
      get
      {
        return cPassRememberisChecked;
      }

      set
      {
        cPassRememberisChecked = value;
        OnPropertyChanged(nameof(cb_CPassRemember));
      }
    }


    private string EmailHash { get { return Email.HashMD5(); } }
    private string lastWifiName = "";
    int GetSetUsageInterval = 1000;
    List<WifiType> wifis = new List<WifiType>();
    public static WifiService.Service1Client _WCF;
    public event PropertyChangedEventHandler PropertyChanged;
    private void OnPropertyChanged(string property) { PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property)); }
    public string bt_LoginContent { get { return Logged ? "Logout" : "Login"; } }
    public string bt_ConnectContent { get { return Connected ? "Disconnect" : "Connect"; } }
    public bool canConnect { get { return (Logged && inSystem()) || Connected; } }
    private long _quota = 0;
    private long _loginquota = 0;
    public long Quota { get { return _quota < 0 ? 0 : _quota; } set { _quota = value; OnPropertyChanged("QuotaStr"); } }
    public string QuotaStr
    {
      get
      {
        if (Quota < 1)
          return "0";
        else if (Quota < 1024)
          return Quota.ToString("#,##0 B");
        else if (Quota < 1024 * 1024)
          return (Quota / 1024).ToString("#,##0 KB");
        else
          return (Quota / (1024 * 1024)).ToString("###,###,###,###,##0 MB");
      }
    }
    private long _connectionid = 0;
    public long ConnectionID
    {
      get { return _connectionid; }
      set { _connectionid = value; receivedBytes = 0; }
    }
    public long startUsage = 0;
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
    private bool _logged = false;
    public bool Logged
    {
      get { return _logged; }
      set
      {
        _logged = value;
        OnPropertyChanged(nameof(bt_LoginContent));
        OnPropertyChanged(nameof(canConnect));
      }
    }
    private string langCode = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
    private ICaptureDevice device;
    private long receivedBytes = 0;
    private MyDictionary dict;
    Thread thrGetSetUsage;

    public WifiService.Service1Client WCF
    {
      get
      {
        if (_WCF == null)
          _WCF = new WifiService.Service1Client();
        return _WCF;
      }
    }

    public class WifiType
    {
      public string SSIDName { get; set; }
      public uint SignalQuality { get; set; }
    }

    public MainWindow()
    {
      InitializeComponent();
    }

    public string GetStringForSSID(Wlan.Dot11Ssid ssid)
    {
      return Encoding.ASCII.GetString(ssid.SSID, 0, (int)ssid.SSIDLength);
    }

    private bool SetSecurityCode()
    {
      SecurityCode = WSRunner("/GetSecurityCode?EmailHash=" + Email.HashMD5());
      return !CheckResult(ref SecurityCode);
    }

    private void bt_Login_Click(object sender, RoutedEventArgs e)
    {
      if (Logged)
        Logout();
      else
        Login();
    }

    private void Logout()
    {
      Logged = false;
    }

    private bool inSystem()
    {
      return ((wlanIface.CurrentConnection.isState == Wlan.WlanInterfaceState.Connected)
          && (GetStringForSSID(wlanIface.CurrentConnection.wlanAssociationAttributes.dot11Ssid).StartsWith(wifiprefix))
          && (GetWebstring("http://" + providerIp + "/ping") == "pong"))
      ;
    }

    private string myEvidence(string Mesaj = "")
    {
      if (!SetSecurityCode())
        return "";
      else
        return EmailHash + (SecurityCode + (EmailHash + Password).HashMD5() + Mesaj.HashMD5()).HashMD5() + Mesaj;
    }


    private bool Login()
    {
      //MessageBox.Show("");
      bool rtn = Logged;
      if (!rtn)
      {
        string result = null;
        while (CheckResult(ref result))
        {
          if (!SetSecurityCode())
          {
            result = "¶E:Güvenlik kodu alınamadı.";
            continue;
          }
          else if (SecurityCode.Length != 32)
          {
            result = "¶E:Güvenlik kodu dönmedi. Dönen değer:" + SecurityCode;
            continue;
          }
          else
            Add2Log("SecurityCode alındı : " + SecurityCode);
          result = WSRunner("/Login?Evidence=" + myEvidence());
          if (CheckResult(ref result)) return false;
          long tmplong = 0;
          if ((result != null) && (result.Substring(32, 1) == ";") && (result.Length > 33) && (mfn.isValidHexString(result.Substring(0, 32))) && (long.TryParse(result.Substring(33), out tmplong)))
          {
            RememberAction();
            Quota = tmplong;
            _loginquota = tmplong;
            SecurityCode = result.Substring(0, 32);
            Logged = true;
            rtn = true;
          }
          else
            return false;
        }
      }
      if (rtn && (Quota > 0) && !Connected && (cb_AutoConnect.IsChecked == true) && inSystem())
        Connect();
      return rtn;
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

    void wlanIface_WlanReasonNotification(Wlan.WlanNotificationData notifyData, Wlan.WlanReasonCode reasonCode)
    {
      //Add2Log4Thread("ReasonNotification : "
      //  + Environment.NewLine + notifyData.dataSize.ToString()
      //  + Environment.NewLine + notifyData.interfaceGuid.ToString()
      //  + Environment.NewLine + notifyData.notificationCode.ToString()
      //  + Environment.NewLine + notifyData.NotificationCode.ToString()
      //  + Environment.NewLine + notifyData.notificationSource.ToString()
      //  + Environment.NewLine + reasonCode.ToString()
      //);
    }

    void wlanIface_WlanNotification(Wlan.WlanNotificationData notifyData)
    {
      // ConnectionNotification evet'ından sonra çalışıyor, aynı şeyler yazıyor (sanırım sadece success olanlar) ancak connNotifiyData parametresi yok
      // Ayrıca ConnectionNotification event'ını çalıştırmayan başka olayları da gösteriyor (SignalQualityChange, ScanComplete gibi) Liste : https://msdn.microsoft.com/en-us/library/windows/desktop/ms706902(v=vs.85).aspx
      //Add2Log4Thread("Notification : "
      //  + Environment.NewLine + notifyData.dataSize.ToString()
      //  + Environment.NewLine + notifyData.interfaceGuid.ToString()
      //  + Environment.NewLine + notifyData.notificationCode.ToString()
      //  + Environment.NewLine + notifyData.NotificationCode.ToString()
      //  + Environment.NewLine + notifyData.notificationSource.ToString()
      //);
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
      //*/
      /*
      _progress = 0;
      if ((notifyData.interfaceGuid.ToString() == wlanIface.InterfaceGuid.ToString()) && (connNotifyData.wlanReasonCode.ToString() == "Success"))
      {
        switch (notifyData.notificationCode)
        {
          case 9: _progress = 1; // ConnectionStart 9
            break;
          case 1: _progress = 2; // Associating 1
            break;
          case 2: _progress = 3; // Associated 2
            break;
          case 3: _progress = 4; // Authenticating 3 
            break;
          case 4: _progress = 5; // Connected 4 
            break;
          case 10: _progress = 6;// ConnectionComplete 10 
            break;
          default: return;
        }
      }
      */
    }

    //private void UsageStats()
    //{
    //  if (Connected)
    //  {
    //    //Thread.Sleep(1000);
    //    long _br = wlanIface.NetworkInterface.GetIPStatistics().BytesReceived - startUsage; // Usage amount since connected
    //    //Console.WriteLine("IPS: " + interfaces.FirstOrDefault(x => x.Name == wlanIface.InterfaceName).GetIPStatistics().BytesReceived.ToString("###,###,###,###") + " (" + ((interfaces.FirstOrDefault(x => x.Name == wlanIface.InterfaceName).GetIPStatistics().BytesReceived - _bytesreceived) / 1024).ToString("###,###,###") + "Kb/sn)");
    //    Console.WriteLine("IPS: " + _br.ToString("###,###,###,###") + " (" + ((_br - Usage) / 1024).ToString("###,###,###") + "Kb/sn)");
    //    Usage = _br;
    //    //Console.WriteLine("IPv4S: " + interfaces.FirstOrDefault(x => x.Name == wlanIface.InterfaceName).GetIPv4Statistics().BytesReceived.ToString("###,###,###,###"));
    //  }
    //}

    public delegate void UpdateTextCallback(string message);
    public delegate void UpdateTextCallbacktb(TextBlock tb, string str);
    public delegate void UpdateTextCallbacktxtb(TextBox tb, string str);
    public delegate void UpdateTextCallbackpassb(PasswordBox tb, string str);

    private void Add2Log(string p)
    {
      m_Log.Dispatcher.Invoke(
              new UpdateTextCallback(this._add2Log),
              new object[] { p }
          );
    }
    private void _add2Log(string p)
    {
      m_Log.AppendText(DateTime.Now.ToShortTimeString() + " " + p);
      m_Log.AppendText(Environment.NewLine);
      m_Log.ScrollToEnd();
      //Thread.Sleep(100);
    }
    private void SetLabel(TextBlock tb, string str)
    {
      tb.Dispatcher.Invoke(
              new UpdateTextCallbacktb(this._setlabel),
              new object[] { tb, str }
          );
    }
    private void _setlabel(TextBlock tb, string str)
    {
      tb.Text = str;
      Thread.Sleep(100);
    }
    private void SetTextBox(TextBox tb, string str)
    {
      tb.Dispatcher.Invoke(
              new UpdateTextCallbacktxtb(this._settextbox),
              new object[] { tb, str }
          );
    }
    private void _settextbox(TextBox tb, string str)
    {
      tb.Text = str;
      Thread.Sleep(100);
    }
    private void SetPasswordBox(PasswordBox tb, string str)
    {
      tb.Dispatcher.Invoke(
              new UpdateTextCallbackpassb(this._setpasstbox),
              new object[] { tb, str }
          );
    }
    private void _setpasstbox(PasswordBox tb, string str)
    {
      tb.Password = str;
      Thread.Sleep(100);
    }
    private string GetTextBox(TextBox tb)
    {
      string result = "";
      System.Windows.Application.Current.Dispatcher.Invoke(
        DispatcherPriority.Normal,
        (ThreadStart)delegate { result = tb.Text; });
      return result;
    }
    private string GetPasswordBox(PasswordBox tb)
    {
      string result = "";
      System.Windows.Application.Current.Dispatcher.Invoke(
        DispatcherPriority.Normal,
        (ThreadStart)delegate { result = tb.Password; });
      return result;
    }

    private void wifiAfterConnect(string ssid)
    {
      Add2Log("wifiAfterConnect : Connected to " + ssid);
      //form loadda zaten tanımlanıyor... device = (SharpPcap.WinPcap.WinPcapDevice)CaptureDeviceList.Instance.FirstOrDefault(x => x.Name.ToUpper().Contains(wlanIface.InterfaceGuid.ToString().ToUpper()));// devices[devices.Count - 1];
      if (Array.IndexOf(Environment.GetCommandLineArgs(), "testercument") > -1)
        providerIp = "192.168.0.1";
      else
      {
        providerIp = "";
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
            providerIp = v.Addr.ipAddress.ToString();
            providerIp = providerIp.Substring(0, providerIp.LastIndexOf('.')) + ".1";
            Add2Log("providerIp : " + providerIp);
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
      Dispatcher.BeginInvoke(
      DispatcherPriority.ContextIdle,
      new Action(delegate ()
      {
        if ((!Logged) && (cb_AutoLogin.IsChecked == true))
          Login();
      }));
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


    public bool CheckForInternetConnection()
    {
      //return false; // TODO: Aşağıdaki kod sisteme bağlanan bir client a internet var diyor... Buna başka bir çare bulalım.
      try
      {
        using (var client = new WebClient())
        {
          //using (var stream = client.OpenRead("http://www.google.com"))
          {
            WCF.GetSecurityCode("", "", "");
            return true;
          }
        }
      }
      catch
      {
        return false;
      }
    }
    /*
    private bool CheckResult(ref string result)
    {
      if (result.StartsWith("error:"))
      {
        result = result.Substring(result.IndexOf(":") + 1);
        Add2Log(result);
        MessageBox.Show(result);
        return false;
      }
      else if (result.StartsWith("message:"))
        result = result.Substring(result.IndexOf(":") + 1);
      Add2Log(result);
      MessageBox.Show(result);
      return true;
    }
    */

    private string GetWebstring(string Url)
    {
      //Add2Log("GetWebstring(" + Url + ")");
      string AFQ = null;
      WebClient wc = new WebClient();
      try
      {
        while (CheckResult(ref AFQ))
        {
          AFQ = wc.DownloadString(Url);
        }
        if (AFQ == "null")
          AFQ = null;
      }
      catch (Exception)
      {
        AFQ = null;//¶E:" + e.Message + "("+Url+")";
        //Add2Log(AFQ);
      }
      wc.Dispose();
      return AFQ;
    }

    private bool CheckResult(ref string result)
    {
      // rtn true ise checkresult sonrasında komut (Ör: WCF.Register) çalıştırılır, false ise çalıştırılmaz
      // result null döndürülürse komut çalıştırlmadığı gibi dış method da sonlandırılır (Ör: bt_registerclick)
      bool rtn = false;
      if (result == null)
      {
        result = "";
        rtn = true;
      }
      else if (result.Contains("¶")) //¶ : ASCII 20
      {
        string[] sl = result.Split('¶');
        result = result.Substring(0, result.IndexOf("¶"));
        for (int i = 1; i < sl.Length; i++)
        {
          if (sl[i].StartsWith("M:")) // Message
          {
            Add2Log(sl[i]);
            MessageBox.Show(sl[i].Substring(sl[i].IndexOf(":") + 1), "", MessageBoxButton.OK, MessageBoxImage.Asterisk);
          }
          else if (sl[i].StartsWith("Q:")) // Question
          {
            Add2Log(sl[i]); // //§ : ASCII 21
            bool cevaplimi = sl[i].Substring(sl[i].IndexOf("§") + 1).StartsWith("-");
            MessageBoxButton mbb = sl[i].Substring(sl[i].IndexOf("§") + 1) == "OK" ? MessageBoxButton.OK : sl[i].Substring(sl[i].IndexOf("§") + 1) == "OKCancel" ? MessageBoxButton.OKCancel : sl[i].Substring(sl[i].IndexOf("§") + 1) == "YesNo" ? MessageBoxButton.YesNo : sl[i].Substring(sl[i].IndexOf("§") + 1) == "YesNoCancel" ? MessageBoxButton.YesNoCancel : MessageBoxButton.OK;
            MessageBoxResult mbr = MessageBoxResult.None;
            while (mbr == MessageBoxResult.None)
              mbr = cevaplimi ?
                 (
                    sl[i].Substring(sl[i].IndexOf("§") + 1).Substring(1) == MessageBoxResult.Cancel.ToString() ? MessageBoxResult.Cancel
                  : sl[i].Substring(sl[i].IndexOf("§") + 1).Substring(1) == MessageBoxResult.No.ToString() ? MessageBoxResult.No
                  : sl[i].Substring(sl[i].IndexOf("§") + 1).Substring(1) == MessageBoxResult.OK.ToString() ? MessageBoxResult.OK
                  : sl[i].Substring(sl[i].IndexOf("§") + 1).Substring(1) == MessageBoxResult.Yes.ToString() ? MessageBoxResult.Yes
                  : MessageBoxResult.None
                )
                : MessageBox.Show(sl[i].OrtasiniGetir(":", "§"), "", mbb, MessageBoxImage.Question);
            result = result + "¶Q:" + sl[i].OrtasiniGetir(":", "§") + "§-" + mbr.ToString(); // (ör: soru1§cevap1¶soru2§cevap2)
            rtn = rtn || !cevaplimi;
          }
          else if (sl[i].StartsWith("E:")) // Error
          {
            Add2Log(sl[i]);
            MessageBox.Show(sl[i].Substring(sl[i].IndexOf(":") + 1), "", MessageBoxButton.OK, MessageBoxImage.Error);
            result = null;
          }
        }
      }
      //else rtn = false;
      return rtn;
    }

    public string WSRunner(string url)
    {
      string AFQ = null;
      while (CheckResult(ref AFQ))
      {
        //if (CheckForInternetConnection())
        //{
        //  switch (url.GetUrlParam("."))
        //  {
        //    case "Register": AFQ = WCF.Register(url.GetUrlParam("Email"), url.GetUrlParam("Pass"), AFQ, langCode); break;
        //    case "Remove": WCF.Remove(url.GetUrlParam("Email")); AFQ = ""; break;
        //    case "GetSecurityCode": AFQ = WCF.GetSecurityCode(url.GetUrlParam("EmailHash"), AFQ, langCode); break;
        //    case "Login": AFQ = WCF.Login(url.GetUrlParam("Evidence"), AFQ, langCode); break;
        //    case "SendResetPasswordCode": AFQ = WCF.SendResetPasswordCode(url.GetUrlParam("EmailHash"), AFQ, langCode); break;
        //  }
        //}
        //else 
        if (inSystem())
          AFQ = GetWebstring("http://" + providerIp + url);
        else // Please check your internet connection.
          AFQ = "¶E:" + dict.GetMessage(6);
      }
      return AFQ;
    }

    private void bt_Register_Click(object sender, RoutedEventArgs e)
    {
      if (!Register()) return;
      tc_RegisterLogin.SelectedIndex = 0;
      Email = RegEmail;
      Password = RegPass1;
      bt_Login.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
    }

    private bool Register()
    {
      string result = null;
      if (!RegEmail.isValidEmail())
        MessageBox.Show(dict.GetMessage(8));
      else if (RegPass1 == "")
        MessageBox.Show(dict.GetMessage(9));
      else if (RegPass1 != RegPass2)
        MessageBox.Show(dict.GetMessage(15));
      else
        result = WSRunner("/Register?Email=" + RegEmail + "&Pass=" + RegPass1);
      if (CheckResult(ref result)) return false;
      if (mfn.isValidHexString(result, 32)) SecurityCode = result; else return false;
      MessageBox.Show(dict.GetMessage(7));
      return true;
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
      lb_networks.ItemsSource = wifis.OrderByDescending(x => x.SignalQuality);
      if ((Logged) && (!Connected) && (wifis.Count > 0))
        connectWifi(wifis.OrderByDescending(x => x.SignalQuality).ToList()[0].SSIDName);
    }

    private void bt_Refresh_Click(object sender, RoutedEventArgs e)
    {
      refreshNetworkListBox();
      Add2Log("Wifi listesi güncellendi");
    }

    private bool connectWifi(string ssid)
    {
      string key = "";
      if (ssid == "dilek ercu")
        key = "Erci8185";
      else
        key = "erci1234";
      string mac = BitConverter.ToString(System.Text.Encoding.Default.GetBytes(ssid)).Replace("-", "");
      string profileXml = string.Format("<?xml version=\"1.0\"?><WLANProfile xmlns=\"http://www.microsoft.com/networking/WLAN/profile/v1\"><name>{0}</name><SSIDConfig><SSID><hex>{1}</hex><name>{0}</name></SSID></SSIDConfig><connectionType>ESS</connectionType><connectionMode>manual</connectionMode><MSM><security><authEncryption><authentication>WPA2PSK</authentication><encryption>AES</encryption><useOneX>false</useOneX></authEncryption><sharedKey><keyType>passPhrase</keyType><protected>false</protected><keyMaterial>{2}</keyMaterial></sharedKey><keyIndex>0</keyIndex></security></MSM></WLANProfile>", ssid, mac, key);
      //      MessageBox.Show("Profil kaydedilecek...");
      wlanIface.SetProfile(Wlan.WlanProfileFlags.AllUser, profileXml, true);
      //      MessageBox.Show("Profil kaydedildi");
      //dispatcherTimer.IsEnabled = false;
      //_scan = false;
      //      MessageBox.Show(ssid);
      try
      {
        //        MessageBox.Show("will connect");
        //        Thread.Sleep(10000);
        wlanIface.Connect(Wlan.WlanConnectionMode.Profile, Wlan.Dot11BssType.Any, ssid);
        //while (true)//!(wlanIface.InterfaceState == Wlan.WlanInterfaceState.Connected))
        //{
        //  Thread.Sleep(200);
        //  MessageBox.Show(wlanIface.InterfaceState.ToString());
        //}
        //MessageBox.Show(wlanIface.CurrentConnection.ToString());
        //MessageBox.Show("was connect");
      }
      catch (Exception err)
      {
        //dispatcherTimer.IsEnabled = true;
        //_scan = true;
        MessageBox.Show(err.Message);
        //throw;
      }
      //MessageBox.Show("connected");
      //MessageBox.Show("Bağlanılıyor... (" + ssid + ")");
      //MessageBox.Show("1");
      //while (!(wlanIface.InterfaceState == Wlan.WlanInterfaceState.Connected))
      //{
      //  Thread.Sleep(200);
      //  MessageBox.Show(wlanIface.InterfaceState.ToString());
      //}
      //MessageBox.Show("1");
      //MessageBox.Show("Bağlantı Sağlandı => " + wlanIface.CurrentConnection.profileName + " (" + wlanIface.NetworkInterface.Speed / 1000000 + " Mbps)");
      //MessageBox.Show("1");
      return true;
    }

    private void lb_networks_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
      try
      {
        var selected = (lb_networks.SelectedItem as WifiType);
        if (selected == null)
          return;
        connectWifi(selected.SSIDName);
      }
      catch (Exception err)
      {
        MessageBox.Show(err.Message);
        //throw;
      }
    }

    private void cb_Remember_Checked(object sender, RoutedEventArgs e)
    {
      RememberAction(sender);
    }

    private void RememberAction(object sender = null)
    {
      SimpleAES saes = new SimpleAES();
      if ((sender == cb_CEmailRemember) || (sender == null))
      {
        if ((cEmailRememberisChecked == true) && (Connected))
          Registry.CurrentUser.CreateSubKey(_projectregaddr).SetValue(cb_CEmailRemember.Name.Substring(3), saes.EncryptToString(Email));
        else if (cEmailRememberisChecked == false) Registry.CurrentUser.CreateSubKey(_projectregaddr).SetValue(cb_CEmailRemember.Name.Substring(3), "");
      }
      if ((sender == cb_CPassRemember) || (sender == null))
      {
        if ((CPassRememberisChecked == true) && (Connected))
          Registry.CurrentUser.CreateSubKey(_projectregaddr).SetValue(cb_CPassRemember.Name.Substring(3), saes.EncryptToString(Password));
        else if (CPassRememberisChecked == false) Registry.CurrentUser.CreateSubKey(_projectregaddr).SetValue(cb_CPassRemember.Name.Substring(3), "");
      }
      if ((sender == cb_AutoLogin) || (sender == cb_AutoConnect))
        Registry.CurrentUser.CreateSubKey(_projectregaddr).SetValue(((CheckBox)sender).Name.Substring(3), (((CheckBox)sender).IsChecked == true ? "*" : ""));
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
          || (!d_ip.StartsWith(providerIp.Substring(0, providerIp.LastIndexOf('.'))))
          || (d_ip.EndsWith(".255"))) return;
        //        if ((d_mac == devmac) && (s_ip != providerIp))
        {
          receivedBytes += eth.Bytes.Length - eth.Header.Length;
          //Add2Log(eth.ToString());
        }
      }
    }

    private void mainWindow_Loaded(object sender, RoutedEventArgs e)
    {
      DataContext = this;
      dict = new MyDictionary(CultureInfo.CurrentUICulture.TwoLetterISOLanguageName, Properties.Resources.Dict.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries));
      thrGetSetUsage = new Thread(new ThreadStart(GetSetUsage));
      tc_RegisterLogin.SelectedIndex = 1;
      SimpleAES saes = new SimpleAES();
      if ((Registry.CurrentUser.OpenSubKey(_projectregaddr) != null) && (Registry.CurrentUser.OpenSubKey(_projectregaddr).GetValue(cb_CEmailRemember.Name.Substring(3)) != null) && (Registry.CurrentUser.OpenSubKey(_projectregaddr).GetValue(cb_CEmailRemember.Name.Substring(3)).ToString() != ""))
        Email = saes.DecryptToString(Registry.CurrentUser.OpenSubKey(_projectregaddr).GetValue(cb_CEmailRemember.Name.Substring(3)).ToString());
      if ((Registry.CurrentUser.OpenSubKey(_projectregaddr) != null) && (Registry.CurrentUser.OpenSubKey(_projectregaddr).GetValue(cb_CPassRemember.Name.Substring(3)) != null) && (Registry.CurrentUser.OpenSubKey(_projectregaddr).GetValue(cb_CPassRemember.Name.Substring(3)).ToString() != ""))
        Password = saes.DecryptToString(Registry.CurrentUser.OpenSubKey(_projectregaddr).GetValue(cb_CPassRemember.Name.Substring(3)).ToString());
      if (Email != "") CEmailRememberisChecked = true;
      if (Password != "") CPassRememberisChecked = true;
      if ((Email != "") || (Password != ""))
        tc_RegisterLogin.SelectedIndex = 0;
      if ((Registry.CurrentUser.OpenSubKey(_projectregaddr) != null) && (Registry.CurrentUser.OpenSubKey(_projectregaddr).GetValue(cb_AutoLogin.Name.Substring(3)) != null))
        cb_AutoLogin.IsChecked = (Registry.CurrentUser.OpenSubKey(_projectregaddr).GetValue(cb_AutoLogin.Name.Substring(3)).ToString() == "*");
      if ((Registry.CurrentUser.OpenSubKey(_projectregaddr) != null) && (Registry.CurrentUser.OpenSubKey(_projectregaddr).GetValue(cb_AutoConnect.Name.Substring(3)) != null))
        cb_AutoConnect.IsChecked = (Registry.CurrentUser.OpenSubKey(_projectregaddr).GetValue(cb_AutoConnect.Name.Substring(3)).ToString() == "*");

      string wfname = CurrentWifi();
      if (wfname.StartsWith(wifiprefix))// && (!isSysConnected()))
        wifiAfterConnect(wfname);
      /*
            if ((tb_Email.Text != "") || (tb_Password.Password != ""))
            {
              tc_RegisterLogin.SelectedIndex = 0;
              if ((tb_Email.Text != "") && (tb_Password.Password != ""))
                bt_Login.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
              else if (tb_Email.Text != "")
                Dispatcher.BeginInvoke(
                  DispatcherPriority.ContextIdle,
                  new Action(delegate ()
                  {
                    tb_Password.Focus();
                  }));
              else if (tb_Password.Password != "")
                Dispatcher.BeginInvoke(
                  DispatcherPriority.ContextIdle,
                  new Action(delegate ()
                  {
                    tb_Email.Focus();
                  }));
            }

      */
      wlanIface.WlanConnectionNotification += wlanIface_WlanConnectionNotification;
      bt_Refresh.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
      //Otomatik taramadan vazgeçtim. Elle yenilesin
      //dispatcherTimer = new System.Windows.Threading.DispatcherTimer();
      //dispatcherTimer.Tick += new EventHandler(dispatcherTimer_Tick);
      //dispatcherTimer.Interval = new TimeSpan(0, 0, wifiCheckInterval);
      //dispatcherTimer.Start();
      //wlanIface.WlanNotification += wlanIface_WlanNotification;
      //wlanIface.WlanReasonNotification += wlanIface_WlanReasonNotification;
      //bt_Refresh_Click(bt_Refresh, null);
      //bt_Remove_Click(bt_Remove, null);
      //Add2Log("Üyelik silindi.");
      //bt_Register_Click(bt_Remove, null);
      //Add2Log("Kayıt tamamlandı.");
      //bt_Connect_Click(bt_Connect, null);
    }

    private void mainWindow_Closing(object sender, CancelEventArgs e)
    {
      Disconnect();
    }

    //private void m_thrperlcrnet()
    //{
    //  recievedBytes_CLRNET += performanceCounterReceivedCLRNET.RawValue;
    //}

    //private void m_thrperPFNI()
    //{
    //  while (true)
    //  {
    //    recievedBytes_PFNI += (int)(performanceCounterReceivedPFNI.NextValue());
    //    Thread.Sleep(300);
    //  }
    //}
    /*
    private void GetSetUsage()
    {
      bool stop = false;
      while ((Connected) && (!stop))
      {
        //Add2Log("GetSetUsage tur başı : " + DateTime.Now.ToString("HH:mm:ss"));
        string result = GetWebstring("http://" + providerIp + "/GetUsage?EmailHash=" + GetTextBox(tb_Email).HashMD5() + "&ConnectionID=" + ConnectionID.ToString());
        if (result == null)
        {
          rcTimer.Start();
          stop = true;
          return;
        }
        else
        {
          if (CheckResult(ref result)) return;
          long usg = long.Parse(result.Substring(0, result.IndexOf(";")));
          //Add2Log("Client : " + recievedBytes.ToString() + " Server : " + usg.ToString());
          if ((recievedBytes + 1024 * 1024 < usg) && (recievedBytes * 1.001 < usg)) // 1MB tan fazla fark var ve bu fark 1/1000 den fazla ise kestiik.
          {
            MessageBox.Show("Kullanım fazlası rapor edildi: " + recievedBytes.ToString() + "<" + usg.ToString());
            rcTimer.Start();
            return;
          }
          else
          {
            //if (usg > 0)
            //  Add2Log("GetUsage: ConnectionID: " + result.Substring(result.IndexOf(";") + 1) + " Usage:" + usg.ToString("###,###,###,###") + " client-server ölçümü:" + (recievedBytes - usg).ToString("###,###,###,###"));
            Quota = (_loginquota - usg) > 0 ? _loginquota - usg : 0;
            Add2Log(Quota.ToString("###,###,###") + "-" + _loginquota.ToString("###,###,###") + "-" + usg.ToString("###,###,###"));
            result = GetWebstring("http://" + providerIp + "/SetUsage?Message=" + GetTextBox(tb_Email).HashMD5() + (SecurityCode + (GetTextBox(tb_Email).HashMD5() + Password).HashMD5() + (usg.ToString()).HashMD5()).HashMD5() + usg.ToString());
            if (result == null)
            {
              rcTimer.Start();
              stop = true;
              return;
            }
            else
            {
              if (CheckResult(ref result)) return;
              if (Quota == 0)
              {
                rcTimer.Start();
                stop = true;
                return;
              }
              if (result.Length == 32)
                SecurityCode = result;
              Thread.Sleep(GetSetUsageInterval);
            }
          }
        }
      }
    }
    */
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
        Quota = _loginquota - _receivedBytes;
        //Add2Log(_loginquota.ToString("###,###,##0") + " - " + _receivedBytes.ToString("###,###,##0") + " = " + Quota.ToString("###,###,##0"));
        string result = GetWebstring("http://" + providerIp + "/SetUsage?Message=" + Email.HashMD5() + (SecurityCode + (Email.HashMD5() + Password).HashMD5() + (_receivedBytes.ToString()).HashMD5()).HashMD5() + _receivedBytes.ToString());
        //Add2Log(result == null ? "null" : result);
        if (CheckResult(ref result)) return;
        if (result.Length == 32)
          SecurityCode = result;
        if (Quota == 0)
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

    private void bt_Connect_Click(object sender, RoutedEventArgs e)
    {
      if (Connected)
        Disconnect();
      else
        Connect();
    }

    private void Disconnect()
    {
      if (CurrentWifi().StartsWith(wifiprefix))
        GetWebstring("http://" + providerIp + "/Disconnect?ConnectionID=" + ConnectionID.ToString());
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
      if (!Logged)
        Login();
      if (Connected) return true; // AutoConnect seçili ise Login() içinden connect olmuş olabilir. 
      bool rtn = false;
      string result = GetWebstring("http://" + providerIp + "/ConnectUS?ClientEvidence=" + myEvidence());
      if (CheckResult(ref result)) return rtn;
      long tmplong = 0;
      if ((result.Length > 33) && (result.Substring(32, 1) == ";") && (mfn.isValidHexString(result.Substring(0, 32))) && (long.TryParse(result.Substring(33), out tmplong)))
      {
        SecurityCode = result.Substring(0, 32);
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

    private void tb_Password_PasswordChanged(object sender, RoutedEventArgs e)
    {
      Password = GetPasswordBox(tb_Password);
    }

    private void tb_RegisterPassword1_PasswordChanged(object sender, RoutedEventArgs e)
    {
      RegPass1 = GetPasswordBox(tb_RegisterPassword1);
    }

    private void tb_RegisterPassword2_PasswordChanged(object sender, RoutedEventArgs e)
    {
      RegPass2 = GetPasswordBox(tb_RegisterPassword2);
    }
  }
}
