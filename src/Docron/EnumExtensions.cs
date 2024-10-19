using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace Docron;

public static class EnumExtensions
{
    public static string GetDisplayName(this Enum enumValue)
    {
        var memberInfo = enumValue.GetType().GetMember(enumValue.ToString());
        var displayAttribute = memberInfo[0].GetCustomAttribute<DisplayAttribute>();
        return displayAttribute?.GetName() ?? enumValue.ToString();
    }
}