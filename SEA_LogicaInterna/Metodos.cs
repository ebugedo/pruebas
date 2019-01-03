using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using CAP1_Utilidades.Fichero;
using CAP1_Email;
using CAP1_Automatization;
using CAP1_Ficheros;
using System.IO;
using System.Configuration;
using System.Data.SqlClient;
using CAP1_Xml;
using CAP1_Datos;

namespace SEA_LogicaInterna
{
    public class Metodos
    {
        public static void Tratamiento_XML_Factiva(CDocumentoXML Xml_in)
        {
            if (Xml_in != null)
            {
                string path_NAS = Xml_in.valorNodo("File_Physical_Name");
                string Id_Transaction = Xml_in.valorNodo("Id_Transaction");
                string id_alerta = Xml_in.valorNodo("Id_Alerta");
                string FUID = Xml_in.valorNodo("FUID");

                if (path_NAS != null && path_NAS.Trim()!="")
                {
                    string filename = Xml_in.valorNodo("FileName");

                    if (Update_AlertMatch(Id_Transaction, id_alerta, FUID, 2))
                    {
                        Insert_file_BBDD(id_alerta, FUID, path_NAS, filename);
                    }
                    else
                    {
                        delete_file(path_NAS);
                    }
                }
                else
                {
                    Update_AlertMatch(Id_Transaction, id_alerta, FUID, 3);
                }
            }
        }

        static void delete_file(string path_NAS)
        {
            try
            {
                if (path_NAS != null && path_NAS.Trim() != "")
                {
                    List<string[]> lstParametros = new List<string[]>();
                    lstParametros.Add(new string[] { "NameFileSystem", ConfigurationManager.AppSettings["FileSystem"].ToString() });
                    CDocumentoXML docEjecucion = new CDocumentoXML().ConstruirLlamadaProcedimientoAlmacenado("CORE_SP_SELECT_FILESYSTEM", lstParametros);
                    DataSet ds = new CEjecutar().Ejecutar(docEjecucion);
                    if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                    {
                        string user = ds.Tables[0].Rows[0]["UserFileSystem"].ToString();
                        string pass = ds.Tables[0].Rows[0]["PassWord"].ToString();
                        string domain = ds.Tables[0].Rows[0]["DomainFileSystem"].ToString();

                        FileHelper.BorrarFichero(domain, user, pass, path_NAS);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(DateTime.Now.ToString() + " ERROR delete_file->" + ex.Message);
            }
        }
        static bool Update_AlertMatch(string id_transaccion, string id_alerta, string fuid, int status)
        {
            try
            {
                List<string[]> lstParametros = new List<string[]>();
                lstParametros.Add(new string[] { "id_transaccion", id_transaccion });
                lstParametros.Add(new string[] { "id_alerta", id_alerta });
                lstParametros.Add(new string[] { "fuid", fuid });
                lstParametros.Add(new string[] { "status", status.ToString() });
                CDocumentoXML docEjecucion = new CDocumentoXML().ConstruirLlamadaProcedimientoAlmacenado("FACTIVA_SP_UPDATE_FUIDS", lstParametros);
                DataSet ds = new CEjecutar().Ejecutar(docEjecucion);
                if(ds!=null && ds.Tables.Count>0 && ds.Tables[0].Rows.Count>0)
                {
                    string result=ds.Tables[0].Rows[0][0].ToString();
                    if (result == "ok") return true;
                    else return false;
                }
                    else return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine(DateTime.Now.ToString() + " ERROR Update_AlertMatch->" + ex.Message);
                return false;
            }
        }

        static void Insert_file_BBDD(string id_alerta, string fuid, string path_remoto, string filename)
        {
            try
            {
                List<string[]> lstParametros = new List<string[]>();
                lstParametros.Add(new string[] { "id_alerta", id_alerta });
                lstParametros.Add(new string[] { "fuid", fuid });
                lstParametros.Add(new string[] { "path", path_remoto });
                lstParametros.Add(new string[] { "filename", filename });
                lstParametros.Add(new string[] { "Filesystem_Name", ConfigurationManager.AppSettings["FileSystem"].ToString() });
                CDocumentoXML docEjecucion = new CDocumentoXML().ConstruirLlamadaProcedimientoAlmacenado("FACTIVA_SP_INSERT_FILE", lstParametros);
                DataSet ds = new CEjecutar().Ejecutar(docEjecucion);
            }
            catch (Exception ex)
            {
                Console.WriteLine(DateTime.Now.ToString() + " ERROR Update_AlertMatch->" + ex.Message);
            }
        }
    }
}
