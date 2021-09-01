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
        private static string SEARCHSDONE_FILE = "SearchsDone.txt";
        private static long? searchsDone;

        public static List<string> Search(string filePath, string logPath, string conditionString)
        {
            var result = new List<string>();

            parseConditions(conditionString, out var conditions, out var isAndCondition);
            verifyFilePath(filePath);
            verifyLogPath(logPath);
            verifyConditions(conditionString, conditions);

            loadSearchsDone();
            increaseSearchDone();
            using (PdfReader reader = new PdfReader(filePath))
            {
                using (StreamWriter file = new StreamWriter(logPath + @"\TextMiningHistory.txt", append: true))
                {
                    var occurrences = new Dictionary<string, int>();

                    var pdfDocument = new PdfDocument(reader);
                    for (int pageNumber = 1; pageNumber <= pdfDocument.GetNumberOfPages(); pageNumber++)
                    {
                        var page = pdfDocument.GetPage(pageNumber);
                        var pageText = PdfTextExtractor.GetTextFromPage(page);
                        var lines = pageText.Split('\n');

                        var linesMatchingCondition = handleConditions(conditions, lines, pageNumber);
                        avaliateResults(linesMatchingCondition, occurrences);
                    }

                    buildResult(isAndCondition, filePath, conditionString, result, occurrences);
                    writeLogLines(result, file);
                }
            }

            return result;
        }

        private static void loadSearchsDone()
        {
            if (File.Exists(SEARCHSDONE_FILE))
            {
                using (StreamReader file = new StreamReader(SEARCHSDONE_FILE))
                {
                    var fileData = file.ReadToEnd();
                    if (long.TryParse(fileData, out var result))
                        searchsDone = result;
                }
            }       
        }

        private static void increaseSearchDone()
        {
            if (searchsDone == null)
                searchsDone = 0;

            searchsDone++;
            using (StreamWriter file = new StreamWriter(SEARCHSDONE_FILE))
            {
                file.WriteLine(searchsDone);
            }
        }

        private static void avaliateResults(Dictionary<string, int> linesMatchingCondition, Dictionary<string, int> occurrences)
        {
            if (linesMatchingCondition.Where(el => el.Value > 0).Any())
            {
                foreach (var value in linesMatchingCondition)
                    feedOccurrences(occurrences, value);
            }
        }

        private static void feedOccurrences(Dictionary<string, int> occurrences, KeyValuePair<string, int> value)
        {
            if (occurrences.ContainsKey(value.Key))
                occurrences[value.Key] += value.Value;
            else
                occurrences[value.Key] = value.Value;
        }

        private static void writeLogLines(List<string> result, StreamWriter file)
        {
            foreach (var line in result)
                file.WriteLine(line);
        }

        private static void runLines(string[] lines, Dictionary<string, int> resultList, string treatedCondition, int pageNumber)
        {
            if (!resultList.ContainsKey(treatedCondition))
                resultList[treatedCondition] = 0;

            for (var lineNumber = 0; lineNumber < lines.Length; lineNumber++)
            {
                var line = lines[lineNumber];
                var treatedLine = line.ToLower();
                if (treatedLine.Contains(treatedCondition))
                {
                    int count = 0;
                    var first = treatedLine.IndexOf(treatedCondition);
                    var last = treatedLine.LastIndexOf(treatedCondition);

                    if (first != last)
                    {
                        while (first != last)
                        {
                            count++;
                            var newSearch = treatedLine.Substring(first, last - first);
                            first = treatedLine.IndexOf(newSearch);
                            last = treatedLine.LastIndexOf(newSearch);

                        }
                    }
                    else
                        count = 1;

                    resultList[treatedCondition] += count;
                }
            }
        }

        private static Dictionary<string, int> handleSingleCondition(string conditionString, string[] orConditions, string[] andConditions, string[] lines, int pageNumber)
        {
            var linesForSingleCondition = new Dictionary<string, int>();

            if (!orConditions.Any() && !andConditions.Any())
            {
                var treatedCondition = conditionString.ToLower().Trim();
                runLines(lines, linesForSingleCondition, treatedCondition, pageNumber);
            }

            return linesForSingleCondition;
        }

        private static Dictionary<string, int> handleConditions(string[] conditions, string[] lines, int pageNumber)
        {
            var linesMatchingAnd = new Dictionary<string, int>();
            foreach (var condition in conditions)
            {
                var treatedCondition = condition.ToLower().Trim();
                runLines(lines, linesMatchingAnd, treatedCondition, pageNumber);
            }

            return linesMatchingAnd;
        }

        private static void buildResult(bool isAndCondition, string filePath, string conditionString, List<string> result, Dictionary<string, int> values)
        {
            var occurencesString = new StringBuilder();

            if (!isAndCondition || (isAndCondition && values.Any() && values.All(el => el.Value > 0)))
            {
                foreach (var value in values)
                {
                    occurencesString.Append(value.Key);
                    occurencesString.Append(" ( ");
                    occurencesString.Append(value.Value);
                    occurencesString.Append(" ) ");
                }
            }

            result.Add("********************************************");
            result.Add($"Número da consulta: {searchsDone}");
            result.Add($"Nome do documento: {Path.GetFileName(filePath)}");
            result.Add($"String de busca: {conditionString}");
            result.Add($"Ocorrências: {occurencesString}");
            if (!result.Any())
                result.Add("Nenhum registro encontrado.");
            result.Add("********************************************");
        }

        private static void parseConditions(string conditionString, out string[] conditions, out bool isAndCondition)
        {
            conditions = new string[1] { conditionString };
            isAndCondition = false;

            if (conditionString.Contains(" AND "))
            {
                conditions = conditionString.Split("AND");
                isAndCondition = true;
            }
            else if (conditionString.Contains(" OR "))
                conditions = conditionString.Split("OR");
        }

        private static void verifyConditions(string conditionString, string[] conditions)
        {
            if (!conditions.Any() && string.IsNullOrWhiteSpace(conditionString))
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
