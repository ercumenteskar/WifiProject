using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using My;
using System.Data;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace WifiService
{
  public class Service1 : IService1
  {

    private string AFQSql()
    {
      return "CREATE TABLE #AFQT (Q NVarChar(MAX), A NVarChar(MAX))" + System.Environment.NewLine +
        ";WITH CTE(t1inx, t2inx, Sonuc) AS" + System.Environment.NewLine +
        "(" + System.Environment.NewLine +
        "  Select t1.inx t1inx, t2.inx t2inx, t2.Sonuc from dbo.StrToTable(@AFQ, '¶') t1" + System.Environment.NewLine +
        "  CROSS APPLY dbo.StrToTable(t1.Sonuc, '§') t2" + System.Environment.NewLine +
        ")" + System.Environment.NewLine +
        "insert into #AFQT" + System.Environment.NewLine +
        "  Select (CASE WHEN t2inx=1 THEN SUBSTRING(Sonuc, 3, LEN(Sonuc)) ELSE '' END), (CASE WHEN t2inx=1 THEN (Select SUBSTRING(Sonuc, 2, LEN(Sonuc)) from CTE c2 where c1.t1inx=c2.t1inx and c2.t2inx=2) ELSE '' END)" + System.Environment.NewLine +
        "  From CTE c1 Where t2inx=1" + System.Environment.NewLine;
    }
    public void Remove(String Email)
    {
      List<SqlParameter> Params = new List<SqlParameter>();
      Params.Add(new SqlParameter("@Email", Email));
      mfn.SelectToDS("Exec dbo.spr_Remove @Email", Params);
    }// Remove, TAMAMEN TESTLERİN HIZLI UYGULANABİLMESİ İÇİN OLUŞTURULDU, SİLİNECEK
    [MethodImpl(MethodImplOptions.NoInlining)]
    public string GetCurrentMethod()
    {
      StackTrace st = new StackTrace();
      StackFrame sf = st.GetFrame(1);

      return sf.GetMethod().Name;
    }

    public String RunAndCatch(String Sql, List<SqlParameter> Params, String MethodName, String AFQ, String LangCode)
    {
      string rtn = "";
      if (Params != null) 
      {
        if (Params.FirstOrDefault(x => x.ParameterName == "AFQ") == null)
          Params.Add(new SqlParameter("AFQ", AFQ!=null? AFQ : ""));
        if (Params.FirstOrDefault(x => x.ParameterName == "LangId") == null)
          Params.Add(new SqlParameter("LangId", System.Data.DbType.Int32) { Value = (LangCode == "tr" ? 2 : 1) });
      }
      try
      {
        rtn = mfn.GetSqlResult(AFQSql() + Sql, Params).Trim();
      }
      catch (Exception e)
      {
        if (e.Message[0] == '¶')
          rtn = AFQ + e.Message;
        else
          rtn = "¶E:" + mfn.GetMessage(5, mfn.GetSqlResult("insert Errorlog (msg, Source) Select '" + e.Message.Replace("'", "''") + "', '" + MethodName + "'; Select SCOPE_IDENTITY()"), (LangCode == "tr" ? 2 : 1));
      }
      return rtn;
    }

    public String GetSecurityCode(String EmailHash, String AFQ, String LangCode)
    {
      List<SqlParameter> Params = new List<SqlParameter>();
      Params.Add(new SqlParameter("@EmailHash", EmailHash));
      return RunAndCatch("Exec dbo.spr_GetSecurityCode @EmailHash OUTPUT; Select @EmailHash", Params, GetCurrentMethod(), AFQ, LangCode);
    }

    public String Login(String Evidence, String AFQ, String LangCode)
    {
      List<SqlParameter> Params = new List<SqlParameter>();
      Params.Add(new SqlParameter("@Evidence", Evidence));
      return RunAndCatch("Exec dbo.spr_Login @Evidence OUTPUT; Select @Evidence", Params, GetCurrentMethod(), AFQ, LangCode);
    }

    public String ConnectUS(String ClientEvidence, String ProviderEvidence, String AFQ, String LangCode)
    {
      List<SqlParameter> Params = new List<SqlParameter>();
      Params.Add(new SqlParameter("@ClientEvidence", ClientEvidence));
      Params.Add(new SqlParameter("@ProviderEvidence", ProviderEvidence));
      Params.Add(new SqlParameter("@ConnectionID", System.Data.DbType.Int64) { Value = 0 });
      return RunAndCatch("Exec dbo.spr_ConnectUS @ClientEvidence OUTPUT, @ProviderEvidence OUTPUT, @ConnectionID OUTPUT; Select isNull(@ClientEvidence,'')+';'+isNull(@ProviderEvidence,'')+';'+isNull(CAST(@ConnectionID as varchar),'')", Params, GetCurrentMethod(), AFQ, LangCode);
    }

    public String SetUsage(String ClientUsageMsg, String ProviderUsageMsg, long ConnectionID, String AFQ, String LangCode)
    {
      List<SqlParameter> Params = new List<SqlParameter>();
      Params.Add(new SqlParameter("@ClientUsageMsg", ClientUsageMsg));
      Params.Add(new SqlParameter("@ProviderUsageMsg", ProviderUsageMsg));
      Params.Add(new SqlParameter("@ConnectionID", System.Data.DbType.Int64) { Value = ConnectionID });
      return RunAndCatch("Exec dbo.spr_SetUsage @ClientUsageMsg OUTPUT, @ProviderUsageMsg OUTPUT, @ConnectionID; Select isNull(@ClientUsageMsg, '')+';'+isNull(@ProviderUsageMsg, '')", Params, GetCurrentMethod(), AFQ, LangCode);
    }

    public String Register(String Email, String Pass, String AFQ, String LangCode)
    {
      List<SqlParameter> Params = new List<SqlParameter>();
      Params.Add(new SqlParameter("@Email", Email));
      Params.Add(new SqlParameter("@Pass", Pass));
      Params.Add(new SqlParameter("@SecurityCode", ""));
      return RunAndCatch("Exec dbo.spr_Register @Email, @Pass, @LangId, @SecurityCode OUTPUT; Select @SecurityCode", Params, GetCurrentMethod(), AFQ, LangCode);
    }

    public String SendResetPasswordCode(String EmailHash, String LangCode, String AFQ)
    {
      List<SqlParameter> Params = new List<SqlParameter>();
      Params.Add(new SqlParameter("@EmailHash", EmailHash));
      string rtn = RunAndCatch("Exec dbo.spr_GetResetPasswordCode @EmailHash, @LangId", Params, GetCurrentMethod(), AFQ, LangCode);
      if ((rtn.Length > 33) && (rtn[32] == ';') && (rtn.Contains("@")))
      {
        Params.Clear();
        String Email = rtn.Substring(33);
        String Message = mfn.GetMessage(3, rtn.Substring(0, 32), (LangCode == "tr" ? 2 : 1));
        //TODO SendMail(Email, Message);
        if (rtn != "")
          rtn = "¶M:" + mfn.GetMessage(4, Email, (LangCode == "tr" ? 2 : 1));// Your reset password mail send succesfully. Please remember to look at your spam folder too.
      }
      return rtn;
    }

    public String ResetPassword(String EmailHash, String RPC, String Pass, String LangCode, String AFQ)
    {
      List<SqlParameter> Params = new List<SqlParameter>();
      Params.Add(new SqlParameter("@EmailHash", EmailHash));
      Params.Add(new SqlParameter("@RPC", RPC));
      Params.Add(new SqlParameter("@Pass", Pass));
      return RunAndCatch("Exec dbo.spr_ResetPassword @EmailHash, @RPC, @Pass, @LangId", Params, GetCurrentMethod(), AFQ, LangCode);
    }

  }
}
