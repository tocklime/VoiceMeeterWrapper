using Microsoft.Win32;
using System;

namespace VoiceMeeterWrapper
{
    public class VmClient : IDisposable
    {
        private Action _onClose = null;
        private string GetVoicemeeterDir()
        {
            const string regKey = @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall";
            const string uninstKey = "VB:Voicemeeter {17359A74-1236-5467}";
            var key = $"{regKey}\\{uninstKey}";
            var k = Registry.GetValue(key, "UninstallString", null);
            if (k == null)
            {
                throw new Exception("Voicemeeter not found");
            }
            return System.IO.Path.GetDirectoryName(k.ToString());
        }
        public VmClient()
        {
            //Find Voicemeeter dir.
            var vmDir = GetVoicemeeterDir();
            VoiceMeeterRemote.LoadDll(System.IO.Path.Combine(vmDir, "VoicemeeterRemote.dll"));
            var lr = VoiceMeeterRemote.Login();
            switch (lr)
            {
                case VbLoginResponse.OK:
                case VbLoginResponse.AlreadyLoggedIn:
                    break;
                case VbLoginResponse.OkVoicemeeterNotRunning:
                    //Launch.
                    break;
                default:
                    throw new InvalidOperationException("Bad response from voicemeeter: " + lr);
            }
        }
        public float GetParam(string n)
        {
            float output = -1;
            VoiceMeeterRemote.GetParameter(n, ref output);
            return output;
        }
        public void SetParam(string n,float v)
        {
            VoiceMeeterRemote.SetParameter(n, v);
        }
        public bool Poll()
        {
            return VoiceMeeterRemote.IsParametersDirty() == 1;
        }
        public void Dispose()
        {
            _onClose?.Invoke();
            VoiceMeeterRemote.Logout();
        }
        public void OnClose(Action a)
        {
            _onClose = a;
        }
    }
}
