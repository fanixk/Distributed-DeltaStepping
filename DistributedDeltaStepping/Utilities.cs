using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DistributedDeltaStepping.Domain;

namespace DistributedDeltaStepping
{
    public class Utilities
    {
        public static void ProcessBucket(IEnumerable<Node> k)
        {
            foreach (Node node in k)
            {

            }
        }

        public static void Relax(Node sourceVertex, Node destinationVertex)
        {

        }
        
        public static List<Node> FillListWithRandomVertices(int listSize)
        {
            
            Random rnd = new Random();
            List<Node> verticesList = new List<Node>();
            for (int i = 0; i < listSize; i++)
            {
                Node vertice = new Node();
                vertice.X = rnd.Next(0, 100);
                vertice.Y = rnd.Next(0, 100);
                //set d(rt) <- 0
                if (i == 0)
                {
                    vertice.DistanceFromRoot = 0;
                }
                else // for all v != rt, set d(v) <- oo
                {
                    vertice.DistanceFromRoot = int.MaxValue;
                }

                verticesList.Add(vertice);
            }
            return verticesList;
        }
    }
}
