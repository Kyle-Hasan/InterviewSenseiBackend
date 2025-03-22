using API.Base;
using API.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace API.CodeRunner;

public class CodeRunnerController(
    ICodeRunnerService codeRunnerService,
    UserManager<AppUser> userManager
) : BaseController(userManager)
{
    [HttpPost("submitCode")]
    public async Task<string> SubmitCode(RunCodeRequest request)
    {
        var token = await codeRunnerService.RunCode(request);
        return token;
    }

    [HttpPost("runCode")]
    public async Task<RunCodeResult> GetCodeResult(TokenResult tokenObj)
    {
        var result= await codeRunnerService.GetCodeResult(tokenObj.token);
        return result;
    }
}