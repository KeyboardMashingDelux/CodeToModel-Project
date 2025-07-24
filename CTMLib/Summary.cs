namespace CTMLib {

    [AttributeUsage(AttributeTargets.All)]
    public class Summary : Attribute {

        public string SummaryText { get; }

        public Summary(string SummaryText) {
            this.SummaryText = SummaryText;
        }
    }
}