
using API.Base;
using API.Extensions;
using API.InteractiveInterviewFeedback;
using API.Interviews;
using API.Messages;
using API.Questions;
using API.Responses;
using API.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace API.Data
{
    public class AppDbContext : IdentityDbContext<AppUser, AppRole, int>
    {
        
        
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            
           
            builder.AddBaseEntity<Question>();
            
            
            builder.Entity<Question>()
                .HasMany(q=> q.Responses)
                .WithOne(r => r.Question)
                .HasForeignKey(r => r.QuestionId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);


            builder.AddBaseEntity<Response>();
            builder.AddBaseEntity<Message>();
            builder.AddBaseEntity<InterviewFeedback>();
            
            
            
            
            builder.AddBaseEntity<Interview>();
            builder.Entity<Interview>()
                .HasMany(i => i.Questions)
                .WithOne(q => q.Interview)
                .HasForeignKey(q => q.InterviewId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade)
                ;
            builder.Entity<Interview>()
                .HasMany(i => i.Messages)
                .WithOne(q => q.Interview)
                .HasForeignKey(q => q.InterviewId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);
            
            builder.Entity<Interview>()
                .HasOne(i=> i.Feedback)
                .WithOne(f => f.Interview)
                .HasForeignKey<InterviewFeedback>(f => f.InterviewId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);


        }

        
        public  async Task<int>  SaveChangesAsync(
            bool acceptAllChangesOnSuccess,
            AppUser user,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            var AddedEntities = ChangeTracker.Entries()
                
                .Where(E => E.State == EntityState.Added )
                .ToList();

            AddedEntities.ForEach(E =>
            {
                E.Property("CreatedDate").CurrentValue = DateTime.Now;
               
                E.Property("CreatedById").CurrentValue = user.Id; ;
            });

            var EditedEntities = ChangeTracker.Entries()
                .Where(E => E.State == EntityState.Modified)
                .ToList();

            EditedEntities.ForEach(E =>
            {
                if ((int)E.Property("CreatedById").CurrentValue != user.Id)
                {
                    throw new UnauthorizedAccessException("You are not authorized to edit this object.");
                }
                E.Property("ModifiedDate").CurrentValue = DateTime.Now;
            });

            var deletedEntites = ChangeTracker.Entries()
                .Where(E => E.State == EntityState.Deleted)
                .ToList();
            deletedEntites.ForEach(
                E =>
                {
                    if ((int)E.Property("CreatedById").CurrentValue != user.Id)
                    {
                        throw new UnauthorizedAccessException("You are not authorized to delete this object.");
                    }
                    E.Property("ModifiedDate").CurrentValue = DateTime.Now;
                }
                );

            var saveEntites = new List<EntityEntry>();
            saveEntites.AddRange(AddedEntities);
            saveEntites.AddRange(EditedEntities);
            saveEntites.ForEach((E) =>
            {
                var entity = E.Entity;
                var properties = entity.GetType().GetProperties();

                foreach (var property in properties)
                {
                    if (property.GetValue(entity) is string value)
                    {
                        property.SetValue(entity,value.Escape());
                    }
                }
                


            });
            

            return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }
        
        
    }
}
