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
            var result = processor.ParseFile(fileName).ToList();

            Assert.IsTrue(result.Count == 14);
            Assert.IsTrue(result[0].Length == 27 * 3);
            Assert.IsTrue(result[1] == " _  _  _  _  _  _  _  _  _ |_ |_ |_ |_ |_ |_ |_ |_ |_  _| _| _| _| _| _| _| _| _|");
        }

        [DataTestMethod]
        [TestMethod("USer Story 1: Read digits into numbers")]
        [DataRow("    _  _     _  _  _  _  _   | _| _||_||_ |_   ||_||_|  ||_  _|  | _||_|  ||_| _|", "123456789")]
        [DataRow(" _  _  _  _  _  _  _  _  _ |_ |_ |_ |_ |_ |_ |_ |_ |_  _| _| _| _| _| _| _| _| _|", "555555555")]
        [DataRow(" _  _  _  _  _  _  _  _    | || || || || || || ||_   ||_||_||_||_||_||_||_| _|  |", "000000051")]
        public void ReadOCRDigits(string digitRow, string result)
        {
            OcrProcessor processor = new OcrProcessor();
            var digits = processor.TranslatedAccountNumber(digitRow);

            Assert.IsTrue(digits == result);
        }
        [DataTestMethod]
        [DataRow("345882865", true)]
        [DataRow("000000302", true)]
        [DataRow("145882865", false)]
        [DataRow("711111111", true)]
        [DataRow("123456789", true)]
        [DataRow("490867715", true)]
        [DataRow("888888888", false)]
        [DataRow("490067715", false)]
        [DataRow("012345678", false)]
        public void TestChecksum(string accountNumber, bool isSuccess)
        {
            OcrProcessor processor = new OcrProcessor();
            var result = processor.AccountChecksum(accountNumber);
            Assert.IsTrue(isSuccess == result);
        }


        [DataTestMethod]
        [DataRow("345882865", "345882865")]
        [DataRow("000000302", "000000302")]
        [DataRow("145882865", "145882865 ERR")]
        [DataRow("711111111", "711111111")]
        [DataRow("123456789", "123456789")]
        [DataRow("49086??15", "49086??15 ILL")]
        [DataRow("88888?888", "88888?888 ILL")]
        [DataRow("490067715", "490067715 ERR")]
        [DataRow("012345678", "012345678 ERR")]
        public void WritevalidatedAccountInfo(string accountNumber, string expected)
        {
            OcrProcessor processor = new OcrProcessor();
            var result = processor.ValidateAccount(accountNumber)!;
            Assert.IsTrue(result== expected);
        }

    }
}