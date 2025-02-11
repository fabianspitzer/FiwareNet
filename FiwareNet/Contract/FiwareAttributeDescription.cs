using System;
using System.Reflection;

namespace FiwareNet;

internal class FiwareAttributeDescription
{
    public string PropertyName { get; set; }

    public Type PropertyType { get; set; }

    public string FiwareName { get; set; }

    public string FiwareType { get; set; }

    public PropertyInfo Property { get; set; }

    public bool RawData { get; set; }

    public bool ReadOnly { get; set; }

    public bool SkipEncode { get; set; }
}