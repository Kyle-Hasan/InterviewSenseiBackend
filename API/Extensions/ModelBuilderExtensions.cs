using System.Linq.Expressions;
using API.Base;
using Microsoft.EntityFrameworkCore;

namespace API.Extensions;

public static class ModelBuilderExtensions
{
    public static  ModelBuilder AddBaseEntity<T>(this ModelBuilder builder) where T : BaseEntity
    {
        builder.Entity<T>().HasOne(p => p.CreatedBy)                
            .WithMany()                               
            .HasForeignKey(p => p.CreatedById)       
            .OnDelete(DeleteBehavior.Cascade);  
        return builder;
    }
}