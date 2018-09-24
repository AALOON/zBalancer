namespace zBalancer.Balancer.Models
{
    public enum ColorMark
    {
        Blue,
        Green
    }

    public class Node
    {
        public int Id { get; set; }
        public string Host { get; set; }
        public int Port { get; set; }
        public string Scheme { get; set; }

        public ColorMark Color { get; set; }
    }
}