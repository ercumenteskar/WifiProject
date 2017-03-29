using System;
using System.ComponentModel;
using My;
using System.Globalization;

namespace WifiSolution
{
  public class WifiAccount : INotifyPropertyChanged
  {
    public WinFuncs wf;
    public WifiCommon wc;
    private MyDictionary dict;
    public WifiAccount(String type, String projectName, String resource_data)
    {
      Type = type;
      dict = new MyDictionary(CultureInfo.CurrentUICulture.TwoLetterISOLanguageName, resource_data.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries));
      wf = new WinFuncs(projectName);
      wc = new WifiCommon(dict, projectName);
      if (wf.ReadFromRegistry(Type + "EmailRemember").ToString() != "")
        Email = saes.DecryptToString(wf.ReadFromRegistry(Type + "EmailRemember").ToString());
      if (wf.ReadFromRegistry(Type + "PassRemember").ToString() != "")
        Password = saes.DecryptToString(wf.ReadFromRegistry(Type + "PassRemember").ToString());
      if (Email != "") cb_EmailRememberIsChecked = true;
      if (Password != "") cb_PassRememberIsChecked = true;
      cb_AutoLogin = (wf.ReadFromRegistry(Type + "AutoLogin").ToString() == "*");
      cb_AutoConnect = (wf.ReadFromRegistry(Type + "AutoConnect").ToString() == "*");

      if (!mfn.IsAdministrator())
      {
        wf.ShowMessageBox(dict.GetMessage(10));
        wf.Shutdown();
      }
      if ((Email != "") || (Password != ""))
      {
        tc_RegisterLoginSelectedIndex = 0;
        if ((Email != "") && (Password != "") && (cb_AutoLogin == true) && (!Logged))
          Login();
      }

    }
    public String ProviderIp = "";
    private String Type = "";
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
    private bool? _cb_EmailRememberIsChecked = false;
    public bool? cb_EmailRememberIsChecked { get { return _cb_EmailRememberIsChecked; } set { _cb_EmailRememberIsChecked = value; RememberAction(value == true, Type + "EmailRemember"); OnPropertyChanged(nameof(cb_EmailRememberIsChecked)); } }
    private bool? _cb_PassRememberIsChecked = false;
    public bool? cb_PassRememberIsChecked { get { return _cb_PassRememberIsChecked; } set { _cb_PassRememberIsChecked = value; RememberAction(value == true, Type + "PassRemember"); OnPropertyChanged(nameof(cb_PassRememberIsChecked)); } }
    private bool? _cb_AutoLogin = false;
    public bool? cb_AutoLogin { get { return _cb_AutoLogin; } set { _cb_AutoLogin = value; RememberAction(value == true, "AutoLogin"); OnPropertyChanged(nameof(cb_AutoLogin)); } }
    private bool? _cb_AutoConnect = false;
    public bool? cb_AutoConnect { get { return _cb_AutoConnect; } set { _cb_AutoConnect = value; RememberAction(value == true, "AutoConnect"); OnPropertyChanged(nameof(cb_AutoConnect)); } }
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
        wf.ShowMessageBox(dict.GetMessage(8));
      else if (RegisterPassword1 == "")
        wf.ShowMessageBox(dict.GetMessage(9));
      else if (RegisterPassword1 != RegisterPassword2)
        wf.ShowMessageBox(dict.GetMessage(15));
      else
        result = wc.GetWebstring("/Register?Email=" + RegisterEmail + "&Pass=" + RegisterPassword1, ProviderIp);
      //wc.WSRunner("/Register?Email=" + RegisterEmail + "&Pass=" + RegisterPassword1);
      //if (result == null) return;
      //if (!wc.CheckResult(ref result)) return;
      if (!mfn.isValidHexString(result, 32)) return;
      wf.ShowMessageBox(dict.GetMessage(7));
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
      else wf.ShowMessageBox(dict.GetMessage(8));
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
      String value = "";
      if (cbName.Contains("EmailRemember")) value = saes.EncryptToString(Email);
      else if (cbName.Contains("PassRemember")) value = saes.EncryptToString(Password);
      //if (cbName == "PEmailRemember") value = saes.EncryptToString(Email);
      //else if (cbName == "PPassRemember") value = saes.EncryptToString(Password);
      if (value == "")
      {
        if (((cbName == "AutoLogin") || (cbName == "AutoConnect")))
          wf.WriteToRegistry(Type + cbName, IsChecked ? "*" : "");
      }
      else
      {
        if ((IsChecked) && (Logged)) wf.WriteToRegistry(cbName, value);
        else if (IsChecked) wf.WriteToRegistry(cbName, "");
      }

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
        if (cb_EmailRememberIsChecked == true)
          RememberAction(true, Type + "EmailRemember");
        if (cb_PassRememberIsChecked == true)
          RememberAction(true, Type + "PassRemember");
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

  }
}
