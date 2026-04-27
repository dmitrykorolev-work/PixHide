# PixHide

PixHide is a desktop application for hiding and extracting data inside PNG images.

> ⚠️ This is an **educational project** and is not intended for production use.

## Features

- Encode files into images  
- Decode hidden data  
- Adjustable encoding settings  
- Optional image preprocessing (Dithering, Contrast correction)
- Local storage with built-in gallery  

## Installation (MSIX)

1. Download `.msix` and `.cer` files  
2. Install the certificate (`.cer`) into **Trusted People** or **Trusted Root**  
3. Run the `.msix` installer  

> Windows will block installation without the certificate.

## Usage

**Encode:** Select image → Select file → adjust encoding settings → Encode  
**Decode:** Load image → Decode

## Notes

- Data capacity depends on image size  
- All images are stored locally (AppData)  
- Intended for learning purposes
