﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using ServerBackendHooliTees.Models.Database;
using ServerBackendHooliTees.Models.Database.Entities;
using ServerBackendHooliTees.Models.Dtos;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;

namespace ServerBackendHooliTees.Controllers;

[Route("api/[controller]")]
[ApiController]
public class UserController : ControllerBase
{

    private MyDbContext dbContextHoolitees;
    private PasswordHasher<string> passwordHasher = new PasswordHasher<string>();
    //var passwordHasher = new PasswordHasher<string>();
    private readonly TokenValidationParameters _tokenParameters;

    public UserController(MyDbContext dbContext, IOptionsMonitor<JwtBearerOptions> jwtOptions)
    {
        //  Base de Datos
        dbContextHoolitees = dbContext;

        //  JWToken
        _tokenParameters = jwtOptions.Get(JwtBearerDefaults.AuthenticationScheme)
            .TokenValidationParameters;
    }


    [HttpGet("userlist")]
    public IEnumerable<UserSignDto> GetUser()
    {
        return dbContextHoolitees.Users.Select(ToDto);
    }


    [HttpPost("signup")]
    public async Task<IActionResult> Post([FromForm] UserSignDto userSignDto)
    {
        string hashedPassword = passwordHasher.HashPassword(userSignDto.Name, userSignDto.Password);

        Users newUser = new Users()
        {
            Name = userSignDto.Name,
            Email = userSignDto.Email,
            Password = hashedPassword,
            Address = userSignDto.Address
        };

        await dbContextHoolitees.Users.AddAsync(newUser);
        await dbContextHoolitees.SaveChangesAsync();

        UserSignDto userCreated = ToDto(newUser);

        return Created($"/{newUser.Id}", userCreated);
    }

    [HttpPost("login")]
    public IActionResult Login([FromForm]UserLoginDto userLoginDto)
    {
        //  Ejemplo
        //  var t = dbContextHoolitees.Users.FirstOrDefault(user => user.Email == "");

        foreach (Users userList in dbContextHoolitees.Users.ToList())
        {
            if ( userList.Email == userLoginDto.Email )
            {
                //  Cifar los datos del usuario
                var result = passwordHasher.VerifyHashedPassword(userList.Name, userList.Password, userLoginDto.Password);

                if ( result == PasswordVerificationResult.Success)
                {
                    string rol = "";

                    if (userList.IsAdmin == true)
                    {
                        //string ro = "admin"
                        rol = "admin";
                    }

                    //  Creamos el Token
                    var tokenDescriptor = new SecurityTokenDescriptor
                    {
                        //  Datos para autorizar al usario
                        Claims = new Dictionary<string, object>
                    {
                        {"id", Guid.NewGuid().ToString() },
                        { ClaimTypes.Role, rol  }
                    },
                        //  Caducidad del Token
                        Expires = DateTime.UtcNow.AddDays(5),
                        //  Clave y algoritmo de firmado
                        SigningCredentials = new SigningCredentials(
                            _tokenParameters.IssuerSigningKey,
                            SecurityAlgorithms.HmacSha256Signature)
                    };

                    //  Creamos el token y lo devolvemos al usuario logeado
                    JwtSecurityTokenHandler tokenHandler = new JwtSecurityTokenHandler();
                    SecurityToken token = tokenHandler.CreateToken(tokenDescriptor);
                    string stringToken = tokenHandler.WriteToken(token);

                    return Ok(stringToken);
                }

            }
        }
        return Unauthorized("Usuario no existe");
    }

    private UserSignDto ToDto(Users users)
    {
        return new UserSignDto()
        {
            Name = users.Name,
            Email = users.Email,
            Password = users.Password,
            Address = users.Address
        };
    }

}
