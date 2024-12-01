using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using API.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Base
{
    [ApiController]
    [Route("api/[controller]")]
    public class BaseController(UserManager<AppUser> userManager): ControllerBase
    {
        [NonAction]
        public async Task<AppUser> GetCurrentUser()
        {
            
            if (HttpContext.User?.Identity?.IsAuthenticated != true)
            {
                return null; 
            }
            
            string idString = HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(idString))
            {
                return null; 
            }
            int id = int.Parse(idString);
            
            return await userManager.Users.FirstOrDefaultAsync(x => x.Id == id);
        }
    
    }
}