using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace NetDoc.CommandLineParser
{
    public class CallbackParser
    {
        private readonly Dictionary<string, Option> _namedOptions;
        private readonly Option[] _positionalOptions;
        private readonly Dictionary<string, Option> _envVarOptions;
        private readonly string _helpText;
        private readonly string _versionInfo;
        private readonly Action<string> _writeOutput;
        private readonly Action<int> _exitProcess;

        public CallbackParser(
            Dictionary<string, Option> namedOptions,
            Option[] positionalOptions,
            Dictionary<string, Option> envVarOptions,
            string helpText,
            string versionInfo,
            Action<string> writeOutput,
            Action<int> exitProcess)
        {
            _namedOptions = namedOptions;
            _positionalOptions = positionalOptions;
            _envVarOptions = envVarOptions;
            _helpText = helpText;
            _versionInfo = versionInfo;
            _writeOutput = writeOutput;
            _exitProcess = exitProcess;
        }

        // TODO: this overload is only used by tests - should move into tests and/or inline
        public void Parse(IEnumerable<string> args, IDictionary? environment = null)
        {
            Option.ReportError errorHandler = (string msg, ErrorType errorType) =>
            {
                _writeOutput(msg);
                _exitProcess(errorType == ErrorType.BadArg ? 1 : 0);
            };

            Parse(args, environment, null, errorHandler);
        }

        internal void Parse(
            IEnumerable<string> args,
            IDictionary? environment,
            Stream? stdin,
            Option.ReportError errorHandler)
        {
            environment ??= Environment.GetEnvironmentVariables();
            stdin ??= Console.OpenStandardInput();

            foreach (var option in _envVarOptions)
            {
                var envValue = environment[option.Key];
                if (envValue != null)
                {
                    option.Value.Handler(envValue.ToString(), errorHandler);
                }
            }

            if (args.Count() == 1 && args.ElementAt(0) == "-")
            {
                var argsFromStdin = new List<string>();
                using (var stdinReader = new StreamReader(stdin))
                {
                    string? line;
                    while ((line = stdinReader.ReadLine()) != null)
                    {
                        argsFromStdin.Add(line);
                    }
                }

                args = argsFromStdin;
            }

            Option? currentOption = null;
            var position = 0;

            foreach (var arg in args)
            {
                if (_namedOptions.ContainsKey(arg))
                {
                    currentOption?.Handler(null, errorHandler);
                    currentOption = _namedOptions[arg];
                }
                else if (currentOption != null)
                {
                    currentOption.Handler(arg, errorHandler);

                    /**
                     * If the current option is an option terminator then
                     * don't unset current option, so we consume all remaining args.
                     */
                    if (!currentOption.IsTerminating)
                    {
                        currentOption = null;
                    }
                }
                else if (position < _positionalOptions.Length)
                {
                    _positionalOptions[position].Handler(arg, errorHandler);
                    position++;
                }
                else
                {
                    errorHandler($"Unexpected value {arg}");
                    errorHandler(_helpText, ErrorType.ShowHelp);
                    break;
                }
            }

            if (currentOption != null && !currentOption.IsTerminating)
            {
                currentOption.Handler(null, errorHandler);
            }
        }
    }
}