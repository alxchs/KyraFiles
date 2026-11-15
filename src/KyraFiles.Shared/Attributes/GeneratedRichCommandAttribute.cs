using System;

namespace KyraFiles.Shared.Attributes;

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class GeneratedRichCommandAttribute : Attribute
{
	public Type[]? AssociatedActions { get; set; }
}
