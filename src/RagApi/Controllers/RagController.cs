using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class RagController : ControllerBase
{
    private readonly RagService _ragService;

    public RagController(RagService ragService)
    {
        _ragService = ragService;
    }

    [HttpGet("ask")]
    public async Task<IActionResult> Ask([FromQuery] string question)
    {
        var response = await _ragService.GetRagResponseAsync(question);
        return Ok(new { question, response });
    }
}
