﻿using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Json.Pointer;

namespace Json.Schema;

/// <summary>
/// Handles `writeOnly`.
/// </summary>
[SchemaKeyword(Name)]
[SchemaDraft(Draft.Draft201909)]
[SchemaDraft(Draft.Draft202012)]
[SchemaDraft(Draft.DraftNext)]
[Vocabulary(Vocabularies.Metadata201909Id)]
[Vocabulary(Vocabularies.Metadata202012Id)]
[Vocabulary(Vocabularies.MetadataNextId)]
[JsonConverter(typeof(WriteOnlyKeywordJsonConverter))]
public class WriteOnlyKeyword : IJsonSchemaKeyword, IEquatable<WriteOnlyKeyword>
{
	/// <summary>
	/// The JSON name of the keyword.
	/// </summary>
	public const string Name = "writeOnly";

	/// <summary>
	/// Whether the instance is read-only.
	/// </summary>
	public bool Value { get; }

	/// <summary>
	/// Creates a new <see cref="WriteOnlyKeyword"/>.
	/// </summary>
	/// <param name="value">Whether the instance is read-only.</param>
	public WriteOnlyKeyword(bool value)
	{
		Value = value;
	}

	/// <summary>
	/// Performs evaluation for the keyword.
	/// </summary>
	/// <param name="context">Contextual details for the evaluation process.</param>
	public void Evaluate(EvaluationContext context)
	{
		context.EnterKeyword(Name);
		context.LocalResult.SetAnnotation(Name, Value);
		context.ExitKeyword(Name, true);
	}

	public IEnumerable<Requirement> GetRequirements(JsonPointer subschemaPath, Uri baseUri, JsonPointer instanceLocation, EvaluationOptions options)
	{
		yield return new Requirement(subschemaPath, instanceLocation,
			(_, _, _) => new KeywordResult(Name, subschemaPath, baseUri, instanceLocation)
			{
				Annotation = Value
			});
	}

	/// <summary>Indicates whether the current object is equal to another object of the same type.</summary>
	/// <param name="other">An object to compare with this object.</param>
	/// <returns>true if the current object is equal to the <paramref name="other">other</paramref> parameter; otherwise, false.</returns>
	public bool Equals(WriteOnlyKeyword? other)
	{
		if (ReferenceEquals(null, other)) return false;
		if (ReferenceEquals(this, other)) return true;
		return Value == other.Value;
	}

	/// <summary>Determines whether the specified object is equal to the current object.</summary>
	/// <param name="obj">The object to compare with the current object.</param>
	/// <returns>true if the specified object  is equal to the current object; otherwise, false.</returns>
	public override bool Equals(object obj)
	{
		return Equals(obj as WriteOnlyKeyword);
	}

	/// <summary>Serves as the default hash function.</summary>
	/// <returns>A hash code for the current object.</returns>
	public override int GetHashCode()
	{
		return Value.GetHashCode();
	}
}

internal class WriteOnlyKeywordJsonConverter : JsonConverter<WriteOnlyKeyword>
{
	public override WriteOnlyKeyword Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		if (reader.TokenType != JsonTokenType.True && reader.TokenType != JsonTokenType.False)
			throw new JsonException("Expected boolean");

		var str = reader.GetBoolean();

		return new WriteOnlyKeyword(str);
	}
	public override void Write(Utf8JsonWriter writer, WriteOnlyKeyword value, JsonSerializerOptions options)
	{
		writer.WriteBoolean(WriteOnlyKeyword.Name, value.Value);
	}
}