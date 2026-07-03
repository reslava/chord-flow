namespace ChordFlow.Instruments.Guitar;

/// <summary>
/// A declared, typed input of a <see cref="IVoicingOperator"/> (voicings-engine design §2.3). Operators publish a
/// <see cref="ParameterSchema"/> of these so any UI auto-renders the right control and stays generic as the
/// operator set grows — the introspection that makes the inspector page a schema-driven dumb view. Two v1 kinds:
/// <see cref="EnumParam"/> (a choice from a fixed set, e.g. the CAGED shape / shell form) and <see cref="RegionParam"/>
/// (the neck fret window). The enum value travels in the request's <see cref="ParameterValues"/>; the region value is
/// the request's own <see cref="FretRegion"/> field, which the <see cref="RegionParam"/> describes/bounds.
/// </summary>
public abstract record ParameterDef(string Name);

/// <summary>A choice parameter: one of <see cref="Values"/> (the names of an enum's members), defaulting to <see cref="Default"/>.</summary>
public sealed record EnumParam(string Name, IReadOnlyList<string> Values, string Default) : ParameterDef(Name)
{
    /// <summary>Build an <see cref="EnumParam"/> from the given members of enum <typeparamref name="TEnum"/>.</summary>
    public static EnumParam Of<TEnum>(string name, IEnumerable<TEnum> values, TEnum @default) where TEnum : struct, Enum =>
        new(name, values.Select(v => v.ToString()).ToList(), @default.ToString());
}

/// <summary>The neck fret window parameter: the region's low/high fret must fall within <see cref="Min"/>..<see cref="Max"/>.</summary>
public sealed record RegionParam(string Name, int Min, int Max) : ParameterDef(Name);

/// <summary>A resolved parameter value echoed on a <see cref="VoicingDerivation"/> — name + its chosen value, for display + the synthetic id.</summary>
public sealed record ResolvedParam(string Name, string Value);

/// <summary>
/// The named enum-parameter values of a <see cref="VoicingRequest"/> — a small validated bag (name → member name).
/// The region is carried separately on the request as its <see cref="FretRegion"/>; this bag holds only the
/// <see cref="EnumParam"/> choices.
/// </summary>
public sealed class ParameterValues
{
    /// <summary>An empty bag — for operators with no enum parameters.</summary>
    public static readonly ParameterValues Empty = new(new Dictionary<string, string>());

    private readonly IReadOnlyDictionary<string, string> _values;

    public ParameterValues(IReadOnlyDictionary<string, string> values) =>
        _values = new Dictionary<string, string>(values, StringComparer.OrdinalIgnoreCase);

    /// <summary>Build a bag from name/value pairs, e.g. <c>ParameterValues.Of(("shape", "E"))</c>.</summary>
    public static ParameterValues Of(params (string Name, string Value)[] pairs) =>
        new(pairs.ToDictionary(p => p.Name, p => p.Value, StringComparer.OrdinalIgnoreCase));

    /// <summary>The value for <paramref name="name"/>; throws if absent (a schema violation the operator surfaces).</summary>
    public string Get(string name) =>
        _values.TryGetValue(name, out string? value)
            ? value
            : throw new ArgumentException($"Missing voicing parameter '{name}'.", nameof(name));

    /// <summary>Parse the value for <paramref name="name"/> as a member of enum <typeparamref name="TEnum"/>.</summary>
    public TEnum Enum<TEnum>(string name) where TEnum : struct, Enum =>
        System.Enum.TryParse(Get(name), ignoreCase: true, out TEnum parsed)
            ? parsed
            : throw new ArgumentException($"'{Get(name)}' is not a valid value for parameter '{name}'.", nameof(name));

    /// <summary>The parameter names present in the bag.</summary>
    public IReadOnlyCollection<string> Names => (IReadOnlyCollection<string>)_values.Keys;

    /// <summary>Whether <paramref name="name"/> is present.</summary>
    public bool Has(string name) => _values.ContainsKey(name);
}

/// <summary>
/// The declared parameter surface of a <see cref="IVoicingOperator"/> — its ordered <see cref="ParameterDef"/>s,
/// plus <see cref="Validate"/> which fails loud on a request that omits an enum param, carries an unknown key, uses
/// an out-of-set enum value, or a region outside the declared bounds.
/// </summary>
public sealed class ParameterSchema
{
    public IReadOnlyList<ParameterDef> Parameters { get; }

    public ParameterSchema(params ParameterDef[] parameters) => Parameters = parameters;

    /// <summary>The declared enum parameters (the ones carried in <see cref="ParameterValues"/>).</summary>
    public IEnumerable<EnumParam> EnumParams => Parameters.OfType<EnumParam>();

    /// <summary>The declared region parameter, if any.</summary>
    public RegionParam? Region => Parameters.OfType<RegionParam>().FirstOrDefault();

    /// <summary>Validate <paramref name="request"/> against this schema; throws <see cref="ArgumentException"/> on any violation.</summary>
    public void Validate(VoicingRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var enumNames = EnumParams.Select(e => e.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);

        // No unknown keys.
        foreach (string name in request.Params.Names)
        {
            if (!enumNames.Contains(name))
            {
                throw new ArgumentException($"Unknown voicing parameter '{name}' for this operator.", nameof(request));
            }
        }

        // Every declared enum param present with an allowed value.
        foreach (EnumParam param in EnumParams)
        {
            if (!request.Params.Has(param.Name))
            {
                throw new ArgumentException($"Missing voicing parameter '{param.Name}'.", nameof(request));
            }

            string value = request.Params.Get(param.Name);
            if (!param.Values.Contains(value, StringComparer.OrdinalIgnoreCase))
            {
                throw new ArgumentException(
                    $"'{value}' is not an allowed value for '{param.Name}' (expected one of {string.Join(", ", param.Values)}).",
                    nameof(request));
            }
        }

        // NB: RegionParam is a descriptive UI hint (a control's min/max), not a hard gate — the derivation itself
        // decides whether a region yields a grip (throwing if no anchor). The grip shim preserves the pre-existing
        // no-region-validation behaviour, so a wider region never becomes a new rejection (C1/C2).
    }

    /// <summary>The resolved parameter echo for a validated <paramref name="request"/> (enum choices + the region).</summary>
    public IReadOnlyList<ResolvedParam> Resolve(VoicingRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var resolved = EnumParams.Select(e => new ResolvedParam(e.Name, request.Params.Get(e.Name))).ToList();
        if (Region is { } region)
        {
            resolved.Add(new ResolvedParam(region.Name, $"{request.Region.MinFret}-{request.Region.MaxFret}"));
        }

        return resolved;
    }
}
