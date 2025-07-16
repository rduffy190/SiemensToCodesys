using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SiemensSCLToPlcOpen
{
    public struct Variable
    {
        public string Name;
        public string Type;
        public string Value;
        public bool hasStartup; 
    }
}
