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
        private static int GetNanoKontrolInputDevice()
        {
            for(int i = 0; i < InputDevice.DeviceCount; i++)
            {
                var info = InputDevice.GetDeviceCapabilities(i);
                if (info.name.Contains("nanoKONTROL"))
                    return i;
            }
            throw new Exception("Cannot find input midi device with 'nanoKONTROL' in the name.");
        }
        private static int GetNanoKontrolOutputDevice()
        {
            for(int i = 0; i < OutputDeviceBase.DeviceCount; i++)
            {
                var info = OutputDeviceBase.GetDeviceCapabilities(i);
                if (info.name.Contains("nanoKONTROL"))
                    return i;
            }
            throw new Exception("Cannot find output midi device with 'nanoKONTROL' in the name.");
        }
        private static void SetLight(OutputDevice od, int controlNum, float value)
        {
            od.Send(new ChannelMessage(ChannelCommand.Controller, 0, controlNum, (int)value*127));
        }
        static void Main(string[] args)
        {

            using (var od = new OutputDevice(GetNanoKontrolOutputDevice()))
            using (var id = new InputDevice(GetNanoKontrolInputDevice()))
            using (var vb = new VmClient())
            {
                //voicemeeter doesn't have midi bindings for arm/disarm recording. We'll do it ourselves.
                //Note that the voicemeeter UI doesn't update until you start recording something.
                id.ChannelMessageReceived += (ob,e)=>
                {
                    var m = e.Message;
                    if (m.MessageType == MessageType.Channel && m.Command == ChannelCommand.Controller && m.Data2 == 127)
                    {
                        if(m.Data1 >= 64 && m.Data1 <= 68)
                        {
                            var slider = m.Data1 - 64;
                            var current = vb.GetRecorderArmStrip(slider);
                            vb.SetRecorderArmStrip(slider,current != 1);
                        }
                    }
                };
                id.StartRecording();
                vb.OnClose(() => ClearAllLights(od));
                while (!Console.KeyAvailable)
                {
                    if (vb.Poll())
                    {
                        SetAllLights(od, vb);
                    }
                    else
                    {
                        Thread.Sleep(20);
                    }
                }
            }
        }
        private static void ClearAllLights(OutputDevice od)
        {
            var all = new[] { 32, 33, 34, 35, 36, 41, 45, 48, 49, 50, 51, 52, 53, 54, 55, 64, 65, 66, 67, 68 };
            foreach(var a in all)
            {
                SetLight(od, a, 0);
            }
        }
        private static void SetAllLights(OutputDevice od, VmClient vb)
        {
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
