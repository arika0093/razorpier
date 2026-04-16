using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CSharpier.Core;
using CSharpier.Core.CSharp;

namespace Razorpier.Core;

public static class RazorFormatter
{
    private static readonly CodeFormatterOptions CSharpierOptions = new CodeFormatterOptions
    {
        EndOfLine = EndOfLine.LF,
        IndentStyle = IndentStyle.Spaces,
        IndentSize = 4,
    };

    public static string Format(string source)
    {
        var normalized = NormalizeNewLines(source ?? string.Empty);
        var sections = ParseSections(normalized);
        var orderedSections = sections
            .Select((section, index) => new OrderedSection(section, index))
            .OrderBy(item => item.Section.Kind)
            .ThenBy(item => item.Section.Kind == RazorSectionKind.Markup || item.Section.Kind == RazorSectionKind.Code ? item.Index : int.MinValue)
            .ThenBy(item => item.Section.Kind == RazorSectionKind.Markup || item.Section.Kind == RazorSectionKind.Code ? string.Empty : item.Section.Content, StringComparer.OrdinalIgnoreCase)
            .Select(item => FormatSection(item.Section))
            .Where(section => !string.IsNullOrWhiteSpace(section))
            .ToArray();

        return orderedSections.Length == 0
            ? string.Empty
            : string.Join("\n\n", orderedSections) + "\n";
    }

    private static string FormatSection(RazorSection section)
    {
        switch (section.Kind)
        {
            case RazorSectionKind.Markup:
                return MarkupFormatter.Format(section.Content);
            case RazorSectionKind.Code:
                return FormatCodeBlock(section.Content);
            default:
                return section.Content.Trim();
        }
    }

    private static List<RazorSection> ParseSections(string source)
    {
        var lines = NormalizeNewLines(source).Split('\n');
        var sections = new List<RazorSection>();
        var markup = new List<string>();

        for (var index = 0; index < lines.Length;)
        {
            var line = lines[index];
            var trimmed = line.Trim();

            if (trimmed.Length == 0)
            {
                markup.Add(string.Empty);
                index++;
                continue;
            }

            string codeBlock;
            int nextIndex;
            if (TryReadCodeBlock(lines, index, out codeBlock, out nextIndex))
            {
                FlushMarkup(markup, sections);
                sections.Add(new RazorSection(RazorSectionKind.Code, codeBlock));
                index = nextIndex;
                continue;
            }

            RazorSectionKind kind;
            if (TryGetDirectiveKind(trimmed, out kind))
            {
                FlushMarkup(markup, sections);
                sections.Add(new RazorSection(kind, trimmed));
                index++;
                continue;
            }

            markup.Add(line);
            index++;
        }

        FlushMarkup(markup, sections);
        return sections;
    }

    private static bool TryReadCodeBlock(string[] lines, int startIndex, out string block, out int nextIndex)
    {
        block = string.Empty;
        nextIndex = startIndex;

        if (!lines[startIndex].TrimStart().StartsWith("@code", StringComparison.Ordinal))
        {
            return false;
        }

        var builder = new StringBuilder();
        var scanner = new BraceScanner();
        var foundOpeningBrace = false;

        for (var index = startIndex; index < lines.Length; index++)
        {
            if (builder.Length > 0)
            {
                builder.Append('\n');
            }

            builder.Append(lines[index]);
            scanner.Scan(lines[index]);

            if (scanner.FoundOpeningBrace)
            {
                foundOpeningBrace = true;
            }

            if (foundOpeningBrace && scanner.Depth == 0)
            {
                block = builder.ToString().Trim();
                nextIndex = index + 1;
                return true;
            }
        }

        block = builder.ToString().Trim();
        nextIndex = lines.Length;
        return true;
    }

    private static void FlushMarkup(List<string> markup, List<RazorSection> sections)
    {
        var content = string.Join("\n", markup).Trim();
        if (content.Length > 0)
        {
            sections.Add(new RazorSection(RazorSectionKind.Markup, content));
        }

        markup.Clear();
    }

    private static bool TryGetDirectiveKind(string trimmedLine, out RazorSectionKind kind)
    {
        if (trimmedLine.StartsWith("@using ", StringComparison.Ordinal))
        {
            kind = RazorSectionKind.Using;
            return true;
        }

        if (trimmedLine.StartsWith("@page ", StringComparison.Ordinal))
        {
            kind = RazorSectionKind.Page;
            return true;
        }

        if (trimmedLine.StartsWith("@attribute ", StringComparison.Ordinal)
            || trimmedLine.StartsWith("@attributes ", StringComparison.Ordinal))
        {
            kind = RazorSectionKind.Attributes;
            return true;
        }

        if (trimmedLine.StartsWith("@inject ", StringComparison.Ordinal))
        {
            kind = RazorSectionKind.Inject;
            return true;
        }

        kind = RazorSectionKind.Using;
        return false;
    }

    private static string FormatCodeBlock(string block)
    {
        var normalized = NormalizeNewLines(block);
        var openBraceIndex = FindFirstBrace(normalized);
        var closeBraceIndex = FindMatchingBrace(normalized, openBraceIndex);

        if (openBraceIndex < 0 || closeBraceIndex < 0)
        {
            return normalized.Trim();
        }

        var body = normalized.Substring(openBraceIndex + 1, closeBraceIndex - openBraceIndex - 1).Trim('\n', '\r', ' ', '\t');
        var wrapped = WrapCodeBody(body);
        var result = CSharpFormatter.Format(wrapped, CSharpierOptions);
        var formattedBody = result.CompilationErrors.Any()
            ? body
            : ExtractFormattedBody(result.Code);

        return BuildCodeBlock(formattedBody);
    }

    private static string WrapCodeBody(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return "internal sealed class __RazorpierHost\n{\n}\n";
        }

        var indentedBody = string.Join(
            "\n",
            NormalizeNewLines(body)
                .Split('\n')
                .Select(line => line.Length == 0 ? string.Empty : "    " + line.TrimEnd()));

        return "internal sealed class __RazorpierHost\n{\n" + indentedBody + "\n}\n";
    }

    private static string ExtractFormattedBody(string formattedWrapper)
    {
        var normalized = NormalizeNewLines(formattedWrapper).Trim();
        var openBraceIndex = FindFirstBrace(normalized);
        var closeBraceIndex = FindMatchingBrace(normalized, openBraceIndex);
        if (openBraceIndex < 0 || closeBraceIndex < 0)
        {
            return string.Empty;
        }

        var body = normalized.Substring(openBraceIndex + 1, closeBraceIndex - openBraceIndex - 1).Trim('\n');
        return RemoveCommonIndent(body);
    }

    private static string BuildCodeBlock(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return "@code\n{\n}";
        }

        var formattedBody = string.Join(
            "\n",
            NormalizeNewLines(body)
                .Split('\n')
                .Select(line => line.Length == 0 ? string.Empty : "    " + line));

        return "@code\n{\n" + formattedBody + "\n}";
    }

    private static int FindFirstBrace(string text)
    {
        var scanner = new BraceScanner();
        for (var index = 0; index < text.Length; index++)
        {
            var match = scanner.Process(text[index], index, text);
            if (match.HasValue)
            {
                return match.Value;
            }
        }

        return -1;
    }

    private static int FindMatchingBrace(string text, int openBraceIndex)
    {
        if (openBraceIndex < 0)
        {
            return -1;
        }

        var scanner = new BraceScanner();
        for (var index = openBraceIndex; index < text.Length; index++)
        {
            var match = scanner.Process(text[index], index, text);
            if (match.HasValue && scanner.Depth == 0)
            {
                return match.Value;
            }
        }

        return -1;
    }

    private static string RemoveCommonIndent(string text)
    {
        var lines = NormalizeNewLines(text).Split('\n');
        var indent = lines
            .Where(line => line.Trim().Length > 0)
            .Select(line => line.TakeWhile(c => c == ' ' || c == '\t').Count())
            .DefaultIfEmpty(0)
            .Min();

        return string.Join(
            "\n",
            lines.Select(line => line.Length >= indent ? line.Substring(indent) : line)).Trim('\n');
    }

    private static string NormalizeNewLines(string value)
    {
        return value.Replace("\r\n", "\n").Replace('\r', '\n');
    }

    private sealed class OrderedSection
    {
        public OrderedSection(RazorSection section, int index)
        {
            Section = section;
            Index = index;
        }

        public RazorSection Section { get; }

        public int Index { get; }
    }

    private sealed class RazorSection
    {
        public RazorSection(RazorSectionKind kind, string content)
        {
            Kind = kind;
            Content = content;
        }

        public RazorSectionKind Kind { get; }

        public string Content { get; }
    }

    private enum RazorSectionKind
    {
        Using = 0,
        Page = 1,
        Attributes = 2,
        Markup = 3,
        Inject = 4,
        Code = 5,
    }

    private sealed class BraceScanner
    {
        private bool inSingleLineComment;
        private bool inMultiLineComment;
        private bool inString;
        private bool inCharacter;
        private bool escaped;

        public int Depth { get; private set; }

        public bool FoundOpeningBrace { get; private set; }

        public void Scan(string line)
        {
            for (var index = 0; index < line.Length; index++)
            {
                Process(line[index], index, line);
            }

            inSingleLineComment = false;
        }

        public int? Process(char current, int index, string line)
        {
            if (line == null)
            {
                line = string.Empty;
            }

            if (inSingleLineComment)
            {
                return null;
            }

            if (inMultiLineComment)
            {
                if (current == '*' && Peek(line, index + 1) == '/')
                {
                    inMultiLineComment = false;
                }

                return null;
            }

            if (inString)
            {
                if (!escaped && current == '"')
                {
                    inString = false;
                }

                escaped = current == '\\' && !escaped;
                return null;
            }

            if (inCharacter)
            {
                if (!escaped && current == '\'')
                {
                    inCharacter = false;
                }

                escaped = current == '\\' && !escaped;
                return null;
            }

            if (current == '/' && Peek(line, index + 1) == '/')
            {
                inSingleLineComment = true;
                return null;
            }

            if (current == '/' && Peek(line, index + 1) == '*')
            {
                inMultiLineComment = true;
                return null;
            }

            escaped = false;

            if (current == '"')
            {
                inString = true;
                return null;
            }

            if (current == '\'')
            {
                inCharacter = true;
                return null;
            }

            if (current == '{')
            {
                FoundOpeningBrace = true;
                Depth++;
                return index;
            }

            if (current == '}')
            {
                Depth--;
                return index;
            }

            return null;
        }

        private static char Peek(string text, int index)
        {
            return index < text.Length ? text[index] : '\0';
        }
    }
}

internal static class MarkupFormatter
{
    private static readonly HashSet<string> VoidElements = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "area", "base", "br", "col", "embed", "hr", "img", "input", "link", "meta", "param", "source", "track", "wbr",
    };

    public static string Format(string markup)
    {
        var lines = markup
            .Replace("\r\n", "\n")
            .Replace('\r', '\n')
            .Split('\n')
            .Select(line => line.TrimEnd())
            .ToList();

        TrimBlankEdges(lines);

        var formatted = new List<string>();
        var indent = 0;
        var previousBlank = false;

        foreach (var rawLine in lines)
        {
            var trimmed = rawLine.Trim();
            if (trimmed.Length == 0)
            {
                if (!previousBlank && formatted.Count > 0)
                {
                    formatted.Add(string.Empty);
                    previousBlank = true;
                }

                continue;
            }

            previousBlank = false;

            if (ShouldDedent(trimmed))
            {
                indent = Math.Max(0, indent - 1);
            }

            formatted.Add(new string(' ', indent * 4) + trimmed);

            if (ShouldIndent(trimmed))
            {
                indent++;
            }
        }

        return string.Join("\n", formatted);
    }

    private static void TrimBlankEdges(List<string> lines)
    {
        while (lines.Count > 0 && string.IsNullOrWhiteSpace(lines[0]))
        {
            lines.RemoveAt(0);
        }

        while (lines.Count > 0 && string.IsNullOrWhiteSpace(lines[lines.Count - 1]))
        {
            lines.RemoveAt(lines.Count - 1);
        }
    }

    private static bool ShouldDedent(string line)
    {
        return line.StartsWith("</", StringComparison.Ordinal)
            || line == "}"
            || line.StartsWith("} ", StringComparison.Ordinal)
            || line.StartsWith("@else", StringComparison.Ordinal)
            || line.StartsWith("@catch", StringComparison.Ordinal)
            || line.StartsWith("@finally", StringComparison.Ordinal);
    }

    private static bool ShouldIndent(string line)
    {
        if (line.EndsWith("{", StringComparison.Ordinal))
        {
            return true;
        }

        if (!line.StartsWith("<", StringComparison.Ordinal) || line.StartsWith("</", StringComparison.Ordinal) || line.Contains("</"))
        {
            return false;
        }

        if (line.StartsWith("<!--", StringComparison.Ordinal) || line.StartsWith("<!", StringComparison.Ordinal))
        {
            return false;
        }

        if (line.EndsWith("/>", StringComparison.Ordinal))
        {
            return false;
        }

        var tagName = GetTagName(line);
        return tagName != null && !VoidElements.Contains(tagName);
    }

    private static string? GetTagName(string line)
    {
        var start = line.IndexOf('<') + 1;
        if (start <= 0 || start >= line.Length)
        {
            return null;
        }

        var end = start;
        while (end < line.Length && !char.IsWhiteSpace(line[end]) && line[end] != '>' && line[end] != '/')
        {
            end++;
        }

        return end > start ? line.Substring(start, end - start) : null;
    }
}
