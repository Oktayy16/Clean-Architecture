﻿using Core.Application.Contracts.Permissions;
using Core.Domain.Identity.Constants;
using Core.Domain.Identity.Entities;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

namespace IntegrationTest.Mvc
{
    public class LocalAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {

        private readonly ApplicationUser _appUser = DefaultApplicationUsers.GetSuperUser();

        public LocalAuthenticationHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger,
            UrlEncoder encoder, ISystemClock clock) : base(options, logger, encoder, clock)
        {
        }

        private List<Claim> UserClaims()
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, _appUser.Id),
                new(ClaimTypes.Name, _appUser.UserName)
            };
            return claims;
        }
        private List<Claim> AllRolesClaims()
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.Role, DefaultApplicationRoles.SuperAdmin),
                new(ClaimTypes.Role, DefaultApplicationRoles.Admin),
                new(ClaimTypes.Role, DefaultApplicationRoles.Moderator),
                new(ClaimTypes.Role, DefaultApplicationRoles.Basic)
            };
            return claims;
        }

        private List<Claim> AllPermissionsClaims()
        {
            var allPermissions = PermissionHelper.GetAllPermissions();
            var newClaims = allPermissions.Select(x => new Claim(CustomClaimTypes.Permission, x.Value)).ToList();
            return newClaims;
        }
 
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {

            var allClaims = new List<Claim>();
            allClaims.AddRange(UserClaims());
            allClaims.AddRange(AllRolesClaims());
            allClaims.AddRange(AllPermissionsClaims());
            var authenticationTicket = new AuthenticationTicket(
                new ClaimsPrincipal(new ClaimsIdentity(allClaims, IdentityConstants.ApplicationScheme)),
                new AuthenticationProperties(),
                IdentityConstants.ApplicationScheme);
            return Task.FromResult(AuthenticateResult.Success(authenticationTicket));
        }
    }
}
