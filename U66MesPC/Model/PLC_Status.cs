using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace U66MesPC.Model
{
    [Table("PLC_Status")]
    public class PLC_Status
    {
        public int ID { get; set; }
        public string MachineID { get; set; }
        public int Status { get; set; }

    }
}
