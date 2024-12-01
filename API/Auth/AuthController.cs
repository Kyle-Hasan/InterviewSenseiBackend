using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API.Base;
using API.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Auth
{
    
    public class AuthController(IJwtTokenService jwtTokenService,UserManager<AppUser> userManager) : BaseController(userManager)

    {
        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<ActionResult<UserDTO>> Login(LoginDto loginDto) {
            var user = await userManager.Users.FirstOrDefaultAsync(x=> x.NormalizedUserName == loginDto.Username.ToUpper());

            if(user == null || user.UserName == null) {
                return Unauthorized("bad login");
            }

            var result = await userManager.CheckPasswordAsync(user,loginDto.Password);
            if(!result) {
                return Unauthorized();
            }
                
           
            string accessToken =  await jwtTokenService.GenerateToken(user,false);
            string refreshToken = await jwtTokenService.GenerateToken(user,true);

            Response.Cookies.Append("accessToken", accessToken, new CookieOptions
        {
            HttpOnly = true, 
            Secure = true,   
            SameSite = SameSiteMode.None, 
            Expires = DateTime.UtcNow.AddMinutes(15) 
        });

        
        Response.Cookies.Append("refreshToken", refreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.None,
            Expires = DateTime.UtcNow.AddDays(7) 
        });
                return new UserDTO {
                    Username = loginDto.Username,
                    AccessToken = accessToken,
                    RefreshToken = refreshToken,
                    userId = user.Id
                    };
                

                
                

        }

        [AllowAnonymous]
        [HttpPost("register")]

        public async Task<ActionResult<UserDTO>> Register(RegisterDTO registerDTO){
            bool exists = await UserExists(registerDTO.Username);

            if(exists) {
                return BadRequest("username taken");
            }

            AppUser user = new AppUser {
                UserName = registerDTO.Username,
                Email = registerDTO.Email
            };

            var result = await userManager.CreateAsync(user,registerDTO.Password);
            
            if(result.Succeeded) {
            var createdUser = await userManager.FindByNameAsync(user.UserName);

            string accessToken =  await jwtTokenService.GenerateToken(user,false);
            string refreshToken = await jwtTokenService.GenerateToken(user,true);


           
        Response.Cookies.Append("accessToken", accessToken, new CookieOptions
        {
            HttpOnly = true, 
            Secure = true,   
            SameSite = SameSiteMode.Strict, 
            Expires = DateTime.UtcNow.AddMinutes(15) 
        });

        
        Response.Cookies.Append("refreshToken", refreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = DateTime.UtcNow.AddDays(7) 
        });

            return new UserDTO
            {
                Username = registerDTO.Username,
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                userId = createdUser.Id
            };

            }
            else {
                return Problem();
            }
        

            


        }

        private async Task<bool> UserExists(string username)
        {
            return await userManager.Users.AnyAsync(x=> x.NormalizedUserName == username.ToUpper() );
        }
    }
}