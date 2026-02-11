

namespace DrawProject.Attributes;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class InspectableAttribute : Attribute
{
    public string DisplayName { get; set; }
    public string Description { get; set; }

    public InspectableAttribute(string displayName = null)
    {
        DisplayName = displayName;
    }
}