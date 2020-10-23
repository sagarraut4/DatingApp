using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using API.Data;
using API.DTOS;
using API.Entities;
using API.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers
{
    public class AccountController : BaseApiController
    {
        private readonly DataContext _Context;
        private readonly ITokenService _tokenService;
        public AccountController(DataContext Context, ITokenService tokenService)
        {
            _tokenService = tokenService;
            _Context = Context;
        }
        [HttpPost("register")]
        public async Task<ActionResult<UserDto>> Register(RegisterDto registerdto)
        {
            if (await UserExist(registerdto.Username)) return BadRequest("Username is taken");

            using var hmac = new HMACSHA512();
            var user = new AppUser
            {
                userName = registerdto.Username,
                passwordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(registerdto.Password)),
                passwordSalt = hmac.Key
            };
            _Context.Users.Add(user);
            await _Context.SaveChangesAsync();
            return new UserDto
            {
                UserName = user.userName,
                Token =_tokenService.CreateToken(user)
            };
        }
        [HttpPost("login")]
        public async Task<ActionResult<UserDto>> Login(LoginDto logindto)
        {
            var user = await _Context.Users.SingleOrDefaultAsync(x => x.userName == logindto.Username);
            if (user == null) return Unauthorized("user doesn't exist");
            using var hmac = new HMACSHA512(user.passwordSalt);
            var comuptedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(logindto.Password));

            for (int i = 0; i < comuptedHash.Length; i++)
            {
                if (comuptedHash[i] != user.passwordHash[i]) return Unauthorized("invalid password");
            }
              return new UserDto
            {
                UserName = user.userName,
                Token =_tokenService.CreateToken(user)
            };
        }

        private async Task<bool> UserExist(string username)
        {
            return await _Context.Users.AnyAsync(x => x.userName.ToLower() == username.ToLower());
        }
    }
}