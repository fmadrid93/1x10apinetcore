using Application.Auth;
using Dtos.Auth;
using Microsoft.AspNetCore.Mvc;
using System;

namespace ApiWeb.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AuthService _service;

        public AuthController(AuthService service)
        {
            _service = service;
        }

        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginRequest request)
        {
            try
            {
                var resp = _service.Login(request.Usuario, request.Clave);
                return Ok(resp);
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
        [HttpGet("test")]
        public IActionResult Test()
        {
            try
            {
              
                return Ok("HOla mundo 1");
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
}