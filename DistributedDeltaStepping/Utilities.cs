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
        public static List<DirectEdge> CreateRandomGraph(int numberOfNodes, out List<Vertex> vertices)
        {
            CompleteGraph graph = new CompleteGraph(numberOfNodes, Directedness.Directed); // create a complete graph on 100 nodes
            var cost = new Dictionary<Node, double>(); // create a cost function on the nodes
            int i = 0;
            vertices = new List<Vertex>();
            foreach (Node node in graph.Nodes())
            {
                cost[node] = i++; // assign some integral costs to the nodes
                Vertex vertex = new Vertex();
                vertex.Id = node.Id;
                vertex.DistanceToRoot = 0;
                vertices.Add(vertex);
            }
            
            Func<Arc, double> arcCost =
                (arc => cost[graph.U(arc)] + cost[graph.V(arc)]); // a cost of an arc will be the sum of the costs of the two nodes

            List<DirectEdge> directEdges = new List<DirectEdge>();
            int count = 0;
            foreach (Arc arc in graph.Arcs())
            {
                DirectEdge directEdge = new DirectEdge();
                directEdge.U.Id = graph.U(arc).Id;
                directEdge.U.DistanceToRoot = 0;
                directEdge.V.Id = graph.V(arc).Id;
               
                directEdge.Cost = arcCost(arc);
                directEdges.Add(directEdge);
                Console.WriteLine("U:{0} ----> V:{1} with Cost:{2}", directEdge.U.Id, directEdge.V.Id, directEdge.Cost);
                count++;
            }

            return directEdges;
        }

        public static void InitVertices(ref Vertex[] localVertices, int rank)
        {
            int count = 0;
            foreach (var vertex in localVertices)
            {
                if (count == 0 && rank==0)
                {
                    // this is the root node , init with distance to self := 0
                    vertex.DistanceToRoot = 0;
                }
                else
                {
                    // this is not the root node , init with distance to self := oo
                    vertex.DistanceToRoot = 999999;
                }
                count++;
            }
        }

        public static void ProcessBucket(IEnumerable<Node> k)
        {
            foreach (Node node in k)
            {

            }
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="u">Source vertex</param>
        /// <param name="v">Source vertex</param>
        /// <param name="graphStructure"></param>
        /// <param name="buckets"></param>
        /// <param name="delta"></param>
        public static void Relax(Vertex u, Vertex v, List<DirectEdge> graphStructure, Bucket[] buckets, int delta)
        {
            //get the direct edge corresponding to source and destination
            var edge = graphStructure.Where(x => x.U.Id == u.Id && x.V.Id == v.Id).FirstOrDefault();
            
            var oldBucketIndex = v.DistanceToRoot / delta;
            if((u.DistanceToRoot + edge.Cost) < v.DistanceToRoot){
                edge.V.DistanceToRoot = edge.U.DistanceToRoot + edge.Cost;
            }
            var newBucketIndex = v.DistanceToRoot / delta;
            if (newBucketIndex < oldBucketIndex)
            {
                buckets[(int)oldBucketIndex].Vertices.Remove(v);
                buckets[(int)newBucketIndex].Vertices.Add(v);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="k"></param>
        /// <param name="graphStructure"></param>
        /// <param name="buckets"></param>
        /// <param name="delta"></param>
        public static void ProcessBucket(int k, List<DirectEdge> graphStructure, Bucket[] buckets, int delta)
        {
            var activeBucket = buckets[k];
            
            //if its the first iteration all vertices are treated as active vertices
            var activeVertices = activeBucket.Vertices;


            // foreach u E A and for each edge e = {u,v}
            foreach (Vertex u in activeVertices)
            {
                var edges = graphStructure.Where(x => x.U.Id == u.Id);
                foreach (DirectEdge edge in edges)
                {
                    Relax(u, edge.V, graphStructure, buckets, delta);
                }
            }
        }
    }
}
