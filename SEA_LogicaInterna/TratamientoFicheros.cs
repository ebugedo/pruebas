using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CAP1_Ficheros;
using System.IO;
using System.Data;
using System.Data.SqlClient;
using System.Configuration;

namespace Prueba
{
    public class TratamientoFicheros
    {

        //private static string BBDD_APSO = ConfigurationSettings.AppSettings["ConexionPSCMSQL"];
        private static string BBDD_APSO = "Data Source=180.185.44.220,50000;Initial Catalog=PSCM_APSO;Persist Security Info=True;User ID=PSCM_APSO;Password=PSCM_APSO; Max Pool Size=512;";

        private static string FOLDER = string.Empty;
        
        private static string DOMAIN = string.Empty;

        private static string USER = string.Empty;

        private static string PASSWORD = string.Empty;

        public void UnirFicheros(string [] ficheros)
        {
            int filaCabecera = 2; //se empieza a contar por 0

            string[] misRutasFicheros = new string[ficheros.Length];
            List<byte[]> contenidoFicheros = new List<byte[]>();
            string[] nombreFicheros = new string[ficheros.Length];
            List<Object[,]> hojasExcel = new List<Object[,]>();
            Tratamiento_Excel excel = new Tratamiento_Excel();
            int sumaLength = 0;
            int numFilasNuevoFichero = 0;
            int numColumnasNuevoFichero = 0;

            for (int i = 0; i < ficheros.Length; i++)
            {
                byte[] contenido = null;
                Object[,] hojaExcel = null;
                misRutasFicheros[i] = RecuperarRuta(ficheros[i]);
                GetFile(misRutasFicheros[i], out contenido, out nombreFicheros[i]);
                contenidoFicheros.Add(contenido);
                string tipoFichero = nombreFicheros[i].Substring(nombreFicheros[i].LastIndexOf("."), nombreFicheros[i].Length - nombreFicheros[i].LastIndexOf("."));
                if (tipoFichero == ".xls")
                {
                    hojaExcel = excel.ReadExcelStream(new MemoryStream(contenido));                    
                }
                else if (tipoFichero == ".xlsx" || tipoFichero == ".xlsm")
                {
                    hojaExcel = excel.ReadExcelXlsxStream(new MemoryStream(contenido), string.Empty);
                }
                else
                {
                    return;
                }
                hojasExcel.Add(hojaExcel);
                sumaLength += hojaExcel.GetLength(0);
            }

            numFilasNuevoFichero = sumaLength - ((filaCabecera + 1) * (ficheros.Length - 1));
            numColumnasNuevoFichero = hojasExcel[0].GetLength(1);
            Object[,] hojaNueva = new Object[numFilasNuevoFichero, numColumnasNuevoFichero];

            for (int col = 0; col < numColumnasNuevoFichero; col++)
            {
                hojaNueva[filaCabecera, col] = hojasExcel[0][filaCabecera, col];
            }
            sumaLength = 0;
            for (int i = 0; i < ficheros.Length; i++)
            {
                for (int row = filaCabecera + 1; row < hojasExcel[i].GetLength(0); row++)
                {
                    for (int col = 0; col < numColumnasNuevoFichero; col++)
                    {
                        hojaNueva[row + sumaLength, col] = hojasExcel[i][row, col];
                    }
                }
                sumaLength += hojasExcel[i].GetLength(0) - (filaCabecera + 1);
            }
            
        }

        //cabecera en fila 1
        //public void GenerarFicheroFusion(string idFicheroTipo1, string idFicheroTipo2)
        //{
        //    //Conseguir id


        //    byte[] contenidoFichero1 = null;
        //    byte[] contenidoFichero2 = null;

        //    string nombreFichero1 = string.Empty;
        //    string nombreFichero2 = string.Empty;

        //    string RutaZipFichero1 = @RecuperarRuta(idFicheroTipo1);
        //    string RutaZipFichero2 = @RecuperarRuta(idFicheroTipo2);

        //    //string RutaZipFichero1 = @"\\gbanasp01.scgbn.geoban.corp\GBNPMOP01\APSO\DES\Tipo1.zip";//Path.Combine(CSession.XmlFileSystem.valorNodo(CConstantesFile.SERVER_FILESYSTEM), Path.Combine(CSession.XmlFileSystem.valorNodo(CConstantesFile.FOLDER_FILESYSTEM), txtNombreFisico.Text));
        //    //string RutaZipFichero2 = @"\\gbanasp01.scgbn.geoban.corp\GBNPMOP01\APSO\DES\Tipo2.zip";

        //    GetFile(RutaZipFichero1, out contenidoFichero1, out nombreFichero1);
        //    GetFile(RutaZipFichero2, out contenidoFichero2, out nombreFichero2);

        //    string tipoFichero1 = nombreFichero1.Substring(nombreFichero1.LastIndexOf("."), nombreFichero1.Length - nombreFichero1.LastIndexOf("."));
        //    string tipoFichero2 = nombreFichero2.Substring(nombreFichero2.LastIndexOf("."), nombreFichero2.Length - nombreFichero2.LastIndexOf("."));

        //    Tratamiento_Excel excel = new Tratamiento_Excel();

        //    Object[,] hojaUnoExcel1 = null;
        //    Object[,] hojaUnoExcel2 = null;

        //    string columnaBusqueda = "ID";
        //    string columnaEstudios = "Estudios";

        //    int indiceFichero1Busqueda = -1;
        //    int indiceFichero2Busqueda = -1;
        //    int indiceFichero2Estudios = -1;

        //    if (tipoFichero1 == ".xls")
        //    {
        //        //hojaUnoExcelList = excel.ReadExcel(rutaFichero); 
        //        hojaUnoExcel1 = excel.ReadExcelStream(new MemoryStream(contenidoFichero1));
        //    }
        //    else if (tipoFichero1 == ".xlsx" || tipoFichero1 == ".xlsm")
        //    {
        //        hojaUnoExcel1 = excel.ReadExcelXlsxStream(new MemoryStream(contenidoFichero1), string.Empty);
        //    }
        //    else
        //    {
        //        return;
        //    }
        //    if (tipoFichero2 == ".xls")
        //    {
        //        //hojaUnoExcelList = excel.ReadExcel(rutaFichero); 
        //        hojaUnoExcel2 = excel.ReadExcelStream(new MemoryStream(contenidoFichero2));
        //    }
        //    else if (tipoFichero2 == ".xlsx" || tipoFichero2 == ".xlsm")
        //    {
        //        hojaUnoExcel2 = excel.ReadExcelXlsxStream(new MemoryStream(contenidoFichero2), string.Empty);
        //    }
        //    else
        //    {
        //        return;
        //    }


        //    for (int r = 0; r <= hojaUnoExcel1.GetLength(1); r++)
        //    {
        //        if (hojaUnoExcel1[0, r].ToString().ToLower() == columnaBusqueda.ToLower())
        //        {
        //            indiceFichero1Busqueda = r;
        //            break;
        //        }
        //    }

        //    bool encontre1 = false;
        //    bool encontre2 = false;

        //    for (int h = 0; h <= hojaUnoExcel2.GetLength(1); h++)
        //    {
        //        if (hojaUnoExcel2[0, h].ToString().ToLower() == columnaBusqueda.ToLower())
        //        {
        //            indiceFichero2Busqueda = h;
        //            encontre1 = true;
        //        }

        //        if (hojaUnoExcel2[0, h].ToString().ToLower() == columnaEstudios.ToLower())
        //        {
        //            indiceFichero2Estudios = h;
        //            encontre2 = true;
        //        }

        //        if (encontre1 && encontre2)
        //        {
        //            break;
        //        }

        //    }

        //    Object[,] hojaFusion = new Object[hojaUnoExcel1.GetLength(0), hojaUnoExcel1.GetLength(1)+1];

        //    for (int row = 0; row < hojaUnoExcel1.GetLength(0); row++)
        //    {
        //        for (int col = 0; col < hojaUnoExcel1.GetLength(1); col++)
        //        {
        //            //Estoy en la ultima
        //            if (col == hojaUnoExcel1.GetLength(1)-1)
        //            {
        //                hojaFusion[row, col] = hojaUnoExcel1[row, col];
        //                for (int i = 0; i < hojaUnoExcel2.GetLength(0); i++)
        //                {
        //                    if (hojaUnoExcel1[row, indiceFichero1Busqueda].ToString() == hojaUnoExcel2[i, indiceFichero2Busqueda].ToString())
        //                    {
        //                        hojaFusion[row, col + 1] = hojaUnoExcel2[i, indiceFichero2Estudios];
        //                    }
        //                }
        //            }
        //            //en cualquier otro caso
        //            else
        //            {
        //                hojaFusion[row, col] = hojaUnoExcel1[row, col];
        //            }
        //        }
        //    }



        //    //for (int row = 0; row < hojaUnoExcel2.GetLength(0); row++)
        //    //{
        //    //    hojaFusion[row, 38] = hojaUnoExcel2[row, 4];
        //    //}

        //    //
        //    //int columnas = hojaUnoExcelList.GetLength(1);  
        //    //                    //Calculo cual es el indice que se corresponde con el nombre de la columna introducida 
        //    //for (int r = 0; r <= columnas; r++)
        //    //{
        //    //    if (hojaUnoExcelList[0, r].ToString().ToLower() == columnA.ToLower())
        //    //    {
        //    //        indiceColumnaA = r;
        //    //        EncontreA = true;
        //    //    }
        //    //}

        //}


        //cabecera en fila 3
        public static DataTable GenerarFicheroFusion(string idFicheroTipo1, string idFicheroTipo2)
        {
            //Conseguir id

            int filaCabecera = 0; //se empieza a contar por 0

            byte[] contenidoFichero1 = null;
            byte[] contenidoFichero2 = null;

            string nombreFichero1 = string.Empty;
            string nombreFichero2 = string.Empty;

            //string RutaZipFichero1 = @RecuperarRuta(idFicheroTipo1);
            //string RutaZipFichero2 = @RecuperarRuta(idFicheroTipo2);

            string RutaZipFichero1 = @"\\gbanasp01.scgbn.geoban.corp\GBNPMOP01\APSO\DES\Tipo1.zip";//Path.Combine(CSession.XmlFileSystem.valorNodo(CConstantesFile.SERVER_FILESYSTEM), Path.Combine(CSession.XmlFileSystem.valorNodo(CConstantesFile.FOLDER_FILESYSTEM), txtNombreFisico.Text));
            string RutaZipFichero2 = @"\\gbanasp01.scgbn.geoban.corp\GBNPMOP01\APSO\DES\Tipo2.zip";

            GetFile(RutaZipFichero1, out contenidoFichero1, out nombreFichero1);
            GetFile(RutaZipFichero2, out contenidoFichero2, out nombreFichero2);

            string tipoFichero1 = nombreFichero1.Substring(nombreFichero1.LastIndexOf("."), nombreFichero1.Length - nombreFichero1.LastIndexOf("."));
            string tipoFichero2 = nombreFichero2.Substring(nombreFichero2.LastIndexOf("."), nombreFichero2.Length - nombreFichero2.LastIndexOf("."));

            Tratamiento_Excel excel = new Tratamiento_Excel();

            Object[,] hojaUnoExcel1 = null;
            Object[,] hojaUnoExcel2 = null;

            string columnaBusqueda = "ID";
            string columnaEstudios = "Estudios";

            int indiceFichero1Busqueda = -1;
            int indiceFichero2Busqueda = -1;
            int indiceFichero2Estudios = -1;

            if (tipoFichero1 == ".xls")
            {
                //hojaUnoExcelList = excel.ReadExcel(rutaFichero); 
                hojaUnoExcel1 = excel.ReadExcelStream(new MemoryStream(contenidoFichero1));
            }
            else if (tipoFichero1 == ".xlsx" || tipoFichero1 == ".xlsm")
            {
                hojaUnoExcel1 = excel.ReadExcelXlsxStream(new MemoryStream(contenidoFichero1), string.Empty);
            }
            else
            {
                return new DataTable();
            }
            if (tipoFichero2 == ".xls")
            {
                //hojaUnoExcelList = excel.ReadExcel(rutaFichero); 
                hojaUnoExcel2 = excel.ReadExcelStream(new MemoryStream(contenidoFichero2));
            }
            else if (tipoFichero2 == ".xlsx" || tipoFichero2 == ".xlsm")
            {
                hojaUnoExcel2 = excel.ReadExcelXlsxStream(new MemoryStream(contenidoFichero2), string.Empty);
            }
            else
            {
                return new DataTable();
            }


            for (int r = 0; r <= hojaUnoExcel1.GetLength(1); r++)
            {
                if (hojaUnoExcel1[filaCabecera, r].ToString().ToLower() == columnaBusqueda.ToLower())
                {
                    indiceFichero1Busqueda = r;
                    break;
                }
            }

            bool encontre1 = false;
            bool encontre2 = false;

            for (int h = 0; h <= hojaUnoExcel2.GetLength(1); h++)
            {
                if (hojaUnoExcel2[filaCabecera, h].ToString().ToLower() == columnaBusqueda.ToLower())
                {
                    indiceFichero2Busqueda = h;
                    encontre1 = true;
                }

                if (hojaUnoExcel2[filaCabecera, h].ToString().ToLower() == columnaEstudios.ToLower())
                {
                    indiceFichero2Estudios = h;
                    encontre2 = true;
                }

                if (encontre1 && encontre2)
                {
                    break;
                }

            }

            Object[,] hojaFusion = new Object[hojaUnoExcel1.GetLength(0) - filaCabecera, hojaUnoExcel1.GetLength(1) + 1];

            for (int row = filaCabecera; row < hojaUnoExcel1.GetLength(0); row++)
            {
                for (int col = 0; col < hojaUnoExcel1.GetLength(1); col++)
                {
                    //Estoy en la ultima
                    if (col == hojaUnoExcel1.GetLength(1) - 1)
                    {
                        hojaFusion[row, col] = hojaUnoExcel1[row, col];
                        for (int i = filaCabecera; i < hojaUnoExcel2.GetLength(0); i++)
                        {
                            if (hojaUnoExcel1[row, indiceFichero1Busqueda].ToString() == hojaUnoExcel2[i, indiceFichero2Busqueda].ToString())
                            {
                                hojaFusion[row - filaCabecera, col + 1] = hojaUnoExcel2[i, indiceFichero2Estudios];
                            }
                        }
                    }
                    //en cualquier otro caso
                    else
                    {
                        hojaFusion[row - filaCabecera, col] = hojaUnoExcel1[row, col];
                    }
                }
            }

            DataTable Fusion = ConvertirADataTable(hojaFusion);


            string NombreFusion = "Información_Consolidada_" + System.DateTime.Now.Date.ToString("yyyyMMdd") + "_" +
                                    System.DateTime.Now.TimeOfDay.ToString("hhmmss") + ".xls";

            return Fusion;

            //Transformar Object[,] hojaFusion en archivo Excel, crearlo en la NAS e insertar en PSCM_APSO.CORE_File
            //Devolver el Id_File del archivo que se acaba de crear
        }

        public void GenerarFicheroVersus(string idFicheroAnt, string idFicheroFusion)
        {
            byte[] contenidoFicheroAnt = null;
            byte[] contenidoFicheroFusion = null;

            string nombreFicheroAnt = string.Empty;
            string nombreFicheroFusion = string.Empty;

            //string RutaZipFicheroAnt = @RecuperarRuta(idFicheroAnt);
            //string RutaZipFicheroFusion = @RecuperarRuta(idFicheroFusion);

            string RutaZipFicheroAnt = @"\\gbanasp01.scgbn.geoban.corp\GBNPMOP01\APSO\DES\FusionAnterior.zip";//Path.Combine(CSession.XmlFileSystem.valorNodo(CConstantesFile.SERVER_FILESYSTEM), Path.Combine(CSession.XmlFileSystem.valorNodo(CConstantesFile.FOLDER_FILESYSTEM), txtNombreFisico.Text));
            string RutaZipFicheroFusion = @"\\gbanasp01.scgbn.geoban.corp\GBNPMOP01\APSO\DES\FusionActual.zip";

            GetFile(RutaZipFicheroAnt, out contenidoFicheroAnt, out nombreFicheroAnt);
            GetFile(RutaZipFicheroFusion, out contenidoFicheroFusion, out nombreFicheroFusion);

            string tipoFicheroAnt = nombreFicheroAnt.Substring(nombreFicheroAnt.LastIndexOf("."), nombreFicheroAnt.Length - nombreFicheroAnt.LastIndexOf("."));
            string tipoFicheroFusion = nombreFicheroFusion.Substring(nombreFicheroFusion.LastIndexOf("."), nombreFicheroFusion.Length - nombreFicheroFusion.LastIndexOf("."));

            Tratamiento_Excel excel = new Tratamiento_Excel();

            Object[,] hojaAnt = null;
            Object[,] hojaFusion = null;

            if (tipoFicheroAnt == ".xls")
            {
                //hojaUnoExcelList = excel.ReadExcel(rutaFichero); 
                hojaAnt = excel.ReadExcelStream(new MemoryStream(contenidoFicheroAnt));
            }
            else if (tipoFicheroAnt == ".xlsx" || tipoFicheroAnt == ".xlsm")
            {
                hojaAnt = excel.ReadExcelXlsxStream(new MemoryStream(contenidoFicheroAnt), string.Empty);
            }
            else
            {
                return;
            }
            if (tipoFicheroFusion == ".xls")
            {
                //hojaUnoExcelList = excel.ReadExcel(rutaFichero); 
                hojaFusion = excel.ReadExcelStream(new MemoryStream(contenidoFicheroFusion));
            }
            else if (tipoFicheroFusion == ".xlsx" || tipoFicheroFusion == ".xlsm")
            {
                hojaFusion = excel.ReadExcelXlsxStream(new MemoryStream(contenidoFicheroFusion), string.Empty);
            }
            else
            {
                return;
            }

            DataTable dtAnterior = ConvertirADataTable(hojaAnt);
            DataTable dtActual = ConvertirADataTable(hojaFusion);

            DataTable dtVersus = new DataTable();
            for (int col = 0; col < dtActual.Columns.Count; col++)
            //foreach (DataColumn dc in dtActual.Columns)
            {
                //dtVersus.Columns.Add(new DataColumn());
                dtVersus.Columns.Add(new DataColumn(dtActual.Rows[0][col].ToString()));
            }

            //DataRow newRow = dtVersus.NewRow();
            //for (int col = 0; col < dtAnterior.Columns.Count; col++)
            //{
            //    newRow[col] = dtAnterior.Rows[0][col];
            //}
            //dtVersus.Rows.Add(newRow);

            //bool encontrado = false;
            //foreach (DataRow dr in dtActual.Rows)
            //{
            //    for (int row = 0; encontrado && (row < dtActual.Rows.Count); row++)
            //    {
            //        if (dtAnterior.Rows[row][0] == dr[0])
            //        {
            //            encontrado = true;
            //            break;
            //        }
            //    }
            //    if (!encontrado)
            //    {
            //        DataRow newFila = dtVersus.NewRow();
            //        for (int col = 0; col < dtActual.Columns.Count; col++)
            //        {
            //            newFila[col] = dr[col];
            //        }
            //        dtVersus.Rows.Add(newFila);
            //    }
            //}


            ComprobarSiHayNuevosRegistros(dtAnterior, dtActual, ref dtVersus);
            foreach (DataRow drAntiguo in dtAnterior.Rows)
            {
                string query = "Column1 LIKE '%" + drAntiguo[0] + "'";
                //string query = "ID LIKE '%" + drAntiguo[0] + "'";
                DataRow[] filas = dtActual.Select(query);
                if (filas.Length == 0)
                {
                    DataRow newFila = dtVersus.NewRow();
                    for (int col = 0; col < dtAnterior.Columns.Count; col++)
                    {
                        newFila[col] = "#####red" + drAntiguo[col];
                    }
                    dtVersus.Rows.Add(newFila);
                }
                else if (filas.Length == 1)
                { 
                    DataRow newFila = dtVersus.NewRow();
                    bool hayCambios = false;
                    for (int col = 0; col < dtAnterior.Columns.Count; col++)
                    {
                        if (filas[0][col].ToString() == drAntiguo[col].ToString())
                        {
                            newFila[col] = filas[0][col];
                        }
                        else if (filas[0][col] == null || filas[0][col].ToString() == string.Empty)
                        {
                            newFila[col] = "#####red" + "*";
                            hayCambios = true;
                        }
                        else
                        {
                            newFila[col] = "#####red" + filas[0][col];
                            hayCambios = true;
                        }
                    }
                    if (hayCambios)
                    {
                        dtVersus.Rows.Add(newFila);
                    }
                }
            }

            

            string fechaEjecucionAntConExt = nombreFicheroAnt.Substring(24);
            string fechaEjecucionAnt = fechaEjecucionAntConExt.Substring(0, fechaEjecucionAntConExt.LastIndexOf("."));
            string fechaEjecucionActualConExt = nombreFicheroFusion.Substring(24);
            string fechaEjecucionActual = fechaEjecucionActualConExt.Substring(0, fechaEjecucionActualConExt.LastIndexOf("."));
            string NombreVersus = fechaEjecucionAnt + "VS" + fechaEjecucionActual + ".xls";
            byte[] FicheroGenerado = excel.GenerateXlsFileFromDataTable(dtVersus, "Hoja1", true);
            string path = @"C:\Archivos\" + NombreVersus;
            File.WriteAllBytes(path, FicheroGenerado);

        }

        private static void ComprobarSiHayNuevosRegistros(DataTable anterior, DataTable actual, ref DataTable versus)
        {
            bool encontrado = false;
            foreach (DataRow dr in actual.Rows)
            {
                encontrado = false;
                for (int row = 0; !encontrado && (row < actual.Rows.Count); row++)
                {
                    if (anterior.Rows[row][0].ToString() == dr[0].ToString())
                    {
                        encontrado = true;
                        break;
                    }
                }
                if (!encontrado)
                {
                    DataRow newFila = versus.NewRow();
                    for (int col = 0; col < actual.Columns.Count; col++)
                    {
                        newFila[col] = "#####green" + dr[col];
                    }
                    versus.Rows.Add(newFila);
                }
            }
        }

        public static DataTable ConvertirADataTable(Object[,] array2D)
        {
            DataTable dt = new DataTable();
            for (int col = 0; col < array2D.GetLength(1); col++)
            {
                dt.Columns.Add(new DataColumn()); 
            }
            for (int row = 0; row < array2D.GetLength(0); row++)
            {
                DataRow newRow = dt.NewRow();
                for (int col = 0; col < array2D.GetLength(1); col++)
                {
                    newRow[col] = array2D[row, col];
                }
                dt.Rows.Add(newRow);
            }
            return dt;
        }

        private static string RecuperarRuta(string fichero)
        {
            DataSet dsFicheros = new DataSet();
            //string whereQuery = string.Empty;
            //foreach (string f in ficheros)
            //{
            //    whereQuery += f.ToString() + ",";
            //}
            //whereQuery = "(" + whereQuery.Substring(0, whereQuery.Length - 1) + ")";
            string selectQuery = "SELECT Id_File, File_Physical_Name, Folder, Domain, [User], Password " +
                                 "FROM CORE_File " +
                                 //"INNER JOIN APSO_File ON APSO_File.Id_File = CORE_File.Id_File " +
                                 "INNER JOIN CORE_FileSystem ON CORE_FileSystem.Id_FileSystem = CORE_File.Id_FileSystem " +
                                 "WHERE Id_File = " + fichero;
                                 //"WHERE Id_File IN " + whereQuery;
                                 //"ORDER BY Order";
            SqlConnection connection = new SqlConnection(BBDD_APSO);
            SqlDataAdapter adapter = new SqlDataAdapter();
            connection.Open();
            SqlCommand command = new SqlCommand(selectQuery, connection);
            adapter.SelectCommand = command;
            adapter.Fill(dsFicheros);
            connection.Close();
            FOLDER = dsFicheros.Tables[0].Rows[0][2].ToString();
            DOMAIN = dsFicheros.Tables[0].Rows[0][3].ToString();
            USER = dsFicheros.Tables[0].Rows[0][4].ToString();
            PASSWORD = dsFicheros.Tables[0].Rows[0][5].ToString();
            string ruta = dsFicheros.Tables[0].Rows[0][1].ToString();
            return ruta;
            //List<string> lista = new List<string>();
            //foreach (DataRow dr in dsFicheros.Tables[0].Rows)
            //{
            //    lista.Add(dr[1].ToString());
            //}
            //return lista.ToArray();
        }

        public static void GetFile(string strPath, out byte[] contenido, out string name)
        {
            byte[] contenidoFichero;
            string nombreFichero;
            CAP1_Utilidades.Fichero.FileHelper.ObtenerFicheroRemoto(out contenidoFichero, out nombreFichero,
                "SCGBN",
                "SvcEsPro9356",
                "cnBNncwzrJXq5W8Wn0DD",
                strPath, true);
            //CAP1_Utilidades.Fichero.FileHelper.ObtenerFicheroRemoto(out contenidoFichero, out nombreFichero,
            //    DOMAIN,
            //    USER,
            //    PASSWORD,
            //    strPath, true);
            contenido = contenidoFichero;
            name = nombreFichero;
        }

        

    }
}
