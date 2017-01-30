using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using ARSoft.Tools.Net.Dns;
using My;
//using nfapinet;
using PacketDotNet;
using SharpPcap;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using System.Web;
using Microsoft.Win32;
//using System.Net.Http;

namespace WifiProvider
{
  /// <summary>
  /// Interaction logic for MainWindow.xaml
  /// </summary>
  public partial class MainWindow : Window
  {
    public MainWindow()
    {
      InitializeComponent();
    }
    private const string _projectName = "Wifi";
    private string TelNo = "5448302898";
    private String TelNoHash = "";
    private String Password = "e";
    private String SecurityCode = "";
    public WifiService.Service1Client WCF = new WifiService.Service1Client();
    //private delegate void Adff();
    //delegate void SimpleDelegate();
    public string startIp = "192.168.137.1", MacMask = "", AdapterIP = "0.0.0.0"; //"0.0.0.0"
    //public bool NetCardAlive = false;
    //private Thread dHCPThread = null;
    //private Socket MainSock;
    private int packetCount = 0;
    private List<String> SLKey = new List<String>();
    private List<long> SLValue = new List<long>();
    private String WSPort = "80";
    private DnsServer dnsServer;
    private ICaptureDevice device;
    private String providerIp = "";
    private String devmac = "";
    /*
    EventHandler m_eh = new EventHandler();
        public SocketAddress convertAddress(byte[] buf)
    {
      if (buf == null)
      {
        return new SocketAddress(AddressFamily.InterNetwork);
      }

      SocketAddress addr = new SocketAddress((AddressFamily)(buf[0]), (int)NF_CONSTS.NF_MAX_ADDRESS_LENGTH);

      for (int i = 0; i < (int)NF_CONSTS.NF_MAX_ADDRESS_LENGTH; i++)
      {
        addr[i] = buf[i];
      }

      return addr;
    }

    unsafe public class EventHandler : NF_EventHandler
    {
      public void threadStart()
      {
      }

      public void threadEnd()
      {
      }

      public void tcpConnectRequest(ulong id, ref NF_TCP_CONN_INFO connInfo)
      {
      }

      public unsafe void tcpConnected(ulong id, NF_TCP_CONN_INFO connInfo)
      {
      }

      public void tcpClosed(ulong id, NF_TCP_CONN_INFO connInfo)
      {
      }

      public void tcpReceive(ulong id, IntPtr buf, int len)
      {
        NFAPI.nf_tcpPostReceive(id, buf, len);
      }

      public void tcpSend(ulong id, IntPtr buf, int len)
      {
        NFAPI.nf_tcpPostSend(id, buf, len);
      }

      public void tcpCanReceive(ulong id)
      {
      }

      public void tcpCanSend(ulong id)
      {
      }

      public void udpCreated(ulong id, NF_UDP_CONN_INFO connInfo)
      {
      }

      public void udpConnectRequest(ulong id, ref NF_UDP_CONN_REQUEST connReq)
      {
      }

      public void udpClosed(ulong id, NF_UDP_CONN_INFO connInfo)
      {
      }

      public void udpReceive(ulong id, IntPtr remoteAddress, IntPtr buf, int len, IntPtr options, int optionsLen)
      {
        NFAPI.nf_udpPostReceive(id, remoteAddress, buf, len, options);
      }

      public void udpSend(ulong id, IntPtr remoteAddress, IntPtr buf, int len, IntPtr options, int optionsLen)
      {
        NFAPI.nf_udpPostSend(id, remoteAddress, buf, len, options);
      }

      public void udpCanReceive(ulong id)
      {
      }

      public void udpCanSend(ulong id)
      {
      }

    }
    */
    public class PacketWrapper
    {
      public RawCapture p;

      public int Count { get; private set; }
      public PosixTimeval Timeval { get { return p.Timeval; } }
      public LinkLayers LinkLayerType { get { return p.LinkLayerType; } }
      public int Length { get { return p.Data.Length; } }

      public PacketWrapper(int count, RawCapture p)
      {
        this.Count = count;
        this.p = p;
      }
    }

    public class Client
    {
      public String Mac = "00:00:00:00:00:00";
      public String Ip = "0.0.0.0";
      public String TelNoHash = "";
      public String ConnectionID = ""; // hiç bir yerde aritmetik işleme sokmayacağım ve her yerde ToString çevriminden kurtulmak için String yapıyorum.
      public long Quota = 0;
      public long Usage = 0;
      private bool _online = false;
      public bool Online
      {
        get { return _online; }
        set { _online = value; 
          //if value 
          //  AllowMac(Mac); 
          //else 
          //  BlockMac(Mac); 
        }
      }
      public DateTime LastQuery;
      public bool isTest;
      // geçici
      public String Password = "e";
      public String SecurityCode = "";
      //
      public Client(String MacAddr, String IpAddr)
      {
        if ((MacAddr.Length == 12) && (MacAddr.IndexOf(":") < 0))
          MacAddr = MacAddr.Substring(0, 2) + MacAddr.Substring(2, 2) + MacAddr.Substring(4, 2) + MacAddr.Substring(6, 2) + MacAddr.Substring(8, 2) + MacAddr.Substring(10, 2);
        this.Mac = MacAddr;
        this.Ip = IpAddr;
        this.Online = false;
      }
    }
    public List<Client> clients = new List<Client>();
    private String GetValue(String Total, String Key)
    {
      string str = Total.Split(Environment.NewLine.ToCharArray(), StringSplitOptions.RemoveEmptyEntries).FirstOrDefault(str4find => str4find.Contains(Key));
      if (str != null)
        return str.Substring(Key.Length);
      else
        return "";
    }
    private long TotalRcv = 0;
    void EnableICS(string shared, string home, bool force)
    {
      var connectionToShare = IcsManager.FindConnectionByIdOrName(shared);
      var homeConnection = IcsManager.FindConnectionByIdOrName(home);
      var currentShare = IcsManager.GetCurrentlySharedConnections();
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
        p1.Start();
        return p1.StandardOutput.ReadToEnd();
      }
    }
    private String myEvidence(String Mesaj = "")
    {
      return TelNoHash + (SecurityCode + (TelNoHash + Password).HashMD5() + Mesaj.HashMD5()).HashMD5() + Mesaj;
    }
    public MyWebServer ws;
    public string addressToString(SocketAddress addr)
    {
      IPEndPoint ipep;

      if (addr.Family == AddressFamily.InterNetworkV6)
      {
        ipep = new IPEndPoint(IPAddress.IPv6None, 0);
      }
      else
      {
        ipep = new IPEndPoint(0, 0);
      }

      ipep = (IPEndPoint)ipep.Create(addr);

      return ipep.ToString();
    }
    private delegate void UpdateTextCallback(string message);
    private void Add2Log(string p)
    {
      tb_Log.Dispatcher.Invoke(
              new UpdateTextCallback(this._add2Log),
              new object[] { p }
          );
      Console.WriteLine(p);
    }
    private void _add2Log(string p)
    {
      tb_Log.AppendText(p);
      tb_Log.AppendText(Environment.NewLine);
      tb_Log.ScrollToEnd();
      Thread.Sleep(100);
    }
    public void Maini()
    {
      /*
      Add2Log(Netsh("wlan set hostednetwork mode=disallow")); //  key=ercierci
      Add2Log(Netsh("wlan stop hostednetwork"));
      */
      //      AppDomain.CurrentDomain.ProcessExit += new EventHandler(CurrentDomain_ProcessExit);
      TelNoHash = TelNo.HashMD5();
      SecurityCode = WCF.GetSecurityCode(TelNoHash);
      if (SecurityCode != "")
        Add2Log("SecurityCode alındı:" + SecurityCode);
      else
      {
        Add2Log("SecurityCode alınamadı:" + SecurityCode);
        return;
      }
      SecurityCode = myEvidence();
      SecurityCode = WCF.Login(SecurityCode);
      SecurityCode = SecurityCode.Substring(0, 32);
      if (SecurityCode != "")
        Add2Log("Login olundu:" + SecurityCode);
      else
      {
        Add2Log("Login hatalı:" + SecurityCode);
        return;
      }
      ///*
      DisableICS();
      Netsh("wlan stop hostednetwork");
      //Netsh("wlan set hostednetwork mode=disallow"); //  key=ercierci
      Netsh("wlan set hostednetwork mode=allow ssid=wifix key=erci1234"); //  key=ercierci
      //*/
      String source = "";
      String hotspot = "";
      List<NetworkInterface> nics = new List<NetworkInterface>();
      foreach (NetworkInterface nic in IcsManager.GetAllIPv4Interfaces())
        if (nic.OperationalStatus == OperationalStatus.Up && nic.NetworkInterfaceType != NetworkInterfaceType.Tunnel && nic.NetworkInterfaceType != NetworkInterfaceType.Loopback)
          nics.Add(nic);
      if (nics.Count > 1)
      {
        for (int i = 0; i < nics.Count; i++)
          Add2Log(i.ToString() + "-) " + nics[i].Name + " " + nics[i].Id);
        Add2Log("Select your internet source");
        source = nics[Int32.Parse(Console.ReadLine())].Id;
      }
      else
        source = nics[0].Id;
      //      String source = nics[1].Id;
      //Add2Log("Select your hotspot");
      //      String hotspot = nics[Int32.Parse(Console.ReadLine())].Id;
      //      String hotspot = nics[0].Id;

      /*
      Add2Log(Netsh("wlan set hostednetwork mode=allow ssid=wifix")); //  key=ercierci
      Add2Log(Netsh("wlan start hostednetwork"));
      */
      Netsh("wlan set hostednetwork mode=allow ssid=wifix key=erci1234"); //  key=ercierci
      //Netsh("wlan start hostednetwork");
      //Add2Log("HotSpot Opened");
      //Thread.Sleep(200);
      nics.Clear();
      foreach (NetworkInterface nic in IcsManager.GetAllIPv4Interfaces())
        if (nic.NetworkInterfaceType != NetworkInterfaceType.Tunnel && nic.NetworkInterfaceType != NetworkInterfaceType.Loopback && nic.Id != source) // nic.OperationalStatus == OperationalStatus.Up && 
          nics.Add(nic);
      //if (nics.Count > 1) // TODO Aslında 0 ı değil kullanıcının istediğini seçmem lazım
      //{
      //  for (int i = 0; i < nics.Count; i++)
      //    Add2Log(i.ToString() + "-) {0} {1}", nics[i].Name, nics[i].Id);
      //  Add2Log("Select your hotspot");
      //  hotspot = nics[Int32.Parse(Console.ReadLine())].Id;
      //}
      //else
      hotspot = nics[0].Id;
      ///*
      EnableICS(source, hotspot, true);
      Netsh("wlan start hostednetwork");
      //*/
      Add2Log("HotSpot Opened");

      /*
      #region DNS SERVER
      dnsServer = new DnsServer(IPAddress.Any, 10, 10);
      dnsServer.QueryReceived += OnQueryReceived;
      //      dnsServer.ClientConnected += OnClientConnected;
      #endregion
      dnsServer.Start();
      Add2Log("DNS Server running");
      //      var gp = IcsManager.FindConnectionByIdOrName(hotspot);
      //EnableICS(source, hotspot, true);
      Add2Log("Internet Connection Sharing Enabled");
      */
      //if (NFAPI.nf_init("netfilter2", m_eh) != 0)
      //{
      //  Console.Out.WriteLine("Failed to connect to driver");
      //  return;
      //}

      #region Capture Device
      //var devices = CaptureDeviceList.Instance;
      //NetworkInterface[] nilist = NetworkInterface.GetAllNetworkInterfaces();
      //var asa = nilist.FirstOrDefault(x => x.Id.Contains(hotspot));
      //IPAddress ial = Dns.GetHostEntry(Dns.GetHostName()).AddressList.FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork);
      device = CaptureDeviceList.Instance.FirstOrDefault(x => x.Name.Contains(hotspot));// devices[devices.Count - 1];
      //string sss = device.ToString();
      providerIp = GetValue(device.ToString().Split(Environment.NewLine.ToCharArray(), StringSplitOptions.RemoveEmptyEntries)[8], "Addr:      ");
      while (providerIp.Length != 13)
      {
        //device = CaptureDeviceList.Instance.FirstOrDefault(x => x.Name.Contains(hotspot));
        providerIp = "192.168.137.1";// GetValue(device.ToString().Split(Environment.NewLine.ToCharArray(), StringSplitOptions.RemoveEmptyEntries)[8], "Addr:      ");
      }
      device.OnPacketArrival += new SharpPcap.PacketArrivalEventHandler(device_OnPacketArrival);
      device.Open(DeviceMode.Normal, 1000); //DeviceMode.Promiscuous
      device.StartCapture();
      //Add2Log("Listening : " + GetValue(device.ToString(), "FriendlyName: ") + " (" + providerIp + ")");
      #endregion
      #region WEB SERVER
      //MyWebServer create etmeden önce beklemezsem hata veriyor (Denetim Masası/Ağ ve Paylaşım Merkezi/HotSpot un erişim türü İnternet olduktan sonra MyWebServer hatasız create oluyor. Burada algılamayı bulamadım, şimdilik 2 sn bekliyorum.
      //Henüz 192.168.137.1 ip adresini alamadığı için hata veriyor. Bunu kontrol edip ip aldıktan sonra devam edersen 4 saniyeden az beklersin, hem de hata almaman garanti olur.
      Thread.Sleep(4000);
      ws = new MyWebServer(SendResponse, "http://" + providerIp + ":" + WSPort + "/");
      //gp = IcsManager.FindConnectionByIdOrName(hotspot);
      //device = CaptureDeviceList.Instance.FirstOrDefault(x => x.Name.Contains(hotspot));// devices[devices.Count - 1];
      //sss = device.ToString();
      ws.Run();
      Add2Log("WEB Server running : http://" + providerIp + ":" + WSPort + "/");
      #endregion
      //if (NFAPI.nf_init("netfilter2", m_eh) != 0)
      //{
      //  Console.Out.WriteLine("Failed to connect to driver");
      //  return;
      //}
      //Console.ReadLine();
      //NFAPI.nf_deleteRules();
      //NF_RULE rule = new NF_RULE();
      //rule.filteringFlag = (uint)NF_FILTERING_FLAG.NF_BLOCK;
      //rule.ip_family = 0;// (ushort)AddressFamily.InterNetwork;
      //rule.localIpAddress = IPAddress.Parse("0.0.0.0").GetAddressBytes();
      //rule.remoteIpAddress = IPAddress.Parse("0.0.0.0").GetAddressBytes();
      //NFAPI.nf_addRule(rule, 0);
      Console.ReadLine();
      //NFAPI.nf_free();


      //packetCount = -1;
      //Console.ReadLine();
      /*
      if (clients.Count() != 0)
      {
        clients[0].Usage = 1;
        while (clients[0].Usage < 1024)
        {
          Thread.Sleep(1000);
          clients[0].Usage = clients[0].Usage * 2;
          Add2Log("Usage : " + clients[0].Usage.ToString());
        }
        Console.ReadLine();
      }
      */
      /* OnClosing e taşındı...
      Add2Log("Kapatılıyor, lütfen bekleyin...");
      dnsServer.Stop();
      device.StopCapture();
      device.Close();
*/
      //ws.Stop();
      /*
            //TODO: bunu aç 
            DisableICS();
            //NFAPI.nf_free();
            Netsh("wlan set hostednetwork mode=disallow"); //  key=ercierci
            //TODO: bunu aç 
            Netsh("wlan stop hostednetwork");
      */
    }

    private String GetUrlParam(String Url, String Param)
    {
      NameValueCollection nvc = HttpUtility.ParseQueryString(Url.Substring(Url.IndexOf("?")));
      return nvc[Param];
    }

    public string GetMacAddress(string ipAddress)
    {
      string macAddress = string.Empty;
      System.Diagnostics.Process pProcess = new System.Diagnostics.Process();
      pProcess.StartInfo.FileName = "arp";
      pProcess.StartInfo.Arguments = "-a " + ipAddress;
      pProcess.StartInfo.UseShellExecute = false;
      pProcess.StartInfo.RedirectStandardOutput = true;
      pProcess.StartInfo.CreateNoWindow = true;
      pProcess.Start();
      string strOutput = pProcess.StandardOutput.ReadLine();
      while (strOutput.IndexOf(providerIp) == -1)
        strOutput = pProcess.StandardOutput.ReadLine();
      while (strOutput.IndexOf(ipAddress) == -1)
        strOutput = pProcess.StandardOutput.ReadLine();
      while (strOutput.IndexOf("  ")>-1)
        strOutput = strOutput.Replace("  ", " ");
      macAddress = strOutput.Trim().Split(' ')[1].Replace("-", "");
      return macAddress.ToUpper();
    }

    public string SendResponse(HttpListenerRequest request)
    {
      string reqmac = GetMacAddress(request.RemoteEndPoint.Address.ToString());
      Client _client = clients.Find(x => x.Mac == reqmac);
      if (_client==null) 
        return "Tanımlanmamış istemci (Undefined client)";
      String url = request.Url.ToString();
      String command = "";
      String _temp = "";
      if ((url.Contains("/")) && (url.Contains("?")) && (url.IndexOf("/") < url.IndexOf("?")))
        command = url.ReverseString().OrtasiniGetir("?", "/").ReverseString();
      //else return "";
      if (command == "Remove")
      {
        String TelNo = GetUrlParam(url, "TelNo");
        WCF.Remove(TelNo);
        return "<a href='Register?TelNo=5448302899'>Register</a>";
      }
      else if (command == "Register")
      {
        _temp = WCF.Register(GetUrlParam(url, "TelNo"), GetUrlParam(url, "PW"), 0);
        if (_temp.Length == 32)
        {
          _client.SecurityCode = _temp;
          return _temp;
          // "<a href='GetSecurityCode?TelNoHash=5CAA8CD9E281E9A815AD88C79DB734FF'>GetSecurityCode</a>";
          //return "<a href='Login?Evidence=" + _client.TelNoHash + (qry + (TelNoHash + _client.Password).HashMD5() + ("").HashMD5()).HashMD5() + "'>Login</a>";//qry;
        }
        else return "error:" + _temp;
      }
      else if (command == "GetSecurityCode")
      {
        String _telNoHash = GetUrlParam(url, "TelNoHash");
        _temp = WCF.GetSecurityCode(_telNoHash);
        if (_temp.Length == 32)
        {
          _client.TelNoHash = _telNoHash;
          _client.SecurityCode = _temp;
          if (GetUrlParam(url, "testercument") == "1")
            return "<a href='Login?Evidence=" + _client.TelNoHash + (_client.SecurityCode + (_telNoHash + _client.Password).HashMD5() + ("").HashMD5()).HashMD5() + "&testercument=1'>Login</a>";
          else
            return _temp;
        }
        else return "error:" + _temp;
      }
      else if (command == "Login")
      {
        String Evidence = GetUrlParam(url, "Evidence");
        _temp = WCF.Login(Evidence);
        if (_temp.Length >= 34)
        {
          _client.Quota = long.Parse(_temp.Substring(33));
          _client.SecurityCode = _temp.Substring(0, 32);
          if (GetUrlParam(url, "testercument") == "1")
            return "<a href='ConnectUS?ClientEvidence=" + _client.TelNoHash + (_client.SecurityCode + (_client.TelNoHash + _client.Password).HashMD5() + ("").HashMD5()).HashMD5() + "&testercument=1'>ConnectUS</a>";
          else
            return _temp;
        }
        else return "error:" + _temp;
      }
      else if (command == "ConnectUS")
      {
        String ClientEvidence = GetUrlParam(url, "ClientEvidence");
        _temp = myEvidence();
        _temp = WCF.ConnectUS(ClientEvidence, _temp);
        if (_temp.Length >= 67)
        {
          _client.SecurityCode = _temp.Substring(0, 32);
          SecurityCode = _temp.Substring(33, 32);
          _client.ConnectionID = _temp.Substring(66);
          _client.Usage = 0;
          Add2Log("BAĞLANAN VAR. CONNECTION ID : " + _client.ConnectionID.ToString());
          _client.isTest = (GetUrlParam(url, "testercument") == "1");
          _client.Online = true;
          if (_client.isTest)
            return "BAĞLANTI SAĞLANDI";//"<a href='WhatsUp?TelNoHash=" + _client.TelNoHash + "'&testercument=1>WhatsUp</a>";
          else
            return _client.SecurityCode + ";" + _client.ConnectionID.ToString(); // "<a href='WhatsUp?TelNoHash=" + _client.TelNoHash + "'>WhatsUp</a>";
          //          return qry.Substring(0, 32) + ";" + qry.Substring(66); // Client SecurityCode+";"+ConnectionID
        }
        else return "error:" + _temp;
      }
      else if (command == "GetUsage")
      {
        String _tnh = GetUrlParam(url, "TelNoHash");
        String _cid = GetUrlParam(url, "ConnectionID");
        if (_client.ConnectionID == "")
          return "error:No Connection on " + _client.Ip + " (" + _client.Mac + ")";
        else if ((_client.TelNoHash.Equals(_tnh)) && (_client.ConnectionID.ToString().Equals(_cid)))
        {
          //return "<a href='SetUsage?Message=" + _client.TelNoHash + (_client.SecurityCode + (TelNoHash + _client.Password).HashMD5() + (usg.ToString()).HashMD5()).HashMD5() + usg.ToString() + "'>SetUsage</a>";
          Add2Log("KULLANIM BILGISI GONDERILDI. KULLANIM : " + _client.Usage.ToString() + " CONNECTION ID : " + _client.ConnectionID.ToString());
          if (_client.Usage > _client.Quota)
          {
            _client.ConnectionID = "";
            return "error:Insufficent funds!!!";
          }
          else
            return _client.Usage.ToString() + ";" + _client.ConnectionID.ToString();
        }
        else return "error:" + _client.TelNoHash + "<>" + _tnh + " OR " + _client.ConnectionID.ToString() + "<>" + _cid;
      }
      else if (command == "SetUsage")
      {
        _temp = GetUrlParam(url, "Message");
        String TelNoHash = _temp.Substring(0, 32);
        if (_client.TelNoHash == TelNoHash)
        {
          String amount = _temp.Substring(64);
          amount = WCF.SetUsage(_temp, myEvidence(amount), long.Parse(_client.ConnectionID));
          _client.LastQuery = DateTime.Now;
          _client.SecurityCode = amount.Substring(0, 32);
          SecurityCode = amount.Substring(33, 32);
          long usg = _client.Usage;
          Add2Log("KULLANIM BILGISI ONAYI ALINDI. KULLANIM : " + usg.ToString() + " CONNECTION ID : " + _client.ConnectionID.ToString());
          // Burada buna bakılmaz. Güncellenmiş olabilir çünkü. Clientdan gelen değeri ona ne zaman gönderdiğimize bakıcaz, x saniyeyi geçmedi ise o miktar ile evidence hazırlayacağız. Güncel _client.Usage ile değil.
          //return "<a href='SetUsage?Message=" + _client.TelNoHash + (_client.SecurityCode + (TelNoHash + _client.Password).HashMD5() + (usg.ToString()).HashMD5()).HashMD5() + usg.ToString() + "'>SetUsage</a>";
          return amount.Substring(0, 32);
        }
        else return "error:" + _client.TelNoHash + "<>" + TelNoHash;
      }
      else if (command == "Disconnect")
      {
        _client.ConnectionID = "";
        Add2Log("BAĞLANTI SONLANDIRILDI. CONNECTION ID : " + GetUrlParam(url, "ConnectionID"));
        return "BAĞLANTI SONLANDIRILDI. CONNECTION ID : " + GetUrlParam(url, "ConnectionID");
      }
      else
        return "<a href='GetSecurityCode?TelNoHash=5CAA8CD9E281E9A815AD88C79DB734FF&testercument=1'>GetSecurityCode</a>";
      //      else return "<a href='Remove?TelNo=5448302899'>DELETE 5448302899</a>";
      //else return "WELLCOME !! YOU HAVE TO RUN OUR CLIENT APPLICATON";
    }

    private bool yetkili(Client cl)
    {
      //return true;
      //return (packetCount == -1);
      if ((cl != null) && (cl.Online))//(cl.Usage < cl.Quota))
        return true;//cl.Mac.StartsWith("40:");
      else
        return false;
    }

    void device_OnPacketArrival(object sender, CaptureEventArgs e)
    {
      devmac = device.MacAddress.ToString();
      String tmp = device.LinkType.ToString();
      var packet = PacketDotNet.Packet.ParsePacket(e.Packet.LinkLayerType, e.Packet.Data);
      if (packet is PacketDotNet.EthernetPacket)
      {
        var eth = ((PacketDotNet.EthernetPacket)packet);
        String s_mac = eth.SourceHwAddress.ToString();
        String d_mac = eth.DestinationHwAddress.ToString();
        String d_ip = "";
        IpPacket ip = PacketDotNet.IpPacket.GetEncapsulated(packet);
        if (ip != null)
          d_ip = ip.DestinationAddress.ToString();
        if ((devmac != s_mac) || (providerIp == d_ip)) return;
        long PackSize = e.Packet.Data.Length;
        Client cl = clients.Find(x => x.Mac == d_mac);
        if (cl == null) 
        {
          cl = new Client(d_mac, d_ip);
          clients.Add(cl);
          Add2Log(cl.Mac + "("+cl.Ip+") Added");
        }
        else if ((cl.Ip == "") && (d_ip != ""))// Bu mac adresi için 2. sefer ise ve öncekinde ip adresi yoktuysa
          cl.Ip = d_ip; // ilk gelişinde ip istemek için geliyor, haliyle kayıtlarda ip adresi yok. 
        else if (cl.Ip != d_ip) // Bu mac adresi için önceden yapılan kayıttan sonra ip adresi değişti ise
        {
          cl.Online = false;
          clients.Remove(cl);
          return;
        }
        if (cl.Online)
        {
          cl.Usage += PackSize;
          if (cl.Usage >= cl.Quota)
          {
            cl.Online = false;
            clients.Remove(cl);
            Console.WriteLine(cl.Mac + " kotasını bitirdiği için bağlantısı kesildi. ");
            return;
          }
          //Add2Log(cl.Mac + " : " + (cl.Usage / 1024).ToString("###,###,##0"));
          Console.WriteLine(cl.Mac + " : " + (cl.Usage / 1024).ToString("###,###,##0"));
        }
        //if (s_mac.StartsWith("40")) //(yetkili(clients.Find(x => x.Mac == key)))
        /*
        {
          if (SLKey.IndexOf(s_mac) > -1)
            SLValue[SLKey.IndexOf(s_mac)] = SLValue[SLKey.IndexOf(s_mac)] + PackSize;
          else
          {
            SLKey.Add(s_mac);
            SLValue.Add(PackSize);
          }
        }
        if (clients.Find(x => x.Mac == d_mac) != null) //(s_mac.StartsWith("96") && d_mac.StartsWith("40"))
          clients.Find(x => x.Mac == d_mac).Usage += PackSize;
        */
      }
    }
/*

    async Task OnQueryReceived(object sender, QueryReceivedEventArgs e)
    {
      DnsMessage query = e.Query as DnsMessage;
      if (query == null) return;
      DnsMessage response = query.CreateResponseInstance();
      if (response.Questions.Any())
      {
        DnsQuestion question = response.Questions[0];
        DnsMessage upstreamResponse = await DnsClient.Default.ResolveAsync(question.Name, question.RecordType, question.RecordClass);
        response.AdditionalRecords.AddRange(upstreamResponse.AdditionalRecords);
        response.ReturnCode = ReturnCode.NoError;
        String ipa = e.RemoteEndpoint.Address.ToString();
        if (yetkili(clients.Find(x => x.Ip == ipa)))
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
                          .Select(a => new ARecord(a.Name, 1, IPAddress.Parse("192.168.137.1"))) // some local ip address
                  )
          );
        }

        e.Response = response;
      }
    }
*/    
    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
      Add2Log("Kapatılıyor, lütfen bekleyin...");
      dnsServer.Stop();
      device.StopCapture();
      device.Close();

    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
      /*
      tc_RegisterLogin.SelectedIndex = 1;
      if ((Registry.CurrentUser.OpenSubKey(_projectName) != null) && (Registry.CurrentUser.OpenSubKey(_projectName).GetValue("TelNo") != null) && (Registry.CurrentUser.OpenSubKey(_projectName).GetValue("TelNo").ToString() != ""))
        tb_TelNo.Text = Registry.CurrentUser.OpenSubKey(_projectName).GetValue("TelNo").ToString();
      else tb_TelNo.Text = "";
      if (tb_TelNo.Text != "")
      {
        tc_RegisterLogin.SelectedIndex = 0;
        tb_Password.Focus();
      }
      */
      Maini();
    }

    private void bt_Register_Click(object sender, RoutedEventArgs e)
    {

    }

  }
}
