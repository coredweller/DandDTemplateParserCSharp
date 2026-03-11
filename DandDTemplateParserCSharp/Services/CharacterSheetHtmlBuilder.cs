using System.Text;
using DandDTemplateParserCSharp.Domain;

namespace DandDTemplateParserCSharp.Services;

/// <summary>
/// Generates styled HTML for D&amp;D character sheet renders.
/// All CSS is inlined — no external dependencies.
/// </summary>
internal static class CharacterSheetHtmlBuilder
{
    private const string Css = """
        * { box-sizing: border-box; margin: 0; padding: 0; }
        body { background: #2c1a0e; font-family: 'Palatino Linotype', Palatino, Georgia, serif; }
        .sheet { max-width: 960px; margin: 2rem auto; background: #fdf8ef; border: 3px solid #7b1f1f;
                 box-shadow: 0 0 30px rgba(0,0,0,.6); }

        /* ── Header ── */
        .header { background: #7b1f1f; color: #fdf8ef; padding: 1.5rem 2rem; text-align: center;
                  border-bottom: 4px solid #c8a951; }
        .char-name { font-size: 2.4rem; letter-spacing: 3px; text-transform: uppercase;
                     text-shadow: 2px 2px 4px rgba(0,0,0,.5); }
        .header-meta { margin-top: .5rem; font-size: 1.1rem; color: #f0d080; letter-spacing: 1px; }
        .header-meta span + span::before { content: ' · '; }

        /* ── Core stats bar ── */
        .stats-bar { display: flex; justify-content: center; gap: 0; background: #4a1010;
                     border-bottom: 3px solid #c8a951; }
        .stat-pill { flex: 1; text-align: center; padding: .7rem .5rem; color: #fdf8ef;
                     border-right: 1px solid #7b1f1f; }
        .stat-pill:last-child { border-right: none; }
        .stat-pill .stat-label { font-size: .65rem; text-transform: uppercase;
                                 letter-spacing: 1px; color: #c8a951; }
        .stat-pill .stat-value { font-size: 1.3rem; font-weight: bold; margin-top: .1rem; }

        /* ── Ability scores ── */
        .ability-grid { display: grid; grid-template-columns: repeat(6, 1fr); gap: 1px;
                        background: #7b1f1f; border: 2px solid #7b1f1f; margin: 1.2rem; }
        .ability-box { background: #fdf8ef; text-align: center; padding: .6rem .3rem; }
        .ability-name { font-size: .6rem; text-transform: uppercase; letter-spacing: 1px;
                        color: #7b1f1f; font-weight: bold; }
        .ability-score { font-size: 1.8rem; font-weight: bold; color: #1a1a1a; line-height: 1; }
        .ability-mod { font-size: 1rem; color: #7b1f1f; font-weight: bold; margin-top: .1rem; }

        /* ── Two-column layout ── */
        .two-col { display: grid; grid-template-columns: 1fr 1fr; gap: 1.2rem;
                   padding: 0 1.2rem 1.2rem; }

        /* ── Sections ── */
        .section { margin-bottom: 1.2rem; }
        .section-title { background: #7b1f1f; color: #fdf8ef; padding: .3rem .7rem;
                         font-size: .75rem; text-transform: uppercase; letter-spacing: 2px;
                         margin-bottom: .5rem; }
        .section-body { padding: 0 .5rem; font-size: .88rem; line-height: 1.5; color: #1a1a1a; }

        /* ── Key-value lists ── */
        .kv-list { list-style: none; }
        .kv-list li { padding: .2rem 0; border-bottom: 1px solid #e8dcc8; }
        .kv-list li:last-child { border-bottom: none; }
        .kv-key { font-weight: bold; color: #7b1f1f; }

        /* ── Full-width sections ── */
        .full-width { padding: 0 1.2rem 1.2rem; }

        /* ── Equipment row ── */
        .equip-grid { display: grid; grid-template-columns: 1fr 1fr 1fr; gap: 1rem; font-size: .88rem; }
        .equip-label { font-weight: bold; color: #7b1f1f; font-size: .75rem;
                       text-transform: uppercase; letter-spacing: 1px; }

        /* ── Legendary badge ── */
        .legendary-badge { text-align: center; padding: .4rem; background: #c8a951;
                           color: #2c1a0e; font-size: .75rem; text-transform: uppercase;
                           letter-spacing: 3px; font-weight: bold; }

        /* ── Divider ── */
        .divider { border: none; border-top: 2px solid #c8a951; margin: 0 1.2rem .8rem; }

        /* ── Spellcasting ── */
        .spellcasting-entry { padding: .4rem 0 !important; }
        .spell-preamble { margin: .3rem 0 .5rem; color: #333; font-style: italic; }
        .spell-level-row { display: flex; align-items: flex-start; gap: .6rem; padding: .25rem 0;
                           border-bottom: 1px solid #e8dcc8; flex-wrap: wrap; }
        .spell-level-row:last-child { border-bottom: none; }
        .spell-level-label { font-weight: bold; color: #7b1f1f; font-size: .78rem; min-width: 9rem;
                             flex-shrink: 0; padding-top: .15rem; }
        .spell-tags { display: flex; flex-wrap: wrap; gap: .25rem; }
        .spell-tag { background: #f5edda; border: 1px solid #c8a951; border-radius: 3px;
                     padding: .1rem .4rem; font-size: .78rem; color: #2c1a0e; }

        /* ── Notes ── */
        .notes-body { padding: .5rem; font-size: .88rem; line-height: 1.6;
                      background: #faf5e8; border: 1px solid #e0d0a8; min-height: 4rem; }

        /* ── Footer ── */
        .footer { background: #7b1f1f; color: #c8a951; text-align: center; padding: .4rem;
                  font-size: .7rem; letter-spacing: 1px; }
        """;

    public static string BuildGeneral(GeneralSheetRequest r)
    {
        var sb = new StringBuilder();
        WriteDocStart(sb, r.CharacterName);
        WriteHeader(sb, r.CharacterName, r.Level, r.Race, r.Class, r.Alignment);
        WriteStatsBar(sb, r.HP, r.AC, r.Speed, null, null);
        WriteAbilityScores(sb, r.AbilityScores);

        sb.AppendLine("<div class=\"two-col\">");
        WriteKvSection(sb, "Saving Throws", r.SavingThrows);
        WriteKvSection(sb, "Skills", r.Skills);
        sb.AppendLine("</div>");

        sb.AppendLine("<div class=\"two-col\">");
        WriteTextSection(sb, "Senses", r.Senses);
        WriteTextSection(sb, "Languages", r.Languages);
        sb.AppendLine("</div>");

        WriteKvSectionFull(sb, "Special Traits", r.SpecialTraits);
        WriteKvSectionFull(sb, "Actions", r.Actions);
        WriteEquipment(sb, r.Equipment);
        WriteNotes(sb, r.Notes);
        WriteFooter(sb, "general");
        WriteDocEnd(sb);
        return sb.ToString();
    }

    public static string BuildLegendary(LegendarySheetRequest r)
    {
        var sb = new StringBuilder();
        WriteDocStart(sb, r.CharacterName);

        sb.AppendLine("<div class=\"legendary-badge\">&#9670; Legendary Creature &#9670;</div>");
        WriteHeader(sb, r.CharacterName, r.Level, r.Race, r.Class, r.Alignment);
        WriteStatsBar(sb, r.HP, r.AC, r.Speed, r.ChallengeRating, r.ProficiencyBonus);
        WriteAbilityScores(sb, r.AbilityScores);

        sb.AppendLine("<div class=\"two-col\">");
        WriteKvSection(sb, "Saving Throws", r.SavingThrows);
        WriteKvSection(sb, "Skills", r.Skills);
        sb.AppendLine("</div>");

        sb.AppendLine("<div class=\"two-col\">");
        WriteTextSection(sb, "Senses", r.Senses);
        WriteTextSection(sb, "Languages", r.Languages);
        sb.AppendLine("</div>");

        sb.AppendLine("<div class=\"two-col\">");
        WriteTextSection(sb, "Damage Resistances", r.DamageResistances);
        WriteTextSection(sb, "Damage Immunities", r.DamageImmunities);
        sb.AppendLine("</div>");

        WriteTextSectionFull(sb, "Condition Immunities", r.ConditionImmunities);

        sb.AppendLine("<hr class=\"divider\">");
        WriteKvSectionFull(sb, "Special Traits", r.SpecialTraits);
        WriteKvSectionFull(sb, "Actions", r.Actions);
        WriteKvSectionFull(sb, "Bonus Actions", r.BonusActions);
        WriteKvSectionFull(sb, "Reactions", r.Reactions);

        sb.AppendLine("<hr class=\"divider\">");
        WriteKvSectionFull(sb, "Legendary Traits", r.LegendaryTraits);
        WriteLegendaryActions(sb, r.LegendaryActions);
        WriteMythicTrait(sb, r.MythicTrait);
        WriteKvSectionFull(sb, "Lair Actions", r.LairActions);
        WriteRegionalEffects(sb, r.RegionalEffects);

        WriteEquipment(sb, r.Equipment);
        WriteNotes(sb, r.Notes);
        WriteFooter(sb, "legendary");
        WriteDocEnd(sb);
        return sb.ToString();
    }

    // ── Private helpers ────────────────────────────────────────────────────────

    private static void WriteDocStart(StringBuilder sb, string title)
    {
        sb.AppendLine($"""
            <!DOCTYPE html>
            <html lang="en">
            <head>
              <meta charset="UTF-8">
              <meta name="viewport" content="width=device-width, initial-scale=1.0">
              <title>Character Sheet &mdash; {Enc(title)}</title>
              <style>{Css}</style>
            </head>
            <body>
            <div class="sheet">
            """);
    }

    private static void WriteDocEnd(StringBuilder sb) =>
        sb.AppendLine("</div></body></html>");

    private static void WriteHeader(
        StringBuilder sb, string name, int level,
        string? race, string? @class, string? alignment)
    {
        sb.AppendLine("<div class=\"header\">");
        sb.AppendLine($"  <div class=\"char-name\">{Enc(name)}</div>");
        sb.AppendLine("  <div class=\"header-meta\">");
        if (!string.IsNullOrWhiteSpace(@class))
            sb.AppendLine($"    <span>{Enc(@class)}</span>");
        if (!string.IsNullOrWhiteSpace(race))
            sb.AppendLine($"    <span>{Enc(race)}</span>");
        if (level > 0)
            sb.AppendLine($"    <span>Level {level}</span>");
        if (!string.IsNullOrWhiteSpace(alignment))
            sb.AppendLine($"    <span>{Enc(alignment)}</span>");
        sb.AppendLine("  </div>");
        sb.AppendLine("</div>");
    }

    private static void WriteStatsBar(
        StringBuilder sb, string? hp, int ac, string? speed,
        string? cr, string? profBonus)
    {
        sb.AppendLine("<div class=\"stats-bar\">");
        WritePill(sb, "Hit Points", hp ?? "—");
        WritePill(sb, "Armor Class", ac > 0 ? ac.ToString() : "—");
        WritePill(sb, "Speed", speed ?? "—");
        if (cr is not null)   WritePill(sb, "Challenge", cr);
        if (profBonus is not null) WritePill(sb, "Prof. Bonus", profBonus);
        sb.AppendLine("</div>");
    }

    private static void WritePill(StringBuilder sb, string label, string value)
    {
        sb.AppendLine($"""
            <div class="stat-pill">
              <div class="stat-label">{Enc(label)}</div>
              <div class="stat-value">{Enc(value)}</div>
            </div>
            """);
    }

    private static void WriteAbilityScores(StringBuilder sb, AbilityScores? scores)
    {
        if (scores is null) return;
        var abilities = new[]
        {
            ("STR", scores.Strength),
            ("DEX", scores.Dexterity),
            ("CON", scores.Constitution),
            ("INT", scores.Intelligence),
            ("WIS", scores.Wisdom),
            ("CHA", scores.Charisma),
        };

        sb.AppendLine("<div class=\"ability-grid\">");
        foreach (var (abbr, score) in abilities)
        {
            if (score is null) continue;
            sb.AppendLine($"""
                <div class="ability-box">
                  <div class="ability-name">{abbr}</div>
                  <div class="ability-score">{score.Score}</div>
                  <div class="ability-mod">{Enc(score.Modifier)}</div>
                </div>
                """);
        }
        sb.AppendLine("</div>");
    }

    private static void WriteKvSection(StringBuilder sb, string title, IDictionary<string, string>? dict)
    {
        sb.AppendLine("<div class=\"section\">");
        sb.AppendLine($"  <div class=\"section-title\">{Enc(title)}</div>");
        sb.AppendLine("  <div class=\"section-body\">");
        if (dict is { Count: > 0 })
        {
            sb.AppendLine("  <ul class=\"kv-list\">");
            foreach (var (k, v) in dict)
                sb.AppendLine($"    <li><span class=\"kv-key\">{Enc(k)}:</span> {Enc(v)}</li>");
            sb.AppendLine("  </ul>");
        }
        else
        {
            sb.AppendLine("  <em style=\"color:#aaa\">—</em>");
        }
        sb.AppendLine("  </div></div>");
    }

    private static void WriteKvSectionFull(StringBuilder sb, string title, IDictionary<string, string>? dict)
    {
        if (dict is null or { Count: 0 }) return;
        sb.AppendLine("<div class=\"full-width\"><div class=\"section\">");
        sb.AppendLine($"  <div class=\"section-title\">{Enc(title)}</div>");
        sb.AppendLine("  <div class=\"section-body\"><ul class=\"kv-list\">");
        foreach (var (k, v) in dict)
        {
            if (k.Equals("Spellcasting", StringComparison.OrdinalIgnoreCase))
                WriteSpellcastingEntry(sb, v);
            else
                sb.AppendLine($"    <li><span class=\"kv-key\">{Enc(k)}:</span> {Enc(v)}</li>");
        }
        sb.AppendLine("  </ul></div></div></div>");
    }

    private static void WriteSpellcastingEntry(StringBuilder sb, string value)
    {
        var parts = value.Split("\n\n", StringSplitOptions.RemoveEmptyEntries);

        sb.AppendLine("    <li class=\"spellcasting-entry\">");
        sb.AppendLine("      <span class=\"kv-key\">Spellcasting:</span>");

        if (parts.Length == 0) { sb.AppendLine("    </li>"); return; }

        sb.AppendLine($"      <div class=\"spell-preamble\">{Enc(parts[0].Trim())}</div>");

        if (parts.Length > 1)
        {
            sb.AppendLine("      <div class=\"spell-levels\">");
            for (int i = 1; i < parts.Length; i++)
            {
                var line = parts[i].Trim();
                var colonIdx = line.IndexOf(':');
                if (colonIdx < 0)
                {
                    sb.AppendLine($"        <div class=\"spell-level-row\"><span>{Enc(line)}</span></div>");
                    continue;
                }

                var levelLabel = line[..colonIdx].Trim();
                var spellsRaw  = line[(colonIdx + 1)..].Trim();
                var spells     = spellsRaw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

                sb.AppendLine("        <div class=\"spell-level-row\">");
                sb.AppendLine($"          <span class=\"spell-level-label\">{Enc(levelLabel)}</span>");
                sb.AppendLine("          <div class=\"spell-tags\">");
                foreach (var spell in spells)
                    sb.AppendLine($"            <span class=\"spell-tag\">{Enc(spell)}</span>");
                sb.AppendLine("          </div>");
                sb.AppendLine("        </div>");
            }
            sb.AppendLine("      </div>");
        }

        sb.AppendLine("    </li>");
    }

    private static void WriteTextSection(StringBuilder sb, string title, string? text)
    {
        sb.AppendLine("<div class=\"section\">");
        sb.AppendLine($"  <div class=\"section-title\">{Enc(title)}</div>");
        sb.AppendLine($"  <div class=\"section-body\">{(string.IsNullOrWhiteSpace(text) ? "<em style=\"color:#aaa\">—</em>" : Enc(text))}</div>");
        sb.AppendLine("</div>");
    }

    private static void WriteTextSectionFull(StringBuilder sb, string title, string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return;
        sb.AppendLine("<div class=\"full-width\"><div class=\"section\">");
        sb.AppendLine($"  <div class=\"section-title\">{Enc(title)}</div>");
        sb.AppendLine($"  <div class=\"section-body\">{Enc(text)}</div>");
        sb.AppendLine("</div></div>");
    }

    private static void WriteLegendaryActions(StringBuilder sb, LegendaryActions? la)
    {
        if (la is null) return;
        sb.AppendLine("<div class=\"full-width\"><div class=\"section\">");
        sb.AppendLine("  <div class=\"section-title\">Legendary Actions</div>");
        sb.AppendLine("  <div class=\"section-body\">");
        if (!string.IsNullOrWhiteSpace(la.LegendaryActionUses))
            sb.AppendLine($"    <p><strong>Uses per round:</strong> {Enc(la.LegendaryActionUses)}</p>");
        if (la.Options is { Count: > 0 })
        {
            sb.AppendLine("    <ul class=\"kv-list\" style=\"margin-top:.4rem\">");
            foreach (var (k, v) in la.Options)
                sb.AppendLine($"      <li><span class=\"kv-key\">{Enc(k)}:</span> {Enc(v)}</li>");
            sb.AppendLine("    </ul>");
        }
        sb.AppendLine("  </div></div></div>");
    }

    private static void WriteMythicTrait(StringBuilder sb, MythicTrait? mt)
    {
        if (mt is null || string.IsNullOrWhiteSpace(mt.Name)) return;
        sb.AppendLine("<div class=\"full-width\"><div class=\"section\">");
        sb.AppendLine($"  <div class=\"section-title\">Mythic Trait &mdash; {Enc(mt.Name)}</div>");
        sb.AppendLine($"  <div class=\"section-body\">{Enc(mt.Description)}</div>");
        sb.AppendLine("</div></div>");
    }

    private static void WriteRegionalEffects(StringBuilder sb, IList<string>? effects)
    {
        if (effects is null or { Count: 0 }) return;
        sb.AppendLine("<div class=\"full-width\"><div class=\"section\">");
        sb.AppendLine("  <div class=\"section-title\">Regional Effects</div>");
        sb.AppendLine("  <div class=\"section-body\"><ul class=\"kv-list\">");
        foreach (var e in effects)
            sb.AppendLine($"    <li>{Enc(e)}</li>");
        sb.AppendLine("  </ul></div></div></div>");
    }

    private static void WriteEquipment(StringBuilder sb, Equipment? eq)
    {
        if (eq is null) return;
        sb.AppendLine("<div class=\"full-width\"><div class=\"section\">");
        sb.AppendLine("  <div class=\"section-title\">Equipment</div>");
        sb.AppendLine("  <div class=\"section-body\"><div class=\"equip-grid\">");
        sb.AppendLine($"    <div><div class=\"equip-label\">Armor</div>{Enc(eq.Armor)}</div>");
        sb.AppendLine($"    <div><div class=\"equip-label\">Weapons</div>{Enc(eq.Weapons)}</div>");
        sb.AppendLine($"    <div><div class=\"equip-label\">Other</div>{Enc(eq.Other)}</div>");
        sb.AppendLine("  </div></div></div></div>");
    }

    private static void WriteNotes(StringBuilder sb, string? notes)
    {
        sb.AppendLine("<div class=\"full-width\"><div class=\"section\">");
        sb.AppendLine("  <div class=\"section-title\">Notes</div>");
        sb.AppendLine($"  <div class=\"notes-body\">{(string.IsNullOrWhiteSpace(notes) ? "" : Enc(notes))}</div>");
        sb.AppendLine("</div></div>");
    }

    private static void WriteFooter(StringBuilder sb, string sheetType)
    {
        sb.AppendLine($"""
            <div class="footer">D&amp;D Character Sheet &mdash; {sheetType} &mdash; rendered by DandD Template Parser</div>
            """);
    }

    // HTML-encode user content to prevent XSS
    private static string Enc(string? s) =>
        string.IsNullOrEmpty(s) ? string.Empty : System.Net.WebUtility.HtmlEncode(s);
}
