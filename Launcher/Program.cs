using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Configuration;
using CAP1_Automatization;
using System.IO;
using CAP1_Encriptacion;
using System.Data;
using CAP1_Xml;
using System.ServiceModel;
using System.Xml.Linq;
using SEA.PSCM_ReceiveInfo;
using CAP1_Constantes;
using CAP1_Datos;
using Newtonsoft.Json.Linq;
using System.Xml;


namespace SEA
{
    class Program
    {
        static bool funcionamiento_en_bucle = true;
        static int  id_job = 0;
        static string error_msg = null;
        static string Execution_Mode = null;
        static string ID_Distributor_Jobs_Log;
        static public CDocumentoXML xml_in_Users = null;
        static int Id_Type_Auto = 0;
        static Distributor.QueueDistributorE oQueue=null;
        static string DefaultConnection = null;
        static string ID_Job_Launcher = null;

        [STAThread]
        static void Main(string[] args)
        {
            try
            {

                Console.WriteLine(DateTime.Now.ToString() + " Starting Main");

                ID_Job_Launcher = ConfigurationManager.AppSettings["ID_Job_Launcher"];

                string test_distributor = ConfigurationManager.AppSettings["test_distributor"];

                CDocumentoXML xml_in = null;
                CDocumentoXML xml_out = null;

                if (test_distributor == null || test_distributor != "true")
                {
                    while ((xml_in = Get_Job_Data()) != null)
                    {

                        xml_out = WatchDog.Start(xml_in, ref error_msg, Execution_Mode, xml_in_Users, id_job, DefaultConnection);

                        SendResponseWS(xml_in, xml_out);

                        Update_Jobs(xml_in, xml_out);
                    }
                }
                else
                {
                    //Random r = new Random();
                    //int rnd_type_automata = r.Next(3);
                    //if (rnd_type_automata == 0) rnd_type_automata = 7;
                    //else if (rnd_type_automata == 2) rnd_type_automata = 20;
                    //Id_Type_Auto = rnd_type_automata;

                    //ID_Job_Launcher = r.Next(7).ToString();
                    int count_test = int.Parse( ConfigurationManager.AppSettings["Count_test_distributor"]);

                    while (count_test>0 && (xml_in = Get_Job_Data()) != null)
                    {
                        Update_Jobs(xml_in, xml_out);

                        //ID_Job_Launcher = r.Next(7).ToString();

                        count_test--;
                    }
                }
            }
            catch (Exception ex)
            {
                Update_Users_Finish_Date(null, xml_in_Users);
                Console.WriteLine(DateTime.Now.ToString() + " error(Main): " + ex.Message);                
            }
        }

        public static void Update_Jobs(CDocumentoXML xml_in, CDocumentoXML xml_out)
        {
            try
            {
                if (xml_out != null)
                {
                    if (xml_out.existeNodo("ID_Job_Launcher"))
                    {
                        xml_out.cambiarValorNodo("ID_Job_Launcher", ConfigurationManager.AppSettings["ID_Job_Launcher"]);
                    }
                    else
                    {
                        xml_out.agregarElemento("ID_Job_Launcher", ConfigurationManager.AppSettings["ID_Job_Launcher"]);
                    }
                }
                Update_Job(xml_in, xml_out);
                Update_Job_Distributor_Log(xml_in, xml_out);
            }
            catch (Exception ex)
            {
                Console.WriteLine(DateTime.Now.ToString() + " error(Update_Jobs): " + ex.Message);
            }
            finally
            {
                try
                {
                    // Actualizamos Fecha de final de los usuarios utilizados
                    Update_Users_Finish_Date(xml_out, xml_in_Users);
                }
                catch(Exception ex2)
                {
                    Console.WriteLine(DateTime.Now.ToString() + " error(Update_Jobs).finally: " + ex2.Message);        
                }
                error_msg = null;
            }
        }

        public  static void Update_Job(CDocumentoXML xml_in, CDocumentoXML xml_out, string pId_Status = null, string pMsg = null)
        {
            try
            {
                int _error_code = 0;

                if (xml_out !=null)
                    if (!int.TryParse(xml_out.valorNodo("ERROR_CODE"), out _error_code)) _error_code = 0;

                List<string[]> lstParametros = new List<string[]>();

                lstParametros.Add(new string[] { "Id_Job", id_job.ToString() });
                string Id_Status = "3";

                if (_error_code >= 0)
                {
                    if (pId_Status != null)
                        Id_Status = pId_Status;

                    if (error_msg != null)
                        Id_Status = "4";
                    else if (pMsg != null)
                        error_msg = pMsg;
                    else
                        error_msg = "Completed corectly.";
                        
                }
                else
                {
                    Id_Status = "1";
                    error_msg = "Restarting job.";
                }

                lstParametros.Add(new string[] { "Id_Status", Id_Status });
                lstParametros.Add(new string[] { "XML_Data_Out", xml_out==null?"":xml_out.OuterXml });
                lstParametros.Add(new string[] { "Msg", error_msg });

                if (_error_code < 0)
                {
                    string newdate = xml_out.valorNodo("RESTART_NEWDATE");
                    if (newdate != null)
                    {
                        lstParametros.Add(new string[] { "NewDate", newdate });
                    }
                }

                string SP = ConfigurationManager.AppSettings["PROCEDURE_UPDATE_JOB"].ToString();
                string Conn = null;

                if (oQueue!= null)
                {
                    SP = oQueue.SP_End_Job;
                    Conn = oQueue.Conection_String;
                }

                Call_Procedure(SP, lstParametros, Conn);

            }
            catch (Exception ex)
            {
                Console.WriteLine(DateTime.Now.ToString() + " error(Update_Job): " + ex.Message);
                error_msg = DateTime.Now.ToString() + " error(Update_Job): " + ex.Message;
            }
        }

        private static void Update_Job_Distributor_Log(CDocumentoXML xml_in, CDocumentoXML xml_out)
        {

      
            if (oQueue == null || string.IsNullOrWhiteSpace(ID_Distributor_Jobs_Log))
                return;

            try
            {
                int _error_code = 0;
                if (!int.TryParse(xml_out.valorNodo("ERROR_CODE"), out _error_code)) _error_code = 0;

                string sConexionString = ConfigurationManager.ConnectionStrings["Conexion_Distributor"].ToString();

                var lstParametros = new List<string[]>();
                lstParametros.Add(new string[] { "ID_Distributor_Jobs_Log", ID_Distributor_Jobs_Log });
                lstParametros.Add(new string[] { "ID_Job", id_job.ToString() });

                string Id_Status = "3";
                if (_error_code >= 0)
                {
                    if (error_msg != null && error_msg != "Completed corectly.")
                    {
                        Id_Status = "4";
                    }
                }

                lstParametros.Add(new string[] { "ID_Job_Status", Id_Status.ToString() });
                lstParametros.Add(new string[] { "Desc_Job_Log", (error_msg == null ? "NULL" : error_msg) });
                lstParametros.Add(new string[] { "ID_Job_Launcher", ID_Job_Launcher.ToString()});

                Call_Procedure("CORE_D_U_DISTRIBUTOR_JOB_LOG", lstParametros, sConexionString);

                ID_Distributor_Jobs_Log = null;
            }
            catch (Exception ex)
            {
                Console.WriteLine(DateTime.Now.ToString() + " error(Add_Job_Distributor_Log): " + ex.Message);
            }
        }

        /// <summary>
        /// Llamada a la actualziación del Log de los usuarios utlizados en el Job
        /// </summary>
        /// <param name="xml_out">Lista con los errores producidos del Job</param>
        /// <param name="xml_in_Users">Datos de usuario utilizado y que se va a actualizar.</param>
        public static void Update_Users_Finish_Date(CDocumentoXML xml_out,CDocumentoXML xml_in_Users)
        {
            CDocumentoXML Usuarios = xml_in_Users.siguienteNodo("Usuarios");
            CDocumentoXML User=  Usuarios.siguienteNodo("Usuario");

            if (Usuarios != null)
            {
                while (User != null)
                {
                    Update_User_Finish_Date(xml_out, User);
                    User = Usuarios.siguienteNodo("Usuario");
                }
            }
        }

        /// <summary>
        /// Actualiza el log correspondiente al usuario en uso.
        /// Incrementa en caso de error, los errores consecutivos del usuario
        /// Bloquea el usuario si el codigo de error que nos devuleve el Job corresponde con el mismo
        /// </summary>
        /// <param name="xml_out">Lista con los errores producidos del Job</param>
        /// <param name="xml_in_Users">Datos de usuario utilizado y que se va a actualizar.</param>
        static void Update_User_Finish_Date(CDocumentoXML xml_out, CDocumentoXML xml_in_Users)
        {
            try
            {
                string sConexionString = ConfigurationManager.ConnectionStrings["Conexion_Distributor"].ToString();

                List<string[]> lstParametros = new List<string[]>();
                string Error_Codes = string.Empty;
                Error_Codes = xml_out.valorNodo("ERROR_CODE");

                lstParametros.Add(new string[] { "Error_Codes", Error_Codes });
                lstParametros.Add(new string[] { "ID_Auto_Log", xml_in_Users.valorNodo("ID_Auto_Log") });
                lstParametros.Add(new string[] { "ID_User_Auto", xml_in_Users.valorNodo("ID_User_Auto") });

                Call_Procedure("CORE_SP_UPDATE_FINISH_JOB_USER", lstParametros, sConexionString);

            }
            catch (Exception ex)
            {
                Console.WriteLine(DateTime.Now.ToString() + " error(Update_User_Finish_Date): " + ex.Message);
                error_msg = DateTime.Now.ToString() + " error(Update_User_Finish_Date): " + ex.Message;
            }
        }


        public static CDocumentoXML Get_Job_Data()
        {
            CDocumentoXML xml_in = null;
            xml_in_Users = null;
            try
            {
                Id_Type_Auto = -9999;

                DefaultConnection = null;

                if (funcionamiento_en_bucle)
                {
                    string config_bucle = ConfigurationManager.AppSettings["funcionamiento_en_bucle"];
                    funcionamiento_en_bucle = config_bucle == "true";

                    Execution_Mode = ConfigurationManager.AppSettings["Execution_Mode"];

                    if (Execution_Mode == "job")
                    {
                        xml_in = Get_Job_Data_Queue_DDBB();
                        if (xml_in == null)
                        {
                            xml_in = Get_Job_Data_DDBB();
                        }
                        if (xml_in != null)
                        {
                            Get_Job_User_Data_DDBB(xml_in);

                            if (xml_in_Users == null || string.IsNullOrWhiteSpace(xml_in_Users.InnerText))
                            {

                                // No tenemos usuarios, actualizamos el JOB a Status 1.
                                Console.WriteLine(DateTime.Now.ToString() + " Job sin usuarios. Establecemos Status Job a 1");

                                Update_Job(xml_in, null, "1", "Job without users.");
                                xml_in = null;
                            }
                        }
                    }
                    else
                    {
                        xml_in = new CDocumentoXML();
                        xml_in.RemoveChild(xml_in.FirstChild);
                        xml_in.agregarElemento("Id_Automata", ConfigurationManager.AppSettings["IdAutomata"]);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(DateTime.Now.ToString() + " error(Get_Job_Data): " + ex.Message);
                xml_in = null;
            }
            return xml_in;
        }


        public static void Get_Job_User_Data_DDBB(CDocumentoXML xml_in)
        {
            try
            {
                if (Id_Type_Auto != -9999)
                {
                    Console.WriteLine(DateTime.Now.ToString() + " Obteniendo Usuarios para el Job");
                    List<string[]> lstParametros = new List<string[]>();
                    string sConexionString = ConfigurationManager.ConnectionStrings["Conexion_Distributor"].ToString();

                    string ID_Queue = string.Empty;

                    if (oQueue!= null)
                        ID_Queue = oQueue.ID_Queue.ToString();

                    CEjecutar OExe = new CEjecutar(sConexionString, false);
                    lstParametros.Add(new string[] { "ID_Type_Auto", Id_Type_Auto.ToString() });
                    lstParametros.Add(new string[] { "ID_Queue", ID_Queue});
                    lstParametros.Add(new string[] { "ID_Job_Launcher", ID_Job_Launcher});
                    lstParametros.Add(new string[] { "ID_Job", id_job.ToString()});
                    var xml_ejec = new CDocumentoXML().ConstruirLlamadaProcedimientoAlmacenado("CORE_SP_GET_USER_AUTO", lstParametros);
                    DataSet dts = OExe.Ejecutar(xml_ejec);

                    if (dts != null && dts.Tables != null && dts.Tables.Count>0 && dts.Tables[0].Rows.Count > 0)
                    {
                        xml_in_Users = new CDocumentoXML();
                        xml_in_Users.RemoveChild(xml_in_Users.FirstChild);
                        // Añadimos nueva clave con los usuarios que tendra el Job
                        xml_in_Users.agregarNodoConRuta("ROOT", "Usuarios", "");

                        for(int i=0; i<dts.Tables[0].Rows.Count; i++)
                        {
                            CDocumentoXML XML_Usuario = new CDocumentoXML();
                            XML_Usuario.agregarNodoConRuta("ROOT", "Usuario", "");                     

                            string func = dts.Tables[0].Rows[i]["Functionality"].ToString();
                            string user = dts.Tables[0].Rows[i]["User"].ToString();
                            string pass = dts.Tables[0].Rows[i]["User_Access"].ToString();

                            string d1 = dts.Tables[0].Rows[i]["Extra_data1"].ToString();
                            string d1_des = dts.Tables[0].Rows[i]["Desc_Extra_data1"].ToString();
                            string d2 = dts.Tables[0].Rows[i]["Extra_data2"].ToString();
                            string d2_des = dts.Tables[0].Rows[i]["Desc_Extra_data2"].ToString();
                            string d3 = dts.Tables[0].Rows[i]["Extra_data3"].ToString();
                            string d3_des = dts.Tables[0].Rows[i]["Desc_Extra_data3"].ToString();

                            if (string.IsNullOrEmpty(func)) func = "Login";
                            else func = prepare_string_user(func);

                            if (string.IsNullOrEmpty(user)) user = "";
                            else user = user.Trim();

                            if (string.IsNullOrEmpty(pass)) pass = "";
                            else pass = pass.Trim();

                            //xml_in_Users.agregarElemento(func + "_User", user);
                            //xml_in_Users.agregarElemento(func + "_Pass", pass);

                            XML_Usuario.agregarNodoConRuta("ROOT/Usuario", func + "_User", user);
                            XML_Usuario.agregarNodoConRuta("ROOT/Usuario", func + "_Pass", pass);

                            if (!string.IsNullOrEmpty(d1_des))
                            {
                                if (string.IsNullOrEmpty(d1)) d1 = "";
                                else d1 = d1.Trim();

                                d1_des = prepare_string_user(d1_des);

                                //xml_in_Users.agregarElemento(func +"_"+ d1_des, d1);
                                XML_Usuario.agregarNodoConRuta("ROOT/Usuario", func + "_" + d1_des, d1);
                            }
                            if (!string.IsNullOrEmpty(d2_des))
                            {
                                if (string.IsNullOrEmpty(d2)) d2 = "";
                                else d2 = d2.Trim();

                                d2_des = prepare_string_user(d2_des);

                                //xml_in_Users.agregarElemento(func + "_" + d2_des, d2);
                                XML_Usuario.agregarNodoConRuta("ROOT/Usuario", func + "_" + d2_des, d2);
                            }
                            if (!string.IsNullOrEmpty(d3_des))
                            {
                                if (string.IsNullOrEmpty(d3)) d3 = "";
                                else d3 = d3.Trim();

                                d3_des = prepare_string_user(d3_des);

                                //xml_in_Users.agregarElemento(func + "_" + d3_des, d3);
                                XML_Usuario.agregarElemento(func + "_" + d3_des, d3);
                            }

                            string Error_Codes = string.Empty;
                            string Id_Logs_User = string.Empty;

                            if (dts.Tables[0].Rows[i]["ID_Auto_Log"] != DBNull.Value && string.IsNullOrWhiteSpace(dts.Tables[0].Rows[i]["ID_Auto_Log"].ToString()) == false)
                                Id_Logs_User = dts.Tables[0].Rows[i]["ID_Auto_Log"].ToString();

                            if (dts.Tables[0].Rows[i]["Error_Code_Login"] != DBNull.Value && string.IsNullOrWhiteSpace(dts.Tables[0].Rows[i]["Error_Code_Login"].ToString()) == false)
                                Error_Codes = dts.Tables[0].Rows[i]["Error_Code_Login"].ToString();

                            XML_Usuario.agregarNodoConRuta("ROOT/Usuario","Error_Code_Login", Error_Codes);
                            XML_Usuario.agregarNodoConRuta("ROOT/Usuario","ID_Auto_Log", Id_Logs_User);
                            XML_Usuario.agregarNodoConRuta ("ROOT/Usuario","ID_User_Auto", dts.Tables[0].Rows[i]["ID_User_Auto"].ToString());

                            xml_in_Users.agregarNodoXMLConRuta("ROOT/Usuarios", "Usuario", XML_Usuario.SelectSingleNode("ROOT/Usuario").InnerXml);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(DateTime.Now.ToString() + " Error(Get_Job_User_Data_DDBB): " + ex.Message);
                xml_in_Users = null;
            }
        }

        public static string prepare_string_user(string s)
        {
            string r = s.Trim().Replace("  ", " ");
            r = r.Trim().Replace("  ", " ");
            r = r.Trim().Replace("  ", " ");
            r = r.Trim().Replace("  ", " ");
            r = r.Trim().Replace("  ", " ");
            r = r.Trim().Replace(" ", "_");

            return r;
        }

        public static CDocumentoXML Get_Job_Data_DDBB()
        {
            CDocumentoXML xml_in = null;
            try
            {
                Console.WriteLine(DateTime.Now.ToString() + " Obteniendo Job de DDBB");
                List<string[]> lstParametros = new List<string[]>();

                string[] params_config = ConfigurationManager.AppSettings["PROCEDURE_GET_JOB_PARAMS"].Split(';');
                foreach (string param_config in params_config)
                {
                    string[] param = param_config.Split('=');
                    lstParametros.Add(new string[] { param[0], (string.IsNullOrEmpty(param[1])) ? "" : param[1] });
                    if (param[0] == "Id_Type_Auto")
                    {
                        Id_Type_Auto = int.Parse(param[1]);
                    }
                }

                DataTable dt = Call_Procedure(ConfigurationManager.AppSettings["PROCEDURE_GET_JOB"], lstParametros,null);

                if (dt != null && dt.Rows.Count > 0)
                {
                    id_job = int.Parse(dt.Rows[0]["ID_JOB"].ToString());
                    

                    xml_in = new CDocumentoXML();
                    xml_in.LoadXml(dt.Rows[0]["AUTO_XML_Data_In"].ToString());
                    Console.WriteLine(DateTime.Now.ToString() + " Ejecuta_Automata_job: ID_JOB=" + id_job.ToString() + " XML=" + dt.Rows[0]["AUTO_XML_Data_In"].ToString());
                    if (!xml_in.existeNodo("Id_Automata"))
                    {
                        if (dt.Rows[0]["Id_Automata"] != null)
                        {
                            xml_in.agregarElemento("Id_Automata", dt.Rows[0]["Id_Automata"].ToString());
                        }
                        else
                        {
                            xml_in.agregarElemento("Id_Automata", ConfigurationManager.AppSettings["IdAutomata"]);
                        }
                    }
                    if (dt.Rows[0]["Priority"] != null)
                    {
                        if (xml_in.existeNodo("Priority"))
                        {
                            xml_in.cambiarValorNodo("Priority", dt.Rows[0]["Priority"].ToString());
                        }
                        else
                        {
                            xml_in.agregarElemento("Priority", dt.Rows[0]["Priority"].ToString());
                        }
                    }

                    if (!xml_in.existeNodo("Execution_Type"))
                    {
                        xml_in.agregarElemento("Execution_Type", ConfigurationManager.AppSettings["Execution_Type"]);
                    }

                    if (dt.Rows[0]["Id_Type_Auto"] != null)
                    {
                        Id_Type_Auto = int.Parse(dt.Rows[0]["Id_Type_Auto"].ToString());
                    }
                    if (xml_in.existeNodo("ID_Job_Launcher"))
                    {
                        xml_in.cambiarValorNodo("ID_Job_Launcher", ConfigurationManager.AppSettings["ID_Job_Launcher"]);
                    }
                    else
                    {
                        xml_in.agregarElemento("ID_Job_Launcher", ConfigurationManager.AppSettings["ID_Job_Launcher"]);
                    }

                }
                else
                {
                    Console.WriteLine(DateTime.Now.ToString() + " Ningun Job en DDBB Encontrado.");
                }

                // Guardamos el Type_Auto para grabarlo en el Log
                //ID_Type_Auto = (from p in lstParametros select p[1]).FirstOrDefault();
            }
            catch (Exception ex)
            {
                Console.WriteLine(DateTime.Now.ToString() + " Error(Get_Job_Data_DDBB(id_job=" + id_job.ToString() + "): " + ex.Message);
                xml_in = null;
                if (error_msg == null) error_msg = "Error(Get_Job_Data_DDBB(id_job=" + id_job.ToString() + "): " + ex.Message;
                else error_msg = error_msg + " Error(Get_Job_Data_DDBB(id_job=" + id_job.ToString() + "): " + ex.Message;
            }
            return xml_in;
        }

        public static DataTable Call_Procedure(string procedure, List<string[]> lstParametros, string ConectionString)
        {
            try
            {
                CDocumentoXML docEjecucion = new CDocumentoXML().ConstruirLlamadaProcedimientoAlmacenado(procedure, lstParametros);
                DataSet ds = null;

                if (ConectionString == null) ds = new CEjecutar().Ejecutar(docEjecucion);
                else ds = new CEjecutar(ConectionString, false).Ejecutar(docEjecucion);

                if (ds != null && ds.Tables != null && ds.Tables.Count > 0) return ds.Tables[0];
                else return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine(DateTime.Now.ToString() + " error(Call_Procedure(" + procedure + "): " + ex.Message);
                return null;
            }
        }

        private static void SendResponseWS(CDocumentoXML xml_in, CDocumentoXML xml_out)
        {
            try
            {
                xml_in.resetearContador();
                var callbackXml = xml_in.SiguienteNodo("callback");

                if (callbackXml != null)
                {
                    var callbackCfg = ReceiveInfoAux.GetCallbackCfg(callbackXml);

                    if (string.IsNullOrEmpty(callbackCfg.Url)) { return; }

                    var endpointAddres = new EndpointAddress(callbackCfg.Url);

                    callbackXml.resetearContador();
                    var parameters = callbackXml.SiguienteNodo("parameters");

                    using (var responseService = new ListenerClient("BasicHttpBinding_IListener", endpointAddres))
                    {
                        Console.WriteLine("Llamando al WS:" + callbackCfg.Url);
                        string result = responseService.GetDataParam(xml_out.InnerXml, parameters.InnerXml);
                        CDocumentoXML response = new CDocumentoXML();
                        response.LoadXml(result);
                        if (response.valorNodo("response") != "ok")
                        {
                            Console.WriteLine("Error llamada WS:" + response.valorNodo("response"));
                        }
                        else
                        {
                            Console.WriteLine("Llamada WS correcta");
                        }
                    }
                }
            }
            catch(Exception ex) 
            {
                Console.WriteLine("Error SendResponseWS->:" + ex.Message);
                if (error_msg==null) error_msg = "Error SendResponseWS->:" + ex.Message;
                else error_msg = error_msg+" Error SendResponseWS->:" + ex.Message;
            }
        }

        public static CDocumentoXML Get_Job_Data_Queue_DDBB()
        {
            CDocumentoXML xml_in = null;
            try
            {
                
                oQueue = Distributor.DistributorDAO.Get_Queue_Asigned(ID_Job_Launcher);

                if (oQueue != null)
                {
                    // Asignación de nueva cola.
                    xml_in = Get_Job_Data_Distributor(oQueue);
                    // Asignamos la cadena de conexion por defecto la de la cola.
                    DefaultConnection = oQueue.Conection_String;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error Get_Job_Data_Queue_DDBB->:" + ex.Message);
                xml_in = null;
            }
            return xml_in;
        }

        public static CDocumentoXML Get_Job_Data_Distributor(Distributor.QueueDistributorE oQueu)
        {
            CDocumentoXML xml_in = null;
            try
            {

                Boolean bInclude_Type_Auto = false;

                List<string[]> lstParametros = new List<string[]>();                

                // Procedimiento a Ejecutar
                string SP_Procedure = oQueu.SP_Get_Job;

                if (oQueu.SP_Params != null)
                {
                    string SP_Procedure_Params = oQueu.SP_Params;

                    string[] params_config = SP_Procedure_Params.Split(';');
                    foreach (string param_config in params_config)
                    {
                        string[] param = param_config.Split('=');
                        lstParametros.Add(new string[] { param[0], (string.IsNullOrEmpty(param[1])) ? "" : param[1] });

                        if (param[0].Trim().ToUpper() == "ID_TYPE_AUTO")
                        {
                            bInclude_Type_Auto = true;
                            Id_Type_Auto = int.Parse(param[1]);
                        }
                    }
                }
                if (bInclude_Type_Auto == false)
                {
                    lstParametros.Insert(0, new string[] { "ID_Type_Auto", oQueu.ID_Type_Auto.ToString() });
                    Id_Type_Auto = oQueu.ID_Type_Auto;
                }
                
                // Guardamos el Type_Auto para grabarlo en el Log
                //ID_Type_Auto = (from p in lstParametros select p[1]).FirstOrDefault();
                // Guardamos el Identificador del Log del distribuidor
                ID_Distributor_Jobs_Log = oQueu.ID_Distributor_Job_Log.ToString();

                DataTable dt = Call_Procedure(SP_Procedure, lstParametros, oQueu.Conection_String);

                if (dt != null && dt.Rows.Count > 0)
                {
                    id_job = int.Parse(dt.Rows[0]["ID_JOB"].ToString());
                    Console.WriteLine(DateTime.Now.ToString() + " Ejecuta_Automata_job: ID_JOB=" + id_job.ToString());

                    xml_in = new CDocumentoXML();
                    xml_in.LoadXml(dt.Rows[0]["AUTO_XML_Data_In"].ToString());
                    if (string.IsNullOrEmpty(xml_in.valorNodo("Id_Automata")) && dt.Rows[0]["Id_Automata"] != null)
                    {
                        xml_in.agregarElemento("Id_Automata", dt.Rows[0]["Id_Automata"].ToString());
                    }
                    else
                    {
                        xml_in.agregarElemento("Id_Automata", ConfigurationManager.AppSettings["IdAutomata"]);
                    }
                    if (dt.Rows[0]["Priority"] != null)
                    {
                        if (xml_in.existeNodo("Priority"))
                        {
                            xml_in.cambiarValorNodo("Priority", dt.Rows[0]["Priority"].ToString());
                        }
                        else
                        {
                            xml_in.agregarElemento("Priority", dt.Rows[0]["Priority"].ToString());
                        }
                    }
                    if (xml_in.existeNodo("ID_Job_Launcher"))
                    {
                        xml_in.cambiarValorNodo("ID_Job_Launcher", ConfigurationManager.AppSettings["ID_Job_Launcher"]);
                    }
                    else
                    {
                        xml_in.agregarElemento("ID_Job_Launcher", ConfigurationManager.AppSettings["ID_Job_Launcher"]);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(DateTime.Now.ToString() + " Error(Get_Job_Data_DDBB(id_job=" + id_job.ToString() + "): " + ex.Message);
                xml_in = null;
                if (error_msg == null) error_msg = "Error(Get_Job_Data_DDBB(id_job=" + id_job.ToString() + "): " + ex.Message;
                else error_msg = error_msg + " Error(Get_Job_Data_DDBB(id_job=" + id_job.ToString() + "): " + ex.Message;
            }
            return xml_in;
        }



    }
}
