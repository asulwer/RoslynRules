namespace RoslynRules.Models
{
    /// <summary>
    /// Provides localized rule descriptions. Implement this interface to integrate
    /// RoslynRules with your application's localization strategy (resx files, JSON
    /// translation dictionaries, database lookups, etc.).
    /// </summary>
    public interface IRuleDescriptionProvider
    {
        /// <summary>
        /// Resolves a description key to a localized string.
        /// </summary>
        /// <param name="key">The localization key from <see cref="Rule.DescriptionKey"/>.</param>
        /// <param name="culture">Optional culture code (e.g., "en-US", "fr-FR"). Null uses the default culture.</param>
        /// <returns>The localized description, or null if the key is not found.</returns>
        string? GetDescription(string key, string? culture = null);
    }
}
