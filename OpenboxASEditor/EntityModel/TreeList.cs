using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SQLite;

namespace OpenboxASEditor.EntityModel
{
    class TreeList
    {
        [PrimaryKey]
        public int id { get; set; }
        public int svc_idx { get; set; }
        public int svc_type { get; set; }
        public int svc_tp_index { get; set; }
        public byte[] svc_name { get; set; }
        public int tp_sat_index { get; set; }
        public int tp_frequency { get; set; }
        public int tp_symbol_rate { get; set; }
        public String tp_polar_qam { get; set; }
        public String tp_name { get; set; }
        public int sat_id_number { get; set; }
        public String sat_name { get; set; }
        public Decimal sat_angle { get; set; }
    }
}
