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
    class Program
    {
        //Delta Constant integer
        public const int Delta = 1;
        public const int numberOfNodes = 40;
        static void Main(string[] args)
        {
            List<DirectEdge> graph = new List<DirectEdge>();
            using (new MPI.Environment(ref args))
            {
                Intracommunicator comm = Communicator.world;
                int numberOfVerticesPerProcessor = numberOfNodes / comm.Size;
                int k = 99999; //a very large number, theoritically close to infinite
                int kInit = 99999;
                //local vertices will have distributed vertices among each processor
                Vertex[] localVertices = new Vertex[numberOfVerticesPerProcessor];
                List<Vertex> allVertices = new List<Vertex>();
                Bucket[] buckets = new Bucket[k];
                //initialize buckets
                //for k=1,2,3,4......n set Bk <- 0
                for (int i = 0; i < k; i++)
                {
                    buckets[i] = new Bucket();
                }
                if (comm.Rank == 0)
                {
                    //first create the random graph using .net graph libraries
                    graph = Utilities.CreateRandomGraph(numberOfNodes, out allVertices);
                }
                //broadcast complete graph structure to all processors
                comm.Broadcast(ref graph, 0);
                //The vertices are equally distributed among the processors
                localVertices = comm.ScatterFromFlattened(allVertices.ToArray(), numberOfVerticesPerProcessor, 0);
                //root node distance := 0
                //all other distances of nodes are set to infinite
                Utilities.InitVertices(ref localVertices, comm.Rank);
                if (comm.Rank == 0)
                {
                    //add root vertice to first bucket, Set B0 <- {rt}
                    buckets[0].Vertices.Add(localVertices.First());
                }
                //add all other vertices to the last bucket, Set Boo <- V - {rt}
                buckets[k - 1].Vertices.AddRange(localVertices.Skip(1));
                Console.WriteLine("Hello from processor : {0} , I have {1} vertices", comm.Rank, String.Join(",", localVertices.Select(x=>x.Id)));
                Console.WriteLine("Processor {0} , have {1} buckets with vertices filled in", comm.Rank, buckets.Where(x=>x.Vertices.Count>0).Count());
                comm.Barrier();
                k = 0;
                do{
                    var bucketToProcess = buckets[k];              
                    Console.WriteLine("Ready to process bucket[{0}]", k);
                    Utilities.ProcessBucket(ref bucketToProcess, graph, ref buckets, Delta, comm, localVertices, numberOfNodes);
                    comm.Barrier();
                    Console.WriteLine("Finished process of bucket[{0}]", k);
                    List<int> bucketIndexes = new List<int>();
                    for (int i = 0; i < buckets.Count(); i++)
                    {
                        if (buckets.Count() > 1)
                        {
                            bucketIndexes.Add(i);
                        }
                    }

                    int[] minBucketsIndexes = null;
                    comm.Allreduce(bucketIndexes.ToArray(), Operation<int>.Min, ref minBucketsIndexes);

                    k = minBucketsIndexes.FirstOrDefault();
                    Console.WriteLine("new k:{0}", k);
                    
                }
                while(k<kInit);
                comm.Barrier();
                Console.WriteLine("end");
                //ftou kai gvainw
                comm.Abort(0);
            }
        }
    }
}
