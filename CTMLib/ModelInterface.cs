namespace CTMLib {

    /// <summary>
    /// Attribute to identfy an <see langword="interface"/> as part of a model.
    /// </summary>
    [AttributeUsage(AttributeTargets.Interface, AllowMultiple = false)]
    public class ModelInterface : Attribute {
    }
}
