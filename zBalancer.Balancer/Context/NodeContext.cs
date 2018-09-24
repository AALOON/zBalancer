using Microsoft.EntityFrameworkCore;
using zBalancer.Balancer.Models;

namespace zBalancer.Balancer.Context
{
    public class NodeContext : DbContext
    {
        public NodeContext(DbContextOptions<NodeContext> options)
            : base(options)
        { }

        public DbSet<Node> Nodes { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Node>()
                .Property(e => e.Id)
                .ValueGeneratedOnAdd();
        }
    }
}