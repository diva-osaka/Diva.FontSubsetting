# Font Subsetter

## Usage

```cs
// Create subset font (font family name: "BIZ UDPGothic+subset")
byte[] fontBytes = await File.ReadAllBytesAsync("Fonts/BIZUDPGothic-Regular.ttf");
List<byte[]> subsetFonts = FontSubsetter.SubsetFonts(fontBytes, "こんにちは", suffix:"subset");
```
## With QuestPDF (2022.12.15)

```cs
// Register subset font
FontManagerHelper.RegisterFont(fontBytes, "こんにちは", suffix:"subset");

// Remove subset font
FontManagerHelper.RemoveSubsetFontsBySuffix("subset");
```


