using System;

namespace FiwareNet;

/// <summary>
/// An attribute to specify that a class property should be ignored for FIWARE entities.
/// </summary>
/// <remarks>
/// Specifies that this class property should be ignored for FIWARE entities.
/// </remarks>
[AttributeUsage(AttributeTargets.Property)]
public sealed class FiwareIgnoreAttribute : Attribute;