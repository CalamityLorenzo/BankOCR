using BankOCR;
using System.Formats.Tar;

namespace BankOCRTests
{
    [TestClass]
    public class UserStories
    {
        [TestMethod("User Story 1: Read OCR File")]
        public void ReadFile()
        {
            var fileName = "OcrFile.txt";
            OcrProcessor processor = new OcrProcessor();
            var result = processor.ParseFile(fileName);

            Assert.IsTrue(result.Count == 3);
            Assert.IsTrue(result[0].Length == 27*3);
            Assert.IsTrue(result[1] == " _  _  _  _  _  _  _  _  _ |_ |_ |_ |_ |_ |_ |_ |_ |_  _| _| _| _| _| _| _| _| _|");
        }

        [DataTestMethod]
        [TestMethod("USer Story 1: Read digits into numbers")]
        [DataRow("    _  _     _  _  _  _  _   | _| _||_||_ |_   ||_||_|  ||_  _|  | _||_|  ||_| _|", "123456789")]
        [DataRow(" _  _  _  _  _  _  _  _  _ |_ |_ |_ |_ |_ |_ |_ |_ |_  _| _| _| _| _| _| _| _| _|","555555555")]
        [DataRow(" _  _  _  _  _  _  _  _    | || || || || || || ||_   ||_||_||_||_||_||_||_| _|  |", "000000051")]
        public void ReadOCRDigits(string digitRow, string result)
        {
            OcrProcessor processor = new OcrProcessor();
            var digits = processor.TranslatedAccountNumber(digitRow);

            Assert.IsTrue(digits == result);
        }
    }
}