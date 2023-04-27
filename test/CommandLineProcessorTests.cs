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
            // Arrange
            var args = Array.Empty<string>();
            var inputs = new List<string>();

            // Act and Assert
            Assert.ThrowsException<ArgumentException>(() => new CommandLineProcessor(args, inputs));
        }

        [TestMethod]
        public void NoInputFiles()
        {
            // Arrange
            var args = new[] { "-o", "output" };
            var inputs = new List<string>();

            // Act and Assert
            Assert.ThrowsException<ArgumentException>(() => new CommandLineProcessor(args, inputs));
        }

        [TestMethod]
        public void MissingOutputAtEnd()
        {
            // Arrange
            var args = new[] { "input1", "-o" };
            var inputs = new List<string>();

            // Act and Assert
            Assert.ThrowsException<ArgumentException>(() => new CommandLineProcessor(args, inputs));
        }

        [TestMethod]
        public void MissingReportAtEnd()
        {
            // Arrange
            var args = new[] { "input1", "-r" };
            var inputs = new List<string>();

            // Act and Assert
            Assert.ThrowsException<ArgumentException>(() => new CommandLineProcessor(args, inputs));
        }

        [TestMethod]
        public void MissingOutput()
        {
            // Arrange
            var args = new[] { "input1", "-o", "-r", "report" };
            var inputs = new List<string>();

            // Act and Assert
            Assert.ThrowsException<ArgumentException>(() => new CommandLineProcessor(args, inputs));
        }

        [TestMethod]
        public void MissingReport()
        {
            // Arrange
            var args = new[] { "input1", "-r", "-o", "output" };
            var inputs = new List<string>();

            // Act and Assert
            Assert.ThrowsException<ArgumentException>(() => new CommandLineProcessor(args, inputs));
        }

        [TestMethod]
        public void MissingOption()
        {
            // Arrange
            var args = new[] { "input1", "-" };
            var inputs = new List<string>();

            // Act and Assert
            Assert.ThrowsException<ArgumentException>(() => new CommandLineProcessor(args, inputs));
        }

        [TestMethod]
        public void InvalidOption()
        {
            // Arrange
            var args = new[] { "input1", "-z" };
            var inputs = new List<string>();

            // Act and Assert
            Assert.ThrowsException<ArgumentException>(() => new CommandLineProcessor(args, inputs));
        }

        [TestMethod]
        public void Usage()
        {
            // Arrange
            var args = new[] { "-?" };
            var inputs = new List<string>();

            // Act and Assert
            var config = new CommandLineProcessor(args, inputs);
            // Successful if no exception thrown
        }

        [TestMethod]
        public void UsageWithOtherOptionsBefore()
        {
            // Arrange
            var args = new[] { "input", "-?" };
            var inputs = new List<string>();

            // Act and Assert
            Assert.ThrowsException<ArgumentException>(() => new CommandLineProcessor(args, inputs));
        }

        [TestMethod]
        public void UsageWithOtherOptionsAfter()
        {
            // Arrange
            var args = new[] { "-?", "input" };
            var inputs = new List<string>();

            // Act and Assert
            Assert.ThrowsException<ArgumentException>(() => new CommandLineProcessor(args, inputs));
        }

        [TestMethod]
        public void Version()
        {
            // Arrange
            var args = new[] { "-v" };
            var inputs = new List<string>();

            // Act and Assert
            var config = new CommandLineProcessor(args, inputs);
            // Successful if no exception thrown
        }

        [TestMethod]
        public void VersionWithOtherOptionsBefore()
        {
            // Arrange
            var args = new[] { "input", "-v" };
            var inputs = new List<string>();

            // Act and Assert
            Assert.ThrowsException<ArgumentException>(() => new CommandLineProcessor(args, inputs));
        }

        [TestMethod]
        public void VersionWithOtherOptionsAfter()
        {
            // Arrange
            var args = new[] { "-v", "input" };
            var inputs = new List<string>();

            // Act and Assert
            Assert.ThrowsException<ArgumentException>(() => new CommandLineProcessor(args, inputs));
        }

        [TestMethod]
        public void OneInput()
        {
            // Arrange
            var args = new[] { "input1" };
            var inputs = new List<string>();

            // Act
            var config = new CommandLineProcessor(args, inputs);

            // Assert
            Assert.AreEqual(inputs.Count, 1);
            Assert.AreEqual(inputs[0], args[0]);
            Assert.AreEqual(config.OutputFilePath, CommandLineProcessor.Defaults.OUTPUT_FILE_PATH);
            Assert.AreEqual(config.ReportFilePath, CommandLineProcessor.Defaults.REPORT_FILE_PATH);
        }

        [TestMethod]
        public void TwoInputs()
        {
            // Arrange
            var args = new[] { "input1", "input2" };
            var inputs = new List<string>();

            // Act
            var config = new CommandLineProcessor(args, inputs);

            // Assert
            Assert.AreEqual(inputs.Count, 2);
            Assert.AreEqual(inputs[0], args[0]);
            Assert.AreEqual(inputs[1], args[1]);
            Assert.AreEqual(config.OutputFilePath, CommandLineProcessor.Defaults.OUTPUT_FILE_PATH);
            Assert.AreEqual(config.ReportFilePath, CommandLineProcessor.Defaults.REPORT_FILE_PATH);
        }

        [TestMethod]
        public void Output()
        {
            // Arrange
            var args = new[] { "input1", "-o", "output" };
            var inputs = new List<string>();

            // Act
            var config = new CommandLineProcessor(args, inputs);

            // Assert
            Assert.AreEqual(inputs.Count, 1);
            Assert.AreEqual(inputs[0], args[0]);
            Assert.AreEqual(config.OutputFilePath, args[2]);
            Assert.AreEqual(config.ReportFilePath, CommandLineProcessor.Defaults.REPORT_FILE_PATH);
        }

        [TestMethod]
        public void Report()
        {
            // Arrange
            var args = new[] { "input1", "-r", "report" };
            var inputs = new List<string>();

            // Act
            var config = new CommandLineProcessor(args, inputs);

            // Assert
            Assert.AreEqual(inputs.Count, 1);
            Assert.AreEqual(inputs[0], args[0]);
            Assert.AreEqual(config.OutputFilePath, CommandLineProcessor.Defaults.OUTPUT_FILE_PATH);
            Assert.AreEqual(config.ReportFilePath, args[2]);
        }

        [TestMethod]
        public void OutputAndReport()
        {
            // Arrange
            var args = new[] { "input1", "-o", "output", "-r", "report" };
            var inputs = new List<string>();

            // Act
            var config = new CommandLineProcessor(args, inputs);

            // Assert
            Assert.AreEqual(inputs.Count, 1);
            Assert.AreEqual(inputs[0], args[0]);
            Assert.AreEqual(config.OutputFilePath, args[2]);
            Assert.AreEqual(config.ReportFilePath, args[4]);
        }

        [TestMethod]
        public void ReportAndOutput()
        {
            // Arrange
            var args = new[] { "input1", "-r", "report", "-o", "output" };
            var inputs = new List<string>();

            // Act
            var config = new CommandLineProcessor(args, inputs);

            // Assert
            Assert.AreEqual(inputs.Count, 1);
            Assert.AreEqual(inputs[0], args[0]);
            Assert.AreEqual(config.OutputFilePath, args[4]);
            Assert.AreEqual(config.ReportFilePath, args[2]);
        }

        [TestMethod]
        public void MultipleInputsWithOutput()
        {
            // Arrange
            var args = new[] { "input1", "input2", "-o", "output" };
            var inputs = new List<string>();

            // Act
            var config = new CommandLineProcessor(args, inputs);

            // Assert
            Assert.AreEqual(inputs.Count, 2);
            Assert.AreEqual(inputs[0], args[0]);
            Assert.AreEqual(inputs[1], args[1]);
            Assert.AreEqual(config.OutputFilePath, args[3]);
            Assert.AreEqual(config.ReportFilePath, CommandLineProcessor.Defaults.REPORT_FILE_PATH);
        }

        [TestMethod]
        public void MultipleInputsAroundOutput()
        {
            // Arrange
            var args = new[] { "input1", "-o", "output", "input2" };
            var inputs = new List<string>();

            // Act
            var config = new CommandLineProcessor(args, inputs);

            // Assert
            Assert.AreEqual(inputs.Count, 2);
            Assert.AreEqual(inputs[0], args[0]);
            Assert.AreEqual(inputs[1], args[3]);
            Assert.AreEqual(config.OutputFilePath, args[2]);
            Assert.AreEqual(config.ReportFilePath, CommandLineProcessor.Defaults.REPORT_FILE_PATH);
        }
    }
}
