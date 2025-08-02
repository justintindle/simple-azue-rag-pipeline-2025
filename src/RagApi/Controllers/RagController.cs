using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class RagController(RagService ragService) : ControllerBase
{
    private readonly RagService _ragService = ragService;

    [HttpGet("ask")]
    public async Task<IActionResult> Ask([FromQuery] string question)
    {
        var response = await _ragService.GetRagResponseAsync(question);
        return Ok(new { question, response });
    }
}
