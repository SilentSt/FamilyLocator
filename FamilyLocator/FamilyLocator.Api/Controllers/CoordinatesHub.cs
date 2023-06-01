using FamilyLocator.DataLayer.DataBase;
using FamilyLocator.Models.Requests;
using Microsoft.AspNetCore.Authentication.JwtBearer;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

using System.Security.Claims;
using FamilyLocator.Api.UDP;
using FamilyLocator.Models;
using Microsoft.EntityFrameworkCore;
using FamilyLocator.Models.Responses;

namespace FamilyLocator.Api.Controllers
{
    public interface ICoordinates
    {
        Task Coordinates(Coordinates data);
        Task ZoneChanged(NewZone zone);
        Task SOS(string userId);
    }

    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class CoordinatesHub : Hub<ICoordinates>
    {
        private readonly ApiDbContext _dbContext;
        public CoordinatesHub(ApiDbContext dbContext, UdpReciever reciever)
        {
            _dbContext = dbContext;
        }
        
        public override Task OnConnectedAsync()
        {
            Console.WriteLine(Context.UserIdentifier);
            var user = _dbContext.Users.Include(x=>x.Family).FirstOrDefault(x=>x.Id == UserId || x.FireBaseId == UserId);
            if (user == null)
            {
                throw new Exception();
            }
            if(string.IsNullOrWhiteSpace(user.Family?.Id))
            {
                throw new Exception();
            }
            Groups.AddToGroupAsync(Context.ConnectionId, user.Family.Id).Wait();
            return base.OnConnectedAsync();
        }

        public string UserId
        {
            get
            {
                var claimIdentity = Context.User.Identity as ClaimsIdentity;
                var userId = claimIdentity.Claims.First(x => x.Type == "Id").Value;
                return userId;
            }
        }

        public async Task ShareCoordinates(Coordinates data)
        {
            var user = await _dbContext.Users.Include(x => x.Family).FirstOrDefaultAsync(x => x.Id == UserId || x.FireBaseId == UserId);
            await Clients.OthersInGroup(user?.Family?.Id).Coordinates(data);
        }

        

        public async Task SOS( )
        {
            var user = await _dbContext.Users.Include(x=>x.Family).FirstOrDefaultAsync(x=>x.Id == UserId || x.FireBaseId==UserId);
            await Clients.OthersInGroup(user.Family.Id).SOS(user.Id);
        }
    }
}
