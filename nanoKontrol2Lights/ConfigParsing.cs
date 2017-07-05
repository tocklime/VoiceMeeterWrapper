using System;
using System.Collections.Generic;
using System.Linq;
using static LanguageExt.Prelude;
using LanguageExt.Parsec;
using static LanguageExt.Parsec.Prim;
using static LanguageExt.Parsec.Char;
using LanguageExt;

namespace nanoKontrol2Lights
{
    public static class ConfigParsing
    {
        public static Parser<BindingDir> binding =>
            from l in optional(ch('<'))
            from _ in ch('=')
            from r in optional(ch('>'))
            select (l.IsSome ? BindingDir.ToBoard : 0) | (r.IsSome ? BindingDir.FromBoard : 0);
        public static Parser<Option<int>> boardControlId => asInteger(many(digit));
        public static Parser<string> voiceMeeterId => asString(manyUntil(anyChar, ch(';')));

        public class BindingLine
        {
            public BindingDir Dir { get; set; }
            public int ControlId { get; set; }
            public string VoicemeeterParam { get; set; }
        }
        public static Parser<Unit> comment =>
            from _1 in ch('#')
            from _2 in many(noneOf('\n'))
            select unit;

        public static Parser<BindingLine> readBindLine =>
            from _ in many(choice(skipMany1(space), comment))
            from oc in boardControlId
            where oc.IsSome
            from _1 in spaces
            from b in binding
            from _2 in spaces
            from v in voiceMeeterId
            select new BindingLine
            {
                Dir = b,
                ControlId = oc.Single(),
                VoicemeeterParam = v
            };
        public static IEnumerable<BindingLine> ParseConfig(string text)
        {
            var x = parse(many(readBindLine), text);
            if (x.IsFaulted)
            {
                throw new Exception($"Bad parse in config file: {x.Reply.Error}");
            }
            return x.Reply.Result.AsEnumerable();
        }
    }
}
