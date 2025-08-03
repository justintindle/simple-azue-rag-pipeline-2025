using Microsoft.AspNetCore.Mvc;
using RagApi.Services;

namespace RagApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RagController(IRagService ragService) : ControllerBase
{
    private readonly IRagService _ragService = ragService;

    [HttpGet("ask")]
    public async Task<IActionResult> Ask([FromQuery] string question)
    {
        var response = await _ragService.GetRagResponseAsync(question);
        return Ok(new { question, response });
    }
}
