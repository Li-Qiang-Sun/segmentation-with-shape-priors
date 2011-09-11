using System;

namespace Research.GraphBasedShapePrior
{
    public static class DebugConfiguration
    {
        public static VerbosityLevel VerbosityLevel { get; set; }

        public static bool SavePictures { get; set; }

        static DebugConfiguration()
        {
            VerbosityLevel = VerbosityLevel.Everything;
            SavePictures = true;
        }

        public static void WriteDebugText(string format, params object[] arg)
        {
            if (VerbosityLevel == VerbosityLevel.Everything)
                Console.WriteLine(format, arg);
        }

        public static void WriteDebugText()
        {
            WriteDebugText(String.Empty);
        }

        public static void WriteImportantDebugText(string format, params object[] arg)
        {
            if (VerbosityLevel != VerbosityLevel.None)
                Console.WriteLine(format, arg);
        }
    }
}
