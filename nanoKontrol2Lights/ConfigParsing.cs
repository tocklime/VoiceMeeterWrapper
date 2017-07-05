using System;
using System.Collections.Generic;
using System.Linq;
using Sprache;

namespace nanoKontrol2Lights
{
    public static class ConfigParsing
    {
        public static Parser<BindingDir> binding =>
            from l in Parse.Optional(Parse.Char('<'))
            from _ in Parse.Char('=')
            from r in Parse.Optional(Parse.Char('>'))
            select (l.IsDefined ? BindingDir.ToBoard : 0) | (r.IsDefined ? BindingDir.FromBoard : 0);
        public static Parser<int> boardControlId => Parse.Number.Select(x=>int.Parse(x));//.asInteger(many(digit));
        public static Parser<string> voiceMeeterId => Parse.Char(x => char.IsLetterOrDigit(x) || "()[].".IndexOf(x) > -1, "Letter, digit or ()[].").AtLeastOnce().Text().Token();

        public class BindingLine
        {
            public BindingDir Dir { get; set; }
            public int ControlId { get; set; }
            public string VoicemeeterParam { get; set; }
        }
        public static Parser<int> comment =>
            from _1 in Parse.Char('#')
            from _2 in Parse.Until(Parse.AnyChar, Parse.Char('\n'))
            select 0;

        public static Parser<BindingLine> readBindLine =>
            from c in readComment.Token().Many()
            from oc in boardControlId.Token()
            from b in binding.Token()
            from v in voiceMeeterId.Token()
            from semi in Parse.Char(';')
            select new BindingLine
            {
                Dir = b,
                ControlId = oc,
                VoicemeeterParam = v
            };
        public static Parser<int> readComment =>
            from h in Parse.Char('#')
            from x in Parse.CharExcept('\n').AtLeastOnce()
            from nl in Parse.Char('\n')
            select 0;
        public static IEnumerable<BindingLine> ParseConfig(string text)
        {
            var x = readBindLine.AtLeastOnce().TryParse(text);
            if (x.WasSuccessful)
                return x.Value;
            else
                throw new Exception($"Can't read config file: {x.Message}");
        }
    }
}
