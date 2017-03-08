using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;

namespace WifiService
{
  // NOTE: You can use the "Rename" command on the "Refactor" menu to change the interface name "IService1" in both code and config file together.
  [ServiceContract]
  public interface IService1
  {
    [OperationContract]
    String GetSecurityCode(String EmailHash, String AFQ, String LangCode);
    [OperationContract]
    String Login(String Evidence, String AFQ, String LangCode);
    [OperationContract]
    String ConnectUS(String ClientEvidence, String ProviderEvidence, String AFQ, String LangCode);
    [OperationContract]
    String SetUsage(String ClientUsageMsg, String ProviderUsageMsg, long ConnectionID, String AFQ, String LangCode);
    [OperationContract]
    String Register(String Email, String Pass, String AFQ, String LangCode);
    [OperationContract]
    // Remove, TAMAMEN TESTLERİN HIZLI UYGULANABİLMESİ İÇİN OLUŞTURULDU, SİLİNECEK
    void Remove(String @Email);
    [OperationContract]
    String SendResetPasswordCode(String EmailHash, String LangCode, String AFQ);


  }

}
