using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using ARSoft.Tools.Net.Dns;
using My;
using PacketDotNet;
using SharpPcap;
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading;
using Microsoft.Win32;
using System.Windows.Controls;
using System.Windows.Threading;
using System.ComponentModel;
using System.Globalization;

//using System.Net.Http;

namespace WifiProvider
{

  public partial class MainWindow //: INotifyPropertyChanged
  {
    public MainWindow()
    {
      InitializeComponent();
    }
    private WifimViewModel vm = new WifimViewModel();
    SimpleAES saes = new SimpleAES();
    public string vPassword
    {
      get { return vm.Password; }
      set
      {
        if (vm.Password != value) vm.Password = value;
        if (GetPasswordBox(tb_Password) != value) SetPasswordBox(tb_Password, value);
      }
    }
    private delegate void UpdateTextCallback(string message);
    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
      vm.Disconnect();
    }
    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
      DataContext = vm;// this;
      if (!mfn.IsAdministrator())
      {
        MessageBox.Show(vm.dict.GetMessage(10));
        Application.Current.Shutdown();
      }
      SetPasswordBox(tb_Password, vm.Password);
      if ((vm.Email != "") || (vm.Password != ""))
      {
        tc_RegisterLogin.SelectedIndex = 0;
        if ((vm.Email != "") && (vm.Password != "") && (vm.cb_AutoLogin==true))
          vm.bt_LoginCommand.Execute(bt_Login);// RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        else if (vm.Email != "")
          Dispatcher.BeginInvoke(
            DispatcherPriority.ContextIdle,
            new Action(delegate ()
            {
              tb_Password.Focus();
            }));
        else if (vm.Password != "")
          Dispatcher.BeginInvoke(
            DispatcherPriority.ContextIdle,
            new Action(delegate ()
            {
              tb_Email.Focus();
            }));
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
    private void bt_Register_Click(object sender, RoutedEventArgs e)
    {
      string result = null;
      if (!GetTextBox(tb_RegisterEmail).isValidEmail())
        MessageBox.Show(vm.dict.GetMessage(8));
      else if (GetPasswordBox(tb_RegisterPassword1) == "")
        MessageBox.Show(vm.dict.GetMessage(9));
      else if (GetPasswordBox(tb_RegisterPassword1) != GetPasswordBox(tb_RegisterPassword2))
        MessageBox.Show(vm.dict.GetMessage(15));
      else
        result = vm.WSRunner("/Register?Email=" + GetTextBox(tb_RegisterEmail) + "&Pass=" + GetPasswordBox(tb_RegisterPassword1));
      if (result == null) return;
      //if (CheckResult(ref result)) return;
      MessageBox.Show(vm.dict.GetMessage(7));
      tc_RegisterLogin.SelectedIndex = 0;
      vm.Email = GetTextBox(tb_RegisterEmail);
      //SetTextBox(tb_Email, GetTextBox(tb_RegisterEmail));
      //SetPasswordBox(tb_Password, GetPasswordBox(tb_RegisterPassword1));
      vm.Password = GetPasswordBox(tb_RegisterPassword1);
      bt_Login.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
    }
    private void bt_Login_Click(object sender, RoutedEventArgs e)
    {
      //{
      //  if (!vm.Email.isValidEmail())
      //    MessageBox.Show(vm.dict.GetMessage(8));
      //  else if (vm.Password == "")
      //    MessageBox.Show(vm.dict.GetMessage(9));
      //  else
        
      //}
    }
    private void cb_Remember_Checked(object sender, RoutedEventArgs e)
    {
      vm.RememberAction(sender);
    }
    unsafe private void Button_Click_1(object sender, RoutedEventArgs e)
    {// (ip.SrcAddr>192.168.137.1 and ip.SrcAddr<192.168.137.255) and 
      //for (int i = 0; i < SLKey.Count; i++)
      //  if (clients.Find(x => x.Mac == SLKey[i].Substring(12)) != null) 
      //    if (clients.Find(x => x.Mac == SLKey[i].Substring(12)).Ip==tb_ip.Text) 
      //  Quota = Quota - SLValue[i];
      //return;
      //SLKey.Clear();
      //SLValue.Clear();
      return;
      //HANDLE handle;          // WinDivert handle
      /*
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
      IntPtr handle = WinDivertMethods.WinDivertOpen("ip.SrcAddr=" + tb_ip.Text + " or ip.DstAddr=" + tb_ip.Text, WINDIVERT_LAYER.WINDIVERT_LAYER_NETWORK, 1, WinDivertConstants.WINDIVERT_FLAG_SNIFF);// WinDivertOpen("...", 0, 0, 0);   // Open some filter
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
      */
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
      /*
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
      */
    }
    private void bt_Forgot_Click(object sender, RoutedEventArgs e)
    {
      String _tmp = "";
      if (vm.Email.isValidEmail())
      {
        _tmp = vm.WSRunner("/SendResetPasswordCode?EmailHash=" + vm.EmailHash);
        if (_tmp == null) return;
        //if (CheckResult(ref _tmp)) return;
      }
      else MessageBox.Show(vm.dict.GetMessage(8));
    }
    private void tb_Password_PasswordChanged(object sender, RoutedEventArgs e)
    {
      vPassword = GetPasswordBox(tb_Password);
    }

  }

}
