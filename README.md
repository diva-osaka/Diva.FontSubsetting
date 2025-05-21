# Font Subsetter

[sfntly](https://github.com/googlefonts/sfntly) と [IKVM](https://github.com/ikvmnet/ikvm) を用いてフォントのサブセット化する C# ライブラリー。

IKVM プロジェクトでは、IKVM でコンパイルされた FOSS Java ライブラリを再配布しないことを推奨しているが、パッケージには含めている。

## 使い方

```cs
// Create subset font (font family name: "BIZ UDPGothic+subset")
byte[] fontBytes = await File.ReadAllBytesAsync("Fonts/BIZUDPGothic-Regular.ttf");
List<byte[]> subsetFonts = FontSubsetter.SubsetFonts(fontBytes, "こんにちは", suffix:"subset");
```

## 制限

* TTF 形式のみ（バリアブルフォント未対応）
* サブセット化したフォントのファミリー名を「{元のファミリー名}+{suffix}」に変更するが、[Name ID](https://learn.microsoft.com/en-us/typography/opentype/spec/name#name-ids) = 1 の値を変更する（Name ID = 16 の場合は未対応）。
