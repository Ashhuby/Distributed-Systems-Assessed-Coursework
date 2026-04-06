using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using DistSysAcwServer.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Http;

namespace DistSysAcwServer.Auth
{
    /// <summary>
    /// Custom authorization handler that checks whether the authenticated user
    /// has the required role to access the requested action.
    /// Works in conjunction with [Authorize(Roles = "...")] attributes on controllers/actions.
    /// </summary>
    public class CustomAuthorizationHandlerMiddleware
        : AuthorizationHandler<RolesAuthorizationRequirement>, IAuthorizationHandler
    {
        private IHttpContextAccessor HttpContextAccessor { get; set; }
        private SharedError Error { get; set; }

        public CustomAuthorizationHandlerMiddleware(
            IHttpContextAccessor httpContextAccessor,
            SharedError error)
        {
            HttpContextAccessor = httpContextAccessor;
            Error = error;
        }

        /// <summary>
        /// Evaluates whether the current user meets the role-based authorization requirement.
        /// If the action requires Admin role only and the user does not have it,
        /// sets a 403 Forbidden with the message "Forbidden. Admin access only."
        /// Does not call context.Fail() to avoid blocking other handlers from succeeding.
        /// </summary>
        /// <param name="context">The authorization context containing the user's claims.</param>
        /// <param name="requirement">The role requirement from the [Authorize] attribute.</param>
        protected override Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            RolesAuthorizationRequirement requirement)
        {
            // Check if the user is authenticated
            if (context.User == null || context.User.Identity == null || !context.User.Identity.IsAuthenticated)
            {
                return Task.CompletedTask;
            }

            // Get the user's role from their claims
            string? userRole = context.User.FindFirst(ClaimTypes.Role)?.Value;

            if (string.IsNullOrEmpty(userRole))
            {
                return Task.CompletedTask;
            }

            // Check if the user's role is in the list of allowed roles
            bool isAllowed = requirement.AllowedRoles
                .Any(r => r.Split(',').Select(s => s.Trim()).Contains(userRole));

            if (isAllowed)
            {
                context.Succeed(requirement);
                return Task.CompletedTask;
            }

            // If Admin-only and user is not Admin, set 403 message
            bool adminOnly = requirement.AllowedRoles.All(r =>
                r.Split(',').Select(s => s.Trim()).All(s => s == "Admin"));

            if (adminOnly)
            {
                Error.StatusCode = StatusCodes.Status403Forbidden;
                Error.Message = "Forbidden. Admin access only.";
            }

            // Don't call context.Fail() — just return without succeeding.
            // This allows the built-in handler to also evaluate and
            // prevents a premature failure from blocking authorization.
            return Task.CompletedTask;
        }
    }
}