﻿using System;
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
    public String GetSecurityCode(String TelNoHash)
    {
      List<SqlParameter> Params = new List<SqlParameter>();
      //      Params.Add(new SqlParameter("@Id", System.Data.DbType.Int32) { Value = Convert.ToInt32(Id) });
      Params.Add(new SqlParameter("@TelNoHash", TelNoHash));
      return mfn.SelectToDS("Exec dbo.spr_GetSecurityCode @TelNoHash OUTPUT; Select @TelNoHash", Params).Tables[0].Rows[0][0].ToString();
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

    public String SetUsage(String ClientUsageMsg, String ProviderUsageMsg, Int64 ConnectionID)
    {
      List<SqlParameter> Params = new List<SqlParameter>();
      Params.Add(new SqlParameter("@ClientUsageMsg", ClientUsageMsg));
      Params.Add(new SqlParameter("@ProviderUsageMsg", ProviderUsageMsg));
      Params.Add(new SqlParameter("@ConnectionID", System.Data.DbType.Int64) { Value = ConnectionID });
      DataSet ds = mfn.SelectToDS("Exec dbo.spr_SetUsage @ClientUsageMsg OUTPUT, @ProviderUsageMsg OUTPUT, @ConnectionID OUTPUT; Select @ClientUsageMsg, @ProviderUsageMsg", Params);
      return ds.Tables[0].Rows[0][0].ToString().Trim() + ";" + ds.Tables[0].Rows[0][1].ToString().Trim();
    }

    public String Register(String @TelNo, String @Pass, Int64 @Quota)
    {
      List<SqlParameter> Params = new List<SqlParameter>();
      Params.Add(new SqlParameter("@TelNo", TelNo));
      Params.Add(new SqlParameter("@Pass", Pass));
      Params.Add(new SqlParameter("@Quota", System.Data.DbType.Int64) { Value = Quota });
      Params.Add(new SqlParameter("@SecurityCode", ""));
      return mfn.SelectToDS("Exec dbo.spr_Register @TelNo, @Pass, @Quota, @SecurityCode OUTPUT; Select @SecurityCode", Params).Tables[0].Rows[0][0].ToString().Trim();
    }
    

  }
}
