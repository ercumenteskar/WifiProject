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

namespace WifiWPFClient
{
  /// <summary>
  /// Interaction logic for MainWindow.xaml
  /// </summary>
  public partial class MainWindow : Window, INotifyPropertyChanged
  {
    private const string _projectName = "Wifi";
    private const string _projectregaddr = "Software\\" + _projectName;
    String wifiprefix = "";
    String providerMAC = "";
    String providerIp = "";
    //private String Email = "5448302899";
    private String Password = "e";
    private String SecurityCode = "";
    //private String _connId = "";
    private WlanClient.WlanInterface wlanIface = (new WlanClient()).Interfaces[0];
    //private long _bytesreceived = 0;
    //private int _progress = 0;
    private String lastWifiName = "";
    //bool _stop = false;
    //bool _scan = true;
    //System.Windows.Threading.DispatcherTimer dispatcherTimer;
    int GetSetUsageInterval = 3000;
    //private int wifiCheckInterval = 3; // Wifi listesi kaç saniyede bir güncellensin?
    //private List<Conn> ConnList = new List<Conn>();
    List<WifiType> wifis = new List<WifiType>();
    public static WifiService.Service1Client _WCF;
    public event PropertyChangedEventHandler PropertyChanged;
    private void OnPropertyChanged(String property) { if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(property)); } }
    public string bt_ConnectContent { get { return Connected ? "Disconnect" : "Connect"; } }
    private long _quota = -1;
    public long Quota { get { return _quota; } set { _quota = value; OnPropertyChanged("QuotaStr"); } }
    public string QuotaStr { get { return (Quota / 1024).ToString("###,###,###,###,##0 KB"); } }
    public long ConnectionID = 0;
    //public String ssid = "";
    //public long Usage = 0;
    public long startUsage = 0;
    private bool _connected = false;
    //    public bool Connected { get { return _connected; } set { _connected = value; OnPropertyChanged("bt_ConnectContent"); } }
    public bool Connected
    {
      get { return _connected; }
      set
      {
        _connected = value;
        tb_Email.IsEnabled = !value;
        tb_Password.IsEnabled = !value;
        if (!value)
        {
          disconnectFromSys();
          Add2Log("Logout başarılı");
        }
        else
          Add2Log("Login başarılı");
        OnPropertyChanged("bt_ConnectContent");
      }
    }
    private string langCode = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
    private SharpPcap.WinPcap.WinPcapDevice device;
    private long recievedBytes = 0;
    private MyDictionary dict;

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
      SecurityCode = GetWebString("http://" + providerIp + "/GetSecurityCode?EmailHash=" + GetTextBox(tb_Email).HashMD5());
      if (SecurityCode == null) return false;
      return !CheckResult(ref SecurityCode);
    }

    private void bt_Connect_Click(object sender, RoutedEventArgs e)
    {
      String wfname = connectedWifi();
      if (Connected)
        Connected = false;
      else if (wfname == "")
        MessageBox.Show(dict.GetMessage(16));
      else if (!inSystem())
        MessageBox.Show(dict.GetMessage(17, wfname));
      else
      {
        if (!GetTextBox(tb_Email).isValidEmail())
          MessageBox.Show(dict.GetMessage(8));
        else if (GetPasswordBox(tb_Password) == "")
          MessageBox.Show(dict.GetMessage(9));
        else
          connect2Sys();
      }
    }

    private bool inSystem()
    {
      return ((wlanIface.CurrentConnection.isState == Wlan.WlanInterfaceState.Connected)
          && (GetStringForSSID(wlanIface.CurrentConnection.wlanAssociationAttributes.dot11Ssid).StartsWith(wifiprefix))
          && (GetWebString("http://" + providerIp + "/ping") == "pong"))
      ;
    }

    private void disconnectFromSys()
    {
      GetWebString("http://" + providerIp + "/Disconnect?ConnectionID=" + ConnectionID.ToString());
      Add2Log(dict.GetMessage(12));
    }

    private void connect2Sys()
    {
      Add2Log("connectSys : started");
      //Add2Log(GetTextBox(tb_Email) + "  "+GetPasswordBox(tb_Password));
      //Email = tb_Email.Text;
      //Password = tb_Password.Text;
      if (!SetSecurityCode()) return;
      else if (SecurityCode.Length != 32)
      {
        Add2Log("Hata: Güvenlik kodu dönmedi. Dönen değer:" + SecurityCode);
        return;
      }
      else
        Add2Log("SecurityCode alındı : " + SecurityCode);
      String result = GetWebString("http://" + providerIp + "/Login?Evidence=" + GetTextBox(tb_Email).HashMD5() + (SecurityCode + (GetTextBox(tb_Email).HashMD5() + GetPasswordBox(tb_Password)).HashMD5() + ("").HashMD5()).HashMD5());
      if (result == null) return;
      if (CheckResult(ref result)) return;
      if ((result.Length > 33) && (result.Substring(32, 1) == ";"))
      {
        Quota = long.Parse(result.Substring(33));
        SecurityCode = result.Substring(0, 32);
        //SetLabel(l_Quota, QuotaStr);
        Add2Log("Login başarılı");
      }
      else
      {
        Quota = 0;
        SecurityCode = "";
        //l_Quota.Text = QuotaStr;
        Add2Log("Hata: Giriş yapılamadı. " + result);
        return;
      }
      long stus = wlanIface.NetworkInterface.GetIPStatistics().BytesReceived;
      result = GetWebString("http://" + providerIp + "/ConnectUS?ClientEvidence=" + GetTextBox(tb_Email).HashMD5() + (SecurityCode + (GetTextBox(tb_Email).HashMD5() + Password).HashMD5() + ("").HashMD5()).HashMD5());
      if (result == null) return;
      if (CheckResult(ref result)) return;
      if ((result.Length > 33) && (result.Substring(32, 1) == ";"))
      {
        SecurityCode = result.Substring(0, 32);
        long cid = long.Parse(result.Substring(33));
        Add2Log("Bağlantı sağlandı. ConnectionID : " + cid.ToString());
        ConnectionID = cid;
        recievedBytes = 0;
        //startUsage = stus; 
        Connected = true;
        device.StartCapture();
        RememberAction();
        //};
      }
      else
        Add2Log("Bağlantı sağlanamadı. (" + result + ")");

      Thread _thrGetSetUsage = new Thread(new ThreadStart(GetSetUsage));
      _thrGetSetUsage.IsBackground = true;
      _thrGetSetUsage.Start();
      Add2Log("GetSetUsage thread started!");

      //Thread _thrUsageStats = new Thread(new ThreadStart(UsageStats));
      //_thrUsageStats.Start();
      //_thrUsageStats.IsBackground = true;
      //Add2Log("UsageStats thread started!");
    }

    private String connectedWifi()
    {
      String rtn = "";
      try
      {
        if (wlanIface.CurrentConnection.isState == Wlan.WlanInterfaceState.Connected)
          //&& (wlanIface.CurrentConnection.wlanAssociationAttributes.Dot11Bssid.ToString().Equals(providerMAC))
          rtn = GetStringForSSID(wlanIface.CurrentConnection.wlanAssociationAttributes.dot11Ssid);
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
      String _wfname = connectedWifi();
      if (!_wfname.Equals(lastWifiName))
      {
        if (!lastWifiName.Equals(""))
          wifiAfterDisconnect(lastWifiName);
        if (!_wfname.Equals(""))
          wifiAfterConnect(_wfname);
        lastWifiName = _wfname;
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

    private void GetSetUsage()
    {
      while (Connected)
      {
        //UsageStats(); // Kendi bilgilerini de güncelle...
        String result = GetWebString("http://" + providerIp + "/GetUsage?EmailHash=" + GetTextBox(tb_Email).HashMD5() + "&ConnectionID=" + ConnectionID.ToString());
        if (result == null)
        {
          Connected = false;
          return;
        }
        else
        {
          if (CheckResult(ref result)) return;
          long usg = long.Parse(result.Substring(0, result.IndexOf(";")));
          if (recievedBytes + 1024 * 1024 * 1024 >= usg) // şimdilik 1 GB sus payı // +1000 Bağlandığımız sıradaki BytesReceived bilgisini StartUsage e atamayı bağlanmadan hemen önceki kısma taşıdım. Gerek kalmamış olması lazım. Yine false alarm verirse 1000 i tekrar eklerim. 
          {
            if (usg > 0)
              Add2Log("GetUsage: ConnectionID: " + result.Substring(result.IndexOf(";") + 1) + " Usage:" + usg.ToString("###,###,###,###") + " server-client ölçümü:" + (usg - recievedBytes).ToString("###,###,###,###"));
            Quota = (Quota - usg) > 0 ? Quota - usg : 0;
            result = GetWebString("http://" + providerIp + "/SetUsage?Message=" + GetTextBox(tb_Email).HashMD5() + (SecurityCode + (GetTextBox(tb_Email).HashMD5() + Password).HashMD5() + (usg.ToString()).HashMD5()).HashMD5() + usg.ToString());
            if (result == null) return;
            if (CheckResult(ref result)) return;
            if (Quota == 0)
              disconnectWifi();
            //SetLabel(l_Quota, QuotaStr);
            if (result.Length == 32)
              SecurityCode = result;
          }
          else
          {
            MessageBox.Show("Kullanım fazlası rapor edildi: " + recievedBytes.ToString() + "<" + usg.ToString());
            Connected = false;
          }
          Thread.Sleep(GetSetUsageInterval);
        }
      }
    }

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
      m_Log.AppendText(p);
      m_Log.AppendText(Environment.NewLine);
      m_Log.ScrollToEnd();
      Thread.Sleep(100);
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
    private String GetTextBox(TextBox tb)
    {
      string result = "";
      System.Windows.Application.Current.Dispatcher.Invoke(
        DispatcherPriority.Normal,
        (ThreadStart)delegate { result = tb.Text; });
      return result;
    }
    private String GetPasswordBox(PasswordBox tb)
    {
      string result = "";
      System.Windows.Application.Current.Dispatcher.Invoke(
        DispatcherPriority.Normal,
        (ThreadStart)delegate { result = tb.Password; });
      return result;
    }

    private void wifiAfterConnect(String ssid)
    {
      Add2Log("wifiAfterConnect : Connected to " + ssid);
      device = (SharpPcap.WinPcap.WinPcapDevice)CaptureDeviceList.Instance.FirstOrDefault(x => x.Name.ToUpper().Contains(wlanIface.InterfaceGuid.ToString().ToUpper()));// devices[devices.Count - 1];
      var v = device.Addresses.First(x => x.Addr.ipAddress.ToString().Contains("."));
      if (v != null)
      {
        providerIp = v.Addr.ipAddress.ToString();
        providerIp = providerIp.Substring(0, providerIp.LastIndexOf('.'))  + ".1";
      }
      if (!inSystem())
      {
        Add2Log("wifiAfterConnect : Wifi is not in the system");
      }
      else
      {
        Add2Log("wifiAfterConnect : will connect to sys");
        if (!Connected)
          connect2Sys();
      }
    }

    private void wifiAfterDisconnect(String ssid)
    {
      Add2Log("wifiAfterDisconnect : Disconnected from " + ssid);
      if (device.Started)
        device.StopCapture();
    }
    private void dispatcherTimer_Tick(object sender, EventArgs e)
    {
      //if (!isSysConnected()) sisteme bağlı olsa bile listenin yenilenmesi iyi olur. Sisteme bağlandığında interval 10 sn ye çekilebilir.
      refreshNetworkListBox();
    }

    private void disconnectWifi()
    {
      string _cstr = wlanIface.GetProfileXml(wlanIface.CurrentConnection.profileName);
      wlanIface.DeleteProfile(wlanIface.CurrentConnection.profileName);
      wlanIface.SetProfile(Wlan.WlanProfileFlags.AllUser, _cstr, true);
    }


    public static bool CheckForInternetConnection()
    {
      try
      {
        using (var client = new WebClient())
        {
          using (var stream = client.OpenRead("http://www.google.com"))
          {
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

    private String GetWebString(String Url)
    {
      String AFQ = null;
      WebClient wc = new WebClient();
      try
      {
        while (CheckResult(ref AFQ))
        {
          AFQ = wc.DownloadString(Url);
        }
      }
      catch (Exception e)
      {
        AFQ = "";//¶E:" + e.Message + "("+Url+")";
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

    public String WSRunner(String url)
    {
      string AFQ = null;
      while (CheckResult(ref AFQ))
      {
        if (CheckForInternetConnection())
        {
          switch (url.GetUrlParam("."))
          {
            case "Register": AFQ = WCF.Register(url.GetUrlParam("Email"), url.GetUrlParam("Pass"), AFQ, langCode); break;
            case "Remove": WCF.Remove(url.GetUrlParam("Email")); AFQ = ""; break;
            case "GetSecurityCode": AFQ = WCF.GetSecurityCode(url.GetUrlParam("EmailHash"), AFQ, langCode); break;
            case "Login": AFQ = WCF.Login(url.GetUrlParam("Evidence"), AFQ, langCode); break;
            case "SendResetPasswordCode": AFQ = WCF.SendResetPasswordCode(url.GetUrlParam("EmailHash"), AFQ, langCode); break;
          }
        } if (inSystem())
          AFQ = GetWebString("http://" + providerIp + url);
        else // Please check your internet connection.
          AFQ = "¶E:" + dict.GetMessage(6);
      }
      return AFQ;
    }

    private void bt_Register_Click(object sender, RoutedEventArgs e)
    {
      string result = null;
      if (!GetTextBox(tb_RegisterEmail).isValidEmail())
        MessageBox.Show(dict.GetMessage(8));
      else if (GetPasswordBox(tb_RegisterPassword1) == "")
        MessageBox.Show(dict.GetMessage(9));
      else if (GetPasswordBox(tb_RegisterPassword1) != GetPasswordBox(tb_RegisterPassword2))
        MessageBox.Show(dict.GetMessage(15));
      else
        result = WSRunner("/Register?Email=" + GetTextBox(tb_RegisterEmail) + "&Pass=" + GetPasswordBox(tb_RegisterPassword1));
      if (result == null) return;
      if (CheckResult(ref result)) return;
      MessageBox.Show(dict.GetMessage(7));
      tc_RegisterLogin.SelectedIndex = 0;
      SetTextBox(tb_Email, GetTextBox(tb_RegisterEmail));
      SetPasswordBox(tb_Password, GetPasswordBox(tb_RegisterPassword1));
      bt_Connect.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
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
        if (wifis.Where(x => x.SSIDName == GetStringForSSID(item.dot11Ssid)).ToList().Count() == 0)
          wifis.Add(new WifiType() { SSIDName = GetStringForSSID(item.dot11Ssid), SignalQuality = item.wlanSignalQuality });
      }
      foreach (var item in wifis)
      {
        if (networks.Where(x => item.SSIDName == GetStringForSSID(x.dot11Ssid)).ToList().Count() == 0)
          item.SignalQuality = 0;//wifis.Remove(item);
      }
      lb_networks.ItemsSource = wifis.OrderByDescending(x => x.SignalQuality);
    }

    private void bt_Refresh_Click(object sender, RoutedEventArgs e)
    {
      refreshNetworkListBox();
      Add2Log("Wifi listesi güncellendi");
    }

    private bool connectWifi(String ssid)
    {
      string key = "";
      if (ssid == "dilek ercu")
        key = "10203040";
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
        if ((cb_CEmailRemember.IsChecked == true) && (Connected))
          Registry.CurrentUser.CreateSubKey(_projectregaddr).SetValue(cb_CEmailRemember.Name.Substring(3), saes.EncryptToString(GetTextBox(tb_Email)));
        else if (cb_CEmailRemember.IsChecked == false) Registry.CurrentUser.CreateSubKey(_projectregaddr).SetValue(cb_CEmailRemember.Name.Substring(3), "");
      }
      if ((sender == cb_CPassRemember) || (sender == null))
      {
        if ((cb_CPassRemember.IsChecked == true) && (Connected))
          Registry.CurrentUser.CreateSubKey(_projectregaddr).SetValue(cb_CPassRemember.Name.Substring(3), saes.EncryptToString(GetPasswordBox(tb_Password)));
        else if (cb_CPassRemember.IsChecked == false) Registry.CurrentUser.CreateSubKey(_projectregaddr).SetValue(cb_CPassRemember.Name.Substring(3), "");
      }
    }

    private void device_OnPcapStatistics(object sender, SharpPcap.WinPcap.StatisticsModeEventArgs e)
    {
      recievedBytes += e.Statistics.RecievedBytes;
    }

    private void mainWindow_Loaded(object sender, RoutedEventArgs e)
    {
      //MessageBox.Show("1");
      dict = new MyDictionary(CultureInfo.CurrentUICulture.TwoLetterISOLanguageName, Properties.Resources.Dict.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries));
      try
      {
        device = CaptureDeviceList.Instance.FirstOrDefault(x => x.Name.Contains(wlanIface.InterfaceGuid.ToString().ToUpper())) as SharpPcap.WinPcap.WinPcapDevice;// devices[devices.Count - 1];
      }
      catch (Exception err)
      {
        MessageBox.Show(err.Message);
        throw;
      } 
      device.OnPcapStatistics += new SharpPcap.WinPcap.StatisticsModeEventHandler(device_OnPcapStatistics);
      device.Open(DeviceMode.Normal, 1000); //DeviceMode.Promiscuous
      device.Mode = SharpPcap.WinPcap.CaptureMode.Statistics;
      device.StartCapture();

      this.DataContext = this;
      tc_RegisterLogin.SelectedIndex = 1;
      SimpleAES saes = new SimpleAES();
      if ((Registry.CurrentUser.OpenSubKey(_projectregaddr) != null) && (Registry.CurrentUser.OpenSubKey(_projectregaddr).GetValue(cb_CEmailRemember.Name.Substring(3)) != null) && (Registry.CurrentUser.OpenSubKey(_projectregaddr).GetValue(cb_CEmailRemember.Name.Substring(3)).ToString() != ""))
        tb_Email.Text = saes.DecryptToString(Registry.CurrentUser.OpenSubKey(_projectregaddr).GetValue(cb_CEmailRemember.Name.Substring(3)).ToString());
      if ((Registry.CurrentUser.OpenSubKey(_projectregaddr) != null) && (Registry.CurrentUser.OpenSubKey(_projectregaddr).GetValue(cb_CPassRemember.Name.Substring(3)) != null) && (Registry.CurrentUser.OpenSubKey(_projectregaddr).GetValue(cb_CPassRemember.Name.Substring(3)).ToString() != ""))
        tb_Password.Password = saes.DecryptToString(Registry.CurrentUser.OpenSubKey(_projectregaddr).GetValue(cb_CPassRemember.Name.Substring(3)).ToString());
      if (tb_Email.Text != "") cb_CEmailRemember.IsChecked = true;
      if (tb_Password.Password != "") cb_CPassRemember.IsChecked = true;
      if ((tb_Email.Text != "") || (tb_Password.Password != ""))
      {
        tc_RegisterLogin.SelectedIndex = 0;
        if ((tb_Email.Text != "") && (tb_Password.Password != ""))
          bt_Connect.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        else if (tb_Email.Text != "")
          Dispatcher.BeginInvoke(
            DispatcherPriority.ContextIdle,
            new Action(delegate()
                {
                  tb_Password.Focus();
                }));
        else if (tb_Password.Password != "")
          Dispatcher.BeginInvoke(
            DispatcherPriority.ContextIdle,
            new Action(delegate()
            {
              tb_Email.Focus();
            }));
      }
      wlanIface.WlanConnectionNotification += wlanIface_WlanConnectionNotification;
      String wfname = connectedWifi();
      if ((!wfname.Equals("")))// && (!isSysConnected()))
        wifiAfterConnect(wfname);
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

  }
}
