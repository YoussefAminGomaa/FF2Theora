# Feeding Frenzy 2 Theora

FF2Theora is an program that changes the structure of an ogg theora file to an feeding frenzy 2 ogg theora file, it also supports cropping, changing video size, can also decode feeding frenzy 2 theora files back to ogg theora files and several much. It is written in C# .NET language.

## Building tutorial: 

In order to build & run the program, you need to first download and setup the .NET SDK 8 from the following website: https://dotnet.microsoft.com/en-us/download/dotnet/8.0.

Once the setup finishes, you need to open the command in the directory where the project is in, by clicking on the path type in the command: dotnet build

this will build the source code into an executable .exe file in an folder called "bin". you can now run the program. Enjoy!

If you want an already compiled application, you can so via joining our discord server: https://discord.gg/Rq8uHXhpZf. There is also older versions of ff2theora.

## Usage: 

A list of commands and their purposes:
### File output options:
* -o, --output:          Alternative output result file.
* -m, --mode:            Switches the mode operation. Available is decode (which decodes feeding frenzy 2 theora files).
* -y, --overwrite        Overwrites the exist output file rather asking to overwrite it.
* -c, --crop <n>:<n>:<n> Crops a range from data. The format should be like this: <packetNumberIndex>:<start>:<length>.
* -O, --old              Use the old method for processing ogg theora files. (not recommended).
* -r, --raw              Do not change anything to the packets. this will prevent the program to fix the video size(that includes padded green/black bar) during decode option (not recommended either).
* -u, --unsafe           Allows you to change the actual size (to be allocated) when using --width|--height command.
* -S, --size <n> [BETA]  Controls the limition size of packets during decoding. the more smaller = more bigger & more overhead. the value is multipled by 255 and it's maxvalue is 255 which is 255^2=65025. the defaultvalue is 255. the command is still in progress, due to the granule reason.
* -C, --comment          always exclude exist comments, except vendor. Rather asking to exclude it (if found).
* -P, --serial <n>       Process a specified serial number. This is useful when processing a multiple streams. They can be viewed using oggz-info.

### Theora output options:
* -W, -X, --width <n>    Changes the video size width in binary data. Useful for several stuff.
* -H, -Y, --height <n>   Same as previous but height.

### Dump output options:
* -g, --granule          Hide the packet granule position timestamp;
* -p, --packetno         Hide the packet sequence number;
* -s, --serialno         Hide the packet serial number;
* -d, --dump             Dump the theora file using all of these options;

### Miscellaneous:
* -n, --nologo           Suppress the copyright logo.
* -v, --visit            Let's you open the browser which has the url with the owner of this program (Shark Attack).
* -h, --help             Shows this message.
* -N, --nocolor          Use the default color from the console. (Gray).

### Examples:
* ff2theora fish.ogg (outputs to fish.theora).
* ff2theora --output animation.theora --overwrite --crop 0:0:-20 --dump --comment dump.ogg
#### crop the video metadata such as framerate, video aspect, video quality, and keyframe:
* ff2theora --crop 0:0:-20 fish.ogg
