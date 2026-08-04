
using Contensive.Models.Db;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
//
namespace Contensive.Processor.Addons.Diagnostics {
    /// <summary>
    /// CLI addon that scans all dbText records for embedded content commands
    /// in any of the three legacy formats: AC tags, IMG tags with AC-encoded IDs, and {% %} JSON tags.
    /// Run from command line: cc -a appname -r "Find Legacy Content Commands"
    /// </summary>
    public class FindLegacyContentCommandsAddon : Contensive.BaseClasses.AddonBaseClass {
        //
        //====================================================================================================
        /// <summary>
        /// addon entry point
        /// </summary>
        public override object Execute(Contensive.BaseClasses.CPBaseClass cp) {
            try {
                var records = DbBaseModel.createList<DbTextModel>(cp, "", "name");
                var report = new StringBuilder();
                int recordsWithCommands = 0;
                int totalAcTags = 0;
                int totalImgTags = 0;
                int totalJsonTags = 0;
                //
                report.AppendLine("Find Legacy Content Commands Report");
                report.AppendLine("====================================");
                report.AppendLine();
                //
                foreach (var record in records) {
                    if (string.IsNullOrEmpty(record.text)) { continue; }
                    //
                    var commands = LegacyContentCommandDetector.detect(record.text);
                    if (commands.Count == 0) { continue; }
                    //
                    recordsWithCommands++;
                    report.AppendLine($"dbText id: {record.id}, name: \"{record.name}\", ccguid: {record.ccguid}");
                    foreach (var command in commands) {
                        report.AppendLine($"  [{command.formatLabel}] addon: \"{command.addonName}\"");
                        switch (command.format) {
                            case LegacyCommandFormat.AcTag: {
                                    totalAcTags++;
                                    break;
                                }
                            case LegacyCommandFormat.ImgTag: {
                                    totalImgTags++;
                                    break;
                                }
                            case LegacyCommandFormat.JsonTag: {
                                    totalJsonTags++;
                                    break;
                                }
                        }
                    }
                    report.AppendLine();
                }
                //
                int totalCommands = totalAcTags + totalImgTags + totalJsonTags;
                report.AppendLine("====================================");
                report.AppendLine($"Summary: {recordsWithCommands} dbText records found with embedded commands ({totalCommands} total commands)");
                report.AppendLine($"  AC tags: {totalAcTags}");
                report.AppendLine($"  IMG tags: {totalImgTags}");
                report.AppendLine($"  {{% %}} commands: {totalJsonTags}");
                //
                return report.ToString();
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex);
                return $"ERROR, unexpected exception during Find Legacy Content Commands: {ex.Message}";
            }
        }
    }
    //
    //====================================================================================================
    /// <summary>
    /// The format type of a detected legacy content command
    /// </summary>
    public enum LegacyCommandFormat {
        AcTag,
        ImgTag,
        JsonTag
    }
    //
    //====================================================================================================
    /// <summary>
    /// Represents a single detected legacy content command
    /// </summary>
    public class LegacyContentCommand {
        public LegacyCommandFormat format { get; set; }
        public string addonName { get; set; }
        public string matchedText { get; set; }
        //
        public string formatLabel {
            get {
                switch (format) {
                    case LegacyCommandFormat.AcTag: { return "AC tag"; }
                    case LegacyCommandFormat.ImgTag: { return "IMG tag"; }
                    case LegacyCommandFormat.JsonTag: { return "{% %}"; }
                    default: { return "unknown"; }
                }
            }
        }
    }
    //
    //====================================================================================================
    /// <summary>
    /// Static detector for legacy content commands in HTML text.
    /// Extracted from the addon so it can be unit tested without a database.
    /// </summary>
    public static class LegacyContentCommandDetector {
        //
        // -- AC tag pattern: <AC type="..." name="..." ...>
        private static readonly Regex acTagPattern = new Regex(
            @"<AC\s[^>]*>",
            RegexOptions.IgnoreCase | RegexOptions.Compiled
        );
        //
        // -- name attribute inside an AC tag
        private static readonly Regex acNamePattern = new Regex(
            @"\bname\s*=\s*""([^""]*)""\s*",
            RegexOptions.IgnoreCase | RegexOptions.Compiled
        );
        //
        // -- IMG tag with AC-encoded id: <img id="AC,TYPE,num,AddonName,..." ...>
        private static readonly Regex imgTagPattern = new Regex(
            @"<img\s[^>]*\bid\s*=\s*""AC,([^""]*)""\s*[^>]*>",
            RegexOptions.IgnoreCase | RegexOptions.Compiled
        );
        //
        // -- {% %} JSON command blocks
        private static readonly Regex jsonTagPattern = new Regex(
            @"\{%(.+?)%\}",
            RegexOptions.Compiled | RegexOptions.Singleline
        );
        //
        // -- extract addon name from {% %} JSON content: {"addon":{"addon":"Name"}} or addon "Name"
        private static readonly Regex jsonAddonNamePattern = new Regex(
            @"""addon""\s*:\s*\{\s*""addon""\s*:\s*""([^""]+)""",
            RegexOptions.Compiled
        );
        private static readonly Regex jsonAddonShortPattern = new Regex(
            @"^\s*addon\s+""([^""]+)""",
            RegexOptions.Compiled
        );
        //
        //====================================================================================================
        /// <summary>
        /// Detect all legacy content commands in the given HTML text
        /// </summary>
        public static List<LegacyContentCommand> detect(string text) {
            var results = new List<LegacyContentCommand>();
            if (string.IsNullOrEmpty(text)) { return results; }
            //
            // -- Format 1: AC tags
            foreach (Match match in acTagPattern.Matches(text)) {
                string addonName = "";
                var nameMatch = acNamePattern.Match(match.Value);
                if (nameMatch.Success) {
                    addonName = nameMatch.Groups[1].Value;
                }
                results.Add(new LegacyContentCommand {
                    format = LegacyCommandFormat.AcTag,
                    addonName = addonName,
                    matchedText = match.Value
                });
            }
            //
            // -- Format 2: IMG tags with AC-encoded IDs
            foreach (Match match in imgTagPattern.Matches(text)) {
                string addonName = "";
                string idContent = match.Groups[1].Value;
                // id format: TYPE,num,AddonName,params,guid
                string[] parts = idContent.Split(',');
                if (parts.Length >= 3) {
                    addonName = parts[2].Trim();
                }
                results.Add(new LegacyContentCommand {
                    format = LegacyCommandFormat.ImgTag,
                    addonName = addonName,
                    matchedText = match.Value
                });
            }
            //
            // -- Format 3: {% %} JSON commands
            foreach (Match match in jsonTagPattern.Matches(text)) {
                string addonName = "";
                string inner = match.Groups[1].Value;
                //
                // -- try full JSON format: {"addon":{"addon":"Name",...}}
                var jsonMatch = jsonAddonNamePattern.Match(inner);
                if (jsonMatch.Success) {
                    addonName = jsonMatch.Groups[1].Value;
                } else {
                    // -- try short format: addon "Name"
                    var shortMatch = jsonAddonShortPattern.Match(inner);
                    if (shortMatch.Success) {
                        addonName = shortMatch.Groups[1].Value;
                    }
                }
                results.Add(new LegacyContentCommand {
                    format = LegacyCommandFormat.JsonTag,
                    addonName = addonName,
                    matchedText = match.Value
                });
            }
            //
            return results;
        }
    }
}
