using System.Collections.Generic;
using USASymbol.Models.Content;
using USASymbol.Models.ViewModels;

namespace USASymbol.Services.Yaml
{
    public static class YamlParse
    {
        public static List<VisualAsset> VisualAssets(Dictionary<object, object> data)
        {
            var result = new List<VisualAsset>();

            if (!data.TryGetValue("visual_assets", out var obj) || obj is not List<object> list)
                return result;

            foreach (var item in list)
            {
                if (item is not Dictionary<object, object> d) continue;

                result.Add(new VisualAsset
                {
                    Id = S(d, "id"),
                    Src = S(d, "src"),
                    Alt = S(d, "alt"),
                    Caption = S(d, "caption"),
                    Layout = S(d, "layout"),
                    Section = S(d, "section"),
                });
            }

            return result;
        }

        public static ComparisonCardsBlock? ComparisonCards(Dictionary<object, object> data)
        {
            if (!data.TryGetValue("comparison_cards", out var obj) || obj is not Dictionary<object, object> block)
                return null;

            var result = new ComparisonCardsBlock
            {
                Heading = S(block, "heading"),
                Framing = S(block, "framing"),
            };

            if (!block.TryGetValue("cards", out var cardsObj) || cardsObj is not List<object> cardsList)
                return result;

            foreach (var item in cardsList)
            {
                if (item is not Dictionary<object, object> d) continue;

                result.Cards.Add(new ComparisonCardItem
                {
                    Id = S(d, "id"),
                    Label = S(d, "label"),
                    Image = S(d, "image"),
                    Alt = S(d, "alt"),
                    Description = S(d, "description"),
                    CrossColor = S(d, "cross_color"),
                    FieldColor = S(d, "field_color"),
                    Adopted = string.IsNullOrWhiteSpace(S(d, "adopted")) ? S(d, "status") : S(d, "adopted"),
                });
            }

            return result;
        }

        private static string S(Dictionary<object, object> d, string key)
            => d.TryGetValue(key, out var v) ? v?.ToString() ?? "" : "";
    }
}
