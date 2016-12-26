using ARSoft.Tools.Net;
using ARSoft.Tools.Net.Dns;
using My;
using nfapinet;
using PacketDotNet;
using SharpPcap;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
//using System.Net.Http;

namespace ConsoleApplication1
{
  class Program
  {
    private static string TelNo = "5448302898";
    private static String TelNoHash = "";
    private static String Password = "e";
    private static String SecurityCode = "";
    public static WifiService.Service1Client WCF = new WifiService.Service1Client();
    //private delegate void Adff();
    //delegate void SimpleDelegate();
    public static string startIp = "192.168.137.1", MacMask = "", AdapterIP = "0.0.0.0"; //"0.0.0.0"
    //public bool NetCardAlive = false;
    //private static Thread dHCPThread = null;
    //private static Socket MainSock;
    private static int packetCount = 0;
    private static List<String> SLKey = new List<String>();
    private static List<long> SLValue = new List<long>();
    private static String WSPort = "80";
    private static DnsServer dnsServer;
    private static ICaptureDevice device;
    private static String providerIp = "";
    static EventHandler m_eh = new EventHandler();

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
      }
    }
    public static List<Client> clients = new List<Client>();
    private static String GetValue(String Total, String Key)
    {
      string str = Total.Split(Environment.NewLine.ToCharArray(), StringSplitOptions.RemoveEmptyEntries).FirstOrDefault(str4find => str4find.Contains(Key));
      if (str != null)
        return str.Substring(Key.Length);
      else
        return "";
    }
    private static long TotalRcv = 0;
    static void EnableICS(string shared, string home, bool force)
    {
      var connectionToShare = IcsManager.FindConnectionByIdOrName(shared);
      var homeConnection = IcsManager.FindConnectionByIdOrName(home);
      var currentShare = IcsManager.GetCurrentlySharedConnections();
      IcsManager.ShareConnection(connectionToShare, homeConnection);
    }
    static void DisableICS()
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
    private static String Netsh(String args)
    {
      using (Process p1 = new Process())
      {
        p1.StartInfo.FileName = "netsh.exe";
        p1.StartInfo.Arguments = args;
        p1.StartInfo.UseShellExecute = false;
        p1.StartInfo.RedirectStandardOutput = true;
        p1.Start();
        return p1.StandardOutput.ReadToEnd();
      }
    }
    private static String myEvidence(String Mesaj = "")
    {
      return TelNoHash + (SecurityCode + (TelNoHash + Password).HashMD5() + Mesaj.HashMD5()).HashMD5() + Mesaj;
    }
    public static MyWebServer ws;
    public static SocketAddress convertAddress(byte[] buf)
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
    public static string addressToString(SocketAddress addr)
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
    public static void Main(string[] args)
    {
      /*
      Console.WriteLine(Netsh("wlan set hostednetwork mode=disallow")); //  key=ercierci
      Console.WriteLine(Netsh("wlan stop hostednetwork"));
      */
      //      AppDomain.CurrentDomain.ProcessExit += new EventHandler(CurrentDomain_ProcessExit);
      TelNoHash = TelNo.HashMD5();
      SecurityCode = WCF.GetSecurityCode(TelNoHash);
      if (SecurityCode != "")
        Console.WriteLine("SecurityCode alındı:" + SecurityCode);
      else
      {
        Console.WriteLine("SecurityCode alınamadı:" + SecurityCode);
        return;
      }
      SecurityCode = myEvidence();
      SecurityCode = WCF.Login(SecurityCode);
      SecurityCode = SecurityCode.Substring(0, 32);
      if (SecurityCode != "")
        Console.WriteLine("Login olundu:" + SecurityCode);
      else
      {
        Console.WriteLine("Login hatalı:" + SecurityCode);
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
          Console.WriteLine(i.ToString() + "-) {0} {1}", nics[i].Name, nics[i].Id);
        Console.WriteLine("Select your internet source");
        source = nics[Int32.Parse(Console.ReadLine())].Id;
      }
      else
        source = nics[0].Id;
      //      String source = nics[1].Id;
      //Console.WriteLine("Select your hotspot");
      //      String hotspot = nics[Int32.Parse(Console.ReadLine())].Id;
      //      String hotspot = nics[0].Id;

      /*
      Console.WriteLine(Netsh("wlan set hostednetwork mode=allow ssid=wifix")); //  key=ercierci
      Console.WriteLine(Netsh("wlan start hostednetwork"));
      */
      Netsh("wlan set hostednetwork mode=allow ssid=wifix key=erci1234"); //  key=ercierci
      //Netsh("wlan start hostednetwork");
      //Console.WriteLine("HotSpot Opened");
      //Thread.Sleep(200);
      nics.Clear();
      foreach (NetworkInterface nic in IcsManager.GetAllIPv4Interfaces())
        if (nic.NetworkInterfaceType != NetworkInterfaceType.Tunnel && nic.NetworkInterfaceType != NetworkInterfaceType.Loopback && nic.Id != source) // nic.OperationalStatus == OperationalStatus.Up && 
          nics.Add(nic);
      //if (nics.Count > 1) // TODO Aslında 0 ı değil kullanıcının istediğini seçmem lazım
      //{
      //  for (int i = 0; i < nics.Count; i++)
      //    Console.WriteLine(i.ToString() + "-) {0} {1}", nics[i].Name, nics[i].Id);
      //  Console.WriteLine("Select your hotspot");
      //  hotspot = nics[Int32.Parse(Console.ReadLine())].Id;
      //}
      //else
      hotspot = nics[0].Id;
      ///*
      EnableICS(source, hotspot, true);
      Netsh("wlan start hostednetwork");
      //*/
      Console.WriteLine("HotSpot Opened");

      #region DNS SERVER
      dnsServer = new DnsServer(IPAddress.Any, 10, 10);
      dnsServer.QueryReceived += OnQueryReceived;
      //      dnsServer.ClientConnected += OnClientConnected;
      #endregion
      dnsServer.Start();
      Console.WriteLine("DNS Server running");
      //      var gp = IcsManager.FindConnectionByIdOrName(hotspot);
      //EnableICS(source, hotspot, true);
      Console.WriteLine("Internet Connection Sharing Enabled");
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
      //Console.WriteLine("Listening : " + GetValue(device.ToString(), "FriendlyName: ") + " (" + providerIp + ")");
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
      Console.WriteLine("WEB Server running : http://" + providerIp + ":" + WSPort + "/");
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
          Console.WriteLine("Usage : " + clients[0].Usage.ToString());
        }
        Console.ReadLine();
      }
      */
      Console.WriteLine("Kapatılıyor, lütfen bekleyin...");
      dnsServer.Stop();
      device.StopCapture();
      device.Close();
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

    private static String GetUrlParam(String Url, String Param)
    {
      NameValueCollection nvc = HttpUtility.ParseQueryString(Url.Substring(Url.IndexOf("?")));
      return nvc[Param];
    }
    
    public static string SendResponse(HttpListenerRequest request)
    {

      if (clients.Find(x => x.Ip == request.RemoteEndPoint.Address.ToString()) == null)
        clients.Add(new Client("", request.RemoteEndPoint.Address.ToString()));
      
      Client _client = clients.Find(x => x.Ip == request.RemoteEndPoint.Address.ToString());
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
        String TelNo = GetUrlParam(url, "TelNo");
        _temp = WCF.Register(TelNo, "e", 999);
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
          Console.WriteLine("BAĞLANAN VAR. CONNECTION ID : " + _client.ConnectionID.ToString());
          _client.isTest = (GetUrlParam(url, "testercument") == "1");
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
          return "error:No Connection on "+_client.Ip + " ("+_client.Mac+")";
        else if ((_client.TelNoHash.Equals(_tnh)) && (_client.ConnectionID.ToString().Equals(_cid)))
        {
          _client.LastQuery = DateTime.Now;
          //return "<a href='SetUsage?Message=" + _client.TelNoHash + (_client.SecurityCode + (TelNoHash + _client.Password).HashMD5() + (usg.ToString()).HashMD5()).HashMD5() + usg.ToString() + "'>SetUsage</a>";
          Console.WriteLine("KULLANIM BILGISI GONDERILDI. KULLANIM : " + _client.Usage.ToString() + " CONNECTION ID : " + _client.ConnectionID.ToString());
          if (_client.Usage > _client.Quota)
            return "error:Insufficent funds!!!";
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
          Console.WriteLine("KULLANIM BILGISI ONAYI ALINDI. KULLANIM : " + usg.ToString() + " CONNECTION ID : " + _client.ConnectionID.ToString());
          // Burada buna bakılmaz. Güncellenmiş olabilir çünkü. Clientdan gelen değeri ona ne zaman gönderdiğimize bakıcaz, x saniyeyi geçmedi ise o miktar ile evidence hazırlayacağız. Güncel _client.Usage ile değil.
          //return "<a href='SetUsage?Message=" + _client.TelNoHash + (_client.SecurityCode + (TelNoHash + _client.Password).HashMD5() + (usg.ToString()).HashMD5()).HashMD5() + usg.ToString() + "'>SetUsage</a>";
          return amount.Substring(0, 32);
        }
        else return "error:" + _client.TelNoHash + "<>" + TelNoHash;
      }
      else
        return "<a href='GetSecurityCode?TelNoHash=5CAA8CD9E281E9A815AD88C79DB734FF&testercument=1'>GetSecurityCode</a>";
      //      else return "<a href='Remove?TelNo=5448302899'>DELETE 5448302899</a>";
      //else return "WELLCOME !! YOU HAVE TO RUN OUR CLIENT APPLICATON";
    }

    private static bool yetkili(Client cl)
    {
      //return true;
      //return (packetCount == -1);
      if ((cl != null) && (cl.ConnectionID != "") && (cl.Usage < cl.Quota))
        return true;//cl.Mac.StartsWith("40:");
      else
        return false;
    }

    static void device_OnPacketArrival(object sender, CaptureEventArgs e)
    {
      String s_mac = device.LinkType.ToString();
      var packet = PacketDotNet.Packet.ParsePacket(e.Packet.LinkLayerType, e.Packet.Data);
      s_mac = "";
      String d_mac = "";
      String s_ip = "";
      if (packet is PacketDotNet.EthernetPacket)
      {
        var eth = ((PacketDotNet.EthernetPacket)packet);
        s_mac = eth.SourceHwAddress.ToString();
        d_mac = eth.DestinationHwAddress.ToString();
        //eth.SourceHwAddress = PhysicalAddress.Parse("00-11-22-33-44-55");
        //eth.DestinationHwAddress = PhysicalAddress.Parse("00-99-88-77-66-55");
        var ip = PacketDotNet.IpPacket.GetEncapsulated(packet);
        if (ip != null)
        {
          s_ip = ip.SourceAddress.ToString();
          //ip.SourceAddress = System.Net.IPAddress.Parse("1.2.3.4");
          //ip.DestinationAddress = System.Net.IPAddress.Parse("44.33.22.11");
        }
        if (clients.Find(x => x.Ip == s_ip) != null)
          clients.Find(x => x.Ip == s_ip).Mac = s_mac;
        long PackSize = e.Packet.Data.Length;
        //if (s_mac.StartsWith("40")) //(yetkili(clients.Find(x => x.Mac == key)))
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
      }
      else
        s_mac = "";
    }

    static async Task OnQueryReceived(object sender, QueryReceivedEventArgs e)
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
          //          Console.WriteLine("ENGELLENDİ");
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
        //*/
      }
    }
  }

}
