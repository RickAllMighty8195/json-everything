using System;
using System.Text.Json;
using NUnit.Framework;
using TestHelpers;

namespace Json.Schema.Tests;

public class BundleTests
{
    [Test]
    public void CreateBundle_IncludesReferencedSchemasAndExcludesIndependent()
    {
        var buildOptions = new BuildOptions { SchemaRegistry = new() };
        var registry = buildOptions.SchemaRegistry;

        var aUri = new Uri("https://json-everything.test/bundle-ref-a");
        var bUri = new Uri("https://json-everything.test/bundle-ref-b");
        var rootUri = new Uri("https://json-everything.test/bundle-root");
        var independentUri = new Uri("https://json-everything.test/bundle-independent");
        var bundleUri = new Uri("https://json-everything.test/bundle");

        _ = new JsonSchemaBuilder(buildOptions)
            .Schema(MetaSchemas.Draft202012Id)
            .Id(aUri)
            .Type(SchemaValueType.String)
            .Build();

        _ = new JsonSchemaBuilder(buildOptions)
            .Schema(MetaSchemas.Draft202012Id)
            .Id(bUri)
            .Type(SchemaValueType.Integer)
            .Build();

        _ = new JsonSchemaBuilder(buildOptions)
            .Schema(MetaSchemas.Draft202012Id)
            .Id(rootUri)
            .Properties(
                ("a", new JsonSchemaBuilder().Ref(aUri)),
                ("b", new JsonSchemaBuilder().Ref(bUri))
            )
            .Build();

        _ = new JsonSchemaBuilder(buildOptions)
            .Schema(MetaSchemas.Draft202012Id)
            .Id(independentUri)
            .Type(SchemaValueType.Object)
            .Build();

        var bundle = registry.CreateBundle(rootUri, bundleUri);

        Assert.That(bundle, Is.Not.Null);

		TestConsole.WriteLine(JsonSerializer.Serialize(bundle, TestEnvironment.TestOutputSerializerOptions));

        var defs = bundle!.Root.Source.GetProperty("$defs");

        Assert.That(defs.TryGetProperty(rootUri.ToString(), out _), Is.True, "Root schema should be in $defs");
        Assert.That(defs.TryGetProperty(aUri.ToString(), out _), Is.True, "Schema A should be in $defs");
        Assert.That(defs.TryGetProperty(bUri.ToString(), out _), Is.True, "Schema B should be in $defs");
        Assert.That(defs.TryGetProperty(independentUri.ToString(), out _), Is.False, "Independent schema should not be in $defs");
    }
}
