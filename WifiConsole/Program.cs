using ARSoft.Tools.Net.Dns;
using Microsoft.TeamFoundation.Build.Common;
using PacketDotNet;
using SharpPcap;
using SmallDHCPServer_C;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using My;
using System.Timers;

namespace ConsoleApplication1
{
  class Program
  {
    private static clsDHCP dhcpServer;
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
    private static List<Int64> SLValue = new List<Int64>();
    private static String WSPort = "80";
    private static DnsServer dnsServer;
    private static ICaptureDevice device;
    private static String providerIp = "";

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
      public Int64  ConnectionID = 0;
      public Int64 Quota = 0;
      public Int64 Usage = 0;
      public DateTime LastWhatsUp;
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

    private static Int64 TotalRcv = 0;
    static void EnableICS(string shared, string home, bool force)
    {
      var connectionToShare = IcsManager.FindConnectionByIdOrName(shared);
      var homeConnection = IcsManager.FindConnectionByIdOrName(home);
      var currentShare = IcsManager.GetCurrentlySharedConnections();
      IcsManager.ShareConnection(connectionToShare, homeConnection);
    }
    static void DisableICS() { 
      if (IcsManager.GetCurrentlySharedConnections().Exists){
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

    public static void Main(string[] args)
    {
      /*
      Console.WriteLine(Netsh("wlan set hostednetwork mode=disallow")); //  key=ercierci
      Console.WriteLine(Netsh("wlan stop hostednetwork"));
      */
//      AppDomain.CurrentDomain.ProcessExit += new EventHandler(CurrentDomain_ProcessExit);
      TelNoHash = TelNo.HashMD5();
      SecurityCode = WCF.GetSecurityCode(TelNoHash);
      if (SecurityCode!="") 
        Console.WriteLine("SecurityCode alındı:"+SecurityCode);
      else
      {
        Console.WriteLine("SecurityCode alınamadı:" + SecurityCode);
        return;
      }
      SecurityCode = WCF.Login(myEvidence()).Substring(0, 32);
      if (SecurityCode != "")
        Console.WriteLine("Login olundu:" + SecurityCode);
      else
      {
        Console.WriteLine("Login hatalı:" + SecurityCode);
        return;
      }
      System.Timers.Timer myTimer = new System.Timers.Timer();
      myTimer.Elapsed += new ElapsedEventHandler(myTimerOnTick);
      myTimer.Interval = 2000;
      myTimer.Enabled = true;
      DisableICS();
      //Netsh("wlan set hostednetwork mode=disallow"); //  key=ercierci
      Netsh("wlan stop hostednetwork");

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
      //Netsh("wlan set hostednetwork mode=allow ssid=wifix2 key=ercierci"); //  key=ercierci
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

      EnableICS(source, hotspot, true);

      Netsh("wlan set hostednetwork mode=allow ssid=wifix key=ercierci"); //  key=ercierci
      Netsh("wlan start hostednetwork");
      Console.WriteLine("HotSpot Opened");

      #region DCHP SERVER
      //dhcpServer = new clsDHCP(AdapterIP);
      //dhcpServer.Announced += new clsDHCP.AnnouncedEventHandler(cDhcp_Announced);
      //dhcpServer.Request += new clsDHCP.RequestEventHandler(cDhcp_Request);
      //dHCPThread = new Thread(dhcpServer.StartDHCPServer);
      //dHCPThread.Start();
      #endregion
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
      #region Capture Device
      var devices = CaptureDeviceList.Instance;
      device = CaptureDeviceList.Instance.FirstOrDefault(x => x.Name.Contains(hotspot));// devices[devices.Count - 1];
      string sss = device.ToString();
      providerIp = GetValue(device.ToString().Split(Environment.NewLine.ToCharArray(), StringSplitOptions.RemoveEmptyEntries)[8], "Addr:      ");
      while (providerIp.Length != 13)
      {
        device = CaptureDeviceList.Instance.FirstOrDefault(x => x.Name.Contains(hotspot));
        providerIp = GetValue(device.ToString().Split(Environment.NewLine.ToCharArray(), StringSplitOptions.RemoveEmptyEntries)[8], "Addr:      ");
      }
      device.OnPacketArrival += new SharpPcap.PacketArrivalEventHandler(device_OnPacketArrival);
      device.Open(DeviceMode.Normal, 1000); //DeviceMode.Promiscuous
      device.StartCapture();
      Console.WriteLine("Listening : " + GetValue(device.ToString(), "FriendlyName: ") + " (" + providerIp + ")");
      #endregion
      #region WEB SERVER
      //MyWebServer create etmeden önce beklemezsem hata veriyor (Denetim Masası/Ağ ve Paylaşım Merkezi/HotSpot un erişim türü İnternet olduktan sonra MyWebServer hatasız create oluyor. Burada algılamayı bulamadım, şimdilik 2 sn bekliyorum.
      Thread.Sleep(4000);
      MyWebServer ws = new MyWebServer(SendResponse, "http://" + providerIp + ":" + WSPort + "/");
      //gp = IcsManager.FindConnectionByIdOrName(hotspot);
      //device = CaptureDeviceList.Instance.FirstOrDefault(x => x.Name.Contains(hotspot));// devices[devices.Count - 1];
      //sss = device.ToString();
      ws.Run();
      Console.WriteLine("WEB Server running : http://" + providerIp + ":" + WSPort + "/");
      #endregion
      Console.ReadLine();
      /*
      cDHCPStruct d_DHCP = new cDHCPStruct(null);
      d_DHCP.dData.IPAddr = GetIPAdd();
      d_DHCP.dData.SubMask = "255.255.0.0";
      d_DHCP.dData.LeaseTime = 2000;
      d_DHCP.dData.ServerName = "Small DHCP Server";
      d_DHCP.dData.MyIP = AdapterIP;
      d_DHCP.dData.RouterIP = "0.0.0.0";
      d_DHCP.dData.LogServerIP = "0.0.0.0";
      d_DHCP.dData.DomainIP = "0.0.0.0";
      dhcpServer.SendDHCPMessage(DHCPMsgType.DHCPDECLINE, d_DHCP);
      Console.ReadLine();
      Console.ReadLine();
      Console.ReadLine();
      Console.ReadLine();
      //dhcpServer.
      */
      Console.WriteLine("Kapatılıyor, lütfen bekleyin...");
      dnsServer.Stop();
      device.StopCapture();
      device.Close();
      //ws.Stop();
      //dhcpServer.Dispose();
      DisableICS();
      //Netsh("wlan set hostednetwork mode=disallow"); //  key=ercierci
      Netsh("wlan stop hostednetwork");
    }
    private static void myTimerOnTick(object source, ElapsedEventArgs e)
    {
      //Console.WriteLine("Hello World!");
    }
    public static string SendResponse(HttpListenerRequest request)
    {
      if (clients.Find(x => x.Ip == request.RemoteEndPoint.Address.ToString()) == null)
        clients.Add(new Client("", request.RemoteEndPoint.Address.ToString()));
      Client _client = clients.Find(x => x.Ip == request.RemoteEndPoint.Address.ToString());
      String qry = "";
      if (request.Url.ToString().Contains(providerIp + "/GetSecurityCode?TelNoHash="))
      {
        qry = request.Url.ToString();
        String TelNoHash = qry.Substring(qry.IndexOf("=") + 1);
        qry = WCF.GetSecurityCode(TelNoHash);
        if (qry.Length == 32)
        {
          _client.TelNoHash = TelNoHash;
          return "<a href='Login?Evidence="+_client.TelNoHash+(qry+(TelNoHash+_client.Password).HashMD5()+("").HashMD5()).HashMD5()+"'>Login</a>";//qry;
        }
        else return "error:" + qry;
      }
      else if (request.Url.ToString().Contains(providerIp + "/Login?Evidence="))
      {
        qry = request.Url.ToString();
        String Evidence = qry.Substring(qry.IndexOf("=") + 1);
        qry = WCF.Login(Evidence);
        if (qry.Length >= 34)
        {
          _client.Quota = Int64.Parse(qry.Substring(33));
          _client.SecurityCode = qry.Substring(0, 32);
          return "<a href='ConnectUS?ClientEvidence=" + _client.TelNoHash + (_client.SecurityCode + (_client.TelNoHash + _client.Password).HashMD5() + ("").HashMD5()).HashMD5() + "'>ConnectUS</a>";//qry;
        }
        else return "error:" + qry;
      }
      else if (request.Url.ToString().Contains(providerIp + "/ConnectUS?ClientEvidence="))
      {
        qry = request.Url.ToString();
        String ClientEvidence = qry.Substring(qry.IndexOf("=") + 1);
        qry = myEvidence();
        qry = WCF.ConnectUS(ClientEvidence, qry);
        if (qry.Length >= 67)
        {
          _client.ConnectionID = Int64.Parse(qry.Substring(66));
          _client.SecurityCode = qry.Substring(0, 32);
          SecurityCode = qry.Substring(33, 32);
          return "<a href='WhatsUp?TelNoHash=" + _client.TelNoHash + "'>WhatsUp</a>";
//          return qry.Substring(0, 32) + ";" + qry.Substring(66); // Client SecurityCode+";"+ConnectionID
        }
        else return "error:" + qry;
      }
      else if (request.Url.ToString().Contains(providerIp + "/WhatsUp?TelNoHash="))
      {
        qry = request.Url.ToString();
        String TelNoHash = qry.Substring(qry.IndexOf("=") + 1);
        if (_client.TelNoHash == TelNoHash)
        {
          _client.LastWhatsUp = DateTime.Now;
          Int64 usg = _client.Usage;
          return "<a href='SetUsage?Message=" + _client.TelNoHash + (_client.SecurityCode + (TelNoHash + _client.Password).HashMD5() + (usg.ToString()).HashMD5()).HashMD5() + usg.ToString() + "'>SetUsage</a>";
//          return _client.Usage.ToString() + ";" + _client.ConnectionID.ToString();
        }
        else return "error:" + _client.TelNoHash + "<>" + TelNoHash;
      }
      else if (request.Url.ToString().Contains(providerIp + "/SetUsage?Message="))
      {
        qry = request.Url.ToString().Substring(request.Url.ToString().IndexOf("=")+1);
        String TelNoHash = qry.Substring(0, 32);
        if (_client.TelNoHash == TelNoHash)
        {
          String amount = qry.Substring(64);
          amount = WCF.SetUsage(qry, myEvidence(amount), _client.ConnectionID);
          _client.LastWhatsUp = DateTime.Now;
          _client.SecurityCode = amount.Substring(0, 32);
          SecurityCode = amount.Substring(33, 32);
          Int64 usg = _client.Usage;
          // Burada buna bakılmaz. Güncellenmiş olabilir çünkü. Clientdan gelen değeri ona ne zaman gönderdiğimize bakıcaz, x saniyeyi geçmedi ise o miktar ile evidence hazırlayacağız. Güncel _client.Usage ile değil.
          return "<a href='SetUsage?Message=" + _client.TelNoHash + (_client.SecurityCode + (TelNoHash + _client.Password).HashMD5() + (usg.ToString()).HashMD5()).HashMD5() + usg.ToString() + "'>SetUsage</a>";
//          return amount.Substring(0, 32);
        }
        else return "error:" + _client.TelNoHash + "<>" + TelNoHash;
      }
      else return "<a href='GetSecurityCode?TelNoHash=5CAA8CD9E281E9A815AD88C79DB734FF'>GetSecurityCode</a>";


/*
      if (!request.Url.ToString().Contains("action")) return "<HTML><BODY>HOŞ GELDİNİZ KAYIT OLMAK İÇİN : <br><a href=\"http://" + providerIp + "/action\">TIKLAYINIZ</a></BODY></HTML>";
      if (clients.Find(x => x.Ip == request.RemoteEndPoint.Address.ToString()) == null)
      {
        clients.Add(new Client("", request.RemoteEndPoint.Address.ToString()));
        Console.WriteLine(request.RemoteEndPoint.Address.ToString() + "İP EKLENDİ +++++++++++++++++++++++++++++++++");
        return string.Format("<HTML><BODY>{0}<br>EKLENDİ</BODY></HTML>", request.RemoteEndPoint.Address.ToString());
      }
      else
      {
        clients.Remove(clients.Find(x => x.Ip == request.RemoteEndPoint.Address.ToString()));
        Console.WriteLine(request.RemoteEndPoint.Address.ToString() + "İP KALDIRILDI ------------------------------");
*/
      /*
        Type netFwPolicy2Type = Type.GetTypeFromProgID("HNetCfg.FwPolicy2");
        INetFwPolicy2 mgr = (INetFwPolicy2)Activator.CreateInstance(netFwPolicy2Type);

        // Gets the current firewall profile (domain, public, private, etc.)
        NET_FW_PROFILE_TYPE2_ fwCurrentProfileTypes = (NET_FW_PROFILE_TYPE2_)mgr.CurrentProfileTypes;
  
        // Get current status
        bool firewallEnabled = mgr.get_FirewallEnabled(fwCurrentProfileTypes);
        string frw_status = "Windows Firewall is " + (firewallEnabled ?
        "enabled" : "disabled");

        // Disables Firewall
        mgr.set_FirewallEnabled(fwCurrentProfileTypes, false);
        */
        //dnsServer.ClientConnected += dnsServer_ClientConnected;
        //dnsServer.Stop();
        //dnsServer = new DnsServer(IPAddress.Any, 10, 10);
        //dnsServer.QueryReceived += OnQueryReceived;
        //dnsServer.Start();
//        return string.Format("<HTML><BODY>{0}<br>SİLİNDİ</BODY></HTML>", request.RemoteEndPoint.Address.ToString());
//        return "SİLİNDİ";
//      }
      //else return string.Format("<HTML><BODY>{0}<br>ZATEN VAR</BODY></HTML>", request.RemoteEndPoint.Address.ToString());
    }

    static async Task dnsServer_ClientConnected(object sender, ClientConnectedEventArgs eventArgs)
    {
      //eventArgs.RefuseConnect = !yetkili(clients.Find(x => x.Ip == eventArgs.RemoteEndpoint.Address.ToString()));
    }

    private static bool yetkili(Client cl)
    {
      //return true;
      //return false;
      if ((cl != null) && (cl.ConnectionID>=0)) 
        return true;//cl.Mac.StartsWith("40:");
      else
        return false;
    }

    static void device_OnPacketArrival(object sender, CaptureEventArgs e)
    {
      packetCount++;
      PacketWrapper packetWrapper = new PacketWrapper(packetCount, e.Packet);
      String sb = Packet.ParsePacket(packetWrapper.p.LinkLayerType, packetWrapper.p.Data).ToString(StringOutputType.Verbose);
      //Console.Clear();
      if (clients.Find(x => x.Ip == GetValue(sb, "IP:                  source = ")) != null)
        clients.Find(x => x.Ip == GetValue(sb, "IP:                  source = ")).Mac = GetValue(sb, "Eth:      source = ");

      String src =
                 GetValue(sb, "Eth:      source = ");
      String des =
                 GetValue(sb, "Eth: destination = ");
      //+" (" + GetValue(sb, "IP:                  source = ") + ") > "
      //         + GetValue(sb, "Eth: destination = ") + " (" + GetValue(sb, "IP:             destination = ") + ")";
      
      
      //if (GetValue(sb, "Eth:      source = ").StartsWith("96:65") || GetValue(sb, "Eth:      source = ").StartsWith("40:b8"))
      //  key = "-";
      /*
              String key = GetValue(sb, "Eth:      source = ") + " > "
                       + GetValue(sb, "Eth: destination = ");

      */
      Int64 PackSize = Int64.Parse(GetValue(sb, "Eth:  ******* Ethernet - \"Ethernet\" - offset=? length="));
      if (src.StartsWith("40:")) //(yetkili(clients.Find(x => x.Mac == key)))
      {
        if (SLKey.IndexOf(src) > -1)
          SLValue[SLKey.IndexOf(src)] = SLValue[SLKey.IndexOf(src)] + PackSize;
        else
        {
          SLKey.Add(src);
          SLValue.Add(PackSize);
        }
      }
      ///*
      //if (SLKey.Count()>0) Console.Clear();
      //for (int i = 0; i < SLKey.Count(); i++)
			//{
      //  Console.WriteLine(SLKey[i] + " : " + SLValue[i].ToString("###,###,###") + "Byte " + (SLValue[i] / 1024).ToString("###,###,###") + "KB " + (SLValue[i] / 1024 / 1024).ToString("###,###,###") + "MB " + (SLValue[i] / 1024 / 1024 / 1024).ToString("###,###,###") + "GB ");// + " (" + TotalRcv.ToString("###,###,###") + ")");
      if (src.StartsWith("96") && des.StartsWith("40"))
      {
        //Console.WriteLine(sb);
        TotalRcv += PackSize;
        clients.Find(x => x.Mac == des).Usage = TotalRcv;
        Console.WriteLine(src + ">" + des + " : " + TotalRcv.ToString("###,###,###") + "Byte " + (TotalRcv / 1024).ToString("###,###,###") + "KB " + (TotalRcv / 1024 / 1024).ToString("###,###,###") + "MB " + (TotalRcv / 1024 / 1024 / 1024).ToString("###,###,###") + "GB ");// + " (" + TotalRcv.ToString("###,###,###") + ")");
      }
      //}
      //*/
      /*
      Console.WriteLine(
        DateTime.Now.ToLongTimeString() + " : "
        + GetValue(sb, "Eth:      source = ") + " > "
        + GetValue(sb, "Eth: destination = ") + " ("
        + GetValue(sb, "Eth:  ******* Ethernet - \"Ethernet\" - offset=? length=") + ")"
        + "Total Received  : " + (TotalRcv/1024).ToString()
        //+ GetValue(sb, "IP:            total length = ") + ")"
        );
      */
//      TotalRcv += Int64.Parse(GetValue(sb, "Eth:  ******* Ethernet - \"Ethernet\" - offset=? length="));
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
          response.AnswerRecords.AddRange(upstreamResponse.AnswerRecords);
          Console.WriteLine("İZİN VERİLDİ");
        }
        else
        {
          Console.WriteLine("ENGELLENDİ");
          response.AnswerRecords.AddRange(
              upstreamResponse.AnswerRecords
                  .Where(w => !(w is ARecord))
                  .Concat(
                      upstreamResponse.AnswerRecords
                          .OfType<ARecord>()
                          .Select(a => new ARecord(a.Name, a.TimeToLive, IPAddress.Parse("192.168.137.1"))) // some local ip address
                  )
          );
        }

        e.Response = response;
      }
    }
//    /*
    public static bool CheckAlive(string IpAdd)
    {
      Ping pingSender = new Ping();
      IPAddress address;
      PingReply reply;

      try
      {
        address = IPAddress.Parse(IpAdd);//IPAddress.Loopback;
        reply = pingSender.Send(address, 100);
        return (reply.Status == IPStatus.Success);
      }
      catch (Exception ex)
      {
        Console.WriteLine(ex.Message);
        return false;
      }
      finally
      {
        if (pingSender != null) pingSender.Dispose();
        pingSender = null;
        address = null;
        reply = null;
      }
    }
    private static uint IPAddressToLongBackwards(string IPAddr)
    {
      System.Net.IPAddress oIP = System.Net.IPAddress.Parse(IPAddr);
      byte[] byteIP = oIP.GetAddressBytes();


      uint ip = (uint)byteIP[0] << 24;
      ip += (uint)byteIP[1] << 16;
      ip += (uint)byteIP[2] << 8;
      ip += (uint)byteIP[3];

      return ip;
    }
    private static string GetIPAdd()
    {
      IPAddress ipadd;
      byte[] yy;
      UInt32 iit;
      try
      {
        iit = IPAddressToLongBackwards(startIp);
        iit -= 1;
        do
        {
          iit += 1;
          yy = new byte[4];
          yy[3] = (byte)(iit);
          yy[2] = (byte)(iit >> 8);
          yy[1] = (byte)(iit >> 16);
          yy[0] = (byte)(iit >> 24);
          ipadd = new IPAddress(yy);
          // yy = IPAddress.HostToNetworkOrder(ii);
        }
        while (CheckAlive(ipadd.ToString()) == true);
        //reaching here means that the ip is free

        return ipadd.ToString();
      }
      catch
      {
        return null;
      }
    }
    public static void cDhcp_Announced(cDHCPStruct d_DHCP, string MacId)
    {
      string str = string.Empty;

      if (true == true)
      {
        //options should be filled with valid data
        d_DHCP.dData.IPAddr = GetIPAdd();
        d_DHCP.dData.SubMask = "255.255.0.0";
        d_DHCP.dData.LeaseTime = 2000;
        d_DHCP.dData.ServerName = "Small DHCP Server";
        d_DHCP.dData.MyIP = AdapterIP;
        d_DHCP.dData.RouterIP = "0.0.0.0";
        d_DHCP.dData.LogServerIP = "0.0.0.0";
        d_DHCP.dData.DomainIP = "0.0.0.0";
        str = "IP requested for Mac: " + MacId;

      }
      dhcpServer.SendDHCPMessage(DHCPMsgType.DHCPOFFER, d_DHCP);
      Console.WriteLine(str);
      //Application.DoEvents()
    }
    public static void cDhcp_Request(cDHCPStruct d_DHCP, string MacId)
    {
      string str = string.Empty;
      if (true == true)
      {
        //announced so then send the offer
        d_DHCP.dData.IPAddr = GetIPAdd();
        d_DHCP.dData.SubMask = "255.255.255.0";
        d_DHCP.dData.LeaseTime = 2000;
        d_DHCP.dData.ServerName = "tiny DHCP Server";
        d_DHCP.dData.MyIP = AdapterIP;
        d_DHCP.dData.RouterIP = "0.0.0.0";
        d_DHCP.dData.LogServerIP = "0.0.0.0";
        d_DHCP.dData.DomainIP = "0.0.0.0";
        dhcpServer.SendDHCPMessage(DHCPMsgType.DHCPACK, d_DHCP);
        str = "IP " + d_DHCP.dData.IPAddr + " Assigned to Mac: " + MacId;
        /*
        if (clients.Find(x => x.Mac == MacId) != null)
          clients.Find(x => x.Mac == MacId).Ip = d_DHCP.dData.IPAddr;
        else
        {
          clients.Add(new Client(MacId, d_DHCP.dData.IPAddr));
        }
        */
      }
      Console.WriteLine(str);
    }
//    */
  }

}
