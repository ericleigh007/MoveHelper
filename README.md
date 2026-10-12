# MoveHelper

MoveHelper is a Windows tray utility. Rest the pointer on a title bar and a handle grows there, so a window can be dragged without hunting for the empty pixels between tabs and caption buttons. The corners grow a larger resize target the same way.

This is a proof of concept. It lives in this repository only. It is not a PowerToys pull request.

## What already exists

[PowerToys Grab And Move](https://learn.microsoft.com/en-us/windows/powertoys/grab-and-move) (PowerToys 0.99, April 2026) already solves “the title bar is a bad target.” Hold Alt or Win and drag anywhere in the window. Right-drag resizes from the nearest edge. [AltSnap](https://github.com/RamonUnch/AltSnap) does the same job as a small standalone tool.

MoveHelper does not replace that. Grab And Move is the better tool when you want the whole window to be a drag surface and you are willing to hold a key. MoveHelper is for the other habit: you are already on the title bar, and the grabbable gap is only a few pixels wide. There is no modifier, so Alt and Win stay with the app under the pointer. A quick click still reaches the window, because the handle waits for the pointer to rest.

## What the proof of concept does

* After the pointer rests on the caption (200 ms by default), a handle appears.
* The handle can sit near the pointer, in the center of the open caption, or across the open caption. The caption buttons are left clear.
* Dragging the handle uses the normal Windows move, including snap.
* Resting on a corner grows an L-shaped resize handle. Dragging it uses the normal Windows resize.
* A tray menu pauses it, picks the handle shape, sets the delay (100 / 200 / 400 ms), and turns corners on or off.
* Full-screen borderless windows are ignored so a game or video does not grow a handle.
* Settings are saved in `%AppData%\MoveHelper\settings.json`. Add process names (without `.exe`) under `excludedProcesses` for apps that should be left alone.

Custom title bars (Chrome, Edge, Terminal, VS Code) often do not report real caption buttons. Those windows get a top band and an assumed button cluster on the right. Tabs inside that band are not detected, so a handle can cover a tab after the hover delay. Move off the handle and it goes away.

An elevated window cannot be moved from a non-elevated MoveHelper. Windows blocks that message.

## Run

Install the .NET 8 SDK, then from this directory:

```powershell
dotnet run --project src/MoveHelper
```

The app closes into the tray. Hover a title bar, then drag the handle. Double-click the tray icon to pause.

To see the caption rectangle MoveHelper computed for the foreground window:

```powershell
dotnet run --project src/MoveHelper -- --probe
```

## Tests

```powershell
dotnet test
```

The tests cover handle placement. They do not drive a live window.

## Later, PowerToys

PowerToys is MIT-licensed and accepts contributions, but a new utility is not a drive-by pull request. The route is an issue or proposal in [microsoft/PowerToys](https://github.com/microsoft/PowerToys) first, using their [contributing guide](https://github.com/microsoft/PowerToys/blob/main/CONTRIBUTING.md). This repository is the place to try the hover handle before that conversation.
