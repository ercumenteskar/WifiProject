using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using My;
using System.Data;

namespace WifiService
{
  // NOTE: You can use the "Rename" command on the "Refactor" menu to change the class name "Service1" in both code and config file together.
  public class Service1 : IService1
  {
    public String GetSecurityCode(String EmailHash)
    {
      List<SqlParameter> Params = new List<SqlParameter>();
      //      Params.Add(new SqlParameter("@Id", System.Data.DbType.Int32) { Value = Convert.ToInt32(Id) });
      Params.Add(new SqlParameter("@EmailHash", EmailHash));
      return mfn.SelectToDS("Exec dbo.spr_GetSecurityCode @EmailHash OUTPUT; Select @EmailHash", Params).Tables[0].Rows[0][0].ToString();
    }

    public String Login(String Evidence)
    {
      List<SqlParameter> Params = new List<SqlParameter>();
      Params.Add(new SqlParameter("@Evidence", Evidence));
      return mfn.SelectToDS("Exec dbo.spr_Login @Evidence OUTPUT; Select @Evidence", Params).Tables[0].Rows[0][0].ToString().Trim();
    }

    public String ConnectUS(String ClientEvidence, String ProviderEvidence)
    {
      List<SqlParameter> Params = new List<SqlParameter>();
      Params.Add(new SqlParameter("@ClientEvidence", ClientEvidence));
      Params.Add(new SqlParameter("@ProviderEvidence", ProviderEvidence));
      Params.Add(new SqlParameter("@ConnectionID", System.Data.DbType.Int64) { Value = 0 });
      DataSet ds = mfn.SelectToDS("Exec dbo.spr_ConnectUS @ClientEvidence OUTPUT, @ProviderEvidence OUTPUT, @ConnectionID OUTPUT; Select @ClientEvidence, @ProviderEvidence, @ConnectionID", Params);
      return ds.Tables[0].Rows[0][0].ToString().Trim() + ";" + ds.Tables[0].Rows[0][1].ToString().Trim() + ";" + ds.Tables[0].Rows[0][2].ToString().Trim();
    }

    public String SetUsage(String ClientUsageMsg, String ProviderUsageMsg, long ConnectionID)
    {
      List<SqlParameter> Params = new List<SqlParameter>();
      Params.Add(new SqlParameter("@ClientUsageMsg", ClientUsageMsg));
      Params.Add(new SqlParameter("@ProviderUsageMsg", ProviderUsageMsg));
      Params.Add(new SqlParameter("@ConnectionID", System.Data.DbType.Int64) { Value = ConnectionID });
      DataSet ds = mfn.SelectToDS("Exec dbo.spr_SetUsage @ClientUsageMsg OUTPUT, @ProviderUsageMsg OUTPUT, @ConnectionID OUTPUT; Select @ClientUsageMsg, @ProviderUsageMsg", Params);
      return ds.Tables[0].Rows[0][0].ToString().Trim() + ";" + ds.Tables[0].Rows[0][1].ToString().Trim();
    }

    public String Register(String @Email, String @Pass, String LangCode, String @AFQ)
    {
      List<SqlParameter> Params = new List<SqlParameter>();
      Params.Add(new SqlParameter("@Email", Email));
      Params.Add(new SqlParameter("@Pass", Pass));
      Params.Add(new SqlParameter("@LangId", System.Data.DbType.Int32) { Value = (LangCode=="tr" ? 2 : 1) });
      Params.Add(new SqlParameter("@SecurityCode", ""));
      Params.Add(new SqlParameter("@AFQ", AFQ));
      string rtn = "";
      try
      {
        DataTable tbl = mfn.SelectToDS(AFQSql() + "Exec dbo.spr_Register @Email, @Pass, @LangId, @SecurityCode OUTPUT; Select @SecurityCode", Params).Tables[0];
        rtn = tbl.Rows[0][0].ToString().Trim(); 
      }
      catch (Exception e)
      {
        if (e.Message[0] == '¶')
          rtn = @AFQ + e.Message;
        else 
        {
          string inx = mfn.GetSqlResult("insert Errorlog (msg) Select '" + e.Message.Replace("'", "''") + "'; Select SCOPE_IDENTITY()");
          rtn = "¶E:Hata oluştu. Hata kayıt numarası : "+inx;
        } 
        
      }
      return rtn;
    }

    private string AFQSql()
    {
      return "CREATE TABLE #AFQT (Q NVarChar(MAX), A NVarChar(MAX))"+System.Environment.NewLine+
        ";WITH CTE(t1inx, t2inx, Sonuc) AS"+System.Environment.NewLine+
        "("+System.Environment.NewLine+
        "  Select t1.inx t1inx, t2.inx t2inx, t2.Sonuc from dbo.StrToTable(@AFQ, '¶') t1"+System.Environment.NewLine+
        "  CROSS APPLY dbo.StrToTable(t1.Sonuc, '§') t2"+System.Environment.NewLine+
        ")"+System.Environment.NewLine+
        "insert into #AFQT"+System.Environment.NewLine+
        "  Select (CASE WHEN t2inx=1 THEN SUBSTRING(Sonuc, 3, LEN(Sonuc)) ELSE '' END), (CASE WHEN t2inx=1 THEN (Select SUBSTRING(Sonuc, 2, LEN(Sonuc)) from CTE c2 where c1.t1inx=c2.t1inx and c2.t2inx=2) ELSE '' END)" + System.Environment.NewLine +
        "  From CTE c1 Where t2inx=1" + System.Environment.NewLine;
    }

    // Remove, TAMAMEN TESTLERİN HIZLI UYGULANABİLMESİ İÇİN OLUŞTURULDU, SİLİNECEK
    public void Remove(String @Email)
    {
      List<SqlParameter> Params = new List<SqlParameter>();
      Params.Add(new SqlParameter("@Email", Email));
      mfn.SelectToDS("Exec dbo.spr_Remove @Email", Params);
    }

    private void Main()
    {
    }
  }
}
