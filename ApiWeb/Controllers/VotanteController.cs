using System;
using System.Data;
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
    public IActionResult BuscarGlobal(
        [FromQuery] string? texto,
        [FromQuery] string? recinto = null,
        [FromQuery] string? idRecinto = null,
        [FromQuery] string? nroMesa = null)
    {
        try
        {
            string? recintoFinal = !string.IsNullOrWhiteSpace(idRecinto) ? idRecinto.Trim() : (!string.IsNullOrWhiteSpace(recinto) ? recinto.Trim() : null);
            var dt = _service.BuscarPadronGlobal(texto ?? "", recintoFinal, nroMesa);


            return Ok(new
            {
                exito = 1,
                dato = dt,
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

    [HttpGet("top10")]
    public IActionResult ObtenerTop10([FromQuery] int? idTerritorio = null)
    {
        try
        {
            var ds = _service.ObtenerTop10(idTerritorio);
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

    [HttpGet("top50")]
    public IActionResult ObtenerTop50([FromQuery] int? idTerritorio = null)
    {
        try
        {
            var ds = _service.ObtenerTop50(idTerritorio);
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

    [HttpPost("marcar-paso-pc")]
    public IActionResult MarcarPasoPorElPC([FromBody] VotanteMarcarPasoPCRequest request)
    {
        try
        {
            var ds = _service.MarcarPasoPorElPC(
                request.IdVotante,
                request.IdUsuarioMarca
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