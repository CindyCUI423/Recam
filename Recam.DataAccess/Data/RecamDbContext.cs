using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Recam.Models.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recam.DataAccess.Data
{
    public class RecamDbContext: IdentityDbContext<
        User,
        Role,
        string,
        IdentityUserClaim<string>,
        UserRole,
        IdentityUserLogin<string>,
        IdentityRoleClaim<string>,
        IdentityUserToken<string>
        >
    {
        public RecamDbContext(DbContextOptions<RecamDbContext> options): base(options)
        {
            
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<UserRole> UserRoles { get; set; }
        public DbSet<Agent> Agents { get; set; }
        public DbSet<PhotographyCompany> PhotographyCompanies { get; set; }
        public DbSet<ListingCase> ListingCases { get; set; }
        public DbSet<AgentPhotographyCompany> AgentPhotographyCompanies { get; set; }
        public DbSet<AgentListingCase> AgentListingCases { get; set; }
        public DbSet<CaseContact> CaseContacts { get; set; }
        public DbSet<MediaAsset> MediaAssets { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<User>(b =>
            {
                b.ToTable("User");
            });

            builder.Entity<Role>(b =>
            {
                b.ToTable<Role>("Role");
            });

            builder.Entity<UserRole>(b => 
            {
                b.ToTable<UserRole>("UserRole");

                b.HasKey(ur => new { ur.UserId, ur.RoleId });

                b.HasOne(ur => ur.User)
                    .WithMany(u => u.UserRoles)
                    .HasForeignKey(ur => ur.UserId);

                b.HasOne(ur => ur.Role)
                    .WithMany(r => r.UserRoles)
                    .HasForeignKey(ur => ur.RoleId);

            });

            // User - Agent (1 : 1)
            builder.Entity<Agent>(b => 
            {
                b.HasOne(a => a.User)
                    .WithOne(u => u.Agent)
                    .HasForeignKey<Agent>(a => a.Id)
                     .OnDelete(DeleteBehavior.Restrict);
                
            });

            // User - PhotographyCompany (1 : 1)
            builder.Entity<PhotographyCompany>(b => 
            {
                b.HasOne(p => p.User)
                    .WithOne(u => u.PhotographyCompany)
                    .HasForeignKey<PhotographyCompany>(p => p.Id)
                    .OnDelete(DeleteBehavior.Restrict);
                
            });

            // Agent - ListingCase (M : N)
            builder.Entity<AgentListingCase>(b => 
            {
                // Composite PK
                b.HasKey(al => new { al.AgentId, al.ListingCaseId });

                b.HasOne(al => al.Agent)
                    .WithMany(a => a.AgentListingCases)
                    .HasForeignKey(al => al.AgentId);

                b.HasOne(al => al.ListingCase)
                    .WithMany(l => l.AgentListingCases)
                    .HasForeignKey(al => al.ListingCaseId);

            });

            // Agent - PhotographyCompany (M : N)
            builder.Entity<AgentPhotographyCompany>(b =>
            {
                b.HasKey(ap => new { ap.AgentId, ap.PhotographyCompanyId });

                b.HasOne(ap => ap.Agent)
                    .WithMany(a => a.AgentPhotographyCompanies)
                    .HasForeignKey(ap => ap.AgentId)
                    .OnDelete(DeleteBehavior.Restrict);

                b.HasOne(ap => ap.PhotographyCompany)
                    .WithMany(p => p.AgentPhotographyCompanies)
                    .HasForeignKey(ap => ap.PhotographyCompanyId)
                    .OnDelete(DeleteBehavior.Restrict);
                ;
            });

            // ListingCase - CaseContact (1 : N)
            builder.Entity<CaseContact>(b =>
            { 
                b.HasOne(cc => cc.ListingCase)
                    .WithMany(lc => lc.CaseContacts)
                    .HasForeignKey(cc => cc.ListingCaseId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ListingCase - MediaAsset (1 : N)
            builder.Entity<MediaAsset>(b => 
            {
                b.HasOne(ma => ma.ListingCase)
                    .WithMany(lc => lc.MediaAssets)
                    .HasForeignKey(ma => ma.ListingCaseId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // User - MediaAsset (1 : N)
            builder.Entity<MediaAsset>(b =>
            {
                b.HasOne(ma => ma.User)
                    .WithMany(u => u.MediaAssets)
                    .HasForeignKey(ma => ma.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });



        }
    }
}
