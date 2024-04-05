using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace BankOCR
{
    public class OcrProcessor
    {
        private Dictionary<string, string> _DigitToNumberDictionary;
        private Dictionary<string, string> _NumberToDigit;

        public OcrProcessor()
        {
            CreateDictionaries();
        }
        /// <summary>
        ///  Simple map and it's inversion to save on computational faffing.
        ///  The digit is the concat string of an entire OCR digit.
        /// </summary>
        private void CreateDictionaries()
        {
            this._DigitToNumberDictionary = new()
        {
            { "     |  |","1"},
            { " _  _||_ ","2"},
            { " _  _| _|","3"},
            { "   |_|  |","4"},
            { " _ |_  _|","5"},
            { " _ |_ |_|","6"},
            { " _   |  |","7"},
            { " _ |_||_|","8"},
            { " _ |_| _|","9"},
            { " _ | ||_|","0"},
        };
            this._NumberToDigit = _DigitToNumberDictionary.ToDictionary(a => a.Value, b => b.Key);
        }

        /// <summary>
        /// This will parse a provided ocr file
        /// And return back 1 entry per account number.
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        public IEnumerable<String> ParseFile(string filename)
        {
            if (String.IsNullOrEmpty(filename)) { throw new ArgumentNullException("filename missing."); }
            // The final list of ocr numbers
            var result = new List<String>();
            using var reader = File.OpenText(filename);

            // This is fairly opaque way of parsing the result.
            // Each account Number is represented as 4 rows of strings.
            // I'm taking each row, and placing that in to an array.
            // once we hit the 4th (and empty) row. The contents of currentAccount are concatenated into a single string.
            var currentAccountNoRows = new string[3];
            var rowIdx = 0;
            // THis should also mirror the size of the result list.
            var accountNumberIdx = 1;
            while (!reader.EndOfStream)
            {
                rowIdx += 1;
                // Note you cannot trim a line, as the digits are whitespace aware eg "  |"
                var line = reader.ReadLine()!;
                if (!(rowIdx % 4 == 0))
                {
                    if (line.Length != 27) { throw new IndexOutOfRangeException($"Line length is incorrect: {line.Length}, \nAccount Entry: {accountNumberIdx}\nRow: {rowIdx}"); }
                    currentAccountNoRows[rowIdx - 1] = line;
                }
                else
                {
                    // discard the empty row
                    if (!String.IsNullOrEmpty(line))
                        throw new ArgumentOutOfRangeException($"Expected blank Spacer row: {line}, \nAccount Entry: {accountNumberIdx}\nRow: {rowIdx}");
                    // pass the complete ocr number and concat all the rows
                    // This is to avoid some complexity in managing three IEnumerbles all at once later on.
                    result.Add(String.Join("", currentAccountNoRows));
                    // Increment the account number idx
                    accountNumberIdx++;
                    // reset our row
                    rowIdx = 0;
                }
            }

            // At this point currentAccountNoRows has values for the final rowaccount number
            result.Add(String.Join("", currentAccountNoRows));

            return result;
        }

        /// <summary>
        ///  Transforms the ocr digits into a machine readable accountnumber string.
        /// Each digit is comprised of 3 portions of the string 0-27-54 3 characters long.
        /// The use of spans is to avoid the multiple allocations of strings (This should yield 1 per key vs 4). 
        /// </summary>
        /// <param name="accountOcr"></param>
        /// <returns></returns>
        public AccountNumber TranslateOcrAccountNumber(string accountOcr)
        {
            if (String.IsNullOrEmpty(accountOcr)) { throw new ArgumentNullException("Account digits must be provided."); }

            if (accountOcr.Length != 27 * 3) { throw new ArgumentOutOfRangeException("Account digits are the incorrect length"); }

            StringBuilder accountNumber = new StringBuilder();
            // we store each original digit along with the account info
            // for error correction purposes.
            var ocrDigits = new List<string>();
            for (var x = 0; x < 9; x++)
            {
                var digitKey = string.Create(9, accountOcr, (buffer, value) =>
                {
                    var accountSpan = accountOcr.AsSpan();
                    accountSpan.Slice((x * 3) + 0, 3).CopyTo(buffer);
                    accountSpan.Slice((x * 3) + 27, 3).CopyTo(buffer.Slice(3, 3));
                    accountSpan.Slice((x * 3) + 54, 3).CopyTo(buffer.Slice(6, 3));
                });
                ocrDigits.Add(digitKey);
                accountNumber.Append(this.LookupNumber(digitKey));
            }

            return new AccountNumber(accountNumber.ToString(), ocrDigits);
        }
        public List<AccountNumber> TranslateOcrAccountNumbers(IEnumerable<string> accountOcrs) => accountOcrs.Select(a => TranslateOcrAccountNumber(a)).ToList();

        public void ValidateAccountsToFile(IEnumerable<AccountNumber> accountNumbers, string filename)
        {
            if (string.IsNullOrEmpty(filename))
                throw new ArgumentNullException($"Filename cannot be null");
            var result = new List<AccountStatus>();
            result.AddRange(ValidateAccounts(accountNumbers));
            File.WriteAllLines(filename, result.Select(a => a.ToString()));
        }

        /// <summary>
        /// For an ILL number ?, pass in the original digit,.
        /// And try and create a new number by changing one piece at a time,
        /// </summary>
        /// <param name="brokenDigit"></param>
        /// <returns></returns>
        public IEnumerable<(string number, string digit)> RepairDigit(string brokenDigit)
        {
            // This will weed out the duplicates for us.
            var result = new HashSet<(string number, string digit)>();
            var top = brokenDigit.Substring(0, 3);
            var middle = brokenDigit.Substring(3, 3);
            var bottom = brokenDigit.Substring(6, 3);

            var topReplace = "   ";
            // Top can only be on or off.
            if (top == "   ")
                topReplace = " _ ";

            var topNumber = this.LookupNumber($"{topReplace}{middle}{bottom}");
            if (topNumber != "?")
                result.Add((topNumber, $"{topReplace}{middle}{bottom}"));

            var midNumbers = DigitCombination(middle)
                .Select(midReplaced => (this.LookupNumber($"{top}{midReplaced}{bottom}"), $"{top}{midReplaced}{bottom}"))
                .Where(a => a.Item1 != "?").ToList();
            var bottomNumbers = DigitCombination(bottom)
                            .Select(bottomReplaced => (this.LookupNumber($"{top}{middle}{bottomReplaced}"), $"{top}{middle}{bottomReplaced}"))
                            .Where(a => a.Item1 != "?").ToList();
            midNumbers.ForEach(mid => result.Add(mid));
            bottomNumbers.ForEach(top => result.Add(top));

            return result;
        }

        // Check to See if the number is illegible or the Checksum value is incorrect.
        public List<AccountStatus> ValidateAccounts(IEnumerable<AccountNumber> accounts)
        {
            var result = new List<AccountStatus>();
            foreach (var accountNumber in accounts)
                result.Add(new(accountNumber, !accountNumber.Number.Contains("?"), ValidAccountChecksum(accountNumber.Number)));
            return result;
        }


        /// <summary>
        /// Checksum is generated by multiplying its value against the position summ the result and then %11 ==0;
        /// HOwever it's in reverse. eg 1 2 3 4 = Positions 4 3 2 1
        /// </summary>
        /// <param name="accountNumber"></param>
        /// <returns></returns>
        public bool ValidAccountChecksum(string accountNumber)
        {
            var reversedAccount = accountNumber.Reverse().ToArray();
            var result = AccountChecksumAccumulator(reversedAccount, 0);
            return (result % 11) == 0;
        }

        // This could have also been a swtich statement.
        private string LookupNumber(string digitKey)
        {
            if (this._DigitToNumberDictionary.ContainsKey(digitKey))
                return this._DigitToNumberDictionary[digitKey];
            else
                return "?";
        }

        // Recursive approach for funsies.
        // Multiplying the value by the posiition, and 
        private int AccountChecksumAccumulator(char[] accountNumber, int position)
        {
            if (position < accountNumber.Length)
            {
                // This is exploting that actual representation of a char number
                // is the same as it's integral value (See an ASCII Chart)
                // Could also have done an implict cast, or int.Parse and cast to a string,or span too.
                int accountDigit = accountNumber[position] - '0';
                var total = accountDigit * (position + 1);
                return total += AccountChecksumAccumulator(accountNumber, position + 1);
            }
            return 0;
        }

        /// <summary>
        /// All the possible combinatins of 1 digit segments (while only altering one at a time)
        /// </summary>
        /// <param name="middle"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        private List<string> DigitCombination(string middle)
        {
            if (middle.Length != 3) throw new ArgumentOutOfRangeException("Length of digit segment in correct");

            return new()
            {
                new string([middle[0] == '|' ? ' ' : '|', middle[1], middle[2]]),
                new string([middle[0], middle[1] == '_' ? ' ' : '_', middle[2]]),
                new string([middle[0], middle[1], middle[2] == '|' ? ' ' : '|']),
                };
        }

        public string RepairAccount(AccountStatus accountStatus)
        {
            if (!accountStatus.isLegible)
            {
                return RepairIllegible(accountStatus);
            }
            else if (!accountStatus.isValidChecksum)
            {
                return RepairChecksum(accountStatus);
            }

            return accountStatus.Accountnumber.Number;
        }

        private string RepairIllegible(AccountStatus account)
        {
            // Iterate and find the illegal char '?'
            // Find the illegible numbers and their positions in the original
            var validChecksums = new List<String>();
            var accountNumChars = account.Accountnumber.Number.ToCharArray();
            for (var x = 0; x < account.Accountnumber.Number.Length; x++)
            {
                if (accountNumChars[x] == '?')
                {
                    var repairedDigits = RepairDigit(account.Accountnumber.OcrDigits[x]);
                    validChecksums.AddRange(RepairedValidation(account, repairedDigits, x));
                }
            }
            return FormatRepairedCheckSum(validChecksums, account.Accountnumber.Number);

        }

        private string RepairChecksum(AccountStatus account)
        {

            // Iterate each digit in the account, run the repair digit on the original ocr source.
            // Then try to validate it.
            // Store each correct validation
            var validChecksums = new List<String>();
            for (var x = 0; x < account.Accountnumber.Number.Length; x++)
            {
                // We can get multiple 'fixes' back from repairing a digit.
                var repairedDigits = this.RepairDigit(account.Accountnumber.OcrDigits[x]);
                validChecksums.AddRange(RepairedValidation(account, repairedDigits, x));
            }
            // return back a string with the details of the results.
            return FormatRepairedCheckSum(validChecksums, account.Accountnumber.Number);
        }
        // FOrmat the output from repairing Accounts to ensure it replicates the kata
        // Note the order clause, this is a subtle indication that userstory4 is reallly mean.
        private string FormatRepairedCheckSum(List<string> validChecksums, string accountNumber)
        {
            if(validChecksums.Count==1) { return validChecksums[0]; }
            else
            {
                return $"{accountNumber} AMB ['{String.Join("', '", validChecksums.Order())}']";
            }
        }

        private IEnumerable<String> RepairedValidation(AccountStatus account, IEnumerable<(string number, string ocrDigi)> repairedDigits, int digitIndex)
        {
            var validChecksums = new List<String>();
            foreach (var digit in repairedDigits)
            {
                var accountNumber = account.Accountnumber.Number.Remove(digitIndex, 1).Insert(digitIndex, digit.number);
                if (this.ValidAccountChecksum(accountNumber))
                {
                    validChecksums.Add(accountNumber);
                }
            }

            return validChecksums;
        }

        public List<String> RepairAccounts(IEnumerable<AccountStatus> accountStatuses)
        {
            List<String> results = new();
            foreach (var status in accountStatuses)
            {
                results.Add(RepairAccount(status));
            }

            return results;
        }
    };

}
