﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace U66MesPC.Model
{
    [Table("CarrierIDProductUnload")]
    public class CarrierIDProductUnload
    {
        public int ID { get; set; }
        public string CarrierID { get; set; }
        public string SNData { get; set; }
    }
}
