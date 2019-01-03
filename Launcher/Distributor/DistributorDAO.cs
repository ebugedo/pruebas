using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Text;
using CAP1_Datos;
using CAP1_Xml;

namespace SEA.Distributor
{
    class DistributorDAO
    {


        /// <summary>
        /// Obtiene la cola asignada
        /// </summary>
        /// <param name="ID_Job_Launcher">Indetificador del Job Launcher que se esta ejectando</param>
        /// <returns>QueueDistributorE: Clase con los datos necesarios</returns>
        public static QueueDistributorE Get_Queue_Asigned(string ID_Job_Launcher)
        {
            QueueDistributorE oQ = null;

            try
            {
                string sConexionString = ConfigurationManager.ConnectionStrings["Conexion_Distributor"].ToString();

                CEjecutar OExe = new CEjecutar(sConexionString, false);
                var lstParametros = new List<string[]>();
                lstParametros.Add(new string[] { "ID_Job_Launcher", ID_Job_Launcher });
                string DISTRIBUTOR_GET_POOL = ConfigurationManager.AppSettings["DISTRIBUTOR_GET_POOL"];
                var xml_ejec = new CDocumentoXML().ConstruirLlamadaProcedimientoAlmacenado(DISTRIBUTOR_GET_POOL, lstParametros);
                DataSet dts = OExe.Ejecutar(xml_ejec);

                if (dts != null && dts.Tables != null && dts.Tables.Count > 0 && dts.Tables[0] != null && dts.Tables[0].Rows.Count > 0)
                {
                    oQ = new QueueDistributorE
                    {
                        ID_Queue = dts.Tables[0].Rows[0].Field<int>("ID_Queue"),
                        Conection_String = dts.Tables[0].Rows[0].Field<string>("CONECTION_STRING"),
                        Jobs_Table = dts.Tables[0].Rows[0].Field<string>("JOBS_TABLE"),
                        ID_Type_Auto = dts.Tables[0].Rows[0].Field<int>("ID_Type_Auto"),
                        ID_Job = dts.Tables[0].Rows[0].Field<int?>("ID_Job"),
                        SP = dts.Tables[0].Rows[0].Field<string>("SP_JOBS_RECOLLECTOR"),
                        SP_Params = dts.Tables[0].Rows[0].Field<string>("SP_JOBS_RECOLLECTOR_PARAMS"),
                        ID_Distributor_Job_Log = dts.Tables[0].Rows[0].Field<int?>("ID_Distributor_Job_Log"),
                        SP_Get_Job = dts.Tables[0].Rows[0].Field<string>("SP_GET_JOB_QUEUE"),
                        SP_End_Job = dts.Tables[0].Rows[0].Field<string>("SP_END_JOB_QUEUE")
                    };
                }

                return oQ;
            }
            catch (Exception ex)
            {
                return null;
            }
        }
    }
}
