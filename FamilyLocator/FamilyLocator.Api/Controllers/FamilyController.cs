using AutoMapper;

using FamilyLocator.Api.Service;
using FamilyLocator.DataLayer.DataBase;
using FamilyLocator.DataLayer.DataBase.Entities;
using FamilyLocator.Models;
using FamilyLocator.Models.Requests;
using FamilyLocator.Models.Responses;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using SBEU.Extensions;
using Swashbuckle.AspNetCore.Annotations;

namespace FamilyLocator.Api.Controllers
{
    [Route("[controller]")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class FamilyController : ControllerExt
    {
        private readonly ApiDbContext _context;
        //private readonly IHubContext<CoordinatesHub, ICoordinates> _coordinateHub;
        private readonly IMapper _mapper;
        public FamilyController(ApiDbContext context,/* IHubContext<CoordinatesHub,ICoordinates> coordinateHub,*/ IMapper mapper)
        {
            _context = context;
            //_coordinateHub = coordinateHub;
            _mapper = mapper;
        }

        [SwaggerResponse(200,"")]
        [HttpPost]
        public async Task<IActionResult> Create()
        {

            var user = await _context.Users.FirstOrDefaultAsync(x=>x.Id==UserId || x.FireBaseId == UserId);
            if (user == null)
            {
                return NotFound(typeof(XIdentityUser));
            }

            user.InFamilyRole = InFamilyRole.Owner;
            var fam = new Family()
            {
                Id = Guid.NewGuid().ToString(),
                SignInCode = Encryptor.GenerateEncryptedString(),
                Users = new List<XIdentityUser>() { user }
            };
            fam.Name = fam.Id;
            _context.Families.Add(fam);

            await _context.SaveChangesAsync();
            return Ok();
        }

        [SwaggerResponse(200, "")]
        [HttpPost("join/{code}")]
        public async Task<IActionResult> Join(string code)
        {
            var user = await _context.Users.FindAsync(UserId);
            if (user == null)
            {
                return NotFound();
            }
            var fam = _context.Families.Include(x=>x.Users).FirstOrDefault(x => x.SignInCode == code);
            if (fam is { })
            {
                user.InFamilyRole = InFamilyRole.User;
                fam.Users.Add(user);
                _context.Families.Update(fam);
            }
            else
            {
                return Unauthorized();
            }

            await _context.SaveChangesAsync();
            return Ok();
        }

        [SwaggerResponse(200,"",typeof(FamilyDto))]
        [HttpGet("myfamily")]
        public async Task<IActionResult> MyFamily()
        {
            var user = await _context.Users.Include(x=>x.Family).FirstOrDefaultAsync(x=>x.Id==UserId || x.FireBaseId == UserId);
            if (user?.Family == null)
            {
                return NotFound();
            }
            var family = _context.Families.Include(x=>x.Users).FirstOrDefault(x=>x.Id==user.Family.Id);
            var familyDto = family.ToDto<FamilyDto>();
            foreach (var mUserDto in familyDto.Users)
            {
                 var tup =  _context.Users.Where(x => x.Id == mUserDto.Id)
                    .SelectMany(x => x.Coordinates).OrderByDescending(x => x.Time)
                    .Select(x =>new{x.Latitude, x.Longitude,x.Battery}).FirstOrDefault();
                 (mUserDto.Latitude, mUserDto.Longitude, mUserDto.Battery) = (tup?.Latitude??0, tup?.Longitude??0,tup?.Battery??100);
            }
            return Json(familyDto);
        }


        [SwaggerResponse(200, "")]
        [HttpPost("zone")]
        public async Task<IActionResult> Zone([FromBody] ZoneDto zone)
        {
            var fam = _context.Families.FirstOrDefault(x => x.Users.Any(s => s.Id == UserId || s.FireBaseId == UserId));
            if (fam == null)
            {
                return NotFound();
            }
            var fZone = new Zone()
            {
                Points = zone.Points,
                Family = fam,
                ZoneType = zone.ZoneType
            };
            _context.Add(fZone);
            await _context.SaveChangesAsync();
            return Ok();
        }

        [HttpDelete("zone/{id}")]
        public async Task<IActionResult> DeleteZone(int id)
        {
            var fam = _context.Families.Include(x=>x.Zones).FirstOrDefault(x => x.Users.Any(s => s.Id == UserId || s.FireBaseId == UserId));
            if (fam == null)
            {
                return NotFound();
            }

            if (fam.Zones?.Count > 0 && fam.Zones.Any(x=>x.Id==id))
            {
                fam.Zones.Remove(fam.Zones.First(x=>x.Id==id));
                return Ok();
            }
            return NotFound();
        }

        [SwaggerResponse(200,"",typeof(IEnumerable<IdZoneDto>))]
        [HttpGet("zone")]
        public async Task<IActionResult> GetZones()
        {
            var fam = _context.Families.Include(x=>x.Zones).FirstOrDefault(x => x.Users.Any(s => s.Id == UserId || s.FireBaseId == UserId));
            var zonesDto = fam.Zones.Select(x=>x.ToDto<ZoneDto>());
            return Json(zonesDto);
        }
    }
}
