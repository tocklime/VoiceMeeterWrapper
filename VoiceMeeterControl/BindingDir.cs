using System;

namespace VoiceMeeterControl
{
    [Flags]
    public enum BindingDir
    {
        ToBoard = 1,
        FromBoard = 2,
        TwoWay = 3,
    }
}
