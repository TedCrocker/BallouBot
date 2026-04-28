namespace BallouBot.Core;

/// <summary>
/// Provides a mechanism for modules to report errors that will be forwarded
/// as DMs to subscribed administrators.
/// </summary>
public interface IErrorNotificationService
{
    /// <summary>
    /// Notifies all subscribed users about an error.
    /// </summary>
    /// <param name="source">The source/module where the error occurred.</param>
    /// <param name="message">A description of the error.</param>
    /// <param name="exception">The exception that occurred, if any.</param>
    /// <param name="guildId">The guild where the error occurred, if applicable. If null, all subscribers are notified.</param>
    Task NotifyErrorAsync(string source, string message, Exception? exception = null, ulong? guildId = null);
}
