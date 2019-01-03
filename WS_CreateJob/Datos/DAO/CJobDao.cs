using System;
using System.Collections.Generic;
using System.Globalization;
using CAP1_Datos;
using CAP1_Xml;
using System.Configuration;
using System.Data;

namespace WS_CreateJob.Datos.DAO
{
    public class CJobDao
    {
        public void SaveJob(string xml)
        {
            CDocumentoXML doc = new CDocumentoXML(xml);

            var parametros = new List<string[]>
            {
                new[] {"Tipo_Automata", doc.valorNodo("TIPO_AUTOMATA")},
                new[] {"Usuario",  ConfigurationManager.AppSettings["Usuario_APP"]},
                new[] {"XML_Parametros", xml},
                new[] {"Id_Aplicacion", ConfigurationManager.AppSettings["Application"]},
                new[] {"Priority", Get_Priority_Job(doc.valorNodo(xml)).ToString()}
            };
            var xml_ejec = new CDocumentoXML().ConstruirLlamadaProcedimientoAlmacenado("CORE_SP_CREATE_NEW_JOB", parametros);
            new CEjecutar().Ejecutar(xml_ejec);
        }


        /// <summary>
        /// Obtiene la prioridad asiganada al tipo de automata y con las reglas de prioridad
        /// </summary>
        /// <param name="xml">XML con los datos de entrada. 
        /// La prioridad vendra inlcuida en un nodo del XML. 
        /// Ejemplo:
        ///     <RAIZ>
        ///         <PRIORITY>
        ///             <Moneda>EUR</Moneda>
        ///             <Numero_Contrados>132</Numero_Contrados>
        ///         </PRIORITY>
        ///         <NODOS_DEL_XML>
        ///             <NODO1>1</NODO1>
        ///             <NODO2>2</NODO2>
        ///             .............
        ///         </NODOS_DEL_XML>
        ///     </RAIZ>
        /// </param>
        /// <returns>Devuelve en la priordad asignada.
        /// </returns>
        private int Get_Priority_Job(string xml)
        {

            int res = 0;

            try
            {
                string sConexionString = ConfigurationManager.ConnectionStrings["Conexion_Distributor"].ToString();

                CEjecutar OExe = new CEjecutar(sConexionString, false);
                CDocumentoXML doc = new CDocumentoXML(xml);
                var lstParametros = new List<string[]>();
                lstParametros.Add(new string[] { "Tipo_Automata", doc.valorNodo("TIPO_AUTOMATA") });
                lstParametros.Add(new string[] { "XML_Parametros", xml });
                var xml_ejec = new CDocumentoXML().ConstruirLlamadaProcedimientoAlmacenado("CORE_D_SP_DISTRIBUTOR_GET_JOB_PRIORITY", lstParametros);
                DataSet dts = OExe.Ejecutar(xml_ejec);

                if (dts != null && dts.Tables != null && dts.Tables.Count > 0 && dts.Tables[0] != null && dts.Tables[0].Rows.Count > 0)
                {
                    res = dts.Tables[0].Rows[0].Field<int>("Priority");
                }

                return res;
            }
            catch (Exception ex)
            {
                return res;
            }
        }
    }
}