using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace U66MesPC.Model
{
    [Table("CarrierIDBindingNoNumber50")]
    public class CarrierIDBindingNoNumber50
    {
        public int ID { get; set; }
        public string CarrierID { get; set; }
        public string NoNumber { get; set; }
    }
}
