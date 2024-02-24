using static CRC;
using static OggHeaderType;
using static OggPacketInfo;
const string appName = "FF2Theora", discordLink = "https://discord.com/invite/Rq8uHXhpZf", creator = "Shark Attack";
string Logo = $@"FF2Theora Created by {creator} Copyright {DateTime.Now.Year}

The suitable tool for the game called ""Feeding Frenzy 2"" for modding .theora files;";
long granule = 0;
int i = 0, mode = -1, code = 0, packetSize = 0, pageSize = 0, version = 19, headertype = 0, segmentLength, dataLen, packetPosition = 0, dataLeftSize = 0, serialNumber = 0;
BinaryWriter? fsw = null;
BinaryReader? fsr = null;
string[] modes =
{
    "decode"
};
// the ogg page we will store because we will compute it;
byte[] header = new byte[27];
List<byte> segmentTable = [];
// the list that will crop data;
List<(int packNumber, int start, int length)> crop = [];
List<int> packetPositions = [];
string? output = null;
// used for generating CRC and checking.
uint crc_reg = 0, crc_check = 0;
int width = -1, height = -1;
int streams = 0;
int serialProcess = -1;
int size = 255;
bool remaining = false, exclude = false;
bool nologo = false, overwrite = false, nocolor = false, old = false, hidePacketGranule = false, hidePacketNumber = false, hidePacketSerialNumber = false, raw = false, Unsafe = false;
// we will use it later.
byte[] buffer;
const ConsoleColor W = ConsoleColor.Yellow, E = ConsoleColor.Red;
void err(string msg)
{
    write(msg, E);
    Environment.Exit(1);
}
void write(string msg, ConsoleColor c = ConsoleColor.Gray)
{
    var prev = Console.ForegroundColor;
    if (!nocolor) Console.ForegroundColor = c;
    Console.WriteLine(msg);
    Console.ForegroundColor = prev;
}
T Parse<T>(string? str = null) where T : IParsable<T>
{
    if (!T.TryParse(str ?? args[i]!, null, out T output)) err("Failed to parse the value.");
    return output!;
}
void Usage()
{
    write(string.Format(@"{0} v0.{2}. Made by {4} 
Usage: {1}{0} [options] [input]

File output options:

{1}-o, --output           Alternative output.
{1}-m, --mode [string]    Switches the operation. Available is {5}.
{1}-y, --overwrite        Overwrites the exist output file rather asking to overwrite it.
{1}-c, --crop <n>:<n>:<n> Crops a range from data. The format should be like this: <packetNumberIndex>:<start>:<length>.
{1}-O, --old              Use the old method for processing ogg theora files. (not recommended).
{1}-r, --raw              Do not change anything to the packets. this will prevent the program to fix the video size(that includes padded green/black bar) during decode option (not recommended either).
{1}-u, --unsafe           Allows you to change the actual size (to be allocated) when using --width|--height command.
{1}-S, --size <n> [BETA]  Controls the limition size of packets during decoding. the more smaller = more bigger & more overhead. the value is multipled by 255 and it's maxvalue is 255 which is 255^2=65025. the defaultvalue is 255. the command is still in progress, due to the granule reason.
{1}-C, --comment          always exclude exist comments, except vendor. Rather asking to exclude it (if found).
{1}-P, --serial <n>       Process a specified serial number. This is useful when processing a multiple streams. They can be viewed using oggz-info.

Theora output options:
{1}-W, -X, --width <n>    Changes the video size width in binary data. Useful for several stuff.
{1}-H, -Y, --height <n>   Same as previous but height.

Dump output options Like (oggzdump):

{1}-g, --granule          Hide the packet granule position timestamp;
{1}-p, --packetno         Hide the packet sequence number;
{1}-s, --serialno         Hide the packet serial number;
{1}-d, --dump             Dump the theora file using all of these options;

Miscellaneous:

{1}-n, --nologo           Suppress the copyright logo.
{1}-v, --visit            Opens a new browser containing the URL of youtube channel who created this program.
{1}-h, --help             Shows this message.
{1}-N, --nocolor          Use the default color from the console. (Gray).

Examples:

{1}{0} fish.ogg (writes to fish.theora).
{1}{0} --output animation.theora animation.ogg (writes to animation.theora).
{1}{0} --output animation.theora --overwrite --crop 0:0:-20 --dump --comment dump.ogg

Crop the video metadata such as framerate, video aspect, video quality, and keyframe:

{1}{0} --crop 0:0:-20 fish.ogg

To decode an payload theora:

{1}{0} --mode decode --output theora.ogv animation.theora


If you encountered a bug in this program, please report us in {3} and show the screen shot of what you encountered. Oh I forgot by the way:

Please credit my tool when you use it. Otherwise, xiph.Org logo will be sad :(", appName, '\t', version, discordLink, creator, string.Join(", ", modes)), ConsoleColor.Cyan);
    Environment.Exit(1);
}

void Ensure(int max = 1)
{
    if (args.Length - i < max) err(string.Format("Excepted {0} argument(s).", max - (args.Length - i)));
    i += max;
}
for (; i < args.Length; i++)
{
    if (!args[i].StartsWith('-'))
    {
        if (!nologo)
            write(Logo);
        var file = args[i];
        if (!File.Exists(file)) err("Input file is not exist.");
        output ??= Path.GetFileNameWithoutExtension(file) + "." + (mode switch
        {
            0 => "ogv",
            _ => "theora"
        });
        if (string.Equals(file, output, StringComparison.OrdinalIgnoreCase)) err("Nah that's not possible.");
        if (File.Exists(output) && !overwrite && Utils.ExecCmd("choice /c yn /n /cs /m \"The output file is exist would you overwrite it? <Y/N>\"") == 2) err("Not overwriting. Exiting.");
#if DEBUG
        write("DEBUG MODE is enabled.", ConsoleColor.Cyan);
#endif
        write(string.Format("Currently mode: {0}", mode == -1 ? "convert" : modes[mode]), ConsoleColor.Cyan);

        if (args.Length - 1 > i) write("There's more commands at the end of input file, they will be ignored;", W);
        try
        {
            fsr = new(new FileStream(file, FileMode.Open));
            fsw = new(new FileStream(output, FileMode.Create));
            long len = fsr.BaseStream.Length;
            // switch modes;
            switch (mode)
            {
                case 0:


                    while (fsr.BaseStream.Position < len - 27)
                    {
                        packetPositions.Add((int)fsr.BaseStream.Position);
                        dataLen = fsr.ReadInt32();
                        remaining = false;
                        while (dataLen > 0)
                        {
                            BitConverter.GetBytes(PACKET_PATTERN).CopyTo(header, 0);
                            // write header type;
                            if (!remaining)
                                headertype = old ? (int)fsr.ReadInt64() : Array.FindIndex(fsr.ReadBytes(8), x => x > 0);
                            header[5] = (byte)(!remaining ? old ? headertype > 0 ? headertype + 1 : headertype : headertype != -1 ? headertype : 0 : CONTINUED);
                            // write granule;
                            BitConverter.GetBytes(!remaining ? fsr.ReadInt64() : 1).CopyTo(header, 6);
                            if (!remaining)
                                fsr.BaseStream.Seek(4, SeekOrigin.Current);
                            if (!remaining) serialNumber = fsr.ReadInt32();
                            BitConverter.GetBytes(serialNumber).CopyTo(header, 14);
                            // write packet number;
                            BitConverter.GetBytes(packetSize).CopyTo(header, 18);
                            buffer = fsr.ReadBytes(Math.Min(dataLen, size * 255));
                            segmentLength = buffer.Length / byte.MaxValue;
                            // check if the requested size are multiple of 255 so we don't include the lacing (EOS) value. because Ogg stream detects a data are multiple of 255 as a continued page, while we check it by the header flag.
                            header[^1] = (byte)(segmentLength + Math.Min(buffer.Length % 255, 1));
                            segmentTable.AddRange(Enumerable.Repeat(byte.MaxValue, header[^1]));
                            if (buffer.Length % 255 != 0)
                                segmentTable[^1] = (byte)(buffer.Length % byte.MaxValue);

                            if (!raw)
                            {
                                // check the packet processed, if 0 then it is the header codec packet.
                                if (packetSize == 0)
                                {
                                    // copy the size to be allocated into the video board.
                                    Array.Copy(BitConverter.GetBytes(BinaryPrimitives.ReverseEndianness(BinaryPrimitives.ReadInt16BigEndian(buffer.AsSpan()[10..]) * 16)), 1, buffer, 14, 3);
                                    Array.Copy(BitConverter.GetBytes(BinaryPrimitives.ReverseEndianness(BinaryPrimitives.ReadInt16BigEndian(buffer.AsSpan()[12..]) * 16)), 1, buffer, 17, 3);

                                }
                            }

                            // copied from library ogg source code;
                            // compute the header (including segment table).
                            for (int j = 0; j < header.Length; j++)
                                crc_reg = (crc_reg << 8) ^ Table[((crc_reg >> 24) & byte.MaxValue) ^ header[j]];

                            foreach (var b in segmentTable) crc_reg = (crc_reg << 8) ^ Table[((crc_reg >> 24) & byte.MaxValue) ^ b];
                            // compute the raw data;
                            for (int j = 0; j < buffer.Length; j++)
                                crc_reg = (crc_reg << 8) ^ Table[((crc_reg >> 24) & byte.MaxValue) ^ buffer[j]];
#if DEBUG
                            write(string.Format("CRC Registered: {0}", crc_reg));
#endif
                            // copy the generated crc to the header;
                            BitConverter.GetBytes(crc_reg).CopyTo(header, 22);
                            crc_reg = 0;
                            fsw.Write(header);
                            fsw.Write(segmentTable.ToArray());
                            fsw.Write(buffer);
                            packetSize++;
                            dataLen -= buffer.Length;
                            remaining = true;
                            segmentTable.Clear();
                            Array.Clear(header);
                        }
                    }
                    write(string.Format("Found {0} payload packet(s) Each is located at: {1}.", packetSize, string.Join(", ", packetPositions.ToArray())), ConsoleColor.Magenta);
                    break;
                default:
                    // convert it to array once so we don't reconvert it in a loop this will increase the performance.
                    var cropArr = crop.ToArray();
                    // now let's mess with packets!
                    // we don't actually detect the end of page by it's header type, because dumped theora files doesn't have that.
                    while (fsr.Read(header, 0, 27) > 0)
                    {
                        segmentTable.Clear();
                        crc_reg = 0;
                        // we will do a complex check by checking checksum.
                        //read the checksum
                        crc_check = BitConverter.ToUInt32(header, 22);
                        //clear it for computation;
                        Array.Clear(header, 22, 4);
                        // segment table;
                        // the current version is supportable for one frame/data in one packet.
                        segmentLength = header[^1];
                        segmentTable.AddRange(fsr.ReadBytes(header[^1]));

                        // segment table;
                        dataLen = (byte.MaxValue * --segmentLength) + segmentTable[^1];
                        // read the raw data;
                        buffer = fsr.ReadBytes(dataLen);


                        // header
                        for (int j = 0; j < header.Length; j++)
                            crc_reg = (crc_reg << 8) ^ Table[((crc_reg >> 24) & byte.MaxValue) ^ header[j]];
                        foreach (var b in segmentTable) crc_reg = (crc_reg << 8) ^ Table[((crc_reg >> 24) & byte.MaxValue) ^ b];
                        // body/data
                        for (int j = 0; j < buffer.Length; j++) crc_reg = (crc_reg << 8) ^ Table[((crc_reg >> 24) & byte.MaxValue) ^ buffer[j]];
                        // then we need to know that it is corrupted.
                        if (crc_check != crc_reg) throw new((pageSize > 0) ? "CRC mismatch!" : "Unable to recognize this format.");
                        //verify capture pattern;
                        if (BitConverter.ToInt32(header) != PACKET_PATTERN) throw new("Failed to verify capture pattern.");
                        if (header[4] != STRUCT_STREAM_VERSION) throw new(string.Format("Isn't structure stream version supposed to be {0}?", STRUCT_STREAM_VERSION));
                        headertype = header[5];
                        granule = BitConverter.ToInt64(header, 6);
                        serialNumber = BitConverter.ToInt32(header, 14);
                        if (serialProcess != -1) if (serialNumber != serialProcess) continue;
                        if ((headertype & STARTOFBITSTREAM) != 0) streams++;

                        if (hidePacketGranule) granule = 0;
                        if (hidePacketSerialNumber) serialNumber = 0;
                        // first packet typically contains the codec header. we are excepting theora.
                        if (packetSize == 0)
                        {
                            var fixedSize = Utils.getFixedVideoSize(width, height);
                            if (dataLen < 20) throw new("theora header codec size must be atleast 20 bytes for holding width and height");
                            if (width != -1)
                            {
                                Array.Copy(BitConverter.GetBytes(BinaryPrimitives.ReverseEndianness(width)), 1, buffer, 14, 3);
                                if (Unsafe)
                                    Array.Copy(BitConverter.GetBytes(BinaryPrimitives.ReverseEndianness((ushort)(fixedSize.width / 16))), 0, buffer, 10, 2);
                            }
                            if (height != -1)
                            {
                                Array.Copy(BitConverter.GetBytes(BinaryPrimitives.ReverseEndianness(height)), 1, buffer, 17, 3);
                                if (Unsafe)
                                    Array.Copy(BitConverter.GetBytes(BinaryPrimitives.ReverseEndianness((ushort)(fixedSize.height / 16))), 0, buffer, 12, 2);
                            }
                        }
                        if (packetSize == 0)
                        {
                            write("Theora video codec info: \n");
                            byte[] video = new byte[4];
                            Array.Copy(buffer, 14, video, 1, 3);
                            (int width, int height) videoActual = default;
                            write($"\tWidth: {width = BinaryPrimitives.ReadInt32BigEndian(video)}, Actual: {videoActual.width = (BinaryPrimitives.ReadUInt16BigEndian(buffer.AsSpan()[10..]) * 16)}", ConsoleColor.Cyan);
                            Array.Copy(buffer, 17, video, 1, 3);
                            write($"\tHeight: {height = BinaryPrimitives.ReadInt32BigEndian(video)}, Actual: {videoActual.height = (BinaryPrimitives.ReadUInt16BigEndian(buffer.AsSpan()[12..]) * 16)}", ConsoleColor.Cyan);
                            if (!raw)
                            {
                                var fixedSize = Utils.getFixedVideoSize(width, height);
                                if (width > videoActual.width)
                                {
                                    write("Video size width is bigger than it's actual size. Adjusting to it's based size...", E);
                                    Array.Copy(BitConverter.GetBytes((ushort)BinaryPrimitives.ReverseEndianness(fixedSize.width / 16)), 0, buffer, 10, 2);
                                }
                                if (height > videoActual.height)
                                {
                                    write("Video size height is bigger than it's actual size. Adjusting to it's based size...", E);
                                    Array.Copy(BitConverter.GetBytes((ushort)BinaryPrimitives.ReverseEndianness(fixedSize.height / 16)), 0, buffer, 12, 2);

                                }
                            }

                        }
                        if (packetSize == 1)
                        {
                            Console.WriteLine("\nCOMMENTS INFO:");
                            int pos = 7;
                            int commentsTotal = 0;
                            int commentLength = BitConverter.ToInt32(buffer, pos);
                            pos += 4;
                            write(string.Format("\tVendor: {0}", Encoding.UTF8.GetString(buffer, pos, commentLength)), ConsoleColor.DarkMagenta);
                            pos += commentLength;
                            int commentsLength = BitConverter.ToInt32(buffer, pos);
                            pos += 4;
                            write(string.Format("Found {0} comment(s).", commentsLength));
                            for (int c = 0; c < commentsLength; c++)
                            {
                                commentLength = BitConverter.ToInt32(buffer, pos);
                                if (commentLength - pos > buffer.Length) throw new("comment length is larger than the data size.");
                                pos += 4;
                                commentsTotal += 4;
                                write('\t' + Encoding.UTF8.GetString(buffer, pos, commentLength), ConsoleColor.Cyan);
                                pos += commentLength;
                                commentsTotal += commentLength;
                            }
                            if (commentsLength > 0)
                                if (exclude || Utils.ExecCmd("choice /c yn /n /cs /m \"Do you want to exclude this comments? <Y/N>\"") == 1)
                                {
                                    // set comments length to 0;
                                    Array.Clear(buffer, buffer.Length - commentsTotal - 4, 4);
                                    buffer = buffer[0..(buffer.Length - commentsTotal)];
                                }
                        }
                        // first theora frame contains the video quality;
                        if (packetSize == 3) write(string.Format("\tTheora video quality (inaccurate): {0}", double.Round(buffer[0] / 6.3, 1)), ConsoleColor.Cyan);
                        if (cropArr.Length > 0)
                        {
                            int index = Array.FindIndex(cropArr, x => x.packNumber == pageSize);
                            if (index != -1)
                            {
                                // ensure if cropping amount is less than the selected packet size so we don't cause a real exception here lol                    
                                if (cropArr[index].start + cropArr[index].length > dataLen) throw new("cropping amount is larger than the selected packet size.");
                                buffer = buffer[(cropArr[index].start < 0 ? dataLen - (-cropArr[index].start) : cropArr[index].start)..(cropArr[index].start >= 0 ? cropArr[index].length < 0 ? Math.Abs(cropArr[index].length) + cropArr[index].start : dataLen - cropArr[index].length : dataLen)];
                            }


                        }
                        pageSize++;
                        // we are turning continued pages into fresh packets.
                        if ((headertype & CONTINUED) != 0)
                        {
                            fsw.Seek(packetPosition, SeekOrigin.Begin);
                            dataLeftSize += buffer.Length;
                            fsw.Write(dataLeftSize);
                            fsw.Seek(0, SeekOrigin.End);
                        }
                        else
                        {
                            packetSize++;
                            packetPosition = (int)fsw.BaseStream.Position;
                            dataLeftSize = buffer.Length;
                            // writing data length where is the most part i was figuring how it works lol.
                            fsw.Write(buffer.Length);
                            fsw.Write(headertype > 0 ? old ? headertype - 1 : (long)Math.Pow(256, headertype) : 0);
                            // write granule position;
                            fsw.Write(granule);
                            // write packet number;
                            fsw.Write(!hidePacketNumber ? packetSize - 1 : 0);
                            // write serial number;
                            fsw.Write(serialNumber);
                        }

                        // write the raw data then. heh.
                        fsw.Write(buffer);
                    }
                    if (streams > 1)
                        write("Found more streams! this is gonna crash the game. Have you put vorbis?", E);
                    if (streams == 0)
                        write("Believe me, using the theora gonna crash the game. Use the -O command when decode the theora and convert it again.", E);
                    write(string.Format("Found {0} packet(s) in {1} page(s). {2} frames right?", packetSize, pageSize, packetSize - 3), ConsoleColor.Magenta);

                    if (pageSize > packetSize) write("Found continued pages, removing it...");
                    if (pageSize > packetSize) write(string.Format("Removed {0} continued page(s). decreased size like {1} bytes.", pageSize - packetSize, (pageSize - packetSize) * 27), W);

                    break;
            }
        }
        catch (Exception ex)
        {
            // if debug is true, then show the line of src code that throw this exception.
            write(
#if DEBUG
                string.Format("Exception: {0}\nLine: {1}", ex.Message, ex.StackTrace!.Substring(ex.StackTrace.LastIndexOf(' ') + 1))
#else
                ex.Message
#endif
, E);
            code = 1;
        }
        finally
        {
            fsw?.Dispose();
            fsr?.Dispose();
            Environment.Exit(code);
        }
    }
    switch (args[i].TrimStart('-'))
    {
        case "output" or "o":
            Ensure();
            output = args[i];
            break;
        case "nologo" or "n":
            nologo = true;
            break;
        case "help" or "h":
            Usage();
            break;
        case "granule" or "g":
            hidePacketGranule = !hidePacketGranule;
            break;
        case "packetno" or "p":
            hidePacketNumber = !hidePacketNumber;
            break;
        case "serialno" or "s":
            hidePacketSerialNumber = !hidePacketSerialNumber;
            break;
        case "dump" or "d":
            hidePacketSerialNumber = !hidePacketSerialNumber;
            hidePacketGranule = !hidePacketGranule;
            hidePacketNumber = !hidePacketNumber;
            break;
        case "mode" or "m":
            Ensure();
            mode = Array.IndexOf(modes, args[i].ToLower());
            if (mode == -1) err("Invalid mode option.");
            break;
        case "raw" or "r":
            raw = true;
            break;
        case "overwrite" or "y":
            overwrite = true;
            break;
        case "nocolor" or "N":
            nocolor = true;
            break;
        case "old" or "O":
            old = true;
            break;
        case "comment" or "C":
            exclude = true;
            break;
        case "size" or "S":
            Ensure();
            size = Math.Clamp(Parse<int>(), 1, 255);
            break;
        case "serial" or "P":
            Ensure();
            serialProcess = Math.Max(Parse<int>(), 0);
            break;
        case "crop" or "c":
            Ensure();
            var arr = args[i].Split(':');
            if (arr.Length < 3) err("can't parse the crop because it is less than the required length.");
            crop.Add((Math.Abs(Parse<int>(arr[0])), Parse<int>(arr[1]), Parse<int>(arr[2])));
            break;
        case "width" or "W" or "X":
            Ensure();
            // max video size for theora codec. Which is 65535*16
            width = Math.Min(Math.Abs(Parse<int>()), 1048560);
            break;
        case "unsafe" or "u":
            Unsafe = true;
            break;
        case "height" or "H" or "Y":
            Ensure();
            height = Math.Min(Math.Abs(Parse<int>()), 1048560);
            break;
        case "visit" or "v":
            Utils.ExecCmd("start http://bit.ly/45MzY9b");
            Environment.Exit(1);
            break;
        default:
            err("Unrecognized command, type -help for usage.");
            break;
    }
}
err("No input file specified. Type -help for usage.");