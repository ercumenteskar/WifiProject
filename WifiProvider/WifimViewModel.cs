﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using My;
using System.Net.NetworkInformation;
using System.Windows;
using System.Diagnostics;
using System.Threading;
using ARSoft.Tools.Net.Dns;
using SharpPcap;
using System.Net;
using PacketDotNet;
using System.Globalization;
using System.Windows.Controls;
using Microsoft.Win32;
using System.Windows.Input;
using System.ServiceProcess;

namespace WifiProvider
{
  class WifimViewModel : INotifyPropertyChanged
  {
    public WifimViewModel()
    {
      String resource_data = Properties.Resources.Dict;
      String[] rows = resource_data.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
      dict = new MyDictionary(CultureInfo.CurrentUICulture.TwoLetterISOLanguageName, rows);
      _canLoginClick = true;
      if ((Registry.CurrentUser.OpenSubKey(_projectregaddr) != null) && (Registry.CurrentUser.OpenSubKey(_projectregaddr).GetValue("PEmailRemember") != null) && (Registry.CurrentUser.OpenSubKey(_projectregaddr).GetValue("PEmailRemember").ToString() != ""))
        Email = saes.DecryptToString(Registry.CurrentUser.OpenSubKey(_projectregaddr).GetValue("PEmailRemember").ToString());
      if ((Registry.CurrentUser.OpenSubKey(_projectregaddr) != null) && (Registry.CurrentUser.OpenSubKey(_projectregaddr).GetValue("PPassRemember") != null) && (Registry.CurrentUser.OpenSubKey(_projectregaddr).GetValue("PPassRemember").ToString() != ""))
        Password = saes.DecryptToString(Registry.CurrentUser.OpenSubKey(_projectregaddr).GetValue("PPassRemember").ToString());
      if (Email != "") cb_PEmailRememberIsChecked = true;
      if (Password != "") cb_PPassRememberIsChecked = true;
      if ((Registry.CurrentUser.OpenSubKey(_projectregaddr) != null) && (Registry.CurrentUser.OpenSubKey(_projectregaddr).GetValue("AutoLogin") != null))
        cb_AutoLogin = (Registry.CurrentUser.OpenSubKey(_projectregaddr).GetValue("AutoLogin").ToString() == "*");
      if ((Registry.CurrentUser.OpenSubKey(_projectregaddr) != null) && (Registry.CurrentUser.OpenSubKey(_projectregaddr).GetValue("AutoConnect") != null))
        cb_AutoConnect = (Registry.CurrentUser.OpenSubKey(_projectregaddr).GetValue("AutoConnect").ToString() == "*");
    }
    public class Client
    {
      public String Mac = "000000000000";
      public String Ip = "0.0.0.0";
      public String EmailHash = "";
      public String ConnectionID = ""; // hiç bir yerde aritmetik işleme sokmayacağım ve her yerde ToString çevriminden kurtulmak için String yapıyorum.
      public long Quota = 0;
      public long Usage = 0;
      public int WDHandle = 0;
      private bool _online = false;
      //public void refresh()
      //{
      //  IntPtr tmp = wdhndl;
      //  string others = "";
      //  //while clients
      //  wdhndl = WinDivertMethods.WinDivertOpen("(not (ip.DstAddr>=192.168.137.1 and ip.DstAddr<=192.168.137.255)) "+others, WINDIVERT_LAYER.WINDIVERT_LAYER_NETWORK, 1, WinDivertConstants.WINDIVERT_FLAG_DROP);
      //  if (tmp != IntPtr.Zero)
      //    WinDivertMethods.WinDivertClose(tmp);
      //}
      public bool Online
      {
        get { return _online; }
        set
        {
          _online = value;
          if (value)
          {
            AllowMac();
            this.LastQuery = DateTime.Now;
          }
          else
          {
            //ConnectionID = "0"; // Kota bittiği için bağlantı kesildikten sonra son hesabı kapatmak için setusage komutu geliyor o sırada connectionId lazım.
            BlockMac();
          }
        }
      }

      public void AllowMac()
      {
        if (this.WDHandle != 0)
          WinDivertMethods.WinDivertClose((IntPtr)this.WDHandle);
        this.WDHandle = 0;
      }

      public void BlockMac()
      {
        if (this.WDHandle != 0)
          AllowMac();
        IntPtr i = WinDivertMethods.WinDivertOpen("(ip.SrcAddr=" + Ip + " or ip.DstAddr=" + Ip + ") and (ip.SrcAddr!=" + providerIp + " and ip.DstAddr!=" + providerIp + ")", WINDIVERT_LAYER.WINDIVERT_LAYER_NETWORK, 1, WinDivertConstants.WINDIVERT_FLAG_DROP);
        if (Int32.Parse(i.ToString()) > 0)
          this.WDHandle = Int32.Parse(i.ToString());
      }

      public DateTime LastQuery;
      public bool isTest;
      // geçici
      public String Password = "e";
      public String SecurityCode = "";
      //
      public Client(String MacAddr, String IpAddr)
      {
        this.WDHandle = 0;
        if ((MacAddr.Length == 12) && (MacAddr.IndexOf(":") < 0))
          MacAddr = MacAddr.Substring(0, 2) + MacAddr.Substring(2, 2) + MacAddr.Substring(4, 2) + MacAddr.Substring(6, 2) + MacAddr.Substring(8, 2) + MacAddr.Substring(10, 2);
        this.Mac = MacAddr;
        this.Ip = IpAddr;
        this.Online = false;
      }
    }

    private bool _canLoginClick;
    private RelayCommand _bt_LoginCommand;
    public ICommand bt_LoginCommand
    {
      get
      {
        if (_bt_LoginCommand == null)
        {
          _bt_LoginCommand = new RelayCommand(p => LoginCommand(), p => _canLoginClick);
        }
        return _bt_LoginCommand;
      }
    }

    public bool canConnect { get { return Logged || Connected; } }
    //private bool canConnect(object obj) { return _canConnect; } // ekran güncellemesi takılıyordu, mouse u üzerine getirene kadar isenable güncellenmiyordu
    private RelayCommand _bt_ConnectCommand;
    public ICommand bt_ConnectCommand
    {
      get
      {
        _bt_ConnectCommand = _bt_ConnectCommand ?? new RelayCommand(ConnectCommand); // , canConnect
        return _bt_ConnectCommand;
      }
    }
    public bool canLogin { get { return Email.isValidEmail() && (Password != ""); } }

    WifimModel m = new WifimModel();
    public event PropertyChangedEventHandler PropertyChanged;
    private void OnPropertyChanged(String property) { PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property)); }
    public WifiService.Service1Client WCF = new WifiService.Service1Client();
    private DnsServer dnsServer;
    private ICaptureDevice device;
    public List<Client> clients = new List<Client>();
    public static IntPtr wdhndl = IntPtr.Zero;
    //BackgroundWorker bwStopSystem;
    //BackgroundWorker bwStartSystem;
    int GetSetUsageInterval = 3000;
    public MyDictionary dict;
    private static String providerIp = "";
    private MyWebServer ws;
    private bool wsStarted = false;
    private String SecurityCode = "";
    public static string _projectName = "Wifi";
    public string _projectregaddr = "Software\\" + _projectName;
    string langCode = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
    SimpleAES saes = new SimpleAES();
    Thread thrGetSetUsage;
    private bool? _cb_PEmailRememberIsChecked = false;
    public bool? cb_PEmailRememberIsChecked { get { return _cb_PEmailRememberIsChecked; } set { _cb_PEmailRememberIsChecked = value; OnPropertyChanged(nameof(cb_PEmailRememberIsChecked)); } }
    private bool? _cb_PPassRememberIsChecked = false;
    public bool? cb_PPassRememberIsChecked { get { return _cb_PPassRememberIsChecked; } set { _cb_PPassRememberIsChecked = value; OnPropertyChanged(nameof(cb_PPassRememberIsChecked)); } }
    private bool? _cb_AutoLogin = false;
    public bool? cb_AutoLogin { get { return _cb_AutoLogin; } set { _cb_AutoLogin = value; OnPropertyChanged(nameof(cb_AutoLogin)); } }
    private bool? _cb_AutoConnect = false;
    public bool? cb_AutoConnect { get { return _cb_AutoConnect; } set { _cb_AutoConnect = value; OnPropertyChanged(nameof(cb_AutoConnect)); } }
    private String password = "";
    public String Password { get { return password; } set { password = value; OnPropertyChanged(nameof(Password)); OnPropertyChanged(nameof(canLogin)); } }
    public String Email { get { return m.Email; }
      set { m.Email = value; OnPropertyChanged(nameof(Email)); OnPropertyChanged(nameof(canLogin)); } }
    public String EmailHash { get { return Email.HashMD5(); } }
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
    public string bt_LoginContent
    {
      get { return Logged ? "Logout" : "Login"; }
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
        if (!value) Quota = 0;
      }
    }
    public bool? _tb_PasswordIsEnabled = true;
    public bool? tb_PasswordIsEnabled { get { return _tb_PasswordIsEnabled; } set { _tb_PasswordIsEnabled = value; OnPropertyChanged(nameof(tb_PasswordIsEnabled)); } }
    public bool? _tb_EmailIsEnabled = true;
    public bool? tb_EmailIsEnabled { get { return _tb_EmailIsEnabled; } set { _tb_EmailIsEnabled = value; OnPropertyChanged(nameof(tb_EmailIsEnabled)); } }
    private long quota = 0;
    public long Quota { get { return quota < 0 ? 0 : quota; } set { quota = value; OnPropertyChanged(nameof(QuotaStr)); } }
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
    private int waitcount = 0;
    private int Waitcount { get { return waitcount; } set { waitcount = value; OnPropertyChanged(nameof(MainGridVisibility)); } }
    public Visibility MainGridVisibility { get { return Waitcount == 0 ? Visibility.Visible : Visibility.Hidden; } }
    private void RuninThread(DoWorkEventHandler work, RunWorkerCompletedEventHandler afterThat)
    {
      ShowWait();
      BackgroundWorker bw = new BackgroundWorker();
      bw.DoWork += work;
      if (afterThat != null)
        bw.RunWorkerCompleted += afterThat;// delegate (object sender, RunWorkerCompletedEventArgs e) { StartSystem(false); HideWait(); };
      bw.RunWorkerCompleted += delegate (object sender, RunWorkerCompletedEventArgs e) { HideWait(); };
      bw.RunWorkerAsync();
    }
    public void StartSystem(bool stopFirst = true) //  = true
    {
      if (stopFirst)
        RuninThread(new DoWorkEventHandler(thrStopSystem), delegate (object sender, RunWorkerCompletedEventArgs e) { StartSystem(false); }); // delegate (object sender, RunWorkerCompletedEventArgs e) { StartSystem(false); HideWait(); }
      else
        RuninThread(new DoWorkEventHandler(thrStartSystem), null); // delegate (object sender, RunWorkerCompletedEventArgs e) { StartSystem(false); HideWait(); }
    }
    private void StopSystem()
    {
      RuninThread(new DoWorkEventHandler(thrStopSystem), null); // delegate (object sender, RunWorkerCompletedEventArgs e) { StartSystem(false); HideWait(); }
    }

    private void thrStartSystem(object sender, DoWorkEventArgs e)
    {
      String source = "";
      String hotspot = "";
      if (!Logged)
        return;
      if (wdhndl != IntPtr.Zero)
        WinDivertMethods.WinDivertClose(wdhndl);
      List<NetworkInterface> nicx = new List<NetworkInterface>();
      foreach (var nic in IcsManager.GetAllIPv4Interfaces())
        if ((nic.OperationalStatus == OperationalStatus.Up && nic.NetworkInterfaceType != NetworkInterfaceType.Tunnel && nic.NetworkInterfaceType != NetworkInterfaceType.Loopback))
          nicx.Add(nic);
      if (nicx.Count > 1)
      {
        MessageBox.Show("Paylaşılabilecek birden fazla internet kaynağı bulundu. Lütfen kontrol edip programı tekrar deneyin.");
        Application.Current.Shutdown();
      }
      else if (nicx.Count == 1)
        source = nicx[0].Id;
      else
      {
        MessageBox.Show("İnternet kaynağı bulunamadı. Lütfen kontrol edip programı tekrar deneyin.");
        Application.Current.Shutdown();
      }
      Netsh("wlan set hostednetwork mode=allow ssid=wifix key=erci1234"); //  key=ercierci
      Add2Log("Hotspot Created");
      nicx.Clear();
      foreach (var nic in IcsManager.GetAllIPv4Interfaces())
        if ((nic.Description.Contains("Virtual") && nic.Id != source && nic.NetworkInterfaceType != NetworkInterfaceType.Tunnel && nic.NetworkInterfaceType != NetworkInterfaceType.Loopback))
          nicx.Add(nic);
      if (nicx.Count > 1)
      {
        MessageBox.Show("Barındırılmış Ağ seçilemedi. Bilgisayarınızda birden fazla sanal wifi sürücüsü görünüyor.");
        Application.Current.Shutdown();
      }
      else if (nicx.Count == 1)
        hotspot = nicx[0].Id;
      else
      {
        MessageBox.Show("Paylaşım için gerekli sürücüler bulunamadı. Lütfen kontrol edip programı tekrar çalıştırın.");
        Application.Current.Shutdown();
      }
      EnableICS(source, hotspot, true);
      Add2Log("Internet Connection Sharing Enabled");
      Netsh("wlan start hostednetwork");
      Add2Log("HotSpot Opened");

      if ((thrGetSetUsage != null) && (thrGetSetUsage.IsAlive))
        thrGetSetUsage.Abort();
      thrGetSetUsage = new Thread(new ThreadStart(GetSetUsage));
      thrGetSetUsage.IsBackground = true;
      thrGetSetUsage.Start();
      Add2Log("GetSetUsage thread started!");

      #region DNS SERVER
      dnsServer = new DnsServer(IPAddress.Any, 10, 10);
      dnsServer.QueryReceived += OnQueryReceived;
      //dnsServer.ClientConnected += OnClientConnected;
      try
      {
        dnsServer.Start();
      }
      catch (Exception ex)
      {
        if (ex is System.Net.Sockets.SocketException)
        {
          ServiceController service = new ServiceController("Dnscache");
          try
          {
            int millisec1 = Environment.TickCount;
            TimeSpan timeout = TimeSpan.FromMilliseconds(5000);

            service.Stop();
            service.WaitForStatus(ServiceControllerStatus.Stopped, timeout);

            // count the rest of the timeout
            int millisec2 = Environment.TickCount;
            timeout = TimeSpan.FromMilliseconds(5000 - (millisec2 - millisec1));

            service.Start();
            service.WaitForStatus(ServiceControllerStatus.Running, timeout);
          }
          catch
          {
            throw;
          }
          MessageBox.Show("Dns Server açılamadı: SocketException");
        }
        else throw;
      }
      Add2Log("DNS Server running");
      #endregion
      //device4Arp = (SharpPcap.LibPcap.LibPcapLiveDevice)CaptureDeviceList.Instance.FirstOrDefault(x => x.Name.Contains(hotspot));
      #region Capture Device // start olmadan önce capture başlarsa capture çalışmıyor...
      //CaptureDeviceList.Instance.Refresh();
      device = CaptureDeviceList.Instance.FirstOrDefault(x => x.Name.Contains(hotspot));// devices[devices.Count - 1];
      var v = ((SharpPcap.WinPcap.WinPcapDevice)device).Addresses.First(x => x.Addr.ipAddress.ToString().Contains("."));
      if (v != null)
        providerIp = v.Addr.ipAddress.ToString(); // Anlamadığım bir sebepten dolayı bazen 0.0.0.0 geliyor. Ya da 192.168.0.1 // Ostoto kurup kaldırınca böyle oldu...
      else
      {
        MessageBox.Show("ProviderIp alınamadı");
        Application.Current.Shutdown();
      }
      //if (!providerIp.Contains(".")) 
      //  providerIp = GetValue(device.ToString().Split(Environment.NewLine.ToCharArray(), StringSplitOptions.RemoveEmptyEntries)[8], "Addr:      ");
      //if (!providerIp.Contains("."))
      //  providerIp = "192.168.137.1";
      device.OnPacketArrival += new SharpPcap.PacketArrivalEventHandler(device_OnPacketArrival);
      device.Open(DeviceMode.Normal, 1000); //DeviceMode.Promiscuous
      device.StartCapture();
      Add2Log("Device Listening");
      #endregion
      #region WEB SERVER
      //MyWebServer create etmeden önce beklemezsem hata veriyor (Denetim Masası/Ağ ve Paylaşım Merkezi/HotSpot un erişim türü İnternet olduktan sonra MyWebServer hatasız create oluyor. Burada algılamayı bulamadım, şimdilik 2 sn bekliyorum.
      //Henüz 192.168.137.1 ip adresini alamadığı için hata veriyor. Bunu kontrol edip ip aldıktan sonra devam edersen 4 saniyeden az beklersin, hem de hata almaman garanti olur.
      //device.
      //Thread.Sleep(4000);
      /*
      Thread _thrwsRun = new Thread(new ThreadStart(wsRun));
      _thrwsRun.IsBackground = true;
      _thrwsRun.Start();
      */
      Ping pingSender = new Ping();
      //IPAddress address = IPAddress.Loopback;
      PingReply reply = pingSender.Send(providerIp, 10);
      while (reply.Status != IPStatus.Success) reply = pingSender.Send(providerIp, 10);
      ws = new MyWebServer(SendResponse, "http://" + providerIp + ":80/");
      ws.Run();
      wsStarted = true;
      Add2Log("WEB Server running : http://" + providerIp + ":80/");
      Connected = true;
      //gp = IcsManager.FindConnectionByIdOrName(hotspot);
      //device = CaptureDeviceList.Instance.FirstOrDefault(x => x.Name.Contains(hotspot));// devices[devices.Count - 1];
      //sss = device.ToString();
      #endregion
    }

    private void thrStopSystem(object sender, DoWorkEventArgs e)
    {
      Netsh("wlan stop hostednetwork");
      DisableICS();
      Netsh("wlan set hostednetwork mode=disallow"); //  key=ercierci
                                                     //      Netsh("wlan set hostednetwork mode=allow ssid=wifix key=erci1234"); //  key=ercierci
      if ((device != null) && (device.Started))
      {
        device.StopCapture();
        device.Close();
      }
      if ((ws != null) && (wsStarted))
      {
        ws.Stop();
        wsStarted = false;
      }
      if (dnsServer != null)
        try { dnsServer.Stop(); } catch (Exception) { }

      if (wdhndl != IntPtr.Zero)
        WinDivertMethods.WinDivertClose(wdhndl);
      if ((thrGetSetUsage != null) && (thrGetSetUsage.IsAlive))
        thrGetSetUsage.Abort();
      Connected = false;
      Add2Log("Sistem kapatıldı.");
    }

    public void ShowWait()
    {
      Waitcount++;
    }

    public void HideWait()
    {
      Waitcount--;
    }

    async Task OnQueryReceived(object sender, QueryReceivedEventArgs e)
    {
      DnsMessage query = e.Query as DnsMessage;
      if (query == null) return;
      DnsMessage response = query.CreateResponseInstance();
      if (response.Questions.Any())
      {
        DnsQuestion question = response.Questions[0];
        DnsMessage upstreamResponse = await DnsClient.Default.ResolveAsync(question.Name, question.RecordType, question.RecordClass);
        if (upstreamResponse == null)
          return;
        response.AdditionalRecords.AddRange(upstreamResponse.AdditionalRecords);
        response.ReturnCode = ReturnCode.NoError;
        String ipa = e.RemoteEndpoint.Address.ToString();
        if ((ipa == providerIp) || (yetkili(clients.Find(x => x.Ip == ipa))))
        {
          response.AnswerRecords.AddRange(upstreamResponse.AnswerRecords
                  .Where(w => !(w is ARecord))
                  .Concat(
                      upstreamResponse.AnswerRecords
                          .OfType<ARecord>()
                          .Select(a => new ARecord(a.Name, a.TimeToLive, a.Address)))); // some local ip address

        }
        else
        {
          //          Add2Log("ENGELLENDİ");
          response.AnswerRecords.AddRange(
              upstreamResponse.AnswerRecords
                  .Where(w => !(w is ARecord))
                  .Concat(
                      upstreamResponse.AnswerRecords
                          .OfType<ARecord>()
                          //                          .Select(a => new ARecord(a.Name, a.TimeToLive, IPAddress.Parse("192.168.137.1"))) // some local ip address
                          .Select(a => new ARecord(a.Name, 1, IPAddress.Parse(providerIp))) // some local ip address
                  )
          );
        }

        e.Response = response;
      }
    }
    // */
    void device_OnPacketArrival(object sender, CaptureEventArgs e)
    {
      String devmac = device.MacAddress.ToString();
      var packet = Packet.ParsePacket(e.Packet.LinkLayerType, e.Packet.Data);
      if (packet is EthernetPacket)
      {
        var eth = ((EthernetPacket)packet);
        String s_mac = eth.SourceHwAddress.ToString();
        String d_mac = eth.DestinationHwAddress.ToString();
        String d_ip = "", s_ip = "";
        IpPacket ip = (IpPacket)packet.Extract(typeof(IpPacket));
        ARPPacket arp = (ARPPacket)packet.Extract(typeof(ARPPacket));
        if (arp != null) return;
        if (ip != null)
        {
          d_ip = ip.DestinationAddress.ToString();
          s_ip = ip.SourceAddress.ToString();
        }
        else return;
        if ((d_mac == devmac) || (d_ip == providerIp) || (d_mac == "")
          || (!d_ip.StartsWith(providerIp.Substring(0, providerIp.LastIndexOf('.'))))
          || (d_ip.EndsWith(".255"))) return;
        long PackSize = 0;
        if (s_ip != providerIp)
          PackSize = eth.Bytes.Length - eth.Header.Length;
        //e.Packet.Data.Length; 4.093.487
        //eth.BytesHighPerformance.ActualBytes().Length; 3.820.088
        //eth.BytesHighPerformance.BytesLength; 3.990.933
        //eth.Bytes.Length - eth.Header.Length; 516.960
        //büyük%10 civarı
        //var ipv4 = (PacketDotNet.IPv4Packet)packet.Extract(typeof(PacketDotNet.IPv4Packet));
        //var tcp = (PacketDotNet.TcpPacket)packet.Extract(typeof(PacketDotNet.TcpPacket));
        //var udp = (PacketDotNet.UdpPacket)packet.Extract(typeof(PacketDotNet.UdpPacket));
        Client cl = clients.Find(x => (x.Mac == d_mac && x.Ip == d_ip));
        if (cl == null)
        {
          cl = clients.Find(x => (x.Mac == d_mac));
          if (cl == null)
          {
            cl = clients.Find(x => (x.Ip == d_ip));
            if (cl != null) // Listedeki bir başka cihazın ip adresini almış yeni bir cihaz.
              RemoveClient(cl);
          }
          else // Mac adresinin eski ip adresi ile kaydı var.
            RemoveClient(cl);
          cl = new Client(d_mac, d_ip);
          clients.Add(cl);
          Add2Log(cl.Mac + "(" + cl.Ip + ") Added (" + GetHostName(cl.Ip) + ")");
          //SetTextBox(tb_ip, cl.Ip);
        }
        if (cl == null) return;
        //if ((cl.Ip != d_ip) && (d_ip.StartsWith(providerIp.Substring(0, providerIp.LastIndexOf('.')) + "."))) // Bu mac adresi için önceden yapılan kayıttan sonra ip adresi değişti ise
        //{
        //RemoveClient(cl);        //  return;
        //}
        if (cl.Online)
        {
          cl.Usage += PackSize;
          //Quota += PackSize; Bunu kapattım çünkü tam doğru rakam bu değil artık. Artık clientlardan gelen rakamı gönderiyoruz, o yüzden küçük farklılıklar olabilir.
          if (cl.Usage >= cl.Quota)
          {
            cl.Online = false; // RemoveClient(_client); listeden kaldırmasın, sadece kullanıcı programını kapattı. listeden silince provider ekranında tekrar added görünüyor...
            Add2Log(dict.GetMessage(11, cl.Mac + "¶" + dict.GetMessage(14)));
            return;
          }
          //Add2Log(cl.Mac + " : " + (cl.Usage / 1024).ToString("###,###,##0"));
          //Console.WriteLine(cl.Mac + " : " + (cl.Usage / 1024).ToString("###,###,##0") + " Kalan : " + ((cl.Quota - cl.Usage) / 1024).ToString("###,###,##0"));
        }
      }
      else
        return;
    }
    public string SendResponse(HttpListenerRequest request)
    {
      String url = request.Url.ToString();
      String command = "";
      String _temp = "";
      if ((url.Contains("/")) && (url.Contains("?")) && (url.IndexOf("/") < url.IndexOf("?")))
        command = url.ReverseString().OrtasiniGetir("?", "/").ReverseString();
      else if (url.Contains("/"))
        command = url.Split('/').Last();
      if (command == "ping")
      {
        return "pong";
      }
      string RemoteIp = request.RemoteEndPoint.Address.ToString();
      ///* // Mac a göre bulurken hem cmd işin içine giriyor (içime sinmedi) hem de kendi makinamdan test edemiyordum...
      string reqmac = "";
      if (RemoteIp == providerIp) //
        reqmac = "";//"888888888887";
      else
        reqmac = GetMacAddress(RemoteIp); //null döndüğü için patlıyor....
      Client _client = clients.Find(x => x.Mac == reqmac);
      if (_client == null)
        return "null";
      if (command == "Remove")
      {
        String Email = url.GetUrlParam("Email");
        WCF.Remove(Email);
        return "<a href='Register?Email='>Register</a>";
      }
      else if (command == "Register")
      {
        _temp = WCF.Register(url.GetUrlParam("Email"), url.GetUrlParam("Pass"), url.GetUrlParam("AFQ"), url.GetUrlParam("LangCode"));
        if (mfn.isValidHexString(_temp, 32))
          _client.SecurityCode = _temp;
        return _temp;
      }
      else if (command == "GetSecurityCode")
      {
        String _EmailHash = url.GetUrlParam("EmailHash");
        _temp = WCF.GetSecurityCode(_EmailHash, url.GetUrlParam("AFQ"), url.GetUrlParam("LangCode"));
        if (mfn.isValidHexString(_temp, 32))
        {
          _client.EmailHash = _EmailHash;
          _client.SecurityCode = _temp;
          if (url.GetUrlParam("testercument") == "1")
            return "<a href='Login?Evidence=" + _client.EmailHash + (_client.SecurityCode + (_EmailHash + _client.Password).HashMD5() + ("").HashMD5()).HashMD5() + "&testercument=1'>Login</a>";
          else
            return _temp;
        }
        else return _temp;
      }
      else if (command == "Login")
      {
        _temp = WCF.Login(url.GetUrlParam("Evidence"), url.GetUrlParam("AFQ"), url.GetUrlParam("LangCode"));
        long qt = 0;
        if ((_temp.Length >= 34) && mfn.isValidHexString(_temp.Substring(0, 32)) && (_temp[32] == ';') && (long.TryParse(_temp.Substring(33), out qt)))//        if (_temp.Length >= 34)
        {
          _client.Quota = qt;
          _client.SecurityCode = _temp.Substring(0, 32);
          if (url.GetUrlParam("testercument") == "1")
            return "<a href='ConnectUS?ClientEvidence=" + _client.EmailHash + (_client.SecurityCode + (_client.EmailHash + _client.Password).HashMD5() + ("").HashMD5()).HashMD5() + "&testercument=1'>ConnectUS</a>";
          else
            return _temp;
        }
        else return _temp;
      }
      else if (command == "ConnectUS")
      {
        if (_client.Quota == 0) return "¶E:" + dict.GetMessage(18);
        String ClientEvidence = url.GetUrlParam("ClientEvidence");
        _temp = myEvidence();
        _temp = WCF.ConnectUS(ClientEvidence, _temp, url.GetUrlParam("AFQ"), url.GetUrlParam("LangCode"));
        long qt;
        if ((_temp.Length >= 67) &&
          mfn.isValidHexString(_temp.Substring(0, 32)) && (_temp[64] == ';') &&
          mfn.isValidHexString(_temp.Substring(65, 32)) && (_temp[129] == ';') &&
          (long.TryParse(_temp.Substring(130), out qt))) // if (_temp.Length >= 67)
        {
          _client.SecurityCode = _temp.Substring(0, 32);
          SecurityCode = _temp.Substring(65, 32);
          _client.ConnectionID = qt.ToString();
          _client.Usage = 0;
          Add2Log("BAĞLANAN VAR. CONNECTION ID : " + _client.ConnectionID.ToString());
          _client.isTest = (url.GetUrlParam("testercument") == "1");
          _client.Online = true;
          if (_client.isTest)
            return "BAĞLANTI SAĞLANDI";//"<a href='WhatsUp?EmailHash=" + _client.EmailHash + "'&testercument=1>WhatsUp</a>";
          else
            return _client.SecurityCode + ";" + _client.ConnectionID.ToString(); // "<a href='WhatsUp?EmailHash=" + _client.EmailHash + "'>WhatsUp</a>";
          //          return qry.Substring(0, 32) + ";" + qry.Substring(66); // Client SecurityCode+";"+ConnectionID
        }
        else return _temp;
      }
      //else if (command == "GetUsage")
      //{
      //  if (!_client.Online)
      //    return "null";
      //  String _emh = url.GetUrlParam("EmailHash");
      //  String _cid = url.GetUrlParam("ConnectionID");
      //  //if (_client.ConnectionID == "")
      //  //  return "error:No Connection on " + _client.Ip + " (" + _client.Mac + ")"; else 
      //  if ((_client.EmailHash.Equals(_emh)) && (_client.ConnectionID.ToString().Equals(_cid)) && (_client.ConnectionID.ToString() != ""))
      //  {
      //    //return "<a href='SetUsage?Message=" + _client.EmailHash + (_client.SecurityCode + (EmailHash + _client.Password).HashMD5() + (usg.ToString()).HashMD5()).HashMD5() + usg.ToString() + "'>SetUsage</a>";
      //    Add2Log("KULLANIM BILGISI GONDERILDI. KULLANIM : " + _client.Usage.ToString() + " CONNECTION ID : " + _client.ConnectionID.ToString());
      //    if (_client.Usage > _client.Quota)
      //    {
      //      _client.ConnectionID = "";
      //      return "error:Insufficent funds!!!";
      //    }
      //    else
      //      return _client.Usage.ToString() + ";" + _client.ConnectionID.ToString();
      //  }
      //  else return "¶E:" + _client.EmailHash + "<>" + _emh + " || " + _client.ConnectionID.ToString() + "<>" + _cid;
      //}
      else if (command == "SetUsage")
      {
        //if (!_client.Online) return "null"; // Adam belki kotası dolduktan sonra fatuasını ödemek istiyor?
        _temp = url.GetUrlParam("Message");
        long amount = 0;
        if ((_temp.Length < 65)
          || (!mfn.isValidHexString(_temp))
          || (!long.TryParse(_temp.Substring(64), out amount)))
          return "null";
        String EmailHash = _temp.Substring(0, 32);
        if (_client.EmailHash == EmailHash)
        {
          //long amount = _temp.Substring(64);
          //if (amount > _client.Quota) amount = _client.Quota;// imza, gelen amount değerine göre, o yüzden değiştiremem...
          //Add2Log("Gelen amount : " + amount.ToString("###,###,###") + " _temp:" + _temp);
          _temp = WCF.SetUsage(_temp, myEvidence(amount.ToString()), long.Parse(_client.ConnectionID), url.GetUrlParam("AFQ"), langCode);
          if (((_client.Usage > amount * 1.001) && (_client.Usage > amount + 1024 * 1024)) || (amount >= _client.Quota)) // 1/1000 den fazla fark varsa ve bu fark 1MB ı aşmışsa...
            return "¶E:Kotanız bittiği için bağlantınız sonlandırıldı.";

          if ((_temp.Length == 65) && (_temp[32] == ';') && (mfn.isValidHexString(_temp.Substring(0, 32))) && (mfn.isValidHexString(_temp.Substring(33, 32))))
          {
            _client.LastQuery = DateTime.Now;
            _client.SecurityCode = _temp.Substring(0, 32);
            SecurityCode = _temp.Substring(33, 32);
            Add2Log("KULLANIM BILGISI ONAYI ALINDI. KULLANIM : " + amount.ToString() + " CONNECTION ID : " + _client.ConnectionID.ToString());
            return _client.SecurityCode;
          }
          else return _temp;
        }
        else return "¶E:" + _client.EmailHash + "<>" + EmailHash;
      }
      else if (command == "Disconnect")
      {
        _client.Online = false;// RemoveClient(_client); listeden kaldırmasın, sadece kullanıcı programını kapattı. listeden silince provider ekranında tekrar added görünüyor...
        Add2Log(_client.Mac + " BAĞLANTISINI SONLANDIRDI. CONNECTION ID : " + url.GetUrlParam("ConnectionID"));
        return "";// "BAĞLANTI SONLANDIRILDI. CONNECTION ID : " + url.GetUrlParam("ConnectionID");
      }
      else
        return "<a href='\\" + providerIp + @"\" + _projectName + @"\WifiWPFClient.exe'>Download Client</a>";
      //      return "<a href='GetSecurityCode?EmailHash=5CAA8CD9E281E9A815AD88C79DB734FF&testercument=1'>GetSecurityCode</a>";
      //      else return "<a href='Remove?Email=5448302899'>DELETE 5448302899</a>";
      //else return "WELLCOME !! YOU HAVE TO RUN OUR CLIENT APPLICATON";
    }

    private void RemoveClient(Client cl)
    {
      if (cl != null)
      {
        cl.Online = true; // Online yapalım ki bloke kalksın, bu ip adresini alacak yeni cihazlar bloke olmasın.
        clients.Remove(cl);
      }
    }
    private string GetHostName(string ipAddress)
    {
      try
      {
        return Dns.GetHostEntry(ipAddress).HostName;
      }
      catch (Exception)
      {
        return "";
      }
    }
    public string GetMacAddress(string ipAddress)
    {
      string macAddress = "";
      /*
            System.Net.IPAddress ip;
            if (System.Net.IPAddress.TryParse(ipAddress, out ip))
            {
              ARP arper = new ARP(device4Arp);
              // print the resolved address or indicate that none was found
              var resolvedMacAddress = arper.Resolve(ip);
              if (resolvedMacAddress != null)
                macAddress = resolvedMacAddress.ToString();
            }
      */
      ///*
      foreach (var arp in GetARPResult(ipAddress).Split(new char[] { '\n', '\r' }))
      {
        // Parse out all the MAC / IP Address combinations
        if (!string.IsNullOrEmpty(arp))
        {
          var pieces = (from piece in arp.Split(new char[] { ' ', '\t' })
                        where !string.IsNullOrEmpty(piece)
                        select piece).ToArray();
          if ((pieces.Length == 3) && (pieces[0] == ipAddress))
            macAddress = pieces[1].Replace("-", "").ToUpper();
        }
      }
      //*/
      return macAddress;
    }
    private static string GetARPResult(string ipAddress = "")
    {
      Process p = null;
      string output = string.Empty;

      try
      {
        p = Process.Start(new ProcessStartInfo("arp", "-a " + ipAddress)
        {
          CreateNoWindow = true,
          UseShellExecute = false,
          RedirectStandardOutput = true
        });

        output = p.StandardOutput.ReadToEnd();

        p.Close();
      }
      catch (Exception ex)
      {
        throw new Exception("ARP error", ex);
      }
      finally
      {
        if (p != null)
        {
          p.Close();
        }
      }

      return output;
    }
    private void Add2Log(string msg)
    {
      Console.WriteLine(msg);
    }
    private String Netsh(String args)
    {
      return CmdExec("netsh.exe", args);
    }
    private String CmdExec(String app, String args)
    {
      using (Process p1 = new Process())
      {
        p1.StartInfo.FileName = app;
        p1.StartInfo.Arguments = args;
        p1.StartInfo.UseShellExecute = false;
        p1.StartInfo.RedirectStandardOutput = true;
        p1.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
        p1.StartInfo.CreateNoWindow = true;
        p1.StartInfo.Verb = "runas";
        p1.Start();
        return p1.StandardOutput.ReadToEnd();
      }
    }
    void EnableICS(string shared, string home, bool force)
    {
      var connectionToShare = IcsManager.FindConnectionByIdOrName(shared);
      var homeConnection = IcsManager.FindConnectionByIdOrName(home);
      var currentShare = IcsManager.GetCurrentlySharedConnections();
      if ((currentShare.HomeConnection != null) && (currentShare.SharedConnection != null))
      {
        string hs = IcsManager.GetProperties(currentShare.HomeConnection).Guid;
        string ss = IcsManager.GetProperties(currentShare.SharedConnection).Guid;
        if ((currentShare.Exists) && (hs + ss != home + shared))
          DisableICS();
      }
      if (!IcsManager.GetCurrentlySharedConnections().Exists)
        IcsManager.ShareConnection(connectionToShare, homeConnection);
    }
    void DisableICS()
    {
      if (IcsManager.GetCurrentlySharedConnections().Exists)
      {
        var share = IcsManager.GetCurrentlySharedConnections();
        if (share.SharedConnection != null)
          IcsManager.GetConfiguration(share.SharedConnection).DisableSharing();
        if (share.HomeConnection != null)
          IcsManager.GetConfiguration(share.HomeConnection).DisableSharing();
      }
    }
    private void GetSetUsage()
    {
      while (true)
      {
        for (int i = clients.Count - 1; i >= 0; i--)
        {
          var item = clients[i];
          if ((item.Online) && ((DateTime.Now - item.LastQuery).TotalMilliseconds > GetSetUsageInterval * 2))
          {
            item.Online = false;
            Add2Log(dict.GetMessage(11, item.Mac + "¶" + dict.GetMessage(13))); // + (GetSetUsageInterval / 1000 * 2).ToString() + " saniyedir yanıt vermediği için bağlantısı kapatıldı.");
          }
        }
        Thread.Sleep(GetSetUsageInterval);
      }
    }
    private String myEvidence(String Mesaj = "")
    {
      if (!SetSecurityCode())
        return "";
      else
        return EmailHash + (SecurityCode + (EmailHash + Password).HashMD5() + Mesaj.HashMD5()).HashMD5() + Mesaj;
    }
    private bool yetkili(Client cl)
    {
      return ((cl != null) && (cl.Online));
    }
    public bool SetSecurityCode()
    {
      SecurityCode = WSRunner("/GetSecurityCode?EmailHash=" + EmailHash);
      if (SecurityCode == null) return false;
      return !CheckResult(ref SecurityCode);
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
        }
        else // Please check your internet connection.
          AFQ = "¶E:" + dict.GetMessage(6);
      }
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
    private void Login()
    {
      RuninThread(new DoWorkEventHandler(thrLogin), null); // delegate (object sender, RunWorkerCompletedEventArgs e) { StartSystem(false); HideWait(); }
    }

    private void thrLogin(object sender, DoWorkEventArgs e)
    {
      Quota = 0;
      if (!SetSecurityCode()) return;
      Add2Log("SecurityCode : " + SecurityCode);
      String _tmp = WSRunner("/Login?Evidence=" + (EmailHash + (SecurityCode + (EmailHash + Password).HashMD5() + ("").HashMD5()).HashMD5()));
      //      String _tmp = Runner(() => WCF.Login(Email.HashMD5() + (SecurityCode + (Email.HashMD5() + Password).HashMD5() + ("").HashMD5()).HashMD5(), langCode, AFQ));
      if (_tmp == null) return;
      //if (CheckResult(ref _tmp)) return;
      if ((_tmp.Length > 33) && (_tmp.Substring(32, 1) == ";"))
      {
        Quota = long.Parse(_tmp.Substring(33));
        SecurityCode = _tmp.Substring(0, 32);
        Logged = true;
        if (cb_AutoConnect == true)
          Connect();
        RememberAction();
      }
    }

    public void RememberAction(object sender = null)
    {
      CheckBox cb = null;
      if (sender != null) cb = (CheckBox)sender;
      if ((cb == null) || (cb.Name == "cb_PEmailRemember"))
      {
        if ((cb_PEmailRememberIsChecked == true) && (Logged))
          Registry.CurrentUser.CreateSubKey(_projectregaddr).SetValue("PEmailRemember", saes.EncryptToString(Email));
        else if (cb_PEmailRememberIsChecked == false) Registry.CurrentUser.CreateSubKey(_projectregaddr).SetValue("PEmailRemember", "");
      }
      if ((sender == null) || (cb.Name == "cb_PPassRemember"))
      {
        if ((cb_PPassRememberIsChecked == true) && (Logged))
          Registry.CurrentUser.CreateSubKey(_projectregaddr).SetValue("PPassRemember", saes.EncryptToString(Password));
        else if (cb_PPassRememberIsChecked == false) Registry.CurrentUser.CreateSubKey(_projectregaddr).SetValue("PPassRemember", "");
      }
      if ((sender != null) && ((cb.Name == "cb_AutoLogin") || (cb.Name == "cb_AutoConnect")))
        Registry.CurrentUser.CreateSubKey(_projectregaddr).SetValue(((CheckBox)sender).Name.Substring(3), (((CheckBox)sender).IsChecked == true ? "*" : ""));
    }
    public void Logout()
    {
      Logged = false;
      Disconnect();
    }

    private void LoginCommand()
    {
      if (Logged)
        Logout();
      else
        Login();
    }

    private void ConnectCommand(object obj)
    {
      if (Connected)
        Disconnect();
      else
        Connect();
    }

    private void Connect()
    {
      StartSystem();
    }

    public void Disconnect()
    {
      StopSystem();
    }

  }
}
