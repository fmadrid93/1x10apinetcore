using Application.Votante;
using Dtos.Votante;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
//[Authorize]
public class VotanteController : ControllerBase
{
    private readonly VotanteService _service = new VotanteService();
    [HttpGet("obtener-votante")]
    public IActionResult ObtenerVotante([FromQuery] string ci)
    {
        try
        {
            var ds = _service.ObtenerVotante(ci);
            return Ok(new
            {
                exito = 1,
                dato = ds,
                status = "ok"
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new
            {
                exito = 0,
                dato = (object?)null,
                status = ex.Message
            });
        }
    }
    [HttpGet("buscar-global")]
    public IActionResult BuscarGlobal([FromQuery] string texto)
    {
        try
        {
            var ds = _service.BuscarPadronGlobal(texto);
            return Ok(new
            {
                exito = 1,
                dato = ds,
                status = "ok"
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new
            {
                exito = 0,
                dato = (object?)null,
                status = ex.Message
            });
        }
    }

    [HttpPost("marcar-ya-voto")]
    public IActionResult MarcarYaVoto([FromBody] VotanteMarcarYaVotoRequest request)
    {
        try
        {
            var ds = _service.MarcarYaVoto(
                request.IdVotante,
                request.IdUsuarioMarca,
                request.Observacion
            );

            return Ok(new
            {
                exito = 1,
                dato = ds,
                status = "ok"
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new
            {
                exito = 0,
                dato = (object?)null,
                status = ex.Message
            });
        }
    }
}