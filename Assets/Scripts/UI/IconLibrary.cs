using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Loads upgrade/stat icons by name from <c>Assets/Resources/Icons/</c>.
///
/// To add icons: drop PNG files into that folder named after the upgrade, e.g.
/// <c>MoveSpeed.png</c>, <c>MaxHealth.png</c>, <c>SwordDamage.png</c>. The names
/// must match the UpgradeType enum / shop keys (see README in that folder).
/// If an icon is missing, the UI simply shows no icon - nothing breaks.
/// </summary>
public static class IconLibrary
{
    private static readonly Dictionary<string, Sprite> _cache = new Dictionary<string, Sprite>();

    /// <summary>Returns the icon sprite for a key (e.g. "MoveSpeed"), or null if none exists.</summary>
    public static Sprite Get(string key)
    {
        if (string.IsNullOrEmpty(key)) return null;

        if (_cache.TryGetValue(key, out Sprite cached))
            return cached;

        // Resources.Load looks under any folder named "Resources"; no extension.
        Sprite sprite = Resources.Load<Sprite>("Icons/" + key);
        _cache[key] = sprite; // cache even null so we don't retry every frame
        return sprite;
    }
}
