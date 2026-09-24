sfntly
======

sfntlyは、TrueTypeフォントとOpenTypeフォントを操作するためのJavaライブラリです。

ソースコードは、以下のリポジトリから入手できます。

https://github.com/googlefonts/sfntly

このフォルダに含まれるjarファイルは、https://github.com/diva-osaka/sfntly のコミット a56f5782f209771aa226063757d57e6b5c948478 を javac 1.8.0_504（Temurin）でビルドしたバイナリです（.github/workflows/build-sfntly.yml で生成）。

※IKVMによるビルド時の変換時間短縮のため、依存しているICU4Jのjarを含まないため、一部の機能は動作しません。
例えば、name()メソッドはICU4Jに依存しているため動作しないため、代わりにnameAsBytes()を使用してください。



