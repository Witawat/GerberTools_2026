# File Extension to Layer Mapping

## Gerber File Detection

`Gerber.FindFileType()` auto-detects:
- **Gerber RS-274X**: contains `%FS` or recognized extension
- **Excellon Drill**: contains `M48`
- **Unsupported**: otherwise

## Layer Classification

`Gerber.DetermineBoardSideAndLayer()` classifies by filename/extension:

### KiCad Standard Extensions

| Extension | Side | Layer | Description |
|-----------|------|-------|-------------|
| `.gtl` | Top | Copper | Top copper layer |
| `.gbl` | Bottom | Copper | Bottom copper layer |
| `.gto` | Top | Silkscreen | Top silkscreen |
| `.gbo` | Bottom | Silkscreen | Bottom silkscreen |
| `.gts` | Top | Soldermask | Top solder mask |
| `.gbs` | Bottom | Soldermask | Bottom solder mask |
| `.gtp` | Top | Paste | Top solder paste |
| `.gbp` | Bottom | Paste | Bottom solder paste |
| `.gko` | Both | Outline | Board outline |
| `.gm1` | Both | Mill | Mechanical/milling layer |

### Eagle/Other Extensions

| Extension | Side | Layer | Description |
|-----------|------|-------|-------------|
| `.oln` | Both | Outline | Board outline |
| `.fab` | Both | Outline | Fabrication outline |
| `.fabrd` | Both | Outline | Fabrication outline |
| `.plc` | Top | Silkscreen | Top placement |
| `.sst` | Top | Soldermask | Top solder mask |
| `.crc` | Bottom | Soldermask | Bottom solder mask |
| `.cmp` | Top | Copper | Top copper |
| `.sol` | Bottom | Copper | Bottom copper |
| `.stc` | Top | Silkscreen | Top silkscreen |
| `.sts` | Bottom | Silkscreen | Bottom silkscreen |
| `.dim` | Both | Outline | Dimension/outline |
| `.gph` | Top | Copper | Top copper plane |
| `.gpl` | Bottom | Copper | Bottom copper plane |
| `.art` | Both | Copper | OrCAD artwork |

### Fallback Detection

If no pattern matches, the parser looks for text clues:
- Keywords like "outline", "dim", "fab" in filename
- Falls back to `BoardSide.Either`, `BoardLayer.Unknown`
