using System;
using WS_CreateJob.Datos.DAO;

namespace WS_CreateJob.Datos
{
    public static class CJobDal
    {
        public static void SaveJob(string xml)
        {
            var daoJob = new CJobDao();
            daoJob.SaveJob(xml);       
        }
    }
}