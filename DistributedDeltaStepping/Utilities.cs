using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Satsuma;
using DistributedDeltaStepping.Domain;

namespace DistributedDeltaStepping
{
    public class Utilities
    {

        /// <summary>
        /// Create a random graph using random nodes, using Satsuma library for c#
        /// http://satsumagraph.sourceforge.net/doc/html/p_tutorial.html
        /// </summary>
        /// <param name="numberOfNodes">Number of nodes to create</param>
        /// <returns>returns a complete graph with edges and cost for each edge</returns>
        public static List<DirectEdge> CreateRandomGraph(int numberOfNodes)
        {
            CompleteGraph graph = new CompleteGraph(numberOfNodes, Directedness.Directed); // create a complete graph on 100 nodes
            var cost = new Dictionary<Node, double>(); // create a cost function on the nodes
            int i = 0;
            foreach (Node node in graph.Nodes()) cost[node] = i++; // assign some integral costs to the nodes
            Func<Arc, double> arcCost =
                (arc => cost[graph.U(arc)] + cost[graph.V(arc)]); // a cost of an arc will be the sum of the costs of the two nodes

            List<DirectEdge> directEdges = new List<DirectEdge>();
            int count = 0;
            foreach (Arc arc in graph.Arcs())
            {
                DirectEdge directEdge = new DirectEdge();
                directEdge.U.Id = graph.U(arc).Id;
                if (count == 0)
                {
                    // this is the root node , init with distance to self := 0
                    directEdge.U.DistanceToRoot = 0;
                }
                else
                {
                    // this is not the root node , init with distance to self := oo
                    directEdge.U.DistanceToRoot = numberOfNodes;
                }
               
                directEdge.V.Id = graph.V(arc).Id;
                directEdge.V.DistanceToRoot = 0;
                directEdge.Cost = arcCost(arc);
                directEdges.Add(directEdge);
                Console.WriteLine("U:{0} ----> V:{1} with Cost:{2}", directEdge.U.Id, directEdge.V.Id, directEdge.Cost);
                count++;
            }

            return directEdges;
        }

        public static void ProcessBucket(IEnumerable<Node> k)
        {
            foreach (Node node in k)
            {

            }
        }

        public static void Relax(DirectEdge edge, Bucket[] buckets, int delta)
        {
            var oldBucketIndex = edge.V.DistanceToRoot / delta;
            if((edge.U.DistanceToRoot + edge.Cost) < edge.V.DistanceToRoot){
                edge.V.DistanceToRoot = edge.U.DistanceToRoot + edge.Cost;
            }
            var newBucketIndex = edge.V.DistanceToRoot / delta;
            if (newBucketIndex < oldBucketIndex)
            {
                buckets[(int)oldBucketIndex].DirectEdges.Remove(edge);
                buckets[(int)newBucketIndex].DirectEdges.Add(edge);
            }
            
        }
        
        
    }
}
