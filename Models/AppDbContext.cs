using Microsoft.EntityFrameworkCore;
using MeetingScheduler.Models;

namespace MeetingScheduler.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Meeting> Meetings { get; set; }
    }
}
