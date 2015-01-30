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
        public const int Delta = 10;
        public const int numberOfNodes = 40;
        static void Main(string[] args)
        {
            List<DirectEdge> graph = new List<DirectEdge>();
            using (new MPI.Environment(ref args))
            {
                Intracommunicator comm = Communicator.world;
                int numberOfVerticesPerProcessor = numberOfNodes / comm.Size;
                int k = 1000; //a very large number, theoritically close to infinite
                int kInit = 1000;
                //local vertices will have distributed vertices among each processor
                Vertex[] localVertices = new Vertex[numberOfVerticesPerProcessor];
                List<Vertex> allVertices = new List<Vertex>();
                Bucket[] buckets = new Bucket[k];
                //initialize buckets
                //For k = 1,2,...,, set Bk ← ∅.
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
                
                //Set B0 ← {rt} and B∞ ← V − {rt}. 
                Utilities.InitVertices(ref localVertices, comm.Rank, kInit);
                if (comm.Rank == 0)
                {
                    //add root vertice to first bucket, Set B0 <- {rt}
                    buckets[0].Vertices.Add(localVertices.First());
                    //add all other vertices to the last bucket, Set Boo <- V - {rt}
                    buckets[k - 1].Vertices.AddRange(localVertices.Skip(1));
                }
                else
                {
                    buckets[k - 1].Vertices.AddRange(localVertices);
                }
                
                Console.WriteLine("Hello from processor : {0} , I have {1} vertices", comm.Rank, String.Join(",", localVertices.Select(x=>x.Id)));
                Console.WriteLine("Processor {0} , have {1} buckets with vertices filled in", comm.Rank, buckets.Where(x=>x.Vertices.Count>0).Count());
                comm.Barrier();
                //∆-Stepping Algorithm k ← 0. Loop // Epochs 
                k = 0;
                do{
                    var bucketToProcess = buckets[k];              
                    Console.WriteLine("P[{1}] Ready to process bucket[{0}]", k, comm.Rank);
                    Utilities.ProcessBucket(ref bucketToProcess, graph, ref buckets, Delta, comm, localVertices, numberOfNodes);
                    Console.WriteLine("P[{1}] Finished process of bucket[{0}]", k, comm.Rank);
                    //Next bucket index : k ← min{i > k : Bi := ∅}. 
                    int[] bucketIndexes = new int[kInit];
                    
                    for (int i = k; i < buckets.Count(); i++)
                    {
                        if (buckets[i].Vertices.Count() > 0)
                        {
                            bucketIndexes[i] = i;
                        }
                    }
                    
                    int min = kInit;
                    bucketIndexes = bucketIndexes.Where(x => x != 0 && x > k).ToArray();

                    if (bucketIndexes.Count() != 0)
                    {
                        min = bucketIndexes.Min();
                    }
                    // Termination checks and computing the next bucket index require Allreduce operations.
                    k = comm.Allreduce(min, Operation<int>.Min);

                    // The iterations and epochs are executed in a bulk synchronous manner, thus a barrier is needed
                    comm.Barrier();
                }
                while(k<kInit);

                comm.Barrier();
                if (comm.Rank == 0) Console.WriteLine("Terminated successfully!");
            }
        }
    }
}
