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
    // generate a new class named server status with 2 properties server name and status
    /// <summary>
    /// Represents the status of a server.
    /// </summary>
    public class ServerStatus
    {
        // add a constructor with 2 parameters
        public ServerStatus(string serverName, bool status)
        {
            // set the server name and status
            ServerName = serverName;
            Status = status;
        }
        public string ServerName { get; set; }
        public bool Status { get; set; }
    }

}
