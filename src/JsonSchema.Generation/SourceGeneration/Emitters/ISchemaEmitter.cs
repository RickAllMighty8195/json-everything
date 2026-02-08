using System.Text;

namespace Json.Schema.Generation.SourceGeneration.Emitters;

/// <summary>
/// Defines a schema code emitter for a specific type kind.
/// </summary>
/// <remarks>
/// Emitters follow a handler pattern similar to <see cref="Generators.ISchemaGenerator"/>,
/// but for source generation instead of runtime reflection.
/// </remarks>
internal interface ISchemaEmitter
{
	/// <summary>
	/// Determines whether this emitter can handle the given type.
	/// </summary>
	/// <param name="type">The type information.</param>
	/// <returns><c>true</c> if the emitter can handle this type; otherwise, <c>false</c>.</returns>
	bool Handles(TypeInfo type);

	/// <summary>
	/// Emits the schema builder code for the type.
	/// </summary>
	/// <param name="sb">The string builder to append code to.</param>
	/// <param name="type">The type information.</param>
	/// <param name="indent">The indentation string.</param>
	void EmitSchema(StringBuilder sb, TypeInfo type, string indent);
}
