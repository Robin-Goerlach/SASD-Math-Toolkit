using System.Text;
using Sasd.Math.Toolkit.Sample;

var outputPath = args.Length switch
{
    0 => Path.Combine(Environment.CurrentDirectory, "sasd-math-toolkit-demo.html"),
    1 => Path.GetFullPath(args[0]),
    _ => throw new ArgumentException("Pass at most one optional output path for the generated HTML report.")
};

var directory = Path.GetDirectoryName(outputPath);
if (!string.IsNullOrWhiteSpace(directory))
{
    Directory.CreateDirectory(directory);
}

var html = NumericalDemoReport.Create();
File.WriteAllText(outputPath, html, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

Console.WriteLine("SASD Math Toolkit numerical demo report generated successfully.");
Console.WriteLine($"Open this file in a browser: {outputPath}");
