﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.36415
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace SEA.PSCM_ReceiveInfo {
    
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    [System.ServiceModel.ServiceContractAttribute(ConfigurationName="PSCM_ReceiveInfo.IListener")]
    public interface IListener {
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/IListener/GetData", ReplyAction="http://tempuri.org/IListener/GetDataResponse")]
        string GetData(string xml);
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/IListener/GetDataParam", ReplyAction="http://tempuri.org/IListener/GetDataParamResponse")]
        string GetDataParam(string xml, string parametersXml);
    }
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    public interface IListenerChannel : SEA.PSCM_ReceiveInfo.IListener, System.ServiceModel.IClientChannel {
    }
    
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    public partial class ListenerClient : System.ServiceModel.ClientBase<SEA.PSCM_ReceiveInfo.IListener>, SEA.PSCM_ReceiveInfo.IListener {
        
        public ListenerClient() {
        }
        
        public ListenerClient(string endpointConfigurationName) : 
                base(endpointConfigurationName) {
        }
        
        public ListenerClient(string endpointConfigurationName, string remoteAddress) : 
                base(endpointConfigurationName, remoteAddress) {
        }
        
        public ListenerClient(string endpointConfigurationName, System.ServiceModel.EndpointAddress remoteAddress) : 
                base(endpointConfigurationName, remoteAddress) {
        }
        
        public ListenerClient(System.ServiceModel.Channels.Binding binding, System.ServiceModel.EndpointAddress remoteAddress) : 
                base(binding, remoteAddress) {
        }
        
        public string GetData(string xml) {
            return base.Channel.GetData(xml);
        }
        
        public string GetDataParam(string xml, string parametersXml) {
            return base.Channel.GetDataParam(xml, parametersXml);
        }
    }
}
