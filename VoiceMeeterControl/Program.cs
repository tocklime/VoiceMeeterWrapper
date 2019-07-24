using Sanford.Multimedia.Midi;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using VoiceMeeterWrapper;

namespace VoiceMeeterControl
{
    class Program
    {
        private static int GetMidiInputDevice(string partialDeviceName)
        {
            for (int i = 0; i < InputDevice.DeviceCount; i++)
            {
                var info = InputDevice.GetDeviceCapabilities(i);
                if (info.name.Contains(partialDeviceName))
                    return i;
            }
            throw new Exception($"Cannot find input midi device with '{partialDeviceName}' in the name.");
        }
        private static int GetMidiOutputDevice(string partialDeviceName)
        {
            for (int i = 0; i < OutputDeviceBase.DeviceCount; i++)
            {
                var info = OutputDeviceBase.GetDeviceCapabilities(i);
                if (info.name.Contains(partialDeviceName))
                    return i;
            }
            throw new Exception($"Cannot find output midi device with '{partialDeviceName}' in the name.");
        }
        private static void SetLight(OutputDevice od, int controlNum, float value)
        {
            od.Send(new ChannelMessage(ChannelCommand.Controller, 0, controlNum, (int)value * 127));
        }
        private static float Scale(float value, float fromMin, float fromMax, float toMin, float toMax)
        {
            var zeroToOne = ((value - fromMin) / (fromMax - fromMin));
            var ans =  zeroToOne * (toMax - toMin) + toMin;
            //Console.WriteLine($"Scale {value} from {fromMin}..{fromMax} to {toMin}..{toMax}: {zeroToOne} {ans}");
            return ans;
        }
        public static string LoadConfig()
        {
            //directories to try:
            var confDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),"VoiceMeeterControl");
            if (!Directory.Exists(confDir))
            {
                Directory.CreateDirectory(confDir);
            }
            var conf = Path.Combine(confDir,"config.txt");
            if (!File.Exists(conf))
            {
                //Write it out.
                File.WriteAllText(conf, LoadInternalConfig());
            }
            return File.ReadAllText(conf);
        }
        public static string LoadInternalConfig()
        {
            using (var str = Assembly.GetEntryAssembly().GetManifestResourceStream("VoiceMeeterControl.config.txt"))
            using (var reader = new StreamReader(str))
            {
                var fromInternal = reader.ReadToEnd();
                return fromInternal;
            }

        }
        private delegate bool ConsoleCtrlHandlerDelegate(int sig);
        [DllImport("Kernel32")]
        private static extern bool SetConsoleCtrlHandler(ConsoleCtrlHandlerDelegate handler, bool add);
        private static bool shouldStop = false;
        private static ConsoleCtrlHandlerDelegate _handler;
        private static bool Handler(int sig)
        {
            Console.WriteLine("Exiting...");
            //Thread.Sleep(5000);
            foreach(var x in disps)
            {
                x.Dispose();
            }
            Console.WriteLine("Cleanup done...");
            shouldStop = true;
            return true;
        }
        private static List<IDisposable> disps = new List<IDisposable>();
        static void Main(string[] args)
        {
            _handler += new ConsoleCtrlHandlerDelegate(Handler);
            SetConsoleCtrlHandler(_handler, true);
            var internalConfig = args.Length > 0 && args[0] == "internal-config";
            var confTxt = internalConfig ? LoadInternalConfig() : LoadConfig();
            var config = ConfigParsing.ParseConfig(confTxt);
            var inputMap = config.Bindings.Where(x => (x.Dir & BindingDir.FromBoard) != 0).ToDictionary(x => x.ControlId);
            using (var vb = new VmClient())
            using (var od = new OutputDevice(GetMidiOutputDevice(config.DeviceName)))
            using (var id = new InputDevice(GetMidiInputDevice(config.DeviceName)))
            {
                disps.Add(vb);
                disps.Add(id);
                disps.Add(od);
                //voicemeeter doesn't have midi bindings for arm/disarm recording. We'll do it ourselves.
                //Note that the voicemeeter UI doesn't update until you start recording something.
                Console.WriteLine("All initialised");
                id.ChannelMessageReceived += (ob, e) =>
                {
                    var m = e.Message;
                    if (m.MessageType == MessageType.Channel && m.Command == ChannelCommand.Controller)
                    {
                        if (inputMap.ContainsKey(m.Data1))
                        {
                            var v = inputMap[m.Data1];
                            if(v.ControlToggle && m.Data2 == v.ControlTo)
                            {
                                var current = vb.GetParam(v.VoicemeeterParam);
                                vb.SetParam(v.VoicemeeterParam, v.VmTo - current);
                            }
                            else if(!v.ControlToggle)
                            {
                                var scaledVal = Scale(m.Data2, v.ControlFrom, v.ControlTo, v.VmFrom, v.VmTo);
                                vb.SetParam(v.VoicemeeterParam, scaledVal);
                            }
                        }
                    }
                };
                id.StartRecording();
                vb.OnClose(() =>
                {
                    foreach (var x in config.Bindings.Where(x => (x.Dir & BindingDir.ToBoard) != 0))
                    {
                        od.Send(new ChannelMessage(ChannelCommand.Controller, 0, x.ControlId, (int)x.ControlFrom));
                    }
                });
                while (!shouldStop)
                {
                    if (vb.Poll())
                    {
                        foreach (var x in config.Bindings.Where(x => (x.Dir & BindingDir.ToBoard) != 0))
                        {
                            var vmVal = vb.GetParam(x.VoicemeeterParam);
                            var scaled = Scale(vmVal, x.VmFrom, x.VmTo, x.ControlFrom, x.ControlTo);
                            od.Send(new ChannelMessage(ChannelCommand.Controller, 0, x.ControlId, (int)scaled));
                        }
                    }
                    else
                    {
                        Thread.Sleep(20);
                    }
                }
            }
        }

        private static void xConsole_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            throw new NotImplementedException();
        }
    }
}
