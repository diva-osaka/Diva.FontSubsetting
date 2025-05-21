using System.Reflection;
using QuestPDF.Fluent;

namespace Diva.FontSubsetting.QuestPDF;

public static class PageExtensions
{
    /// <summary>
    /// ページからすべてのTextBlockのテキストを収集します。
    /// </summary>
    public static IEnumerable<string> ExtractAllText(this PageDescriptor page)
    {
        var pathsToSearch = new List<string>
        {
            "root.Page.Header.Child",
            "root.Page.Content.Child",
            "root.Page.Footer.Child",
            "root.Page.Foreground.Child",
            "root.Page.Background.Child"
        };
        return FindTextBlocksText(page, pathsToSearch);
    }

    /// <summary>
    /// 指定したパスから探索を始める。
    /// </summary>
    /// <param name="root"></param>
    /// <param name="pathsToSearch"></param>
    /// <returns></returns>
    private static List<string> FindTextBlocksText(object root, List<string> pathsToSearch)
    {
        var result = new List<string>();
        var processed = new HashSet<object>();

        foreach (var path in pathsToSearch)
        {
            // パスを分割して目的のオブジェクトに到達
            var pathParts = path.Split('.');
            var currentObj = root;

            for (var i = 1; i < pathParts.Length && currentObj != null; i++)
            {
                var part = pathParts[i];
                var prop = currentObj.GetType().GetProperty(part,
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                currentObj = prop != null ? prop.GetValue(currentObj) : null;
            }

            if (currentObj == null)
                continue;

            // 見つかったオブジェクトからTextBlockを検索
            FindTextBlocksText(currentObj, result, processed);
        }

        return result;
    }

    /// <summary>
    /// 再帰でTextBlockのTextを収集する。
    /// </summary>
    /// <param name="obj"></param>
    /// <param name="result"></param>
    /// <param name="processed"></param>
    private static void FindTextBlocksText(object? obj, List<string> result,
        HashSet<object> processed)
    {
        if (obj == null || !processed.Add(obj)) return;

        var type = obj.GetType();

        // TextBlockを見つけたらTextプロパティの内容を追加
        if (type.FullName?.Contains("QuestPDF.Elements.Text.TextBlock") == true)
        {
            var textProperty = type.GetProperty("Text",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (textProperty == null)
                return;

            var textValue = textProperty.GetValue(obj) as string;
            if (!string.IsNullOrEmpty(textValue))
            {
                result.Add(textValue);
            }

            return; // TextBlockを見つけたら再帰を終了
        }

        // GetChildrenメソッドがあれば使用
        var getChildrenMethod = type.GetMethod("GetChildren",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        if (getChildrenMethod == null)
            return;

        try
        {
            var children = getChildrenMethod.Invoke(obj, null);
            if (children is not System.Collections.IEnumerable collection)
                return;

            foreach (var child in collection)
            {
                if (child != null)
                    FindTextBlocksText(child, result, processed);
            }
        }
        catch
        {
            // 例外は無視
        }
    }
}