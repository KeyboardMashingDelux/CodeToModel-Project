namespace CTMLib {

    [AttributeUsage(AttributeTargets.All)]
    public class Remarks : Attribute {

        public string RemarksText { get; }

        public Remarks(string RemarksText) {
            this.RemarksText = RemarksText;
        }
    }
}