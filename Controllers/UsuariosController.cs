using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using WebApiAlmacen.DTOs;
using WebApiAlmacen.Models;
using WebApiAlmacen.Services.Hash;

namespace WebApiAlmacen.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsuariosController : ControllerBase
    {
        private readonly MiAlmacenContext _context;
        private readonly IConfiguration _configuration;
        private readonly IDataProtector dataProtector;
        private readonly IHashService _hashService;
        private readonly ILogger<UsuariosController> _logger;
        public UsuariosController(MiAlmacenContext context, IConfiguration configuration,
            IDataProtectionProvider dataProtectionProvider, IHashService hashService, ILogger<UsuariosController> logger)
        {
            _context = context;
            _configuration = configuration;
            dataProtector = dataProtectionProvider.CreateProtector(configuration["ClaveEncriptacion"]);
            _hashService = hashService;
            _logger = logger;
        }

        [HttpPost("encriptar/nuevousuario")]
        public async Task<ActionResult> PostNuevoUsuario([FromBody] DTOUsuario usuario)
        {
            var passEncriptado = dataProtector.Protect(usuario.Password);
            var newUsuario = new Usuario
            {
                Email = usuario.Email,
                Password = passEncriptado
            };
            _logger.LogInformation("Usuario creado");
            await _context.Usuarios.AddAsync(newUsuario);
            await _context.SaveChangesAsync();

            return Ok(newUsuario);
        }

        [HttpPost("encriptar/loginusuario")]
        public async Task<ActionResult> PostCheckUserPassEncriptado([FromBody] DTOUsuario usuario)
        {
            var usuarioDB = await _context.Usuarios.FirstOrDefaultAsync(x => x.Email == usuario.Email);
            if (usuarioDB == null)
            {
                return Unauthorized();
            }

            var passDesencriptado = dataProtector.Unprotect(usuarioDB.Password);
            if (usuario.Password == passDesencriptado)
            {
                return Ok();
            }
            else
            {
                return Unauthorized();
            }
        }

        [HttpPost("hash/nuevousuario")]
        public async Task<ActionResult> PostNuevoUsuarioHash([FromBody] DTOUsuario usuario)
        {
            var resultadoHash = _hashService.Hash(usuario.Password);
            var newUsuario = new Usuario
            {
                Email = usuario.Email,
                Password = resultadoHash.Hash,
                Salt = resultadoHash.Salt
            };

            await _context.Usuarios.AddAsync(newUsuario);
            await _context.SaveChangesAsync();

            return Ok(newUsuario);
        }

        [HttpPost("hash/loginusuario")]
        public async Task<ActionResult> CheckUsuarioHash([FromBody] DTOUsuario usuario)
        {
            var usuarioDB = await _context.Usuarios.FirstOrDefaultAsync(x => x.Email == usuario.Email);
            if (usuarioDB == null)
            {
                return Unauthorized();
            }

            var resultadoHash = _hashService.Hash(usuario.Password, usuarioDB.Salt);
            if (usuarioDB.Password == resultadoHash.Hash)
            {
                return Ok();
            }
            else
            {
                return Unauthorized();
            }
        }

        [HttpPost("login")]
        public async Task<ActionResult> Login([FromBody] DTOUsuario usuario)
        {
            var usuarioDB = await _context.Usuarios.FirstOrDefaultAsync(x => x.Email == usuario.Email);
            if (usuarioDB == null)
            {
                return BadRequest();
            }

            var resultadoHash = _hashService.Hash(usuario.Password, usuarioDB.Salt);
            if (usuarioDB.Password == resultadoHash.Hash)
            {
                var response = GenerarToken(usuario);
                return Ok(response);
            }
            else
            {
                _logger.LogInformation("Usuario no autenticado por credenciales erróneas a las: " + DateTime.Now);
                return Unauthorized();
            }
        }

        private DTOLoginResponse GenerarToken(DTOUsuario credencialesUsuario)
        {
            var claims = new List<Claim>()
            {
                new Claim(ClaimTypes.Email, credencialesUsuario.Email),
                new Claim("lo que yo quiera", "cualquier otro valor")
            };

            var clave = _configuration["ClaveJWT"];
            var claveKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(clave));
            var signinCredentials = new SigningCredentials(claveKey, SecurityAlgorithms.HmacSha256);

            var securityToken = new JwtSecurityToken(
                claims: claims,
                expires: DateTime.Now.AddDays(30),
                signingCredentials: signinCredentials
            );


            var tokenString = new JwtSecurityTokenHandler().WriteToken(securityToken);

            return new DTOLoginResponse()
            {
                Token = tokenString,
                Email = credencialesUsuario.Email
            };
        }




    }
}
