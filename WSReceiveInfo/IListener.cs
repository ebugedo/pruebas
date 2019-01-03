using System.ServiceModel;

namespace WSReceiveInfo
{
    [ServiceContract]
    public interface IListener
    {
        [OperationContract]
        string GetData(string xml);

        [OperationContract]
        string GetDataParam(string xml, string parametersXml);
    }
}
