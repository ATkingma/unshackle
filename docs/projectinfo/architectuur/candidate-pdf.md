# Candidate: PDF libraries (iTextSharp 5 / iText 7+ / QuestPDF) to PDFsharp / MigraDoc / PdfPig

Scout for TAAK-400, measured on 2026-10-22. Light survey: web sources, shallow clones, grep counts, no builds.
The verdict is a proposal; the go/no-go is a council decision.

## Verdict

| Source library | Verdict | One-line why |
| -------------- | ------- | ------------ |
| iTextSharp 5 (`iTextSharp`) | **Conditional go, narrow slice only** (read/extract/merge/stamp), effort **L** | Biggest pain (AGPL, EOL, 64M downloads) but the layout half (`Document`/`PdfPTable`) has no 1:1 target |
| iText 7+ (`itext`, `itext7`) | **No-go for now** | Same AGPL pain, but no user code found in this scout (0 of 15 repos); API is a different shape from iText 5 |
| QuestPDF | **No-go** | The community license is a threshold, not a removal; no target with the same fluent layout model (MigraDoc is document-tree based) |

## License and lifecycle facts (primary sources)

- iText Core: the vendor states it is offered under AGPLv3 or a commercial license. AGPL page
  (<https://itextpdf.com/how-buy/agpl-license>, read 2026-10-22): you must distribute all source code "including your own
  product and web-based applications", keep the producer line in every PDF, and use outside an AGPL environment is
  prohibited; a commercial license lifts the copyleft.
- iTextSharp NuGet page (<https://www.nuget.org/packages/itextsharp>): marked EOL, "replaced by iText", only security fixes;
  the vendor recommends iText 7+. Latest `iTextSharp` on NuGet is 5.5.13.6 (flat-container index, 2026-10-22).
- QuestPDF Community License, version 3.0, effective 6 July 2026 (<https://www.questpdf.com/license/community.html>):
  free below USD 1,000,000 annual gross revenue, learning, charities, academia, eligible open source; **not** public-sector
  entities and **not** publicly traded companies, regardless of revenue. 90 days to buy or stop once you stop qualifying.
  Earlier license history was not verified (the page does not say what changed from earlier versions).
- Targets: PDFsharp 6.5.0 and PdfPig 0.1.16 (read only). Their licenses (MIT, Apache-2.0) are from memory of the projects and
  were not re-read here; verify from their repos before a task starts.

## Demand (NuGet total downloads, search API, 2026-10-22)

| Package | Version | Total downloads |
| ------- | ------- | --------------: |
| iTextSharp | 5.5.13.6 | 64,153,424 |
| itext7 | 9.8.0 | 53,855,073 |
| itext | 9.8.0 | 27,622,342 |
| QuestPDF | 2026.9.1 | 36,275,337 |
| PDFsharp (target) | 6.5.0 | 72,945,075 |
| PdfPig (target) | 0.1.16 | 34,246,290 |

Downloads are cumulative and include transitive restores: they show scale, not current migration pressure.

## API distance

| Task in user code | iTextSharp 5 | Target | Distance |
| ----------------- | ------------ | ------ | -------- |
| Open/read | `new PdfReader(path)` | `PdfReader.Open` (PDFsharp) | small; stream/password overloads differ |
| Text extraction | `PdfTextExtractor.GetTextFromPage(reader, n, strategy)` | PdfPig `PdfDocument.Open` + `page.Text` | medium; strategies and whitespace differ, output not identical |
| Merge/split | `PdfCopy`/`PdfSmartCopy`/`PdfImportedPage` | `PdfDocument.AddPage(importedPage)` | medium, mechanical |
| Stamp/fill | `PdfStamper` (`GetOverContent`, `AcroFields`) | PDFsharp `XGraphics.FromPdfPage`, AcroForm API | large; form filling differs |
| Layout | `Document` + `PdfWriter` + `Paragraph`/`PdfPTable` | MigraDoc `Section`/`Table` | large; different model (writer state machine vs tree) |
| HTML to PDF | `XMLWorker`/`HTMLWorker` | none in the MIT stack | no target: Manual |
| Digital signatures | `PdfSignatureAppearance`/`MakeSignature` | none in PDFsharp | no target: Manual |

QuestPDF (`Document.Create(...).Page(...)`, `.Column`/`.Row`) to MigraDoc is a redesign of the layout, not a rewrite.

## Fixed vs Manual (sketch)

- Fixed candidates (Roslyn can resolve the types): whole-page text extraction; whole-document merge; `Image.GetInstance` plus add;
  page count and metadata reads.
- Manual always: `PdfPTable`/`PdfPCell`, `Paragraph`/`Phrase`/`Chunk` styling, `XMLWorker`, signatures, `PdfStamper` form filling,
  all QuestPDF layouts, anything with fonts (`BaseFont`; font resolver setup differs).
- A package swap also changes `using`s and assembly references; legacy repos use `packages.config` or `HintPath` dll references,
  which MSBuildWorkspace handles poorly (see risks).

## Behaviour proof idea

Render-and-compare is not feasible for layout (different engines, no pixel equality). Feasible harness, Fixed slice only: run the
before and after code on a small fixture PDF set, extract text with PdfPig from both outputs, compare normalised text, page count and
metadata. Merge/split: compare page count and per-page text. This proves content, not appearance, and the report must say so.
Proofs need `before` to compile against iTextSharp 5.5.13.x (AGPL, test-only use) and `after` against PDFsharp/PdfPig.

## Corpus (shallow clones, outside the repo)

Location: `D:/playground/corpus-candidates/pdf/src/<name>/` (depth 1, pinned at the sha below, not built). Found with
`gh search code "using iTextSharp.text.pdf"`; the GitHub search rate limit was then hit, so the iText 7 and QuestPDF searches
returned nothing, and `itext/i7n-samples` was not found at the guessed URL. Counts are lines matching a regex over `*.cs`
(comments not stripped).

| Name | Repo (sha) | Library | cs files | Files using lib |
| ---- | ---------- | ------- | -------: | --------------: |
| pdfpatcher | `wmjordan/PDFPatcher` (`e8c720a1d0b2`) | iTextSharp (dll ref) | 386 | 59 |
| pdftoolbox | `Cheeseman5/PDF-Toolbox` (`26a340199cdd`) | iTextSharp-LGPL 4.1.6 | 372 | 309 |
| pdfdocparser | `SergiyStoyan/PdfDocumentParser` (`fee1c42b4ce1`) | iTextSharp | 229 | 6 |
| concatpdf | `k-ujihara/ConcatPDF` (`d1b81b094068`) | iTextSharp 5.5.13 | 37 | 13 |
| bulkpdf | `Tadelsucht/BulkPDF` (`8f5ff00d5be0`) | iTextSharp 5.5.13.3 | 29 | 2 |
| exportblog | `sqzhuyi/ExportBlog` (`c7d5858e4503`) | iTextSharp (dll) | 29 | 2 |
| dynamopdf | `grevit-dev/DynamoPDF` (`0b5ed93b5dae`) | iTextSharp 5.5.10 | 24 | 18 |
| betasign | `DennisWacom/BetaSign` (`30d4adf88c2b`) | iTextSharp 5.5.10 | 16 | 4 |
| sharppdflabel | `wheelibin/SharpPDFLabel` (`5a07bef6be51`) | iTextSharp 5.5.13.1 | 12 | 3 |
| tabster | `Tabster/Tabster.Plugin.PdfFile` (`132a143924a1`) | iTextSharp 5.5.8 | 4 | 2 |
| parsepdf | `ssemyan/ParsePdf` (`8488b81ed0bf`) | iTextSharp 5.5.13 | 4 | 1 |
| pdf2md | `Sam-Hedges/PDFtoMD` (`325be145410e`) | iTextSharp 5.5.13.3 | 4 | 1 |
| img2pdf | `smourier/Img2Pdf` (`69b25919c7d6`) | iTextSharp-LGPL 4.1.6 | 2 | 1 |
| questpdf | `QuestPDF/QuestPDF` (`12d78dd4beac`) | the library itself, not a user | 467 | 464 |
| qinvoice | `QuestPDF/example-invoice` (`da866e56208e`) | QuestPDF 2026.7.2 | 4 | 4 |

The last two are not migration subjects (library source, vendor sample); they only show QuestPDF's API shape.

### API shape counts (matching lines per repo)

Reader = `new PdfReader`, Stamper = `PdfStamper`, Writer = `PdfWriter`, Table = `PdfPTable|PdfPCell`, Para = `new Paragraph|Phrase|Chunk`,
Copy = `PdfCopy|PdfSmartCopy|PdfImportedPage|PdfConcatenate`, TextX = `PdfTextExtractor|TextExtractionStrategy`,
Sign = `PdfSignatureAppearance|MakeSignature`, Img = `Image.GetInstance`. XMLWorker (`XMLWorker|HTMLWorker`) is 0 in every repo.

| Name | Reader | Stamper | Writer | Table | Para | Copy | TextX | Sign | Img |
| ---- | -----: | ------: | -----: | ----: | ---: | ---: | ----: | ---: | --: |
| pdfpatcher | 3 | 4 | 16 | 0 | 0 | 3 | 0 | 0 | 8 |
| pdftoolbox | 24 | 0 | 74 | 0 | 0 | 9 | 0 | 0 | 2 |
| pdfdocparser | 2 | 0 | 0 | 0 | 0 | 0 | 4 | 0 | 0 |
| concatpdf | 5 | 1 | 77 | 0 | 0 | 4 | 0 | 0 | 1 |
| bulkpdf | 2 | 2 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| exportblog | 0 | 0 | 2 | 0 | 4 | 0 | 0 | 0 | 1 |
| dynamopdf | 4 | 2 | 7 | 0 | 1 | 0 | 3 | 0 | 1 |
| betasign | 2 | 2 | 0 | 0 | 0 | 0 | 0 | 3 | 2 |
| sharppdflabel | 0 | 0 | 2 | 6 | 4 | 0 | 0 | 0 | 1 |
| tabster | 1 | 0 | 1 | 0 | 1 | 0 | 2 | 0 | 0 |
| parsepdf | 3 | 0 | 0 | 0 | 0 | 0 | 1 | 0 | 0 |
| pdf2md | 1 | 0 | 0 | 0 | 0 | 0 | 1 | 0 | 0 |
| img2pdf | 0 | 0 | 1 | 0 | 0 | 0 | 0 | 0 | 1 |

Reading: usage is small and mixed. Text extraction (5 repos) and merge (4 repos) recur; layout (tables, paragraphs) is rare,
signing is 1 repo, HTML conversion is 0. QuestPDF reference: 292 `.Column(`/`.Row(` lines in the library repo, 6 in the sample invoice.

## Findings and caveats

- **Sample bias**: 12 of 13 usable repos are old .NET Framework projects (net35 to net48), several with `packages.config` or a
  checked-in `iTextSharp.dll` `HintPath`. Earlier Unshackle corpora were modern SDK-style repos; loading these in MSBuildWorkspace
  needs its own spike. Two repos are on iTextSharp-LGPL 4.1.6 (an older line; its license was not re-read here), so they are
  not the AGPL case.
- **No iText 7 or QuestPDF user repos** were found; that is a limit of this scout (search rate limit), not evidence of no usage.
- Public repos under-represent the demand: closed-source commercial users, who face the AGPL problem, are invisible here.
- Mostly small hobby repos, so the licensing pain is not visible in this corpus.

## Effort and recommendation

- iTextSharp 5 slice (read/extract/merge/stamp, Manual for layout, HTML, signing): **L**. A fix of 3 to 5 patterns, proofs needing two
  PDF libraries, a content-compare harness, plus legacy-project loading work.
- iText 7+: **L**, plus a second API family; not recommended until a corpus exists.
- QuestPDF: not applicable (no equivalent target).
- Proposal: do not start before Moq/Polly/EPPlus/MassTransit. If the council wants it, begin with a report-only scan (flag iText use
  and list the shapes, all Manual): **S**, in line with Unshackle's "report first" principle.

## Top risks

1. Layout code (`PdfPTable`, `Paragraph`) cannot be rewritten without changing output; most of the migration would be Manual.
2. The behaviour proof covers content, not appearance.
3. Legacy `packages.config`/net4x projects may not load in MSBuildWorkspace.
4. AGPL: test fixtures using iTextSharp 5.5.x inside this repo need a license decision for the test project.
5. PDFsharp/PdfPig have no HTML-to-PDF or signing; the target is not a full replacement.

## Council questions

1. Is a report-only fix (S) an acceptable first PDF step, or should it wait for a corpus of modern repos?
2. Is a partial migration (extract/merge only, everything else Manual) worth shipping when the layout code stays on the AGPL library?
3. May the test project reference iTextSharp 5.5.13.x (AGPL) for proofs, given the "no packages with license checks" rule?
4. Is iText 7+ in scope, or only the EOL iTextSharp 5?
5. QuestPDF: explicitly out of scope (threshold license, no target)?

## Draft task (taak-start format)

```
# TAAK-401 - PDF fix: iTextSharp 5 text extraction and merge to PdfPig / PDFsharp (report-first)
Status: draft (needs council go).
Goal: `unshackle scan --fix pdf` reports iTextSharp/iText use per shape; Fixed for PdfTextExtractor.GetTextFromPage and
whole-document PdfCopy merge; everything else Manual with a reason.
Steps: 1. corpus of 8+ modern SDK-style repos incl. iText 7; 2. project-loading spike on packages.config repos;
3. patterns + proofs (text-compare harness with PdfPig); 4. report rows; 5. corpus round.
Where: src/Unshackle.Core/Fixes/Pdf/, src/Unshackle.Tests/Proofs/Pdf/, docs/projectinfo/patterns/.
```

## See also

- `claude/tasks/TAAK-400-scout-pdf.md`: the task of this scout
- `docs/projectinfo/architectuur/corpus-automapper.md`: model of a survey
- `docs/projectinfo/project.md`: roadmap
