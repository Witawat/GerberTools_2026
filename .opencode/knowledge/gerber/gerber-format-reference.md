# Gerber RS-274X Format Reference

## Overview

Gerber RS-274X is the standard file format for PCB manufacturing data. Files consist of Gerber commands (G-codes, D-codes) and extended commands (%-blocks).

## File Structure

```
Header section:
  %FS...%     - Format specification (coordinate format)
  %MO...%     - Mode (metric/imperial)
  %AD...%     - Aperture definition
  %AM...%     - Aperture macro
  %AB...%     - Aperture block
  %SR...%     - Step and repeat
  %LN...%     - Layer name
  %TD...%     - Comment/attribute delete
  %TA...%     - Attribute
  %TO...%     - Object attribute
  %TF...%     - File attribute

Body section:
  G01/G02/G03 - Linear/CW arc/CCW arc interpolation
  G04         - Comment
  G36         - Start polygon area
  G37         - End polygon area
  G74         - Single quadrant mode
  G75         - Multi quadrant mode
  D01         - Plot (expose aperture with movement)
  D02         - Move (without exposure)
  D03         - Flash (expose aperture at single point)
}

  M00*        - Program stop
  M02*        - End of file
```

## Coordinate Format (FS)

```
%FSLAX24Y24*%
  L = Leading zero omission (T=Trailing, L=Leading)
  A = Absolute coordinates (I=Incremental)
  X24 = X: 2 integer digits, 4 decimal digits
  Y24 = Y: 2 integer digits, 4 decimal digits

%FSLAX34Y34*%  -> metric, 3+4 = 7 digits total (e.g. 0012345 = 1.2345 mm)
%FSLAX24Y24*%  -> imperial, 2+4 = 6 digits total (e.g. 012345 = 1.2345 inch)
```

## Mode (MO)

```
%MOIN*%  - Imperial (inches)
%MOMM*%  - Metric (millimeters)
```

## Aperture Definitions (AD)

```
%AD<code><shape>,<parameters>*%

Shape types:
  C - Circle:        %ADD10C,0.5*%       (diameter 0.5)
  R - Rectangle:     %ADD11R,1.0x0.5*%   (width=1.0, height=0.5)
  O - Obround:       %ADD12O,1.0x0.5*%   (width=1.0, height=0.5)
  P - Polygon:       %ADD13P,0.5x6-45*%  (diameter=0.5, vertices=6, rotation=-45deg)
  M - Macro:         %ADD14M,MacroName*%
```

## Aperture Macros (AM)

```
%AM<name>*<parts>*%
```

Primitive types:
- 1: Circle (center X, center Y, diameter)
- 2, 20: Line (width, start X, start Y, end X, end Y)
- 4: Outline (line count + vertices)
- 5: Polygon (center X, center Y, diameter, vertices, rotation)
- 6: Moire (center X, center Y, outer dia, ring count, ring width, gap, cross, rotation)
- 7: Thermal (center X, center Y, outer dia, inner dia, gap width, rotation)
- 21: CenterLine (width, height, center X, center Y, rotation)
- 22: LowerLeftLine (width, height, lower left X, lower left Y, rotation)

Variables ($1, $2, etc.) can be used and defined in AD command.

## Interpolation

```
G01*       - Linear interpolation (X/Y/I/J, D01/D02)
G02*       - Clockwise circular interpolation (with I,J center)
G03*       - Counter-clockwise circular interpolation (with I,J center)
G74*       - Single quadrant arc mode
G75*       - Multi quadrant arc mode
```

## Polygon Mode

```
G36*       - Begin polygon (all subsequent D01 commands fill material)
G37*       - End polygon (output filled shape)
```

## Step and Repeat (SR)

```
%SRX3Y2I5J10*%  - Repeat X 3 times, Y 2 times, step X=5mm, step Y=10mm
%SRX1Y1*%       - Cancel step and repeat
```

## Polarity

```
%LPD*%     - Dark polarity (add material)
%LPC*%     - Clear polarity (remove material)
```

## Mirror, Rotate, Scale

```
%MIA*%     - Mirror X axis
%MIB*%     - No mirror
%LR0*%     - No rotation
%LR90*%    - 90 degree rotation
%LS0.5*%   - Scale by 0.5
```

## Excellon Drill Format (M48)

```
M48
; HEADER
METRIC (or INCH)
T1C0.500  (tool 1, diameter 0.5)
T2C1.000  (tool 2, diameter 1.0)
%
G90       (absolute coordinates)
G05       (drill mode)
T01
X1.0Y2.0  (drill hit)
X3.0Y4.0
T02
X5.0Y6.0
M30       (end of file)
```

Extended: G85 (slot), G41/G42 (cutter compensation), G93 (work zero).
