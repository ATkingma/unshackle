# Candidate: Newtonsoft.Json to System.Text.Json

Scout report for TAAK-420 (2026-10-22). Facts from Microsoft's migration guide
(<https://learn.microsoft.com/dotnet/standard/serialization/system-text-json/migrate-from-newtonsoft>, "Table of differences")
and the Newtonsoft.Json repo; corpus counts are grep-only on shallow clones. No builds.
The guide was cited from knowledge, not re-fetched in this session: re-check it before building anything.

## Verdict

**Conditional no-go for a blanket fix; go only for a narrow "Fixed-only, report the rest" fix. Effort L overall.**
Newtonsoft.Json has no license, sponsor or EOL problem (MIT, maintained, no paid tier), so it does not match the Unshackle
selection criterion. Demand is huge, but the migration is a behaviour-change minefield and the main risk is silent
wire-format drift, which contradicts "provably equivalent". Recommend it only if the council wants a volume play.

## Provably equivalent vs Manual

| Shape | Class | Why |
| ----- | ----- | --- |
| `[JsonProperty("n")]` on a property (Name only) | Fixed, with conditions | maps to `[JsonPropertyName("n")]`; any other argument (`Required`, `NullValueHandling`, `Order`, `DefaultValueHandling`, `ItemConverterType`) is Manual |
| `[JsonIgnore]` | Fixed | same name in `System.Text.Json.Serialization`; equal for plain properties |
| `JsonConvert.SerializeObject(x)`, default settings | Fixed only if a harness compares output | STJ escapes `<`, `>`, `&`, `'` and non-ASCII as `\uXXXX`; STJ ignores public fields by default; enums as numbers in both |
| `JsonConvert.DeserializeObject<T>(s)`, default settings | Fixed only with `PropertyNameCaseInsensitive = true` | STJ is case-sensitive by default, Newtonsoft is not |
| untyped `DeserializeObject(s)` / `dynamic` | Manual | yields `JObject`; STJ yields `JsonElement` |
| `JObject`, `JToken`, `JArray`, `JValue`, `SelectToken` | Manual | closest is `JsonNode`/`JsonElement`; different semantics (no JSONPath, no implicit conversions) |
| `JsonConverter` subclasses, `[JsonConverter]` | Manual | different base class and Read/Write contract (`Utf8JsonReader` is a ref struct) |
| `ContractResolver`, `NamingStrategy`, `JsonSerializerSettings` | Manual (few settings map: `Formatting.Indented` to `WriteIndented`, camelCase to `PropertyNamingPolicy`) | no equivalent for `TypeNameHandling` (deliberately unsupported), `MissingMemberHandling`, `DefaultValueHandling`; `ReferenceLoopHandling` differs (STJ throws, or `ReferenceHandler.IgnoreCycles` on .NET 6+) |
| `[JsonObject]`, `[JsonExtensionData]`, `[JsonRequired]`, `[JsonConstructor]` | Manual | partial equivalents differ (STJ `[JsonRequired]` only .NET 7+) |
| `JsonTextReader/Writer`, `JsonSerializer` instances | Manual | `Utf8JsonReader/Writer` |

Difference list to attach to every Manual reason (from the guide): case sensitivity; comments and trailing commas (rejected by
default); quoted numbers; date/time formats (ISO 8601-1:2019 only, Newtonsoft is looser); null into non-nullable value types
(STJ throws, Newtonsoft is lenient); private setters and non-public members; `Dictionary` key types; polymorphism (`$type`);
deserializing into `object`; read-only collection members; duplicate keys; numeric overflow; max depth (64 vs unlimited).

## Behaviour proof idea

A round-trip harness: for each migrated type, build sample values (plus hand-written edge JSON), serialize with Newtonsoft
before and STJ after, and compare as normalized JSON (parse both, compare structurally, ignore escaping); also feed the
Newtonsoft output into the STJ deserializer and assert structural equality. This fits the Proofs model only partly: today a
proof is before/after source, here the proof is behavioural and needs a runnable test per type, so it is a new proof kind
(generated tests in the user's solution, not in Unshackle). Honest ceiling: equivalence for sampled inputs, not all inputs.

## Corpus (10 repos, shallow, 2026-10-22, in `<corpus>/corpus-candidates/newtonsoft/`)

Counts are matching lines in `*.cs`. "files" = files with `using Newtonsoft`. The `[JsonProperty` column also matches
`[JsonPropertyName`, so it is inflated (marked `*`) in repos already on STJ. "Reader" = `SelectToken`/`JsonTextReader`/`JsonTextWriter`.

| Repo (sha) | Status | files | Serialize | Deserialize | `[JsonProperty` | `[JsonIgnore` | JObject etc. | Converter | Resolver/Naming | Settings | Attrs | Reader |
| ---------- | ------ | ----: | ----: | ----: | ----: | ----: | ----: | ----: | ----: | ----: | ----: | ----: |
| `Azure/azure-functions-host` (`62473a2c4f92`) | live | 193 | 68 | 57 | 109 | 7 | 633 | 5 | 5 | 11 | 12 | 6 |
| `btcpayserver/btcpayserver` (`564d9d0ad1c5`) | live | 345 | 54 | 77 | 322 | 89 | 859 | 21 | 18 | 90 | 260 | 7 |
| `nopSolutions/nopCommerce` (`f9e883bc9586`) | live | 283 | 55 | 53 | 1040 | 147 | 9 | 0 | 4 | 39 | 27 | 3 |
| `NuGet/NuGetGallery` (`9cbb95d88442`) | live | 301 | 140 | 74 | 464 | 13 | 473 | 10 | 0 | 34 | 61 | 36 |
| `nuke-build/nuke` (`27e8077b8ab0`) | live | 99 | 9 | 9 | 89 | 17 | 55 | 3 | 63 | 98 | 1 | 0 |
| `Radarr/Radarr` (`56f6168bb6f2`) | live | 115 | 4 | 39 | 235 | 27 | 45 | 25 | 3 | 3 | 13 | 3 |
| `bitwarden/server` (`3327e2fdedeb`) | mostly migrated | 11 | 5 | 15 | 316* | 23 | 94 | 27 | 2 | 7 | 90 | 0 |
| `jellyfin/jellyfin` (`c7ce6a1b56ee`) | migrated | 1 | 0 | 0 | 320* | 316 | 3 | 21 | 0 | 0 | 17 | 0 |
| `OrchardCMS/OrchardCore` (`0f7975917a53`) | migrated | 0 | 1 | 0 | 18 | 30 | 210 | 18 | 0 | 55 | 10 | 0 |
| `JustArchiNET/ArchiSteamFarm` (`fa8906694e05`) | migrated | 0 | 0 | 0 | 165* | 30 | 0 | 2 | 0 | 0 | 268 | 0 |

"Attrs" = `[JsonConverter|JsonObject|JsonConstructor|JsonExtensionData|JsonRequired]`.

Reading: four of ten large, well-run projects already migrated by hand. In the live repos the Fixed shapes (default
Serialize/Deserialize, `[JsonProperty(Name)]`, `[JsonIgnore]`) are the bulk of lines, but most also have hundreds of
JObject/JToken lines (azure-functions-host 633, btcpayserver 859, NuGetGallery 473), converters and settings. A repo is only
fully migratable when its Manual tail is zero; none reaches that. nopCommerce has the most Fixed-friendly shape (1040
`[JsonProperty`, 9 JObject). JObject use cannot be classified by grep (read vs build vs mutate).

## Effort and demand

- Demand: very high (among the most used .NET packages), but with no license pressure the drivers are performance, AOT and
  trimming: a weaker "must migrate" than for the other Unshackle libraries.
- Effort **L**. The Fixed patterns are small (S), but a correct fix needs settings-flow analysis (where a
  `JsonSerializerSettings` is created and which call uses it; `JsonConvert.DefaultSettings`; ASP.NET Core
  `AddNewtonsoftJson`), the package swap, and a behaviour harness. The Manual tail per repo is large.
- Fixer sketch: `newtonsoft` fix with patterns JsonIgnore, JsonPropertyNameOnly, SerializeObjectDefault,
  DeserializeObjectDefault (adds case-insensitive options); everything else Manual with the difference list. Mixed files
  (both libraries) must be allowed, since JObject users stay on Newtonsoft.

## Risks

1. Silent behaviour change (case, escaping, dates, nulls, fields vs properties); tests may not cover it.
2. Wire compatibility with stored JSON and external clients: the biggest production risk, not caught by compiling.
3. Not a license problem, so the council may consider it out of scope.
4. Partial migration leaves both packages referenced; the package-removal step rarely applies.
5. ASP.NET Core `AddNewtonsoftJson` changes behaviour globally, outside call-site rewriting.

## Draft task (taak-start format)

```
# TAAK-421 - Newtonsoft fix: Fixed-only patterns + behaviour harness (draft)
Status: not started (needs council go)
Goal: `--fix newtonsoft` rewrites only provably safe shapes ([JsonIgnore], [JsonProperty(Name)], default
Serialize/Deserialize with case-insensitive options) and reports everything else as Manual with the difference list.
Steps:
1. Pick the pattern set, proofs per pattern.
2. Settings-flow detector (refuse files using DefaultSettings / AddNewtonsoftJson).
3. Round-trip harness design (generated test in the user's solution).
4. Corpus run on nopCommerce, Radarr, NuGetGallery; report Fixed vs Manual share.
5. KingmaDoc plan + verify per pattern.
```

## Council questions

1. Does a library without a license/sponsor/EOL problem belong in Unshackle's scope at all?
2. Is "equivalent for sampled inputs" an acceptable proof standard, or must Fixed require a user-supplied test?
3. Are mixed Newtonsoft/STJ files an acceptable end state?
4. Should ASP.NET Core `AddNewtonsoftJson` projects be refused outright?
5. Prefer this over the planned Moq/Polly/EPPlus/MassTransit in the queue?

## See also

- `docs/projectinfo/architectuur/corpus-automapper.md` - survey model
- `docs/projectinfo/project.md` - roadmap
- `claude/tasks/TAAK-420-scout-newtonsoft.md` - scout task
