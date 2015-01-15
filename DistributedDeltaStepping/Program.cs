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
                int k = numberOfNodes * 1000; //a very large number, related to number of total nodes given in graph

                DirectEdge[] localVertices = new DirectEdge[numberOfVerticesPerProcessor];
                Bucket[] buckets = new Bucket[k];
                //initialize buckets
                //for k=1,2,3,4......n set Bk <- 0
                for (int i = 0; i < numberOfNodes; i++)
                {
                    buckets[i] = new Bucket();
                }
                if (comm.Rank == 0)
                {
                    //first create the random graph using .net graph libraries
                    //root node distance := 0
                    //all other distances of nodes are set to infinite
                    graph = Utilities.CreateRandomGraph(numberOfNodes);
                    //add root vertice to first bucket, Set B0 <- {rt}
                    buckets[0].DirectEdges.Add(graph.First());
                    //add all other vertices to the last bucket, Set Boo <- V - {rt}
                    buckets[numberOfNodes - 1].DirectEdges.AddRange(graph.Skip(1));
                }
                else // not rank 0
                {
                    // program for all other ranks
                }
                //The vertices are equally distributed among the processors
                localVertices = comm.ScatterFromFlattened(graph.ToArray(), numberOfVerticesPerProcessor, 0);

                Console.WriteLine("Hello from processor : {0} , I have {1} vertices", comm.Rank, localVertices.Count());

                for (int i = 0; i < k; i++) //Epochs
                {
                    //TODO: ProcessBucket(Buckets[i])
                }
            }
        }
    }
}
