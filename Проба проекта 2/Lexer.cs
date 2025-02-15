﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Documents;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Проба_проекта_2
{
    class Lexer
    {
        public static Task<List<QualifiedToken>> Parse(IEnumerable<RawText> texts)
        {
            var lexer = new Lexer(texts);
            return Task.Run(() => lexer.Parse().ToList());
        }

        List<int> partIndices = new List<int>();
        List<TextPointer> pointers = new List<TextPointer>();
        string totalText;

        Lexer(IEnumerable<RawText> texts)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var text in texts)
            {
                partIndices.Add(sb.Length);
                sb.Append(text.Text);
                pointers.Add(text.Start);
            }
            totalText = sb.ToString();
        }

        Tuple<TextPointer, int> GetBasePointerAndOffset(int position)
        {
            var partNo = partIndices.BinarySearch(position);
            if (partNo < 0)
                partNo = ~partNo - 1;
            var partStart = partIndices[partNo];
            var delta = position - partStart;
            return Tuple.Create(pointers[partNo], delta);
        }

        static Regex combineRegex(Dictionary<string, string> name2pattern)
        {
            var combinedRegexParts =
                    "^(" +
                    string.Join("|", name2pattern.Select(
                            kvp => string.Format("(?<{0}>{1})", kvp.Key, kvp.Value))) +
                    ")";
            return new Regex(combinedRegexParts,
                             RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.Singleline);
        }

        static Lexer()
        {
            var numPatternWithoutSign = @"(\d+(\.\d*)?)|(\.\d+)";
            var numPatternWithSign = @"[+-]?" + numPatternWithoutSign;

            var regexParts = new Dictionary<string, string>()
            {
                { "ident",   @"\p{L}\w*" },
                { "num",     numPatternWithSign },
                { "comment", @"(/\*.*?\*/)" },
                { "punct",   @"(:=)|(<=)|(>=)|(==)|[;+\-\*/\(\){}:<>=]" },
                { "string",  "\"[^\"\\\\]*(?:\\\\.[^\"\\\\]*)*\"" },
                { "space",   @"\s+" }
            };

            recognizer = combineRegex(regexParts);

            regexParts["num"] = numPatternWithoutSign;
            recognizerWithoutSignedNumbers = combineRegex(regexParts);

            names = regexParts.Keys;
        }

        static Regex recognizer, recognizerWithoutSignedNumbers;
        static IEnumerable<string> names;
        static Dictionary<string, TokenType?> tokenMapping =
            new Dictionary<string, TokenType?>()
            {
                { "ident", TokenType.Ident },
                { "num", TokenType.Number },
                { "comment", TokenType.Comment },
                { "punct", TokenType.Punct },
                { "string",  TokenType.String },
                { "space", null }
            };

        IEnumerable<QualifiedToken> Parse()
        {
            string restLine = totalText;
            int currPos = 0;
            TokenType? lastSignigicantToken = null;

            while (restLine != "")
            {
                bool allowSignedNumber = lastSignigicantToken == TokenType.Punct;
                var match = (allowSignedNumber ? recognizer : recognizerWithoutSignedNumbers)
                        .Match(restLine);
                var nameAndGroup =
                        names.Select(name => new { name, group = match.Groups[name] })
                             .Single(ng => ng.group.Success);

                var text = nameAndGroup.group.Value;
                var length = nameAndGroup.group.Length;

                var tokenType = tokenMapping[nameAndGroup.name];
                if (tokenType == TokenType.Ident && CheckKeyword(text))
                    tokenType = TokenType.Keyword;

                if (tokenType != null)
                {
                    var start = GetBasePointerAndOffset(currPos);
                    var end = GetBasePointerAndOffset(currPos + length);
                    yield return new QualifiedToken()
                                     {
                                         Type = tokenType.Value,
                                         StartPosition = start.Item1,
                                         StartOffset = start.Item2,
                                         EndPosition = end.Item1,
                                         EndOffset = end.Item2
                                     };
                }

                if (tokenType != null && tokenType != TokenType.Comment)
                    lastSignigicantToken = tokenType;

                currPos += length;
                restLine = restLine.Substring(length);
            }
        }

        static HashSet<string> keywords = new HashSet<string>()
        {
            "ADD",
            "ALTER",
            "ALL",
            "AND",
            "ANY",
            "AS",
            "ASC",
            "BACKUP",
            "BETWEEN",
            "BY",
            "CASE",
            "CHECK",
            "COLUMN",
            "CONSTRAIN",
            "CREATE",
            "DATABASE",
            "DEFAULT",
            "DELETE",
            "DESC",
            "DISTINCT",
            "DROP",
            "EXEC",
            "EXISTS",
            "FOREIGN",
            "FROM",
            "FULL",
            "GROUP",
            "HAVING",
            "IN",
            "INDEX",
            "INNER",
            "INSERT",
            "INTO",
            "IS",
            "JOIN",
            "KEY",
            "LEFT",
            "LIKE",
            "LIMIT",
            "NOT",
            "NULL",
            "OR",
            "ORDER",
            "OUTER",
            "PRIMARY",
            "PROCEDURE",
            "RIGHT",
            "ROWNUM", 
            "SELECT",
            "SET",
            "TABLE",
            "TOP",
            "TRUNCATE",
            "UNION",
            "UNIQUE",
            "UPDATE",
            "VALUES",
            "VIEW",
            "WHERE"
        };

        bool CheckKeyword(string text)
        {
            return keywords.Contains(text);
        }
    }
}
