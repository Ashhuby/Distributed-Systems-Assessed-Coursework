using DistSysAcwServer.Models;
using DistSysAcwServer.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DistSysAcwServer.Controllers
{
    /// <summary>
    /// Handles user management requests: registration, lookup, deletion, and role changes.
    /// </summary>
    public class UserController : BaseController
    {
        /// <summary>
        /// Constructs a User controller with dependency-injected services.
        /// </summary>
        /// <param name="dbcontext">The Entity Framework database context.</param>
        /// <param name="error">The shared error object for the request pipeline.</param>
        public UserController(UserContext dbcontext, SharedError error)
            : base(dbcontext, error) { }

        #region Task4

        /// <summary>
        /// GET api/user/new?username=UserOne
        /// Checks whether a user with the given username already exists in the database.
        /// Returns a descriptive string indicating whether the user exists.
        /// </summary>
        /// <param name="username">The username to look up.</param>
        /// <returns>A string indicating whether the user exists, with status 200.</returns>
        [HttpGet]
        [ActionName("New")]
        public IActionResult GetNew([FromQuery] string username)
        {
            if (!string.IsNullOrEmpty(username) && UserDatabaseAccess.UserExistsByName(username, DbContext))
            {
                return Ok("True - User Does Exist! Did you mean to do a POST to create a new user?");
            }

            return Ok("False - User Does Not Exist! Did you mean to do a POST to create a new user?");
        }

        /// <summary>
        /// POST api/user/new
        /// Creates a new user with the given username (sent as a JSON string in the body).
        /// Generates a GUID API Key. First user receives Admin role; all others receive User role.
        /// Content-Type must be application/json.
        /// </summary>
        /// <param name="username">
        /// The desired username, sent as a raw JSON string in the request body
        /// (e.g. "UserOne" with quotes, not a JSON object).
        /// </param>
        /// <returns>The generated API Key string with status 200, or an error message.</returns>
        [HttpPost]
        [ActionName("New")]
        public IActionResult PostNew([FromBody] string? username)
        {
            // Validate that a username was provided
            if (string.IsNullOrEmpty(username))
            {
                return BadRequest("Oops. Make sure your body contains a string with your username and your Content-Type is Content-Type:application/json");
            }

            // Check if the username is already taken
            if (UserDatabaseAccess.UserExistsByName(username, DbContext))
            {
                return StatusCode(403, "Oops. This username is already in use. Please try again with a new username.");
            }

            // Create the new user and return their API Key
            User newUser = UserDatabaseAccess.CreateUser(username, DbContext);
            return Ok(newUser.ApiKey);
        }

        #endregion

        #region Task7

        /// <summary>
        /// DELETE api/user/removeuser?username=UserOne
        /// Deletes a user from the database. Requires a valid API Key in the header.
        /// A user can delete themselves. An Admin can delete any user.
        /// </summary>
        /// <param name="username">The username of the user to delete, from the query string.</param>
        /// <returns>True if the user was deleted; otherwise false. Always returns status 200.</returns>
        [HttpDelete]
        [Authorize(Roles = "Admin,User")]
        public IActionResult RemoveUser([FromQuery] string username)
        {
            // Extract the API Key from the request header
            string? apiKey = Request.Headers["ApiKey"].FirstOrDefault();
            if (string.IsNullOrEmpty(apiKey))
            {
                return Ok(false);
            }

            // Get the calling user
            User? callingUser = UserDatabaseAccess.GetUserByKey(apiKey, DbContext);
            if (callingUser == null)
            {
                return Ok(false);
            }

            UserDatabaseAccess.AddLog(apiKey, "User requested /User/RemoveUser", DbContext);

            // Get the target user to be deleted
            User? targetUser = UserDatabaseAccess.GetUserByName(username, DbContext);
            if (targetUser == null)
            {
                return Ok(false);
            }

            // Check permission: user can delete themselves, Admin can delete anyone
            bool isSelf = callingUser.ApiKey == targetUser.ApiKey;
            bool isAdmin = callingUser.Role == "Admin";

            if (!isSelf && !isAdmin)
            {
                return Ok(false);
            }

            // Delete the user (logs are archived inside this method)
            UserDatabaseAccess.DeleteUser(targetUser, DbContext);
            return Ok(true);
        }

        #endregion

        #region Task8

        /// <summary>
        /// POST api/user/changerole
        /// Changes the role of a specified user. Only accessible by Admin users.
        /// Body must contain a JSON object: { "username": "UserOne", "role": "Admin" }
        /// </summary>
        /// <param name="request">The ChangeRoleRequest containing the target username and new role.</param>
        /// <returns>A status string indicating the result.</returns>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public IActionResult ChangeRole([FromBody] ChangeRoleRequest request)
        {
            try
            {
                // Validate the request body was provided
                if (request == null || string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.Role))
                {
                    return BadRequest("NOT DONE: An error occured");
                }

                // Validate the role is either "User" or "Admin"
                if (request.Role != "User" && request.Role != "Admin")
                {
                    return BadRequest("NOT DONE: Role does not exist");
                }

                // Validate the username exists
                if (!UserDatabaseAccess.UserExistsByName(request.Username, DbContext))
                {
                    return BadRequest("NOT DONE: Username does not exist");
                }
                
                // Log it duh
                string? apiKey = Request.Headers["ApiKey"].FirstOrDefault();
                if (!string.IsNullOrEmpty(apiKey))
                {
                    UserDatabaseAccess.AddLog(apiKey, "User requested /User/ChangeRole", DbContext);
                }

                // Update the role
                bool success = UserDatabaseAccess.ChangeUserRole(request.Username, request.Role, DbContext);
                if (success)
                {
                    return Ok("DONE");
                }

                return BadRequest("NOT DONE: An error occured");
            }
            catch
            {
                return BadRequest("NOT DONE: An error occured");
            }
        }

        #endregion
    }
}