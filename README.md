# Font Subsetter

[sfntly](https://github.com/googlefonts/sfntly) と [IKVM](https://github.com/ikvmnet/ikvm) を用いてフォントをサブセット化する C# ライブラリー。

IKVM プロジェクトでは、IKVM でコンパイルされた FOSS Java ライブラリーを[再配布しないことを推奨している](https://github.com/ikvmnet/ikvm?tab=readme-ov-file#notice-to-project-owners)が、パッケージには含めている。

## 使い方

```cs
// Create subset font (font family name: "BIZ UDPGothic+subset")
byte[] fontBytes = await File.ReadAllBytesAsync("Fonts/BIZUDPGothic-Regular.ttf");
List<byte[]> subsetFonts = FontSubsetter.SubsetFonts(fontBytes, "こんにちは", suffix:"subset");
```

## 制限

* TTF 形式のみ（バリアブルフォント未対応）

## 仕様

* サブセット化したフォントのファミリー名を「{元のファミリー名}+{suffix}」に変更する。[Name ID](https://learn.microsoft.com/en-us/typography/opentype/spec/name#name-ids) = 1, 16 の値を変更する。
* サブセット化したフォントの PostScript 名を「{タグ}+{元の PostScript 名}」に変更する。[Name ID](https://learn.microsoft.com/en-us/typography/opentype/spec/name#name-ids) = 6 の値を変更する。
  * タグは suffix から生成する大文字6文字（PDF 仕様のフォントサブセット接頭辞）。SkiaSharp は PostScript 名を PDF の `/BaseFont` にそのまま使うため、これにより複数の PDF を結合しても別々のサブセットが同名にならない。
