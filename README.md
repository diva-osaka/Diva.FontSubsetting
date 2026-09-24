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
  * タグは suffix とサブセットに含める文字のハッシュから生成する大文字6文字（PDF 仕様のフォントサブセット接頭辞）。SkiaSharp は PostScript 名を PDF の `/BaseFont` にそのまま使うため、複数の PDF を結合しても別々のサブセットが同名になりにくくなる。
  * タグは 26^6 通りのため衝突はあり得る（例: 62 個のサブセットで衝突する確率は約 0.0006%）。同じ PDF 内での一意性を保証する必要がある場合は、PDF を組み立てる側で衝突を検出して回避すること。

## sfntly の jar の更新

`Diva.FontSubsetting/lib/sfntly` の jar は、[diva-osaka/sfntly](https://github.com/diva-osaka/sfntly)（アーカイブ済みの [googlefonts/sfntly](https://github.com/googlefonts/sfntly) のフォーク）をソースからビルドしたもの。

Actions の「Build sfntly jars」ワークフローを手動実行すると、指定したコミットを JDK 8（Temurin）と Ant でビルドし、jar と `lib/sfntly/README.txt` を更新する PR を作成する。

* IKVM 8.x は Java 8 のクラスファイルまでしか変換できないため、JDK 8 でビルドする。
* jar はタイムスタンプを含むため、展開したクラスファイルに差分がある場合のみ差し替える。`force` を指定すると差分がなくても差し替える（ローカルでビルドした jar を CI でビルドしたものに置き換える場合など）。
* ワークフローが作成した PR では CI が自動実行されないため、PR の「Approve workflows to run」ボタンで実行する。
