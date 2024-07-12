using System.ComponentModel.DataAnnotations;
using DirectorySync.Infrastructure.Exceptions;

namespace DirectorySync.Infrastructure.Logging;

internal static class DataAnnotationsValidator
{
    public static void Validate<T>(T obj) where T : class
    {
        ArgumentNullException.ThrowIfNull(obj);
        var errors = ValidateInternal(obj);
        if (errors.Length != 0)
        {
            var aggregation = errors.Aggregate($"Configuration errors:",
                (acc, curr) => $"{acc}{Environment.NewLine}  - {curr}");
            throw new InvalidConfigurationException(aggregation);
        }
    }

    private static string[] ValidateInternal(object obj)
    {
        var context = new ValidationContext(obj,null, null);
        
        var errorResults = new List<ValidationResult>();
        if (!Validator.TryValidateObject(obj, context, errorResults, true))
        {
            return errorResults.Select(x => x.ErrorMessage)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToArray()!;
        }

        var props = obj.GetType().GetProperties().Where(x => x.PropertyType.IsClass);
        foreach (var info in props)
        {
            var value = info.GetValue(obj);
            if (value is null)
            {
                continue;
            }

            var errors = ValidateInternal(value);
            if (errors.Length != 0)
            {
                return errors;
            }
        }

        return [];
    }
    
    
    // private static string GetPropertyPath<TClass, TProperty>(Expression<Func<TClass, TProperty>> propertySelector, string separator = ":") where TClass : class
    // {
    //     if (propertySelector is null) throw new ArgumentNullException(nameof(propertySelector));
    //     if (separator is null) throw new ArgumentNullException(nameof(separator));
    //     if (propertySelector.Body.NodeType != ExpressionType.MemberAccess) throw new Exception("Invalid property name");
    //
    //     var path = propertySelector.ToString().Split('.').Skip(1) ?? Array.Empty<string>();
    //     return string.Join(separator, path);
    // }
    
}
