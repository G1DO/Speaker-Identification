# Recorder Speaker-ID

A C# WinForms application for speaker identification using MFCC features and Dynamic Time Warping (DTW).
This project allows you to enroll speakers by extracting MFCC templates from their voice recordings, and then identify unknown speakers by comparing their MFCC sequence against all enrolled templates.

---

## Table of Contents

1. [Introduction](#introduction)
2. [Features](#features)
3. [Project Structure](#project-structure)
4. [Prerequisites](#prerequisites)
5. [Build & Run](#build--run)
6. [Usage](#usage)

   * [Enrollment Phase](#enrollment-phase)
   * [Identification Phase](#identification-phase)
7. [Test Cases](#test-cases)
8. [Algorithms](#algorithms)

   * [Feature Extraction (MFCC)](#feature-extraction-mfcc)
   * [Dynamic Time Warping (DTW)](#dynamic-time-warping-dtw)
   * [Window-Pruned DTW](#window-pruned-dtw)
9. [Time & Space Complexity](#time--space-complexity)
10. [Documentation](#documentation)
11. [Contributing](#contributing)
12. [License](#license)

---

## Introduction

Speaker Identification is the task of determining “who is speaking” from a short audio sample. This application:

* Records or loads a WAV file.
* Extracts a sequence of 13-dimensional MFCC features from the trimmed audio.
* Stores each enrolled speaker’s MFCC matrix as a template in `Templates/<Name>.bin`.
* Identifies unknown speakers by computing DTW distances between the unknown’s MFCC sequence and all stored templates.

The core matching uses both the **plain DTW** (O(N×M)) and an **O(N×W) window-pruned DTW** (Sakoe–Chiba band) for faster runtime on longer sequences.

---

## Features

* **WinForms GUI** for simple audio recording/loading and speaker interactions.
* **Enrollment**: create a template by extracting MFCC and saving to disk.
* **Identification**: compare a new recording against all templates using DTW.
* **Plain DTW** implementation (no pruning).
* **Window-pruned DTW** implementation (limit warping to a fixed window).
* **Simple CSV-based storage** of templates (no external database).
* **Comprehensive documentation** including complexity analysis and performance benchmarks.

---

## Project Structure

```
Recorder/                           ← root folder containing the solution file
├─ Recorder.sln                     ← Visual Studio solution
├─ README.md                        ← This file
├─ Documentation.pdf                ← Full project write-up
├─ TestCases/                       ← Contains audio files for sample and complete tests
│    ├─ [1] SAMPLE/                 ← Sample-case WAVs for Crystal, Mike, Rich (conspiracy, plausible)
│    └─ …                            ← Any additional test directories
│
├─ GUI/                             ← WinForms code for recording & playback
│    ├─ MainForm.cs
│    ├─ MainForm.Designer.cs
│    └─ MainForm.resx
│
├─ MainFunctions/                   ← Audio I/O and preprocessing
│    ├─ AudioOperations.cs          ← Open/Trim WAV, remove silence
│
├─ MFCC/                            ← MFCC feature extraction (MATLAB integration)
│    └─ MFCC.cs                     ← ExtractFeatures(sig, sampleRate)
│
├─ SpeakerID/                       ← Core Speaker-ID logic
│    ├─ DTW.cs                      ← Full DTW & window-pruned DTW
│    ├─ Utils.cs                    ← Serialize/Deserialize double[][] as CSV
│    ├─ Enroll.cs                   ← Enrollment (extract MFCC → save template)
│    └─ Identify.cs                 ← Identification (load templates → run DTW)
│
└─ Templates/                       ← (Created at runtime) holds `<Name>.bin` files
```

* **`GUI/`**: Contains the WinForms form, buttons, and event handlers.
* **`MainFunctions/AudioOperations.cs`**:

  * `OpenAudioFile(string path) → AudioSignal` (data\[], sampleRate, lengthMs)
  * `RemoveSilence(double[] signal, ...) → double[]` (Voice Activity Detection).
* **`MFCC/`**:

  * `static Seq = MFCC.ExtractFeatures(double[] signal, int sampleRate)`
  * Returns `Sequence` with `MFCCFrame[] Frames`, each `MFCCFrame.Features` is a length-13 array.
* **`SpeakerID/`**: Core logic described below.
* **`Templates/`**: Stores enrolled templates (one CSV-like `.bin` file per speaker).
* **`TestCases/`**: Contains structured folders of WAV files for testing sample and complete cases.

---

## Prerequisites

* **Visual Studio 2019 or later** (with .NET Framework 4.x support).
* **MATLAB Compiler Runtime 2012a** (or the version that matches your MFCC extraction DLL).
* **.NET dependencies**: No external NuGet packages required beyond the default .NET Framework libraries.

---

## Build & Run

1. **Clone or download** this repository to your local machine:

   ```bash
   git clone https://github.com/<YourUsername>/RecorderSpeakerID.git
   cd RecorderSpeakerID
   ```

2. **Open** `Recorder.sln` in Visual Studio.

3. **Build** the solution (Ctrl+Shift+B).

4. **Run** the WinForms application (F5).

---

## Usage

### Enrollment Phase

1. **Load or record** an audio WAV in the GUI (using the File menu or Record button).
2. Type a **Speaker Name** in the text field.
3. Click **Enroll**.

   * The app will:

     1. Remove silence from the audio.
     2. Extract MFCC features (`13 × nFrames`).
     3. Serialize the resulting `double[nFrames][13]` matrix into `Templates/<Name>.bin`.

You should see a popup:

> “Speaker ‘<Name>’ enrolled.”

And a new file in `Templates/` (e.g. `Templates/Alice.bin`).

### Identification Phase

1. **Load or record** a new (unknown) audio WAV.
2. Click **Identify**.

   * The app will:

     1. Remove silence and extract MFCC from the test audio.
     2. For each `*.bin` in `Templates/`:

        * Deserialize the template (`double[mFrames][13]`).
        * Compute **plain DTW** cost and **window-pruned DTW** cost.
        * Take the smaller as that speaker’s distance.
     3. Select the speaker with the **lowest** cost.
   * A popup displays:

     > “Identified speaker: `<Name>`”.

---

## Test Cases

The **TestCases/** folder contains one or more subdirectories of labeled WAV files for sample and complete testing. Example layout:

```
TestCases/
└─ [1] SAMPLE/
   ├─ Crystal_conspiracy.wav
   ├─ Crystal_plausible.wav
   ├─ Mike_conspiracy.wav
   ├─ Mike_plausible.wav
   ├─ Rich_conspiracy.wav
   └─ Rich_plausible.wav
```

* **Set 1: Sample Case**
  This folder has 6 WAV files (2 words × 3 users) for quick testing.
* **Complete Cases**
  You can create additional subfolders (e.g., `[2] COMPLETE_SMALL`, `[3] COMPLETE_LARGE`) with more speakers or longer samples.

**Running Tests:**

1. **Populate** `Templates/` by enrolling each user’s WAV file. E.g.:

   ```bash
   // enroll Crystal with both words, then Mike, then Rich
   Enroll.SaveTemplate("Crystal", "TestCases/[1] SAMPLE/Crystal_conspiracy.wav");
   Enroll.SaveTemplate("Crystal", "TestCases/[1] SAMPLE/Crystal_plausible.wav");
   Enroll.SaveTemplate("Mike",   "TestCases/[1] SAMPLE/Mike_conspiracy.wav");
   // … and so on …
   ```
2. **Identify** a test WAV by clicking **Identify** in the GUI (or calling `Identify.IdentifyBest(path)`).
3. Check the popup or console output to see if the correct speaker is chosen.
4. For pruning performance tests, use longer WAVs placed in `[2]` or `[3]` subfolders and measure time/space.

---

## Algorithms

### Feature Extraction (MFCC)

* Each recorded/loaded WAV is trimmed of silence.
* Audio is divided into overlapping frames and processed by the MATLAB‐based MFCC function:

  ```csharp
  Sequence ExtractFeatures(double[] signal, int sampleRate);
  // returns MFCCFrame[] with 13 coefficients per frame
  ```
* Resulting `Sequence.Frames` is an array of `MFCCFrame`, each containing a `double[13] Features` array.

### Dynamic Time Warping (DTW)

#### Plain DTW

* **Signature**:

  ```csharp
  public static double ComputeFull(double[][] a, double[][] b)
  ```

  * `a` and `b` are two MFCC feature matrices (shape: `n × 13` and `m × 13`).
* Builds a `(n+1)×(m+1)` matrix `DTW[i,j]` initialized to `∞`, with `DTW[0,0]=0`.
* For each `i=1…n`, `j=1…m`:

  1. Compute per-frame **Euclidean distance** between `a[i-1]` and `b[j-1]`:

     ```
     cost = sqrt( Σₖ (a[i−1][k] − b[j−1][k])² )
     ```
  2. `DTW[i,j] = cost + min(DTW[i−1,j], DTW[i,j−1], DTW[i−1,j−1])`.
* **Output**: `DTW[n,m]` is the total minimal warping cost.

**Time & Space**: O(n×m)

---

#### DTW with Window Pruning

* **Signature**:

  ```csharp
  public static double ComputeWindow(double[][] a, double[][] b, int W)
  ```

  * `W` is the window width.
* Only computes `DTW[i,j]` for `|i−j| ≤ W`, leaving the rest at `∞`.
* Reduces complexity to O(n×W) time and O(n×m) space if storing the full matrix, or O(W) space if rolling rows.
* Early abandon: you may skip cells once their cost exceeds a global threshold (not implemented here but can be added).

---

## Time & Space Complexity

| Method                           | Time Complexity | Space Complexity                       |
| -------------------------------- | --------------- | -------------------------------------- |
| **DTW (plain)**                  | O(n × m)        | O(n × m)                               |
| **DTW (window-pruned, width W)** | O(n × W)        | O(n × m) (or O(W) with rolling arrays) |

* **n** = number of frames in test sequence (unknown).
* **m** = number of frames in template sequence (enrolled).
* **W** = pruning window width (e.g., 50).

---

## Documentation

The complete project specification, algorithm descriptions, and complexity analysis are provided in **Documentation.pdf**. You can view or download it directly:

[Download Documentation.pdf](./Documentation.pdf) citeturn15file0

---

## Contributing

Contributions are welcome! If you’d like to:

1. **Improve the DTW algorithm** (e.g., add beam pruning or time-synchronous search).
2. **Optimize performance** and memory usage for very long signals.
3. **Add support** for other feature types (e.g., PLP, spectral features).
4. **Fix bugs** or enhance the UI (e.g., show matching costs in the GUI).

Please fork the repository, make your changes, and submit a Pull Request.

---

## License

This project is provided under the **MIT License**. See the [LICENSE](LICENSE) file for details.
Feel free to use, modify, and distribute under the terms of MIT.
