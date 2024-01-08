The intended use for thi executable is to be inserted as an autostart entry inside the .SCU file of a project in development fase, to make the demo last significatly longer.

How to use:
FAQ.exe /fixVersion:X [/demoLenght:(0-32767)] [/logLevel:(0-4)] [/readOnly] [/getVersion] [/help]

Examples:
FAQ.exe /fixVersion:65
FAQ.exe /fixVersion:65 /demoLenght:32767
FAQ.exe /fixVersion:65 /logLevel:4
FAQ.exe /fixVersion:65 /demoLenght:234 /logLevel:2

fixVersion:
The parameter fixVersion, specify the iFix's version of which the user intends to modify the demo lenght.
It is necessary to find in the file "FixVersions.xml", found in the same directory as the .exe, the right offset in memory.
When using CheatEngine to modify the demo lenght, the address of the signed two bytes which represents the left minutes of the demo (120 at the start by default), is specified with a format of fix.exe+X;
The "X" is the offset of the memory area, and is expressed in Hexadecimal, while in the .xml is expressed in decimal, therefore, a conversion in needded when adding new versions.

demoLenght:
It represend the value at which the memory address will be set at;
By default it is 720, if it is not passed as a parameter.
Negative numbers are possible, but non-sense.
32767 is the max number.

logLevel:
It range from 0 to 4 and represents the level of details the executable will comunicate to the user what is going on.
0: Critical
1: Error
2: Warning
3: Info
4: Debug

readOnly:
Will print the current demoLenght stored in the memory address, with no additional decoration, if the logLevel is below Info.
Usefull to automate some procedure or diagnosis.
note: this parameter will ONLY print the remaining demo time, even if provided, it will prevent the write of a new demoLenght.

getVersion:
Will ONLY print the version of the EXE.

By: Ex_FST; Based on an idea and with the contribution of Shylix12