using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace TextMiningLibrary
{
    public static class SearchTextInPdf
    {
        public static List<string> Search(string filePath, string logPath, string conditionString)
        {
            var result = new List<string>();

            parseConditions(conditionString, out var orConditions, out var andConditions);
            verifyFilePath(filePath);
            verifyLogPath(logPath);
            verifyConditions(conditionString, orConditions, andConditions);

            using (PdfReader reader = new PdfReader(filePath))
            {
                using (StreamWriter file = new StreamWriter(logPath + @"\TextMiningLog.log", append: true))
                {
                    var pdfDocument = new PdfDocument(reader);
                    for (int pageNumber = 1; pageNumber <= pdfDocument.GetNumberOfPages(); pageNumber++)
                    {
                        var page = pdfDocument.GetPage(pageNumber);
                        var pageText = PdfTextExtractor.GetTextFromPage(page);
                        var lines = pageText.Split('\n');

                        var linesMatchingOr = handleOrCondition(orConditions, lines, pageNumber);
                        var linesMatchingAnd = handleAndCondition(andConditions, lines, pageNumber);
                        var linesForSingleCondition = handleSingleCondition(conditionString, orConditions, andConditions, lines, pageNumber);

                        var orLine = "Lines that matched the OR condition";
                        var andLine = "Lines that matched the AND condition";
                        var singleLine = "Lines that matched the condition";

                        buildResult(result, linesMatchingOr, linesMatchingAnd, linesForSingleCondition, orLine, andLine, singleLine, pageNumber);
                        writeLogLines(result, file);
                    }
                }
            }

            return result;
        }

        private static void writeLogLines(List<string> result, StreamWriter file)
        {
            foreach (var line in result)
                file.WriteLine(line);
        }

        private static void runLines(string[] lines, List<string> resultList, string treatedCondition, int pageNumber, params Action<string, int>[] extraActionsPerLine)
        {
            for (var lineNumber = 0; lineNumber < lines.Length; lineNumber++)
            {
                var line = lines[lineNumber];
                var treatedLine = line.ToLower();
                if (treatedLine.Contains(treatedCondition))
                {
                    feedResult(resultList, lineNumber, line, pageNumber);
                    foreach (var extraAction in extraActionsPerLine)
                        extraAction(line, lineNumber);
                }
            }
        }

        private static List<string> handleSingleCondition(string conditionString, string[] orConditions, string[] andConditions, string[] lines, int pageNumber)
        {
            var linesForSingleCondition = new List<string>();

            if (!orConditions.Any() && !andConditions.Any())
            {
                var treatedCondition = conditionString.ToLower().Trim();
                runLines(lines, linesForSingleCondition, treatedCondition, pageNumber);
            }

            return linesForSingleCondition;
        }

        private static List<string> handleAndCondition(string[] andConditions, string[] lines, int pageNumber)
        {
            foreach (var condition in andConditions)
            {
                var filteredLines = new List<string>();
                var treatedCondition = condition.ToLower().Trim();
                runLines(lines, filteredLines, treatedCondition, pageNumber);
                lines = filteredLines.ToArray();
            }

            if (andConditions.Any())
                return lines.ToList();

            return new List<string>();
        }

        private static List<string> handleOrCondition(string[] orConditions, string[] lines, int pageNumber)
        {
            var linesMatchingOr = new List<string>();
            foreach (var condition in orConditions)
            {
                var treatedCondition = condition.ToLower().Trim();
                runLines(lines, linesMatchingOr, treatedCondition, pageNumber);
            }

            return linesMatchingOr;
        }

        private static void feedResult(List<string> linesMatching, int lineNumber, string line, int pageNumber)
        {
            var result = new StringBuilder();
            result.Append(line);
            if (!line.Contains("| Page:") && !line.Contains("| Line:"))
            {
                result.Append(" | Page: ");
                result.Append(pageNumber);
                result.Append(" | Line: ");
                result.Append(lineNumber);
            }
            linesMatching.Add(result.ToString());
        }

        private static void buildResult(List<string> result, List<string> linesMatchingOr, List<string> linesMatchingAnd, List<string> linesForSingleCondition, string orLine, string andLine, string singleLine, int pageNumber)
        {
            appendDataToResult(result, linesMatchingOr, orLine, pageNumber);
            appendDataToResult(result, linesMatchingAnd, andLine, pageNumber);
            appendDataToResult(result, linesForSingleCondition, singleLine, pageNumber);
            if (!result.Any())
                result.Add("Nenhum registro encontrado.");
        }

        private static void appendDataToResult(List<string> result, List<string> content, string message, int pageNumber)
        {
            if (content.Any())
            {
                if (pageNumber == 1)
                    result.Add(message);

                result.AddRange(content);
            }
        }

        private static void parseConditions(string conditionString, out string[] orConditions, out string[] andConditions)
        {
            orConditions = new string[] { };
            andConditions = new string[] { };

            if (conditionString.Contains(" AND "))
                andConditions = conditionString.Split("AND");
            else if (conditionString.Contains(" OR "))
                orConditions = conditionString.Split("OR");
        }

        private static void verifyConditions(string conditionString, string[] orConditions, string[] andConditions)
        {
            if (!orConditions.Any() && !andConditions.Any() && string.IsNullOrWhiteSpace(conditionString))
                throw new Exception("No conditions found");
        }

        private static void verifyLogPath(string logPath)
        {
            if (!Directory.Exists(logPath))
                throw new Exception($"Log path {logPath} not found");
        }

        private static void verifyFilePath(string filePath)
        {
            if (!File.Exists(filePath))
                throw new Exception($"File {filePath} not found!");
        }
    }
}
