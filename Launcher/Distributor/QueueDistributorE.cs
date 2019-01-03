using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SEA.Distributor
{
    public class QueueDistributorE
    {
          public int ID_Queue { get; set; }
          public string Conection_String { get; set; }
          public string  Jobs_Table{ get; set; }
          public string SP { get; set; }
          public string SP_Params { get; set; }
          public int ID_Type_Auto { get; set; }
          public int? ID_Job { get; set; }
          public int? ID_Distributor_Job_Log { get; set; }
          public string SP_Get_Job { get; set; }
          public string SP_End_Job { get; set; }

          public void Main()
          {
          }
    }
}
