# Excellon Drill Format Reference

## Overview

Excellon format is used for PCB drill data. GerberTools parses this via `ExcellonFile.cs`.

## File Structure

```
M48                 - Start of header
; COMMENT           - Comments start with semicolon
METRIC              - Units: METRIC or INCH
T1C0.500            - Tool 1, diameter 0.500 mm
T2C1.000            - Tool 2, diameter 1.000 mm
T3F0.800S1.600      - Tool 3, feed rate 0.800, spindle speed 1.600
%                   - End of header
G90                 - Absolute coordinates (G91 = incremental)
G05                 - Drill mode
M15                 - Tool ON
T01                 - Select tool 1
X10.0Y20.0          - Drill hit
X30.0Y40.0
T02                 - Select tool 2
X50.0Y60.0
T00                 - Retract tool
M30                 - End of program
```

## Extended Commands

| Command | Description |
|---------|-------------|
| G85 X...Y...I...J... | Slot (I,J = end point of slot) |
| G41 | Cutter compensation left |
| G42 | Cutter compensation right |
| G93 X...Y... | Work zero (origin offset) |
| G00-G03 | Routing moves (linear/arc) |
| M15/M16 | Tool ON/OFF |
| M25/M26 | Spindle ON/OFF |
| M30 | End of file |
| M00/M01 | Program stop/optional stop |
| R | Repeat count for previous position |

## GerberTools Parsing

- Detection: `Gerber.FindFileType()` checks for `M48` in file content
- Class: `ExcellonFile` in `GerberLibrary.Core/Core/ExcellonFile.cs` (812 lines)
- Key methods:
  - `Load()` - parses Excellon file
  - `Merge()` - merges multiple files
  - `Write()` - outputs merged Excellon
  - `WriteContainedOnly()` - writes only items inside an outline
- Tools are tracked in `DrillToolSet` with diameter and tool number

## Common Issues

1. **Missing M48** - file not detected as Excellon
2. **Tool not defined** - drill hit uses T code not defined in header
3. **Unit mismatch** - METRIC header but INCH coordinates (or vice versa)
4. **G90/G91 confusion** - absolute vs incremental coordinates
