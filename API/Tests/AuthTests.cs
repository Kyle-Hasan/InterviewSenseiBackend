using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using API.Auth;
using API.Users;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace  API.Tests;
public class AuthControllerTests
{
    private readonly Mock<IJwtTokenService> _mockJwtTokenService;
    private readonly Mock<UserManager<AppUser>> _mockUserManager;
    private readonly AuthController _authController;
    private readonly Mock<IResponseCookies> _mockResponseCookies;
    private readonly Mock<IRequestCookieCollection> _mockRequestCookies;
    private readonly DefaultHttpContext _httpContext;

    public AuthControllerTests()
    {
        _mockJwtTokenService = new Mock<IJwtTokenService>();
        _mockUserManager = new Mock<UserManager<AppUser>>(Mock.Of<IUserStore<AppUser>>(), null, null, null, null, null, null, null, null);
        _mockResponseCookies = new Mock<IResponseCookies>();
        _mockRequestCookies = new Mock<IRequestCookieCollection>();

        _httpContext = new DefaultHttpContext();
        _httpContext.Request.Headers["Cookie"] = "refreshToken=valid_token";
        _httpContext.Request.Cookies = _mockRequestCookies.Object;
        Mock.Get(_httpContext.Response).Setup(x => x.Cookies).Returns(_mockResponseCookies.Object);

        _authController = new AuthController(_mockJwtTokenService.Object, _mockUserManager.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = _httpContext }
        };
    }

    [Fact]
    public async Task RefreshAccessToken_ReturnsUnauthorized_WhenNoRefreshToken()
    {
        _mockRequestCookies.Setup(x => x["refreshToken"]).Returns((string)null);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _authController.RefreshAccessToken());
    }

    [Fact]
    public async Task RefreshAccessToken_ReturnsUnauthorized_WhenInvalidToken()
    {
        _mockRequestCookies.Setup(x => x["refreshToken"]).Returns("invalid_token");
        _mockJwtTokenService.Setup(x => x.ValidateRefreshToken("invalid_token")).Returns(-1);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _authController.RefreshAccessToken());
    }

    [Fact]
    public async Task RefreshAccessToken_ReturnsNewToken_WhenValid()
    {
        _mockRequestCookies.Setup(x => x["refreshToken"]).Returns("valid_token");
        _mockJwtTokenService.Setup(x => x.ValidateRefreshToken("valid_token")).Returns(1);
        _mockUserManager.Setup(x => x.Users).Returns(new List<AppUser> { new AppUser { Id = 1, UserName = "test" } }.AsQueryable());
        _mockJwtTokenService.Setup(x => x.GenerateToken(It.IsAny<AppUser>(), false)).ReturnsAsync("new_access_token");

        await _authController.RefreshAccessToken();
        _mockResponseCookies.Verify(x => x.Append("accessToken", "new_access_token", It.IsAny<CookieOptions>()), Times.Once);
    }

    [Fact]
    public async Task Logout_ClearsCookies()
    {
        await _authController.Logout();
        _mockResponseCookies.Verify(x => x.Delete("accessToken"), Times.Once);
        _mockResponseCookies.Verify(x => x.Delete("refreshToken"), Times.Once);
    }
}