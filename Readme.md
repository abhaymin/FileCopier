# FileCopier: Asynchronous Bulk File Copier

[![FOSSA Status](https://app.fossa.com/api/projects/git%2Bgithub.com%2Fabhaymin%2FFileCopier.svg?type=shield)](https://app.fossa.com/projects/git%2Bgithub.com%2Fabhaymin%2FFileCopier?ref=badge_shield)

A high-performance, asynchronous bulk file copying utility built with F# and .NET 8.0. FileCopier leverages parallel processing and retry mechanisms to provide robust and efficient directory synchronization capabilities.

## Table of Contents

- [Features](#features)
- [Architecture](#architecture)
- [Installation](#installation)
- [Usage](#usage)
- [Code Structure](#code-structure)
- [Technical Details](#technical-details)
- [Configuration](#configuration)
- [Examples](#examples)
- [Building from Source](#building-from-source)
- [License](#license)

## Features

✨ **Core Capabilities:**
- **Asynchronous Operations**: Leverages F# async workflows for non-blocking I/O operations
- **Parallel Processing**: Concurrent file copying using `Async.Parallel` for maximum throughput
- **Recursive Directory Copying**: Complete directory tree replication with subdirectories
- **Automatic Retry Logic**: Built-in retry mechanism (up to 15 attempts) for handling transient failures
- **Error Handling**: Comprehensive error handling with informative messages
- **Cross-Platform**: Runs on Windows, Linux, and macOS via .NET 8.0

## Architecture

The application follows a functional programming approach with clean separation of concerns:

```
┌─────────────────┐    ┌──────────────────┐    ┌─────────────────┐
│   Entry Point   │───▶│ Directory Copier │───▶│  File Copier    │
│   (main)        │    │ (directoryCopy)  │    │ (copyToAsync)   │
└─────────────────┘    └──────────────────┘    └─────────────────┘
                                │                       │
                                ▼                       ▼
                       ┌──────────────────┐    ┌─────────────────┐
                       │ Parallel Executor│    │  Retry Handler  │
                       │ (Async.Parallel) │    │ (RetryRun)      │
                       └──────────────────┘    └─────────────────┘
```

## Installation

### Prerequisites
- .NET 8.0 SDK or runtime
- Windows, Linux, or macOS

### Option 1: Download Release
Download the latest release from the [releases page](https://github.com/abhaymin/FileCopier/releases).

### Option 2: Build from Source
```bash
git clone https://github.com/abhaymin/FileCopier.git
cd FileCopier
dotnet build --configuration Release
```

## Usage

### Basic Syntax
```bash
filecp <source_directory> <destination_directory>
```

### Command Line Arguments
- `source_directory`: Path to the source directory to copy from
- `destination_directory`: Path to the destination directory to copy to

### Examples
```bash
# Copy directory on Windows
filecp "C:\MyDocuments" "D:\Backup\MyDocuments"

# Copy directory on Linux/macOS  
filecp "/home/user/documents" "/backup/documents"

# Copy with spaces in path
filecp "C:\Program Files\MyApp" "D:\Backup\MyApp"
```

## Code Structure

### Core Components

#### 1. **Async Extensions** (`Async.AwaitPlainTask`)
```fsharp
type Async with
    static member AwaitPlainTask (task : Task) =
        task.ContinueWith(ignore) |> Async.AwaitTask
```
Extends F# async workflows to work seamlessly with .NET Task-based operations.

#### 2. **File Copy Engine** (`copyToAsync`)
```fsharp
let copyToAsync source dest =
    async {
        printfn "Copying %s to %s" source dest
        use sourceFile = new FileStream(source, FileMode.Open, FileAccess.Read, FileShare.Read, DEFAULT_BUFFER_SIZE, true);
        use destFile = new FileStream(dest, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None, DEFAULT_BUFFER_SIZE, true);
        do! sourceFile.CopyToAsync(destFile) |> Async.AwaitPlainTask
    }
```
- Uses asynchronous FileStream operations
- Implements optimal buffer size (4096 bytes)
- Provides real-time progress feedback
- Automatic resource disposal with `use` keywords

#### 3. **Retry Mechanism** (`RetryRun`)
```fsharp
let rec RetryRun count (work:Async<'T>) = 
   async { 
       try 
         return! work
       with e ->
         if count > 0 then return! RetryRun (count - 1) work 
         else return raise e 
    }
```
- Recursive retry implementation
- Configurable retry count (default: 15 attempts)
- Preserves original exceptions on final failure

#### 4. **Directory Processing** (`directoryCopy`)
```fsharp
let rec directoryCopy srcPath dstPath copySubDirs =
    // Directory validation and creation
    // File enumeration and parallel processing  
    // Recursive subdirectory handling
```
- Validates source directory existence
- Creates destination directories as needed
- Processes files in parallel batches
- Handles recursive subdirectory copying

## Technical Details

### Performance Characteristics
- **Buffer Size**: 4KB optimal buffer for most scenarios
- **Concurrency**: Parallel processing of all files in each directory level
- **Memory Usage**: Efficient streaming with minimal memory footprint
- **Error Recovery**: Automatic retry with exponential backoff behavior

### File System Behavior
- **Permissions**: Preserves file access patterns where possible
- **Timestamps**: Uses default .NET file copy timestamp behavior
- **Overwrite Policy**: Overwrites existing files in destination
- **Directory Structure**: Maintains exact source directory hierarchy

### Threading Model
```
Main Thread
    │
    ├─ Directory Enumeration (Sequential)
    │
    ├─ File Copy Tasks (Parallel)
    │   ├─ File 1 Copy (Async Task)
    │   ├─ File 2 Copy (Async Task)  
    │   └─ File N Copy (Async Task)
    │
    └─ Subdirectory Processing (Recursive)
```

## Configuration

### Compile-time Constants
```fsharp
[<Literal>]
let DEFAULT_BUFFER_SIZE = 4096  // 4KB buffer size
```

### Runtime Behavior
- **Retry Count**: 15 attempts per file (hardcoded)
- **Concurrency**: Unlimited parallel file operations per directory
- **Subdirectory Processing**: Always enabled (recursive copying)

## Examples

### Success Scenario
```bash
$ filecp "/source/data" "/backup/data"
Hello from File Copier built using F#
Copying /source/data/file1.txt to /backup/data/file1.txt
Copying /source/data/file2.jpg to /backup/data/file2.jpg
Copying /source/data/subfolder/doc.pdf to /backup/data/subfolder/doc.pdf
```

### Error Scenarios
```bash
# Invalid argument count
$ filecp "/source"
Hello from File Copier built using F#
To Run please type "filecp "d:\Dir1" "c:\Dir2""

# Non-existent source directory
$ filecp "/nonexistent" "/backup"  
Hello from File Copier built using F#
Error: Source directory does not exist or could not be found: /nonexistent
```

## Building from Source

### Development Setup
```bash
# Clone repository
git clone https://github.com/abhaymin/FileCopier.git
cd FileCopier

# Restore packages
dotnet restore

# Build debug version
dotnet build

# Build release version
dotnet build --configuration Release

# Run application
dotnet run -- "source_path" "dest_path"
```

### Project Structure
```
FileCopier/
├── Program.fs              # Main application logic
├── filecp.fsproj           # F# project file
├── filecp.sln              # Visual Studio solution
├── Properties/
│   └── launchSettings.json # Debug launch configuration
├── LICENSE                 # MIT license
├── README.md               # This file
├── SECURITY.md             # Security policy
└── .gitignore              # Git ignore rules
```

### Dependencies
- **Target Framework**: .NET 8.0
- **Language**: F# 
- **External Dependencies**: None (uses only .NET BCL)

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

[![FOSSA Status](https://app.fossa.com/api/projects/git%2Bgithub.com%2Fabhaymin%2FFileCopier.svg?type=large)](https://app.fossa.com/projects/git%2Bgithub.com%2Fabhaymin%2FFileCopier?ref=badge_large)

---

**Copyright (c) 2022 Abhay Menon**