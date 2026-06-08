namespace ChordFlow.Domain;

/// <summary>
/// How strongly a lead <see cref="TargetZone"/> should be emphasised. Guide tones (3 &amp; 7) are
/// <see cref="Primary"/> sweet spots; other chord/scale tones are <see cref="Secondary"/>.
/// </summary>
public enum Importance
{
    Primary,
    Secondary,
}
