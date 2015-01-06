using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DistributedDeltaStepping.Domain
{
    [Serializable]
    public class Node
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int DistanceFromRoot { get; set; }
    }
}
