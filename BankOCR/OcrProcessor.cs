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
        /// <param name="accountDigits"></param>
        /// <returns></returns>
        public string TranslatedAccountNumber(string accountDigits)
        {
            if (String.IsNullOrEmpty(accountDigits)) { throw new ArgumentNullException("Account digits must be provided."); }

            if (accountDigits.Length != 27 * 3) { throw new ArgumentOutOfRangeException("Account digits are the incorrect length"); }

            StringBuilder accountNumber = new StringBuilder();
            for (var x = 0; x < 9; x++)
            {
                var digitKey = string.Create(9, accountDigits, (buffer, value) =>
                {
                    var accountSpan = accountDigits.AsSpan();
                    accountSpan.Slice((x * 3) + 0, 3).CopyTo(buffer);
                    accountSpan.Slice((x * 3) + 27, 3).CopyTo(buffer.Slice(3, 3));
                    accountSpan.Slice((x * 3) + 54, 3).CopyTo(buffer.Slice(6, 3));
                });

                accountNumber.Append(this.LookupNumber(digitKey));
            }

            return accountNumber.ToString();
        }
        
        public void WriteValidateAccounts(IEnumerable<String> accountNumbers, string filename)
        {
            if (string.IsNullOrEmpty(filename))
                throw new ArgumentNullException($"Filename cannot be null");
            var result = new List<AccountStatus>();
            foreach(var accountNumber in accountNumbers)
            {
                result.Add(new(accountNumber, !accountNumber.Contains("?"), AccountChecksum(accountNumber)));
            }
            File.WriteAllLines(filename, result.Select(a=>a.ToString()));
        }
        
        public string ValidateAccount(string accountNumber)
        {
            AccountStatus ac = new(accountNumber, !accountNumber.Contains("?"), AccountChecksum(accountNumber));
            return ac.ToString();
        }


        /// <summary>
        /// Checksum is generated by multiplying its value against the position summ the result and then %11 ==0;
        /// HOwever it's in reverse. eg 1 2 3 4 = Positions 4 3 2 1
        /// </summary>
        /// <param name="accountNumber"></param>
        /// <returns></returns>
        public bool AccountChecksum(string accountNumber)
        {
            var reversedAccount = accountNumber.Reverse().ToArray();
            var result = AccountChecksumAccumulator(reversedAccount, 0);
            return (result% 11) == 0;
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
        private int AccountChecksumAccumulator(char [] accountNumber, int position)
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
    };

}
