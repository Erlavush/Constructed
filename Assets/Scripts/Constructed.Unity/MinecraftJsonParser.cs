using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Constructed.Unity
{
    public static class MinecraftJsonParser
    {
        public static Dictionary<string, object> ParseObject(string json, string sourceDescription)
        {
            Parser parser = new Parser(json, sourceDescription);
            return parser.ParseRootObject();
        }

        public static bool TryGetObject(IDictionary<string, object> source, string key, out Dictionary<string, object> value)
        {
            value = null;
            if (!source.TryGetValue(key, out object rawValue) || rawValue == null)
                return false;

            value = AsObject(rawValue, key);
            return true;
        }

        public static bool TryGetArray(IDictionary<string, object> source, string key, out List<object> value)
        {
            value = null;
            if (!source.TryGetValue(key, out object rawValue) || rawValue == null)
                return false;

            value = AsArray(rawValue, key);
            return true;
        }

        public static bool TryGetString(IDictionary<string, object> source, string key, out string value)
        {
            value = null;
            if (!source.TryGetValue(key, out object rawValue) || rawValue == null)
                return false;

            value = AsString(rawValue, key);
            return true;
        }

        public static bool TryGetBoolean(IDictionary<string, object> source, string key, out bool value)
        {
            value = false;
            if (!source.TryGetValue(key, out object rawValue) || rawValue == null)
                return false;

            value = AsBoolean(rawValue, key);
            return true;
        }

        public static bool TryGetNumber(IDictionary<string, object> source, string key, out double value)
        {
            value = 0d;
            if (!source.TryGetValue(key, out object rawValue) || rawValue == null)
                return false;

            value = AsDouble(rawValue, key);
            return true;
        }

        public static Dictionary<string, object> AsObject(object value, string context)
        {
            if (value is Dictionary<string, object> objectValue)
                return objectValue;

            throw new FormatException("Expected a JSON object for " + context + ".");
        }

        public static List<object> AsArray(object value, string context)
        {
            if (value is List<object> arrayValue)
                return arrayValue;

            throw new FormatException("Expected a JSON array for " + context + ".");
        }

        public static string AsString(object value, string context)
        {
            if (value is string stringValue)
                return stringValue;

            throw new FormatException("Expected a JSON string for " + context + ".");
        }

        public static bool AsBoolean(object value, string context)
        {
            if (value is bool booleanValue)
                return booleanValue;

            throw new FormatException("Expected a JSON boolean for " + context + ".");
        }

        public static double AsDouble(object value, string context)
        {
            if (value is double doubleValue)
                return doubleValue;
            if (value is int intValue)
                return intValue;
            if (value is long longValue)
                return longValue;

            throw new FormatException("Expected a JSON number for " + context + ".");
        }

        public static float ToFloat(object value, string context)
        {
            return (float)AsDouble(value, context);
        }

        public static int ToInt(object value, string context)
        {
            return Convert.ToInt32(AsDouble(value, context), CultureInfo.InvariantCulture);
        }

        private sealed class Parser
        {
            private readonly string json;
            private readonly string sourceDescription;
            private int index;

            public Parser(string json, string sourceDescription)
            {
                if (string.IsNullOrWhiteSpace(json))
                    throw new ArgumentException("JSON content cannot be empty.", nameof(json));

                this.json = json;
                this.sourceDescription = string.IsNullOrWhiteSpace(sourceDescription) ? "JSON" : sourceDescription;
            }

            public Dictionary<string, object> ParseRootObject()
            {
                SkipWhitespace();
                Dictionary<string, object> result = AsObject(ParseValue(), sourceDescription);
                SkipWhitespace();
                if (index != json.Length)
                    throw Error("Unexpected trailing characters.");

                return result;
            }

            private object ParseValue()
            {
                SkipWhitespace();
                if (index >= json.Length)
                    throw Error("Unexpected end of JSON.");

                char current = json[index];
                switch (current)
                {
                    case '{':
                        return ParseObjectValue();
                    case '[':
                        return ParseArrayValue();
                    case '"':
                        return ParseStringValue();
                    case 't':
                        ExpectKeyword("true");
                        return true;
                    case 'f':
                        ExpectKeyword("false");
                        return false;
                    case 'n':
                        ExpectKeyword("null");
                        return null;
                    default:
                        if (current == '-' || char.IsDigit(current))
                            return ParseNumberValue();
                        throw Error("Unexpected character '" + current + "'.");
                }
            }

            private Dictionary<string, object> ParseObjectValue()
            {
                Expect('{');
                SkipWhitespace();

                Dictionary<string, object> values = new Dictionary<string, object>(StringComparer.Ordinal);
                if (TryConsume('}'))
                    return values;

                while (true)
                {
                    string key = ParseStringValue();
                    SkipWhitespace();
                    Expect(':');
                    object value = ParseValue();
                    values.Add(key, value);
                    SkipWhitespace();
                    if (TryConsume('}'))
                        return values;

                    Expect(',');
                }
            }

            private List<object> ParseArrayValue()
            {
                Expect('[');
                SkipWhitespace();

                List<object> values = new List<object>();
                if (TryConsume(']'))
                    return values;

                while (true)
                {
                    values.Add(ParseValue());
                    SkipWhitespace();
                    if (TryConsume(']'))
                        return values;

                    Expect(',');
                }
            }

            private string ParseStringValue()
            {
                Expect('"');

                StringBuilder builder = new StringBuilder();
                while (index < json.Length)
                {
                    char current = json[index++];
                    if (current == '"')
                        return builder.ToString();
                    if (current != '\\')
                    {
                        builder.Append(current);
                        continue;
                    }

                    if (index >= json.Length)
                        throw Error("Unexpected end of JSON string.");

                    char escape = json[index++];
                    switch (escape)
                    {
                        case '"':
                        case '\\':
                        case '/':
                            builder.Append(escape);
                            break;
                        case 'b':
                            builder.Append('\b');
                            break;
                        case 'f':
                            builder.Append('\f');
                            break;
                        case 'n':
                            builder.Append('\n');
                            break;
                        case 'r':
                            builder.Append('\r');
                            break;
                        case 't':
                            builder.Append('\t');
                            break;
                        case 'u':
                            builder.Append(ParseUnicodeEscape());
                            break;
                        default:
                            throw Error("Unsupported JSON escape \\" + escape + ".");
                    }
                }

                throw Error("Unterminated JSON string.");
            }

            private double ParseNumberValue()
            {
                int start = index;
                if (json[index] == '-')
                    index++;

                ConsumeDigits();
                if (index < json.Length && json[index] == '.')
                {
                    index++;
                    ConsumeDigits();
                }

                if (index < json.Length && (json[index] == 'e' || json[index] == 'E'))
                {
                    index++;
                    if (index < json.Length && (json[index] == '+' || json[index] == '-'))
                        index++;
                    ConsumeDigits();
                }

                string value = json.Substring(start, index - start);
                if (!double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double result))
                    throw Error("Invalid JSON number '" + value + "'.");

                return result;
            }

            private void ConsumeDigits()
            {
                if (index >= json.Length || !char.IsDigit(json[index]))
                    throw Error("Expected a JSON digit.");

                while (index < json.Length && char.IsDigit(json[index]))
                    index++;
            }

            private char ParseUnicodeEscape()
            {
                if (index + 4 > json.Length)
                    throw Error("Incomplete unicode escape.");

                string hex = json.Substring(index, 4);
                index += 4;
                return (char)Convert.ToInt32(hex, 16);
            }

            private void ExpectKeyword(string keyword)
            {
                for (int i = 0; i < keyword.Length; i++)
                {
                    if (index + i >= json.Length || json[index + i] != keyword[i])
                        throw Error("Expected '" + keyword + "'.");
                }

                index += keyword.Length;
            }

            private void Expect(char expected)
            {
                SkipWhitespace();
                if (index >= json.Length || json[index] != expected)
                    throw Error("Expected '" + expected + "'.");

                index++;
            }

            private bool TryConsume(char expected)
            {
                SkipWhitespace();
                if (index < json.Length && json[index] == expected)
                {
                    index++;
                    return true;
                }

                return false;
            }

            private void SkipWhitespace()
            {
                while (index < json.Length && char.IsWhiteSpace(json[index]))
                    index++;
            }

            private FormatException Error(string message)
            {
                return new FormatException(sourceDescription + " at index " + index + ": " + message);
            }
        }
    }
}
