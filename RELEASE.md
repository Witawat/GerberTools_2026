# Release Notes — GerberTools 2026-06

---

## GerberViewer (v2.0)

### Layer Panel & Rendering — ViewMatePRO Parity

#### Layer Panel UI ใหม่
- **6 คอลัมน์**: `Vis` (✓), `Colour` (แถบสี), `File`, `Layer`, `Side`, `Alpha`
- **ปุ่มควบคุม**: `All On` / `All Off` / `Invert` / `Clear All` / `Save to PNG`
- แถบสีพื้นหลังแยกตาม **Board Side**:
  - `Top` — แดงเข้ม
  - `Bottom` — น้ำเงินเข้ม
  - `Both` — เขียวเข้ม
  - `Internal/Other` — เหลืองเข้ม
- Dark theme ทั้ง DataGridView (`#2D2D30`)
- `ReadOnly` + `EditProgrammatically` ป้องกัน edit mode รบกวนการแสดงผล

#### Per-Layer Transparency
- เพิ่ม property `Alpha` ใน `DisplayGerber` (default `1.0f`)
- คลิกที่คอลัมน์ **Alpha** เพื่อวนค่า: `100% → 80% → 60% → 40% → 20%`
- ใช้ alpha ต่อ layer ในการ render ทันที

#### LayerVisibilityForm (Right-Click Dialog)
- แก้ bug **dedup logic** — เมื่อมีหลายไฟล์ประเภทเดียวกัน layer ไม่หายอีกต่อไป
- เพิ่ม **TrackBar** ปรับ opacity (10–100%) แบบ real-time
- ปุ่ม `Invert` สำหรับสลับ selection
- แสดงค่า alpha บน label แต่ละแถว: `Copper [Top] α=80% filename.gbr`

#### Drawing Order (Z-Order)
```
Copper → Paste → SolderMask → Carbon → Silk
→ Assembly → Courtyard → Fab → Notes
→ Drill → Mill → Outline
```
- **Outline** อยู่บนสุดเสมอ
- **Mill** อยู่เหนือ regular layers
- **Drill** อยู่ระหว่าง notes กับ mill
- `GetDefaultSortOrder()` รองรับทุก layer type (16 ประเภท จากเดิม 6)

#### Rendering
- พื้นหลังเปลี่ยนจากดำสนิท `(0,0,0)` → เทาเข้ม `#2D2D30`
- Opposite-side layers แสดงด้วย alpha **25%** (แทนการ blend หายกับพื้นหลัง)
- Per-layer alpha ส่งผลต่อ rendering ทันที ไม่ต้อง reload

#### Bug Fixes
- `ColumnCount` 4 → 6 (เดิมตั้ง 4 แต่ข้อมูลมี 5+1 คอลัมน์ ทำให้ Side หาย)
- `CellPainting` เปลี่ยนจากอ่าน `e.Value` → อ่าน `Document.Gerbers[e.RowIndex].visible` โดยตรง
- ปุ่ม `All On/Off/Invert` อัปเดตค่า cell ทั้งหมด + `Refresh()` แทน `Invalidate()`
- `InvalidateCell()` หลังเปลี่ยนค่า Vis cell

---

## GerberPanelizer (v2.0)

### Stability & Workflow

- **Auto-resize canvas** เมื่อโหลดไฟล์ — ไม่ต้องปรับขนาดเอง
- **Numerical precision**: normalize Gerber origin ให้ตำแหน่งแม่นยำ หลีกเลี่ยง floating-point error
- **Zoom limits** — ป้องกันการซูมเกินขอบเขต
- **Debug log toggle** สำหรับตรวจสอบปัญหา
- **File association** — ดับเบิลคลิกไฟล์ Gerber ใน Windows Explorer เปิด GerberPanelizer ได้
- แก้ไข **menu bar bugs**, ปรับปรุง **box selection**
- แก้ **build warnings** ทั้งหมด 25 รายการ

---

## QuickGerberRender (v1.5)

### UI & Build

- Render output **อยู่กึ่งกลางหน้าต่าง** เสมอ
- `build_all.bat` เร็วขึ้น **~50 เท่า** (parallel build)
- **Interactive menu** สำหรับ build เฉพาะโปรเจกต์
- รองรับ `GerberTools.sln` สำหรับเปิดทั้ง workspace ใน IDE

---

## ทั่วทั้ง 3 โปรเจกต์

- Migrate csproj ทั้งหมด **38 ไฟล์** จาก old-style → **SDK-style** (targeting .NET Framework 4.8)
- เพิ่ม `build_all.ps1` (PowerShell build script)
- `KNOWLEDGE_BASE/` และ `README` ปรับปรุงใหม่
- GerberLibrary shared dependency อัปเดตพร้อมกันทุกโปรเจกต์
