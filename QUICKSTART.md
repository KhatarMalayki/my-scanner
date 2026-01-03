# BlindScanner - Quick Start Guide

Panduan singkat untuk menjalankan BlindScanner dalam 5 menit!

## 🚀 Langkah Cepat

### 1. Build Aplikasi

```bash
cd D:\Project\BlindScanner
dotnet build --configuration Release
```

### 2. Download Tessdata (OCR Language Files)

Minimal download bahasa Inggris untuk testing:

```powershell
# Buat folder tessdata
cd D:\Project\BlindScanner\BlindScanner.ServiceHost\bin\Release\net6.0-windows
mkdir tessdata

# Download English language data
Invoke-WebRequest -Uri "https://github.com/tesseract-ocr/tessdata/raw/main/eng.traineddata" -OutFile "tessdata\eng.traineddata"

# OPSIONAL: Download bahasa Indonesia
Invoke-WebRequest -Uri "https://github.com/tesseract-ocr/tessdata/raw/main/ind.traineddata" -OutFile "tessdata\ind.traineddata"
```

**Alternative**: Download manual dari browser:
- Kunjungi: https://github.com/tesseract-ocr/tessdata
- Download file `eng.traineddata` 
- Simpan ke folder `BlindScanner.ServiceHost\bin\Release\net6.0-windows\tessdata\`

### 3. Jalankan Aplikasi

```bash
cd BlindScanner.ServiceHost\bin\Release\net6.0-windows
.\BlindScanner.ServiceHost.exe
```

Atau double-click `BlindScanner.ServiceHost.exe` di Windows Explorer.

### 4. Akses Web Interface

1. Icon tray akan muncul di system tray (pojok kanan bawah)
2. Balloon notification akan muncul dengan URL
3. Buka browser dan akses: **http://localhost:8080**
4. Atau double-click icon tray untuk membuka browser otomatis

## 📡 Test Scanner

### Jika Anda Punya Scanner Fisik:

1. Pastikan scanner terhubung via USB dan powered on
2. Install driver scanner dari vendor (HP, Canon, Epson, dll)
3. Test scanner dengan "Windows Fax and Scan" terlebih dahulu
4. Refresh di web interface BlindScanner
5. Scanner akan muncul di daftar "Available Scanners"

### Jika Tidak Punya Scanner (Testing):

Aplikasi tetap bisa dijalankan dan web interface akan tampil, tapi tidak akan ada scanner yang terdeteksi. Untuk testing lengkap, Anda perlu:
- Scanner fisik USB, atau
- Multifunction Printer (MFP) yang punya fitur scan via USB

## 🎯 Menggunakan Web UI

1. **Lihat Scanner**: Daftar scanner akan tampil di halaman utama
2. **Start Scan**: Klik tombol "🖨️ Scan" pada scanner yang diinginkan
3. **Atur Setting**: 
   - Resolution: 300 DPI (recommended)
   - Color Mode: Centang untuk warna, uncheck untuk grayscale
   - OCR Language: Pilih bahasa dokumen
4. **Klik "Start Scan"**: Job akan masuk antrian
5. **Download Hasil**: Setelah status "Completed", klik "⬇️ Download"

## 📁 Lokasi File Output

Hasil scan default disimpan di:
```
C:\Users\[YourUsername]\Documents\BlindScanner\Output\
```

Setiap scan job punya folder sendiri dengan GUID sebagai nama folder.

## ⚙️ Konfigurasi (Opsional)

Edit `appsettings.json` di folder aplikasi:

```json
{
  "OutputFolder": "C:\\Users\\YourUsername\\Documents\\BlindScanner\\Output",
  "SharedFolder": "",
  "TessdataPath": "tessdata",
  "DefaultOcrLanguage": "eng",
  "WebServerPort": 8080,
  "DefaultDpi": 300,
  "AutoSaveToSharedFolder": false,
  "EnableOcr": true,
  "EnableTextToSpeech": false,
  "MaxConcurrentJobs": 3
}
```

**Restart aplikasi** setelah mengubah settings.

## 🔧 Troubleshooting Cepat

### Scanner tidak terdeteksi?
- Install driver dari vendor scanner
- Test dengan "Windows Fax and Scan"
- Klik "🔄 Refresh Scanners" di web UI
- Restart aplikasi

### OCR error?
- Pastikan file `eng.traineddata` ada di folder `tessdata`
- Check console output untuk error detail

### Port 8080 sudah dipakai?
- Edit `appsettings.json`, ganti `WebServerPort` ke port lain (misal 8081)
- Restart aplikasi

### Web UI tidak bisa diakses?
- Pastikan icon tray muncul (aplikasi berjalan)
- Allow Windows Firewall jika diminta
- Coba akses: http://127.0.0.1:8080

## 🎉 Selesai!

Aplikasi BlindScanner sekarang siap digunakan. 

**Tray Icon Menu:**
- **Open Web UI**: Buka browser
- **Refresh Scanners**: Refresh daftar scanner
- **Settings**: Lihat settings saat ini
- **Exit**: Keluar dari aplikasi

## 📚 Dokumentasi Lengkap

Untuk dokumentasi lengkap, lihat [README.md](README.md)

## 🆘 Butuh Bantuan?

- Lihat console output untuk error messages
- Check [README.md](README.md) untuk troubleshooting detail
- Scanner harus support WIA (Windows Image Acquisition)

---

**Tips**: Untuk production use, letakkan aplikasi di folder yang permanent (bukan di folder Project), misalnya `C:\Program Files\BlindScanner\` atau `C:\Apps\BlindScanner\`.