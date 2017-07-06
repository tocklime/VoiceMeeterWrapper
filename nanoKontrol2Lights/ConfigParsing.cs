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
        public static Parser<int> integer =>
            from sign in Parse.Char('-').Optional()
            from x in Parse.Number
            select (sign.IsDefined ? -1 : 1) * int.Parse(x);
        public static Parser<string> voiceMeeterId => Parse.Char(x => char.IsLetterOrDigit(x) || "()[].".IndexOf(x) > -1, "Letter, digit or ()[].").AtLeastOnce().Text().Token();

        public class BindingLine
        {
            public BindingDir Dir { get; set; }
            public int ControlId { get; set; }
            public string VoicemeeterParam { get; set; }
            public float ControlFrom { get; set; }
            public float ControlTo { get; set; }
            public bool ControlToggle { get; set; }
            public float VmFrom { get; set; }
            public float VmTo { get; set; }
        }
        public class DeviceLine
        {
            public string DeviceName { get; set; }
        }
        public static Parser<int> comment =>
            from _1 in Parse.Char('#')
            from _2 in Parse.Until(Parse.AnyChar, Parse.Char('\n'))
            select 0;

        public static Parser<Unit> readBindLine(Config co) =>
            from oc in integer.Token()
            from boardRange in range.Optional().Token()
            from isToggle in Parse.String("toggle").Optional().Token()
            from b in binding.Token()
            from v in voiceMeeterId.Token()
            from vmRange in range.Optional()
            from semi in Parse.Char(';')
            select co.Add(new BindingLine
            {
                Dir = b,
                ControlId = oc,
                VoicemeeterParam = v,
                ControlFrom = boardRange.IsDefined ? boardRange.Get().Item1:0,
                ControlTo = boardRange.IsDefined ? boardRange.Get().Item2:127,
                VmFrom = vmRange.IsDefined ? vmRange.Get().Item1:0,
                VmTo = vmRange.IsDefined ? vmRange.Get().Item2:1,
                ControlToggle = isToggle.IsDefined
            });
        public static Parser<Unit> readComment =>
            from h in Parse.Char('#')
            from x in Parse.CharExcept('\n').AtLeastOnce()
            from nl in Parse.Char('\n')
            select Unit.unit;
        public static Parser<Unit> readDeviceLine(Config c) =>
            from h in Parse.String("MIDI Device: ")
            from devName in Parse.Letter.AtLeastOnce().Text().Token()
            select c.Add(devName);
        public static Parser<Unit> readConfigLine(Config c) =>
            readComment.Or(readDeviceLine(c)).Or(readBindLine(c)).Or(Parse.WhiteSpace.AtLeastOnce().Select(x=>Unit.unit));

        public static Parser<Tuple<int, int>> range =>
            from f in integer.Token()
            from _ in Parse.String("..")
            from t in integer.Token()
            select Tuple.Create(f, t);

        public static Config ParseConfig(string text)
        {
            var conf = new Config();

            var x = readConfigLine(conf).AtLeastOnce().End().TryParse(text);
            if (x.WasSuccessful)
                return conf;
            else
                throw new Exception($"Can't read config file: {x.Message} at {x.Remainder.Line}:{x.Remainder.Column}");
        }
        public class Config
        {
            public Config()
            {
                Bindings = new List<BindingLine>();
            }
            public string DeviceName { get; set; }
            public List<BindingLine> Bindings { get; set; }
            public Unit Add(string deviceName)
            {
                DeviceName = deviceName;
                return Unit.unit;
            }
            public Unit Add(BindingLine bl)
            {
                Bindings.Add(bl);
                return Unit.unit;
            }
        }
        public class Unit
        {
            private Unit()
            {

            }
            static Unit()
            {
                unit = new Unit();
            }
            public static Unit unit { private set; get; }
        }
    }
}
