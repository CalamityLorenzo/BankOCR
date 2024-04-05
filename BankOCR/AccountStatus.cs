namespace BankOCR
{
    // Basic processed account number and the original digits that define each number.
    public readonly record struct AccountNumber
    {
        public List<string> OcrDigits { get; } = new List<string>();
        public string Number { get; }

        public AccountNumber(string accountNumber, IEnumerable<String> ocrDigits)
        {
            this.OcrDigits = ocrDigits.ToList();
            this.Number = accountNumber;
        }

    }
    // Composite type to record the status of the account number.
    // The ToString is used as the actual output
    public readonly record struct AccountStatus(AccountNumber Accountnumber, bool isLegible, bool isValidChecksum)
    {
        public AccountStatus(AccountNumber Accountnumber) : this(Accountnumber, false, false) { }

        public override string ToString()
        {
            return $"{Accountnumber.Number}{(!isLegible ? " ILL" : !isValidChecksum ? " ERR" : "")}";
        }
    }

}