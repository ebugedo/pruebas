using CAP1_Automatization;
using CAP1_Xml;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SEA
{
    public static class WatchDog
    {
        public static IRobot robot = null;
        public static CDocumentoXML xml_in_Users;
        public static int Id_Job = -1;
        public static string DefaultConnection;
        public static CDocumentoXML Start(CDocumentoXML xml_in, ref string Error_Msg, string Execution_Mode, CDocumentoXML _xml_in_Users, int _Id_Job, string _defaultConnection)
        {
            try
            {
                string Execution_Type = xml_in.valorNodo("Execution_Type");
                DefaultConnection = _defaultConnection;
                Id_Job = _Id_Job;
                if (Execution_Type == "web") robot = new Logic_Web();
                else if (Execution_Type == "desktop") robot = new Logic_Desktop();
                //else if (Execution_Type == "host") robot = new Logic_Host();
                xml_in_Users = _xml_in_Users;

                if (Execution_Mode == "direct")
                {
                    Start_Direct(xml_in);
                }
                else //if (Execution_Mode == "unatended")
                {
                    Run_Unatended_robot(xml_in);
                }
            }
            catch (Exception ex)
            {
                Error_Msg = "error(Start): " + ex.Message;
                Console.WriteLine("error(Start): " + ex.Message);
            }

            CDocumentoXML xml_out = null;
            try
            {
                xml_out = robot.Stop_Automata("clean");
            }
            catch (Exception ex2)
            {
                Console.WriteLine("error(Start->Stop_Automata): " + ex2.Message);
            }

            if (Error_Msg != null)
            {
                if (xml_out==null)
                {
                    xml_out = new CDocumentoXML();
                    xml_out.RemoveChild(xml_out.FirstChild);
                }
                xml_out.agregarElemento("ERROR", "WatchDog.Start ->" + Error_Msg);
                xml_out.agregarElemento("ERROR_CODE", "9999");
            }
            return xml_out;
        }

        private static void Run_Unatended_robot(CDocumentoXML xml_in)
        {
            string Error_Msg = null;
            Thread newThread = null;
            try
            {
                int reintento = 0;
                int number_retry_excecution = 0;
                if (!int.TryParse(ConfigurationManager.AppSettings["number_retry_excecution"], out number_retry_excecution))
                    number_retry_excecution = 0;


                newThread = new Thread(Start_Direct);
                newThread.SetApartmentState(ApartmentState.STA);
                newThread.Start(xml_in);

                Thread.Sleep(500);
                //while (!newThread.IsAlive) ;
                long actual_step = -1;
                int Miliseconds_wait_sentence_default = int.Parse(ConfigurationManager.AppSettings["Miliseconds_wait_sentence"].ToString());

                int m = 0;
                DateTime dt_temporizador = DateTime.Now;
                CDocumentoXML robot_status = robot.Get_Actual_Status();
                long actual_step_new = 0;

                while (robot_status!=null && robot_status.valorNodo("Completed") != "True")
                {
                    actual_step_new = long.Parse(robot_status.valorNodo("actual_step"));

                    if (actual_step == actual_step_new)
                    {
                        if ((DateTime.Now - dt_temporizador).TotalMilliseconds > m)
                        {
                            Console.WriteLine(DateTime.Now.ToString() + " Aborting process, time limit exceded.");
                            robot.Stop_Automata("clean");
                            newThread.Abort();
                            int count_aux = 0;
                            while (newThread.IsAlive && count_aux < 100)
                            {
                                Thread.Sleep(50);
                                count_aux++;
                            }
                            newThread = null;
                            reintento++;
                            if (reintento > number_retry_excecution)
                            {
                                Error_Msg = "error(Run_Unatended_robot): The server is not responding.";
                                break;
                            }
                            else
                            {
                                newThread = new Thread(Start_Direct);
                                newThread.SetApartmentState(ApartmentState.STA);
                                newThread.Start(xml_in);
                                count_aux = 0;
                                while (newThread.IsAlive && count_aux < 100)
                                {
                                    Thread.Sleep(50);
                                    count_aux++;
                                }
                            }
                        }
                    }
                    else
                    {
                        actual_step = actual_step_new;
                        dt_temporizador = DateTime.Now;
                        if (!int.TryParse(robot_status.valorNodo("DoEvents_Timer"), out m)) m = 0;
                        else m *= 2;
                        if (m < Miliseconds_wait_sentence_default) m = Miliseconds_wait_sentence_default;
                    }
                    Thread.Sleep(200);
                    robot_status = robot.Get_Actual_Status();
                }
            }
            catch (Exception ex2)
            {
                Error_Msg = "error(Run_Unatended_robot): " + ex2.Message;
                Console.WriteLine(Error_Msg);
            }
            finally
            {
                try
                {
                    if (newThread != null)
                    {
                        if (newThread.IsAlive) newThread.Abort();
                        newThread = null;
                    }
                }
                catch (Exception ex2)
                {
                    Console.WriteLine("error(Run_Unatended_robot.finally): " + ex2.Message);
                }
            }
            if (Error_Msg != null)
            {
                throw new Exception(Error_Msg);
            }
        }

        public static void Start_Direct(object xml_in)
        {
            try
            {
                string tmp_working_ubication = System.AppDomain.CurrentDomain.BaseDirectory;
                tmp_working_ubication += ConfigurationManager.AppSettings["TEMPFILES"] ?? string.Empty;

                if (!System.IO.Directory.Exists(tmp_working_ubication))
                {
                    System.IO.Directory.CreateDirectory(tmp_working_ubication);
                }
                try
                {
                    robot.Init((CDocumentoXML)xml_in, tmp_working_ubication, xml_in_Users, Id_Job, DefaultConnection);
                }
                finally
                {
                    string[] filePaths = Directory.GetFiles(tmp_working_ubication);
                    foreach (string filePath in filePaths)
                        File.Delete(filePath);
                }
            }
            catch (Exception ex2)
            {
                Console.WriteLine("error(Start_Direct): " + ex2.Message);
            }
        }
    }
}
