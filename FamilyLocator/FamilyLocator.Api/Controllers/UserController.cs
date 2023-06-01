using AutoMapper;
using FamilyLocator.Api.Service;
using FamilyLocator.DataLayer.DataBase;
using FamilyLocator.DataLayer.DataBase.Entities;
using FamilyLocator.Models.Requests;
using FamilyLocator.Models.Responses;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

using SBEU.Extensions;
using Swashbuckle.AspNetCore.Annotations;
using System.IdentityModel.Tokens.Jwt;

using System.Security.Claims;

using System.Text;

namespace FamilyLocator.Api.Controllers
{
    [Route("[controller]")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class UserController : ControllerExt
    {
        private readonly ApiDbContext _context;
        private readonly IMapper _mapper;
        private readonly UserManager<XIdentityUser> _userManager;

        private readonly JwtConfig _jwtConfig;

        public UserController(ApiDbContext context, IMapper mapper, UserManager<XIdentityUser> userManager, IOptionsMonitor<JwtConfig> optionsMonitor)
        {
            _context = context;
            _mapper = mapper;
            _userManager = userManager;
            _jwtConfig = optionsMonitor.CurrentValue;
        }

        [AllowAnonymous]
        [HttpPost("auth")]
        public async Task<IActionResult> AuthByMail([FromBody] EmailAuthRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null)
            {
                if (request.UserName == null)
                {
                    //return BadRequest("User not found and username not provided");
                }
                user = new XIdentityUser()
                {
                    UserName = request.UserName,
                    Email = request.Email,
                    NormalizedEmail = request.Email.ToUpper(),
                    NormalizedUserName = request.UserName?.ToUpper()
                    
                };
                await _userManager.CreateAsync(user);
            }

            var hash = await user.SendConfirmationEmail(_context);
            return Json(new { Code = hash });
        }
        [AllowAnonymous]
        [HttpPost("confirm")]
        public async Task<IActionResult> ConfirmAuth([FromBody] ConfirmEmailAuthRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            var confirmation = _context.UserConfirmations.Include(x => x.User)
                .FirstOrDefault(x => x.HashCode == request.Code);
            if (confirmation == null)
            {
                return BadRequest("Invalid hash code");
            }

            if (confirmation.MailCode == request.MailCode)
            {
                _context.Remove(confirmation);
                await _context.SaveChangesAsync();
                var jwt = await GenerateJwtToken(confirmation.User);
                return Json(jwt);
            }
            return BadRequest("Invalid mail code");
        }

        [SwaggerResponse(200,"",typeof(UserDto))]
        [HttpGet("me")]
        public async Task<IActionResult> GetMe()
        {
            var user = await _context.Users.Include(x=>x.Image).FirstOrDefaultAsync(x=>x.Id == UserId || x.FireBaseId == UserId);
            if (user == null)
            {
                return NotFound();
            }
            //Console.WriteLine("__");
            var userDto = user.ToDto<UserDto>();
            return Ok(userDto);
        }

        public class ImageDto
        {
            public byte[] image { get; set; }
        }

        [SwaggerResponse(200,"")]
        [HttpPost("image")]
        public async Task<IActionResult> Image(IFormFile image)
        {
            var user = await _context.Users.FirstOrDefaultAsync(x => x.Id == UserId || x.FireBaseId == UserId);
            if (user == null)
            {
                return NotFound();
            }

            var ms = new MemoryStream();
            await image.CopyToAsync(ms);
            var imageb= ms.ToArray();

            user.Image = new UserImage()
            {
                Image = imageb,
            };
            _context.Users.Update(user);
            await _context.SaveChangesAsync();
            return Ok();
        }

        [SwaggerResponse(200,"Добавление токена пуш уведомлений")]
        [HttpPost("push")]
        public async Task<IActionResult> PushToken([FromBody] string token)
        {
            var user = await _context.Users.FirstOrDefaultAsync(x => x.Id == UserId || x.FireBaseId == UserId);
            if (user == null)
            {
                return NotFound();
            }

            var firetoken = new FirePushTokens()
            {
                Token = token
            };
            user.FirePushTokens.Add(firetoken);
            _context.Update(user);
            await _context.SaveChangesAsync();
            return Ok();
        }

        [SwaggerResponse(200,"",typeof(ListCoordinatesDto))]
        [HttpGet("route/{userId}")]
        public async Task<IActionResult> GetRoute([FromQuery] DateTime? dateStart, [FromQuery] DateTime? dateEnd, string userId)
        {
            var coord =
                (await _context.Users.Include(x=>x.Coordinates).FirstOrDefaultAsync(x => x.Id == userId || x.FireBaseId == userId)).Coordinates
                .Where(x => x.Time > (dateStart ?? (DateTime.UtcNow.AddDays(-7))) &&
                            x.Time < (dateEnd ?? DateTime.UtcNow));
            var coordList = coord.ToDto<CoordinatesDto>().GroupBy(x=>x.Time.Date);
            var coordListDto = new ListCoordinatesDto() { Coordinates = coordList };
            return Json(coordListDto);
        }

        [SwaggerResponse(200,"")]
        [HttpPost("delete/userid/fireid")]
        public async Task<IActionResult> Delete(string userid, string fireid)
        {
            var user = await _context.Users.FindAsync(userid);
            if (user.FireBaseId == fireid)
            {
                await _userManager.DeleteAsync(user);
                return Ok();
            }

            return Unauthorized();
        }
        private async Task<AuthResult> GenerateJwtToken(XIdentityUser user)
        {
            var jwtTokenHandler = new JwtSecurityTokenHandler();

            var key = Encoding.ASCII.GetBytes(_jwtConfig.Secret);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim("Id", user.Id),
                    new Claim(JwtRegisteredClaimNames.Email, user.Email),
                    new Claim(JwtRegisteredClaimNames.Sub, user.Email),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
                }),
                Expires = DateTime.UtcNow.Add(_jwtConfig.ExpiryTimeFrame),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = jwtTokenHandler.CreateToken(tokenDescriptor);
            var jwtToken = jwtTokenHandler.WriteToken(token);

            var refreshToken = new RefreshToken()
            {
                JwtId = token.Id,
                IsUsed = false,
                UserId = user.Id,
                AddedDate = DateTime.UtcNow,
                ExpiryDate = DateTime.UtcNow.AddYears(1),
                IsRevoked = false,
                Token = RandomString(25) + Guid.NewGuid()
            };

            await _context.RefreshTokens.AddAsync(refreshToken);
            await _context.SaveChangesAsync();

            return new AuthResult()
            {
                Token = jwtToken,
                Success = true,
                RefreshToken = refreshToken.Token
            };
        }

        private string RandomString(int length)
        {
            var random = new Random();
            var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }
}
