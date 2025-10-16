namespace MLYSO.Web.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    public sealed class SkipAuditAttribute : Attribute { }
}
