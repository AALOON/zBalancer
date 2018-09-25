namespace zBalancer.Balancer.Dto
{
    /// <summary>
    /// Model of the node
    /// </summary>
    public class NodeDto
    {
        /// <summary>
        /// Identifier of the node
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Host of the node without port
        /// </summary>
        public string Host { get; set; }

        /// <summary>
        /// Port of the node
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// Scheme of the node - http, https and other
        /// </summary>
        public string Scheme { get; set; }
    }
}