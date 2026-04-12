using System.Text;

namespace Json.Schema.Generation.SourceGeneration.Emitters;

internal class AnySchemaEmitter : ISchemaEmitter
{
	public bool Handles(TypeInfo type) => type.Kind == TypeKind.Any;

	public void EmitSchema(StringBuilder sb, TypeInfo type, string indent, SchemaEmissionContext context)
	{
		// Intentionally empty: no constraints means true schema.
	}
}
