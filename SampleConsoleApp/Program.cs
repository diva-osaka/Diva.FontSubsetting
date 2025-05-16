using Diva.FontSubsetting;
using Diva.FontSubsetting.QuestPDF;
using QuestPDF.Drawing;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

FontManager.RegisterFont(File.OpenRead("Fonts/BIZUDPGothic-Regular.ttf"));

var fontBytes = await File.ReadAllBytesAsync("Fonts/BIZUDPGothic-Regular.ttf");

const string headerText = "こんにちは";
const string paragraph =
    "「雨にふりこめられた下人が、行き所がなくて、途方にくれていた」と云う方が、適当である。その上、今日の空模様も少からず、この平安朝の下人の Sentimentalisme に影響した。申さるの刻こく下さがりからふり出した雨は、いまだに上るけしきがない。そこで、下人は、何をおいても差当り明日あすの暮しをどうにかしようとして――云わばどうにもならない事を、どうにかしようとして、とりとめもない考えをたどりながら、さっきから朱雀大路にふる雨の音を、聞くともなく聞いていたのである。";

FontManagerHelper.RegisterFont(fontBytes, headerText + paragraph, "subset");

// var subsetFonts = FontSubsetter.SubsetFonts(fontBytes, headerText + paragraph, "subset");
// foreach (var subsetFont in subsetFonts)
// {
//     using var stream = new MemoryStream(subsetFont);
//     FontManager.RegisterFont(stream);
// }

Document.Create(container =>
    {
        container.Page(page =>
        {
            page.DefaultTextStyle(x => x
                .FontFamily(FontSubsetter.GetSubsetFontFamilyName("BIZ UDPGothic", "subset"))
                .FontSize(20)
                .Fallback(y => y
                    .FontFamily("BIZ UDPGothic")
                )
            );

            page.Size(PageSizes.A4);
            page.Margin(2, Unit.Centimetre);
            page.PageColor(Colors.White);

            page.Header()
                .Text(headerText)
                .SemiBold().FontSize(36).FontColor(Colors.Blue.Medium);

            page.Content()
                .PaddingVertical(1, Unit.Centimetre)
                .Column(x =>
                {
                    x.Spacing(20);

                    x.Item().Text(paragraph);
                    x.Item().Image(Placeholders.Image(200, 100));
                });

            page.Footer()
                .AlignCenter()
                .Text(x =>
                {
                    x.Span("page ");
                    x.CurrentPageNumber();
                });
        });
    })
    .GeneratePdf("hello.pdf");

FontManagerHelper.RemoveSubsetFontsBySuffix("subset");