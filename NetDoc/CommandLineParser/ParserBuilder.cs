using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace NetDoc.CommandLineParser
{
    public class ParserBuilder
    {
        private readonly List<Option> _options = new List<Option>();
        private string _assemblyName = Assembly.GetEntryAssembly()!.GetName().Name!;

        private string _assemblyVersion =
            Assembly.GetEntryAssembly()!.GetName().Version!.ToString();

        private Action<string> _writeOutput = Console.Error.WriteLine;
        private Action<int> _exitProcess = Environment.Exit;

        public ParserBuilder Add(Option option)
        {
            _options.Add(option);
            return this;
        }

        public ParserBuilder WithNameAndVersion(string name, string version)
        {
            _assemblyName = name;
            _assemblyVersion = version;
            return this;
        }

        public ParserBuilder SetUi(Action<string> writeOutput, Action<int> exitProcess)
        {
            _writeOutput = writeOutput;
            _exitProcess = exitProcess;
            return this;
        }

        public CallbackParser Build()
        {
            var positionalOptions = new Dictionary<int, Option>();
            var namedOptions = new Dictionary<string, Option>();
            var envVarOptions = new Dictionary<string, Option>();

            var versionInfo = $"{_assemblyName} {_assemblyVersion}";
            var options = _options.ToList();

            options.Add(
                new Option(
                    "--help",
                    (_, reportError) => reportError(
                        CalcHelpText(options, _assemblyName, _assemblyVersion),
                        ErrorType.ShowHelp))
                { HelpText = "Display this help text and quit." });
            options.Add(
                new Option(
                    "--version",
                    (_, reportError) => reportError(versionInfo, ErrorType.ShowVersion))
                {
                    HelpText = "Display version information and quit."
                });

            foreach (var option in options)
            {
                var spec = option.Spec.Split('|');
                foreach (var alternative in spec)
                {
                    if (int.TryParse(alternative, out var position))
                    {
                        positionalOptions[position] = option;
                    }
                    else if (Regex.Match(alternative, "^[A-Z0-9_]+$").Success)
                    {
                        envVarOptions[alternative] = option;
                    }
                    else
                    {
                        namedOptions[alternative] = option;
                    }
                }
            }

            var helpText = CalcHelpText(options, _assemblyName, _assemblyVersion);

            var sortedPositionalOptions =
                positionalOptions.OrderBy(x => x.Key).Select(x => x.Value).ToArray();

            return new CallbackParser(
                namedOptions,
                sortedPositionalOptions,
                envVarOptions,
                helpText.ToString(),
                versionInfo,
                _writeOutput,
                _exitProcess);
        }

        private static string CalcHelpText(
            IEnumerable<Option> options,
            string assemblyName,
            string assemblyVersion)
        {
            var helpText = new StringBuilder();

            helpText.Append($"{assemblyName} {assemblyVersion}\n");

            var argLines = options
                .Select(option => (option, text: string.Join(", ", option.Spec.Split('|'))))
                .ToList();
            var longestArg = argLines.Max(x => x.text.Length);
            var argLinesWithHelp = argLines
                .Select(x => $"  {x.text.PadRight(longestArg)}    {x.option.HelpText}\n")
                .ToList();

            foreach (var line in argLinesWithHelp)
            {
                helpText.Append(line);
            }

            return helpText.ToString();
        }
    }
}