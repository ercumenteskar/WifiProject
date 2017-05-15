using System;
using System.ComponentModel;
using My;
using System.Globalization;
using NetFwTypeLib;
using System.Linq;
using System.Threading;
using System.Collections.Generic;

namespace WifiSolution
{
  public class WifiAccount : INotifyPropertyChanged
  {
    private WinFuncs wf;
    private WifiCommon wc;
    public WifiAccount(WifiCommon _wc, WinFuncs _wf)
    {
      wf = _wf;
      wc = _wc;
      if (wf.ReadFromRegistry("LastMode").ToString() != "")
        LastMode = int.Parse(wf.ReadFromRegistry("LastMode").ToString());
      if (wf.ReadFromRegistry(wc.AType + "EmailRemember").ToString() != "")
        Email = saes.DecryptToString(wf.ReadFromRegistry(wc.AType + "EmailRemember").ToString());
      if (wf.ReadFromRegistry(wc.AType + "PassRemember").ToString() != "")
        Password = saes.DecryptToString(wf.ReadFromRegistry(wc.AType + "PassRemember").ToString());
      if (Email != "") EmailRememberIsChecked = true;
      if (Password != "") PassRememberIsChecked = true;
      AutoLogin = (wf.ReadFromRegistry(wc.AType + "AutoLogin").ToString() == "*");
      AutoConnect = (wf.ReadFromRegistry(wc.AType + "AutoConnect").ToString() == "*");
      if (!mfn.IsAdministrator())
      {
        wf.ShowMessageBox(wc.dict.GetMessage(10));
        wf.Shutdown();
      }
      //FirewallAyarla();
      if ((Email != "") || (Password != ""))
      {
        tc_RegisterLoginSelectedIndex = 0;
        if ((Email != "") && (Password != "") && (AutoLogin == true) && (!Logged))
          Login();
      }
    }
    ~WifiAccount()
    {
    }

    String defaultfwbackupfilename = AppDomain.CurrentDomain.BaseDirectory + "DefaultFirewallPolicies.wfw";
    String ourfwbackupfilename = AppDomain.CurrentDomain.BaseDirectory + "ActiveFirewallPolicies.wfw";
    public String ProviderIp = "";
    SimpleAES saes = new SimpleAES();
    public event PropertyChangedEventHandler PropertyChanged;
    private void OnPropertyChanged(String property) { PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property)); }
    public String SecurityCode = "";
    private int _tc_RegisterLoginSelectedIndex = 1;
    public int tc_RegisterLoginSelectedIndex
    {
      get { return _tc_RegisterLoginSelectedIndex; }
      set { _tc_RegisterLoginSelectedIndex = value; OnPropertyChanged(nameof(tc_RegisterLoginSelectedIndex)); }
    }
    private bool? _EmailRememberIsChecked = false;
    public bool? EmailRememberIsChecked { get { return _EmailRememberIsChecked; } set { _EmailRememberIsChecked = value; RememberAction(value == true, wc.AType + "EmailRemember"); OnPropertyChanged(nameof(EmailRememberIsChecked)); } }
    private bool? _PassRememberIsChecked = false;
    public bool? PassRememberIsChecked { get { return _PassRememberIsChecked; } set { _PassRememberIsChecked = value; RememberAction(value == true, wc.AType + "PassRemember"); OnPropertyChanged(nameof(PassRememberIsChecked)); } }
    private bool? _AutoLogin = false;
    public bool? AutoLogin { get { return _AutoLogin; } set { _AutoLogin = value; RememberAction(value == true, wc.AType + "AutoLogin"); OnPropertyChanged(nameof(AutoLogin)); } }
    private bool? _AutoConnect = false;
    public bool? AutoConnect { get { return _AutoConnect; } set { _AutoConnect = value; RememberAction(value == true, wc.AType + "AutoConnect"); OnPropertyChanged(nameof(AutoConnect)); } }
    private int _LastMode = 0;
    public int LastMode { get { return _LastMode; } set { _LastMode = value; } }
    private String password = "";
    public String Password { get { return password; } set { password = value; OnPropertyChanged(nameof(Password)); OnPropertyChanged(nameof(canLogin)); } }
    private String _email = "";
    public String Email
    {
      get { return _email; }
      set { _email = value; OnPropertyChanged(nameof(Email)); OnPropertyChanged(nameof(canLogin)); }
    }
    public String EmailHash { get { return Email.ToLower().HashMD5(); } }
    private String _registerEmail = "";
    public String RegisterEmail
    {
      get { return _registerEmail; }
      set { _registerEmail = value; OnPropertyChanged(nameof(RegisterEmail)); }
    }
    private String _registerPassword1 = "";
    public String RegisterPassword1
    {
      get { return _registerPassword1; }
      set { _registerPassword1 = value; OnPropertyChanged(nameof(RegisterPassword1)); }
    }
    private String _registerPassword2 = "";
    public String RegisterPassword2
    {
      get { return _registerPassword2; }
      set { _registerPassword2 = value; OnPropertyChanged(nameof(RegisterPassword2)); }
    }
    private RelayCommand _bt_RegisterCommand;
    public RelayCommand bt_RegisterCommand
    {
      get
      {
        if (_bt_RegisterCommand == null)
        {
          _bt_RegisterCommand = new RelayCommand(p => RegisterCommand());//, p => _canLoginClick);
        }
        return _bt_RegisterCommand;
      }
    }

    private void RegisterCommand()
    {
      string result = null;
      if (!RegisterEmail.isValidEmail())
        wf.ShowMessageBox(wc.dict.GetMessage(8));
      else if (RegisterPassword1 == "")
        wf.ShowMessageBox(wc.dict.GetMessage(9));
      else if (RegisterPassword1 != RegisterPassword2)
        wf.ShowMessageBox(wc.dict.GetMessage(15));
      else
        result = wc.GetWebstring("/Register?Email=" + RegisterEmail + "&Pass=" + RegisterPassword1, ProviderIp);
      //wc.WSRunner("/Register?Email=" + RegisterEmail + "&Pass=" + RegisterPassword1);
      //if (result == null) return;
      //if (!wc.CheckResult(ref result)) return;
      if (!mfn.isValidHexString(result, 32)) return;
      wf.ShowMessageBox(wc.dict.GetMessage(7));
      tc_RegisterLoginSelectedIndex = 0;
      Email = RegisterEmail;
      Password = RegisterPassword1;
      Login();
    }
    private RelayCommand _bt_ForgotCommand;
    public RelayCommand bt_ForgotCommand
    {
      get
      {
        if (_bt_ForgotCommand == null)
        {
          _bt_ForgotCommand = new RelayCommand(p => ForgotCommand());//, p => _canLoginClick);
        }
        return _bt_ForgotCommand;
      }
    }
    private void ForgotCommand()
    {
      String _tmp = "";
      if (Email.isValidEmail())
      {
        _tmp = wc.GetWebstring("/SendResetPasswordCode?EmailHash=" + EmailHash, ProviderIp);
        //wc.WSRunner("/SendResetPasswordCode?EmailHash=" + EmailHash);
        //if (!wc.CheckResult(ref result)) return;
        if (!mfn.isValidHexString(_tmp, 32)) return;
      }
      else wf.ShowMessageBox(wc.dict.GetMessage(8));
    }
    public bool canLogin { get { return Email.isValidEmail() && (Password != ""); } }
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
        OnPropertyChanged(nameof(tb_EmailIsEnabled));
        OnPropertyChanged(nameof(tb_PasswordIsEnabled));
        OnPropertyChanged(nameof(bt_LoginContent));

        if (!value) Quota = 0;
      }
    }
    public bool? tb_PasswordIsEnabled { get { return !Logged; } } // set { _tb_PasswordIsEnabled = value; OnPropertyChanged(nameof(tb_PasswordIsEnabled)); } 
    public bool? tb_EmailIsEnabled { get { return !Logged; } } // set { _tb_EmailIsEnabled = value; OnPropertyChanged(nameof(tb_EmailIsEnabled)); } 
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

    public void RememberAction(bool IsChecked = false, String cbName = null)
    {
      if ((cbName.Contains("AutoLogin")) || (cbName.Contains("AutoConnect")))
        wf.WriteToRegistry(cbName, IsChecked ? "*" : "");
      else if (cbName.Contains("EmailRemember"))
        wf.WriteToRegistry(cbName, IsChecked ? saes.EncryptToString(Email) : "");
      else if (cbName.Contains("PassRemember"))
        wf.WriteToRegistry(cbName, IsChecked ? saes.EncryptToString(Password) : "");
    }

    public bool SetSecurityCode()
    {
      SecurityCode = wc.GetWebstring("/GetSecurityCode?EmailHash=" + EmailHash, ProviderIp);
      //wc.WSRunner("/GetSecurityCode?EmailHash=" + EmailHash);
      //if (!wc.CheckResult(ref result)) return;
      return mfn.isValidHexString(SecurityCode, 32);
      //return !wc.CheckResult(ref SecurityCode);
    }

    public void Login()
    {
      Quota = 0;
      if (!SetSecurityCode()) return;
      Console.WriteLine("SecurityCode : " + SecurityCode);
      String _tmp = wc.GetWebstring("/Login?Evidence=" + (EmailHash + (SecurityCode + (EmailHash + Password).HashMD5() + ("").HashMD5()).HashMD5()), ProviderIp);
      if ((_tmp.Length > 33) && (_tmp.Substring(32, 1) == ";"))
      {
        Quota = long.Parse(_tmp.Substring(33));
        SecurityCode = _tmp.Substring(0, 32);
        Logged = true;
        if (EmailRememberIsChecked == true)
          RememberAction(true, wc.AType + "EmailRemember");
        if (PassRememberIsChecked == true)
          RememberAction(true, wc.AType + "PassRemember");
      }
    }

    public void Logout()
    {
      Logged = false;
    }


    public String myEvidence(String Mesaj = "")
    {
      if (!SetSecurityCode())
        return "";
      else
        return EmailHash + (SecurityCode + (EmailHash + Password).HashMD5() + Mesaj.HashMD5()).HashMD5() + Mesaj;
    }

    private void FirewallAyarla(bool Acilis = true)
    {
      //if (Acilis)
      //{
      //  if (!System.IO.File.Exists(defaultfwbackupfilename))
      //    wf.Netsh("advfirewall export \"" + defaultfwbackupfilename + "\"");

      //  bool FirewallEnabled = (wf.ReadFromRegistry("EnableFirewall", "M", @"System\CurrentControlSet\Services\SharedAccess\Parameters\FirewallPolicy\PublicProfile").ToString() == "1");
      //  if (!FirewallEnabled)
      //    wf.Netsh("advfirewall set publicprofile state on");
      //  //wf.Netsh("advfirewall reset"); reset tüm profillerde firewall u açıyor...
      //  INetFwServices svc = (Activator.CreateInstance(Type.GetTypeFromCLSID(new Guid("{304CE942-6E39-40D8-943A-B913C40C9CD4}"))) as INetFwMgr).LocalPolicy.CurrentProfile.Services;
      //  /*
      //  String strfap = svc.Cast<INetFwService>()?.FirstOrDefault(sc => sc.Type == NET_FW_SERVICE_TYPE_.NET_FW_SERVICE_FILE_AND_PRINT)?.Name;
      //  String strrdp = svc.Cast<INetFwService>()?.FirstOrDefault(sc => sc.Type == NET_FW_SERVICE_TYPE_.NET_FW_SERVICE_REMOTE_DESKTOP)?.Name;
      //  String strmax = svc.Cast<INetFwService>()?.FirstOrDefault(sc => sc.Type == NET_FW_SERVICE_TYPE_.NET_FW_SERVICE_TYPE_MAX)?.Name;
      //  String strpnp = svc.Cast<INetFwService>()?.FirstOrDefault(sc => sc.Type == NET_FW_SERVICE_TYPE_.NET_FW_SERVICE_UPNP)?.Name;
      //  */
      //  INetFwPolicy2 fwPolicy = (INetFwPolicy2)Activator.CreateInstance(Type.GetTypeFromProgID("HNetCfg.FwPolicy2"));
      //  List<INetFwRule> RuleList = new List<INetFwRule>();
      //  foreach (INetFwRule rule in fwPolicy.Rules)
      //    if ((rule.Enabled) && (rule.Profiles == 7))
      //      if (rule.Name.ToLower().StartsWith("file and printer sharing") || rule.Name.ToLower().StartsWith("remote desktop") || rule.Name.ToLower().StartsWith("network discovery") || rule.Name.ToLower().StartsWith("remote assistance"))
      //      wf.Netsh("advfirewall firewall set rule name=\"" + rule.Name + "\" new enable=No profile=public");
      //  /*
      //  wf.Netsh("advfirewall firewall set rule group=" + strfap + " new enable=No profile=public");
      //  wf.Netsh("advfirewall firewall set rule group=" + strrdp + " new enable=No profile=public");
      //  wf.Netsh("advfirewall firewall set rule group=" + strmax + " new enable=No profile=public");
      //  wf.Netsh("advfirewall firewall set rule group=" + strpnp + " new enable=No profile=public");
      //  */
      //  if (System.IO.File.Exists(ourfwbackupfilename))
      //    wf.Netsh("advfirewall import \"" + ourfwbackupfilename + "\"");
      //}
      //else
      //{
      //  if (System.IO.File.Exists(ourfwbackupfilename))
      //    System.IO.File.Delete(ourfwbackupfilename);
      //  wf.Netsh("advfirewall export \"" + ourfwbackupfilename + "\"");
      //  if (System.IO.File.Exists(defaultfwbackupfilename))
      //  { 
      //    wf.Netsh("advfirewall import \"" + defaultfwbackupfilename + "\"");
      //    wf.CmdExec("del", "/Q \"" + defaultfwbackupfilename + "\"");
      //  }
      //}
    }
  }
}
