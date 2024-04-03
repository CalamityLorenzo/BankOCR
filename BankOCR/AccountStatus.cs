namespace BankOCR
{
    public readonly record struct AccountStatus(String Accountnumer, bool isLegible, bool isValidChecksum)
    {
        public AccountStatus(string Accountnumer) : this(Accountnumer, false, false) { }

        public override string ToString()
        {
            return $"{Accountnumer}{(!isLegible ? " ILL" : !isValidChecksum ? " ERR" : "")}";
        }
    }

}