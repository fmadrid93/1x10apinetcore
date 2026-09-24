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
        [FromQuery] string? nroMesa = null,
        [FromQuery] int? idTerritorio = null)
    {
        try
        {
            string? recintoFinal = !string.IsNullOrWhiteSpace(idRecinto) ? idRecinto.Trim() : (!string.IsNullOrWhiteSpace(recinto) ? recinto.Trim() : null);
            var dt = _service.BuscarPadronGlobal(texto ?? "", recintoFinal, nroMesa, idTerritorio);

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

        [HttpGet("recinto-monitoreo-conteos")]
    public IActionResult RecintoDiadConteos([FromQuery] string idRecinto, [FromQuery] int? idAdmin = null, [FromQuery] string? nroMesa = null)
    {
        try
        {
            var ds = _service.RecintoDiadConteos(idRecinto, idAdmin, nroMesa);
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

    [HttpGet("recinto-padron-faltan")]
    public IActionResult RecintoPadronFaltan(
        [FromQuery] string idRecinto,
        [FromQuery] string? nroMesa = null,
        [FromQuery] string? texto = null,
        [FromQuery] int offset = 0,
        [FromQuery] int limit = 100)
    {
        try
        {
            var dt = _service.RecintoPadronFaltan(idRecinto, nroMesa, texto, offset, limit);
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

    [HttpGet("recinto-votaron-no-registrados")]
    public IActionResult RecintoVotaronNoRegistrados(
        [FromQuery] string idRecinto,
        [FromQuery] string? nroMesa = null,
        [FromQuery] string? texto = null,
        [FromQuery] int offset = 0,
        [FromQuery] int limit = 100)
    {
        try
        {
            var dt = _service.RecintoVotaronNoRegistrados(idRecinto, nroMesa, texto, offset, limit);
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

    [HttpGet("recinto-registrados-faltan")]
    public IActionResult RecintoRegistradosFaltan(
        [FromQuery] string idRecinto,
        [FromQuery] int? idAdmin = null,
        [FromQuery] string? nroMesa = null,
        [FromQuery] string? texto = null,
        [FromQuery] int offset = 0,
        [FromQuery] int limit = 100)
    {
        try
        {
            var dt = _service.RecintoRegistradosFaltan(idRecinto, idAdmin, nroMesa, texto, offset, limit);
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

    [HttpGet("recinto-mesas/{idRecinto}")]
    public IActionResult RecintoMesas(string idRecinto)
    {
        try
        {
            var dt = _service.RecintoMesas(idRecinto);
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

    [HttpGet("veedores-rendimiento")]
    public IActionResult VeedoresRendimiento(
        [FromQuery] int? idTerritorio = null,
        [FromQuery] int? idAdmin = null,
        [FromQuery] int? idGerente = null)
    {
        try
        {
            var dt = _service.VeedoresRendimientoResumen(idTerritorio, idAdmin, idGerente);
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

    [HttpGet("votantes-marcados")]
    public IActionResult VotantesMarcados(
        [FromQuery] int? idUsuarioMarca = null,
        [FromQuery] string? tipoMarca = null,
        [FromQuery] int? idTerritorio = null,
        [FromQuery] int? idAdmin = null,
        [FromQuery] int? idGerente = null,
        [FromQuery] int? idMovilizador = null,
        [FromQuery] string? texto = null,
        [FromQuery] int offset = 0,
        [FromQuery] int limit = 1000)
    {
        try
        {
            var dt = _service.VotantesMarcadosListar(idUsuarioMarca, tipoMarca, idTerritorio, idAdmin, idGerente, idMovilizador, texto, offset, limit);
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
}