namespace CTMLib {

    /// <summary>
    /// Attribute to identfy an <see langword="enum"/> as part of a model.
    /// </summary>
    [AttributeUsage(AttributeTargets.Enum, AllowMultiple = false)]
    public class ModelEnum : Attribute {
    }
}
