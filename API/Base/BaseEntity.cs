using System.ComponentModel.DataAnnotations;
using API.Users;

namespace API.Base;

public abstract class BaseEntity
{
    public int Id { get; set; }
    [Required]
    public DateTime CreatedDate { get; set; }
    [Required]
    public DateTime ModifiedDate { get; set; }
    [Required]
    public AppUser CreatedBy { get; set; }
    [Required]
    public int CreatedById { get; set; }
    
}