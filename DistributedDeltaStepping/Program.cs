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
        public const int numberOfNodes = 80;
        static void Main(string[] args)
        {
            List<DirectEdge> graph = new List<DirectEdge>();
            using (new MPI.Environment(ref args))
            {
                Intracommunicator comm = Communicator.world;
                int numberOfVerticesPerProcessor = numberOfNodes / comm.Size;
                int k = 99999; //a very large number, theoritically close to infinite
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
                Console.WriteLine("Processor {0} , have {1} buckets", comm.Rank, buckets.Count());
                for (int i = 0; i < k; i++) //Epochs
                {
                    //TODO: ProcessBucket(Buckets[i])
                }
            }
        }
    }
}
