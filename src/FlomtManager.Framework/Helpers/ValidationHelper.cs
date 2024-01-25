using System.ComponentModel.DataAnnotations;

namespace FlomtManager.Framework.Helpers
{
    public static class ValidationHelper
    {
        public static IEnumerable<ValidationResult> Validate(object context)
        {
            var results = new List<ValidationResult>();
            Validator.TryValidateObject(context, new ValidationContext(context), results, true);
            return results;
        }
    }
}
