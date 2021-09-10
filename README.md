# Counter sample
Tested with dotnet version 5.0.202

### How to run 
```
dotnet run -p src/Counter
```

Gives this error
```
Unhandled exception. System.TypeInitializationException: The type initializer for '<StartupCode$Counter>.$Program' threw an exception.
 ---> System.IO.FileLoadException: Could not load file or assembly 'FsCodec, Version=2.0.0.0, Culture=neutral, PublicKeyToken=null'. The located assembly's manifest definition does not match the assembly reference. (0x80131040)
File name: 'FsCodec, Version=2.0.0.0, Culture=neutral, PublicKeyToken=null'
   --- End of inner exception stack trace ---
   at Program.main(String[] argv) in /Users/kristianlundstrom/prog/counter-sample/src/Counter/Program.fs:line 85
```