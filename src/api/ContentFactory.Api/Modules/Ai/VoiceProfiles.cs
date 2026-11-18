namespace ContentFactory.Api.Modules.Ai;

public static class VoiceProfiles
{
    public static readonly VoiceProfile IaSimpleEsDefault = new(
        ProfileKey: "ia-simple-es-default",
        ProfileName: "IA Simple ES Narrator",
        Language: "es",
        ConfiguredLocale: "es-ES",
        SpeakingStyle: "Explanatory",
        Tone: "Credible, Approachable, Modern",
        Warmth: "MediumHigh",
        Energy: "Dynamic",
        Pace: "Normal (140 WPM)",
        PronunciationHints: new Dictionary<string, string>
        {
            ["AI"] = "inteligencia artificial",
            ["GPT-4"] = "ge-pe-te cuatro",
            ["LLM"] = "ele-ele-eme"
        }
    );

    public static readonly VoiceProfile IaSimpleEsLatam = new(
        ProfileKey: "ia-simple-es-latam",
        ProfileName: "IA Simple ES Latin America",
        Language: "es",
        ConfiguredLocale: "es-419",
        SpeakingStyle: "Conversational",
        Tone: "Credible, Modern, Neutral",
        Warmth: "High",
        Energy: "Dynamic",
        Pace: "Normal (140 WPM)",
        PronunciationHints: new Dictionary<string, string>
        {
            ["AI"] = "inteligencia artificial",
            ["GPT-4"] = "ge-pe-te cuatro",
            ["LLM"] = "ele-ele-eme"
        }
    );

    public static readonly Dictionary<string, VoiceProfile> Presets = new(StringComparer.OrdinalIgnoreCase)
    {
        [IaSimpleEsDefault.ProfileKey] = IaSimpleEsDefault,
        [IaSimpleEsLatam.ProfileKey] = IaSimpleEsLatam
    };

    public static VoiceProfile GetProfileForChannel(string? channelSlug, string? preferredKey = null)
    {
        if (!string.IsNullOrWhiteSpace(preferredKey) && Presets.TryGetValue(preferredKey, out var profile))
        {
            return profile;
        }

        if (string.Equals(channelSlug, "ia-simple-es", StringComparison.OrdinalIgnoreCase))
        {
            return IaSimpleEsDefault;
        }

        return new VoiceProfile(
            ProfileKey: "generic-es-default",
            ProfileName: "Default Spanish Narrator",
            Language: "es",
            ConfiguredLocale: "es-ES",
            SpeakingStyle: "Explanatory",
            Tone: "Neutral",
            Warmth: "Neutral",
            Energy: "Moderate",
            Pace: "Normal"
        );
    }
}
