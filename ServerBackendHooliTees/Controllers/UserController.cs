﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServerBackendHooliTees.Models.Database;
using ServerBackendHooliTees.Models.Database.Entities;
using ServerBackendHooliTees.Models.Dtos;
using System.IO;
using System.Security.Claims;

namespace ServerBackendHooliTees.Controllers;

[Route("api/[controller]")]
[ApiController]
public class UserController : ControllerBase
{

    private MyDbContext dbContextHoolitees;

    public UserController(MyDbContext dbContext)
    {
        dbContextHoolitees = dbContext;
    }


    [HttpGet("userlist")]
    public IEnumerable<UserSignDto> GetUser()
    {
        return dbContextHoolitees.Users.Select(ToDto);
    }


    [HttpPost("signup")]
    public async Task<IActionResult> Post([FromForm] UserSignDto userSignDto)
    {

        Users newUser = new Users()
        {
            Name = userSignDto.Name,
            Email = userSignDto.Email,
            Password = userSignDto.Password,
            Address = userSignDto.Address
        };

        await dbContextHoolitees.Users.AddAsync(newUser);
        await dbContextHoolitees.SaveChangesAsync();

        UserSignDto userCreated = ToDto(newUser);

        return Created($"/{newUser.Id}", userCreated);
    }

    [HttpPost("login")]
    public async Task<Boolean> Post([FromForm]UserLoginDto userLoginDto)
    {

        List<Users> users = new List<Users>();

        foreach (Users userList in users)
        {
            Console.WriteLine("Hola");
            Console.WriteLine(userList);
        }

        //Console.WriteLine(users);


        if (userLoginDto.Email == "ejemplo@gmail")
        {
            return true;
        }

        return false;
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
