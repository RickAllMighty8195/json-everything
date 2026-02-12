namespace Json.Schema.Generation.Serialization;

/// <summary>
/// Indicates the naming convention to use for property names in generated schemas.
/// </summary>
public enum NamingConvention
{
	/// <summary>
	/// Properties are generated with the name as declared in code.
	/// </summary>
	AsDeclared,
	/// <summary>
	/// Property names are converted to camelCase (e.g. `camelCase`).
	/// </summary>
	CamelCase,
	/// <summary>
	/// Property names are converted to PascalCase (e.g. `PascalCase`).
	/// </summary>
	PascalCase,
	/// <summary>
	/// Property names are converted to lower_snake_case (e.g. `lower_snake_case`).
	/// </summary>
	LowerSnakeCase,
	/// <summary>
	/// Property names are converted to UPPER_SNAKE_CASE (e.g. `UPPER_SNAKE_CASE`).
	/// </summary>
	UpperSnakeCase,
	/// <summary>
	/// Property names are converted to kebab-case (e.g. `kebab-case`).
	/// </summary>
	KebabCase,
	/// <summary>
	/// Property names are converted to UPPER-KEBAB-CASE (e.g. `UPPER-KEBAB-CASE`).
	/// </summary>
	UpperKebabCase
}
