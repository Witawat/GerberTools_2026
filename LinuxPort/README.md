# GerberTools LinuxPort

Mono port ของ GerberPanelizer, QuickGerberRender, และ GerberViewer สำหรับ Linux

**ไม่มีการแก้ไขไฟล์ต้นฉบับ** — ทุกการเปลี่ยนแปลงอยู่ในโฟลเดอร์นี้เท่านั้น (ก็อปปี้มาจากต้นฉบับแล้วแก้ในนี้)

---

## โครงสร้าง

```
LinuxPort/
├── GerberLibrary/            ← Core library (ก็อป, แก้ unused WinForms ref)
├── EagleLoaders/             ← Eagle PCB loader (ก็อป, ไม่ได้แก้)
├── GerberPanelizer/          ← ตัวหลัก 1 (ก็อป, แก้ 4 จุด)
│   └── QuickFont/            ← OpenGL font subsystem (ก็อป, ไม่ได้แก้)
├── GerberViewer/             ← ตัวหลัก 2 (ก็อป, ไม่ได้แก้ DockPanelSuite + OpenTK)
├── QuickGerberRender/        ← ตัวหลัก 3 (ก็อป, ไม่ได้แก้ — ใช้ได้เลย)
├── LinuxPort.sln             ← Solution file สำหรับ build ทั้งหมด
├── build.sh                  ← Linux build script
├── build.ps1                 ← Windows PowerShell build script
├── build.bat                 ← Windows batch (เรียก build.ps1)
└── README.md
```

---

## สิ่งที่แตกต่างจากต้นฉบับ

| การเปลี่ยนแปลง | ไฟล์ |
|---------------|------|
| `FileAssociation` ใช้ runtime check `Environment.OSVersion.Platform` — บน Linux ข้าม Registry + shell32 | `GerberPanelizer/FileAssociation.cs` |
| `explorer.exe` → `xdg-open` สำหรับเปิดโฟลเดอร์ | `GerberPanelizer/GerberPanelize.cs` |
| `Graphics.FromHwnd(IntPtr.Zero)` → DPI fallback เป็น 96 บน Linux | `GerberPanelizer/GerberPanelize.cs` |
| `GerberViewer.exe` hardcoded path → เปลี่ยนตามแพลตฟอร์ม | `GerberPanelizer/GerberPanelize.cs` |
| `using Microsoft.Win32` ที่ไม่ได้ใช้ → ลบออก | `GerberPanelizer/GerberPanelizerParent.cs` |
| `using System.Windows.Forms` ที่ไม่ได้ใช้ → ลบออก | `GerberLibrary/Core/GerberMerger.cs` |

โค้ดทั้งหมดใช้ **runtime check** (`Environment.OSVersion.Platform`) ไม่ใช่ `#if LINUX` — หมายความว่า **binary เดียวกันใช้ได้ทั้ง Windows และ Linux**

---

## ความต้องการ

### Linux (Runtime)
```bash
sudo apt install mono-complete libgdiplus libgl1-mesa-glx libopenal1
```

### Linux (Build Tool)
```bash
sudo apt install dotnet-sdk-8.0
```

### Windows
```
- .NET SDK 8.0+
- ไม่ต้องติดตั้งอะไรเพิ่ม
```

---

## Build

### Linux
```bash
cd LinuxPort
chmod +x build.sh

# Debug
./build.sh

# Release
./build.sh Release
```

### Windows
```powershell
# PowerShell
.\build.ps1

# หรือ cmd
.\build.bat
```

### หรือใช้ `dotnet build` โดยตรง (Mac/Linux/Windows)
```bash
dotnet build LinuxPort.sln
```

---

## Output

หลัง build ไฟล์จะไปรวมอยู่ที่:

```
LinuxPort/
├── Output/
│   ├── QuickGerberRender/    ← GUI apps
│   ├── GerberViewer/
│   ├── GerberPanelizer/
│   └── CommandLine/           ← CLI tools
└── Combined/                  ← 3 ตัวหลักรวมกัน
```

---

## วิธีรัน

### Linux (ต้องมี Mono)
```bash
cd LinuxPort
mono Output/QuickGerberRender/QuickGerberRender.exe
mono Output/GerberViewer/GerberViewer.exe
mono Output/GerberPanelizer/GerberPanelizer.exe
```

### Windows
```
.\Output\GerberPanelizer\GerberPanelizer.exe
```

---

## รายละเอียดแต่ละโปรเจกต์

| โปรเจกต์ | ประเภท | Dependencies | หมายเหตุ |
|----------|--------|-------------|----------|
| GerberLibrary | Library | DotNetZip, ExcelLibrary, Triangle | Core library สำหรับอ่าน/เขียน Gerber |
| EagleLoaders | Library | GerberLibrary | โหลดไฟล์ Eagle CAD |
| QuickGerberRender | GUI (WinForms) | GerberLibrary | ลาก Gerber → วาง → render PNG |
| GerberViewer | GUI (WinForms + OpenTK) | GerberLibrary, DockPanelSuite, OpenTK | ดู Gerber layer พร้อม dock UI |
| GerberPanelizer | GUI (WinForms + OpenTK) | GerberLibrary, OpenTK, QuickFont, FarseerPhysics | Panelizing แบบ visual |

---

## จุดเสี่ยงที่ต้องทดสอบบน Linux จริง

| จุดเสี่ยง | โปรเจกต์ | อาการที่อาจเจอ |
|-----------|----------|---------------|
| **DockPanelSuite** | GerberViewer | WinForms docking library — Mono รองรับบางส่วน อาจมี UI glitch |
| **OpenTK.GLControl** | GerberPanelizer, GerberViewer | GL rendering บน X11/Wayland — ต้องมี Mesa drivers |
| **QuickFont** | GerberPanelizer | System.Drawing + OpenGL texture — ต้อง libgdiplus |
| **libgdiplus** | ทั้งหมด | System.Drawing rendering — คุณภาพอาจต่างจาก GDI+ จริง |
| **File dialogs** | ทั้งหมด | Mono ใช้ GTK# wrapper — ลักษณะ UI อาจต่างกัน |
