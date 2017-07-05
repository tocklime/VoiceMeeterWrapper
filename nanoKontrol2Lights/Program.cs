using Sanford.Multimedia.Midi;
using System;
using System.Linq;
using System.Threading;
using VoiceMeeterWrapper;
using LanguageExt;

namespace nanoKontrol2Lights
{
    class Program
    {
        private static int GetNanoKontrolInputDevice()
        {
            for (int i = 0; i < InputDevice.DeviceCount; i++)
            {
                var info = InputDevice.GetDeviceCapabilities(i);
                if (info.name.Contains("nanoKONTROL"))
                    return i;
            }
            throw new Exception("Cannot find input midi device with 'nanoKONTROL' in the name.");
        }
        private static int GetNanoKontrolOutputDevice()
        {
            for (int i = 0; i < OutputDeviceBase.DeviceCount; i++)
            {
                var info = OutputDeviceBase.GetDeviceCapabilities(i);
                if (info.name.Contains("nanoKONTROL"))
                    return i;
            }
            throw new Exception("Cannot find output midi device with 'nanoKONTROL' in the name.");
        }
        private static void SetLight(OutputDevice od, int controlNum, float value)
        {
            od.Send(new ChannelMessage(ChannelCommand.Controller, 0, controlNum, (int)value * 127));
        }
        static void Main(string[] args)
        {
            var confTxt = System.IO.File.ReadAllText("nanoKontrol2.txt");
            var config = ConfigParsing.ParseConfig(confTxt).ToList();
            var inputMap = config.Where(x => (x.Dir & BindingDir.FromBoard) != 0).ToDictionary(x => x.ControlId, x => x.VoicemeeterParam);

            using (var od = new OutputDevice(GetNanoKontrolOutputDevice()))
            using (var id = new InputDevice(GetNanoKontrolInputDevice()))
            using (var vb = new VmClient())
            {
                //voicemeeter doesn't have midi bindings for arm/disarm recording. We'll do it ourselves.
                //Note that the voicemeeter UI doesn't update until you start recording something.
                id.ChannelMessageReceived += (ob, e) =>
                {
                    var m = e.Message;
                    if (m.MessageType == MessageType.Channel && m.Command == ChannelCommand.Controller && m.Data2 == 127)
                    {
                        if (inputMap.ContainsKey(m.Data1))
                        {
                            var v = inputMap[m.Data1];
                            var current = vb.GetParam(v);
                            vb.SetParam(v, 1 - current);
                        }
                    }
                };
                id.StartRecording();
                vb.OnClose(() =>
                {
                    foreach (var x in config.Where(x => (x.Dir & BindingDir.ToBoard) != 0))
                    {
                        SetLight(od, x.ControlId, 0);
                    }
                });
                while (!Console.KeyAvailable)
                {
                    if (vb.Poll())
                    {
                        foreach (var x in config.Where(x => (x.Dir & BindingDir.ToBoard) != 0))
                        {
                            SetLight(od, x.ControlId, vb.GetParam(x.VoicemeeterParam));
                        }
                    }
                    else
                    {
                        Thread.Sleep(20);
                    }
                }
            }
        }
    }
}
