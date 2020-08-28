using System;

namespace NetDoc.CommandLineParser
{
    /// <summary>
    /// This is a bit weird, but similar to CommandLineParser we treat --help/--version
    /// as special kinds of errors since they result in exceptional control flow
    /// </summary>
    public enum ErrorType
    {
        BadArg,
        ShowHelp,
        ShowVersion
    }

    public class Option
    {
        public Option(string spec, Action<string?> handler)
        {
            Spec = spec;
            Handler = (string? arg, ReportError reportError) =>
            {
                try
                {
                    handler(arg);
                }
                catch (Exception e)
                {
                    reportError(
                        $"Unhandled exception handling value <{arg ?? "null"}> for {spec}: {e.Message}");
                }
            };
        }

        public Option(string spec, Action<string?, ReportError> handler)
        {
            Spec = spec;
            Handler = handler;
        }

        public delegate void ReportError(string errorMessage, ErrorType type = ErrorType.BadArg);

        public string Spec { get; }
        public Action<string?, ReportError> Handler { get; }
        public string HelpText { get; set; } = "";

        /// <summary>
        /// When set to true, the parser will treat this option as a get-opt style
        /// option terminator, consuming all remaining arguments for the handler.
        /// </summary>
        public bool IsTerminating { get; set; } = false;
    }

    public class BoolOption : Option
    {
        public BoolOption(string spec, Action<bool> handler)
            : base(spec, (str, reportError) => Handle(spec, str, handler, reportError)) { }

        private static void Handle(
            string spec,
            string? str,
            Action<bool> handler,
            ReportError reportError)
        {
            try
            {
                handler(str == null || Convert.ToBoolean(str));
            }
            catch
            {
                reportError(
                    $"Unable to convert value <{str ?? null}> to boolean for option {spec}");
            }
        }
    }

    public class EnumOption : Option
    {
        public EnumOption(string spec, Type type, Action<object> handler)
            : base(spec, str => handler(ParseEnum(spec, type, str))) { }

        private static object ParseEnum(string spec, Type type, string? str)
        {
            if (string.IsNullOrWhiteSpace(str))
            {
                throw new ArgumentNullException(str);
            }

            var parsed = Enum.TryParse(type, str, true, out var result);
            if (parsed)
            {
                return result!;
            }

            throw new Exception($"[{str}] is not recognised as a valid value for option {spec}");
        }
    }
}