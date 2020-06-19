using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;

namespace rcx_parse_cli
{
    public class CustomParserAttribute : Attribute
    {
        public string Spec { get; }
        public string HelpText { get; set; } = "";

        public CustomParserAttribute(string spec) => Spec = spec;
    }

    public class OptionAttribute : CustomParserAttribute
    {
        public OptionAttribute(string spec)
            : base(spec) { }
    }

    public class OptionTerminatorAttribute : CustomParserAttribute
    {
        public OptionTerminatorAttribute(string spec)
            : base(spec) { }
    }

    public static class ObjectParser
    {
        private static CustomParserAttribute? GetOptionAttribute(PropertyInfo prop) =>
            prop.GetCustomAttribute(typeof(CustomParserAttribute)) as CustomParserAttribute;

        private static bool ConstructorMatchesProps(
            ConstructorInfo constructor,
            IEnumerable<string> propsNames)
        {
            // TODO: check param types
            var constructorParams = constructor.GetParameters()
                .Select(x => x.Name)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            return constructorParams.Count == propsNames.Count()
                && propsNames.All(prop => constructorParams.Contains(prop));
        }

        private static ConstructorInfo AssertSingle(
            ConstructorInfo[] constructors,
            Type type,
            string[] propNames)
        {
            var props = String.Join(",", propNames);
            if (constructors.Length < 1)
            {
                throw new ArgumentException(
                    $"Didn't find a constructor with parameter names matching property names [{props}] of type {type.Name}");
            }

            if (constructors.Length > 1)
            {
                throw new ArgumentException(
                    $"Found multiple ambiguous constructors with parameter naems matching property names [{props}] of type {type.Name}");
            }

            return constructors[0];
        }

        public static ParseResult<T> Parse<T>(
            string[] args,
            IDictionary? environment = null,
            Stream? stdin = null)
            where T : class
        {
            // we take the type we've been given, then use reflection to find all the
            // properties with attached [Option] attributes:
            var type = typeof(T);
            var props = type.GetProperties()
                .Select(p => (name: p.Name, attr: GetOptionAttribute(p)!))
                .Where(x => x.attr != null)
                .ToList();

            // we then look for a constructor with parameters which matches the set of properties:
            var propNames = props.Select(x => x.name).ToArray();
            var constructors = type.GetConstructors()
                .Where(c => ConstructorMatchesProps(c, propNames))
                .ToArray();
            var constructor = AssertSingle(constructors, type, propNames);

            // we match up the constructor params with the option attributes they represent
            // TODO: this duplicates the logic in ConstructorMatchesProps - is there a nice way to dedupe?
            var constructorParams = constructor.GetParameters()
                .Select(
                    param =>
                    {
                        var prop = props.Single(
                            prop => prop.name.Equals(
                                param.Name,
                                StringComparison.OrdinalIgnoreCase));
                        return (attr: prop.attr, param: param);
                    })
                .ToArray();

            // we create and run a parser based on the [Option] attributes, and store
            // the resulting values in a big bucket for now...
            var argsBucket = new Dictionary<string, object?>(
                props.Count,
                StringComparer.OrdinalIgnoreCase);
            var parserBuilder = new ParserBuilder();
            foreach (var param in constructorParams)
            {
                switch (param.attr)
                {
                    case OptionAttribute optionAttr:
                        parserBuilder.Add(
                            CreateOption(
                                param.param.ParameterType,
                                param.attr,
                                CreateHandlerForStandardOption(param.param.Name!, argsBucket)));

                        break;

                    case OptionTerminatorAttribute terminatingAttr:
                        if (!typeof(IList).IsAssignableFrom(param.param.ParameterType))
                        {
                            throw new ArgumentException(
                                $"Bad property type for parameter '{param.attr.Spec}'. "
                                + "Type OptionTerminatorAttribute only supports property types that implement IList.",
                                nameof(param.attr));
                        }

                        parserBuilder.Add(
                            CreateOption(
                                param.param.ParameterType,
                                param.attr,
                                CreateHandlerForOptionTerminator(
                                    param.param.ParameterType,
                                    param.param.Name!,
                                    argsBucket)));

                        break;

                    default:
                        throw new ArgumentException(
                            $"Unhandled attribute type {param.attr?.GetType().Name} for property {param.attr?.Spec}.",
                            nameof(param.attr));
                }
            }

            var errors = new List<(ErrorType errorType, string message)>();

            var parser = parserBuilder.Build();
            parser.Parse(args, environment, stdin, (m, t) => errors.Add((t, m)));

            // we use the nullability of the constructor params to decide whether the relevant options are required.
            // this is a bit unintuitive - why don't we use the nullability of the properties? -
            // but it means we can eg support default values implemented in the constructor
            var requiredParams = constructorParams.Where(p => !IsNullable(constructor, p.param))
                .ToArray();

            foreach (var (attr, parameterInfo) in requiredParams)
            {
                if (!argsBucket.ContainsKey(parameterInfo.Name!))
                {
                    errors.Add((ErrorType.BadArg, "Missing required argument: " + attr?.Spec));
                }
                else if (argsBucket.GetValueOrDefault(parameterInfo.Name!) == null)
                {
                    errors.Add(
                        (ErrorType.BadArg, "Missing required argument value: " + attr?.Spec));
                }
            }

            if (errors.Any())
            {
                return new ParseResult<T>(errors.ToArray());
            }

            // ...then assemble the values from the bucket into a call to the constructor we found:
            var constructorArgs = constructor.GetParameters()
                .Select(p => argsBucket.GetValueOrDefault(p.Name!))
                .ToArray();

            // finally we can invoke the object's constructor to create the result.
            return new ParseResult<T>((T)constructor.Invoke(constructorArgs));
        }

        private static Option CreateOption(
            Type parameterType,
            CustomParserAttribute attr,
            Action<object?> handler)
        {
            parameterType = Nullable.GetUnderlyingType(parameterType) ?? parameterType;
            if (parameterType == typeof(string))
            {
                return new Option(attr.Spec, handler) { HelpText = attr.HelpText };
            }

            if (parameterType == typeof(bool))
            {
                return new BoolOption(attr.Spec, x => handler(x)) { HelpText = attr.HelpText };
            }

            if (parameterType.IsEnum)
            {
                return new EnumOption(attr.Spec, parameterType, x => handler(x))
                {
                    HelpText = attr.HelpText
                };
            }

            if (typeof(IList).IsAssignableFrom(parameterType))
            {
                return new Option(attr.Spec, handler)
                {
                    HelpText = attr.HelpText,
                    IsTerminating = true
                };
            }

            throw new ArgumentException(
                $"Unhandled parameter type {parameterType} in parameter {attr.Spec}",
                nameof(parameterType));
        }

        private static Action<object?> CreateHandlerForOptionTerminator(
            Type parameterType,
            string parameterName,
            Dictionary<string, object?> argumentValues)
        {
            return (x) =>
            {
                if (argumentValues.GetValueOrDefault(parameterName) == null)
                {
                    argumentValues[parameterName] = Activator.CreateInstance(parameterType);
                }

                    ((IList?)argumentValues[parameterName])?.Add(x);
            };
        }

        private static Action<object?> CreateHandlerForStandardOption(
            string parameterName,
            Dictionary<string, object?> argumentValues)
        {
            return x => argumentValues[parameterName] = x;
        }

        /// <summary>Based on https://stackoverflow.com/a/58454489 with simplifications/comments added</summary>
        private static bool IsNullable(ConstructorInfo constructor, ParameterInfo parameter)
        {
            // first we check for a nullable value type. This is relatively straightforward:
            if (parameter.ParameterType.IsGenericType
                && parameter.ParameterType.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                return true;
            }

            // The rest of this method checks for nullable reference types.
            // see https://github.com/dotnet/roslyn/blob/master/docs/features/nullable-metadata.md for more information

            // the compiler embeds nullability information into an internal NullableAttribute value
            var nullableAttribute = parameter.CustomAttributes.FirstOrDefault(
                x => x.AttributeType.FullName
                    == "System.Runtime.CompilerServices.NullableAttribute");
            if (IsNullable(nullableAttribute))
            {
                return true;
            }

            // As a size optimisation, the compiler may combine the most common nullability value into a
            // NullableContextAttribute on the constructor or type instead, so we try to fall back to that.
            var contextAttribute = constructor.CustomAttributes.FirstOrDefault(
                x => x.AttributeType.FullName
                    == "System.Runtime.CompilerServices.NullableContextAttribute");
            if (IsNullable(contextAttribute))
            {
                return true;
            }

            contextAttribute = constructor.DeclaringType!.CustomAttributes.FirstOrDefault(
                x => x.AttributeType.FullName
                    == "System.Runtime.CompilerServices.NullableContextAttribute");
            if (IsNullable(contextAttribute))
            {
                return true;
            }

            // Couldn't find a suitable attribute.
            // We assume not-nullable as the default state, since making a parameter required
            // is more likely to break in a safe way.
            return false;
        }

        private static bool IsNullable(CustomAttributeData? attribute)
        {
            // We look at the attribute's constructor arguments to find the value.
            // Known byte values are 0=oblivious;1=non-nullable;2=nullable
            if (attribute == null || attribute.ConstructorArguments.Count != 1)
            {
                return false;
            }

            // NullableAttribute has two constructors: (byte) and (byte[])
            var attributeArgument = attribute.ConstructorArguments[0];
            if (attributeArgument.ArgumentType == typeof(byte[]))
            {
                // the (byte[]) constructor is used for generic types with a series of arguments.
                // we only care about the outermost nullability value here.
                var args =
                    attributeArgument.Value as ReadOnlyCollection<CustomAttributeTypedArgument>;
                if (args != null && args.Count > 0 && args[0].ArgumentType == typeof(byte))
                {
                    return (byte)args[0].Value! == 2;
                }
            }
            else if (attributeArgument.ArgumentType == typeof(byte))
            {
                // the (byte) constructor is used in non-generic cases
                return (byte)attributeArgument.Value! == 2;
            }

            return false;
        }
    }

    public class ParseResult<T>
        where T : class
    {
        internal readonly T? _result;

        internal readonly (ErrorType errorType, string message)[] _errors;

        public ParseResult(T result)
        {
            _result = result;
            _errors = new (ErrorType, string)[0];
        }

        public ParseResult((ErrorType errorType, string message)[] errors)
        {
            if (!errors.Any())
            {
                throw new ArgumentException(
                    "Errors constructor used but no errors passed",
                    nameof(errors));
            }

            _errors = errors;
            _result = null;
        }

        public T GetResultOrHandleErrors(
            Action<string>? writeOutput = null,
            Action<int>? exitProcess = null)
        {
            // TODO: We'd really like to not call Environment.Exit here (it tends to muck up
            // test processes and isn't generally composable) but the alternative seems to be
            // something like a ShowHelpException that the client has to handle and figure out
            // the exit code from.
            writeOutput ??= Console.WriteLine;
            exitProcess ??= Environment.Exit;

            if (!_errors.Any())
            {
                // because of the two constructors we have we assume _result must be set
                // if there are no errors
                return _result!;
            }

            var isRealError = !_errors.All(
                x => x.errorType == ErrorType.ShowHelp || x.errorType == ErrorType.ShowVersion);

            foreach (var error in _errors)
            {
                writeOutput(error.message);
            }

            exitProcess(isRealError ? 1 : 0);

            // this line should only be hit by tests where `exitProcess` doesn't actually exit
            return null!;
        }
    }
}