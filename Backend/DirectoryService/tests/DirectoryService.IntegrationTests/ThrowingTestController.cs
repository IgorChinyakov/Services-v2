using Microsoft.AspNetCore.Mvc;

namespace DirectoryService.IntegrationTests;

[ApiController]
[Route("test/unhandled-exception")]
public sealed class ThrowingTestController : ControllerBase
{
    public const string SensitiveExceptionMessage = "Database credentials leaked";

    [HttpGet]
    public IActionResult Get()
    {
        throw new InvalidOperationException(SensitiveExceptionMessage);
    }
}
