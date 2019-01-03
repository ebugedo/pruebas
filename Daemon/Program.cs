using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CAP1_Automatization;
using System.Configuration;
using CAP1_Xml;
using CAP1_Datos;
using CAP1_Encriptacion;
using System.Xml;
using CAP1_Constantes;
using System.Xml.Linq;
using System.IO;
using CAP1_Utilidades.Fichero;


namespace Daemon
{
    class Program
    {

        static void Main(string[] args)
        {
            Console.WriteLine(DateTime.Now.ToString() + " Inicio lanzador XML_IN");
            Llama_WS_CreaJobs();
            Guardar_FUID_Lista_AML();

            Crear_Casos();
        }

        static void Crear_Casos()
        {
            try
            {
                List<string[]> lstParametros = new List<string[]>();
                CDocumentoXML docEjecucion = new CDocumentoXML().ConstruirLlamadaProcedimientoAlmacenado("FACTIVA_SP_CREAR_CASOS_TRAS_AUTOMATAS", lstParametros);
                DataSet ds = new CEjecutar().Ejecutar(docEjecucion);

            }
            catch (Exception ex)
            {
                Console.WriteLine(DateTime.Now.ToString() + " ERROR Crear_Casos->" + ex.Message);
            }
        }

        static void Guardar_FUID_Lista_AML()
        {
            string directorio_temp_trabajo = System.AppDomain.CurrentDomain.BaseDirectory;
            directorio_temp_trabajo += ConfigurationManager.AppSettings["TEMPFILES"] ?? string.Empty;
            
            try
            {
                if (!Directory.Exists(directorio_temp_trabajo))
                {
                    Directory.CreateDirectory(directorio_temp_trabajo);
                }

                List<string[]> lstParametros = new List<string[]>();
                lstParametros.Add(new string[] { "NameFileSystem", ConfigurationManager.AppSettings["FileSystem"].ToString() });
                CDocumentoXML docEjecucion = new CDocumentoXML().ConstruirLlamadaProcedimientoAlmacenado("CORE_SP_SELECT_FILESYSTEM", lstParametros);
                DataSet ds = new CEjecutar().Ejecutar(docEjecucion);

                if (ds != null && ds.Tables != null && ds.Tables[0].Rows.Count > 0)
                {
                    var ruta = ds.Tables[0].Rows[0]["FolderFileSystem"].ToString();
                    var domain = ds.Tables[0].Rows[0]["DomainFileSystem"].ToString();
                    var user = ds.Tables[0].Rows[0]["UserFileSystem"].ToString();
                    var pass = ds.Tables[0].Rows[0]["PassWord"].ToString();

                    if (!ruta.EndsWith("\\")) ruta = ruta + "\\";
                    if (!directorio_temp_trabajo.EndsWith("\\")) directorio_temp_trabajo = directorio_temp_trabajo + "\\";

                    DataTable dt_match = null;

                    while ((dt_match = Get_Lista_AML_Data()) != null)
                    {
                        try
                        {
                            string filePath = directorio_temp_trabajo + dt_match.Rows[0]["Output_FileName"].ToString();
                            File.WriteAllText(filePath, dt_match.Rows[0]["DetallePago"].ToString());
                            string FicheroZipeado = FileHelper.ComprimirFichero(filePath, false);

                            /*ZIPEA, COPIA ZIP LOCAL Y LIMPIA*/
                            string file_name_zip = FicheroZipeado.Substring(FicheroZipeado.LastIndexOf('\\') + 1);
                            string path_remoto =Path.Combine(ruta, file_name_zip);

                            FileHelper.CopiarFichero(domain, user, pass, FicheroZipeado, path_remoto);
                            FileHelper.BorrarFichero(FicheroZipeado);
                            FileHelper.BorrarFichero(filePath);

                            Update_AlertMatch(dt_match, 2); //terminado correctamente
                            Insert_file_BBDD(dt_match, path_remoto); //grabacion de fichero

                        }
                        catch (Exception ex2)
                        {
                            Update_AlertMatch(dt_match, 3); //terminado incorrectamente
                            Console.WriteLine(DateTime.Now.ToString() + " ERROR Guardar_FUID_Lista_AML(Bucle)->" + ex2.Message);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(DateTime.Now.ToString() + " ERROR Guardar_FUID_Lista_AML->" + ex.Message);
            }
            finally
            {
                string[] filePaths = Directory.GetFiles(directorio_temp_trabajo);
                foreach (string filePath in filePaths)
                {
                    try
                    {
                        File.Delete(filePath);
                    }
                    catch(Exception ex2)
                    {
                        Console.WriteLine(DateTime.Now.ToString() + " ERROR Guardar_FUID_Lista_AML(delete filetemp)->" + ex2.Message);
                    }
                }
            }
        }

        static void Update_AlertMatch(DataTable dt_match, int status)
        {
            try
            {
                List<string[]> lstParametros = new List<string[]>();
                lstParametros.Add(new string[] { "id_transaccion", dt_match.Rows[0]["ID_transaccion"].ToString() });
                lstParametros.Add(new string[] { "id_alerta", dt_match.Rows[0]["ID_Alerta"].ToString() });
                lstParametros.Add(new string[] { "fuid", dt_match.Rows[0]["FUID"].ToString() });
                lstParametros.Add(new string[] { "status", status.ToString() });
                CDocumentoXML docEjecucion = new CDocumentoXML().ConstruirLlamadaProcedimientoAlmacenado("FACTIVA_SP_UPDATE_FUIDS", lstParametros);
                DataSet ds = new CEjecutar().Ejecutar(docEjecucion);
            }
            catch (Exception ex)
            {
                Console.WriteLine(DateTime.Now.ToString() + " ERROR Update_AlertMatch->" + ex.Message);
            }
        }

        static void Llama_WS_CreaJobs()
        {
            try
            {
                string z = null;

                while ((z = CreateXML()) != null)
                {
                    try
                    {
                        WS_CreateJob.Service_CreateJobClient client = new WS_CreateJob.Service_CreateJobClient();
                        Console.WriteLine("XML_Parametros creado OK");
                        System.Threading.Thread.Sleep(500);
                        client.SetDataJob(z);
                        client.Close();
                    }
                    catch (Exception ex2)
                    {
                        Console.WriteLine(DateTime.Now.ToString() + " ERROR Llama_WS_CreaJobs(Bucle)->" + ex2.Message);
                    }
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine(DateTime.Now.ToString() + " ERROR Llama_WS_CreaJobs->" + ex.Message);
            }
        }

        static string CreateXML()
        {
            DataSet ds = BuscaFUIDs();

            if (ds != null && ds.Tables[0].Rows.Count > 0)
            {
                DataSet ds_ws = Busca_WS_Data();
                if (ds_ws != null && ds_ws.Tables[0].Rows.Count > 0)
                {
                    var xml = new XElement(CConstantesXML.ROOT,
                        new XElement("USER", EncriptarDesencriptar.Encriptar(ds_ws.Tables[0].Rows[0]["USER_FACTIVA"].ToString())),
                        new XElement("PASSWORD", EncriptarDesencriptar.Encriptar(ds_ws.Tables[0].Rows[0]["PASS_FACTIVA"].ToString())),
                        new XElement("TIPO_AUTOMATA", ds_ws.Tables[0].Rows[0]["TIPO_AUTOMATA"].ToString()),
                        new XElement("FUID", ds.Tables[0].Rows[0]["FUID"].ToString()),
                        new XElement("Id_Alerta", ds.Tables[0].Rows[0]["ID_Alerta"].ToString()),
                        new XElement("Id_Transaction", ds.Tables[0].Rows[0]["ID_transaccion"].ToString()),
                        new XElement("Filesystem_User", EncriptarDesencriptar.Encriptar(ds_ws.Tables[0].Rows[0]["Filesystem_User"].ToString())),
                        new XElement("Filesystem_Pass", EncriptarDesencriptar.Encriptar(ds_ws.Tables[0].Rows[0]["Filesystem_Pass"].ToString())),
                        new XElement("Filesystem_Domain", ds_ws.Tables[0].Rows[0]["Filesystem_Domain"].ToString()),
                        new XElement("Filesystem_Path", ds_ws.Tables[0].Rows[0]["Filesystem_Path"].ToString()),
                        new XElement("Output_FileName", ds.Tables[0].Rows[0]["Output_FileName"].ToString()),

                        new XElement("callback",
                            new XElement("url", new XCData(ds_ws.Tables[0].Rows[0]["WebService"].ToString())),
                            new XElement("parameters",
                                new XElement("Operation", ds_ws.Tables[0].Rows[0]["Operation"].ToString()),
                                new XElement("Sec_User", ds_ws.Tables[0].Rows[0]["Usuario_WS"].ToString()),
                                new XElement("DLL", ds_ws.Tables[0].Rows[0]["DLL"].ToString()),
                                new XElement("Class", ds_ws.Tables[0].Rows[0]["Class"].ToString()),
                                new XElement("Method", ds_ws.Tables[0].Rows[0]["Method"].ToString()))));

                    CDocumentoXML xml_doc = new CDocumentoXML(xml.ToString());
                    return xml_doc.InnerXml;
                }
                else
                {
                    return null;
                }
            }
            else
            {
                return null;
            }
        }

        static DataSet BuscaFUIDs()
        {
            List<string[]> lstParametros = new List<string[]>();
            CDocumentoXML docEjecucion = new CDocumentoXML().ConstruirLlamadaProcedimientoAlmacenado("FACTIVA_SP_SELECT_FUIDS", lstParametros);
            DataSet ds = new CEjecutar().Ejecutar(docEjecucion);

            if (ds != null && ds.Tables[0].Rows.Count > 0)
                return ds;
            else
                return null;
        }

        static DataSet Busca_WS_Data()
        {

            List<string[]> lstParametros = new List<string[]>();
            lstParametros.Add(new string[] { "NameFileSystem", ConfigurationManager.AppSettings["FileSystem"].ToString() });
            CDocumentoXML docEjecucion = new CDocumentoXML().ConstruirLlamadaProcedimientoAlmacenado("FACTIVA_SP_DATA_TO_CALL_WS", lstParametros);
            DataSet ds = new CEjecutar().Ejecutar(docEjecucion);

            if (ds != null && ds.Tables[0].Rows.Count > 0)
                return ds;
            else
                return null;
        }


        static void Insert_file_BBDD(DataTable dt_match, string path_remoto)
        {

            try
            {
                List<string[]> lstParametros = new List<string[]>();
                lstParametros.Add(new string[] { "id_alerta", dt_match.Rows[0]["ID_Alerta"].ToString() });
                lstParametros.Add(new string[] { "fuid", dt_match.Rows[0]["FUID"].ToString() });
                lstParametros.Add(new string[] { "path", path_remoto });
                lstParametros.Add(new string[] { "filename", dt_match.Rows[0]["Output_FileName"].ToString() });
                lstParametros.Add(new string[] { "Filesystem_Name", ConfigurationManager.AppSettings["FileSystem"].ToString() });
                CDocumentoXML docEjecucion = new CDocumentoXML().ConstruirLlamadaProcedimientoAlmacenado("FACTIVA_SP_INSERT_FILE", lstParametros);
                DataSet ds = new CEjecutar().Ejecutar(docEjecucion);
            }
            catch (Exception ex)
            {
                Console.WriteLine(DateTime.Now.ToString() + " ERROR Update_AlertMatch->" + ex.Message);
            }
        }

        static DataTable Get_Lista_AML_Data()
        {
            List<string[]> lstParametros = new List<string[]>();
            CDocumentoXML docEjecucion = new CDocumentoXML().ConstruirLlamadaProcedimientoAlmacenado("FACTIVA_SP_SELECT_FUIDS_LISTA_AML", lstParametros);
            DataSet ds = new CEjecutar().Ejecutar(docEjecucion);

            if (ds != null && ds.Tables[0].Rows.Count > 0)
                return ds.Tables[0];
            else
                return null;
        }

    }
}
