using System.Text.RegularExpressions;
using Newtonsoft.Json;

namespace FiwareNet;

/// <summary>
/// A class holding information to filter entities by ID and/or type.
/// </summary>
[JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
public class EntityFilter
{
    #region constructors
    /// <summary>
    /// Initializes a new instance of the <see cref="EntityFilter"/> class.
    /// </summary>
    public EntityFilter() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="EntityFilter"/> class.
    /// </summary>
    /// <param name="id">The ID of the entity.</param>
    public EntityFilter(string id) => Id = id;

    /// <summary>
    /// Initializes a new instance of the <see cref="EntityFilter"/> class.
    /// </summary>
    /// <param name="id">The ID of the entity.</param>
    /// <param name="type">The type of the entity.</param>
    public EntityFilter(string id, string type)
    {
        Id = id;
        Type = type;
    }
    #endregion

    #region public properties
    /// <summary>
    /// Gets or sets an exact string for matching an entity ID.
    /// Only one of <see cref="Id"/> or <see cref="IdPattern"/> may be used.
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// Gets or sets a <see cref="Regex"/> pattern to use for matching entity IDs.
    /// Only one of <see cref="Id"/> or <see cref="IdPattern"/> may be used.
    /// </summary>
    public Regex IdPattern { get; set; }

    /// <summary>
    /// Gets or sets an exact string for matching an entity type.
    /// Only one of <see cref="Type"/> or <see cref="TypePattern"/> may be used.
    /// </summary>
    public string Type { get; set; }

    /// <summary>
    /// Gets or sets a <see cref="Regex"/> pattern to use for matching entity types.
    /// Only one of <see cref="Type"/> or <see cref="TypePattern"/> may be used.
    /// </summary>
    public Regex TypePattern { get; set; }
    #endregion

    #region operator overloads
    /// <summary>
    /// Converts <see cref="EntityBase"/> object to a new <see cref="EntityFilter"/> instance.
    /// </summary>
    /// <param name="entity">The <see cref="EntityBase"/> object to convert.</param>
    public static implicit operator EntityFilter(EntityBase entity) => new(entity.Id, entity.Type);
    #endregion
}