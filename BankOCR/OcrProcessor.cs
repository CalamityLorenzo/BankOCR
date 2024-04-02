using System.Text;

namespace BankOCR
{
    public class OcrProcessor
    {

        /// <summary>
        /// This will parse a provided ocr file
        /// And return back 1 entry per account number.
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        public List<String> ParseFile(string filename)
        {

            var result = new List<String>();
            using var reader = File.OpenText(filename);

            // This is fairly opaque
            // Each account Number is represented as 4 rows of strings.
            // I'm taking each row, and placing that in to an array.
            // once we hit the 4th (and empty) row. The contents of currentAccount are concatenated into a single string.
            var currentAccountNoRows = new string[3];
            var rowIdx = 0;
            // THis should also mirror the size of the result list.
            var accountNumberIdx = 0;
            while (!reader.EndOfStream)
            {
                rowIdx += 1;
                // The ping is to ensure a fail fast.
                // Note you cannot trim a line, as the digits are "right leaning" eg "  |"
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
                    // pass the complete number
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

        // Transforms the ocr digits into a readable string.
        // Each digit is comprised of 3 portions of the string 0-27-54 3 characters long.
        // This is a dictionary entry
        public string TranslatedAccountNumber(string accountDigits)
        {
            StringBuilder accountNumber = new StringBuilder();
            for (var x = 0; x < 9; x++)
            {
                var digitKey = string.Create(9, accountDigits, (buffer, value) =>
                {
                    var accountSpan = accountDigits.AsSpan();
                    accountSpan.Slice(0+(x*3), 3).CopyTo(buffer);
                    accountSpan.Slice((x * 3) + 27, 3).CopyTo(buffer.Slice(3,3));
                    accountSpan.Slice((x * 3) + 54, 3).CopyTo(buffer.Slice(6, 3));
                });

                accountNumber.Append(this.GetNumber(digitKey));
            }

            return accountNumber.ToString();
        }

        private string GetNumber(string digitKey)
        {
            if (this.ocrDigitDictionary.ContainsKey(digitKey))
                return this.ocrDigitDictionary[digitKey];
            else
                return "?";
        }

        private Dictionary<string, string> ocrDigitDictionary = new()
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

    };

}
