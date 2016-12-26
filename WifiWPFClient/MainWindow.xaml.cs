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

namespace WifiWPFClient
{
  /// <summary>
  /// Interaction logic for MainWindow.xaml
  /// </summary>
  public partial class MainWindow : Window
  {
    String wifiprefix = "di";
    String providerMAC = "";// Her provider aynı mac olsun istersem bunu doldururum.
    private String TelNo = "5448302899";
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
    //private int wifiCheckInterval = 3; // Wifi listesi kaç saniyede bir güncellensin?
    private List<Conn> ConnList = new List<Conn>();
    private class Conn
    {
      public long ConnectionID = 0;
      public long Quota = 0;
      public String ssid = "";
      public long Usage = 0;
      public long startUsage = 0;
      public bool Connected = false;
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

    private String GetSecurityCode()
    {
      return GetWebString("http://192.168.137.1/GetSecurityCode?TelNoHash=" + TelNo.HashMD5());
    }

    private void SetSecurityCode()
    {
      SecurityCode = GetWebString("http://192.168.137.1/GetSecurityCode?TelNoHash=" + TelNo.HashMD5());
    }

    private String GetWebString(String Url)
    {
      WebClient wc = new WebClient();
      String result = "";
      try
      {
        result = wc.DownloadString(Url);
      }
      catch (Exception)
      {
        result = "";
        //throw;
      }
      wc.Dispose();
      return result;
    }

    private void bt_Connect_Click(object sender, RoutedEventArgs e)
    {

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
      String _wfname = connectedWifi();
      if (!_wfname.Equals(lastWifiName))
      {
        if (!lastWifiName.Equals(""))
          wifiAfterDisconnect(lastWifiName);
        if (!_wfname.Equals(""))
          wifiAfterConnect(_wfname);
        lastWifiName = _wfname;
      }
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

    String connectedWifi()
    {
      String rtn = "";
      try
      {
        if ((wlanIface.CurrentConnection.isState == Wlan.WlanInterfaceState.Connected) && (rtn.StartsWith(wifiprefix)))
          //&& (wlanIface.CurrentConnection.wlanAssociationAttributes.Dot11Bssid.ToString().Equals(providerMAC))
          rtn = GetStringForSSID(wlanIface.CurrentConnection.wlanAssociationAttributes.dot11Ssid);
      }
      catch (Exception)
      {
      }
      return rtn;
    }

    bool isSysConnected()
    {
      return false;
    }

    private void UsageStats()
    {
      if (ConnList.Last().Connected)
      {
        //Thread.Sleep(1000);
        long _br = wlanIface.NetworkInterface.GetIPStatistics().BytesReceived - ConnList.Last().startUsage; // Usage amount since connected
        //Console.WriteLine("IPS: " + interfaces.FirstOrDefault(x => x.Name == wlanIface.InterfaceName).GetIPStatistics().BytesReceived.ToString("###,###,###,###") + " (" + ((interfaces.FirstOrDefault(x => x.Name == wlanIface.InterfaceName).GetIPStatistics().BytesReceived - _bytesreceived) / 1024).ToString("###,###,###") + "Kb/sn)");
        Console.WriteLine("IPS: " + _br.ToString("###,###,###,###") + " (" + ((_br - ConnList.Last().Usage) / 1024).ToString("###,###,###") + "Kb/sn)");
        ConnList.Last().Usage = _br;
        //Console.WriteLine("IPv4S: " + interfaces.FirstOrDefault(x => x.Name == wlanIface.InterfaceName).GetIPv4Statistics().BytesReceived.ToString("###,###,###,###"));
      }
    }
    private void GetSetUsage()
    {
      while (ConnList.Last().Connected)
      {
        String _tmp = GetWebString("http://192.168.137.1/GetUsage?TelNoHash=" + TelNo.HashMD5() + "&ConnectionID=" + ConnList.Last().ConnectionID.ToString());
        UsageStats(); // Kendi bilgilerini de güncelle...
        if ((_tmp.StartsWith("error:")) || (_tmp.Length < 3) || (!_tmp.Contains(";")))
        {
          Add2Log("Hata: " + _tmp);
          ConnList.Last().Connected = false;
        }
        else if (_tmp.Substring(_tmp.IndexOf(";") + 1) != ConnList.Last().ConnectionID.ToString())
        {
          Add2Log("GetUsage: Farklı ConnectionID döndürdü: " + _tmp.Substring(_tmp.IndexOf(";") + 1) + "<>" + ConnList.Last().ConnectionID.ToString());
          ConnList.Last().Connected = false;
        }
        else
        {
          long usg = long.Parse(_tmp.Substring(0, _tmp.IndexOf(";")));
          if (ConnList.Last().Usage >= usg) // şimdilik 1 kb sus payı // +1000 Bağlandığımız sıradaki BytesReceived bilgisini StartUsage e atamayı bağlanmadan hemen önceki kısma taşıdım. Gerek kalmamış olması lazım. Yine false alarm verirse 1000 i tekrar eklerim. 
          {
            if (usg > 0)
              Add2Log("GetUsage: ConnectionID: " + _tmp.Substring(_tmp.IndexOf(";") + 1) + " Usage:" + usg.ToString("###,###,###,###") + " server-client ölçümü:" + (usg-ConnList.Last().Usage).ToString("###,###,###,###"));
            _tmp = GetWebString("http://192.168.137.1/SetUsage?Message=" + TelNo.HashMD5() + (SecurityCode + (TelNo.HashMD5() + Password).HashMD5() + (usg.ToString()).HashMD5()).HashMD5() + usg.ToString());
            if (ConnList.Last().Quota - usg <= 0)
              disconnectWifi();
            Set2TB(tb_Quota, (ConnList.Last().Quota - usg).ToString("###,###,###,###"));
            if (_tmp.Length == 32)
              SecurityCode = _tmp;
          }
          else
          {
            MessageBox.Show("Kullanım fazlası rapor edildi: " + ConnList.Last().Usage.ToString() + "<" + usg.ToString());
            ConnList.Last().Connected = false;
          }
          Thread.Sleep(1000);
        }
      }
    }

    public delegate void UpdateTextCallback(string message);
    public delegate void UpdateTextCallbacktb(TextBox tb, string str);
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
    private void Set2TB(TextBox tb, string str)
    {
      tb.Dispatcher.Invoke(
              new UpdateTextCallbacktb(this._set2tb),
              new object[] { tb, str }
          );
    }
    private void _set2tb(TextBox tb, string str)
    {
      tb.Text = str;
      Thread.Sleep(100);
    }
    private void wifiAfterConnect(String ssid)
    {
      Add2Log("wifiAfterConnect : Connected to " + ssid);
      if ((GetWebString("http://192.168.137.1") == "") && (ssid.StartsWith(wifiprefix)))
      {
        Add2Log("wifiAfterConnect : Wifi is not in the system");
      }
      else { 
        Add2Log("wifiAfterConnect : will connect to sys");
        connect2Sys();
      }
    }
    private void wifiAfterDisconnect(String ssid)
    {
      Add2Log("wifiAfterDisconnect : Disconnected from " + ssid);
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

    private void Grid_Loaded(object sender, RoutedEventArgs e)
    {
      wifiprefix = ""; // test amaçlı
      wlanIface.WlanConnectionNotification += wlanIface_WlanConnectionNotification;
      String wfname = connectedWifi();
      if ((!wfname.Equals("")))// && (!isSysConnected()))
        wifiAfterConnect(wfname);
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
    
    private void bt_Remove_Click(object sender, RoutedEventArgs e)
    {
      GetWebString("http://192.168.137.1/Remove?TelNo=" + tb_TelNo.Text);
    }

    private void bt_Register_Click(object sender, RoutedEventArgs e)
    {
      GetWebString("http://192.168.137.1/Register?TelNo=" + tb_TelNo.Text);
    }

    private void refreshNetworkListBox()
    {
      List<WifiType> wifis = new List<WifiType>();
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
          wifis.Remove(item);
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
        //MessageBox.Show(err.Message);
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

    private void connect2Sys()
    {
      Add2Log("connectSys : started");
      //TelNo = tb_TelNo.Text;
      //Password = tb_Password.Text;
      SetSecurityCode();
      if (SecurityCode.StartsWith("error:"))
      {
        Add2Log(SecurityCode.Substring(6, SecurityCode.Length));
        return;
      }
      else if (SecurityCode.Length != 32)
      {
        Add2Log("Hata: Güvenlik kodu dönmedi. Dönen değer:" + SecurityCode);
        return;
      };
      Add2Log("SecurityCode alındı : " + SecurityCode);
      String _tmp = GetWebString("http://192.168.137.1/Login?Evidence=" + TelNo.HashMD5() + (SecurityCode + (TelNo.HashMD5() + Password).HashMD5() + ("").HashMD5()).HashMD5());
      long _quota = 0;
      if ((_tmp.Length > 33) && (_tmp.Substring(32, 1) == ";"))
      {
        _quota = long.Parse(_tmp.Substring(33));
        SecurityCode = _tmp.Substring(0, 32);
        Set2TB(tb_Quota, _quota.ToString("###,###,###,###"));
        //tb_Quota.Text = Quota.ToString();
        Add2Log("Login başarılı");
      }
      else
      {
        _quota = 0;
        SecurityCode = "";
        tb_Quota.Text = _quota.ToString();
        Add2Log("Hata: Giriş yapılamadı. " + _tmp);
        return;
      }
      long stus = wlanIface.NetworkInterface.GetIPStatistics().BytesReceived;
      _tmp = GetWebString("http://192.168.137.1/ConnectUS?ClientEvidence=" + TelNo.HashMD5() + (SecurityCode + (TelNo.HashMD5() + Password).HashMD5() + ("").HashMD5()).HashMD5());
      if ((_tmp.Length > 33) && (_tmp.Substring(32, 1) == ";"))
      {
        SecurityCode = _tmp.Substring(0, 32);
        long cid = long.Parse(_tmp.Substring(33));
        Add2Log("Bağlantı sağlandı. ConnectionID : " + cid.ToString());
        ConnList.Add(new Conn() { ConnectionID = cid, Quota = _quota, ssid = connectedWifi(), Usage = 0, startUsage = stus, Connected = true});
      }
      else
        Add2Log("Bağlantı sağlanamadı. (" + _tmp + ")");

      Thread _thrGetSetUsage = new Thread(new ThreadStart(GetSetUsage));
      _thrGetSetUsage.IsBackground = true;
      _thrGetSetUsage.Start();
      Add2Log("GetSetUsage thread started!");
      Thread _thrUsageStats = new Thread(new ThreadStart(UsageStats));
      _thrUsageStats.Start();
      _thrUsageStats.IsBackground = true;
      Add2Log("UsageStats thread started!");
    }
  }
}
