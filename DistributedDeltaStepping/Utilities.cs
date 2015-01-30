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
            //Set d(rt) ← 0; for all v 6= rt, set d(v) ← ∞
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
            //get the direct edge corresponding to source and destination
            var uId = u.Id;
            var vId = v.Id; 
            var edge = graphStructure.Where(x => x.U.Id == uId && x.V.Id == vId).FirstOrDefault();

            int oldBucketIndex = v.DistanceToRoot / delta;
            bool changed = false;
            //Old bucket: i ← D(v)/ ∆ 
            //d(v) ← min{d(v),d(u) + w(u,v)}
            //New bucket : j ← D(v) / Δ 
            if ((u.DistanceToRoot + edge.Cost) < v.DistanceToRoot)
            {
                v.DistanceToRoot = u.DistanceToRoot + edge.Cost;
                changed = true;
            }
            int newBucketIndex = v.DistanceToRoot / delta;
            if (newBucketIndex < oldBucketIndex)
            {
                //If j < i, move v from Bi to Bj.
                buckets[oldBucketIndex-1].Vertices.Remove(v);
                if (newBucketIndex > 0)
                {
                    newBucketIndex = newBucketIndex - 1;
                }

                buckets[newBucketIndex].Vertices.Add(v);
            }
            else if (newBucketIndex == oldBucketIndex)
            {
                //A0 ← {x : d(x) changed in the previous step} 
                if(changed == true)
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

            /* The thing is there is a lot of re-insertion to the buckets which result a lot of iteration of the outer loop which lead to a lot of request for distance vector. 
             * If the number of nodes is large, there must be a lot lot of re-insertion… And network is congested. 
             * So, to reduce network overhead, we decided to use p2p model. 
             * That is, all nodes will get updates of all other nodes. 
             * By doing this, all nodes automatically gets updated and they do not even need to ask master node to send out newest copy*/
            var allVertices = comm.AllgatherFlattened(localVertices, localVertices.Count());
            //Console.WriteLine("Active Vertices : {0}", String.Join(",", activeVertices.Select(x=>x.Id)));
            var changedVertices = new List<Vertex>();
            //A ← Bk. 
            //active vertices While A 6= ∅ 
            //phases For each u ∈ A and for each edge e = hu,vi Do Relax(u,v) 
            //A0 ← {x : d(x) changed in the previous step} A ← Bk ∩ A0
            while(activeVertices.Count() > 0)
            {
                //if its the first iteration all vertices are treated as active vertices
                changedVertices.Clear();

                // foreach u E A and for each edge e = {u,v}
                for (int i = 0; i < activeVertices.Count; i++)
                {
                    Vertex u = activeVertices[i];
                    Vertex v = null;

                    var edges = graphStructure.Where(x => x.U.Id == u.Id);
                    foreach (DirectEdge edge in edges)
                    {
                        //we didnt find the processing vertex , ask other processors
                        v = localVertices.FirstOrDefault(x => x.Id == edge.V.Id);
                        //check if we have the processing destination vertex
                        if (v == null)
                        {
                            //
                            v = allVertices.FirstOrDefault(x => x.Id == edge.V.Id);
                        }

                        Relax(ref u, ref v, graphStructure, ref buckets, delta, ref changedVertices);
                        //Console.WriteLine("DoRelax finished, changed vertices : {0}", changedVertices.Count());
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

                //A ← Bk ∩ A'
                activeVertices.Clear();
                activeVertices.AddRange(newActiveVertices);
            }
            comm.Barrier();

        }
    }
}
