using Pelican_Keeper;

namespace Pelican_Keeper_Unit_Testing;

public class TestingHelperClass
{
    public static IEnumerable<TestCaseData> NullPropertyCases<T>(
        Func<T> createValidObject)
        where T : class
    {
        var properties = typeof(T)
            .GetProperties()
            .Where(p =>
                p.CanWrite &&
                (!p.PropertyType.IsValueType ||
                 Nullable.GetUnderlyingType(p.PropertyType) != null));

        foreach (var property in properties)
        {
            var obj = createValidObject();

            property.SetValue(obj, null);

            yield return new TestCaseData(obj)
                .SetName($"{typeof(T).Name}_{property.Name}_Null");
        }
    }
    
    public static IEnumerable<TestCaseData> InvalidEnumJsonCases()
    {
        var enumProperties = typeof(TemplateClasses.Config)
            .GetProperties()
            .Where(p => p.PropertyType.IsEnum);

        foreach (var property in enumProperties)
        {
            yield return new TestCaseData(property.Name, "BadEnumValue")
                .SetName($"{property.Name}_InvalidEnumString");
        }
    }
}