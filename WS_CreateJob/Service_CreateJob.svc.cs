using WS_CreateJob.Datos;

namespace WS_CreateJob
{
    public class ServiceSmm : IService_CreateJob
    {
        bool IService_CreateJob.SetDataJob(string xml)
        {
            try
            {
                CJobDal.SaveJob(xml);
                return true;
            }
            catch { return false; }
        }
    }
}
