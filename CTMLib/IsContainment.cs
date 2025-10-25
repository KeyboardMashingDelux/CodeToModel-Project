namespace CTMLib {

    /// <summary>
    /// Attribute to identfy a type as containment.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class IsContainment : Attribute {
    }
}
