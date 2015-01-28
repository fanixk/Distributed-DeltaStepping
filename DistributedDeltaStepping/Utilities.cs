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

                directEdge.Cost = (int) arcCost(arc);
                directEdges.Add(directEdge);
                Console.WriteLine("U:{0} ----> V:{1} with Cost:{2}", directEdge.U.Id, directEdge.V.Id, directEdge.Cost);
                count++;
            }

            return directEdges;
        }

        public static void InitVertices(ref Vertex[] localVertices, int rank, int k)
        {
            int count = 0;
            foreach (var vertex in localVertices)
            {
                if (count == 0 && rank == 0)
                {
                    // this is the root node , init with distance to self := 0
                    vertex.DistanceToRoot = 0;
                }
                else
                {
                    // this is not the root node , init with distance to self := oo
                    vertex.DistanceToRoot = k;
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
        public static void Relax(ref Vertex u, ref Vertex v, List<DirectEdge> graphStructure, ref Bucket[] buckets, int delta, ref List<Vertex> changedVertices)
        {
            //Console.WriteLine("DoRelax({0}, {1})", u.Id, v.Id);
            //get the direct edge corresponding to source and destination
            var uId = u.Id;
            var vId = v.Id; 
            var edge = graphStructure.Where(x => x.U.Id == uId && x.V.Id == vId).FirstOrDefault();

            int oldBucketIndex = v.DistanceToRoot / delta;
            //Console.WriteLine("d(v) = {0}", v.DistanceToRoot);
            if ((u.DistanceToRoot + edge.Cost) < v.DistanceToRoot)
            {
                v.DistanceToRoot = u.DistanceToRoot + edge.Cost;
            }
            int newBucketIndex = v.DistanceToRoot / delta;
            //Console.WriteLine("old {0} new {1}", oldBucketIndex, newBucketIndex);
            if (newBucketIndex < oldBucketIndex)
            {
                buckets[oldBucketIndex-1].Vertices.Remove(v);
                if (newBucketIndex > 0)
                {
                    newBucketIndex = newBucketIndex - 1;
                }

                buckets[newBucketIndex].Vertices.Add(v);
            }
            else if (newBucketIndex == oldBucketIndex)
            {
                changedVertices.Clear();
                changedVertices.Add(v);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="k"></param>
        /// <param name="graphStructure"></param>
        /// <param name="buckets"></param>
        /// <param name="delta"></param>
        public static void ProcessBucket(ref Bucket bucketToProcess, List<DirectEdge> graphStructure, ref Bucket[] buckets, int delta, Intracommunicator comm, Vertex[] localVertices, int totalVertices)
        {
            var activeBucket = bucketToProcess;

            List<Vertex> activeVertices = new List<Vertex>();
            activeVertices.AddRange(activeBucket.Vertices);

            var allVertices = comm.AllgatherFlattened(localVertices, localVertices.Count());
            Console.WriteLine("Active Vertices : {0}", String.Join(",", activeVertices.Select(x=>x.Id)));
            var changedVertices = new List<Vertex>();

            while(activeVertices.Count() > 0)
            {
                //Console.WriteLine("loop count {0}", ++count);
                //if its the first iteration all vertices are treated as active vertices

                // foreach u E A and for each edge e = {u,v}
                for (int i = 0; i < activeVertices.Count; i++)
                {
                    Vertex u = activeVertices[i];
                    Vertex v = null;
                    var edges = graphStructure.Where(x => x.U.Id == u.Id);
                    foreach (DirectEdge edge in edges)
                    {
                        //we didnt find the processing vertex , ask other processors
                        int processorWithWantedVertex = (int)(edge.V.Id / (totalVertices / comm.Size));
                        v = localVertices.FirstOrDefault(x => x.Id == edge.V.Id);
                        //check if we have the processing destination vertex
                        if (v == null)
                        {
                            /*
                            VertexRequest wantedVertex = new VertexRequest() { VertexId = edge.V.Id, RequestProcessorRank = comm.Rank };
                            VertexRequest foundVertex = null;

                            comm.SendReceive(wantedVertex, processorWithWantedVertex, 1, out foundVertex);
                            v = foundVertex.ResponseVertex;
                            Console.WriteLine("Processor {0} sends to {1} a vertex request for vertex {2}", comm.Rank, processorWithWantedVertex, wantedVertex.VertexId);
                            //comm.Gather(v, comm.Rank);*/

                            v = allVertices.FirstOrDefault(x => x.Id == edge.V.Id);
                        }

                        Relax(ref u, ref v, graphStructure, ref buckets, delta, ref changedVertices);
                        //Console.WriteLine("DoRelax finished, changed vertices : {0}",changedVertices.Count());
                    }
                }

                List<Vertex> newActiveVertices = new List<Vertex>();
                //intersection of two sets, active vertices and changed vertices
                foreach (Vertex changedVertex in changedVertices)
                {
                    if (activeVertices.Any(x => x.Id == changedVertex.Id))
                    {
                        newActiveVertices.Add(changedVertex);
                    }
                }
                //if (newActiveVertices.Count > 0)
                //{
                //    activeVertices.Clear();
                //    activeVertices.AddRange(newActiveVertices);
                //}

                activeVertices.Clear();
                activeVertices.AddRange(newActiveVertices);
                Console.WriteLine("New active vertices : {0}", activeVertices.Count);
            }
            comm.Barrier();

        }
    }
}
