using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using FiwareNet.Converters;

namespace FiwareNet;

/// <summary>
/// A class containing information about an Orion context broker.
/// </summary>
[JsonConverter(typeof(OrionInfoConverter))]
public class OrionInfo
{
    /// <summary>
    /// Gets the current version of the Orion context broker.
    /// </summary>
    public Version Version { get; set; }

    /// <summary>
    /// Gets the current uptime of the Orion context broker.
    /// </summary>
    public TimeSpan Uptime { get; set; }

    /// <summary>
    /// Gets the hash of the Git commit that was used to build this version.
    /// </summary>
    public string GitHash { get; set; }

    /// <summary>
    /// Gets the date/time when this version was compiled.
    /// </summary>
    public DateTime CompileTime { get; set; }

    /// <summary>
    /// Gets the user who compiled this version.
    /// </summary>
    public string CompiledBy { get; set; }

    /// <summary>
    /// Gets the hash of the Git branch that was used to build this version.
    /// </summary>
    public string CompiledIn { get; set; }

    /// <summary>
    /// Gets the date/time when this version was released.
    /// </summary>
    public DateTime ReleaseDate { get; set; }

    /// <summary>
    /// Gets the machine architecture specifier for this build.
    /// </summary>
    public string Machine { get; set; }

    /// <summary>
    /// Gets a URL to the API documentation for this version.
    /// </summary>
    public string Doc { get; set; }

    /// <summary>
    /// Gets a list of software versions of 3rd-party libraries.
    /// </summary>
    public IDictionary<string, string> LibraryVersions { get; set; }
}