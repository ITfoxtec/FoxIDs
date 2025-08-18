using FoxIDs.Infrastructure.DataAnnotations;
using ITfoxtec.Identity;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace FoxIDs.Models.Api
{
    public class UsersRequest : IValidatableObject  
    {
        [ListLength(Constants.ControlApi.SaveAndDeleteManyMin, Constants.ControlApi.SaveAndDeleteManyMax)]
        public List<CreateUserRequest> Users { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (Users != null && Users.Count(u => !u.Password.IsNullOrWhiteSpace()) > Constants.ControlApi.SaveAndDeleteManyWithPasswordMax)
            {
                yield return new ValidationResult($"A maximum of {Constants.ControlApi.SaveAndDeleteManyWithPasswordMax} users with a password can be uploaded in a single request.", [nameof(Users)]);
            }
        }
    }
}
