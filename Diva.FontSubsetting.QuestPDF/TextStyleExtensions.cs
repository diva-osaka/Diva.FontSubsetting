using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace Diva.FontSubsetting.QuestPDF;

public static class TextStyleExtensions
{
    /// <summary>
    /// ページに含まれるテキストのみのサブセットフォントを登録済みのフォントから作成しTextStyleに適用します。
    /// </summary>
    /// <param name="style"></param>
    /// <param name="page"></param>
    /// <param name="fontName"></param>
    /// <param name="suffix"></param>
    /// <returns></returns>
    public static TextStyle SubsetFontFamily(this TextStyle style, PageDescriptor page, string fontName, string suffix)
    {
        var text = string.Join("", page.ExtractAllText());
        FontManagerHelper.RegisterFont(fontName, text, suffix);
        return style.FontFamily(FontSubsetter.GetSubsetFontFamilyName(fontName, suffix));
    }
}