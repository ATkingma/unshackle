# Candidate: SixLabors.ImageSharp → SkiaSharp / Magick.NET

Scout result for TAAK-390 (2026-10-22). Light survey: web sources, shallow clones, regex counts, no builds. The go/no-go is a
council decision (`claude/docs/council.md`); this document gives the evidence and a recommendation.

## Recommendation

**Conditional no-go for now (park as roadmap candidate, revisit after Moq/Polly/EPPlus/MassTransit).** Why: the license
problem is real but narrow, the API is not mechanically translatable into a *behaviour-identical* result (resampling, JPEG
quality, orientation, GIF encode), so most of the fix would be `Manual`, and the only commercial driver is closed-source
companies above 1M USD revenue. If the council wants it anyway: target **SkiaSharp** (MIT), scope **only the "simple pipeline"
shape** (load → resize/crop/rotate → save), everything else `Manual`. Effort: **L** (see below).

## License facts (primary sources)

| Fact | Source |
| --- | --- |
| ImageSharp v3 / ImageSharp.Web v3 / Drawing v2 moved from Apache 2.0 to the **Six Labors Split License 1.0** (June 2022 text); v2.x stays Apache 2.0, final 2.x is 2.1.13 (2025-11-25 on NuGet) | https://www.nuget.org/packages/SixLabors.ImageSharp (version history), license text https://raw.githubusercontent.com/SixLabors/ImageSharp/main/LICENSE |
| Apache 2.0 applies if: used in an OSS/source-available project; received only as a transitive dependency of an unrelated third-party package; direct install by a for-profit with < 1M USD annual gross revenue; non-profit / registered charity. Commercial license in all other cases | LICENSE (same link) |
| Pricing (per year): Boutique 799 USD (up to 10 devs), Agency 1,299 (11-20), Enterprise 4,999 (unlimited); Community tier free on application | https://sixlabors.com/pricing/ |
| **Build-time enforcement** since ImageSharp 4.0.0 (also Drawing 3.0.0, Web 4.0.0, Fonts 3.0.0): a `sixlabors.lic` file is checked at compile time for projects that reference the library *directly*; missing/invalid key fails the build. Standard key lasts 1 year, trial 90 days; announced 2025-09-04 | https://sixlabors.com/posts/licence-enforcement-changes |
| Current versions (NuGet, 2026-10-22): ImageSharp 4.1.3 (2026-10-09), 3.2.0 (2026-10-09), 3.1.12 (2025-10-29); 4.0.0 was 2026-05-12 | NuGet page above |
| Total downloads: ImageSharp 328.5M; SkiaSharp 361.6M (MIT, 4.153.1); Magick.NET-Q16-AnyCPU 48.6M (Apache-2.0, 15.0.0) | NuGet pages |

Secondary note (not verified against Six Labors): an ABP support thread (abp.io/support/questions/10663) reports the 4.x
build-key requirement biting a framework consumer. The 3.x line has *no* build key, so legal exposure for 3.x users is
contractual (revenue test), not technical. The original 2022 announcement page was not retrievable (404); the June 2022 date
comes from the NuGet license page for 3.1.3 via search snippet, so treat the exact announcement date as unverified.

Reading: pain is **real but bounded**. OSS projects, small companies and transitive consumers are all fine. Affected: direct
reference, closed source, > 1M USD revenue. That is exactly the "enterprise team that got a procurement letter" profile.
It is also a forward problem: NuGet flags several 4.x versions with a high-severity vulnerability, and staying on an old line
means no fixes.

## API distance

| ImageSharp shape | SkiaSharp | Magick.NET | Fixed? |
| --- | --- | --- | --- |
| `Image.Load(stream/path)` | `SKBitmap.Decode` / `SKCodec.Create` | `new MagickImage(stream)` | Yes (type-for-type, but variable types change) |
| `img.Mutate(x => x.Resize(w,h))` | `bitmap.Resize(info, SKSamplingOptions)` | `img.Resize(w,h)` | Partial: default resampler differs (ImageSharp Bicubic vs chosen `SKSamplingOptions`); `ResizeOptions` Mode (Crop/Pad/Max/Stretch) needs own math in Skia |
| `Crop(Rectangle)`, `Rotate`, `Flip` | `SKCanvas` + `DrawBitmap`, or `ExtractSubset`; rotate through canvas | `Crop`, `Rotate`, `Flip` | Partial (Skia: no one-liner) |
| `SaveAsJpeg/Png/Webp(+Encoder{Quality})` | `SKImage.Encode(SKEncodedImageFormat, quality)` | `Write(stream, MagickFormat)` + `Quality` | Partial: defaults differ (ImageSharp JPEG 75, Skia caller-given, PNG compression differs). **GIF: Skia cannot encode** (Magick only) |
| `Image<Rgba32>` + `ProcessPixelRows` / indexer | `SKBitmap.Pixels`, `GetPixelSpan` (premultiplied alpha by default) | `GetPixels()` | **Manual**: alpha model, byte order and row access differ |
| Filters (`GaussianBlur`, `Grayscale`, `Contrast`, ...) | `SKImageFilter` / `SKColorFilter` | most exist as methods | Manual in Skia, Partial in Magick |
| Drawing (`DrawText`, `DrawImage`, polygons; `SixLabors.Fonts`, `ImageSharp.Drawing`) | `SKCanvas`, `SKPaint`, `SKFont` | `Draw`, `Annotate` | **Manual** (text layout, font loading differ) |
| Metadata (`ExifProfile`, ICC) | `SKCodec.EncodedOrigin` only; no EXIF writer | `GetExifProfile()` | Manual (Skia), Partial (Magick) |
| Animated GIF/WebP `Frames` | decode frames via `SKCodec`; no encode | `MagickImageCollection` | Manual |
| `ImageSharp.Web` middleware (URL-based resize, cache, providers) | none | none | **No equivalent**: Manual |

The Skia/Magick API cells are from the scout's knowledge of those libraries, not re-verified against their docs in this pass.

Skia vs Magick: SkiaSharp is MIT, small native assets, fast, but a lower-level canvas API. Magick.NET is Apache-2.0, API
closer to ImageSharp (fluent calls), full format coverage incl. GIF/TIFF, but large native binaries (Q8/Q16/Q16-HDRI variants,
OS-specific packages) and a memory-hungry model. A tool shipping one target would pick **Skia** for the common web-resize case;
Magick for GIF/TIFF/metadata-heavy code. Supporting both doubles the pattern set.

## Fixed vs Manual sketch

- Fixed (cautiously): `Image.Load` + a single `Mutate(x => x.Resize(w, h))` or `Resize(ResizeOptions{Mode=Max})` + `SaveAsJpeg/Png/Webp(stream)` in the same method. Needs flow tracking of the image variable; `using var` lifetimes and async stream APIs (`LoadAsync`, `SaveAsync`) complicate it.
- Manual: pixel access, filters, drawing/fonts, metadata/EXIF, animated frames, `Configuration`/`DecoderOptions`, ImageSharp.Web (all 60 Web hits in Umbraco are Manual).
- Project-level (the part that matters): remove `SixLabors.*` PackageReference, add `SkiaSharp` + `SkiaSharp.NativeAssets.*`, remove `sixlabors.lic` wiring (`SixLaborsLicenseFile`/`SixLaborsLicenseKey` properties).

## Behaviour proof idea

A **pixel-compare harness** is feasible: a fixture (`before.cs` on ImageSharp, `after.cs` on Skia) that is *run*, not just
compiled, on a small image set, comparing dimensions exactly and pixels by tolerance (mean absolute error per channel, PSNR
threshold). Exact equality is unattainable (different resampling kernels, JPEG encoders). This is a new proof kind for
Unshackle (proofs are compile-only today; `Integration/` runs samples via MSBuild). Realistic hard guarantees: dimensions,
format, orientation and alpha behaviour; "looks the same" is not guaranteed.

## Corpus

Repos cloned shallowly (`--depth 1`) under `D:/playground/corpus-candidates/imagesharp/<name>/`, pinned at the sha cloned on
2026-10-22 (default branch HEAD). Counting script (outside repo): scratchpad `m.py`, regex over `*.cs` that mention `SixLabors`
(comments stripped, bin/obj/wwwroot skipped). Counts are regex matches, indicative only.

| Repo | sha | ImageSharp ver. | .cs files | Load | Mutate | Resize | Crop/Rot/Flip | Save | Encoders | Pixel types | Pixel rows | Drawing/Fonts | Metadata | Web |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| GoNorth | 6a4970868a | 3.1.10 | 2 | 2 | 6 | 7 | 1 | 2 | 6 | 16 | 0 | 0 | 0 | 0 |
| tranga | 7986e3c86d | 3.1.12 | 3 | 2 | 4 | 6 | 0 | 4 | 1 | 0 | 0 | 0 | 0 | 0 |
| Fergun | e282436524 | 3.1.12 (+Drawing 2.1.7) | 1 | 0 | 1 | 0 | 0 | 1 | 1 | 1 | 0 | 1 | 0 | 0 |
| Umbraco-CMS | acfaa92e9e | 3.2.0 (+Web 3.3.0) | 22 | 2 | 3 | 4 | 8 | 0 | 1 | 1 | 0 | 0 | 10 | 60 |
| piranha.core | 90f9636b10 | 2.1.13 | 1 | 6 | 5 | 4 | 6 | 4 | 0 | 0 | 0 | 0 | 0 | 0 |
| MiniStarter | 4a1bbf1009 | 2.1.3 | 1 | 1 | 1 | 2 | 1 | 1 | 0 | 0 | 0 | 0 | 0 | 0 |
| twigaten | f767d05e76 | 2.1.0 (+Drawing beta) | 6 | 6 | 2 | 0 | 5 | 2 | 0 | 7 | 0 | 3 | 0 | 0 |
| painter | e691e969ae | 1.0.3 (+Drawing beta) | 3 | 1 | 4 | 1 | 0 | 1 | 0 | 15 | 4 | 2 | 0 | 0 |
| puck-cms-platform | 727c871679 | 1.0.4 (+Web 1.0.4) | 4 | 2 | 1 | 0 | 5 | 0 | 0 | 0 | 0 | 0 | 0 | 14 |
| nopCommerce | f9e883bc95 | none: already on SkiaSharp 4.152.1 | 0 | - | - | - | - | - | - | - | - | - | - | - |
| OrchardCore | 0f7975917a | none in csproj/cs | 0 | - | - | - | - | - | - | - | - | - | - | - |

Observations:

- 9 of 11 repos use ImageSharp; most only use the simple pipeline (Load/Mutate/Resize/Crop/Save), typically 1-3 files.
  That is the Fixed-candidate shape. Pixel access (painter, twigaten, GoNorth) and Drawing/Fonts (painter, twigaten, Fergun) are
  the Manual tail. Umbraco is the heavy case: ImageSharp.Web (60 hits) plus Configuration/Metadata.
- Only 4 of 9 are on 3.x (Apache for them as OSS); the rest are on 1.x/2.x. The corpus is biased: OSS is the free case, and
  real demand lives in closed-source code that GitHub search cannot see.
- Precedent: **nopCommerce already uses SkiaSharp** (with HarfBuzz for text), a hint that the ecosystem treats Skia as the
  landing place. Package-reference scan only; the migration commit was not inspected.
- The corpus has no before/after ground truth like the AutoMapper corpus and offers few large migration targets.

## Demand

- ImageSharp is huge (328.5M downloads; 28.6M for 3.1.12 alone), but most of it is transitive or OSS use, which is license-free.
- Addressable set: direct, closed-source, > 1M USD revenue. Size unknown. Narrower than FluentAssertions or AutoMapper; comparable to EPPlus (same "pay or build fails" story).
- The license (799-4,999 USD/year) is cheap versus a migration project, which weakens the economic case.

## Effort

**L** (7-12 tasks): two possible targets, about 5 patterns for the Fixed shape, a project-file pattern, a large Manual-reason
catalogue, ImageSharp.Web as pure Manual, plus a new pixel-compare harness (new infrastructure, not just patterns). A smaller
**M** cut: only the simple-pipeline pattern and the package/license-key project pattern on SkiaSharp, no harness, everything
else reported as Manual.

## Top risks

1. Behaviour drift with no cheap proof: resampling and encoder defaults change output; "Fixed" may silently alter images.
2. Skia cannot encode GIF and has no EXIF writing; such users must land on Magick, doubling the work.
3. Small, uncertain audience (closed-source, > 1M USD, direct reference); low license price.
4. Native assets: Skia/Magick packages bring platform natives (Linux `NativeAssets`, fonts), an operational change.
5. Version split (1.x/2.x/3.x/4.x) and the Drawing/Fonts/Web family multiply the type surface.

## Council questions

1. Is the narrow audience enough, given the license price of 799-4,999 USD/year?
2. SkiaSharp only, or Magick.NET too (GIF, EXIF)? Or Magick only because its API is closer?
3. Is an executed pixel-compare an acceptable new proof kind, or do we accept compile-only proofs plus a loud "behaviour may differ" report line?
4. Ranking against Moq/Polly/EPPlus/MassTransit: EPPlus has the same story with a clearer audience.
5. Should 4.x build-key enforcement trigger a "detect only" mode (report which projects need a key) before any rewrite?

## Draft task (if go)

```
# TAAK-391 — ImageSharp → SkiaSharp: simple pipeline + project pattern
Goal: new fix `imagesharp` (target SkiaSharp). Patterns: LoadResizeSave (Fixed, single-method flow), PackageSwap (csproj:
remove SixLabors refs and license-key properties, add SkiaSharp + NativeAssets), everything else Manual with reasons.
Steps: 1. Council decision, Skia vs Magick. 2. Sample (SampleShop-style) with ImageSharp 3.1.12. 3. Patterns + proofs in
Proofs/ImageSharp/<Pattern>/ (before on ImageSharp, after on SkiaSharp 4.x). 4. Optional pixel-compare harness. 5. Corpus round, docs, roadmap row.
```

## See also

- `docs/projectinfo/architectuur/corpus-automapper.md` — model survey for this document
- `docs/projectinfo/project.md` — roadmap
- `claude/tasks/TAAK-390-scout-imagesharp.md` — scout task
- `claude/tasks/CLAIMS.md` — file claims
