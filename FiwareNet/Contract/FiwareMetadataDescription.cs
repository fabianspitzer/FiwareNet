using System;
using System.Reflection;

namespace FiwareNet;

internal class FiwareMetadataDescription
{
    public string JsonPropertyName { get; set; }

    public Type PropertyType { get; set; }

    public string FiwareName { get; set; }

    public PropertyInfo Property { get; set; }
}