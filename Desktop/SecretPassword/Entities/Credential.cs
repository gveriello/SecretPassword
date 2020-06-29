using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entities
{
    public class Credential
    {
        public int ID { get; set; }
        public string Title { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public bool ShowPassword { get; set; }
        public string Url { get; set; }
        public string Notes { get; set; }
        public DateTime? Expires { get; set; }
        public int? GroupID { get; set; }
        public int UserID { get; set; }
    }
}
