using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entities
{
    public class Group
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public string Notes { get; set; }
        public int? UserID { get; set; }
    }
}
