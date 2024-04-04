namespace BankOCR
{
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

    public readonly record struct AccountStatus(AccountNumber Accountnumber, bool isLegible, bool isValidChecksum)
    {
        public AccountStatus(AccountNumber Accountnumber) : this(Accountnumber, false, false) { }

        public override string ToString()
        {
            return $"{Accountnumber.Number}{(!isLegible ? " ILL" : !isValidChecksum ? " ERR" : "")}";
        }
    }

}