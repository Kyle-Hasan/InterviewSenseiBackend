using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace API.Users
{
    public class UserDTO
{
    [Required]
    public string Username {get;set;} = string.Empty;
    [Required]

    public string RefreshToken {get;set;} = string.Empty;

    [Required]

    public string AccessToken {get;set;} = string.Empty;

    [Required]
    public int userId {get;set;}
}
}