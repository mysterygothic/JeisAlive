using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace JeisAlive.Engine
{
    public static class PolymorphicTransforms
    {
        private static readonly RandomNumberGenerator Rng = RandomNumberGenerator.Create();

        public static string Apply(string source)
        {
            // Transforms are disabled — they require placeholder markers in the
            // generated C source (e.g. __FN_*, __VAR_*, __STR_*) to work correctly.
            // The stub is already unique per build due to random encryption keys/salt.
            return source;
        }

        private static string RandomizeIdentifiers(string source)
        {
            var mapping = new Dictionary<string, string>();
            var pattern = new Regex(@"__(FN|VAR|STR)_[A-Za-z0-9_]+__");

            return pattern.Replace(source, match =>
            {
                string placeholder = match.Value;
                if (!mapping.TryGetValue(placeholder, out string? replacement))
                {
                    replacement = GenerateIdentifier(RandomInt(8, 13));
                    mapping[placeholder] = replacement;
                }
                return replacement;
            });
        }

        private static string ShuffleFunctions(string source)
        {
            var functionPattern = new Regex(
                @"^(static\s+\S+\s+\S+\s*\([^)]*\)\s*\{)",
                RegexOptions.Multiline);

            var functions = new List<(int Start, int End, string Body, bool IsMain)>();
            var matches = functionPattern.Matches(source);

            foreach (Match match in matches)
            {
                int braceStart = match.Value.LastIndexOf('{') + match.Index;
                int bodyEnd = FindMatchingBrace(source, braceStart);
                if (bodyEnd < 0) continue;

                string fullFunction = source.Substring(match.Index, bodyEnd - match.Index + 1);
                bool isMain = match.Value.Contains("main(") || match.Value.Contains("WinMain(");
                functions.Add((match.Index, bodyEnd, fullFunction, isMain));
            }

            if (functions.Count < 2) return source;

            var nonMain = functions.Where(f => !f.IsMain).ToList();
            var main = functions.Where(f => f.IsMain).ToList();

            Shuffle(nonMain);

            var reordered = nonMain.Concat(main).ToList();

            var sb = new StringBuilder();
            var sorted = functions.OrderBy(f => f.Start).ToList();

            int preambleEnd = sorted[0].Start;
            sb.Append(source, 0, preambleEnd);

            for (int i = 0; i < reordered.Count; i++)
            {
                if (i > 0) sb.AppendLine();
                sb.AppendLine(reordered[i].Body);
            }

            int lastOriginalEnd = sorted[sorted.Count - 1].End + 1;
            if (lastOriginalEnd < source.Length)
                sb.Append(source, lastOriginalEnd, source.Length - lastOriginalEnd);

            return sb.ToString();
        }

        private static string InsertJunkCode(string source)
        {
            var result = new StringBuilder(source.Length * 2);
            int i = 0;

            while (i < source.Length)
            {
                result.Append(source[i]);

                if (source[i] == '{' && IsFunctionBodyStart(source, i))
                {
                    result.AppendLine();
                    InsertJunkStatements(result);
                }

                i++;
            }

            return result.ToString();
        }

        private static bool IsFunctionBodyStart(string source, int braceIndex)
        {
            int lookback = Math.Max(0, braceIndex - 200);
            string preceding = source.Substring(lookback, braceIndex - lookback).TrimEnd();

            if (Regex.IsMatch(preceding, @"\[\]\s*=\s*$"))
                return false;

            return Regex.IsMatch(preceding, @"\)\s*$");
        }

        private static void InsertJunkStatements(StringBuilder sb)
        {
            int varCount = RandomInt(1, 4);
            int deadCount = RandomInt(0, 3);
            var junkVars = new List<string>();

            for (int i = 0; i < varCount; i++)
            {
                string name = "_j" + GenerateIdentifier(6);
                junkVars.Add(name);
                int a = RandomInt(0x10000, int.MaxValue);
                sb.AppendLine($"    volatile int {name} = 0x{a:X} ^ 0x{a:X};");
            }

            for (int i = 0; i < deadCount && junkVars.Count > 0; i++)
            {
                string v = junkVars[RandomInt(0, junkVars.Count)];
                int pattern = RandomInt(0, 3);
                switch (pattern)
                {
                    case 0:
                        sb.AppendLine($"    if (({v} * {v} + 1) % 2 == 1) {{ {v}++; }}");
                        break;
                    case 1:
                        sb.AppendLine($"    if ({v} > 0x7FFFFFFF) {{ {v} = ~{v}; }}");
                        break;
                    case 2:
                        sb.AppendLine($"    {v} = ({v} | 0) & (~{v} | {v});");
                        break;
                }
            }
        }

        private static string ObfuscateStringLiterals(string source)
        {
            var assignPattern = new Regex(
                @"(?<prefix>[=,(]\s*)""(?<content>[^""\\]{3,}(?:\\.[^""\\]*)*)""");

            var result = new StringBuilder(source.Length * 2);
            var hoisted = new List<string>();
            int lastIndex = 0;

            foreach (Match match in assignPattern.Matches(source))
            {
                if (IsInsideArrayInitializer(source, match.Index))
                    continue;

                string content = UnescapeCString(match.Groups["content"].Value);
                if (content.Length < 3) continue;

                byte xorKey = GenerateXorKey();
                string varName = "_s" + GenerateIdentifier(6);

                var charEntries = new StringBuilder();
                for (int i = 0; i < content.Length; i++)
                {
                    if (i > 0) charEntries.Append(", ");
                    charEntries.Append($"0x{((byte)content[i] ^ xorKey):X2}");
                }
                charEntries.Append(", 0");

                string decl = $"char {varName}[] = {{ {charEntries} }}; " +
                              $"for(int _i=0; _i<sizeof({varName})-1; _i++) {varName}[_i] ^= 0x{xorKey:X2};";
                hoisted.Add(decl);

                result.Append(source, lastIndex, match.Index - lastIndex);
                result.Append(match.Groups["prefix"].Value);
                result.Append(varName);

                lastIndex = match.Index + match.Length;
            }

            if (hoisted.Count == 0)
                return source;

            result.Append(source, lastIndex, source.Length - lastIndex);

            string transformed = result.ToString();
            return InjectHoistedDeclarations(transformed, hoisted);
        }

        private static string InjectHoistedDeclarations(string source, List<string> declarations)
        {
            var sb = new StringBuilder(source.Length + declarations.Sum(d => d.Length + 10));
            var funcBodyPattern = new Regex(@"\)\s*\{");
            var matches = funcBodyPattern.Matches(source);

            if (matches.Count == 0)
            {
                sb.AppendLine(string.Join(Environment.NewLine, declarations.Select(d => "    " + d)));
                sb.Append(source);
                return sb.ToString();
            }

            var firstFunc = matches[0];
            int insertPos = firstFunc.Index + firstFunc.Length;

            sb.Append(source, 0, insertPos);
            sb.AppendLine();
            foreach (var decl in declarations)
                sb.AppendLine("    " + decl);
            sb.Append(source, insertPos, source.Length - insertPos);

            return sb.ToString();
        }

        private static bool IsInsideArrayInitializer(string source, int position)
        {
            int searchStart = Math.Max(0, position - 100);
            string preceding = source.Substring(searchStart, position - searchStart);
            return Regex.IsMatch(preceding, @"\[\]\s*=\s*\{[^}]*$");
        }

        private static string UnescapeCString(string s)
        {
            return s.Replace("\\n", "\n")
                    .Replace("\\t", "\t")
                    .Replace("\\r", "\r")
                    .Replace("\\\\", "\\")
                    .Replace("\\\"", "\"")
                    .Replace("\\0", "\0");
        }

        private static int FindMatchingBrace(string source, int openIndex)
        {
            int depth = 0;
            for (int i = openIndex; i < source.Length; i++)
            {
                if (source[i] == '{') depth++;
                else if (source[i] == '}') depth--;

                if (depth == 0) return i;
            }
            return -1;
        }

        private static string GenerateIdentifier(int length)
        {
            const string letters = "abcdefghijklmnopqrstuvwxyz";
            const string alphanumeric = "abcdefghijklmnopqrstuvwxyz0123456789";

            byte[] bytes = new byte[length];
            Rng.GetBytes(bytes);

            var result = new char[length];
            result[0] = letters[bytes[0] % letters.Length];
            for (int i = 1; i < length; i++)
                result[i] = alphanumeric[bytes[i] % alphanumeric.Length];

            return new string(result);
        }

        private static int RandomInt(int minInclusive, int maxExclusive)
        {
            byte[] bytes = new byte[4];
            Rng.GetBytes(bytes);
            int raw = BitConverter.ToInt32(bytes, 0) & int.MaxValue;
            return minInclusive + (raw % (maxExclusive - minInclusive));
        }

        private static byte GenerateXorKey()
        {
            byte[] b = new byte[1];
            do { Rng.GetBytes(b); } while (b[0] == 0);
            return b[0];
        }

        private static void Shuffle<T>(List<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = RandomInt(0, i + 1);
                T temp = list[i];
                list[i] = list[j];
                list[j] = temp;
            }
        }
    }
}
