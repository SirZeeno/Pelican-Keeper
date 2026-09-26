using System.Reflection;
using System.Text.RegularExpressions;

namespace Pelican_Keeper.Validators;

public static class MarkdownValidator
{
    public static void ValidateMarkdown(string markdownFileContents)
    {
        var validation = Validate(markdownFileContents);

        if (validation.IsValid) return;
        foreach (var error in validation.Errors)
        {
            ConsoleExt.WriteLine(error, ConsoleExt.CurrentStep.Markdown, ConsoleExt.OutputType.Error, new FormatException());
        }
    }
    
    private static readonly HashSet<string> ValidVariables =
        typeof(TemplateClasses.ServerViewModel)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(p => p.Name)
            .ToHashSet(StringComparer.Ordinal);

    private static readonly HashSet<string> ValidBlocks =
    [
        "Title"
    ];

    private static readonly Regex VariableRegex =
        new(@"\{\{([^{}]*)\}\}", RegexOptions.Compiled);

    private static readonly Regex BlockRegex =
        new(@"\[(/?)([A-Za-z][A-Za-z0-9]*)\]",
            RegexOptions.Compiled);

    private static TemplateValidationResult Validate(string content)
    {
        var result = new TemplateValidationResult();

        if (string.IsNullOrEmpty(content))
        {
            result.Errors.Add(new TemplateValidationError
            {
                Message = "Template is empty.",
                Line = 1,
                Column = 1
            });

            return result;
        }

        ValidateVariables(content, result);
        ValidateMalformedVariables(content, result);
        ValidateBlocks(content, result);

        return result;
    }

    private static void ValidateVariables(string content, TemplateValidationResult result)
    {
        foreach (Match match in VariableRegex.Matches(content))
        {
            string variableName = match.Groups[1].Value.Trim();

            // {{ }}
            if (string.IsNullOrWhiteSpace(variableName))
            {
                result.Errors.Add(new TemplateValidationError
                {
                    Message = "Empty variable '{{}}'.",
                    Line = GetLineNumber(content, match.Index),
                    Column = GetColumnNumber(content, match.Index)
                });

                continue;
            }

            // {{SomethingThatDoesNotExist}}
            if (!ValidVariables.Contains(variableName))
            {
                result.Errors.Add(new TemplateValidationError
                {
                    Message = $"Unknown variable '{{{{{variableName}}}}}'.",
                    Line = GetLineNumber(content, match.Index),
                    Column = GetColumnNumber(content, match.Index)
                });
            }
        }
    }

    private static void ValidateMalformedVariables(string content, TemplateValidationResult result)
    {
        for (int i = 0; i < content.Length; i++)
        {
            if (content[i] != '{')
                continue;

            // We only care about {{
            if (i + 1 >= content.Length || content[i + 1] != '{')
                continue;

            // Find the closing }}
            int closingIndex = content.IndexOf("}}", i + 2, StringComparison.Ordinal);

            if (closingIndex == -1)
            {
                result.Errors.Add(new TemplateValidationError
                {
                    Message = "Variable is missing closing '}}'.",
                    Line = GetLineNumber(content, i),
                    Column = GetColumnNumber(content, i)
                });

                break;
            }

            // Skip past the closing }}
            i = closingIndex + 1;
        }
    }

    private static void ValidateBlocks(string content, TemplateValidationResult result)
    {
        var stack = new Stack<(string Name, int Position)>();

        foreach (Match match in BlockRegex.Matches(content))
        {
            bool isClosing = match.Groups[1].Value == "/";
            string blockName = match.Groups[2].Value;

            // [Something] / [/Something]
            if (!ValidBlocks.Contains(blockName))
            {
                result.Errors.Add(new TemplateValidationError
                {
                    Message = $"Unknown block '[{(isClosing ? "/" : "")}{blockName}]'.",
                    Line = GetLineNumber(content, match.Index),
                    Column = GetColumnNumber(content, match.Index)
                });

                continue;
            }

            // [Title]
            if (!isClosing)
            {
                stack.Push((blockName, match.Index));
                continue;
            }

            // [/Title] without [Title]
            if (stack.Count == 0)
            {
                result.Errors.Add(new TemplateValidationError
                {
                    Message = $"Unexpected closing block '[/{blockName}]'.",
                    Line = GetLineNumber(content, match.Index),
                    Column = GetColumnNumber(content, match.Index)
                });

                continue;
            }

            var opening = stack.Pop();

            // [Title] ... [/SomethingElse]
            if (!string.Equals(
                    opening.Name,
                    blockName,
                    StringComparison.Ordinal))
            {
                result.Errors.Add(new TemplateValidationError
                {
                    Message =
                        $"Expected '[/{opening.Name}]' but found '[/{blockName}]'.",
                    Line = GetLineNumber(content, match.Index),
                    Column = GetColumnNumber(content, match.Index)
                });
            }
        }

        // Anything left in the stack wasn't closed.
        while (stack.Count > 0)
        {
            var unclosed = stack.Pop();

            result.Errors.Add(new TemplateValidationError
            {
                Message = $"Missing closing block '[/{unclosed.Name}]'.",
                Line = GetLineNumber(content, unclosed.Position),
                Column = GetColumnNumber(content, unclosed.Position)
            });
        }
    }

    private static int GetLineNumber(string content, int position)
    {
        int line = 1;

        for (int i = 0; i < position && i < content.Length; i++)
        {
            if (content[i] == '\n')
                line++;
        }

        return line;
    }

    private static int GetColumnNumber(string content, int position)
    {
        int column = 1;

        for (int i = position - 1; i >= 0; i--)
        {
            if (content[i] == '\n')
                break;

            column++;
        }

        return column;
    }
}

public sealed class TemplateValidationResult
{
    public bool IsValid => Errors.Count == 0;

    public List<TemplateValidationError> Errors { get; } = [];
}
    
public sealed class TemplateValidationError
{
    public required string Message { get; init; }

    public int Line { get; init; }

    public int Column { get; init; }
}