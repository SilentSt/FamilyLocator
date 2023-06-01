using Microsoft.AspNetCore.Mvc;

using System.Security.Claims;
using FamilyLocator.DataLayer.DataBase.Entities;

namespace FamilyLocator.Api.Service
{
    public class ControllerExt : Controller
    {
        public string? UserId
        {
            get
            {
                var claimIdentity = this.User.Identity as ClaimsIdentity;
                var userId = claimIdentity?.Claims?.First(x => x.Type == "Id")?.Value;
                return userId;
            }
        }
    }
}
