namespace BankOCR
{
    public class Model
    {
        public string Cap { get; } = " _ ";
        public string CapRight { get; } = " _|";
        public string CapLeft { get; } = "|_";
        public string Bucket { get; } = "|_|";
        public string Left { get; } = "|  ";
        public string Right { get; } = " |";
        public string HoleyBucket{ get; } = "| |";
        public string Empty { get; } = "  ";

    }
}
