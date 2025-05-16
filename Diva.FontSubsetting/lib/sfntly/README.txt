sfntly
======

sfntlyは、TrueTypeフォントとOpenTypeフォントを操作するためのJavaライブラリです。

ソースコードは、以下のリポジトリから入手できます。

https://github.com/googlefonts/sfntly

このフォルダに含まれるjarファイルは、2025年5月時点で最新のソースコードをOpenJDK 8.0でビルドしたバイナリです。

※IKVMによるビルド時の変換時間短縮のため、依存しているICU4Jのjarを含まないため、一部の機能は動作しません。
例えば、name()メソッドはICU4Jに依存しているため動作しないため、代わりにnameAsBytes()を使用してください。



