using System.Globalization;

namespace ChordFlow.Music.Progressions.Transforms;

/// <summary>
/// The single registry that maps a transform name + raw argument string (the inside of a Song-DSL
/// <c>@name(args)</c> token, already lexed by <see cref="Songs.SongParser"/>) to a concrete
/// <see cref="IProgressionTransform"/>. The rest of the idea's priority set registers here additively;
/// today only <c>take</c> is wired (<c>@repeat</c> stays unbuilt — it duplicates the Song-level <c>x&lt;n&gt;</c>).
/// Unknown names and malformed arguments throw <see cref="FormatException"/> naming the token (the Song-DSL
/// error convention).
/// </summary>
public static class ProgressionTransform
{
    /// <summary>Build the transform named <paramref name="name"/> from its raw <paramref name="args"/> string.</summary>
    public static IProgressionTransform Parse(string name, string args)
    {
        switch (name)
        {
            case "take":
                if (!int.TryParse(args, NumberStyles.None, CultureInfo.InvariantCulture, out int count) || count < 1)
                {
                    throw new FormatException(
                        $"Transform \"@take({args})\" requires a positive integer argument.");
                }

                return new TakeTransform(count);

            default:
                throw new FormatException($"Unknown progression transform \"@{name}\".");
        }
    }
}
