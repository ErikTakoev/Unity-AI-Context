# Unity AI Context

🇺🇸 [English](README.md) | 🇺🇦 [Українська](README_UA.md)

Unity tool for analyzing C# code structure and generating XML files with metadata about classes, methods, and fields. Specifically designed to improve context for AI assistants and tools. Allows AI to better understand project architecture without needing to index all code.

## Video Presentation

[![Watch the video](https://img.youtube.com/vi/Rfj9ufq07pU/0.jpg)](https://www.youtube.com/watch?v=Rfj9ufq07pU)

## Installation

### Via Package Manager

1. Open Package Manager in Unity (Window > Package Manager)
2. Click "+" in the top left corner
3. Select "Add package from git URL..."
4. Paste URL: `https://github.com/ErikTakoev/Unity-AI-Context.git`
5. Click "Add"

## Setup

### Creating Settings

1. In the main menu select:  
   `Expecto > AI Context > Create Settings`
2. The `CodeAnalyzerSettings.asset` file will be created automatically in the `Assets/Expecto` folder.
3. If needed, move the file to your desired project folder.
4. Configure parameters:
   - Output Directory: directory for saving XML files
   - Namespace Filters: namespaces to analyze
   - Combined Namespace Filters: namespaces to combine into a single XML file

![Create Settings Step 2](Readme/Settings/CreateSettings2.png)

## Usage

### Running Code Analysis

Code analysis runs automatically on Unity startup or script recompilation. You can also run analysis manually:

1. In the main menu select:  
   `Expecto > AI Context > Generate Context`

### Using Attributes

#### ContextCodeAnalyzerAttribute

Adds additional context to a class, method, field, or property:

```csharp
[ContextCodeAnalyzer(
  @purpose: "Attempts to generate a new chip if the generator is charged and there is free space.",
  @usage: "Call when generator is charged and a chip needs to be generated, either automatically or manually.",
  @returns: "True if a chip was generated, false otherwise.",
  @notes: "Handles charge decrement, state transitions, and chip creation. Sets waiting state if no space is available."
)]
private bool TryGenerateChip()
{
    ...
}
```

#### IgnoreCodeAnalyzerAttribute

Excludes a class, method, or field from analysis:

```csharp
[IgnoreCodeAnalyzer]
private void TestFillField()
{
    ...
}
```

## Quick Project Context Setup
1. **Rule file:** [context-guidelines.mdc](Rules/context-guidelines.mdc) — add rule for cursor, or use as context
2. **Prompt:** `"Add context for classes, methods, and fields according to the rules in @context-guidelines.mdc"` — use this exact text for better results
3. **Run** AI agent on your code files
4. **Review** generated context attributes

## Output XML File Format

Generated XML files contain the following information:

```xml
<CodeAnalysis Namespace="Merge2">
  <Class n="ChipGenerator" b="Chip" c="Purpose: Represents a chip that can generate other chips, supporting both automatic and manual generation modes.; Usage: Attach to a cell in the game field. Initialize with ChipData. Handles chip generation, charging, and visual effects.; Notes: Manages event subscriptions, runtime state, and effect activation. Key for gameplay mechanics involving chip creation and field interaction.">
    <Fields>
      ...
      <Field v="- generatorData: ChipGeneratorData" c="Purpose: Stores static configuration for the chip generator.; Usage: Initialized in Init from ChipData. Used for generation logic.; Notes: Should not be null. Affects generator mode and chip creation." />
      ...
    </Fields>
    <Methods>
      ...
      <Method v="- TryGenerateChip(): bool" c="Purpose: Attempts to generate a new chip if the generator is charged and there is free space.; Usage: Call when generator is charged and a chip needs to be generated, either automatically or manually.; Returns: True if a chip was generated, false otherwise.; Notes: Handles charge decrement, state transitions, and chip creation. Sets waiting state if no space is available." />
      ...
    </Methods>
  </Class>
</CodeAnalysis>
```

Abbreviation explanations:
- **n** — class name
- **b** — base class
- **c** — context (description)
- **v** — value

Access modifiers:
- **++** and **+** — public
- **+-** — public getter, private setter
- **~** — protected
- **-** — private

## Support me and the project!

![](Readme/HelpPlz.jpg)

## License

[MIT](LICENSE)

## Author

[Erik Takoev](https://github.com/ErikTakoev/)