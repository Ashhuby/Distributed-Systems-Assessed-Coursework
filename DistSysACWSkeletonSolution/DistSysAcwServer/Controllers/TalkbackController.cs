using System.Collections.Generic;
using DistSysAcwServer.Middleware;
using DistSysAcwServer.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace DistSysAcwServer.Controllers
{
    /// <summary>
    /// Handles simple talkback requests that do not require authentication.
    /// Provides Hello and Sort endpoints as per the specification.
    /// </summary>
    public class TalkbackController : BaseController
    {
        /// <summary>
        /// Constructs a TalkBack controller with dependency-injected services.
        /// </summary>
        /// <param name="dbcontext">The Entity Framework database context.</param>
        /// <param name="error">The shared error object for the request pipeline.</param>
        public TalkbackController(Models.UserContext dbcontext, SharedError error)
            : base(dbcontext, error) { }

        /// <summary>
        /// GET api/talkback/hello
        /// Returns "Hello World" with a 200 OK status code.
        /// </summary>
        [HttpGet]
        public IActionResult Hello()
        {
            return Ok("Hello World");
        }

        /// <summary>
        /// GET api/talkback/sort?integers=8&amp;integers=2&amp;integers=5
        /// Accepts an array of integers from the query string, sorts them
        /// in ascending order, and returns the sorted array as JSON.
        /// Returns an empty array [] if no integers are provided.
        /// Returns 400 Bad Request if invalid (non-integer) values are submitted.
        /// </summary>
        /// <param name="integers">An array of integers from the query string.</param>
        [HttpGet]
        public IActionResult Sort([FromQuery] int[] integers)
        {
            if (integers == null || integers.Length == 0)
            {
                return Ok(Array.Empty<int>());
            }

            int[] sorted = integers.OrderBy(i => i).ToArray();
            return Ok(sorted);
        }

        [HttpGet]
        [Authorize(Roles = "Admin,User")]
        public IActionResult Debug()
        {
            var claims = User.Claims.Select(c => $"{c.Type}: {c.Value}").ToList();
            return Ok(new { Claims = claims, IsAuth = User.Identity?.IsAuthenticated });
        }
    }
}