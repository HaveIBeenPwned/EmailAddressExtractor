using Microsoft.VisualStudio.TestTools.UnitTesting;
using MyAddressExtractor;

namespace AddressExtractorTest
{
    [TestClass]
    public class CommandLineProcessorTests
    {
        [TestMethod]
        public void NoArguments()
        {
            var args = Array.Empty<string>();
            var inputs = new List<string>();
            Assert.ThrowsException<ArgumentException>(() => CommandLineProcessor.Process(args, inputs));
        }

        [TestMethod]
        public void NoInputFiles()
        {
            var args = new[] { "-o", "output" };
            var inputs = new List<string>();
            Assert.ThrowsException<ArgumentException>(() => CommandLineProcessor.Process(args, inputs));
        }

        [TestMethod]
        public void MissingOutputAtEnd()
        {
            var args = new[] { "input1", "-o" };
            var inputs = new List<string>();
            Assert.ThrowsException<ArgumentException>(() => CommandLineProcessor.Process(args, inputs));
        }

        [TestMethod]
        public void MissingReportAtEnd()
        {
            var args = new[] { "input1", "-r" };
            var inputs = new List<string>();
            Assert.ThrowsException<ArgumentException>(() => CommandLineProcessor.Process(args, inputs));
        }

        [TestMethod]
        public void MissingOutput()
        {
            var args = new[] { "input1", "-o", "-r", "report" };
            var inputs = new List<string>();
            Assert.ThrowsException<ArgumentException>(() => CommandLineProcessor.Process(args, inputs));
        }

        [TestMethod]
        public void MissingReport()
        {
            var args = new[] { "input1", "-r", "-o", "output" };
            var inputs = new List<string>();
            Assert.ThrowsException<ArgumentException>(() => CommandLineProcessor.Process(args, inputs));
        }

        [TestMethod]
        public void MissingOption()
        {
            var args = new[] { "input1", "-" };
            var inputs = new List<string>();
            Assert.ThrowsException<ArgumentException>(() => CommandLineProcessor.Process(args, inputs));
        }

        [TestMethod]
        public void InvalidOption()
        {
            var args = new[] { "input1", "-z" };
            var inputs = new List<string>();
            Assert.ThrowsException<ArgumentException>(() => CommandLineProcessor.Process(args, inputs));
        }

        [TestMethod]
        public void Usage()
        {
            var args = new[] { "-?" };
            var inputs = new List<string>();
            CommandLineProcessor.Process(args, inputs);
        }

        [TestMethod]
        public void UsageWithOtherOptionsBefore()
        {
            var args = new[] { "input", "-?" };
            var inputs = new List<string>();
            Assert.ThrowsException<ArgumentException>(() => CommandLineProcessor.Process(args, inputs));
        }

        [TestMethod]
        public void UsageWithOtherOptionsAfter()
        {
            var args = new[] { "-?", "input" };
            var inputs = new List<string>();
            Assert.ThrowsException<ArgumentException>(() => CommandLineProcessor.Process(args, inputs));
        }

        [TestMethod]
        public void Version()
        {
            var args = new[] { "-v" };
            var inputs = new List<string>();
            CommandLineProcessor.Process(args, inputs);
        }

        [TestMethod]
        public void VersionWithOtherOptionsBefore()
        {
            var args = new[] { "input", "-v" };
            var inputs = new List<string>();
            Assert.ThrowsException<ArgumentException>(() => CommandLineProcessor.Process(args, inputs));
        }

        [TestMethod]
        public void VersionWithOtherOptionsAfter()
        {
            var args = new[] { "-v", "input" };
            var inputs = new List<string>();
            Assert.ThrowsException<ArgumentException>(() => CommandLineProcessor.Process(args, inputs));
        }

        [TestMethod]
        public void OneInput()
        {
            var args = new[] { "input1" };
            var inputs = new List<string>();
            CommandLineProcessor.Process(args, inputs);
            Assert.AreEqual(inputs.Count, 1);
            Assert.AreEqual(inputs[0], args[0]);
            Assert.AreEqual(CommandLineProcessor.OUTPUT_FILE_PATH, CommandLineProcessor.Defaults.OUTPUT_FILE_PATH);
            Assert.AreEqual(CommandLineProcessor.REPORT_FILE_PATH, CommandLineProcessor.Defaults.REPORT_FILE_PATH);
        }

        [TestMethod]
        public void TwoInputs()
        {
            var args = new[] { "input1", "input2" };
            var inputs = new List<string>();
            CommandLineProcessor.Process(args, inputs);
            Assert.AreEqual(inputs.Count, 2);
            Assert.AreEqual(inputs[0], args[0]);
            Assert.AreEqual(inputs[1], args[1]);
            Assert.AreEqual(CommandLineProcessor.OUTPUT_FILE_PATH, CommandLineProcessor.Defaults.OUTPUT_FILE_PATH);
            Assert.AreEqual(CommandLineProcessor.REPORT_FILE_PATH, CommandLineProcessor.Defaults.REPORT_FILE_PATH);
        }

        [TestMethod]
        public void Output()
        {
            var args = new[] { "input1", "-o", "output" };
            var inputs = new List<string>();
            CommandLineProcessor.Process(args, inputs);
            Assert.AreEqual(inputs.Count, 1);
            Assert.AreEqual(inputs[0], args[0]);
            Assert.AreEqual(CommandLineProcessor.OUTPUT_FILE_PATH, args[2]);
            Assert.AreEqual(CommandLineProcessor.REPORT_FILE_PATH, CommandLineProcessor.Defaults.REPORT_FILE_PATH);
        }

        [TestMethod]
        public void Report()
        {
            var args = new[] { "input1", "-r", "report" };
            var inputs = new List<string>();
            CommandLineProcessor.Process(args, inputs);
            Assert.AreEqual(inputs.Count, 1);
            Assert.AreEqual(inputs[0], args[0]);
            Assert.AreEqual(CommandLineProcessor.OUTPUT_FILE_PATH, CommandLineProcessor.Defaults.OUTPUT_FILE_PATH);
            Assert.AreEqual(CommandLineProcessor.REPORT_FILE_PATH, args[2]);
        }

        [TestMethod]
        public void OutputAndReport()
        {
            var args = new[] { "input1", "-o", "output", "-r", "report" };
            var inputs = new List<string>();
            CommandLineProcessor.Process(args, inputs);
            Assert.AreEqual(inputs.Count, 1);
            Assert.AreEqual(inputs[0], args[0]);
            Assert.AreEqual(CommandLineProcessor.OUTPUT_FILE_PATH, args[2]);
            Assert.AreEqual(CommandLineProcessor.REPORT_FILE_PATH, args[4]);
        }

        [TestMethod]
        public void ReportAndOutput()
        {
            var args = new[] { "input1", "-r", "report", "-o", "output" };
            var inputs = new List<string>();
            CommandLineProcessor.Process(args, inputs);
            Assert.AreEqual(inputs.Count, 1);
            Assert.AreEqual(inputs[0], args[0]);
            Assert.AreEqual(CommandLineProcessor.OUTPUT_FILE_PATH, args[4]);
            Assert.AreEqual(CommandLineProcessor.REPORT_FILE_PATH, args[2]);
        }

        [TestMethod]
        public void MultipleInputsWithOutput()
        {
            var args = new[] { "input1", "input2", "-o", "output" };
            var inputs = new List<string>();
            CommandLineProcessor.Process(args, inputs);
            Assert.AreEqual(inputs.Count, 2);
            Assert.AreEqual(inputs[0], args[0]);
            Assert.AreEqual(inputs[1], args[1]);
            Assert.AreEqual(CommandLineProcessor.OUTPUT_FILE_PATH, args[3]);
            Assert.AreEqual(CommandLineProcessor.REPORT_FILE_PATH, CommandLineProcessor.Defaults.REPORT_FILE_PATH);
        }

        [TestMethod]
        public void MultipleInputsAroundOutput()
        {
            var args = new[] { "input1", "-o", "output", "input2" };
            var inputs = new List<string>();
            CommandLineProcessor.Process(args, inputs);
            Assert.AreEqual(inputs.Count, 2);
            Assert.AreEqual(inputs[0], args[0]);
            Assert.AreEqual(inputs[1], args[3]);
            Assert.AreEqual(CommandLineProcessor.OUTPUT_FILE_PATH, args[2]);
            Assert.AreEqual(CommandLineProcessor.REPORT_FILE_PATH, CommandLineProcessor.Defaults.REPORT_FILE_PATH);
        }
    }
}
