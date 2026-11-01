# Investigation and implementation boundary

## Existing product baseline

Verified official documentation on2026-11-01:

- [Wispr Transforms](https://docs.wisprflow.ai/articles/8068950331-how-to-use-transforms-beta) describes desktop beta rewriting, custom instructions, diff/undo and optional automatic post-dictation application. A generic math-rewrite prompt therefore has substantial existing-feature overlap.
- [Wispr dictionary](https://docs.wisprflow.ai/articles/4052411709-teach-flow-your-words-with-the-dictionary) covers technical vocabulary and corrections.

Those documentation pages are changeable observations, not immutable source snapshots or proof of undocumented feature absence. No claim is made that Wispr cannot output mathematical notation. The different contract implemented here is a bounded deterministic typed parser with explicit rejection and exact supported equivalence.

## REA relationship

[REA README at the portfolio's studied revision](https://github.com/morluto/rea/blob/6e9ee97a69cb16c7150d8b550e705cba055ee4f0/README.md) describes reverse-engineering analysis across artifacts/runtime. This project arose from that app-candidate investigation. It does not include a Wispr binary patch, extracted ASR model, proprietary parser source or reconstructed cloud service. No successful Wispr-specific runtime trace or model extraction is claimed here. Private inventory/baseline files are explicitly excluded from public packaging and were not read for this implementation.

## Owning implementation

- `src/Scientific.Core/Language.cs`: strict token aliases, Pratt-like recursive precedence grammar, immutable typed expressions, M/L/T checks, known-constant real-domain evaluation, preview generation and rational-polynomial equivalence. No LLM/API dependency.
- `src/Scientific.App/Workbench.cs`: actual native Windows Forms UI; editable transcript, declarations, previews, AST, explicit-copy gating, comparison and opt-in microphone adapter. Editing source invalidates prior previews.
- `src/Scientific.Cli/Program.cs`: same core, JSON diagnostics, fixture runner and self-test mode.
- `tests/TestSuite.cs`: meaningful examples, rejection/domain/equivalence cases and resource-budget checks. Tests and the app both call the same implementation; expected algebraic identities, numeric values, error classes and exact code strings assert behavior rather than mirroring parsing steps.

## Windows speech adapter

Microsoft's [System.Speech9.0.22 source](https://github.com/dotnet/runtime/blob/v9.0.22/src/libraries/System.Speech/src/Recognition/SpeechRecognitionEngine.cs) and [SetInputToDefaultAudioDevice API](https://learn.microsoft.com/en-us/dotnet/api/system.speech.recognition.speechrecognitionengine.setinputtodefaultaudiodevice) establish the supported adapter route. The app loads a DictationGrammar and requests one utterance only after explicit Start microphone. No recognizer object is created by normal parser startup or tests. Transcript received from recognition still requires manual review and Parse.

This adapter compiled successfully. No microphone recording, recognizer-engine enumeration, Windows speech configuration, acoustic test or ASR accuracy evaluation was performed. Device/engine availability and SAPI behavior remain unverified.

## Verified delivery boundary

Native UI render tests prove accepted/rejected state rendering, copy gating, invalidation on edit and visible multiline declarations. They do not press Copy, change the clipboard, record audio or insert text into another app. The source ZIP is original code plus build/docs/tests, not a claim to redistribute Wispr source. See `docs/validation.json` for release evidence.
