using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Satsuma;
using DistributedDeltaStepping.Domain;
using MPI;

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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="u">Source vertex</param>
        /// <param name="v">Source vertex</param>
        /// <param name="graphStructure"></param>
        /// <param name="buckets"></param>
        /// <param name="delta"></param>
        public static void Relax(Vertex u, Vertex v, List<DirectEdge> graphStructure, Bucket[] buckets, int delta, out List<Vertex> changedVertices)
        {
            Console.WriteLine("DoRelax({0}, {1})", u, v);
            //get the direct edge corresponding to source and destination
            var edge = graphStructure.Where(x => x.U.Id == u.Id && x.V.Id == v.Id).FirstOrDefault();
            changedVertices = new List<Vertex>();
            var oldBucketIndex = v.DistanceToRoot / delta;
            if((u.DistanceToRoot + edge.Cost) < v.DistanceToRoot){
                v.DistanceToRoot = edge.U.DistanceToRoot + edge.Cost;
                changedVertices.Add(v);
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
        public static void ProcessBucket(int k, List<DirectEdge> graphStructure, Bucket[] buckets, int delta, Intracommunicator comm, Vertex[] localVertices)
        {
            var activeBucket = buckets[k];

            IList<Vertex> activeVertices = new List<Vertex>();
            activeBucket.Vertices.ForEach(x => activeVertices.Add(x));
            int count = 0;
            while (activeVertices.Count() > 0)
            {
                Console.WriteLine("loop count {0}", ++count);
                //if its the first iteration all vertices are treated as active vertices
                
                var changedVertices = new List<Vertex>();

                // foreach u E A and for each edge e = {u,v}
                foreach (Vertex u in activeVertices)
                {
                    Vertex v = null;
                    var edges = graphStructure.Where(x => x.U.Id == u.Id);
                    foreach (DirectEdge edge in edges)
                    {
                        //check if we have the processing destination vertex
                        if (localVertices.All(x => x.Id != edge.V.Id))
                        {
                            //we didnt find the processing vertex , ask other processors
                            var gatheredVertices = comm.Gather(localVertices, comm.Rank);
                            var gatheredVerticesList = gatheredVertices.SelectMany(i => i).ToList();
                            v = gatheredVerticesList.FirstOrDefault(x => x.Id == edge.V.Id);
                        }
                        else
                        {
                            v = localVertices.FirstOrDefault(x => x.Id == edge.V.Id);
                        }
                       
                        Relax(u, v, graphStructure, buckets, delta, out changedVertices);
                    }
                }
                Console.WriteLine("New active vertices : {0}", activeVertices.Count);
                List<Vertex> newActiveVertices = new List<Vertex>();
                //intersection of two sets, active vertices and changed vertices
                foreach (Vertex changedVertex in changedVertices)
                {
                    if (activeVertices.Any(x=> x.Id == changedVertex.Id))
                    {
                        newActiveVertices.Add(changedVertex);
                    }
                }
                activeVertices = newActiveVertices;
                Console.WriteLine("New active vertices : {0}", activeVertices.Count);
            }
        }
    }
}
