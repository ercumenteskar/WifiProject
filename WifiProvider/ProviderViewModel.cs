﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using My;
using System.Net.NetworkInformation;
using System.Diagnostics;
using System.Threading;
using ARSoft.Tools.Net.Dns;
using SharpPcap;
using System.Net;
using PacketDotNet;
using System.Globalization;
using System.ServiceProcess;

namespace WifiSolution.WifiProvider
{
  public class ProviderViewModel : INotifyPropertyChanged
  {
    private WinFuncs wf;
    private WifiCommon wc;
    #region Constructor&Destructor
    public ProviderViewModel()
    {
      wc = new WifiCommon("P", ProjectName, Properties.Resources.Dict);
      wf = wc.wf;
    }

    public void AfterCtor()
    {
      account = new WifiAccount(wc, wf);
      if (canConnect && (account.AutoConnect == true))
        Connect();
    }

    #endregion
    private int waitcount = 0;
    public int Waitcount
    {
      get { return waitcount; }
      set
      {
        waitcount = value;
        OnPropertyChanged(nameof(MainGridVisibility));
      }
    }

    public bool MainGridVisibility
    {
      get
      {
        return Waitcount == 0;
      }
    } //  ? Visibility.Visible : Visibility.Hidden; 

    #region Classes
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
    #endregion
    #region Commands
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
    public bool canConnect { get { return ((Account!=null) && (Account.Logged || Connected)); } }
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
    #region Globals
    private WifiAccount account;
    public WifiAccount Account
    {
      get { return account; }
      //set { account = value; }
    }

    public event PropertyChangedEventHandler PropertyChanged;
    private void OnPropertyChanged(String property) { PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property)); }
    private DnsServer dnsServer;
    private ICaptureDevice device;
    public List<Client> clients = new List<Client>();
    public static IntPtr wdhndl = IntPtr.Zero;
    private int GetSetUsageInterval = 3000;
    private static String providerIp = "";
    private MyWebServer ws;
    private bool wsStarted = false;
    public static string ProjectName = "Wifi";
    string langCode = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
    SimpleAES saes = new SimpleAES();
    Thread thrGetSetUsage;
    #endregion
    #region Methods
    public void StartSystem(bool stopFirst = true) //  = true
    {
      if (stopFirst)
        wc.RuninThread(new DoWorkEventHandler(thrStopSystem), delegate (object sender, RunWorkerCompletedEventArgs e) { StartSystem(false); }); // delegate (object sender, RunWorkerCompletedEventArgs e) { StartSystem(false); Account.Waitcount--; }
      else
        wc.RuninThread(new DoWorkEventHandler(thrStartSystem), null); // delegate (object sender, RunWorkerCompletedEventArgs e) { StartSystem(false); Account.Waitcount--; }
    }
    private void StopSystem()
    {
      wc.RuninThread(new DoWorkEventHandler(thrStopSystem), delegate (object sender, RunWorkerCompletedEventArgs e) { OnPropertyChanged(nameof(canConnect)); } ); // delegate (object sender, RunWorkerCompletedEventArgs e) { StartSystem(false); Account.Waitcount--; }
    }
    private void thrStartSystem(object sender, DoWorkEventArgs e)
    {
      Waitcount++;
      try
      {
        String source = "";
        String hotspot = "";
        if (!Account.Logged)
          return;
        if (wdhndl != IntPtr.Zero)
          WinDivertMethods.WinDivertClose(wdhndl);
        List<NetworkInterface> nicx = new List<NetworkInterface>();
        foreach (var nic in IcsManager.GetAllIPv4Interfaces())
          if ((nic.OperationalStatus == OperationalStatus.Up && nic.NetworkInterfaceType != NetworkInterfaceType.Tunnel && nic.NetworkInterfaceType != NetworkInterfaceType.Loopback))
            nicx.Add(nic);
        if (nicx.Count > 1)
        {
          wf.ShowMessageBox(wc.dict.GetMessage(19));
          wf.Shutdown();
        }
        else if (nicx.Count == 1)
          source = nicx[0].Id;
        else
        {
          wf.ShowMessageBox(wc.dict.GetMessage(20));
          wf.Shutdown();
        }
        wf.Netsh("wlan set hostednetwork mode=allow ssid=wifix key=erci1234"); //  key=ercierci
        Add2Log("Hotspot Created");
        nicx.Clear();
        foreach (var nic in IcsManager.GetAllIPv4Interfaces())
          if ((nic.Description.Contains("Virtual") && nic.Id != source && nic.NetworkInterfaceType != NetworkInterfaceType.Tunnel && nic.NetworkInterfaceType != NetworkInterfaceType.Loopback))
            nicx.Add(nic);
        if (nicx.Count > 1)
        {
          wf.ShowMessageBox(wc.dict.GetMessage(21));
          wf.Shutdown();
        }
        else if (nicx.Count == 1)
          hotspot = nicx[0].Id;
        else
        {
          wf.ShowMessageBox(wc.dict.GetMessage(22));
          wf.Shutdown();
        }
        EnableICS(source, hotspot, true);
        Add2Log("Internet Connection Sharing Enabled");
        wf.Netsh("wlan start hostednetwork");
        Add2Log("HotSpot Opened");
        try
        {
          NetworkInterface _nic = NetworkInterface.GetAllNetworkInterfaces().FirstOrDefault(x => x.Id == hotspot);
          foreach (UnicastIPAddressInformation ip in _nic.GetIPProperties().UnicastAddresses)
            if (ip.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
              providerIp = ip.Address.ToString();
        }
        catch (Exception)
        {
          wf.ShowMessageBox(wc.dict.GetMessage(24));
          wf.Shutdown();
        }
        if ((thrGetSetUsage != null) && (thrGetSetUsage.IsAlive))
          thrGetSetUsage.Abort();
        thrGetSetUsage = new Thread(new ThreadStart(GetSetUsage));
        thrGetSetUsage.IsBackground = true;
        thrGetSetUsage.Start();
        Add2Log("GetSetUsage thread started!");

        #region DNS SERVER
        dnsServer = new DnsServer(IPAddress.Any, 10, 10);
        dnsServer.QueryReceived += OnQueryReceived;
        //dnsServer.ClientAccount.Connected += OnClientAccount.Connected;
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
              dnsServer.Start();
            }
            catch
            {
              throw;
            }
            wf.ShowMessageBox(wc.dict.GetMessage(23));
          }
          else throw;
        }
        Add2Log("DNS Server running");
        #endregion
        #region Capture Device // start olmadan önce capture başlarsa capture çalışmıyor...
        //CaptureDeviceList.Instance.Refresh();
        device = CaptureDeviceList.Instance.FirstOrDefault(x => x.Name.Contains(hotspot));// devices[devices.Count - 1];
        //var v = ((SharpPcap.WinPcap.WinPcapDevice)device).Addresses.First(x => x.Addr.ipAddress.ToString().Contains("."));
        //if (v != null)
        //  providerIp = v.Addr.ipAddress.ToString(); // Anlamadığım bir sebepten dolayı bazen 0.0.0.0 geliyor. Ya da 192.168.0.1 // Ostoto kurup kaldırınca böyle oldu...
        //else
        //{
        //  wf.ShowMessageBox(wc.dict.GetMessage(24));
        //  wf.Shutdown();
        //}
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
        string pib = providerIp.Substring(0, providerIp.LastIndexOf('.') + 1);
        string filter = "ip.SrcAddr>=" + pib + "1 and ip.SrcAddr<=" + pib + "255 and ip.DstAddr>=" + pib + "1 and ip.DstAddr<=" + pib + "255";
        //wdhndl = WinDivertMethods.WinDivertOpen(filter, WINDIVERT_LAYER.WINDIVERT_LAYER_NETWORK, 1, WinDivertConstants.WINDIVERT_FLAG_DROP);
        Connected = true;
        //gp = IcsManager.FindConnectionByIdOrName(hotspot);
        //device = CaptureDeviceList.Instance.FirstOrDefault(x => x.Name.Contains(hotspot));// devices[devices.Count - 1];
        //sss = device.ToString();
        #endregion
      }
      finally
      {
        Waitcount--;
      }
    }
    private void thrStopSystem(object sender, DoWorkEventArgs e)
    {
      try
      {
        Waitcount++;
        wf.Netsh("wlan stop hostednetwork");
        DisableICS();
        wf.Netsh("wlan set hostednetwork mode=disallow"); //  key=ercierci
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
      finally
      {
        Waitcount--;
      }
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
            Add2Log(wc.dict.GetMessage(11, cl.Mac + "¶" + wc.dict.GetMessage(14)));
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
      command = url.GetUrlParam("Command");
      //if ((url.Contains("/")) && (url.Contains("?")) && (url.IndexOf("/") < url.IndexOf("?")))
      //  command = url.ReverseString().OrtasiniGetir("?", "/").ReverseString();
      //else if (url.Contains("/"))
      //  command = url.Split('/').Last();
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
        return "";// _client = clients[0];
      //return "null";
      if (command == "Remove")
      {
        String Email = url.GetUrlParam("Email");
        wc.GetWebstring("/" + command + "?" + url.Substring(url.IndexOf("&") + 1));
        //wc.WCF.Remove(Email);
        return "<a href='Register?Email='>Register</a>";
      }
      else if (command == "Register")
      {
        _temp = wc.GetWebstring("/" + command + "?" + url.Substring(url.IndexOf("&") + 1));
        //wc.GetWebstring(url);
        //wc.WCF.Register(url.GetUrlParam("Email"), url.GetUrlParam("Pass"), url.GetUrlParam("AFQ"), url.GetUrlParam("LangCode"));
        if (mfn.isValidHexString(_temp, 32))
          _client.SecurityCode = _temp;
        return _temp;
      }
      else if (command == "GetSecurityCode")
      {
        String _EmailHash = url.GetUrlParam("EmailHash");
        _temp = wc.GetWebstring("/" + command + "?" + url.Substring(url.IndexOf("&") + 1));
        //wc.GetWebstring(url);
        //wc.WCF.GetSecurityCode(_EmailHash, url.GetUrlParam("AFQ"), url.GetUrlParam("LangCode"));
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
        _temp = wc.GetWebstring("/" + command + "?" + url.Substring(url.IndexOf("&") + 1));
        //wc.GetWebstring(url);
        //wc.WCF.Login(url.GetUrlParam("Evidence"), url.GetUrlParam("AFQ"), url.GetUrlParam("LangCode"));
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
        if (_client.Quota == 0) return "¶E:" + wc.dict.GetMessage(18);
        String ClientEvidence = url.GetUrlParam("ClientEvidence");
        _temp = Account.myEvidence();
        _temp = wc.GetWebstring("/" + command + "?" + url.Substring(url.IndexOf("&") + 1));
        //wc.GetWebstring(url);
        //wc.WCF.ConnectUS(ClientEvidence, _temp, url.GetUrlParam("AFQ"), url.GetUrlParam("LangCode"));
        long qt;
        if ((_temp.Length >= 67) &&
          mfn.isValidHexString(_temp.Substring(0, 32)) && (_temp[64] == ';') &&
          mfn.isValidHexString(_temp.Substring(65, 32)) && (_temp[129] == ';') &&
          (long.TryParse(_temp.Substring(130), out qt))) // if (_temp.Length >= 67)
        {
          _client.SecurityCode = _temp.Substring(0, 32);
          Account.SecurityCode = _temp.Substring(65, 32);
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
      //    //return "<a href='SetUsage?Message=" + _client.EmailHash + (_client.SecurityCode + (Account.EmailHash + _client.Password).HashMD5() + (usg.ToString()).HashMD5()).HashMD5() + usg.ToString() + "'>SetUsage</a>";
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
          _temp = wc.GetWebstring("/" + command + "?" + url.Substring(url.IndexOf("&") + 1));
          //wc.GetWebstring(url);
          //wc.WCF.SetUsage(_temp, Account.myEvidence(amount.ToString()), long.Parse(_client.ConnectionID), url.GetUrlParam("AFQ"), langCode);
          if (((_client.Usage > amount * 1.001) && (_client.Usage > amount + 1024 * 1024)) || (amount >= _client.Quota)) // 1/1000 den fazla fark varsa ve bu fark 1MB ı aşmışsa...
            return "¶E:Kotanız bittiği için bağlantınız sonlandırıldı.";

          if ((_temp.Length == 65) && (_temp[32] == ';') && (mfn.isValidHexString(_temp.Substring(0, 32))) && (mfn.isValidHexString(_temp.Substring(33, 32))))
          {
            _client.LastQuery = DateTime.Now;
            _client.SecurityCode = _temp.Substring(0, 32);
            Account.SecurityCode = _temp.Substring(33, 32);
            Add2Log("KULLANIM BILGISI ONAYI ALINDI. KULLANIM : " + amount.ToString() + " CONNECTION ID : " + _client.ConnectionID.ToString());
            return _client.SecurityCode;
          }
          else return _temp;
        }
        else return "¶E:" + _client.EmailHash + "<>" + Account.EmailHash;
      }
      else if (command == "Disconnect")
      {
        _client.Online = false;// RemoveClient(_client); listeden kaldırmasın, sadece kullanıcı programını kapattı. listeden silince provider ekranında tekrar added görünüyor...
        Add2Log(_client.Mac + " BAĞLANTISINI SONLANDIRDI. CONNECTION ID : " + url.GetUrlParam("ConnectionID"));
        return "";// "BAĞLANTI SONLANDIRILDI. CONNECTION ID : " + url.GetUrlParam("ConnectionID");
      }
      else
        return "<a href='\\" + providerIp + @"\" + ProjectName + @"\WifiWPFClient.exe'>Download Client</a>";
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
      Console.WriteLine(DateTime.Now.ToShortTimeString() + ": " + msg);
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
            Add2Log(wc.dict.GetMessage(11, item.Mac + "¶" + wc.dict.GetMessage(13))); // + (GetSetUsageInterval / 1000 * 2).ToString() + " saniyedir yanıt vermediği için bağlantısı kapatıldı.");
          }
        }
        Thread.Sleep(GetSetUsageInterval);
      }
    }
    private bool yetkili(Client cl)
    {
      return ((cl != null) && (cl.Online));
    }

    private void LoginCommand()
    {
      if (!Account.Logged)
        wc.RuninThread(
          delegate (object sender, DoWorkEventArgs e) { Account.Login(); },
          //new DoWorkEventHandler(Account.Login),
          delegate (object sender, RunWorkerCompletedEventArgs e) { if ((Account.AutoConnect == true) && (Account.Logged) && (!Connected)) Connect(); }
        ); // delegate (object sender, RunWorkerCompletedEventArgs e) { StartSystem(false); Account.Waitcount--; }
      else
        wc.RuninThread(
          delegate (object sender, DoWorkEventArgs e) { Account.Logout(); },
          //new DoWorkEventHandler(Account.Login),
          delegate (object sender, RunWorkerCompletedEventArgs e) { Disconnect(); }
        ); // delegate (object sender, RunWorkerCompletedEventArgs e) { StartSystem(false); Account.Waitcount--; }
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
    #endregion
  }
}
