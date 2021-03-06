// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace RoughBench
{
    /// <summary>
    ///  Benchmarker is compatible with Benchmark.net's BenchmarkRunner,
    ///  but favors quick results over accuracy and is designed for fast
    ///  "inner dev loop" code tuning and to provide performance regression tests.
    ///  
    ///  Benchmarker will run methods on each class with an attribute called [Benchmark].
    ///  It logs the results to the Console and a Markdown file (Results/Benchmark.yyyyMMddhhmmss.md).
    ///  It compares current results with Baseline.md in the output folder and the previou run.
    ///  Copy a result file to 'Baseline.md' to update the Baseline.
    /// </summary>
    public class Benchmarker
    {
        public const string OutputFolderPath = "Reports";
        public const string BaselinePath = "Baseline.md";
        public const string BaselineColumnName = "Baseline";
        public const string LastColumnName = "Last";
        public const string CalibrationMethodName = "Calibration";

        private double _failThreshold = 0.8d;
        private double _baselineAdjustment = 1.0d;

        private MeasureSettings _settings;
        private ConsoleTable _table;
        private Dictionary<string, Dictionary<string, double>> _comparisons;

        public bool HasFailures { get; private set; }
        public string OutputPath { get; }

        public Benchmarker(MeasureSettings settings = null)
        {
            _settings = settings ?? MeasureSettings.Default;
            _comparisons = LoadComparisons();

            List<TableCell> columns = new List<TableCell>();
            columns.Add(new TableCell("Name"));
            columns.Add(new TableCell("Mean", Align.Right, TableColor.Green));

            foreach (string baseline in _comparisons.Keys)
            {
                columns.Add(new TableCell(baseline, Align.Right));
                columns.Add(new TableCell("/Mean", Align.Right));
            }

            _table = new ConsoleTable(columns);

            Directory.CreateDirectory(OutputFolderPath);
            OutputPath = Path.GetFullPath($"{OutputFolderPath}/Benchmarks.{DateTime.UtcNow:yyyyMMddhhmmss}.md");

            Calibrate();
        }

        public void WriteSummary()
        {
            _table.Save(File.Create(OutputPath));

            Console.WriteLine();
            Console.WriteLine($"Saved as: \"{OutputPath}\"");
            Console.WriteLine($"To update baseline, replace \"{BaselinePath}\" with latest.");

            if (HasFailures)
            {
                Console.WriteLine("FAIL: At least one benchmark regressed versus baseline.");
            }
            else
            {
                Console.WriteLine("PASS: All benchmarks fast enough versus baseline.");
            }
        }

        /// <summary>
        ///  Similar to Benchmark.net's Benchmarker.Run.
        ///  Benchmarks each method on the given class with the [Benchmark] attribute.
        ///  Less accurate but much faster to complete than Benchmark.net runs.
        /// </summary>
        /// <typeparam name="T">Type containing methods to benchmark</typeparam>
        /// <param name="settings">Measurement settings, or null for defaults</param>
        public void Run<T>()
        {
            Run(typeof(T));
        }

        /// <summary>
        ///  Similar to Benchmark.net's Benchmarker.Run.
        ///  Benchmarks each method on the given class with the [Benchmark] attribute.
        ///  Less accurate but much faster to complete than Benchmark.net runs.
        /// </summary>
        /// <param name="typeWithBenchmarkMethods">Type containing methods to benchmark</typeparam>
        /// <param name="settings">Measurement settings, or null for defaults</param>
        public void Run(Type typeWithBenchmarkMethods)
        {
            Dictionary<string, Action> benchmarkMethods = BenchmarkReflector.BenchmarkMethods<Action>(typeWithBenchmarkMethods);

            foreach (var method in benchmarkMethods)
            {
                Run(method.Key, method.Value);
            }
        }

        /// <summary>
        ///  Similar to Benchmark.net's Benchmarker.Run.
        ///  Benchmarks each method on the given class with the [Benchmark] attribute.
        ///  Less accurate but much faster to complete than Benchmark.net runs.
        /// </summary>
        /// <param name="typeWithBenchmarkMethods">Type containing methods to benchmark</typeparam>
        /// <param name="settings">Measurement settings, or null for defaults</param>
        public MeasureResult Run(string methodName, Action method)
        {
            List<TableCell> row = new List<TableCell>();

            // Benchmark this method
            MeasureResult result = Measure.Operation(method, _settings);

            // Report current time
            row.Add(TableCell.String(methodName));
            row.Add(TableCell.Time(result.SecondsPerIteration));

            // Compare to each loaded benchmark
            foreach (var comparison in _comparisons)
            {
                bool isBaseline = comparison.Key == BaselineColumnName;

                double comparisonTime;
                if (!comparison.Value.TryGetValue(methodName, out comparisonTime)) { comparisonTime = 0.0; }

                row.Add(TableCell.Time(comparisonTime));
                row.Add(BenchmarkRatio(comparisonTime, result.SecondsPerIteration, isBaseline));

                if (isBaseline && row.Last().Color == TableColor.Red) { HasFailures = true; }
            }

            _table.AppendRow(row);
            return result;
        }

        private TableCell BenchmarkRatio(double comparisonTime, double currentTime, bool isBaseline)
        {
            TableColor color = TableColor.Default;

            if (currentTime != 0.0)
            {
                double calibratedComparison = (isBaseline ? comparisonTime * _baselineAdjustment : comparisonTime);
                double ratio = calibratedComparison / currentTime;

                if (ratio > (1 / _failThreshold))
                {
                    color = TableColor.Green;
                }
                else if (ratio < _failThreshold)
                {
                    color = TableColor.Red;
                }
            }

            return TableCell.Ratio(comparisonTime, currentTime, color);
        }

        internal void Calibrate()
        {
            MeasureResult calibrationResult = Run(CalibrationMethodName, CalibrationFunction);

            // Ensure a "failure" in calibration isn't counted
            HasFailures = false;

            // Save calibration ratio
            Dictionary<string, double> baseline = _comparisons.Values.FirstOrDefault();
            if (baseline != null && baseline.TryGetValue(CalibrationMethodName, out double baselineSeconds) && baselineSeconds >= 0.0)
            {
                // If current was 2x baseline, must multiply other baselines by 2x to get a scaled value to compare to.
                _baselineAdjustment = (calibrationResult.SecondsPerIteration / baselineSeconds);
            }
        }

        private static void CalibrationFunction()
        {
            long sum = 0;

            for (int i = 0; i < 1000; ++i)
            {
                sum += i;
            }
        }

        private static void Nothing()
        {
            // Available to test overhead of benchmarking infrastructure 
            // (2.5 ns in Debug typical)
        }

        internal static Dictionary<string, Dictionary<string, double>> LoadComparisons()
        {
            Dictionary<string, Dictionary<string, double>> reports = new Dictionary<string, Dictionary<string, double>>();
            Dictionary<string, double> report;

            if (TryLoadReport(BaselinePath, out report))
            {
                reports[BaselineColumnName] = report;
            }

            if (Directory.Exists(OutputFolderPath) && TryLoadReport(Directory.GetFiles(OutputFolderPath).LastOrDefault(), out report))
            {
                reports[LastColumnName] = report;
            }

            return reports;
        }

        /// <summary>
        ///  Load and Parse a previously written Markdown report, returning the previous
        ///  function names and Mean times in a Dictionary to use as baseline values for
        ///  the current run.
        /// </summary>
        /// <remarks>
        ///  Requires Function Name to be the first column and Mean to be the second column.
        /// </remarks>
        /// <param name="reportFilePath">File Path of Baseline ConsoleTable Markdown to load</param>
        /// <param name="result">Loaded baseline, if found and parsed successfully</param>
        /// <returns>True if baseline loaded, False otherwise</returns>
        internal static bool TryLoadReport(string reportFilePath, out Dictionary<string, double> result)
        {
            result = null;
            if (reportFilePath == null || !File.Exists(reportFilePath)) { return false; }

            Dictionary<string, double> report = new Dictionary<string, double>();

            try
            {
                IEnumerable<string> reportLines = File.ReadLines(reportFilePath);
                string headingLine = reportLines.First();

                List<string> columnNames = headingLine
                    .Split('|')
                    .Select((cell) => cell.Trim())
                    .ToList();

                foreach (string contentLine in reportLines.Skip(2))
                {
                    string[] cells = contentLine.Split('|');
                    string functionName = cells[1].Trim();
                    double meanSeconds = Format.ParseTime(cells[2].Trim());
                    report[functionName] = meanSeconds;
                }
            }
            catch (FileNotFoundException)
            {
                // Return no baseline available
                return false;
            }
            catch (FormatException) when (!Debugger.IsAttached)
            {
                // Return no baseline available
                Console.WriteLine($"Unable to parse baseline \"{reportFilePath}\". Excluding.");
                return false;
            }

            result = report;
            return true;
        }
    }
}
