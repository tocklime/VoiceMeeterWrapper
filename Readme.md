# VoiceMeeter Wrapper and simple client application to interface with nanokontrol 2

## Build info

Open the project in Visual Studio, run 'VoiceMeeterControl'. This relies on Voicemeeter being installed, and that the 
code in [/VoiceMeeterWrapper/VmClient.cs](/VoiceMeeterWrapper/VmClient.cs) function `GetVoiceMeeterDir()` can find your voicmeeter install (works for me!)

## Config info

The config file is made up of lines. There are 2 types of line:

1. A line beginning `MIDI Device: ` will set the midi device to use. Multiple instances of a line like this won't do anything useful (Only the last will have an effect)
2. A binding line of the form `{Midi Id} [{Range}] [toggle] [{Binding}] {VM Id} [{Range}] ;`
   1. A Midi ID is an integer.
   2. A Range is two integers with `..` between. If specified, the range on the left will be scaled to the range on the right. You can use this to invert a slider by having a range on one side like `127..0` and `0..127` on the other. If range is not specified, then it defaults to `0..127` for the board side and `0..1` for the Voicemeeter side.
   3. A Binding is one of `<=>`, `<=` or `=>`. This controls which side updates the other (or both)
   4. A VM ID is something like `Strip[0].Gain`.

You can see a relatively complete config file at [/VoiceMeeterControl/config.txt](/VoiceMeeterControl/config.txt)