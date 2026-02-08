using System.Text;

namespace Json.Schema.Generation.SourceGeneration.Emitters;

internal interface ISchemaEmitter
{
	bool Handles(TypeInfo type);
	void EmitSchema(StringBuilder sb, TypeInfo type, string indent, SchemaEmissionContext context);
}
