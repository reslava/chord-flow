namespace ChordFlow.Domain;

/// <summary>
/// A key-independent scale degree (1 = I, 4 = IV, 5 = V, ...) carrying the chord
/// quality to use at that degree.
/// </summary>
public readonly record struct RomanDegree(int Degree, Quality Quality);
