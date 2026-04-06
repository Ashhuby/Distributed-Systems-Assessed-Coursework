using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DistSysAcwServer.Models
{
    /// <summary>
    /// Represents a registered user in the system.
    /// Entity Framework Code First model mapped to the Users table.
    /// </summary>
    public class User
    {
        /// <summary>
        /// The user's API Key. Acts as the unique primary key in the database.
        /// Generated as a GUID string when the user is created.
        /// </summary>
        [Key]
        public string ApiKey { get; set; }

        /// <summary>
        /// The user's chosen username. Must be unique.
        /// </summary>
        public string UserName { get; set; }

        /// <summary>
        /// The user's role. Either "Admin" or "User".
        /// The first registered user receives "Admin"; all subsequent users receive "User".
        /// </summary>
        public string Role { get; set; }

        /// <summary>
        /// Navigation property for the user's log entries.
        /// Virtual to enable lazy loading if configured.
        /// </summary>
        public virtual ICollection<Log> Logs { get; set; }

        /// <summary>
        /// Parameterless constructor required by Entity Framework.
        /// </summary>
        public User()
        {
            Logs = new List<Log>();
        }
    }
}