using System;
using System.Security.Cryptography;
using zBalancer.Balancer.Models;

namespace zBalancer.Balancer.Services
{
    public class RandomNodeSelectionService : INodeSelectionService
    {
        // use RNGCryptoServiceProvider if needed
        private readonly Random _random = new Random();

        public Node GetNode(Node[] nodes)
        {
            var index = nodes.Length > 1 ? _random.Next(0, nodes.Length) : 0;
            return nodes[index];
        }
    }
}