using System.Collections;
using QuestPDF.Drawing;

namespace Diva.FontSubsetting.QuestPDF;

public static class FontManagerHelper
{
    // FontManagerへのアクセスを同期化するためのロックオブジェクト
    private static readonly object LockObject = new();

    /// <summary>
    /// サブセットフォントを生成して登録します。
    /// </summary>
    /// <param name="fontBytes">フォントデータ</param>
    /// <param name="subsetString">サブセットフォントに含める文字</param>
    /// <param name="suffix">サブセットフォントのフォントファミリー名の接尾辞（未エンコード）</param>
    /// <param name="includesAsciiPrintableCharacters">サブセットにASCII印刷可能文字を含める</param>
    public static void RegisterFont(
        byte[] fontBytes,
        string subsetString,
        string? suffix = null,
        bool includesAsciiPrintableCharacters = true)
    {
        var subsetFonts = FontSubsetter.SubsetFonts(fontBytes, subsetString, suffix, includesAsciiPrintableCharacters);

        lock (LockObject)
        {
            foreach (var subsetFont in subsetFonts)
            {
                using var stream = new MemoryStream(subsetFont);
                FontManager.RegisterFont(stream);
            }
        }
    }

    /// <summary>
    /// 既存のサブセットフォントを削除し、新しいサブセットフォントを登録します。
    /// </summary>
    /// <param name="fontBytes">フォントデータ</param>
    /// <param name="subsetString">サブセットフォントに含める文字</param>
    /// <param name="suffix">サブセットフォントのフォントファミリー名の接尾辞（未エンコード）</param>
    /// <param name="includesAsciiPrintableCharacters">サブセットにASCII印刷可能文字を含める</param>
    public static void UpdateFont(
        byte[] fontBytes,
        string subsetString,
        string suffix,
        bool includesAsciiPrintableCharacters = true)
    {
        // サブセット生成は計算コストが高いのでlockの外で実行
        var subsetFonts = FontSubsetter.SubsetFonts(fontBytes, subsetString, suffix, includesAsciiPrintableCharacters);

        lock (LockObject)
        {
            // 既存のフォントを削除
            RemoveFontsInternal(x => x.EndsWith($"+{FontSubsetter.EncodeSuffix(suffix)}"));

            // 新しいフォントを登録
            foreach (var subsetFont in subsetFonts)
            {
                using var stream = new MemoryStream(subsetFont);
                FontManager.RegisterFont(stream);
            }
        }
    }

    /// <summary>
    /// lock内で実行される内部メソッド
    /// </summary>
    private static void RemoveFontsInternal(Func<string, bool> fontNamePredicate)
    {
        // FontManagerのStyleSetsを取得して、条件に合致するものを削除する
        var fontManagerType = typeof(FontManager);
        var styleSetField = fontManagerType.GetField("StyleSets",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        var styleSet = styleSetField?.GetValue(null) ??
                       throw new InvalidOperationException("FontManager.StyleSets is null");

        var keysToRemove = ((IDictionary)styleSet).Keys
            .OfType<string>()
            .Where(fontNamePredicate)
            .ToList();

        var styleSetType = styleSet.GetType();
        var tryRemoveMethod = styleSetType
            .GetMethod(
                "TryRemove",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance,
                [
                    typeof(string),
                    styleSetType.GenericTypeArguments[1].MakeByRefType()
                ]
            ) ?? throw new InvalidOperationException("FontManager.StyleSets.TryRemove is null");

        foreach (var key in keysToRemove)
        {
            tryRemoveMethod.Invoke(styleSet, [key, null]);
        }
    }

    /// <summary>
    /// 指定の名前に完全一致するフォントを削除します。
    /// </summary>
    /// <param name="fontName">削除するフォントファイリー名</param>
    public static void RemoveSubsetFontByName(string fontName)
    {
        lock (LockObject)
        {
            RemoveFontsInternal(x => x == fontName);
        }
    }

    /// <summary>
    /// 指定のsuffixの付くサブセットフォントを削除します。
    /// </summary>
    /// <param name="suffix">サブセットフォントのフォントファイリー名の接尾辞（未エンコード）</param>
    /// <remarks>
    /// このメソッドは、フォントファミリー名が「+エンコードされた接尾辞」で終わるフォントを削除します。
    /// 内部では <c>x.EndsWith($"+{encodeSuffix}")</c> の条件で一致するフォントを特定しています。
    /// </remarks>
    public static void RemoveSubsetFontsBySuffix(string suffix)
    {
        lock (LockObject)
        {
            var encodeSuffix = FontSubsetter.EncodeSuffix(suffix);
            RemoveFontsInternal(x => x.EndsWith($"+{encodeSuffix}"));
        }
    }
}