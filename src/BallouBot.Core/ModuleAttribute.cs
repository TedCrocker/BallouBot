namespace BallouBot.Core;

/// <summary>
/// Marks a class as a BallouBot module for discovery by the module loader.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class BotModuleAttribute : Attribute
{
    /// <summary>
    /// Gets the unique identifier for this module.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets or sets whether this module is enabled by default.
    /// </summary>
    public bool EnabledByDefault { get; set; } = true;

    /// <summary>
    /// Initializes a new instance of the <see cref="BotModuleAttribute"/> class.
    /// </summary>
    /// <param name="id">A unique identifier for the module (e.g., "welcome", "admin").</param>
    public BotModuleAttribute(string id)
    {
        Id = id;
    }
}
