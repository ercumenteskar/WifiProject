using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
//using ARSoft.Tools.Net.Dns;
using My;
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
using System.Windows.Controls;
using System.Windows.Threading;
using System.ComponentModel;
using System.Runtime.InteropServices;
using ARSoft.Tools.Net.Dns;
//using System.Net.Http;

namespace WifiProvider
{
  /// <summary>
  /// Interaction logic for MainWindow.xaml
  /// </summary>
  public partial class MainWindow : Window, INotifyPropertyChanged
  {
    public MainWindow()
    {
      InitializeComponent();
    }
    private const string _projectName = "Wifi";
    private const string _projectregaddr = "Software\\" + _projectName;
    //    private string TelNo = "5448302898";
    private String TelNoHash { get { return GetTextBox(tb_TelNo).HashMD5(); } }
    //private String TelNoHash = "";
    private String Password = "e";
    private String SecurityCode = "";
    public WifiService.Service1Client WCF = new WifiService.Service1Client();
    public MyWebServer ws;
    //System.Windows.Threading.DispatcherTimer dispatcherTimer;
    int GetSetUsageInterval = 3000;
    private IntPtr hndl;
    private bool wsStarted = false;
    //private delegate void Adff();
    //delegate void SimpleDelegate();
    public string startIp = "192.168.137.1", MacMask = "", AdapterIP = "0.0.0.0"; //"0.0.0.0"
    //public bool NetCardAlive = false;
    //private Thread dHCPThread = null;
    //private Socket MainSock;
    //private int packetCount = 0;
    private List<String> SLKey = new List<String>();
    private List<long> SLValue = new List<long>();
    private String WSPort = "80";
    private DnsServer dnsServer;
    private ICaptureDevice device;
    private String providerIp = "";
    private String devmac = "";
    public event PropertyChangedEventHandler PropertyChanged;
    private void OnPropertyChanged(String property) { if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(property)); } }
    private long _quota = -1;
    public long Quota { get { return _quota; } set { _quota = value; OnPropertyChanged("QuotaStr"); } }
    public string QuotaStr { get { return (Quota / 1024).ToString("###,###,###,###,##0"); } }
    public string bt_ConnectContent { get { return Connected ? "Disconnect" : "Connect"; } }
    private bool _connected = false;
    public bool Connected
    {
      get { return _connected; }
      set
      {
        _connected = value;
        tb_TelNo.IsEnabled = !value;
        tb_Password.IsEnabled = !value;
        if (!value)
        {
          Add2Log("Logout başarılı");
          StopSystem();
        }
        else
        {
          Add2Log("Login başarılı");
          StartSystem();
        }
        OnPropertyChanged("bt_ConnectContent");
      }
    }
    public List<Client> clients = new List<Client>();
    public class Client
    {
      public String Mac = "00:00:00:00:00:00";
      public String Ip = "0.0.0.0";
      public String TelNoHash = "";
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
            AllowMac();
          else
            BlockMac();
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
        //if ((this.Ip == "192.168.137.1") || (this.Ip == tb_i))
        //  return;
        if (this.WDHandle != 0)
          AllowMac();
        IntPtr i = WinDivertMethods.WinDivertOpen("(ip.SrcAddr=" + Ip + " or ip.DstAddr=" + Ip + ") and (ip.SrcAddr!=192.168.137.1 and ip.DstAddr!=192.168.137.1)", WINDIVERT_LAYER.WINDIVERT_LAYER_NETWORK, 1, WinDivertConstants.WINDIVERT_FLAG_DROP);
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

    /*
     private void AllowMac(string Mac)
    {
      Client cl = clients.Find(x => x.Mac == Mac);
      if (cl != null)
      {
        cl.AllowMac();
      }
    }

    private void BlockMac(string Mac)
    {
      throw new NotImplementedException();
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


    private String GetValue(String Total, String Key)
    {
      string str = Total.Split(Environment.NewLine.ToCharArray(), StringSplitOptions.RemoveEmptyEntries).FirstOrDefault(str4find => str4find.Contains(Key));
      if (str != null)
        return str.Substring(Key.Length);
      else
        return "";
    }
    //private long TotalRcv = 0;
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
    public void StartSystem()
    {
      //return;
      StopSystem();
      if (!Connected)
        return;
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
      //Thread.Sleep(100);
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
      Add2Log("Internet Connection Sharing Enabled");
      Netsh("wlan start hostednetwork");
      //*/
      Add2Log("HotSpot Opened");
      Thread _thrGetSetUsage = new Thread(new ThreadStart(GetSetUsage));
      _thrGetSetUsage.IsBackground = true;
      //_thrGetSetUsage.Start();
      Add2Log("GetSetUsage thread started!");

      /*
      DisableICS();
      #region DNS SERVER
      dnsServer = new DnsServer(IPAddress.Any, 10, 10);
      dnsServer.QueryReceived += OnQueryReceived;
      //      dnsServer.ClientConnected += OnClientConnected;
      dnsServer.Start();
      Add2Log("DNS Server running");
      #endregion
      EnableICS(source, hotspot, true);
      //      var gp = IcsManager.FindConnectionByIdOrName(hotspot);
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
      //device.
      //Thread.Sleep(4000);
      Thread _thrwsRun = new Thread(new ThreadStart(wsRun));
      _thrwsRun.IsBackground = true;
      _thrwsRun.Start();
      //gp = IcsManager.FindConnectionByIdOrName(hotspot);
      //device = CaptureDeviceList.Instance.FirstOrDefault(x => x.Name.Contains(hotspot));// devices[devices.Count - 1];
      //sss = device.ToString();
      #endregion
    }

    private void wsRun()
    {
      Ping pingSender = new Ping ();
      //IPAddress address = IPAddress.Loopback;
      PingReply reply = pingSender.Send("192.168.137.1", 10);
      while (reply.Status != IPStatus.Success) reply = pingSender.Send("192.168.137.1", 10);
      ws = new MyWebServer(SendResponse, "http://" + providerIp + ":" + WSPort + "/");
      ws.Run();
      wsStarted = true;
      Add2Log("WEB Server running : http://" + providerIp + ":" + WSPort + "/");
    }

    private void StopSystem()
    {
      Netsh("wlan stop hostednetwork");
      Netsh("wlan set hostednetwork mode=disallow"); //  key=ercierci
      Netsh("wlan set hostednetwork mode=allow ssid=wifix key=erci1234"); //  key=ercierci
      if ((device != null) && (device.Started))
      {
        device.StopCapture();
        device.Close();
      }
      //DisableICS();
      if ((ws != null) && (wsStarted))
      {
        ws.Stop();
        wsStarted = false;
      }
      if (dnsServer != null)
        dnsServer.Stop();
      Add2Log("Sistem kapatıldı.");
    }

    private String GetUrlParam(String Url, String Param)
    {
      NameValueCollection nvc = HttpUtility.ParseQueryString(Url.Substring(Url.IndexOf("?")));
      return nvc[Param];
    }

    public string GetMacAddress(string ipAddress)
    {
      ARP arper = new ARP((SharpPcap.LibPcap.LibPcapLiveDevice)device);

      // print the resolved address or indicate that none was found
      var resolvedMacAddress = arper.Resolve(IPAddress.Parse(ipAddress));
      if (resolvedMacAddress != null)
      {
        return resolvedMacAddress.ToString();
//        Console.WriteLine("Timeout, no mac address found for ip of " + ip);
      }
      else
      {
        return "";
//        Console.WriteLine(ip + " is at: " + arper.Resolve(ip));
      }
      //return;
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
      while (strOutput.IndexOf("  ") > -1)
        strOutput = strOutput.Replace("  ", " ");
      macAddress = strOutput.Trim().Split(' ')[1].Replace("-", "");
      return macAddress.ToUpper();
    }

    public string SendResponse(HttpListenerRequest request)
    {
      string RemoteIp = request.RemoteEndPoint.Address.ToString();
      ///* // Mac a göre bulurken hem cmd işin içine giriyor (içime sinmedi) hem de kendi makinamdan test edemiyordum...
        string reqmac = "";
        if (RemoteIp == providerIp) //
          reqmac = "";//"888888888887";
        else
          reqmac = GetMacAddress(RemoteIp); //null döndüğü için patlıyor....
        Client _client = clients.Find(x => x.Mac == reqmac);
      //*/
      //if (RemoteIp == "192.168.137.1") RemoteIp = "192.168.137.100";
      //Client _client = clients.Find(x => x.Ip == RemoteIp);
      if (_client == null)
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
          if (!amount.StartsWith("error:"))
          {
            _client.LastQuery = DateTime.Now;
            _client.SecurityCode = amount.Substring(0, 32);
            SecurityCode = amount.Substring(33, 32);
            long usg = _client.Usage;
            Add2Log("KULLANIM BILGISI ONAYI ALINDI. KULLANIM : " + usg.ToString() + " CONNECTION ID : " + _client.ConnectionID.ToString());
            // Burada buna bakılmaz. Güncellenmiş olabilir çünkü. Clientdan gelen değeri ona ne zaman gönderdiğimize bakıcaz, x saniyeyi geçmedi ise o miktar ile evidence hazırlayacağız. Güncel _client.Usage ile değil.
            //return "<a href='SetUsage?Message=" + _client.TelNoHash + (_client.SecurityCode + (TelNoHash + _client.Password).HashMD5() + (usg.ToString()).HashMD5()).HashMD5() + usg.ToString() + "'>SetUsage</a>";
            return amount.Substring(0, 32);
          }
          else return amount;
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
        String d_ip = "", s_ip = "";
        //        IpPacket ip = PacketDotNet.IpPacket.GetEncapsulated(packet); // deprecated
        IpPacket ip = (IpPacket)packet.Extract(typeof(IpPacket));
        if (ip != null)
        {
          d_ip = ip.DestinationAddress.ToString();
          s_ip = ip.SourceAddress.ToString();
        }
        //if (d_mac == "96659C170A34") d_mac = "888888888887";
        //if (d_ip == "192.168.137.1") d_ip = "192.168.137.100";
        //if (
        //     (devmac != s_mac)
        //  //|| (s_ip != providerIp) || (!s_ip.Contains('.')) // mesela youtube dan video izlerken kaynak dış ip adresi oluyor...
        //  //|| (s_ip.Substring(0, s_ip.LastIndexOf('.')) != d_ip.Substring(0, d_ip.LastIndexOf('.'))) // mesela youtube dan video izlerken kaynak dış ip adresi oluyor...
        //  || (d_ip == providerIp) || (d_ip == "") || (!d_ip.Contains('.'))
        //  || (d_mac == "") || (d_mac.Replace(d_mac.Substring(0, 1), "") == "") || (d_ip == "255.255.255.255")
        //  ) return;
        //string sss = s_ip.Substring(0, s_ip.LastIndexOf('.'));
        //packet.Bytes.Length
        //long PackSize = e.Packet.Data.Length;//büyük%10 civarı
        //long PackSize = packet.Bytes.Length; //büyük%10 civarı
        //long PackSize = packet.Bytes.Length - packet.Header.Length; //büyük%10 civarı
        long PackSize = eth.Bytes.Length - eth.Header.Length; //büyük%10 civarı

        var ipv4 = (PacketDotNet.IPv4Packet)packet.Extract(typeof(PacketDotNet.IPv4Packet));
        var tcp = (PacketDotNet.TcpPacket)packet.Extract(typeof(PacketDotNet.TcpPacket));
        var udp = (PacketDotNet.UdpPacket)packet.Extract(typeof(PacketDotNet.UdpPacket));

        int sli = SLKey.IndexOf(d_mac + "	" + s_mac + "	" + d_ip + "	" + s_ip + "	" + ((ipv4 != null) ? "ipv4" : "") + "	" + ((tcp != null) ? "tcp" : "") + "	" + ((udp != null) ? "udp" : ""));
        if (sli >= 0)
        {
          SLValue[sli] = SLValue[sli] + PackSize;
        }
        else
        {
          SLKey.Add(d_mac + "	" + s_mac + "	" + d_ip + "	" + s_ip + "	" + ((ipv4 != null) ? "ipv4" : "") + "	" + ((tcp != null) ? "tcp" : "") + "	" + ((udp != null) ? "udp" : ""));
          SLValue.Add(PackSize);
        }
        Client cl = clients.Find(x => x.Mac == d_mac);
        if (
            (cl == null)
          && (s_ip == providerIp) && (s_ip.Contains('.')) // mesela youtube dan video izlerken kaynak dış ip adresi oluyor...
          && (s_ip.Substring(0, s_ip.LastIndexOf('.')) == d_ip.Substring(0, d_ip.LastIndexOf('.'))) // mesela youtube dan video izlerken kaynak dış ip adresi oluyor...
            && (
            (
              (devmac == s_mac)
              && (d_ip != providerIp) && (d_ip != "") && (d_ip.Contains('.'))
              && (d_mac != "") && (d_mac.Replace(d_mac.Substring(0, 1), "") != "") && (d_ip != "255.255.255.255")
              ) //|| (devmac == "96659C170A34")
            )
          )
        {
          if ((clients.Find(x => x.Mac == devmac) == null) && (devmac == "96659C170A34"))
          {
            cl = new Client(devmac, "192.168.137.1");
            clients.Add(cl);
            Add2Log(cl.Mac + "(" + cl.Ip + ") Added");
          }
          cl = new Client(d_mac, d_ip);
          clients.Add(cl);
          Add2Log(cl.Mac + "(" + cl.Ip + ") Added");
          SetTextBox(tb_ip, cl.Ip);
        }
        if (cl == null) return;
        //else if ((cl.Ip == "") && (d_ip != ""))// Bu mac adresi için 2. sefer ise ve öncekinde ip adresi yoktuysa
        //{
        //  cl.Ip = d_ip; // ilk gelişinde ip istemek için geliyor, haliyle kayıtlarda ip adresi yok. 
        //  if (cl.Ip.Contains('.'))
        //    Add2Log(cl.Mac + "(" + cl.Ip + ") Added");
        //} else
        if ((cl.Ip != d_ip) && (d_ip != "") && (d_ip.StartsWith("192.168.137."))) // Bu mac adresi için önceden yapılan kayıttan sonra ip adresi değişti ise
        {
          cl.Online = false;
          clients.Remove(cl);
          return;
        }
        if (cl.Online)
        {
          cl.Usage += PackSize;
          Quota += PackSize;
          if (cl.Usage >= cl.Quota)
          {
            cl.Online = false;
            clients.Remove(cl);
            Console.WriteLine(cl.Mac + " kotasını bitirdiği için bağlantısı kesildi. ");
            return;
          }
          //Add2Log(cl.Mac + " : " + (cl.Usage / 1024).ToString("###,###,##0"));
          //Console.WriteLine(cl.Mac + " : " + (cl.Usage / 1024).ToString("###,###,##0") + " Kalan : " + ((cl.Quota - cl.Usage) / 1024).ToString("###,###,##0"));
        }
      }
    }
    ///*

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
                              .Select(a => new ARecord(a.Name, a.TimeToLive, IPAddress.Parse("192.168.137.1"))) // some local ip address
                      )
              );
            }

            e.Response = response;
          }
        }
    // */
    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
      Connected = false;
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
      this.DataContext = this;
      tc_RegisterLogin.SelectedIndex = 1;
      if ((Registry.CurrentUser.OpenSubKey(_projectregaddr) != null) && (Registry.CurrentUser.OpenSubKey(_projectregaddr).GetValue(cb_TelNoRemember.Name.Substring(3)) != null) && (Registry.CurrentUser.OpenSubKey(_projectregaddr).GetValue(cb_TelNoRemember.Name.Substring(3)).ToString() != ""))
        tb_TelNo.Text = Registry.CurrentUser.OpenSubKey(_projectregaddr).GetValue(cb_TelNoRemember.Name.Substring(3)).ToString();
      if ((Registry.CurrentUser.OpenSubKey(_projectregaddr) != null) && (Registry.CurrentUser.OpenSubKey(_projectregaddr).GetValue(cb_PassRemember.Name.Substring(3)) != null) && (Registry.CurrentUser.OpenSubKey(_projectregaddr).GetValue(cb_PassRemember.Name.Substring(3)).ToString() != ""))
        tb_Password.Password = Registry.CurrentUser.OpenSubKey(_projectregaddr).GetValue(cb_PassRemember.Name.Substring(3)).ToString();
      if (tb_TelNo.Text != "") cb_TelNoRemember.IsChecked = true;
      if (tb_Password.Password != "") cb_PassRemember.IsChecked = true;
      if ((tb_TelNo.Text != "") || (tb_Password.Password != ""))
      {
        tc_RegisterLogin.SelectedIndex = 0;
        if ((tb_TelNo.Text != "") && (tb_Password.Password != ""))
          bt_Connect.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        else if (tb_TelNo.Text != "")
          tb_Password.Focus();
        else if (tb_Password.Password != "")
          tb_TelNo.Focus();
      }
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

    public delegate void UpdateTextCallbacktxtb(TextBox tb, string str);
    public delegate void UpdateTextCallbackpassb(PasswordBox tb, string str);
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

    private bool CheckResult(string result)
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

    private void bt_Register_Click(object sender, RoutedEventArgs e)
    {
      string result = "";
      if (CheckForInternetConnection())
        result = WCF.Register(GetTextBox(tb_RegisterTelNo), GetPasswordBox(tb_RegisterPassword1), 0);
      else
        result = "";
      if (result == "") result = "error:Bağlantı sağlanamadı.";
      if (CheckResult(result))
      {
        result = "Kayıt başarılı. Giriş yapılacak.";
        Add2Log(result);
        MessageBox.Show(result);
        tc_RegisterLogin.SelectedIndex = 0;
        SetTextBox(tb_TelNo, GetTextBox(tb_RegisterTelNo));
        SetPasswordBox(tb_Password, GetPasswordBox(tb_RegisterPassword1));
        bt_Connect.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
      }
    }

    private void GetSetUsage()
    {
      while (true)
      {
        for (int i = clients.Count - 1; i >= 0; i--)
        {
          var item = clients[i];
          if ((item.Online)  && ((DateTime.Now - item.LastQuery).TotalMilliseconds > GetSetUsageInterval * 2))
          {
            item.Online = false;
            Add2Log(item.Mac + " adresli cihaz " + (GetSetUsageInterval / 1000 * 2).ToString() + " saniyedir yanıt vermediği için bağlantısı kapatıldı.");
          }
        }
        Thread.Sleep(GetSetUsageInterval);
      }
    }


    private void SetSecurityCode()
    {
      SecurityCode = WCF.GetSecurityCode(GetTextBox(tb_TelNo).HashMD5());
    }

    private void Login()
    {
      Quota = 0;
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
      String _tmp = WCF.Login(GetTextBox(tb_TelNo).HashMD5() + (SecurityCode + (GetTextBox(tb_TelNo).HashMD5() + Password).HashMD5() + ("").HashMD5()).HashMD5());
      if ((_tmp.Length > 33) && (_tmp.Substring(32, 1) == ";"))
      {
        Quota = long.Parse(_tmp.Substring(33));
        SecurityCode = _tmp.Substring(0, 32);
        //SetLabel(l_Quota, QuotaStr);
        Connected = true;
        RememberAction();
      }
      else
      {
        SecurityCode = "";
        //l_Quota.Text = QuotaStr;
        Add2Log("Hata: Giriş yapılamadı. " + _tmp);
        return;
      }
    }

    private void RememberAction(object sender = null)
    {
      if ((sender == cb_TelNoRemember) || (sender == null))
      {
        if ((cb_TelNoRemember.IsChecked == true) && (Connected))
          Registry.CurrentUser.CreateSubKey(_projectregaddr).SetValue(cb_TelNoRemember.Name.Substring(3), GetTextBox(tb_TelNo));
        else if (cb_TelNoRemember.IsChecked == false) Registry.CurrentUser.CreateSubKey(_projectregaddr).SetValue(cb_TelNoRemember.Name.Substring(3), "");
      }
      if ((sender == cb_PassRemember) || (sender == null))
      {
        if ((cb_PassRemember.IsChecked == true) && (Connected))
          Registry.CurrentUser.CreateSubKey(_projectregaddr).SetValue(cb_PassRemember.Name.Substring(3), GetPasswordBox(tb_Password));
        else if (cb_PassRemember.IsChecked == false) Registry.CurrentUser.CreateSubKey(_projectregaddr).SetValue(cb_PassRemember.Name.Substring(3), "");
      }
    }

    private void bt_Connect_Click(object sender, RoutedEventArgs e)
    {
      if (Connected)
      {
        Connected = false;
        return;
      }
      if ((GetTextBox(tb_TelNo) != "") && (GetPasswordBox(tb_Password) != ""))
        Login();
      else MessageBox.Show("Telefon numarası veya parola boş!");
    }

    private void cb_Remember_Checked(object sender, RoutedEventArgs e)
    {
      RememberAction(sender);
    }

    private void Button_Click(object sender, RoutedEventArgs e)
    {
      for (int i = 0; i < SLKey.Count; i++)
        Add2Log(SLKey[i] + "	" + SLValue[i].ToString());
      return;
      if (l_defQuota.Content.ToString().StartsWith("Kot"))
      {
        hndl = WinDivertMethods.WinDivertOpen("ip.SrcAddr=192.168.137.59 or ip.DstAddr=192.168.137.59", WINDIVERT_LAYER.WINDIVERT_LAYER_NETWORK, 1, WinDivertConstants.WINDIVERT_FLAG_DROP);
        l_defQuota.Content = hndl.ToString();
      }
      else
      {
        WinDivertMethods.WinDivertClose(hndl);
        l_defQuota.Content = "Kota :";
      }
    }

    unsafe private void Button_Click_1(object sender, RoutedEventArgs e)
    {

      //for (int i = 0; i < SLKey.Count; i++)
      //  if (clients.Find(x => x.Mac == SLKey[i].Substring(12)) != null) 
      //    if (clients.Find(x => x.Mac == SLKey[i].Substring(12)).Ip==tb_ip.Text) 
      //  Quota = Quota - SLValue[i];
      //return;
      SLKey.Clear();
      SLValue.Clear();
      return;
      //HANDLE handle;          // WinDivert handle
    WINDIVERT_ADDRESS addr = new WINDIVERT_ADDRESS(); // Packet address
    WINDIVERT_IPHDR iphdr = new WINDIVERT_IPHDR();
    WINDIVERT_IPHDR* iphdrP = &iphdr;
    WINDIVERT_IPHDR** iphdrPP = &iphdrP;
    WINDIVERT_IPV6HDR ipv6hdr = new WINDIVERT_IPV6HDR();
    WINDIVERT_IPV6HDR* ipv6hdrP = &ipv6hdr;
    WINDIVERT_IPV6HDR** ipv6hdrPP = &ipv6hdrP;
    WINDIVERT_ICMPHDR icmphdr = new WINDIVERT_ICMPHDR(); WINDIVERT_ICMPHDR* icmphdrP = &icmphdr; WINDIVERT_ICMPHDR** icmphdrPP = &icmphdrP;
    WINDIVERT_ICMPV6HDR icmpv6hdr = new WINDIVERT_ICMPV6HDR(); WINDIVERT_ICMPV6HDR* icmpv6hdrP = &icmpv6hdr; WINDIVERT_ICMPV6HDR** icmpv6hdrPP = &icmpv6hdrP;
    WINDIVERT_TCPHDR tcphdr = new WINDIVERT_TCPHDR(); WINDIVERT_TCPHDR* tcphdrP = &tcphdr; WINDIVERT_TCPHDR** tcphdrPP = &tcphdrP;
    WINDIVERT_UDPHDR udphdr = new WINDIVERT_UDPHDR(); WINDIVERT_UDPHDR* udphdrP = &udphdr; WINDIVERT_UDPHDR** udphdrPP = &udphdrP;
    uint packetLen = 0xFFFF;
    uint* ui = &packetLen;
    byte[] packet = new byte[packetLen];// = new Packet[];    // Packet buffer
    IntPtr writelen = (IntPtr)0;
    IntPtr handle = WinDivertMethods.WinDivertOpen("ip.SrcAddr="+tb_ip.Text+" or ip.DstAddr="+tb_ip.Text, WINDIVERT_LAYER.WINDIVERT_LAYER_NETWORK, 1, WinDivertConstants.WINDIVERT_FLAG_SNIFF);// WinDivertOpen("...", 0, 0, 0);   // Open some filter
    //if (handle == INVALID_HANDLE_VALUE)
    //{
    //    // Handle error
    //    exit(1);
    //}

    // Main capture-modify-inject loop:
    while (true)
    {
        if (!WinDivertMethods.WinDivertRecv(handle, packet, packetLen, ref addr, ref packetLen))
        {
            // Handle recv error
          
            continue;
        }

        // Modify packet.
        fixed (byte* pPacketP = &packet[0])
        {
          WinDivertMethods.WinDivertHelperParsePacket(pPacketP, packetLen, iphdrPP, ipv6hdrPP, icmphdrPP, icmpv6hdrPP, tcphdrPP, udphdrPP, null, ui);
          if (((*(*iphdrPP)).DstAddr != null) && ((*(*iphdrPP)).DstAddr.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork))
            Console.WriteLine((*(*iphdrPP)).ToString());
        }

        if (!WinDivertMethods.WinDivertSend(handle, packet, packetLen, ref addr, writelen))
        {
            // Handle send error
            continue;
        }
    }
      return;
      uint readLen = 0xFFFF;
//      uint packetLen = 0xFFFF;
      //uint* packetLenP = &packetLen;
      uint* pDataLen = &packetLen;
      WINDIVERT_ADDRESS pAddr = new WINDIVERT_ADDRESS();
      byte[] pPacket = new byte[packetLen];
      //byte* pPacketP = pPacket;
      //byte** ppData = new byte**;
      byte dire;
      //WINDIVERT_IPHDR iphdr = new WINDIVERT_IPHDR();
      //WINDIVERT_IPHDR* iphdrP = &iphdr;
      //WINDIVERT_IPHDR** iphdrPP = &iphdrP;
      //WINDIVERT_IPV6HDR ipv6hdr = new WINDIVERT_IPV6HDR();
      //WINDIVERT_IPV6HDR* ipv6hdrP = &ipv6hdr;
      //WINDIVERT_IPV6HDR** ipv6hdrPP = &ipv6hdrP;
      //WINDIVERT_ICMPHDR icmphdr = new WINDIVERT_ICMPHDR(); WINDIVERT_ICMPHDR* icmphdrP = &icmphdr; WINDIVERT_ICMPHDR** icmphdrPP = &icmphdrP;
      //WINDIVERT_ICMPV6HDR icmpv6hdr = new WINDIVERT_ICMPV6HDR(); WINDIVERT_ICMPV6HDR* icmpv6hdrP = &icmpv6hdr; WINDIVERT_ICMPV6HDR** icmpv6hdrPP = &icmpv6hdrP;
      //WINDIVERT_TCPHDR tcphdr = new WINDIVERT_TCPHDR(); WINDIVERT_TCPHDR* tcphdrP = &tcphdr; WINDIVERT_TCPHDR** tcphdrPP = &tcphdrP;
      //WINDIVERT_UDPHDR udphdr = new WINDIVERT_UDPHDR(); WINDIVERT_UDPHDR* udphdrP = &udphdr; WINDIVERT_UDPHDR** udphdrPP = &udphdrP;
      while (true)
      {
        WinDivertMethods.WinDivertRecv(hndl, pPacket, packetLen, ref pAddr, ref readLen);
        // pAddr 1 ise Source 0 ise DstAddr değişkenindeki ip 192.168.137.* dır
        fixed (byte* pPacketP = &pPacket[0])
        {
          WinDivertMethods.WinDivertHelperParsePacket(pPacketP, readLen, iphdrPP, ipv6hdrPP, icmphdrPP, icmpv6hdrPP, tcphdrPP, udphdrPP, null, &readLen);
          WINDIVERT_IPHDR _iphdr = *iphdrP;
        }
      }
      if (WinDivertMethods.WinDivertRecv(hndl, pPacket, packetLen, ref pAddr, ref readLen))
        dire = pAddr.Direction;
    }

    private void Button_Click_2(object sender, RoutedEventArgs e)
    {
      Client cl = clients.Find(x => x.Ip == tb_ip.Text);
      if (cl != null)
      {
        cl.Online = !cl.Online;
        cl.Quota = 1234567890;
      }
    }
  }
}
