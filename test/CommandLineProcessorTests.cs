using HaveIBeenPwned.AddressExtractor.Objects;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HaveIBeenPwned.AddressExtractor.Tests
{
    [TestClass]
    public class CommandLineProcessorTests
    {
        #region Invalid Input

        [TestMethod]
        public void NoArguments()
        {
            // Arrange
            var args = Array.Empty<string>();

            // Act and Assert
            Assert.ThrowsException<ArgumentException>(() => CommandLineProcessor.Parse(args, out _));
        }

        [TestMethod]
        public void NoInputFiles()
        {
            // Arrange
            var args = new[] { "-o", "output" };

            // Act and Assert
            Assert.ThrowsException<ArgumentException>(() => CommandLineProcessor.Parse(args, out _));
        }

        [TestMethod]
        public void MissingOutputAtEnd()
        {
            // Arrange
            var args = new[] { "input1", "-o" };

            // Act and Assert
            Assert.ThrowsException<ArgumentException>(() => CommandLineProcessor.Parse(args, out _));
        }

        [TestMethod]
        public void MissingReportAtEnd()
        {
            // Arrange
            var args = new[] { "input1", "-r" };

            // Act and Assert
            Assert.ThrowsException<ArgumentException>(() => CommandLineProcessor.Parse(args, out _));
        }

        [TestMethod]
        public void MissingOutput()
        {
            // Arrange
            var args = new[] { "input1", "-o", "-r", "report" };

            // Act and Assert
            Assert.ThrowsException<ArgumentException>(() => CommandLineProcessor.Parse(args, out _));
        }

        [TestMethod]
        public void MissingReport()
        {
            // Arrange
            var args = new[] { "input1", "-r", "-o", "output" };

            // Act and Assert
            Assert.ThrowsException<ArgumentException>(() => CommandLineProcessor.Parse(args, out _));
        }

        [TestMethod]
        public void MissingOption()
        {
            // Arrange
            var args = new[] { "input1", "-" };

            // Act and Assert
            Assert.ThrowsException<ArgumentException>(() => CommandLineProcessor.Parse(args, out _));
        }

        [TestMethod]
        public void InvalidOption()
        {
            // Arrange
            var args = new[] { "input1", "-z" };

            // Act and Assert
            Assert.ThrowsException<ArgumentException>(() => CommandLineProcessor.Parse(args, out _));
        }

        #endregion

        [TestMethod]
        public void Usage()
        {
            var all = new[] { "-?", "-h", "--help" };
            foreach (var option in all)
            {
                // Arrange
                var args = new[] { option };

                // Act and Assert
                var config = CommandLineProcessor.Parse(args, out _);
                // Successful if no exception thrown
            }
        }

        [TestMethod]
        public void UsageWithOtherOptionsBefore()
        {
            // Arrange
            var args = new[] { "input", "-?" };

            // Act and Assert
            Assert.ThrowsException<ArgumentException>(() => CommandLineProcessor.Parse(args, out _));
        }

        [TestMethod]
        public void UsageWithOtherOptionsAfter()
        {
            // Arrange
            var args = new[] { "-?", "input" };

            // Act and Assert
            Assert.ThrowsException<ArgumentException>(() => CommandLineProcessor.Parse(args, out _));
        }

        #region Version flag

        [TestMethod]
        public void Version()
        {
            var all = new[] { "-v", "--version" };
            foreach (var option in all)
            {
                // Arrange
                var args = new[] { option };

                // Act and Assert
                var config = CommandLineProcessor.Parse(args, out _);
                // Successful if no exception thrown
            }
        }

        [TestMethod]
        public void VersionWithOtherOptionsBefore()
        {
            // Arrange
            var args = new[] { "input", "-v" };

            // Act and Assert
            Assert.ThrowsException<ArgumentException>(() => CommandLineProcessor.Parse(args, out _));
        }

        [TestMethod]
        public void VersionWithOtherOptionsAfter()
        {
            // Arrange
            var args = new[] { "-v", "input" };

            // Act and Assert
            Assert.ThrowsException<ArgumentException>(() => CommandLineProcessor.Parse(args, out _));
        }

        #endregion
        #region Config flags

        [TestMethod]
        public void Debug()
        {
            var args = new[] { "input", "--debug" };

            // Act and Assert
            var config = CommandLineProcessor.Parse(args, out _);
            Assert.IsTrue(config.Debug, "Debug mode should be enabled");
        }

        [TestMethod]
        public void SkipPrompt()
        {
            var all = new[] { "-y", "--yes" };
            foreach (var option in all)
            {
                // Arrange
                var args = new[] { "input", option };

                // Act and Assert
                var config = CommandLineProcessor.Parse(args, out _);
                Assert.IsTrue(config.SkipPrompts, "Skipping prompts should be enabled");
            }
        }

        #endregion
        #region File Inputs

        [TestMethod]
        public void OneInput()
        {
            // Arrange
            var args = new[] { "input1" };

            // Act
            var config = CommandLineProcessor.Parse(args, out IList<string> inputs);

            // Assert
            Assert.AreEqual(inputs.Count, 1);
            Assert.AreEqual(inputs[0], args[0]);
            Assert.AreEqual(config.OutputFilePath, Config.Defaults.OUTPUT_FILE_PATH);
            Assert.AreEqual(config.ReportFilePath, Config.Defaults.REPORT_FILE_PATH);
        }

        [TestMethod]
        public void TwoInputs()
        {
            // Arrange
            var args = new[] { "input1", "input2" };

            // Act
            var config = CommandLineProcessor.Parse(args, out IList<string> inputs);

            // Assert
            Assert.AreEqual(inputs.Count, 2);
            Assert.AreEqual(inputs[0], args[0]);
            Assert.AreEqual(inputs[1], args[1]);
            Assert.AreEqual(config.OutputFilePath, Config.Defaults.OUTPUT_FILE_PATH);
            Assert.AreEqual(config.ReportFilePath, Config.Defaults.REPORT_FILE_PATH);
        }

        #endregion

        [TestMethod]
        public void Output()
        {
            // Arrange
            var args = new[] { "input1", "-o", "output" };

            // Act
            var config = CommandLineProcessor.Parse(args, out IList<string> inputs);

            // Assert
            Assert.AreEqual(inputs.Count, 1);
            Assert.AreEqual(inputs[0], args[0]);
            Assert.AreEqual(config.OutputFilePath, args[2]);
            Assert.AreEqual(config.ReportFilePath, Config.Defaults.REPORT_FILE_PATH);
        }

        [TestMethod]
        public void Report()
        {
            // Arrange
            var args = new[] { "input1", "-r", "report" };

            // Act
            var config = CommandLineProcessor.Parse(args, out IList<string> inputs);

            // Assert
            Assert.AreEqual(inputs.Count, 1);
            Assert.AreEqual(inputs[0], args[0]);
            Assert.AreEqual(config.OutputFilePath, Config.Defaults.OUTPUT_FILE_PATH);
            Assert.AreEqual(config.ReportFilePath, args[2]);
        }

        [TestMethod]
        public void OutputAndReport()
        {
            // Arrange
            var args = new[] { "input1", "-o", "output", "-r", "report" };

            // Act
            var config = CommandLineProcessor.Parse(args, out IList<string> inputs);

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

            // Act
            var config = CommandLineProcessor.Parse(args, out IList<string> inputs);

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

            // Act
            var config = CommandLineProcessor.Parse(args, out IList<string> inputs);

            // Assert
            Assert.AreEqual(inputs.Count, 2);
            Assert.AreEqual(inputs[0], args[0]);
            Assert.AreEqual(inputs[1], args[1]);
            Assert.AreEqual(config.OutputFilePath, args[3]);
            Assert.AreEqual(config.ReportFilePath, Config.Defaults.REPORT_FILE_PATH);
        }

        [TestMethod]
        public void MultipleInputsAroundOutput()
        {
            // Arrange
            var args = new[] { "input1", "-o", "output", "input2" };

            // Act
            var config = CommandLineProcessor.Parse(args, out IList<string> inputs);

            // Assert
            Assert.AreEqual(inputs.Count, 2);
            Assert.AreEqual(inputs[0], args[0]);
            Assert.AreEqual(inputs[1], args[3]);
            Assert.AreEqual(config.OutputFilePath, args[2]);
            Assert.AreEqual(config.ReportFilePath, Config.Defaults.REPORT_FILE_PATH);
        }
    }
}
