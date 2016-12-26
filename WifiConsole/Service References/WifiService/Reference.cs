﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.34209
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace ConsoleApplication1.WifiService {
    
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    [System.ServiceModel.ServiceContractAttribute(ConfigurationName="WifiService.IService1")]
    public interface IService1 {
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/IService1/GetSecurityCode", ReplyAction="http://tempuri.org/IService1/GetSecurityCodeResponse")]
        string GetSecurityCode(string TelNoHash);
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/IService1/GetSecurityCode", ReplyAction="http://tempuri.org/IService1/GetSecurityCodeResponse")]
        System.Threading.Tasks.Task<string> GetSecurityCodeAsync(string TelNoHash);
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/IService1/Login", ReplyAction="http://tempuri.org/IService1/LoginResponse")]
        string Login(string Evidence);
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/IService1/Login", ReplyAction="http://tempuri.org/IService1/LoginResponse")]
        System.Threading.Tasks.Task<string> LoginAsync(string Evidence);
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/IService1/ConnectUS", ReplyAction="http://tempuri.org/IService1/ConnectUSResponse")]
        string ConnectUS(string ClientEvidence, string ProviderEvidence);
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/IService1/ConnectUS", ReplyAction="http://tempuri.org/IService1/ConnectUSResponse")]
        System.Threading.Tasks.Task<string> ConnectUSAsync(string ClientEvidence, string ProviderEvidence);
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/IService1/SetUsage", ReplyAction="http://tempuri.org/IService1/SetUsageResponse")]
        string SetUsage(string ClientUsageMsg, string ProviderUsageMsg, long ConnectionID);
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/IService1/SetUsage", ReplyAction="http://tempuri.org/IService1/SetUsageResponse")]
        System.Threading.Tasks.Task<string> SetUsageAsync(string ClientUsageMsg, string ProviderUsageMsg, long ConnectionID);
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/IService1/Register", ReplyAction="http://tempuri.org/IService1/RegisterResponse")]
        string Register(string TelNo, string Pass, long Quota);
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/IService1/Register", ReplyAction="http://tempuri.org/IService1/RegisterResponse")]
        System.Threading.Tasks.Task<string> RegisterAsync(string TelNo, string Pass, long Quota);
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/IService1/Remove", ReplyAction="http://tempuri.org/IService1/RemoveResponse")]
        void Remove(string TelNo);
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/IService1/Remove", ReplyAction="http://tempuri.org/IService1/RemoveResponse")]
        System.Threading.Tasks.Task RemoveAsync(string TelNo);
    }
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    public interface IService1Channel : ConsoleApplication1.WifiService.IService1, System.ServiceModel.IClientChannel {
    }
    
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    public partial class Service1Client : System.ServiceModel.ClientBase<ConsoleApplication1.WifiService.IService1>, ConsoleApplication1.WifiService.IService1 {
        
        public Service1Client() {
        }
        
        public Service1Client(string endpointConfigurationName) : 
                base(endpointConfigurationName) {
        }
        
        public Service1Client(string endpointConfigurationName, string remoteAddress) : 
                base(endpointConfigurationName, remoteAddress) {
        }
        
        public Service1Client(string endpointConfigurationName, System.ServiceModel.EndpointAddress remoteAddress) : 
                base(endpointConfigurationName, remoteAddress) {
        }
        
        public Service1Client(System.ServiceModel.Channels.Binding binding, System.ServiceModel.EndpointAddress remoteAddress) : 
                base(binding, remoteAddress) {
        }
        
        public string GetSecurityCode(string TelNoHash) {
            return base.Channel.GetSecurityCode(TelNoHash);
        }
        
        public System.Threading.Tasks.Task<string> GetSecurityCodeAsync(string TelNoHash) {
            return base.Channel.GetSecurityCodeAsync(TelNoHash);
        }
        
        public string Login(string Evidence) {
            return base.Channel.Login(Evidence);
        }
        
        public System.Threading.Tasks.Task<string> LoginAsync(string Evidence) {
            return base.Channel.LoginAsync(Evidence);
        }
        
        public string ConnectUS(string ClientEvidence, string ProviderEvidence) {
            return base.Channel.ConnectUS(ClientEvidence, ProviderEvidence);
        }
        
        public System.Threading.Tasks.Task<string> ConnectUSAsync(string ClientEvidence, string ProviderEvidence) {
            return base.Channel.ConnectUSAsync(ClientEvidence, ProviderEvidence);
        }
        
        public string SetUsage(string ClientUsageMsg, string ProviderUsageMsg, long ConnectionID) {
            return base.Channel.SetUsage(ClientUsageMsg, ProviderUsageMsg, ConnectionID);
        }
        
        public System.Threading.Tasks.Task<string> SetUsageAsync(string ClientUsageMsg, string ProviderUsageMsg, long ConnectionID) {
            return base.Channel.SetUsageAsync(ClientUsageMsg, ProviderUsageMsg, ConnectionID);
        }
        
        public string Register(string TelNo, string Pass, long Quota) {
            return base.Channel.Register(TelNo, Pass, Quota);
        }
        
        public System.Threading.Tasks.Task<string> RegisterAsync(string TelNo, string Pass, long Quota) {
            return base.Channel.RegisterAsync(TelNo, Pass, Quota);
        }
        
        public void Remove(string TelNo) {
            base.Channel.Remove(TelNo);
        }
        
        public System.Threading.Tasks.Task RemoveAsync(string TelNo) {
            return base.Channel.RemoveAsync(TelNo);
        }
    }
}
