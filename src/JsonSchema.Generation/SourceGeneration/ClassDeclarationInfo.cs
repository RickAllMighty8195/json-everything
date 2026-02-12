using System.Collections.Generic;

namespace Json.Schema.Generation.SourceGeneration;

/// <summary>
/// Holds information about the GeneratedJsonSchemas class declaration.
/// </summary>
internal sealed class ClassDeclarationInfo
{
	public bool IsPublic { get; init; }
	public bool IsInternal { get; init; }
	public bool IsStatic { get; init; }
	public bool IsPartial { get; init; }

	/// <summary>
	/// Default declaration: public static partial class GeneratedJsonSchemas
	/// </summary>
	public static ClassDeclarationInfo Default => new()
	{
		IsPublic = true,
		IsStatic = true,
		IsPartial = true
	};

	/// <summary>
	/// Gets the declaration string for the class.
	/// </summary>
	public string GetDeclarationString()
	{
		var parts = new List<string>();

		if (IsPublic)
			parts.Add("public");
		else if (IsInternal)
			parts.Add("internal");
		else
			parts.Add("private");

		if (IsStatic)
			parts.Add("static");

		parts.Add("partial");
		parts.Add("class");
		parts.Add("GeneratedJsonSchemas");

		return string.Join(" ", parts);
	}
}
