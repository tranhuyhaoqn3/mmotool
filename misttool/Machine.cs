using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace misttool
{
    public class Machine
    {
        public string IpAdrress { get; set; }
        public int Port { get; set; }
        public string Image { get; set; }
        public string Status { get; set; }
        public DateTime DateUpdate { get; set; }
        public string SockConnect { get; set; }
        public string Run { get; set; }
        public string Time { get; set; }
    }
}
