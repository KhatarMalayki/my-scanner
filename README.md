# BlindScanner

**BlindScanner** adalah aplikasi native Windows yang memungkinkan Anda untuk mengakses scanner USB yang terhubung ke komputer Windows melalui antarmuka web, mirip dengan shared printer. Aplikasi ini dirancang untuk memberikan akses scanning ke seluruh jaringan lokal dengan fitur OCR multi-bahasa offline.

## 🚀 Fitur

- ✅ **Deteksi Otomatis Scanner USB** - Mendeteksi semua perangkat scanner yang terhubung via WIA
- ✅ **Web Interface** - Antarmuka web modern yang mudah digunakan
- ✅ **Multi-Scanner Support** - Kelola beberapa scanner sekaligus
- ✅ **Job Queue** - Antrian scan dengan dukungan concurrent jobs
- ✅ **Flexible Scan Settings** - Atur DPI, color mode, duplex
- ✅ **OCR Multi-Bahasa** - Tesseract OCR dengan dukungan berbagai bahasa (offline)
- ✅ **PDF Generation** - Konversi hasil scan ke PDF
- ✅ **Shared Folder Support** - Simpan hasil scan ke folder bersama (SMB)
- ✅ **System Tray** - Berjalan di background dengan icon di system tray
- ✅ **No Internet Required** - Semua proses berjalan offline

## 📋 Persyaratan Sistem

- **OS**: Windows 10 atau lebih baru
- **.NET**: .NET 6.0 Runtime atau lebih tinggi
- **Scanner**: Scanner USB yang mendukung WIA (Windows Image Acquisition)
- **RAM**: Minimal 2GB (4GB direkomendasikan untuk OCR)
- **Disk Space**: 500MB untuk aplikasi + 100MB-1GB untuk tessdata (bahasa OCR)

## 🛠️ Instalasi

### 1. Install .NET 6.0 Runtime (jika belum terinstall)

Download dan install dari: https://dotnet.microsoft.com/download/dotnet/6.0

Atau jalankan command berikut di PowerShell (sebagai Administrator):
```powershell
winget install Microsoft.DotNet.Runtime.6
```

### 2. Clone atau Download Project

```bash
git clone <repository-url>
cd BlindScanner
```

### 3. Restore Dependencies dan Build

```bash
dotnet restore
dotnet build --configuration Release
```

### 4. Download Tessdata (File Bahasa OCR)

File bahasa OCR Tesseract perlu diunduh secara terpisah. Download dari repository resmi Tesseract:

https://github.com/tesseract-ocr/tessdata

**Bahasa yang umum digunakan:**
- `eng.traineddata` - English
- `ind.traineddata` - Indonesian
- `fra.traineddata` - French
- `deu.traineddata` - German
- `spa.traineddata` - Spanish
- `chi_sim.traineddata` - Chinese Simplified
- `jpn.traineddata` - Japanese
- `kor.traineddata` - Korean

**Cara mengunduh:**

1. Buat folder `tessdata` di dalam folder output aplikasi:
   ```
   BlindScanner\BlindScanner.ServiceHost\bin\Release\net6.0-windows\tessdata\
   ```

2. Download file `.traineddata` yang Anda butuhkan dari link di atas

3. Salin file tersebut ke folder `tessdata` yang telah dibuat

**Contoh dengan PowerShell:**
```powershell
# Buat folder tessdata
mkdir .\BlindScanner.ServiceHost\bin\Release\net6.0-windows\tessdata

# Download bahasa Inggris
Invoke-WebRequest -Uri "https://github.com/tesseract-ocr/tessdata/raw/main/eng.traineddata" -OutFile ".\BlindScanner.ServiceHost\bin\Release\net6.0-windows\tessdata\eng.traineddata"

# Download bahasa Indonesia
Invoke-WebRequest -Uri "https://github.com/tesseract-ocr/tessdata/raw/main/ind.traineddata" -OutFile ".\BlindScanner.ServiceHost\bin\Release\net6.0-windows\tessdata\ind.traineddata"
```

### 5. Konfigurasi (Opsional)

Edit file `appsettings.json` yang akan dibuat otomatis saat pertama kali aplikasi dijalankan, atau buat manual:

```json
{
  "OutputFolder": "C:\\Users\\YourUsername\\Documents\\BlindScanner\\Output",
  "SharedFolder": "\\\\SERVER\\SharedScans",
  "TessdataPath": "C:\\Path\\To\\BlindScanner\\tessdata",
  "DefaultOcrLanguage": "eng",
  "WebServerPort": 8080,
  "DefaultDpi": 300,
  "AutoSaveToSharedFolder": false,
  "EnableOcr": true,
  "EnableTextToSpeech": false,
  "MaxConcurrentJobs": 3
}
```

**Penjelasan Setting:**
- `OutputFolder`: Folder lokal untuk menyimpan hasil scan
- `SharedFolder`: Path UNC ke folder bersama di jaringan (opsional)
- `TessdataPath`: Path ke folder tessdata
- `DefaultOcrLanguage`: Bahasa OCR default (eng, ind, dll)
- `WebServerPort`: Port untuk web server (default: 8080)
- `DefaultDpi`: DPI default untuk scanning (300 recommended)
- `AutoSaveToSharedFolder`: Otomatis copy hasil scan ke shared folder
- `EnableOcr`: Aktifkan/nonaktifkan OCR
- `EnableTextToSpeech`: Aktifkan/nonaktifkan TTS (belum diimplementasi)
- `MaxConcurrentJobs`: Maksimal scan job yang berjalan bersamaan

## 🚀 Menjalankan Aplikasi

### Mode Development

```bash
cd BlindScanner.ServiceHost
dotnet run
```

### Mode Production (dari Build)

```bash
cd BlindScanner.ServiceHost\bin\Release\net6.0-windows
.\BlindScanner.ServiceHost.exe
```

Aplikasi akan:
1. Muncul icon di system tray
2. Menampilkan balloon notification dengan URL web interface
3. Web UI dapat diakses di `http://localhost:8080` (atau port yang dikonfigurasi)

## 📱 Menggunakan Web Interface

### 1. Buka Browser

Akses `http://localhost:8080` dari browser di komputer yang sama dengan server.

### 2. Interface Utama

- **Available Scanners**: Daftar scanner yang terdeteksi
- **Recent Scan Jobs**: Daftar job scanning yang sedang berjalan atau selesai

### 3. Memulai Scan

1. Klik tombol **"🖨️ Scan"** pada scanner yang diinginkan
2. Atur parameter scan:
   - **Resolution (DPI)**: 150, 200, 300, atau 600 DPI
   - **Color Mode**: Color atau Grayscale
   - **Duplex**: Scan kedua sisi (jika didukung scanner)
   - **OCR Language**: Pilih bahasa untuk OCR
3. Klik **"Start Scan"**
4. Job akan muncul di daftar **Recent Scan Jobs**
5. Setelah selesai, klik **"⬇️ Download"** untuk mengunduh hasil

### 4. Menu Tray Icon

Klik kanan icon di system tray untuk:
- **Open Web UI**: Buka browser ke web interface
- **Refresh Scanners**: Refresh daftar scanner
- **Settings**: Lihat informasi settings
- **Exit**: Keluar dari aplikasi

## 🔧 Troubleshooting

### Scanner Tidak Terdeteksi

1. **Pastikan scanner terhubung dan powered on**
2. **Install driver scanner** dari vendor (HP, Canon, Epson, dll)
3. **Test dengan Windows Fax and Scan**:
   - Buka Start Menu → Windows Fax and Scan
   - Jika scanner terlihat di sana, BlindScanner juga seharusnya bisa mendeteksi
4. **Restart aplikasi**: Klik Exit di tray icon, lalu jalankan lagi

### OCR Tidak Berfungsi

1. **Pastikan file tessdata sudah diunduh**:
   - Cek folder `tessdata` di direktori aplikasi
   - File harus bernama `<language>.traineddata` (contoh: `eng.traineddata`)
2. **Periksa path di appsettings.json**:
   - `TessdataPath` harus menunjuk ke folder yang berisi file `.traineddata`
3. **Periksa console output** untuk error messages

### Web Interface Tidak Bisa Diakses

1. **Periksa apakah aplikasi berjalan**: Lihat icon di system tray
2. **Periksa port**: Pastikan port 8080 (atau port custom) tidak digunakan aplikasi lain
3. **Firewall**: Windows Defender Firewall mungkin memblok, klik "Allow" jika muncul prompt
4. **Browser cache**: Coba hard refresh (Ctrl+F5) atau buka di incognito mode

### Scan Gagal dengan Error

1. **Periksa Recent Scan Jobs** untuk error message detail
2. **Scanner busy**: Tunggu hingga scan sebelumnya selesai
3. **Driver issue**: Update driver scanner dari website vendor
4. **Restart scanner**: Matikan dan hidupkan kembali scanner

### Permission Error saat Menyimpan ke Shared Folder

1. **Pastikan Anda punya akses write** ke shared folder
2. **Gunakan UNC path** (contoh: `\\SERVER\ShareName`) bukan mapped drive
3. **Test akses** dengan Windows Explorer terlebih dahulu
4. **Credential**: Pastikan akun Windows yang menjalankan BlindScanner punya akses

## 🏗️ Arsitektur Aplikasi

```
BlindScanner/
│
├── BlindScanner.Core/              # Core library
│   ├── Models/                     # Data models
│   │   ├── ScanJob.cs
│   │   ├── ScannerDevice.cs
│   │   └── AppSettings.cs
│   └── Services/                   # Service interfaces
│       ├── IScannerService.cs
│       ├── IOcrService.cs
│       └── IPdfService.cs
│
├── BlindScanner.ServiceHost/       # Main application
│   ├── Services/                   # Service implementations
│   │   ├── WiaScannerService.cs   # WIA scanner integration
│   │   ├── TesseractOcrService.cs # Tesseract OCR wrapper
│   │   └── PdfService.cs          # PDF generation
│   ├── WebUI/                      # Web interface
│   │   └── index.html             # Single-page web app
│   └── Program.cs                  # Entry point + web server
│
└── README.md                       # This file
```

## 🔌 API Endpoints

BlindScanner menyediakan REST API yang dapat digunakan untuk integrasi:

### Devices

- `GET /api/devices` - List all scanner devices
- `POST /api/devices/refresh` - Refresh device list

### Scanning

- `POST /api/scan` - Start a new scan job
  ```json
  {
    "DeviceId": "scanner-id",
    "Dpi": 300,
    "ColorMode": true,
    "Duplex": false,
    "OcrLanguage": "eng"
  }
  ```

### Jobs

- `GET /api/jobs` - List all scan jobs
- `GET /api/jobs/{id}` - Get job status
- `POST /api/jobs/{id}/cancel` - Cancel a job

### Downloads

- `GET /api/download/{jobId}` - Download scan result

### Settings

- `GET /api/settings` - Get current settings
- `POST /api/settings` - Update settings

### OCR

- `GET /api/ocr/languages` - Get available OCR languages

## 🛡️ Security Notes

- Web server hanya binding ke `localhost` secara default
- Tidak ada autentikasi built-in (aplikasi dirancang untuk akses lokal)
- Jika ingin akses dari jaringan, pertimbangkan menggunakan reverse proxy dengan autentikasi
- File scan disimpan di folder lokal yang hanya readable oleh user yang menjalankan aplikasi

## 🚧 Roadmap / TODO

- [ ] TWAIN support untuk scanner yang tidak mendukung WIA dengan baik
- [ ] Multi-page ADF (Automatic Document Feeder) support
- [ ] Image preprocessing (deskew, denoise, binarization)
- [ ] Searchable PDF dengan OCR text layer
- [ ] Text-to-Speech untuk hasil OCR
- [ ] Email hasil scan otomatis
- [ ] FTP/SFTP upload support
- [ ] WebDAV support
- [ ] Network scanner (TCP/IP) support
- [ ] Windows Service mode
- [ ] MSI Installer
- [ ] Authentication untuk web interface
- [ ] Multi-user support dengan job isolation

## 📝 Lisensi

[Tentukan lisensi project Anda di sini]

## 🤝 Kontribusi

Kontribusi sangat diterima! Silakan buat issue atau pull request.

## 📧 Kontak

[Informasi kontak Anda]

## 🙏 Credits

- **WIA (Windows Image Acquisition)** - Microsoft
- **Tesseract OCR** - Google / tesseract-ocr
- **PdfSharpCore** - PDF generation library
- **ASP.NET Core** - Microsoft

---

**Happy Scanning! 🖨️📄**