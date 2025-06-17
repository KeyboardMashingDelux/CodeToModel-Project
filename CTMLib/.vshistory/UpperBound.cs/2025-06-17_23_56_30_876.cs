namespace CTMLib {

    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class UpperBound(int value) : NumberAttribute(value) {
    }
}
