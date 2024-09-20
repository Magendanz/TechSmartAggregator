using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using DataAccess;

namespace TechSmartAggregator
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Usage: TechSmartAggregator <file.csv>");
                return;
            }
            var filename = args[0];

            // Load CSV file that has been exported from TechSmart Gradebook
            var dt = DataTable.New.ReadCsv(filename);

            dt.DeleteColumn(2);     // We don't care about the student email address
            FixColumnHeaders(dt);
            RemoveEmptyColumns(dt);
            AggregateAssignments(dt, "Classwork", "Warm Up", "Instruction", "Instruction Practice", "Teacher Code Planning", "Teacher Code Writing", "Student Code Planning", "Student Code Writing", "Student Code Debug", "Lesson Notes");
            ReduceColumns(dt, "Lesson Check", "Assessment");

            // Save result and open with Excel
            dt.SaveCSV(@$"Output\{filename}");
            Process.Start("explorer.exe", @$"Output\{filename}");
        }

        static void FixColumnHeaders(MutableDataTable dt)
        {
            int unit = 0, lesson = 0;

            foreach (var col in dt.Columns)
            {
                if (!string.IsNullOrWhiteSpace(col.Values[2]))
                {
                    // Get latest unit and lesson
                    var matches = Regex.Matches(col.Name, @$"Unit (\d+):");
                    if (matches.Count > 0)
                        int.TryParse(matches[0].Groups[1].Value, out unit);
                    matches = Regex.Matches(col.Values[0], @$"Lesson (\d+):");
                    if (matches.Count > 0)
                        int.TryParse(matches[0].Groups[1].Value, out lesson);

                    // Ensure name is unique so that we can delete by column name later
                    var prefix = $"{unit}.{lesson} ";
                    col.Name = col.Values[1];
                    if (!col.Name.StartsWith(prefix))
                        col.Name = prefix + col.Name;
                    while (dt.ColumnNames.Count(i => i == col.Name) > 1)
                        col.Name += " ";

                    // We'll use this for LINQ grouping later
                    col.Values[1] = prefix;
                }
            }
        }

        static void RemoveEmptyColumns(MutableDataTable dt)
        {
            var names = new List<string>();
            for (int i = dt.Columns.Length - 1; i > 0; i--)
                if (dt.Columns[i].Values.Count(c => !string.IsNullOrEmpty(c)) <= 4)       // First 4 rows are just header
                    dt.DeleteColumn(i);
        }

        static void AggregateAssignments(MutableDataTable dt, params string[] categories)
        {
            // Filter down to the assignment types of interest
            var cols = dt.Columns.Where(c => categories.Contains(c.Values[2]));

            // Group by lesson number
            var groups = cols.GroupBy(c => c.Values[1]);
            var names = new List<string>();
            foreach (var group in groups)
            {
                var first = group.First();
                first.Values[2] = "Assignments";
                for (int i = 4; i < first.Values.Count(); i++)
                {
                    // Set number of assignments completed for each student
                    first.Values[i] = $"{group.Sum(c => ParseAssignmentScore(c.Values[i]))}";
                }

                // Save total assignment count in header
                first.Values[3] = $"{group.Count()}";

                // Mark consolidated columns for removal
                foreach (var col in group)
                    if (col != first)
                        names.Add(col.Name);
            }
            dt.DeleteColumns(names.ToArray());
        }

        static int ParseAssignmentScore(string desc)
        {
            if (desc.Contains("Syntax error"))
                return 0;
            if (desc.StartsWith("Turned In"))
                return 1;

            var matches = Regex.Matches(desc, @$"(\d+)/(\d+)\s");
            if (matches.Count > 0)
            {
                var match = matches.First();
                double.TryParse(match.Groups[1].Value, out var n);
                double.TryParse(match.Groups[2].Value, out var d);

                // We'll consider 20% or greater to be complete
                return n / d >= 0.2 ? 1 : 0;
            }

            return 0;
        }

        static void ReduceColumns(MutableDataTable dt, params string[] categories)
        {
            // Filter down to the assignment types of interest
            var cols = dt.Columns.Where(c => categories.Contains(c.Values[2]));

            // Parse scores for each column to simplify presentation
            foreach (var col in cols)
            {
                for (int i = 4; i < col.Values.Count(); i++)
                {
                    // Look for fraction pattern
                    var matches = Regex.Matches(col.Values[i], @$"(\d+)/(\d+)\s");
                    if (matches.Count > 0)
                    {
                        var match = matches.First();
                        col.Values[i] = match.Groups[1].Value;      // Replace entry with numerator
                        col.Values[3] = match.Groups[2].Value;      // Save denominoator in header
                    }
                    else
                        col.Values[i] = string.Empty;       // We don't care about other states like "In progress"
                }
            }
        }
    }
}
