﻿using System;
using System.IO;
using System.Linq;
using Microsoft.ProgramSynthesis.Compound.Split.Constraints;
using Constraint = Microsoft.ProgramSynthesis.Wrangling.Constraints.Constraint
    <Microsoft.ProgramSynthesis.DslLibrary.StringRegion,
        Microsoft.ProgramSynthesis.Wrangling.Schema.TableOutput.ITable
            <Microsoft.ProgramSynthesis.DslLibrary.StringRegion>>;

namespace Microsoft.ProgramSynthesis.Compound.Split.Sample 
{
    /// <summary>
    ///     Sample of how to restrict the language expressiveness of Split API
    ///     and read the properties of the synthesized program.
    /// </summary>
    public static class SampleProgram
    {
        private static void Main(string[] args)
        {
            RestrictedDslSample();
            FixedWidthOrDelimitedDemo();
            Console.WriteLine("\nDone.");
        }
        
        private static void RestrictedDslSample()
        {
            var input =
                @"#This is a comment
#number1 number2
1 2
3 4
5 6
";
            var constraints = new Constraint[] { new SimpleDelimiter() };

            var stringSession = new Session();
            stringSession.AddInput(input);
            stringSession.Constraints.Add(constraints);

            var streamSession = new Session();
            // Create stream from string
            streamSession.AddInput(new StringReader(input));
            streamSession.Constraints.Add(constraints);

            Session[] sessions = { stringSession, streamSession };
            foreach (Session session in sessions)
            {
                Program program = session.Learn();

                Console.WriteLine($"Skip lines = {program.Properties.SkipLinesCount}");

                Console.WriteLine($"Column delimiter = {program.Properties.ColumnDelimiter}");

                Console.WriteLine($"New line string = {string.Join(",", program.Properties.NewLineStrings)}");

                Console.WriteLine($"Column count = {program.Properties.ColumnCount}");

                if (program.Properties.RawColumnNames != null)
                {
                    Console.WriteLine($"Column names = {string.Join(",", program.Properties.RawColumnNames)}");
                }
                Console.WriteLine();
            }
        }
        
        private static void FixedWidthOrDelimitedDemo()
        {
            // two sample files containing the same data, one in delimited format and the other in fixed-width format:
            var delimitedFile =
                @"#This is a comment
#num1; num2; word1
12; 21132; abc
3123; 42; d
5; 625; ef
";
            var fixedWidthFile =
                @"#This is a comment
#num1 num2 word1
12    21132abc
3123  42   d
5     625  ef
";

            // we first consider the case where we do not know if the file is delimited or fixed width, 
            // and would like to determine the file type and metadata
            Session session = new Session();
            session.Constraints.Add(new SimpleDelimiterOrFixedWidth());

            // we first check the sample fixed-width file
            session.Inputs.Add(Session.CreateStringRegion(fixedWidthFile));
            Program program = session.Learn();
            Console.WriteLine($"Inferring the fixed-width file properties without knowing the file format:");
            PrintProperties(program);

            // we now check the sample delimited file
            session.Inputs.Clear();
            session.Inputs.Add(Session.CreateStringRegion(delimitedFile));
            program = session.Learn();
            Console.WriteLine($"Inferring the delimited file properties without knowing the file format:");
            PrintProperties(program);
            
            // we now consider the case where we know that the file is fixed-width and just want to get the metadata such as column start positions
            session.Inputs.Clear();
            session.Constraints.Clear();
            session.Constraints.Add(new FixedWidth());
            session.Inputs.Add(Session.CreateStringRegion(fixedWidthFile));
            program = session.Learn();
            Console.WriteLine($"Inferring the fixed width file properties when we know the format is fixed-width:");
            PrintProperties(program);

            // we now consider the case where we know the file is delimited and just want to get the metadata such as delimiters used
            session.Inputs.Clear();
            session.Constraints.Clear();
            session.Constraints.Add(new SimpleDelimiter());
            session.Inputs.Add(Session.CreateStringRegion(delimitedFile));
            program = session.Learn();
            Console.WriteLine($"Inferring the delimited file properties when we know the format is delimited:");
            PrintProperties(program);
        }

        private static void PrintProperties(Program program) 
        {
            if (program.Properties.FieldPositions == null)
            {
                Console.WriteLine("File type: Delimited");
                Console.WriteLine($"Column delimiter = {program.Properties.ColumnDelimiter}");
            }
            else
            {
                Console.WriteLine("File type: Fixed-width");
                var positions = program.Properties.FieldPositions.Select(p => $"({p.Item1}, {p.Item2?.ToString() ?? "null"})");
                Console.WriteLine($"Field positions = [ {string.Join(", ", positions)} ]");
            }
            Console.WriteLine($"Skip lines = {program.Properties.SkipLinesCount}");
            Console.WriteLine($"New line string = {string.Join(",", program.Properties.NewLineStrings)}");
            Console.WriteLine($"Column count = {program.Properties.ColumnCount}");
            if (program.Properties.RawColumnNames != null)
            {
                Console.WriteLine($"Column names = {string.Join(", ", program.Properties.RawColumnNames)}");
            }
            Console.WriteLine();
        }
    }
}
