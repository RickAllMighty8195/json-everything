using System.Collections.Generic;
using Json.Schema.Generation.Serialization;
using Microsoft.CodeAnalysis;

namespace Json.Schema.Generation.SourceGeneration;

/// <summary>
/// Represents a type that has been analyzed for schema generation.
/// </summary>
internal sealed class TypeInfo
{
	public required INamedTypeSymbol TypeSymbol { get; init; }
	public required string FullyQualifiedName { get; init; }
	public required string SchemaPropertyName { get; init; }
	public required NamingConvention PropertyNaming { get; init; }
	public required PropertyOrder PropertyOrder { get; init; }
	public required TypeKind Kind { get; init; }
	public required bool IsNullable { get; init; }
	public List<PropertyInfo> Properties { get; init; } = new();
	public List<string> EnumValues { get; init; } = new();
	public TypeInfo? ElementType { get; init; }
	public string? XmlDocSummary { get; init; }
	public List<AttributeInfo> TypeAttributes { get; init; } = new();
}

/// <summary>
/// Represents a property or field that will be in the schema.
/// </summary>
internal sealed class PropertyInfo
{
	public required string Name { get; init; }
	public required string SchemaName { get; init; }
	public required ITypeSymbol Type { get; init; }
	public required bool IsRequired { get; init; }
	public required bool IsNullable { get; init; }
	public required bool IsReadOnly { get; init; }
	public required bool IsWriteOnly { get; init; }
	public List<AttributeInfo> Attributes { get; init; } = new();
	public string? XmlDocSummary { get; init; }
}

/// <summary>
/// Represents a supported attribute on a type or property.
/// </summary>
internal sealed class AttributeInfo
{
	public required string AttributeName { get; init; }
	public Dictionary<string, object?> Parameters { get; init; } = new();
	
	/// <summary>
	/// If true, this attribute implements IAttributeHandler and has a static Apply method.
	/// </summary>
	public bool IsCustomEmitter { get; init; }
	
	/// <summary>
	/// The fully qualified name of the attribute class for custom emitters.
	/// </summary>
	public string? AttributeFullName { get; init; }
	
	/// <summary>
	/// The Apply method signature for custom emitters (excluding the builder parameter).
	/// </summary>
	public List<ApplyParameterInfo>? ApplyMethodParameters { get; init; }
}

/// <summary>
/// Represents a parameter in a custom attribute's Apply method.
/// </summary>
internal sealed class ApplyParameterInfo
{
	public required string Name { get; init; }
	public required string TypeName { get; init; }
}

/// <summary>
/// Indicates the kind of type being analyzed.
/// </summary>
internal enum TypeKind
{
	Unknown,
	Boolean,
	Integer,
	Number,
	String,
	DateTime,
	Guid,
	Uri,
	Enum,
	Array,
	Object
}
