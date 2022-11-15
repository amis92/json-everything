﻿using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Json.Pointer;

namespace Json.Schema;

/// <summary>
/// Handles `if`.
/// </summary>
[Applicator]
[SchemaKeyword(Name)]
[SchemaDraft(Draft.Draft7)]
[SchemaDraft(Draft.Draft201909)]
[SchemaDraft(Draft.Draft202012)]
[SchemaDraft(Draft.DraftNext)]
[Vocabulary(Vocabularies.Applicator201909Id)]
[Vocabulary(Vocabularies.Applicator202012Id)]
[Vocabulary(Vocabularies.ApplicatorNextId)]
[JsonConverter(typeof(IfKeywordJsonConverter))]
public class IfKeyword : IJsonSchemaKeyword, IRefResolvable, ISchemaContainer, IEquatable<IfKeyword>
{
	/// <summary>
	/// The JSON name of the keyword.
	/// </summary>
	public const string Name = "if";

	/// <summary>
	/// The schema to match.
	/// </summary>
	public JsonSchema Schema { get; }

	/// <summary>
	/// Creates a new <see cref="IfKeyword"/>.
	/// </summary>
	/// <param name="value">The schema to match.</param>
	public IfKeyword(JsonSchema value)
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
		context.Push(context.EvaluationPath.Combine(Name), Schema);
		context.Evaluate();
		var valid = context.LocalResult.IsValid;
		context.Pop();
		context.LocalResult.SetAnnotation(Name, valid);
		context.ExitKeyword(Name, true);
	}

	public IEnumerable<IRequirement> GetRequirements(JsonPointer evaluationPath, Uri baseUri, JsonPointer instanceLocation)
	{
		throw new NotImplementedException();
	}

	void IRefResolvable.RegisterSubschemas(SchemaRegistry registry, Uri currentUri)
	{
		Schema.RegisterSubschemas(registry, currentUri);
	}

	/// <summary>Indicates whether the current object is equal to another object of the same type.</summary>
	/// <param name="other">An object to compare with this object.</param>
	/// <returns>true if the current object is equal to the <paramref name="other">other</paramref> parameter; otherwise, false.</returns>
	public bool Equals(IfKeyword? other)
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
		return Equals(obj as IfKeyword);
	}

	/// <summary>Serves as the default hash function.</summary>
	/// <returns>A hash code for the current object.</returns>
	public override int GetHashCode()
	{
		return Schema.GetHashCode();
	}
}

internal class IfKeywordJsonConverter : JsonConverter<IfKeyword>
{
	public override IfKeyword Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		var schema = JsonSerializer.Deserialize<JsonSchema>(ref reader, options)!;

		return new IfKeyword(schema);
	}
	public override void Write(Utf8JsonWriter writer, IfKeyword value, JsonSerializerOptions options)
	{
		writer.WritePropertyName(IfKeyword.Name);
		JsonSerializer.Serialize(writer, value.Schema, options);
	}
}