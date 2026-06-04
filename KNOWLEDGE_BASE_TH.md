# GerberTools — ฐานความรู้ (Knowledge Base)

## ภาพรวม

**GerberTools** คือชุดเครื่องมือและไลบรารี 49 รายการ สำหรับการประมวลผลไฟล์ผลิต PCB (แผงวงจรพิมพ์), การจัดเรียงแผง (panelization), การเรนเดอร์ภาพ, และระบบอัตโนมัติในกระบวนการผลิต ทั้ง 38 โปรเจกต์ `.csproj` แบบดั้งเดิมได้ถูกย้ายเป็นรูปแบบ SDK-style แล้ว ทำให้ใช้ `dotnet build` ได้โดยไม่ต้องใช้ Visual Studio โปรเจกต์นี้มี 2 ยุค:

| ยุค | .NET | UI | ไลบรารี |
|-----|------|----|---------|
| **ดั้งเดิม** | .NET Framework 4.8 | Windows Forms + OpenTK | `GerberLibrary` (System.Drawing, DotNetZip) |
| **สมัยใหม่** | .NET 9.0 (ข้ามแพลตฟอร์ม) | Avalonia UI (MVVM) | `GerberLibrary.Core` (ImageSharp, SharpZipLib) |

กำลังอยู่ในระหว่างการย้ายจาก WinForms แบบดั้งเดิมไปยัง Avalonia สมัยใหม่ (ติดตามได้ใน `ProjectProgress/`)

---

## ไลบรารีหลัก (Core Libraries)

### GerberLibrary (`GerberLibrary/`)
- **ประเภท:** DLL, .NET Framework 4.8
- **หน้าที่:** ไลบรารีประมวลผล Gerber สำหรับ Windows เท่านั้น แยกวิเคราะห์ไฟล์ Gerber RS-274X, ไฟล์เจาะ Excellon เรนเดอร์ภาพ PCB ผ่าน System.Drawing รวม/ตัดรูปทรง Gerber มีอัลกอริทึมจัดเรียงแผง (MaxRects, RectanglePack) และโครงสร้างข้อมูล BOM สำหรับการประกอบ JLCPCB

### GerberLibrary.Core (`GerberLibrary.Core/`)
- **ประเภท:** DLL, .NET 9.0 (ข้ามแพลตฟอร์ม)
- **หน้าที่:** ไลบรารีตัวใหม่ทดแทน `GerberLibrary` ฟังก์ชันเหมือนเดิมแต่ใช้ SixLabors.ImageSharp สำหรับเรนเดอร์ และ SharpZipLib สำหรับจัดการไฟล์บีบอัด เครื่องมือใหม่ทั้งหมดควรใช้ไลบรารีนี้

### EagleLoaders (`EagleLoaders/`)
- **ประเภท:** DLL, .NET Framework 4.8
- **หน้าที่:** อ่านและเรนเดอร์ไฟล์ Eagle `.brd`/`.lbr` มีคลาสที่สร้างอัตโนมัติจาก Eagle XSD schema สามารถวาด PCB ที่ออกแบบด้วย Eagle เป็นภาพ Bitmap

### TilingLibrary.Core (`TilingLibrary.Core/`)
- **ประเภท:** DLL, .NET 9.0
- **หน้าที่:** เอ็นจิ้นคำนวณลายกระเบื้อง (tiling) และการแบ่งย่อยรูปทรงเรขาคณิต สำหรับสร้างลวดลายศิลปะ รองรับรูปแบบ tiling มากกว่า 15 แบบ (Penrose, Danzer 7-fold, Ammann-Beenker, Pinwheel, Sphinx, Conway, Chair ฯลฯ) รวมถึงโหมด QuadTree และ Delaunay triangulation

---

## เครื่องมือบรรทัดคำสั่ง (CLI Tools)

### GerberAnalyse (`GerberAnalyse/`)
- **ประเภท:** CLI (.NET 4.8)
- **หน้าที่:** วิเคราะห์ไฟล์ Gerber/Excellon แสดงขนาดบอร์ด (กว้าง x สูง), พิกัดมุม, จำนวนรูเจาะทั้งหมด รองรับไฟล์เดี่ยว, โฟลเดอร์, หรือ ZIP

### GerberClipper (`GerberClipper/`)
- **ประเภท:** CLI (.NET 4.8)
- **หน้าที่:** ตัดไฟล์ Gerber ด้วยขอบบอร์ด (outline) รับ outline + ไฟล์ที่จะตัด + ไฟล์ผลลัพธ์ เขียนเฉพาะรูปทรงที่อยู่ภายใน outline เท่านั้น

### GerberCombiner (`GerberCombiner/`)
- **ประเภท:** CLI (.NET 4.8)
- **หน้าที่:** รวมไฟล์ Gerber หลายไฟล์ หรือไฟล์ Excellon drill หลายไฟล์ เข้าเป็นไฟล์เดียว ตรวจจับประเภทไฟล์อัตโนมัติ

### GerberDebugger (`GerberDebugger/`)
- **ประเภท:** CLI (.NET 9.0)
- **หน้าที่:** เครื่องมือวินิจฉัยปัญหาหลากคำสั่ง มีคำสั่งย่อย: `validate`, `analyze`, `visualize`, `panel-validate`, `diff`, `fix`, `check-apertures`, `patterns` — เปรียบเสมือนมีดพับสำหรับแก้ปัญหา Gerber

### GerberMover (`GerberMover/`)
- **ประเภท:** CLI (.NET 4.8)
- **หน้าที่:** ย้ายตำแหน่ง, หมุน, และแปลงไฟล์ Gerber/Excellon รองรับระยะ X/Y, จุดหมุน, และมุมองศา ใช้ได้ทั้งไฟล์เดี่ยวและโฟลเดอร์ สร้างภาพ PNG ของผลลัพธ์

### GerberSanitize (`GerberSanitize/`)
- **ประเภท:** CLI (.NET 4.8)
- **หน้าที่:** ทำความสะอาด/ปรับรูปแบบไฟล์ Gerber RS-274X อ่านไฟล์, ส่งผ่าน `PolyLineSet.SanitizeInputLines()`, เขียนไฟล์ `<ชื่อไฟล์>.sanitized.txt`

### GerberSplitter (`GerberSplitter/`)
- **ประเภท:** CLI (.NET 9.0)
- **หน้าที่:** แยกชุด Gerber โดยใช้แม่แบบรูปหลายเหลี่ยม outline ("slice file") แต่ละรูปหลายเหลี่ยมจะสร้างโฟลเดอร์ย่อย (`Slice1/`, `Slice2/`, ...) พร้อมไฟล์ Gerber ที่ถูกตัดแล้ว

### GerberSubtract (`GerberSubtract/`)
- **ประเภท:** CLI (.NET 4.8)
- **หน้าที่:** ลบ aperture flash ที่ซ้อนทับจากไฟล์ Gerber ต้นทางด้วยไฟล์ Gerber ที่ใช้ลบ **คำเตือน:** เครื่องมือทดลอง — ผู้เขียนระบุว่า "อันตรายมาก" ควรตรวจสอบผลลัพธ์ด้วยตนเองเสมอ

### GerberToDxf (`GerberToDxf/`)
- **ประเภท:** CLI (.NET 9.0)
- **หน้าที่:** แปลง Gerber เป็นไฟล์ DXF สำหรับ CAD เชิงกล รองรับการแปลงทั้งโฟลเดอร์ มีตัวเลือก `-nooutline`, `-nodisplay`

### GerberToImage (`GerberToImage/`)
- **ประเภท:** CLI (.NET 9.0)
- **หน้าที่:** เรนเดอร์ชุดเลเยอร์ Gerber เป็นภาพ PNG คุณภาพสูง ตัวเลือก: `--dpi N`, `--noxray`, `--nopcb`, `--silk <สี>`, `--trace <สี>`, `--copper <สี>`, `--mask <สี>`

### GerberToOutline (`GerberToOutline/`)
- **ประเภท:** CLI (.NET 9.0)
- **หน้าที่:** แปลง Gerber/Excellon เป็นภาพ SVG แสดงขอบบอร์ด preview เปิด SVG ในเบราว์เซอร์อัตโนมัติ (บน Windows)

### AutoPanelBuilder (`AutoPanelBuilder/`)
- **ประเภท:** CLI (.NET 4.8)
- **หน้าที่:** จัดเรียง PCB เป็นแผงอัตโนมัติแบบ batch รับรายชื่อโฟลเดอร์ Gerber + ไฟล์ตั้งค่า XML, ใช้ MaxRectPack จัดเรียงบอร์ด, ใส่ auto-tabs (mousebites) ด้วย Delaunay triangulation, ส่งออก Gerber รวมและไฟล์ `.gerberset`

---

## แอปพลิเคชัน GUI

### GerberPanelizer (`GerberPanelizer/`)
- **ประเภท:** WinExe (.NET 4.8, Windows Forms + OpenTK)
- **หน้าที่:** **แอปพลิเคชันหลัก** โปรแกรมจัดเรียงแผง PCB แบบภาพเต็มรูปแบบ อินเทอร์เฟซ MDI พร้อมผืนผ้าใบเร่งด้วย OpenGL:
  - ลากและวาง instance บอร์ด, ซูม/เลื่อน, undo/redo (20 ระดับ)
  - เลือกหลายตัวด้วย box-select และ Ctrl+คลิก
  - โหมด snap: 1mm, 0.5mm, 100mil, 50mil, Off
  - เครื่องมือจัดเรียง: Align Left/Right/Top/Bottom, Center Horizontally/Vertically
  - จัดการ break-tab: ใส่, สร้างอัตโนมัติ, ลบทั้งหมด/ที่มีปัญหา, รวมที่ซ้อนทับ
  - อัลกอริทึมจัดเรียงอัตโนมัติ: Naive RectanglePack, MaxRects (พร้อมหมุน)
  - ส่งออก Gerber รวม, บันทึก/โหลดไฟล์โครงการ `.gerberset`
  - รายชื่อไฟล์ล่าสุด, ลากและวางแผงจาก Explorer
  - ตัวแก้ไขคุณสมบัติ instance, มุมมองต้นไม้, กล่องโต้ตอบ autofit canvas

### GerberViewer (`GerberViewer/`)
- **ประเภท:** WinExe (.NET 4.8, Windows Forms + OpenTK)
- **หน้าที่:** โปรแกรมดูไฟล์ Gerber เร่งด้วย OpenGL อินเทอร์เฟซแบบ dockable (DockPanelSuite) พร้อมมุมมองบอร์ดด้านบน/ล่างเคียงข้างกัน แผงรายชื่อเลเยอร์สำหรับเปิด/ปิดการแสดงผลแต่ละเลเยอร์

### QuickGerberRender (`QuickGerberRender/`)
- **ประเภท:** WinExe (.NET 4.8, Windows Forms)
- **หน้าที่:** โปรแกรมลากและวางแปลง Gerber เป็นภาพ PNG น้ำหนักเบา วางโฟลเดอร์ Gerber เลือกสี (solder mask, silk, copper, traces) ตั้งค่า DPI ได้ภาพ PNG ทันที มีตัวเลือก X-Ray และ PCB overlay

### GerberDrop (`GerberDrop/`)
- **ประเภท:** WinExe (.NET 9.0, Avalonia UI)
- **หน้าที่:** ตัวทดแทน QuickGerberRender แบบข้ามแพลตฟอร์ม ฟังก์ชันเดียวกัน (ลากวาง Gerber → PNG) แต่ใช้สถาปัตยกรรม MVVM, แสดงตัวอย่างสีก่อนเรนเดอร์, และ UI แบบ Avalonia

### JLCDrop (`JLCDrop/`)
- **ประเภท:** WinExe + CLI (.NET 4.8, Windows Forms)
- **หน้าที่:** โปรแกรมแพ็คเกจสำหรับส่งผลิต JLCPCB สองโหมด ลากวางโฟลเดอร์ KiCad/Eagle/Diptrace สร้างไฟล์ BOM + Pick-and-Place + ZIP Gerber พร้อมอัปโหลด JLCPCB โดยอัตโนมัติ มีโหมด CLI ด้วยตัวเลือก `-zip`/`-dontzip`, `-frame`

### VScorePanel (`VScorePanel/`)
- **ประเภท:** WinExe (.NET 4.8, Windows Forms)
- **หน้าที่:** โปรแกรมลากและวางสร้างแผง PCB สองโหมด:
  - **"Tabby"** — break-tabs (mousebites) ระหว่างบอร์ดพร้อมกรอบแผง
  - **"Groovy"** — เส้น V-score/V-groove สำหรับหักแยก
  - ตั้งค่ากริด (X×Y), ระยะห่าง, ความกว้างกรอบ, ข้อความชื่อแผง สร้าง Gerber รวม, ไฟล์กรอบ, และไฟล์ jig (ถ้ามี)

### PnP_Processor (`PnP_Processor/`)
- **ประเภท:** WinExe + CLI (.NET 4.8, Windows Forms + DockPanelSuite)
- **หน้าที่:** โปรแกรมประมวลผล Pick-and-Place สำหรับการประกอบ PCB โหลด BOM, ไฟล์ตำแหน่ง PnP, ZIP Gerber, และไฟล์สต็อก (ไม่บังคับ) ตรวจสอบตำแหน่งชิ้นส่วนกับบอร์ด รองรับการพลิกบอร์ด (none/diagonal/horizontal) สำหรับการประกอบสองด้าน GUI แบบ dockable พร้อมแสดงบอร์ด, รายการ BOM แบบค้นหาได้, และแผง actions

### SolderTool (`SolderTool/`)
- **ประเภท:** WinExe (.NET 4.8, Windows Forms + DockPanelSuite)
- **หน้าที่:** ผู้ช่วยในการบัดกรีด้วยมือ โหลดข้อมูล BOM + ตำแหน่งจาก ZIP Gerber แสดงแผนที่ชิ้นส่วนบนบอร์ด ติดตามสถานะบัดกรีแล้ว/ยังด้วยแป้นพิมพ์ (Up/Down + Enter) แสดงความคืบหน้าเป็นจำนวนที่บัดกรีแล้ว/ทั้งหมด อินเทอร์เฟซแบบหลายเอกสาร (tabbed)

### CaseBuilder (`CaseBuilder/`)
- **ประเภท:** WinExe (.NET 4.8, Windows Forms)
- **หน้าที่:** สร้างไฟล์ DXF สำหรับกล่อง enclosure ตัดเลเซอร์จาก Gerber PCB ("Sick Of Beige") ลากวางโฟลเดอร์ Gerber, ปรับระยะ offset และขนาดรูยึด, เปิด DXF สำหรับกล่อง enclosure โดยอัตโนมัติ

### EagleBoardToCHeader (`EagleBoardToCHeader/`)
- **ประเภท:** WinExe (.NET 4.8, Windows Forms)
- **หน้าที่:** แปลงไฟล์ Eagle `.brd` เป็นไฟล์ C/C++ header สกัดข้อมูลตำแหน่งชิ้นส่วน (x, y, การหมุน, ชื่อ, ไลบรารี, แพ็กเกจ, ค่า) และสร้างอาร์เรย์ C-header สำหรับอ้างอิงในเฟิร์มแวร์

### FrontPanelBuilder (`FrontPanelBuilder/`)
- **ประเภท:** WinExe (.NET 4.8, Windows Forms)
- **หน้าที่:** สร้างไฟล์ Gerber สำหรับแผงหน้า (front panel) — โดยทั่วไปใช้กับโมดูล Eurorack/synth ประมวลผล PCB silkscreen + ภาพ PNG ซ้อนทับ (เลเยอร์ silkscreen และ gold) เพื่อผลิตไฟล์ผลิตแผงหน้า รองรับทั้งแผงด้านบนและด้านล่าง

### FitBitmapToOutlineAndMerge (`FitBitmapToOutlineAndMerge/`)
- **ประเภท:** WinExe (.NET 4.8, Windows Forms)
- **หน้าที่:** รวมภาพบิตแมพ (โลโก้/กราฟิก) ลงบน PCB silkscreen ภายในขอบบอร์ด เลือกบิตแมพ, Gerber outline, และ Gerber silkscreen; เครื่องมือจะปรับขนาดบิตแมพให้พอดีและรวมลงบนเลเยอร์ silkscreen

---

## เครื่องมือสร้างแบบ PCB (PCB Design Generators)

### AntennaBuilder (`AntennaBuilder/`)
- **ประเภท:** CLI (.NET 4.8)
- **หน้าที่:** สร้างไฟล์ Gerber + Excellon drill สำหรับ PCB เสาอากาศ NFC ผลิตบอร์ดสี่เหลี่ยมพร้อมรอยทองแดงขดเป็นเกลียวหลายรอบบนชั้นทองแดงทั้งด้านบนและล่าง, vias, รูยึด, solder mask, silkscreen, และ outline

### ProtoBoardGenerator (`ProtoBoardGenerator/`)
- **ประเภท:** CLI (.NET 4.8)
- **หน้าที่:** สร้างไฟล์ Gerber สำหรับ PCB ต้นแบบ/แผงทดลอง (perfboard) สองสไตล์: แบบกริดมาตรฐาน และแบบ "ดอกไม้" ตั้งค่าความกว้าง, ความสูง, รูยึด, การมนมุม, ระยะห่าง pad ได้

### ProductionFrame (`ProductionFrame/`)
- **ประเภท:** WinExe (.NET 4.8, Windows Forms)
- **หน้าที่:** GUI สำหรับสร้างไฟล์ Gerber กรอบ/แผงการผลิต พร้อมระยะขอบ, รูยึด, และ fiducial markers ส่งออกเลเยอร์มาตรฐานทั้งหมด (outline, silkscreen, copper, solder mask, drill)

### LightPipeBuilder (`LightPipeBuilder/`)
- **ประเภท:** CLI (.NET 4.8)
- **หน้าที่:** อ่านไฟล์ KiCad PCB และสร้างไฟล์ OpenSCAD `.scad` สำหรับท่อนำแสงที่พิมพ์ 3 มิติ ค้นหาชิ้นส่วน LED_0603 และ LightpipeHole footprint ส่งออกโมดูล SCAD ที่พิกัดบอร์ดพร้อมตารางตำแหน่ง CSV

---

## เครื่องมือศิลปะ / Tiling (แบรนด์ TINRS — "This Is Not Rocket Science")

### TINRS-ArtWorkGenerator (`TINRS-ArtWorkGenerator/`)
- **ประเภท:** WinExe (.NET 4.8, Windows Forms)
- **หน้าที่:** **GUI รุ่นดั้งเดิม** สำหรับสร้างลวดลายเรขาคณิตจากภาพหน้ากาก (mask bitmap) ใช้ไลบรารี `TINRS-ArtWork` รุ่นเก่า โหลดภาพหน้ากาก, ตั้งค่าประเภท tiling/ความลึก/เกณฑ์/สมมาตร, ส่งออกเป็น SVG

### TiNRS-Tiler (`TiNRS-Tiler/`)
- **ประเภท:** WinExe (.NET 9.0, Avalonia UI)
- **หน้าที่:** **ตัวทดแทนสมัยใหม่** สำหรับ TINRS-ArtWorkGenerator GUI แบบข้ามแพลตฟอร์มด้วย Avalonia และ MVVM pattern สร้างลวดลายเรขาคณิตเช่นเดียวกัน ด้วยระบบ reactive properties, การเรนเดอร์เบื้องหลัง, file watcher, และ debounced auto-update

### ImageToGerber (`ImageToGerber/`)
- **ประเภท:** CLI (.NET 4.8)
- **หน้าที่:** แปลงภาพ PNG (silkscreen + ลายทองแดง) เป็นไฟล์ Gerber สำหรับผลิตแผงหน้า อ่าน board outline, จับคู่ภาพซ้อน `*Silk.png` และ `*Gold.png`, ส่งออกเลเยอร์ `.gto`, `.gbl`/`.gtl`, `.gbs`/`.gts`

### MakeIcon (`MakeIcon/`)
- **ประเภท:** CLI (.NET 4.8)
- **หน้าที่:** สร้างไฟล์ `.ico` หลายความละเอียดโดยใช้เอ็นจิ้นศิลปะเรขาคณิตของ TilingLibrary ใช้สำหรับไอคอนแบรนด์/UI

### IconBuilder (`Project_Utilities/IconBuilder/`)
- **ประเภท:** WinExe (.NET 4.8, Windows Forms)
- **หน้าที่:** GUI สำหรับออกแบบและดูตัวอย่างไอคอนแบบโต้ตอบ เรนเดอร์ด้วย TilingLibrary artwork แสดงหลาย IconFrame ในขนาดต่างๆ

### IconScanner (`Project_Utilities/IconScanner/`)
- **ประเภท:** CLI (.NET 4.8)
- **หน้าที่:** สร้างไอคอนแบบ batch สแกนโฟลเดอร์ (หรือไฟล์ `.svgsort`/`.svgs`), เก็บชื่อ, สร้างไฟล์ `.ico` หลายความละเอียดพร้อมแต้มสีไม่ซ้ำกัน

---

## ชุดเครื่องมือ DirtyPCBs

ชุดเครื่องมือ CLI 6 รายการสำหรับบริการผลิต DirtyPCBs:

| เครื่องมือ | หน้าที่ |
|-----------|---------|
| **SickOfBeige** | CLI สร้าง DXF กล่อง enclosure จาก Gerber (ปรับ offset + ขนาดรูได้) |
| **BoardStats** | ดึงขนาด PCB + จำนวนรูเจาะ เป็น JSON |
| **BoardRender** | เรนเดอร์ภาพพรีวิว PCB พร้อมสีที่ปรับแต่งได้ (สำหรับแสดงบนเว็บ) |
| **DXFStats** | ดึงความยาวเส้นรวม + ขอบเขตจากไฟล์ DXF |
| **Base64Extractor** | ถอดรหัสอีเมล Base64 เป็น ZIP |
| **LocaleTest** | ดีบักปัญหาการแยกวิเคราะห์พิกัด Gerber จาก locale |

---

## เครื่องมือสนับสนุนการผลิต

### BOMConsolidator (`BOMConsolidator/`)
- **ประเภท:** CLI (.NET 4.8)
- **หน้าที่:** รวมและย่อไฟล์ BOM รูปแบบ JLCPCB รับเส้นทางไฟล์ BOM + CPL ส่งออก BOM แบบย่อ `*_newcondens`

---

## โครงสร้างพื้นฐานและยูทิลิตี้

### Project_Utilities
- **TilingLibrary (TINRS-ArtWork)** — ไลบรารี artwork รุ่น .NET 4.8 ดั้งเดิม ใช้โดย TINRS-ArtWorkGenerator, MakeIcon, IconBuilder, IconScanner
- **ReleaseBuilder** — CLI รวบรวมไฟล์ build output เป็น ZIP พร้อม timestamp
- **IconBuilder** / **IconScanner** — เครื่องมือออกแบบและสร้างไอคอนแบบ batch (ดูด้านบน)

### ProjectProgress (`ProjectProgress/`)
- **ประเภท:** โฟลเดอร์เอกสาร (ไม่ใช่ executable)
- **หน้าที่:** ติดตามการย้าย TiNRS-Tiler จาก .NET 4.8 WinForms → .NET 9.0 Avalonia UI มีรายการตรวจสอบการย้าย, เอกสารสถาปัตยกรรม, คู่มือเริ่มต้น, และตัวอย่างรูปแบบโค้ด

### Tests (`Tests/`)
- **ประเภท:** CLI (.NET 9.0)
- **หน้าที่:** ตัวสร้างไฟล์ทดสอบสังเคราะห์ (`GerberTools.TestGenerator`) สร้างบอร์ดมาตรฐาน 100×100mm พร้อม outline, copper บน/ล่าง, silk, และไฟล์ drill เป็น Gerber X2/Excellon ASCII สำหรับทดสอบเครื่องมืออื่น

### Build Scripts
- `build_all.ps1` — สคริปต์ PowerShell สำหรับบิวด์ทั้งหมด ~46 โปรเจกต์ (ข้าม net9.0 หากไม่มี SDK), คัดลอก GUI ไปที่ `Build/Output/<ชื่อ>/` และ CLI ไปที่ `CommandLine/` วิธีใช้: `.\build_all.ps1` หรือ `.\build_all.ps1 -Config Release`
- `build_all.bat` — สคริปต์ batch รุ่นเก่า (เลิกใช้แล้ว; ใช้ build_all.ps1 แทน)
- `build.sh` — สำหรับ Linux
- `build_QuickRender.bat` — บิวด์เฉพาะ QuickRender
- `BuildRelease.bat` — บิวด์ release เต็มรูปแบบ

ทุกโปรเจกต์ใช้รูปแบบ SDK-style csproj — สามารถบิวด์ด้วย `dotnet build <โปรเจกต์>.csproj` โดยไม่ต้องใช้ Visual Studio

### ButtonsAndIcons (`ButtonsAndIcons/`)
- **ประเภท:** โฟลเดอร์ทรัพยากร (ไม่ใช่โปรเจกต์)
- **หน้าที่:** ภาพไอคอนปุ่ม 16 ไฟล์ JPG (Up, Down, Left, Right, RotateLeft, RotateRight, ScaleUp, ScaleDown + แบบ hover) ใช้โดยแอป GUI

### CarvedOut (`CarvedOut/`)
- **ประเภท:** WinExe (VB.NET, .NET 5.0) — **โครงเปล่า**
- **หน้าที่:** placeholder Windows Forms app ยังไม่มีการพัฒนาใดๆ

### MigrationTest (`MigrationTest/`)
- **ประเภท:** CLI (.NET 9.0)
- **หน้าที่:** ทดสอบควัน (smoke test) สำหรับการย้าย GerberLibrary → GerberLibrary.Core สร้างไฟล์ Gerber/drill ทดสอบ, เรียก `ZipGerberFolderToFactoryFolder()`, ตรวจสอบว่า SharpZipLib อ่าน ZIP กลับได้

---

## ข้อสังเกตทางสถาปัตยกรรม

1. **ไลบรารีสองรุ่นอยู่ร่วมกัน:**
   - `GerberLibrary` (.NET 4.8) → ใช้โดยแอป WinForms รุ่นเก่าทั้งหมด
   - `GerberLibrary.Core` (.NET 9.0) → ใช้โดยเครื่องมือ Avalonia/CLI ใหม่
   - หลายโปรเจกต์ยังต้องย้ายจากไลบรารีเก่าไปใหม่

2. **การย้าย UI กำลังดำเนินการ:**
   - รุ่นเก่า: Windows Forms + OpenTK + DockPanelSuite
   - รุ่นใหม่: Avalonia UI + MVVM (CommunityToolkit.Mvvm)
   - ตัวอย่าง: `QuickGerberRender` → `GerberDrop`, `TINRS-ArtWorkGenerator` → `TiNRS-Tiler`

3. **รูปแบบการพึ่งพา:**
   - การเรนเดอร์/ประมวลผลทั้งหมดผ่าน `GerberLibrary.Core.Gerber` (หรือ `GerberLibrary.Gerber`)
   - ClipperLib และ Triangle ใช้ทั่วทั้งระบบสำหรับการตัดและสามเหลี่ยมรูปหลายเหลี่ยม
   - ไลบรารีภาพ: System.Drawing (รุ่นเก่า) vs SixLabors.ImageSharp (รุ่นใหม่)

4. **เครื่องมือแบรนด์ TINRS:**
   - "This Is Not Rocket Science" — การออกแบบ PCB เชิงศิลปะ
   - `TiNRS-Tiler` / `TINRS-ArtWorkGenerator` — เครื่องมือสร้างลวดลายเรขาคณิต
   - `TilingLibrary.Core` — เอ็นจิ้น tiling ข้ามแพลตฟอร์ม
   - MakeIcon / IconBuilder / IconScanner — สร้างไอคอนจากลาย tiling

---

## ตารางอ้างอิง: ทุกโปรเจกต์

| # | โฟลเดอร์ | ชื่อ | ประเภท | .NET | หน้าที่ |
|---|---------|------|--------|------|---------|
| 1 | GerberLibrary | GerberLibrary | Library | 4.8 | ไลบรารีหลักประมวลผล Gerber/PCB (Windows) |
| 2 | GerberLibrary.Core | GerberLibrary.Core | Library | 9.0 | ไลบรารีหลักประมวลผล Gerber/PCB (ข้ามแพลตฟอร์ม) |
| 3 | EagleLoaders | EagleLoaders | Library | 4.8 | ตัวอ่านและเรนเดอร์ไฟล์ Eagle BRD/LBR |
| 4 | TilingLibrary.Core | TilingLibrary.Core | Library | 9.0 | เอ็นจิ้นลายเรขาคณิต tiling |
| 5 | GerberPanelizer | GerberPanelizer | WinExe | 4.8 | โปรแกรมจัดเรียงแผง PCB แบบภาพ (ตัวหลัก) |
| 6 | GerberViewer | GerberViewer | WinExe | 4.8 | โปรแกรมดูเลเยอร์ Gerber แบบ OpenGL |
| 7 | QuickGerberRender | QuickGerberRender | WinExe | 4.8 | ลากวาง Gerber → PNG |
| 8 | GerberDrop | GerberDrop | WinExe | 9.0 | Avalonia ลากวาง Gerber → PNG |
| 9 | JLCDrop | JLCDrop | WinExe | 4.8 | แพ็คเกจสำหรับ JLCPCB (BOM+Gerber ZIP) |
| 10 | VScorePanel | FrameDrop | WinExe | 4.8 | สร้างแผง PCB (tabs + V-score) |
| 11 | PnP_Processor | PnP_Processor | WinExe | 4.8 | โปรแกรมประมวลผล Pick-and-Place |
| 12 | SolderTool | SolderTool | WinExe | 4.8 | ผู้ช่วยบัดกรีด้วยมือ |
| 13 | CaseBuilder | CaseBuilder | WinExe | 4.8 | สร้าง DXF enclosure |
| 14 | EagleBoardToCHeader | EagleBoardToCHeader | WinExe | 4.8 | แปลง Eagle → C header |
| 15 | FrontPanelBuilder | FrontPanelBuilder | WinExe | 4.8 | สร้าง Gerber แผงหน้า |
| 16 | FitBitmapToOutlineAndMerge | FitBitmapToOutlineAndMerge | WinExe | 4.8 | รวมภาพบิตแมพ → PCB silkscreen |
| 17 | ProductionFrame | ProductionFrame | WinExe | 4.8 | สร้างกรอบแผงผลิต |
| 18 | TINRS-ArtWorkGenerator | TINRS-ArtWorkGenerator | WinExe | 4.8 | สร้างลาย tiling (รุ่นดั้งเดิม) |
| 19 | TiNRS-Tiler | TiNRS.Tiler | WinExe | 9.0 | สร้างลาย tiling (Avalonia) |
| 20 | IconBuilder | IconBuilder | WinExe | 4.8 | ออกแบบไอคอนแบบโต้ตอบ |
| 21 | CarvedOut | CarvedOut | WinExe | 5.0 | โครงเปล่า VB.NET |
| 22 | GerberAnalyse | GerberAnalyse | Exe | 4.8 | วิเคราะห์สถิติ Gerber/drill |
| 23 | GerberClipper | GerberClipper | Exe | 4.8 | ตัด Gerber ด้วย outline |
| 24 | GerberCombiner | GerberCombiner | Exe | 4.8 | รวมไฟล์ Gerber/drill หลายไฟล์ |
| 25 | GerberDebugger | GerberDebugger | Exe | 9.0 | เครื่องมือดีบักหลายคำสั่ง |
| 26 | GerberMover | GerberMover | Exe | 4.8 | ย้าย/หมุนไฟล์ Gerber |
| 27 | GerberSanitize | GerberSanitize | Exe | 4.8 | ทำความสะอาดรูปแบบไฟล์ Gerber |
| 28 | GerberSplitter | GerberSplitter | Exe | 9.0 | แยก Gerber ด้วยรูปหลายเหลี่ยม outline |
| 29 | GerberSubtract | GerberSubtract | Exe | 4.8 | ลบคุณลักษณะ Gerber (ทดลอง) |
| 30 | GerberToDxf | GerberToDxf | Exe | 9.0 | แปลง Gerber → DXF |
| 31 | GerberToImage | GerberToImage | Exe | 9.0 | เรนเดอร์ Gerber → PNG |
| 32 | GerberToOutline | GerberToOutline | Exe | 9.0 | แปลง Gerber → SVG outline |
| 33 | AutoPanelBuilder | AutoPanelBuilder | Exe | 4.8 | จัดเรียงแผงอัตโนมัติแบบ batch |
| 34 | AntennaBuilder | NFCAntennaBuilder | Exe | 4.8 | สร้าง Gerber เสาอากาศ NFC |
| 35 | ProtoBoardGenerator | ProtoBoardGenerator | Exe | 4.8 | สร้าง Gerber แผงทดลอง |
| 36 | LightPipeBuilder | LightPipeBuilder | Exe | 4.8 | KiCad → OpenSCAD ท่อนำแสง |
| 37 | ImageToGerber | ImageToGerber | Exe | 4.8 | PNG → Gerber แผงหน้า |
| 38 | MakeIcon | MakeIcon | Exe | 4.8 | สร้างไอคอนจากลาย tiling |
| 39 | IconScanner | IconScanner | Exe | 4.8 | สร้างไอคอนแบบ batch |
| 40 | BOMConsolidator | BOMConsolidator | Exe | 4.8 | รวม BOM ของ JLCPCB |
| 41 | SickOfBeige | SickOfBeige | Exe | 4.8 | CLI สร้าง DXF enclosure |
| 42 | BoardStats | BoardStats | Exe | 4.8 | ขนาด PCB + จำนวนรู JSON |
| 43 | BoardRender | BoardRender | Exe | 4.8 | เรนเดอร์ภาพพรีวิว PCB |
| 44 | DXFStats | DXFStats | Exe | 4.8 | วิเคราะห์ความยาวเส้น DXF |
| 45 | Base64Extractor | Base64Extractor | Exe | 4.8 | ถอดอีเมล Base64 → ZIP |
| 46 | LocaleTest | LocaleTest | Exe | 4.8 | เครื่องมือดีบัก locale |
| 47 | ReleaseBuilder | ReleaseBuilder | Exe | 4.8 | รวบรวม build output → ZIP |
| 48 | MigrationTest | MigrationTest | Exe | 9.0 | ทดสอบการย้ายไลบรารี |
| 49 | Tests | TestGenerator | Exe | 9.0 | สร้างไฟล์ Gerber ทดสอบสังเคราะห์ |
