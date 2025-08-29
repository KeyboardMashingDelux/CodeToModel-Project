namespace CTMLib {

    /// <summary>
    /// Attribute to identfy a type as containment.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class IsContainment : Attribute {
    }
}
