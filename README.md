# VideoCombiner

VideoCombiner is a Windows desktop app (C#, WPF, .NET 8) that joins multiple video files into one using FFmpeg.

## Features

- Drag and drop video files onto the window to queue them (files already in the list are skipped)
- Reorder the list with the ↑/↓ buttons or by dragging items, and remove items with ✕
- Accepts `.mp4`, `.avi`, `.mkv`, `.webm` and `.mov` files
- Joins clips with FFmpeg's concat demuxer without re-encoding, so it's fast and lossless
- Shows progress while combining

## Limitations

- **Clips must share the same encoding.** Videos are joined with `-c copy` (no re-encoding), so every clip needs the
  same codec, resolution and frame rate. This is typical of clips from the same camera or export preset. Mixing
  containers or codecs (for example, a `.webm` with a `.mov`) will usually fail or produce a broken file.
- **Windows only**, because it uses WPF.

## Prerequisites

- Windows
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) or newer (to build), or the .NET 8 Desktop Runtime
  (to run a prebuilt copy)

FFmpeg is included at `external/ffmpeg.exe` and the build copies it next to the app, so you don't need to install it.

## Build and run

From the repository root:

```
dotnet run --project VideoCombinerGUI.csproj
```

Name the project explicitly: the solution contains both the app and its tests, so a bare `dotnet run` can't tell
which one to start.

Alternatively, open `VideoCombinerGUI.sln` in Visual Studio or Rider, set **VideoCombinerGUI** as the startup
project, and run it. A build places the executable at `bin\Debug\net8.0-windows\VideoCombinerGUI.exe`.

In VS Code (with the C# Dev Kit extension), press **F5** to build and debug the app. **Ctrl+Shift+B** runs the
`build` task, and the `test` task (Terminal > Run Task) runs the test suite.

## Usage

1. Launch the application.
2. Drag and drop video files onto the list.
3. Arrange them in the order you want: use ↑/↓ or drag an item onto another to move it, and ✕ to remove it.
4. Click **Combine Videos**.
5. Choose a location and file name for the output (default: `combined_video.mp4`).
6. Wait for the progress bar to finish. A confirmation appears when it's done, and the list is cleared.

## Tests

Tests live in `tests/VideoCombinerGUI.Tests` and use xUnit v3.

```
dotnet test VideoCombinerGUI.sln
```

The suite includes an integration test that generates two short clips with the bundled FFmpeg and combines them. It
is tagged `Category=Integration` and skips itself if `external/ffmpeg.exe` is missing. To run only the fast unit
tests:

```
dotnet test VideoCombinerGUI.sln --filter "Category!=Integration"
```

## Project structure

| Path                      | Purpose                                                     |
|---------------------------|-------------------------------------------------------------|
| `MainWindow.xaml(.cs)`    | UI: drag-and-drop list, progress bar, combine button        |
| `VideoCombiner.cs`        | Core logic: reads durations, runs FFmpeg, reports progress  |
| `FFmpegProgressParser.cs` | Parses FFmpeg `time=` output into a progress percentage     |
| `external/ffmpeg.exe`     | Bundled FFmpeg binary                                       |
| `.vscode/`                | VS Code launch config (F5) and build/test tasks             |
| `tests/`                  | xUnit test project                                          |
| `REQUIREMENTS.md`         | Functional and non-functional requirements                  |

## Dependencies

- [FFmpeg](https://ffmpeg.org/) for video processing (bundled)
- [TagLib#](https://github.com/mono/taglib-sharp) (NuGet `TagLibSharp`) for reading video durations

## Contributing

Contributions are welcome! Please run `dotnet test` before opening a pull request.
