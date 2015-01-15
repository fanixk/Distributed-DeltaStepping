﻿using System;
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
                DirectEdge[] localGraph = new DirectEdge[numberOfVerticesPerProcessor];

                if (comm.Rank == 0)
                {
                    //first create the random graph using .net graph libraries
                    graph = Utilities.CreateRandomGraph(numberOfNodes);

                    //initialisation phase
                    //root node distance := 0
                    //all other distances of nodes are set to infinite
                    //create ten nodes


                }
                else // not rank 0
                {
                    // program for all other ranks
                }

                //The vertices are equally distributed among the processors
                localGraph = comm.ScatterFromFlattened(graph.ToArray(), numberOfVerticesPerProcessor, 0);
                Console.WriteLine("Hello from processor : {0} , I have {1} vertices", comm.Rank, localGraph.Count());
                
            }
        }
    }
}
