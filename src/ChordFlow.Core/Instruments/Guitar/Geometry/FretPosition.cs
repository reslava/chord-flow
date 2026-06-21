
namespace ChordFlow.Instruments.Guitar;

/// <summary>
/// A single fretted note: <paramref name="String"/> uses alphaTab numbering
/// (1 = high E ... 6 = low E), <paramref name="Fret"/> is the fret number (0 = open).
/// </summary>
public readonly record struct FretPosition(int String, int Fret);
