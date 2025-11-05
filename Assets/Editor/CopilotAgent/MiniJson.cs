// Minimal JSON parser for Unity (based on Unity's MiniJSON, MIT License)
// Supports Deserialize(string) -> object (Dictionary<string,object>, List<object>, string, double, long, bool, null)
// and Serialize(object) -> string (not used here but kept for completeness)
// Source adapted to be self-contained in editor scripts.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace CopilotAgent
{
    public static class MiniJson
    {
        public static object Deserialize(string json)
        {
            if (json == null) return null;
            // Strip UTF-8 BOM and other leading non-printable chars
            json = json.TrimStart('\uFEFF', '\u200B', '\u200E', '\u200F');
            return Parser.Parse(json);
        }

        public static string Serialize(object obj)
        {
            return Serializer.Serialize(obj);
        }

        private sealed class Parser : IDisposable
        {
            private readonly string json;
            private int index;

            private Parser(string json)
            {
                this.json = json;
                index = 0;
            }

            public static object Parse(string json)
            {
                using (var instance = new Parser(json))
                {
                    return instance.ParseValue();
                }
            }

            public void Dispose()
            {
            }

            private enum TOKEN
            {
                NONE,
                CURLY_OPEN,
                CURLY_CLOSE,
                SQUARED_OPEN,
                SQUARED_CLOSE,
                COLON,
                COMMA,
                STRING,
                NUMBER,
                TRUE,
                FALSE,
                NULL
            }

            private Dictionary<string, object> ParseObject()
            {
                var table = new Dictionary<string, object>();

                // {
                NextToken();

                // }
                while (true)
                {
                    var token = LookAhead();
                    if (token == TOKEN.NONE)
                        return null;
                    if (token == TOKEN.CURLY_CLOSE)
                    {
                        NextToken();
                        return table;
                    }

                    // key
                    string name = ParseString();
                    if (name == null)
                        return null;

                    // :
                    if (NextToken() != TOKEN.COLON)
                        return null;

                    // value
                    object value = ParseValue();

                    table[name] = value;

                    switch (NextToken())
                    {
                        case TOKEN.COMMA:
                            continue;
                        case TOKEN.CURLY_CLOSE:
                            return table;
                        default:
                            return null;
                    }
                }
            }

            private List<object> ParseArray()
            {
                var array = new List<object>();

                // [
                NextToken();

                // ]
                bool parsing = true;
                while (parsing)
                {
                    TOKEN nextToken = LookAhead();
                    if (nextToken == TOKEN.NONE)
                        return null;
                    if (nextToken == TOKEN.SQUARED_CLOSE)
                    {
                        NextToken();
                        break;
                    }

                    object value = ParseValue();
                    array.Add(value);

                    switch (NextToken())
                    {
                        case TOKEN.COMMA:
                            break;
                        case TOKEN.SQUARED_CLOSE:
                            parsing = false;
                            break;
                        default:
                            return null;
                    }
                }

                return array;
            }

            private object ParseValue()
            {
                switch (LookAhead())
                {
                    case TOKEN.STRING:
                        return ParseString();
                    case TOKEN.NUMBER:
                        return ParseNumber();
                    case TOKEN.CURLY_OPEN:
                        return ParseObject();
                    case TOKEN.SQUARED_OPEN:
                        return ParseArray();
                    case TOKEN.TRUE:
                        NextToken();
                        return true;
                    case TOKEN.FALSE:
                        NextToken();
                        return false;
                    case TOKEN.NULL:
                        NextToken();
                        return null;
                    default:
                        return null;
                }
            }

            private string ParseString()
            {
                var s = new StringBuilder();
                char c;

                // "
                NextChar();

                bool parsing = true;
                while (parsing)
                {
                    if (index == json.Length)
                        break;

                    c = NextChar();
                    switch (c)
                    {
                        case '"':
                            parsing = false;
                            break;
                        case '\\':
                            if (index == json.Length)
                                parsing = false;
                            else
                            {
                                c = NextChar();
                                switch (c)
                                {
                                    case '"':
                                    case '\\':
                                    case '/':
                                        s.Append(c);
                                        break;
                                    case 'b':
                                        s.Append('\b');
                                        break;
                                    case 'f':
                                        s.Append('\f');
                                        break;
                                    case 'n':
                                        s.Append('\n');
                                        break;
                                    case 'r':
                                        s.Append('\r');
                                        break;
                                    case 't':
                                        s.Append('\t');
                                        break;
                                    case 'u':
                                        var hex = new char[4];
                                        for (int i = 0; i < 4; i++)
                                            hex[i] = NextChar();
                                        s.Append((char)Convert.ToInt32(new string(hex), 16));
                                        break;
                                }
                            }
                            break;
                        default:
                            s.Append(c);
                            break;
                    }
                }

                return s.ToString();
            }

            private object ParseNumber()
            {
                EatWhitespace();
                int lastIndex = GetLastIndexOfNumber(index);
                string numberStr = json.Substring(index, lastIndex - index + 1);
                index = lastIndex + 1;
                if (numberStr.IndexOf('.') != -1 || numberStr.IndexOf('e') != -1 || numberStr.IndexOf('E') != -1)
                {
                    if (double.TryParse(numberStr, NumberStyles.Float, CultureInfo.InvariantCulture, out var d))
                        return d;
                }
                else
                {
                    if (long.TryParse(numberStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out var l))
                        return l;
                }
                // fallback
                double.TryParse(numberStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var dd);
                return dd;
            }

            private int GetLastIndexOfNumber(int idx)
            {
                int lastIndex;
                for (lastIndex = idx; lastIndex < json.Length; lastIndex++)
                {
                    if ("0123456789+-.eE".IndexOf(json[lastIndex]) == -1)
                        break;
                }
                return lastIndex - 1;
            }

            private void EatWhitespace()
            {
                for (; index < json.Length; index++)
                {
                    char ch = json[index];
                    if (ch == '\uFEFF' || ch == '\u200B' || ch == '\u200E' || ch == '\u200F') { continue; }
                    if (" \t\n\r".IndexOf(ch) == -1)
                        break;
                }
            }

            private char NextChar()
            {
                return json[index++];
            }

            private TOKEN LookAhead()
            {
                int saveIndex = index;
                return NextTokenCore(ref saveIndex);
            }

            private TOKEN NextToken()
            {
                return NextTokenCore(ref index);
            }

            private TOKEN NextTokenCore(ref int idx)
            {
                EatWhitespaceInternal(ref idx);
                if (idx == json.Length) return TOKEN.NONE;
                char c = json[idx++];
                switch (c)
                {
                    case '{':
                        return TOKEN.CURLY_OPEN;
                    case '}':
                        return TOKEN.CURLY_CLOSE;
                    case '[':
                        return TOKEN.SQUARED_OPEN;
                    case ']':
                        return TOKEN.SQUARED_CLOSE;
                    case ',':
                        return TOKEN.COMMA;
                    case '"':
                        return TOKEN.STRING;
                    case ':':
                        return TOKEN.COLON;
                    case '0':
                    case '1':
                    case '2':
                    case '3':
                    case '4':
                    case '5':
                    case '6':
                    case '7':
                    case '8':
                    case '9':
                    case '-':
                        return TOKEN.NUMBER;
                }
                idx--;
                int remainingLength = json.Length - idx;

                // true
                if (remainingLength >= 4 && json[idx] == 't' && json[idx + 1] == 'r' && json[idx + 2] == 'u' && json[idx + 3] == 'e')
                {
                    idx += 4; return TOKEN.TRUE;
                }

                // false
                if (remainingLength >= 5 && json[idx] == 'f' && json[idx + 1] == 'a' && json[idx + 2] == 'l' && json[idx + 3] == 's' && json[idx + 4] == 'e')
                {
                    idx += 5; return TOKEN.FALSE;
                }

                // null
                if (remainingLength >= 4 && json[idx] == 'n' && json[idx + 1] == 'u' && json[idx + 2] == 'l' && json[idx + 3] == 'l')
                {
                    idx += 4; return TOKEN.NULL;
                }

                return TOKEN.NONE;
            }

            private void EatWhitespaceInternal(ref int idx)
            {
                while (idx < json.Length)
                {
                    char ch = json[idx];
                    if (ch == '\uFEFF' || ch == '\u200B' || ch == '\u200E' || ch == '\u200F') { idx++; continue; }
                    if (" \t\n\r".IndexOf(ch) != -1) { idx++; continue; }
                    break;
                }
            }
        }

        private sealed class Serializer
        {
            StringBuilder builder;

            private Serializer()
            {
                builder = new StringBuilder();
            }

            public static string Serialize(object obj)
            {
                var instance = new Serializer();
                instance.SerializeValue(obj);
                return instance.builder.ToString();
            }

            private void SerializeValue(object value)
            {
                if (value == null)
                {
                    builder.Append("null");
                }
                else if (value is string)
                {
                    SerializeString((string)value);
                }
                else if (value is bool)
                {
                    builder.Append(((bool)value) ? "true" : "false");
                }
                else if (value is IList)
                {
                    SerializeArray((IList)value);
                }
                else if (value is IDictionary)
                {
                    SerializeObject((IDictionary)value);
                }
                else if (value is char)
                {
                    SerializeString(new string((char)value, 1));
                }
                else
                {
                    SerializeOther(value);
                }
            }

            private void SerializeObject(IDictionary obj)
            {
                bool first = true;
                builder.Append('{');
                foreach (object e in obj.Keys)
                {
                    if (!first) builder.Append(',');
                    SerializeString(e.ToString());
                    builder.Append(':');
                    SerializeValue(obj[e]);
                    first = false;
                }
                builder.Append('}');
            }

            private void SerializeArray(IList anArray)
            {
                builder.Append('[');
                bool first = true;
                for (int i = 0; i < anArray.Count; i++)
                {
                    if (!first) builder.Append(',');
                    SerializeValue(anArray[i]);
                    first = false;
                }
                builder.Append(']');
            }

            private void SerializeString(string str)
            {
                builder.Append('"');
                foreach (var c in str)
                {
                    switch (c)
                    {
                        case '\\': builder.Append("\\\\"); break;
                        case '"': builder.Append("\\\""); break;
                        case '\n': builder.Append("\\n"); break;
                        case '\r': builder.Append("\\r"); break;
                        case '\t': builder.Append("\\t"); break;
                        case '\b': builder.Append("\\b"); break;
                        case '\f': builder.Append("\\f"); break;
                        default:
                            if (c < ' ')
                            {
                                builder.AppendFormat("\\u{0:X4}", (int)c);
                            }
                            else builder.Append(c);
                            break;
                    }
                }
                builder.Append('"');
            }

            private void SerializeOther(object value)
            {
                if (value is float || value is double || value is decimal)
                {
                    builder.Append(Convert.ToString(value, CultureInfo.InvariantCulture));
                }
                else
                {
                    builder.Append(value.ToString());
                }
            }
        }
    }
}
