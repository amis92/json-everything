﻿using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Json.More;
using Json.Pointer;

namespace Json.Schema;

/// <summary>
/// Handles `exclusiveMinimum`.
/// </summary>
[SchemaKeyword(Name)]
[SchemaDraft(Draft.Draft6)]
[SchemaDraft(Draft.Draft7)]
[SchemaDraft(Draft.Draft201909)]
[SchemaDraft(Draft.Draft202012)]
[SchemaDraft(Draft.DraftNext)]
[Vocabulary(Vocabularies.Validation201909Id)]
[Vocabulary(Vocabularies.Validation202012Id)]
[Vocabulary(Vocabularies.ValidationNextId)]
[JsonConverter(typeof(ExclusiveMinimumKeywordJsonConverter))]
public class ExclusiveMinimumKeyword : IJsonSchemaKeyword, IEquatable<ExclusiveMinimumKeyword>
{
	/// <summary>
	/// The JSON name of the keyword.
	/// </summary>
	public const string Name = "exclusiveMinimum";

	/// <summary>
	/// The minimum value.
	/// </summary>
	public decimal Value { get; }

	/// <summary>
	/// Creates a new <see cref="ExclusiveMinimumKeyword"/>.
	/// </summary>
	/// <param name="value">The minimum value.</param>
	public ExclusiveMinimumKeyword(decimal value)
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
		var schemaValueType = context.LocalInstance.GetSchemaValueType();
		if (schemaValueType is not (SchemaValueType.Number or SchemaValueType.Integer))
		{
			context.WrongValueKind(schemaValueType);
			return;
		}

		var number = context.LocalInstance!.AsValue().GetNumber();
		if (!(Value < number))
			context.LocalResult.Fail(Name, ErrorMessages.ExclusiveMinimum, ("received", number), ("limit", Value));
		context.ExitKeyword(Name, context.LocalResult.IsValid);
	}

	public IEnumerable<Requirement> GetRequirements(JsonPointer subschemaPath, Uri baseUri, JsonPointer instanceLocation, EvaluationOptions options)
	{
		yield return new Requirement(subschemaPath, instanceLocation,
			(node, _, _) =>
			{
				if (node.GetSchemaValueType() is not (SchemaValueType.Integer or SchemaValueType.Number)) return null;

				var value = node!.AsValue().GetNumber();
				var isValid = value > Value;

				return new KeywordResult(Name, subschemaPath, baseUri, instanceLocation)
				{
					ValidationResult = isValid,
					Error = isValid ? null : ErrorMessages.ExclusiveMinimum.ReplaceTokens(("received", value), ("limit", Value))
				};
			});
	}

	/// <summary>Indicates whether the current object is equal to another object of the same type.</summary>
	/// <param name="other">An object to compare with this object.</param>
	/// <returns>true if the current object is equal to the <paramref name="other">other</paramref> parameter; otherwise, false.</returns>
	public bool Equals(ExclusiveMinimumKeyword? other)
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
		return Equals(obj as ExclusiveMinimumKeyword);
	}

	/// <summary>Serves as the default hash function.</summary>
	/// <returns>A hash code for the current object.</returns>
	public override int GetHashCode()
	{
		return Value.GetHashCode();
	}
}

internal class ExclusiveMinimumKeywordJsonConverter : JsonConverter<ExclusiveMinimumKeyword>
{
	public override ExclusiveMinimumKeyword Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		if (reader.TokenType != JsonTokenType.Number)
			throw new JsonException("Expected number");

		var number = reader.GetDecimal();

		return new ExclusiveMinimumKeyword(number);
	}
	public override void Write(Utf8JsonWriter writer, ExclusiveMinimumKeyword value, JsonSerializerOptions options)
	{
		writer.WriteNumber(ExclusiveMinimumKeyword.Name, value.Value);
	}
}

public static partial class ErrorMessages
{
	private static string? _exclusiveMinimum;

	/// <summary>
	/// Gets or sets the error message for <see cref="ExclusiveMinimumKeyword"/>.
	/// </summary>
	/// <remarks>
	///	Available tokens are:
	///   - [[received]] - the value provided in the JSON instance
	///   - [[limit]] - the lower limit in the schema
	/// </remarks>
	public static string ExclusiveMinimum
	{
		get => _exclusiveMinimum ?? Get();
		set => _exclusiveMinimum = value;
	}
}