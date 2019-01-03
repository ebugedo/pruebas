using System.ServiceModel;

namespace WS_CreateJob
{
    [ServiceContract]
    public interface IService_CreateJob
    {
        [OperationContract]
        bool SetDataJob(string xml);
    }
}
