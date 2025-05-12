using Microsoft.AspNetCore.Mvc;
using VisitService.DTOs;
using VisitService.Services;

namespace VisitService.Controllers;

[ApiController]
[Route("api/visits")]
public class VisitsController : ControllerBase
{
    private readonly IVisitService _service;
    public VisitsController(IVisitService service) => _service = service;

    [HttpGet("{id}")]
    public async Task<IActionResult> GetVisit(int id)
    {
        try
        {
            var result = await _service.GetVisitAsync(id);
            return Ok(result);
        }
        catch (Exception e) when (e.Message == "Not Found")
        {
            return NotFound();
        }
    }

    [HttpPost]
    public async Task<IActionResult> AddVisit([FromBody] VisitRequestDTO dto)
    {
        try
        {
            await _service.AddVisitAsync(dto);
            return Created($"/api/visits/{dto.VisitId}", null);
        }
        catch (Exception e) when (e.Message.Contains("Not Found"))
        {
            return NotFound(e.Message);
        }
        catch (Exception e) when (e.Message == "Conflict")
        {
            return Conflict();
        }
        catch (Exception e)
        {
            return BadRequest(e.Message);
        }
    }
}