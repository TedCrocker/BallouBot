namespace BallouBot.Core.Entities;

/// <summary>
/// Defines the type of list entry for the Random Richard module.
/// </summary>
public enum RichardListType
{
    /// <summary>
    /// User is whitelisted to receive Random Richard DMs.
    /// </summary>
    Whitelist = 0,

    /// <summary>
    /// User is blacklisted from receiving Random Richard DMs.
    /// </summary>
    Blacklist = 1
}
