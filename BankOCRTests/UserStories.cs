using BankOCR;
using System.Runtime.CompilerServices;

namespace BankOCRTests
{
    [TestClass]
    public class UserStories
    {

        [TestMethod]
        public void UserStory1()
        {
            string[] accountNumbers = ["000000000", "111111111", "222222222", "333333333", "444444444", "555555555", "666666666", "777777777","888888888", "999999999", "123456789"];

            string fileName = "UserStory1.txt";
            OcrProcessor processor = new OcrProcessor();
            var accountOcrDigits = processor.ParseFile(fileName).ToList();
            var accounts = processor.TranslateOcrAccountNumbers(accountOcrDigits);
            for(var x =0;x<accountNumbers.Length;x++)
            {
                Assert.IsTrue(accountNumbers[x] == accounts[x].Number);
            }

        }

            [DataTestMethod]
        [DataRow("711111111", true)]
        [DataRow("123456789", true)]
        [DataRow("490867715", true)]
        [DataRow("888888888", false)]
        [DataRow("490067715", false)]
        [DataRow("012345678", false)]
        public void UserStory2(string input, bool isValid)
        {
            //string fileName = "UserStory3.txt";
            OcrProcessor processor = new OcrProcessor();
            Assert.IsTrue(processor.ValidAccountChecksum(input) == isValid);
        }
        [DataTestMethod]
        [DataRow(" _  _  _  _  _  _  _  _    | || || || || || || ||_   ||_||_||_||_||_||_||_| _|  |", "000000051")]
        [DataRow("    _  _  _  _  _  _     _ |_||_|| || ||_   |  |  | _   | _||_||_||_|  |  |  | _|", "49006771? ILL")]
        [DataRow("    _  _     _  _  _  _  _   | _| _||_| _ |_   ||_||_|  ||_  _|  | _||_|  ||_| _ ", "1234?678? ILL")]
        public void UserStory3_ValidAccount(string input, string result)
        {
            //string fileName = "UserStory3.txt";
            OcrProcessor processor = new OcrProcessor();
            //var accountOcrDigits = processor.ParseFile(fileName).ToList();
            var accounts = processor.TranslateOcrAccountNumbers(new List<String>() { input });
            var validatedAccounts = processor.ValidateAccounts(accounts);
            Assert.IsTrue(validatedAccounts[0].ToString() == result);
        }


        [DataTestMethod]
        public void UserStory3_SaveResults()
        {
            var inputFilename = "UserStory3.txt";
            var outputFilename = "UserStory3_output.txt";
            OcrProcessor processor = new OcrProcessor();
            var accountOcrDigits = processor.ParseFile(inputFilename);
            var accounts = processor.TranslateOcrAccountNumbers(accountOcrDigits);
            //var validatedAccounts = processor.ValidateAccounts(accounts);
            processor.ValidateAccountsToFile(accounts, outputFilename);

            var allEntries = File.ReadAllLines(outputFilename);
            Assert.IsTrue(allEntries.Length == 3);
            Assert.IsTrue(allEntries[0] == "000000051");
            Assert.IsTrue(allEntries[1] == "49006771? ILL");
            Assert.IsTrue(allEntries[2] == "1234?678? ILL");
        }

        [DataTestMethod]
        public void UserStory4()
        {
            string fileName = "UserStory4.txt";
            OcrProcessor processor = new OcrProcessor();
            var accountOcrDigits = processor.ParseFile(fileName).ToList();
            var accounts = processor.TranslateOcrAccountNumbers(accountOcrDigits);
            var validatedAccounts = processor.ValidateAccounts(accounts);

            var results = processor.RepairAccounts(validatedAccounts);
            Assert.IsTrue(results[0] == "711111111");
            Assert.IsTrue(results[1] == "777777177");
            Assert.IsTrue(results[2] == "200800000");
            Assert.IsTrue(results[3] == "333393333");
            Assert.IsTrue(results[4] == "888888888 AMB ['888886888', '888888880', '888888988']");
            Assert.IsTrue(results[5] == "555555555 AMB ['555655555', '559555555']");
            Assert.IsTrue(results[6] == "666666666 AMB ['666566666', '686666666']");
            Assert.IsTrue(results[7] == "999999999 AMB ['899999999', '993999999', '999959999']");
            Assert.IsTrue(results[8] == "490067715 AMB ['490067115', '490067719', '490867715']");
            Assert.IsTrue(results[9] == "123456789");
            Assert.IsTrue(results[10] == "000000051");
            Assert.IsTrue(results[11] == "490867715");
        }
    }
}