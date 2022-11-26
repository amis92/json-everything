﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Json.Pointer;

namespace Json.Schema;

/// <summary>
/// Handles `propertyNames`.
/// </summary>
[Applicator]
[SchemaPriority(10)]
[SchemaKeyword(Name)]
[SchemaDraft(Draft.Draft6)]
[SchemaDraft(Draft.Draft7)]
[SchemaDraft(Draft.Draft201909)]
[SchemaDraft(Draft.Draft202012)]
[SchemaDraft(Draft.DraftNext)]
[Vocabulary(Vocabularies.Applicator201909Id)]
[Vocabulary(Vocabularies.Applicator202012Id)]
[Vocabulary(Vocabularies.ApplicatorNextId)]
[JsonConverter(typeof(PropertyNamesKeywordJsonConverter))]
public class PropertyNamesKeyword : IJsonSchemaKeyword, IRefResolvable, ISchemaContainer, IEquatable<PropertyNamesKeyword>
{
	/// <summary>
	/// The JSON name of the keyword.
	/// </summary>
	public const string Name = "propertyNames";

	/// <summary>
	/// The schema to match.
	/// </summary>
	public JsonSchema Schema { get; }

	/// <summary>
	/// Creates a new <see cref="PropertyNamesKeyword"/>.
	/// </summary>
	/// <param name="value">The schema to match.</param>
	public PropertyNamesKeyword(JsonSchema value)
	{
		Schema = value ?? throw new ArgumentNullException(nameof(value));
	}

	/// <summary>
	/// Performs evaluation for the keyword.
	/// </summary>
	/// <param name="context">Contextual details for the evaluation process.</param>
	public void Evaluate(EvaluationContext context)
	{
		context.EnterKeyword(Name);
		var schemaValueType = context.LocalInstance.GetSchemaValueType();
		if (schemaValueType != SchemaValueType.Object)
		{
			context.WrongValueKind(schemaValueType);
			return;
		}

		var obj = (JsonObject)context.LocalInstance!;
		context.Options.LogIndentLevel++;
		var overallResult = true;
		foreach (var name in obj.Select(p => p.Key))
		{
			context.Log(() => $"Evaluating property name '{name}'.");
			context.Push(context.InstanceLocation.Combine(name), name,
				context.EvaluationPath.Combine(name), Schema);
			context.Evaluate();
			overallResult &= context.LocalResult.IsValid;
			context.Log(() => $"Property name '{name}' {context.LocalResult.IsValid.GetValidityString()}.");
			context.Pop();
			if (!overallResult && context.ApplyOptimizations) break;
		}
		context.Options.LogIndentLevel--;

		if (!overallResult)
			context.LocalResult.Fail();
		context.ExitKeyword(Name, context.LocalResult.IsValid);
	}

	public IEnumerable<Requirement> GetRequirements(JsonPointer subschemaPath, DynamicScope scope, JsonPointer instanceLocation, EvaluationOptions options)
	{
		IEnumerable<(string Key, Requirement Requirement)> GetDynamicRequirements(IEnumerable<string> properties)
		{
			foreach (var property in properties)
			{
				foreach (var requirement in Schema.GenerateRequirements(scope, subschemaPath.Combine(Name), instanceLocation, options))
				{
					yield return (property, requirement);
				}
			}
		}

		yield return new Requirement(subschemaPath, instanceLocation,
			(node, cache, catalog) =>
			{
				if (node is not JsonObject obj) return null!;

				var propertyNames = obj.Select(x => x.Key);

				var dynamicRequirements = GetDynamicRequirements(propertyNames);
				var relevantResults = new List<KeywordResult>();
				foreach (var check in dynamicRequirements)
				{
					var localResult = check.Requirement.Evaluate(check.Key, cache, catalog);
					if (localResult == null) continue;

					cache.Add(localResult);
					relevantResults.Add(localResult);
				}

				return new KeywordResult(Name, subschemaPath, scope.LocalScope, instanceLocation)
				{
					ValidationResult = relevantResults.All(x => x.ValidationResult != false)
					// TODO: add error message
				};
			}, 10);
	}

	void IRefResolvable.RegisterSubschemas(SchemaRegistry registry, Uri currentUri)
	{
		Schema.RegisterSubschemas(registry, currentUri);
	}

	/// <summary>Indicates whether the current object is equal to another object of the same type.</summary>
	/// <param name="other">An object to compare with this object.</param>
	/// <returns>true if the current object is equal to the <paramref name="other">other</paramref> parameter; otherwise, false.</returns>
	public bool Equals(PropertyNamesKeyword? other)
	{
		if (ReferenceEquals(null, other)) return false;
		if (ReferenceEquals(this, other)) return true;
		return Equals(Schema, other.Schema);
	}

	/// <summary>Determines whether the specified object is equal to the current object.</summary>
	/// <param name="obj">The object to compare with the current object.</param>
	/// <returns>true if the specified object  is equal to the current object; otherwise, false.</returns>
	public override bool Equals(object obj)
	{
		return Equals(obj as PropertyNamesKeyword);
	}

	/// <summary>Serves as the default hash function.</summary>
	/// <returns>A hash code for the current object.</returns>
	public override int GetHashCode()
	{
		return Schema.GetHashCode();
	}
}

internal class PropertyNamesKeywordJsonConverter : JsonConverter<PropertyNamesKeyword>
{
	public override PropertyNamesKeyword Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		var schema = JsonSerializer.Deserialize<JsonSchema>(ref reader, options)!;

		return new PropertyNamesKeyword(schema);
	}
	public override void Write(Utf8JsonWriter writer, PropertyNamesKeyword value, JsonSerializerOptions options)
	{
		writer.WritePropertyName(PropertyNamesKeyword.Name);
		JsonSerializer.Serialize(writer, value.Schema, options);
	}
}