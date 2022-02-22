using System.Security.Cryptography;
using System.Text;
using API.Data;
using API.Entities;
using API.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using API.Interfaces;

namespace API.Controllers
{
  public class AccountController : BaseApiController
  {
    private readonly DataContext _context;
    private readonly ITokenService _tokenService;
    public AccountController(DataContext context, ITokenService tokenService)
    {
      _tokenService = tokenService;
      _context = context;
    }
    
    [HttpPost("register")]

    public async Task<ActionResult<UserDto>> Register(RegisterDto registerDto)
    {
        // Check if user exists
        if (await UserExists(registerDto.Username)) return BadRequest("Username is taken");
      
        // Using statement ensure that resource is disposed of correctly once its done
        // Implements IDisposable interface - must implement 'dispose' method
        // Provides algorithm for hashing
        using var hmac = new HMACSHA512();

        // Initialise new AppUser with properties
        var user = new AppUser
        {
            UserName = registerDto.Username.ToLower(),

            // Encode password string to byte array
            PasswordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(registerDto.Password)),

            // Pass the randomly generated key from the hash class to the password salt 
            PasswordSalt = hmac.Key
        };

        // New user will be tracked by EF until SaveChanges method is called.  
        _context.Users.Add(user);

        // Save user to the db
        await _context.SaveChangesAsync();

        return new UserDto
        {
          Username = user.UserName,
          Token = _tokenService.CreateToken(user)
        };
    }

    [HttpPost("login")]
    public async Task<ActionResult<UserDto>> Login(LoginDto loginDto)
    {
      // check if user exists in the db
      var user = await _context.Users.SingleOrDefaultAsync(x => x.UserName == loginDto.Username);

      // if no user found return 401 response
      if (user == null) return Unauthorized("Invalid username");

      // create new instance of hmac and pass secret key from db
      using var hmac = new HMACSHA512(user.PasswordSalt);

      // calculate the hash from the password 
      var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(loginDto.Password));

      // compare password hash to the hash in the db
      for (int i = 0; i < computedHash.Length; i++)
      {
        // if hash doesnt match return 401 response
        if (computedHash[i] != user.PasswordHash[i]) return Unauthorized("Invalid password");
      }

      // else return the user
      return new UserDto
        {
          Username = user.UserName,
          Token = _tokenService.CreateToken(user)
        };
    }

    // Helper method to ensure uniqueness of usernames
    private async Task<bool> UserExists(string username)
    {
      // Check if there a user exists with the samer username
      return await _context.Users.AnyAsync(x => x.UserName == username.ToLower());
    }
  }
}