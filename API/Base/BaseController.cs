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
                throw new UnauthorizedAccessException();
            }
            
            string idString = HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(idString))
            {
                throw new UnauthorizedAccessException();
            }
            int id = int.Parse(idString);
            
            AppUser user = await userManager.Users.FirstOrDefaultAsync(x => x.Id == id);

            if (user == null)
            {
                throw new UnauthorizedAccessException();
            }
            return user;
        }
    
    }
}