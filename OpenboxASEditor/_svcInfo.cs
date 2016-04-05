using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SQLite;

namespace OpenboxASEditor
{
    public class _svcInfo
    {
        [PrimaryKey, AutoIncrement]
        public int id { get; set; }
        
        public byte [] svc_name { get; set; }

        [MaxLength(100), Ignore]
        public String svc_name2 { get; set; }
    }
}
