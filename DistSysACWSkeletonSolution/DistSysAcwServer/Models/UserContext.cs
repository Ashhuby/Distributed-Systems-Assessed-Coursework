using Microsoft.EntityFrameworkCore;

namespace DistSysAcwServer.Models
{
    /// <summary>
    /// Entity Framework database context for the Distributed Systems ACW.
    /// Manages Users, Logs, and LogArchives tables via Code First.
    /// </summary>
    public class UserContext : DbContext
    {
        public UserContext() : base() { }

        /// <summary>
        /// The set of registered users in the system.
        /// </summary>
        public required DbSet<User> Users { get; set; }

        /// <summary>
        /// Active log entries linked to existing users.
        /// </summary>
        public required DbSet<Log> Logs { get; set; }

        /// <summary>
        /// Archived log entries preserved after a user has been deleted.
        /// </summary>
        public required DbSet<LogArchive> LogArchives { get; set; }

        /// <summary>
        /// Configures the database connection. Uses SQL Server LocalDB as specified.
        /// Do not change this connection string — it must be restored before submission.
        /// </summary>
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            //optionsBuilder.UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=DistSysAcw;"); // Remember to change this back on submission                        
            optionsBuilder.UseSqlServer("Server=(localdb)\\DistSysAcw;Database=DistSysAcw;Integrated Security=true;");
        }

        /// <summary>
        /// Configures the entity relationships and delete behaviours.
        /// </summary>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // When a User is deleted, set the UserApiKey on their Logs to null
            // rather than cascade-deleting the logs. This preserves log data.
            modelBuilder.Entity<Log>()
                .HasOne(l => l.User)
                .WithMany(u => u.Logs)
                .HasForeignKey(l => l.UserApiKey)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}