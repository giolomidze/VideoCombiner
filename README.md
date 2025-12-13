# VideoCombiner

VideoCombiner is a C# WPF application that allows users to combine multiple video files into a single video file using
FFmpeg.

## Features

- Drag and drop interface for adding video files
- Supports multiple video formats (.mp4, .avi, .mkv, .webm, .mov)
- Combines videos using FFmpeg
- Real-time progress tracking
- Simple and intuitive user interface

## Prerequisites

- .NET 8.0 SDK
- FFmpeg executable (placed in the `external` folder of the application)

## Installation

1. Clone this repository or download the source code.
2. Open the solution in Visual Studio.
3. Restore NuGet packages if necessary.
4. Build the solution.
5. Place the FFmpeg executable in the `external` folder of the output directory.

## How to Run

### From IDE (Visual Studio/Rider)

1. Open the `VideoCombinerGUI.sln` solution file in Visual Studio or Rider.
2. Build the solution (Build > Build Solution or Ctrl+Shift+B).
3. Run the application (Debug > Start Debugging or F5).
4. Ensure `ffmpeg.exe` is present in the `external` folder of the output directory (it should be copied automatically during build).

### From Command Line

1. Navigate to the project directory.
2. Restore NuGet packages (if needed):
   ```bash
   dotnet restore
   ```
3. Run the application:
   ```bash
   dotnet run
   ```
   Or build and run the solution:
   ```bash
   dotnet build
   dotnet run
   ```
4. Ensure `ffmpeg.exe` is present in the `external` folder (it should be copied automatically).

### From Compiled Executable

1. After building the solution, navigate to the output directory:
   - For Debug builds: `bin/Debug/net8.0-windows/`
   - For Release builds: `bin/Release/net8.0-windows/`
2. Double-click `VideoCombinerGUI.exe` to run the application.
3. Ensure `ffmpeg.exe` is present in the `external` folder within the same directory as the executable.

## Usage

1. Launch the application.
2. Drag and drop video files into the list box, or use the file dialog to add videos.
3. Arrange the videos in the desired order (if needed).
4. Click the "Combine" button.
5. Choose a location and filename for the output video.
6. Wait for the combining process to complete.

## Dependencies

- System.Diagnostics
- System.IO
- System.Text.RegularExpressions
- TagLib# (for reading video durations)

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## Acknowledgements

- This project uses FFmpeg (https://ffmpeg.org/) for video processing.
- TagLib# is used for reading video metadata.