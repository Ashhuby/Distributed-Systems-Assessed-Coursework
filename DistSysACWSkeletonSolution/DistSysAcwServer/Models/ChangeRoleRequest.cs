namespace DistSysAcwServer.Models
{
    /// <summary>
    /// Data transfer object for the User/ChangeRole POST request body.
    /// Expected JSON format: { "username": "UserOne", "role": "Admin" }
    /// </summary>
    public class ChangeRoleRequest
    {
        /// <summary>
        /// The username of the user whose role should be changed.
        /// </summary>
        public required string Username { get; set; }

        /// <summary>
        /// The new role to assign. Must be either "Admin" or "User".
        /// </summary>
        public required string Role { get; set; }
    }
}