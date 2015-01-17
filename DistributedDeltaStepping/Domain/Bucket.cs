using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DistributedDeltaStepping.Domain
{
    public class Bucket
    {
        public Bucket()
        {
            Vertices = new List<Vertex>();
        }

        public List<Vertex> Vertices
        {
            get;
            set;
        }
    }
}
