using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MinimizerCommon.Commons
{
    public class LifeCheck
    {
        public LifeCheck(string name, bool status)
        {
            Name = name;
            Status = status;
            Datetime = DateTime.Now;
        }

        public string Name { get; set; }
        public bool Status { get; set; }
        public DateTime Datetime { get; set; }
    }
}
