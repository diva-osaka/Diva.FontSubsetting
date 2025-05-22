using System.Text;
using com.google.typography.font.sfntly;
using com.google.typography.font.sfntly.table.core;
using com.google.typography.font.sfntly.table.truetype;
using com.google.typography.font.tools.subsetter;
using java.io;
using java.lang;
using static com.google.typography.font.sfntly.Font;
using static com.google.typography.font.sfntly.table.core.CMap;
using static com.google.typography.font.sfntly.table.core.NameTable;
using static com.google.typography.font.sfntly.table.truetype.Glyph;

namespace Diva.FontSubsetting;

/// <summary>
/// フォントをサブセット化します。
/// </summary>
/// <remarks>
/// <see href="https://github.com/googlefonts/sfntly/tree/main/java/src/com/google/typography/font/tools/sfnttool">sfnttool</see>を移植したものです。
/// </remarks>
public static class FontSubsetter
{
    /// <summary>
    /// サブセットフォントファミリー名の接尾辞をエンコードします。
    /// </summary>
    /// <param name="suffix"></param>
    /// <returns></returns>
    public static string EncodeSuffix(string suffix) => Uri.EscapeDataString(suffix);

    /// <summary>
    /// サブセットフォントファミリー名を取得します。
    /// </summary>
    /// <param name="originalName">オリジナルフォントファミリー名</param>
    /// <param name="suffix">接尾辞（未エンコード）</param>
    /// <returns>オリジナルフォントファミリー名+エンコードした接尾辞</returns>
    public static string GetSubsetFontFamilyName(string originalName, string? suffix = null) =>
        $"{originalName}+{EncodeSuffix(suffix ?? "subset")}";

    /// <summary>
    /// フォントをサブセット化します（フォントコレクションをサポート）。
    /// サブセット化されたフォントのフォントファミリー名は「元のフォントファミリー名+エンコードされたサブセット接尾辞」になります。
    /// </summary>
    /// <param name="fontBytes">フォントデータ</param>
    /// <param name="subsetString">サブセットフォントに含める文字</param>
    /// <param name="suffix">サブセットフォントのフォントファミリー名の接尾辞（未エンコード）</param>
    /// <param name="includesAsciiPrintableCharacters">サブセットにASCII印刷可能文字を含める</param>
    /// <returns>サブセット化されたフォントデータ</returns>
    public static List<byte[]> SubsetFonts(
        byte[] fontBytes,
        string subsetString,
        string? suffix = null,
        bool includesAsciiPrintableCharacters = true)
    {
        var fontFactory = FontFactory.getInstance();
        var fonts = fontFactory.loadFonts(fontBytes);

        if (includesAsciiPrintableCharacters)
        {
            // ASCII印刷可能文字
            subsetString += string.Concat(Enumerable.Range(0x20, 0x7F - 0x20 + 1).Select(x => (char)x));
        }

        return fonts
            .Select(x => SubsetFont(fontFactory, x, subsetString, suffix))
            .ToList();
    }

    /// <summary>
    /// フォントをサブセット化します。
    /// サブセット化されたフォントのフォントファミリー名は「元のフォントファミリー名+エンコードされたサブセット接尾辞」になります。
    /// </summary>
    /// <remarks>
    /// sfnttoolからのコードを含む
    /// </remarks>
    /// <param name="fontFactory">フォントファクトリー</param>
    /// <param name="font">フォント</param>
    /// <param name="subsetString">サブセットフォントに含める文字</param>
    /// <param name="suffix">サブセットフォントのフォントファミリー名の接尾辞（未エンコード）</param>
    /// <returns></returns>
    private static byte[] SubsetFont(FontFactory fontFactory, Font font, string subsetString, string? suffix)
    {
        // MEMO: IKVMで生成されるコードは、Javaのジェネリックの型消去された後であることに注意。
        // java.util.HashSet<T>は、.NET側ではjava.util.HashSetとして見えるため、作成時点で何を入れてもエラーにならないが、
        // Java側のコードでは中身はT型で揃っていることが期待されているため、後のコードを実行中にエラーが出る。
        // したがって、java.util.HashSet<java.lang.Integer>を期待しているコードにint（.NETのInt32）を入れてはならず、
        // java.lang.Integerを入れなければならない。

        // サブセット化する
        // （sfnttoolからのコード）

        var cmapIds = new java.util.ArrayList();
        cmapIds.add(CMapTable.CMapId.WINDOWS_BMP);

        Subsetter subsetter = new RenumberingSubsetter(font, fontFactory);
        subsetter.setCMaps(cmapIds, 1);
        var glyphs = GetGlyphCoverage(font, subsetString);
        subsetter.setGlyphs(glyphs);
        var removeTables = new java.util.HashSet();
        // Most of the following are valid tables, but we don't renumber them yet, so strip
        removeTables.add(new Integer(Tag.GDEF));
        removeTables.add(new Integer(Tag.GPOS));
        removeTables.add(new Integer(Tag.GSUB));
        removeTables.add(new Integer(Tag.kern));
        removeTables.add(new Integer(Tag.hdmx));
        removeTables.add(new Integer(Tag.vmtx));
        removeTables.add(new Integer(Tag.VDMX));
        removeTables.add(new Integer(Tag.LTSH));
        removeTables.add(new Integer(Tag.DSIG));
        removeTables.add(new Integer(Tag.vhea));
        // AAT tables, not yet defined in sfntly Tag class
        removeTables.add(new Integer(Tag.intValue("mort".Select(x => (byte)x).ToArray())));
        removeTables.add(new Integer(Tag.intValue("morx".Select(x => (byte)x).ToArray())));
        subsetter.setRemoveTables(removeTables);
        var fontBuilder = subsetter.subset();

        // フォントファミリー名を変更する
        var originalNameTable = (NameTable)font.getTable(Tag.name);
        var nameTableBuilder = (NameTable.Builder)fontBuilder.getTableBuilder(Tag.name);
        for (var i = 0; i < originalNameTable.nameCount(); i++)
        {
            var originalEntry = originalNameTable.nameEntry(i);
            var nameId = originalEntry.nameId();

            if (nameId != NameId.FontFamilyName.value())
            // Name ID = 1 (Font Family name) と Name ID = 16 (Typographic Family name) を書き換える
            if (nameId != NameId.FontFamilyName.value() && nameId != NameId.PreferredFamily.value())
                continue;

            // 深追いはしていないが、ICU4JはIKVMで変換したときにうまく内部リソースデータを取得できない&ビルドに時間が掛かる
            // ので含めていない。ICU4Jの呼び出しを回避するためバイト列で取得し、.NETのEncodingを使用して変換する  
            var nameBytes = originalEntry.nameAsBytes();
            var encodingId = originalEntry.encodingId();
            var platformId = originalEntry.platformId();
            var languageId = originalEntry.languageId();
            var encoding = GetEncoding(platformId, encodingId, languageId);

            var name = encoding.GetString(nameBytes);
            var newName = GetSubsetFontFamilyName(name, suffix);
            var newNameBytes = encoding.GetBytes(newName);
            var nameEntryBuilder = nameTableBuilder.nameBuilder(
                platformId,
                encodingId,
                languageId,
                nameId
            );
            nameEntryBuilder.setName(newNameBytes);
        }

        // フォントデータを作成して返す
        var newFont = fontBuilder.build();
        using var baos = new ByteArrayOutputStream();
        fontFactory.serializeFont(newFont, baos);
        return baos.toByteArray();
    }

    /// <summary>
    /// 適したエンコーディングを取得します。
    /// （現在はBIZ UD系のフォントが対応しているWindowsのUnicodeUCS2の場合のみテストしている。 他のフォントを使うときは要テスト）
    /// </summary>
    /// <param name="platformId"></param>
    /// <param name="encodingId"></param>
    /// <param name="languageId"></param>
    /// <returns></returns>
    private static Encoding GetEncoding(int platformId, int encodingId, int languageId)
    {
        if (platformId == PlatformId.Windows.value())
        {
            if (encodingId == WindowsEncodingId.UnicodeUCS2.value())
                return Encoding.BigEndianUnicode;

            if (encodingId == WindowsEncodingId.UnicodeUCS4.value())
                return new UTF32Encoding(bigEndian: true, byteOrderMark: false);

            if (encodingId == WindowsEncodingId.ShiftJIS.value())
                return Encoding.GetEncoding("Shift_JIS");
        }
        else if (platformId == PlatformId.Macintosh.value())
        {
            if (encodingId == MacintoshEncodingId.Japanese.value())
                return Encoding.GetEncoding("Shift_JIS");
        }
        else if (platformId == PlatformId.Unicode.value())
        {
            if (encodingId == UnicodeEncodingId.Unicode2_0.value())
                return new UTF32Encoding(bigEndian: true, byteOrderMark: false);

            return Encoding.BigEndianUnicode;
        }

        // どれにも該当しない場合は、とりあえずUTF-8を返す。
        // 1バイト系のエンコードの場合、ASCIIの範囲で一致しているかもしれないので。
        return Encoding.UTF8;
    }

    /// <summary>
    /// 与えられた文字に関連するグリフのリストを取得します。
    /// </summary>
    /// <remarks>
    /// sfnttoolからのコードを含む。
    /// </remarks>
    /// <param name="font">フォント</param>
    /// <param name="glyphs">文字一覧</param>
    /// <returns>与えられた文字に関連する関連するグリフのリスト</returns>
    /// <exception cref="InvalidOperationException"></exception>
    private static java.util.List GetGlyphCoverage(Font font, string glyphs)
    {
        var cmapTable = (CMapTable)font.getTable(Tag.cmap);
        var cmap = GetBestCMap(cmapTable) ?? throw new InvalidOperationException("CMap is not found");
        var coverage = new java.util.HashSet();
        coverage.add(new Integer(0)); // Always include notdef
        // TODO: doesn't support non-BMP scripts, should use StringCharacterIterator instead
        foreach (var glyph in glyphs)
        {
            var c = glyph & 0xffff;
            var glyphId = cmap.glyphId(c);
            TouchGlyph(font, coverage, glyphId);
        }

        var sortedCoverage = new java.util.ArrayList(coverage);
        java.util.Collections.sort(sortedCoverage);
        return sortedCoverage;
    }

    /// <summary>
    /// そのグリフと依存するグリフを取得して、指定のSetに追加します。
    /// </summary>
    /// <remarks>
    /// sfnttoolからのコードを含む。
    /// </remarks>
    /// <param name="font"></param>
    /// <param name="coverage"></param>
    /// <param name="glyphId"></param>
    private static void TouchGlyph(Font font, java.util.Set coverage, int glyphId)
    {
        if (coverage.contains(new Integer(glyphId)))
            return;

        coverage.add(new Integer(glyphId));
        var glyph = GetGlyph(font, glyphId);
        if (glyph.glyphType() != GlyphType.Composite)
            return;

        var composite = (CompositeGlyph)glyph;
        for (var i = 0; i < composite.numGlyphs(); i++)
        {
            TouchGlyph(font, coverage, composite.glyphIndex(i));
        }
    }

    /// <summary>
    /// 最適なCMapを取得します。
    /// </summary>
    /// <remarks>
    /// sfnttoolからのコードを含む。
    /// </remarks>
    /// <param name="cmapTable"></param>
    /// <returns></returns>
    private static CMap? GetBestCMap(CMapTable cmapTable)
    {
        foreach (CMap cmap in cmapTable)
        {
            if (cmap.format() == CMapFormat.Format12.value())
                return cmap;
        }

        foreach (CMap cmap in cmapTable)
        {
            if (cmap.format() == CMapFormat.Format4.value())
                return cmap;
        }

        return null;
    }

    /// <summary>
    /// 指定されたグリフIDのグリフを取得します。
    /// </summary>
    /// <remarks>
    /// sfnttoolからのコードを含む。
    /// </remarks>
    /// <param name="font">フォント</param>
    /// <param name="glyphId">グリフID</param>
    /// <returns>グリフ</returns>
    private static Glyph GetGlyph(Font font, int glyphId)
    {
        var locaTable = (LocaTable)font.getTable(Tag.loca);
        var glyfTable = (GlyphTable)font.getTable(Tag.glyf);
        var offset = locaTable.glyphOffset(glyphId);
        var length = locaTable.glyphLength(glyphId);
        return glyfTable.glyph(offset, length);
    }
}