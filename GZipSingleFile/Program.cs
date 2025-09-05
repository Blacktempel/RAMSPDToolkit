namespace GZipSingleFile;

static class Program
{
    static void Main(string[] args)
    {
        if (args.Length != 1)
        {
            Console.WriteLine("Argument must be one file to be gzipped.");
            DoExit();
            return;
        }

        var file = args[0];

        if (!File.Exists(file))
        {
            Console.WriteLine($"Provided file does not exist '{file}'");
            DoExit();
        }

        Console.WriteLine($"Zipping file '{file}'");

        GZipper.CompressFile(file, file + ".gz", false);

        DoExit();
    }

    static void DoExit()
    {
        Console.WriteLine("Press enter to exit...");
        Console.ReadLine();
    }
}
