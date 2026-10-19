// One list for the summary chip and the warning bars. The count is the list.
using System.Collections.Generic;
using Kestrel.Core.Flight;
using Kestrel.Core.Hangar;

namespace Kestrel.Core.Designer
{
    public sealed class DesignerNotice
    {
        public string code;
        public string text;
        public string detail;
        public string level;
        public string tab;
        public string field;
    }

    public static class DesignerNotices
    {
        public static List<DesignerNotice> Collect(IList<Advisory> advisories, IList<FlightRisk> risks)
        {
            var list = new List<DesignerNotice>();
            var seen = new HashSet<string>();
            if (advisories != null)
            {
                for (int i = 0; i < advisories.Count; i++)
                {
                    var advisory = advisories[i];
                    if (advisory == null || advisory.severity == AdvisorySeverity.Note) continue;
                    string key = Key(advisory.code);
                    if (string.IsNullOrEmpty(key)) key = "advisory-" + i;
                    if (!seen.Add(key)) continue;
                    string text = ShownText.Plain(advisory.code, advisory.kid, advisory.engineer);
                    if (string.IsNullOrEmpty(text)) continue;
                    list.Add(new DesignerNotice
                    {
                        code = advisory.code,
                        text = text,
                        detail = ShownText.Screen(advisory.engineer ?? ""),
                        level = advisory.severity == AdvisorySeverity.Block ? "red" : "amber",
                        tab = advisory.tab,
                    });
                }
            }
            if (risks != null)
            {
                for (int i = 0; i < risks.Count; i++)
                {
                    var risk = risks[i];
                    if (risk == null || (risk.level != "red" && risk.level != "amber")) continue;
                    string key = Key(risk.kind);
                    if (string.IsNullOrEmpty(key)) key = "risk-" + i;
                    if (!seen.Add(key)) continue;
                    string text = ShownText.Plain(risk.kind, risk.kid, risk.engineer);
                    if (string.IsNullOrEmpty(text)) continue;
                    list.Add(new DesignerNotice
                    {
                        code = risk.kind,
                        text = text,
                        detail = ShownText.Screen(risk.engineer ?? ""),
                        level = risk.level,
                        tab = risk.tab,
                        field = risk.field,
                    });
                }
            }
            return list;
        }

        public static string Chip(IList<DesignerNotice> notices)
            => ShownText.CountLabel(notices != null ? notices.Count : 0);

        public static string Key(string code) => ShownText.Norm(code);
    }
}
