static class Utils
{
    [DllImport("ucrtbase", ExactSpelling = true, EntryPoint = "system")]
    public static extern int ExecCmd(string cmd);
    // fixes a video size that are not divisable by 16, used in padding theora files;
    public static (int width, int height) getFixedVideoSize(int width, int height)
    {
        // we are checking not returning at once because if the inputted one of width/height fixed, it will be increased by 16.
        if (width % 16 != 0) width += 16 - width % 16;
        if (height % 16 != 0) height += 16 - height % 16;
        return (width, height);
    }
}