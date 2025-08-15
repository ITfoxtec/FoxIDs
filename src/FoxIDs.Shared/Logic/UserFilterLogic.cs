using FoxIDs.Models;
using System;
using System.Linq.Expressions;

namespace FoxIDs.Logic
{
    public static class UserFilterLogic
    {
        private const string UserDataType = Constants.Models.DataType.User;

        /// <summary>
        /// Creates a filter expression for users based on the provided filter criteria.
        /// This logic matches the filtering implementation in TUsersController.GetUsers method.
        /// </summary>
        /// <param name="filterEmail">Filter by email (case insensitive partial match)</param>
        /// <param name="filterPhone">Filter by phone (partial match)</param>
        /// <param name="filterUsername">Filter by username (case insensitive partial match)</param>
        /// <param name="filterUserId">Filter by user ID (case insensitive partial match)</param>
        /// <returns>Expression to filter users</returns>
        public static Expression<Func<User, bool>> CreateUserFilterExpression(
            string filterEmail = null, 
            string filterPhone = null, 
            string filterUsername = null, 
            string filterUserId = null)
        {
            var queryFilters = !string.IsNullOrWhiteSpace(filterEmail) || 
                              !string.IsNullOrWhiteSpace(filterPhone) || 
                              !string.IsNullOrWhiteSpace(filterUsername) || 
                              !string.IsNullOrWhiteSpace(filterUserId);

            Expression<Func<User, bool>> whereQuery = u => !queryFilters ? u.DataType.Equals(UserDataType) :
                u.DataType.Equals(UserDataType) && (
                    (!string.IsNullOrWhiteSpace(filterEmail) && u.Email.Contains(filterEmail, StringComparison.CurrentCultureIgnoreCase)) ||
                    (!string.IsNullOrWhiteSpace(filterPhone) && u.Phone.Contains(filterPhone, StringComparison.CurrentCultureIgnoreCase)) ||
                    (!string.IsNullOrWhiteSpace(filterUsername) && u.Username.Contains(filterUsername, StringComparison.CurrentCultureIgnoreCase)) ||
                    (!string.IsNullOrWhiteSpace(filterUserId) && u.UserId.Contains(filterUserId, StringComparison.CurrentCultureIgnoreCase))
                );

            return whereQuery;
        }
    }
}
