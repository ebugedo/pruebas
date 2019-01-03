using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Runtime.CompilerServices;
using System.ServiceModel;
using System.Text.RegularExpressions;
using System.Web.UI.WebControls;
using System.Xml;
using System.Xml.Linq;
using CAP1_Constantes;
using CAP1_Datos;
using CAP1_Nucleo;
using CAP1_Xml;
using System.Reflection;
using SEA_LogicaInterna;
using System.Web;

namespace WSReceiveInfo
{
    public class Listener : IListener
    {

        private static readonly string OkResponse =
            new XElement(CConstantesXML.ROOT, new XElement("response", "ok")).ToString(SaveOptions.DisableFormatting);

        private static readonly string KoResponse =
            new XElement(CConstantesXML.ROOT, new XElement("response", "ko")).ToString(SaveOptions.DisableFormatting);

        public string GetData(string xml)
        {
            throw new FaultException<string>("No implementado");
        }

        public string GetDataParam(string xml, string parametersXml)
        {
            try
            {
                var cdXml = new CDocumentoXML(parametersXml);
                var cdXml_in = new CDocumentoXML(xml);

                var operation = cdXml.valorNodo("Operation");
                var user = cdXml.valorNodo("Sec_User");
                var namespace_clase = cdXml.valorNodo("Class");
                var metodo_a_ejecutar = cdXml.valorNodo("Method");
                var dll = cdXml.valorNodo("DLL");

                if (operation != null)
                {
                    Exec_Method(dll, namespace_clase, metodo_a_ejecutar, cdXml_in, user);
                    return OkResponse;
                }

                return KoResponse;
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        public static void Exec_Method(string dll_filename, string namespace_clase, string metodo_a_ejecutar, object parameter, string user)
        {
            string app_path = null;
            try
            {
                
                app_path = AppDomain.CurrentDomain.BaseDirectory;

                if (app_path.Substring(app_path.Length - 1) != "\\") app_path += "\\bin\\";
                else app_path += "bin\\";

                Assembly Assembly_Application = Assembly_Application = Assembly.LoadFile(app_path + dll_filename);

                Type thisType = Assembly_Application.GetType(namespace_clase);

                System.Reflection.MethodInfo m = thisType.GetMethod(metodo_a_ejecutar);
                if (m != null)
                {
                    if (parameter is DataTable)
                    {
                        List<DataTable> lst_dt = new List<DataTable>();
                        lst_dt.Add(((DataTable)parameter));
                        m.Invoke(thisType, lst_dt.ToArray());
                    }
                    else if (parameter is string)
                    {
                        List<string> proc_parameters = new List<string>();
                        if (parameter != null) proc_parameters.Add((string)parameter);
                        m.Invoke(thisType, proc_parameters.ToArray());
                    }
                    else if (parameter is CDocumentoXML)
                    {

                        CDocumentoXML[] method_param = null;
                        if (parameter != null) method_param = new CDocumentoXML[] { (CDocumentoXML)parameter };
                        m.Invoke(thisType, method_param);
                    }
                }
                else
                {
                    Console.WriteLine("ERROR Exec_Method->Método de clase no encontrada: '" + metodo_a_ejecutar + "'.");
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine("ERROR Exec_Method->" + ex.Message + " Path:" + app_path);
                throw new Exception("ERROR Exec_Method->" + ex.Message + " Path:" + app_path);
            }

        }
       
    }
}
