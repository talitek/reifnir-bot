using Nellebot.Common.Extensions;
using Nellebot.Common.Models.Ordbok.Api;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Nellebot.Common.Models.Ordbok
{
    public static class ContentStringHelper
    {
        public static string GetExplanationContent(Explanation explanation)
        {
            var contentString = explanation.Content;

            var contentHasVariables = explanation.ExplanationItems.Any();

            if (!contentHasVariables)
                return contentString;

            var regex = new Regex(Regex.Escape("$"));

            foreach (var item in explanation.ExplanationItems)
            {
                switch (item)
                {
                    case ExplanationIdElement idElement:
                        contentString = regex.Replace(contentString, $"?{idElement.Id}?", 1);
                        break;
                    case ExplanationItemArticleRef articleRef:
                        var firstLemma = articleRef.Lemmas.FirstOrDefault();
                        if (firstLemma != null)
                        {
                            var value = firstLemma.Value;
                            var hgNo = firstLemma.HgNo.ToRomanNumeral();
                            var definitionOrder = articleRef.DefinitionOrder;

                            var pValues = new List<string>();

                            if (!string.IsNullOrWhiteSpace(hgNo))
                                pValues.Add(hgNo);
                            if (definitionOrder > 0)
                                pValues.Add(definitionOrder.ToString());

                            var displayValue = firstLemma.Value;

                            if (pValues.Count > 0)
                            {
                                displayValue = $"{displayValue} ({string.Join(",", pValues)})";
                            }

                            contentString = regex.Replace(contentString, displayValue, 1);
                        }

                        break;
                }
            }

            return contentString;
        }
    }
}
