using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace DistSysAcwServer.Models
{
    /// <summary>
    /// Provides loosely coupled data access for User and Log entities.
    /// All database operations are performed through this class,
    /// keeping controllers decoupled from the data access implementation.
    /// </summary>
    public static class UserDatabaseAccess
    {
        #region User Operations

        /// <summary>
        /// Creates a new user with the given username.
        /// Generates a new GUID as the API Key.
        /// Assigns "Admin" role if this is the first user, otherwise "User".
        /// </summary>
        /// <param name="username">The desired username.</param>
        /// <param name="context">The database context.</param>
        /// <returns>The newly created User object.</returns>
        public static User CreateUser(string username, UserContext context)
        {
            bool isFirstUser = !context.Users.Any();

            User newUser = new User
            {
                ApiKey = Guid.NewGuid().ToString(),
                UserName = username,
                Role = isFirstUser ? "Admin" : "User"
            };

            context.Users.Add(newUser);
            context.SaveChanges();

            return newUser;
        }

        /// <summary>
        /// Checks whether a user with the given API Key exists in the database.
        /// </summary>
        /// <param name="apiKey">The API Key to look up.</param>
        /// <param name="context">The database context.</param>
        /// <returns>True if a user with the given API Key exists; otherwise false.</returns>
        public static bool UserExistsByKey(string apiKey, UserContext context)
        {
            return context.Users.Any(u => u.ApiKey == apiKey);
        }

        /// <summary>
        /// Checks whether a user with the given username exists in the database.
        /// </summary>
        /// <param name="username">The username to look up.</param>
        /// <param name="context">The database context.</param>
        /// <returns>True if a user with the given username exists; otherwise false.</returns>
        public static bool UserExistsByName(string username, UserContext context)
        {
            return context.Users.Any(u => u.UserName == username);
        }

        /// <summary>
        /// Checks whether a user exists whose API Key and UserName both match.
        /// </summary>
        /// <param name="apiKey">The API Key.</param>
        /// <param name="username">The username.</param>
        /// <param name="context">The database context.</param>
        /// <returns>True if a matching user exists; otherwise false.</returns>
        public static bool UserExistsByKeyAndName(string apiKey, string username, UserContext context)
        {
            return context.Users.Any(u => u.ApiKey == apiKey && u.UserName == username);
        }

        /// <summary>
        /// Retrieves a user by their API Key.
        /// </summary>
        /// <param name="apiKey">The API Key to look up.</param>
        /// <param name="context">The database context.</param>
        /// <returns>The User object if found; otherwise null.</returns>
        public static User? GetUserByKey(string apiKey, UserContext context)
        {
            return context.Users.FirstOrDefault(u => u.ApiKey == apiKey);
        }

        /// <summary>
        /// Retrieves a user by their username.
        /// </summary>
        /// <param name="username">The username to look up.</param>
        /// <param name="context">The database context.</param>
        /// <returns>The User object if found; otherwise null.</returns>
        public static User? GetUserByName(string username, UserContext context)
        {
            return context.Users.FirstOrDefault(u => u.UserName == username);
        }

        /// <summary>
        /// Deletes a user from the database.
        /// Archives all of the user's logs into LogArchives before deletion.
        /// Logs in the Logs table will have their UserApiKey set to null (via EF cascade behaviour).
        /// </summary>
        /// <param name="user">The user to delete.</param>
        /// <param name="context">The database context.</param>
        public static void DeleteUser(User user, UserContext context)
        {
            // Archive all logs belonging to this user before deletion
            List<Log> userLogs = context.Logs
                .Where(l => l.UserApiKey == user.ApiKey)
                .ToList();

            foreach (Log log in userLogs)
            {
                context.LogArchives.Add(new LogArchive
                {
                    LogString = log.LogString,
                    LogDateTime = log.LogDateTime,
                    UserApiKey = user.ApiKey
                });
            }

            context.Users.Remove(user);
            context.SaveChanges();
        }

        /// <summary>
        /// Updates the role of the user with the given username.
        /// </summary>
        /// <param name="username">The username whose role should be changed.</param>
        /// <param name="newRole">The new role ("Admin" or "User").</param>
        /// <param name="context">The database context.</param>
        /// <returns>True if the role was updated successfully; otherwise false.</returns>
        public static bool ChangeUserRole(string username, string newRole, UserContext context)
        {
            User? user = GetUserByName(username, context);
            if (user == null)
            {
                return false;
            }

            user.Role = newRole;
            context.SaveChanges();
            return true;
        }

        #endregion

        #region Logging Operations

        /// <summary>
        /// Adds a log entry to the specified user's log collection.
        /// </summary>
        /// <param name="apiKey">The API Key of the user to log against.</param>
        /// <param name="logString">A description of the action performed.</param>
        /// <param name="context">The database context.</param>
        public static void AddLog(string apiKey, string logString, UserContext context)
        {
            User? user = context.Users
                .Include(u => u.Logs)
                .FirstOrDefault(u => u.ApiKey == apiKey);

            if (user != null)
            {
                Log newLog = new Log(logString)
                {
                    UserApiKey = user.ApiKey
                };

                user.Logs.Add(newLog);
                context.SaveChanges();
            }
        }

        #endregion
    }
}