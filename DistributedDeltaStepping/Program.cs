using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MPI;
using DistributedDeltaStepping.Domain;

namespace DistributedDeltaStepping
{
    class Program
    {
        //Delta Constant integer
        public const int Delta = 1;
        public const int numberOfNodes = 80;
        static void Main(string[] args)
        {
            List<Node> nodes = new List<Node>();
            
            using (new MPI.Environment(ref args))
            {
                Intracommunicator comm = Communicator.world;
                int numberOfBuckets = numberOfNodes / comm.Size;

                if (comm.Rank == 0)
                {
                    
                    //initialisation phase
                    //root node distance := 0
                    //all other distances of nodes are set to infinite
                    //create ten nodes
                    nodes = Utilities.FillListWithRandomVertices(numberOfNodes);

                    //root bucket gets only the root node
                    List<Node> bucketRoot = new List<Node>();
                    bucketRoot.Add(nodes.FirstOrDefault());
                    
                }
                else // not rank 0
                {
                    // program for all other ranks
                }
                comm.Barrier();

                //scatter v all nodes to each processor buckets
                IEnumerable<Node> bucket = comm.ScatterFromFlattened<Node>(nodes.ToArray(), numberOfBuckets, 0);

                Console.WriteLine("I'm processor {0} and I have {1} nodes in my bucket", comm.Rank, bucket.Count());

                Utilities.ProcessBucket(bucket);
            }
        }
    }
}
