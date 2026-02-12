using System.Collections.Generic;

namespace Json.Schema.Generation.SourceGeneration.Emitters;

/// <summary>
/// Shared registry of schema emitters.
/// </summary>
internal static class SchemaEmitterRegistry
{
	/// <summary>
	/// The shared collection of stateless schema emitters.
	/// </summary>
	internal static readonly IReadOnlyList<ISchemaEmitter> Emitters = new List<ISchemaEmitter>
	{
		new BooleanSchemaEmitter(),
		new IntegerSchemaEmitter(),
		new NumberSchemaEmitter(),
		new StringSchemaEmitter(),
		new DateTimeSchemaEmitter(),
		new GuidSchemaEmitter(),
		new UriSchemaEmitter(),
		new EnumSchemaEmitter(),
		new ArraySchemaEmitter(),
		new ObjectSchemaEmitter(),
	};
}
