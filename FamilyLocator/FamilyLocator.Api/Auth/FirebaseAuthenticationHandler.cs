using System.Runtime.CompilerServices;
using System.Security.Claims;
using System.Text.Encodings.Web;
using FamilyLocator.DataLayer.DataBase;
using FamilyLocator.DataLayer.DataBase.Entities;
using FirebaseAdmin;
using FirebaseAdmin.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace FamilyLocator.Api.Auth
{
    public class FirebaseAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        private readonly FirebaseApp _firebase;
        private readonly ApiDbContext _context;
        private readonly UserManager<XIdentityUser> _userManager;
        public FirebaseAuthenticationHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock, FirebaseApp firebase, UserManager<XIdentityUser> userManager, ApiDbContext context) : base(options, logger, encoder, clock)
        {
            _firebase = firebase;
            _userManager = userManager;
            _context = context;
        }

        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Context.Request.Headers.ContainsKey("Authorization"))
            {
                return AuthenticateResult.NoResult();
            }

            string bearToken = Context.Request.Headers["Authorization"];
            if (string.IsNullOrEmpty(bearToken) || !bearToken.StartsWith("Bearer "))
            {
                return AuthenticateResult.Fail("Invalid token");
            }

            var token = bearToken.Substring("Bearer ".Length);
            
            try
            {
                var fbtoken = await FirebaseAuth.GetAuth(_firebase).VerifyIdTokenAsync(token);
                
                
                string phone= fbtoken.Claims["user_id"].ToString();
                var user = await _context.Users.FirstOrDefaultAsync(x=>x.FireBaseId == phone);
                if (user == null)
                {
                    user = new XIdentityUser { FireBaseId = phone };
                    await _userManager.CreateAsync(user);
                    //user.FireBaseId = fbtoken.Claims["user_id"].ToString();
                    _context.Add(user);
                    await _context.SaveChangesAsync();
                }
                
                return AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(new List<ClaimsIdentity>()
                {
                    new (ToClaims(fbtoken.Claims),nameof(FirebaseAuthenticationHandler))
                }), JwtBearerDefaults.AuthenticationScheme));
            }
            catch
            {
                return AuthenticateResult.Fail("");
            }
        }

        private IEnumerable<Claim> ToClaims(IReadOnlyDictionary<string, object> fbtokenClaims)
        {
            return new List<Claim>
            {
                new ("id", fbtokenClaims["user_id"].ToString()),
                new ("phone_number", fbtokenClaims["phone_number"].ToString()),
                //new Claim("name", fbtokenClaims["key"].ToString())
            };
        }
    }
}
