using System.Drawing;
using System.Linq.Expressions;
using FamilyLocator.DataLayer.DataBase.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Newtonsoft.Json;


namespace FamilyLocator.DataLayer.DataBase
{
    public class ApiDbContext : IdentityDbContext<XIdentityUser> 
    {
        public ApiDbContext(DbContextOptions<ApiDbContext> options) : base(options) { }
        public virtual DbSet<RefreshToken> RefreshTokens { get; set; }
        public virtual DbSet<Family> Families { get; set; }
        public virtual DbSet<XUCoordinates> XUCoordinates { get; set; }
        public virtual DbSet<UserImage> UserImages { get; set; }
        public virtual DbSet<XIdentityUserConfirm> UserConfirmations { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            builder.Entity<UserImage>(ui =>
            {
                ui.ToTable("AspNetUsers");
            });

            builder.Entity<XIdentityUser>(xiu =>
            {
                xiu.ToTable("AspNetUsers");
                xiu.HasOne(x => x.Image).WithOne().HasForeignKey<UserImage>(x => x.Id);
            });

            builder.Entity<Zone>().Property(x => x.Points).HasConversion(new ValueConverter<List<PointF>, string>(x=>JsonConvert.SerializeObject(x), x=>JsonConvert.DeserializeObject<List<PointF>>(x)));

            base.OnModelCreating(builder);
        }
    }
    
    
}
