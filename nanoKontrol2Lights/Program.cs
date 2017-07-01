using Sanford.Multimedia.Midi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using VoiceMeeterWrapper;

namespace nanoKontrol2Lights
{
    class Program
    {
        private static int GetNanoKontrolDevice()
        {
            for(int i = 0; i < OutputDevice.DeviceCount; i++)
            {
                var info = OutputDevice.GetDeviceCapabilities(i);
                if (info.name.Contains("nanoKONTROL"))
                    return i;
            }
            return -1;
        }
        private static void SetLight(OutputDevice od, int controlNum, float value)
        {
            od.Send(new ChannelMessage(ChannelCommand.Controller, 0, controlNum, (int)value*127));
        }
        static void Main(string[] args)
        {

            using (var od = new OutputDevice(GetNanoKontrolDevice()))
            using (var vb = new VmClient())
            {
                vb.OnClose(() =>
                {
                    var all = new[] { 32, 33, 34, 35, 36, 41, 45, 48, 49, 50, 51, 52, 53, 54, 55, 64, 65, 66, 67, 68 };
                    foreach(var a in all)
                    {
                        SetLight(od, a, 0);
                    }
                });
                od.Reset();
                while (!Console.KeyAvailable)
                {
                    while (!vb.Poll() && !Console.KeyAvailable) { Thread.Sleep(20); }
                    //Solo inputs
                    SetLight(od, 32, vb.GetParam(StripNumProperty.Solo, 0));
                    SetLight(od, 33, vb.GetParam(StripNumProperty.Solo, 1));
                    SetLight(od, 34, vb.GetParam(StripNumProperty.Solo, 2));
                    SetLight(od, 35, vb.GetParam(StripNumProperty.Solo, 3));
                    SetLight(od, 36, vb.GetParam(StripNumProperty.Solo, 4));

                    //Mute inputs
                    SetLight(od, 48, vb.GetParam(StripNumProperty.Mute, 0));
                    SetLight(od, 49, vb.GetParam(StripNumProperty.Mute, 1));
                    SetLight(od, 50, vb.GetParam(StripNumProperty.Mute, 2));
                    SetLight(od, 51, vb.GetParam(StripNumProperty.Mute, 3));
                    SetLight(od, 52, vb.GetParam(StripNumProperty.Mute, 4));

                    //Mute outputs
                    SetLight(od, 53, vb.GetParam(BusNumProperty.Mute, 0));
                    SetLight(od, 54, vb.GetParam(BusNumProperty.Mute, 1));
                    SetLight(od, 55, vb.GetParam(BusNumProperty.Mute, 2));

                    //Recording buttons:
                    SetLight(od, 45, vb.GetParam(RecorderProperty.record));
                    SetLight(od, 41, vb.GetParam(RecorderProperty.play));

                    //Record lights:
                    SetLight(od, 64, vb.GetRecorderArmStrip(0));
                    SetLight(od, 65, vb.GetRecorderArmStrip(1));
                    SetLight(od, 66, vb.GetRecorderArmStrip(2));
                    SetLight(od, 67, vb.GetRecorderArmStrip(3));
                    SetLight(od, 68, vb.GetRecorderArmStrip(4));

                }
            }
        }
    }
}
