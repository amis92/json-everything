﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Json.More;

namespace Json.Schema;

/// <summary>
/// Handles `uniqueItems`.
/// </summary>
[SchemaKeyword(Name)]
[SchemaSpecVersion(SpecVersion.Draft6)]
[SchemaSpecVersion(SpecVersion.Draft7)]
[SchemaSpecVersion(SpecVersion.Draft201909)]
[SchemaSpecVersion(SpecVersion.Draft202012)]
[SchemaSpecVersion(SpecVersion.DraftNext)]
[Vocabulary(Vocabularies.Validation201909Id)]
[Vocabulary(Vocabularies.Validation202012Id)]
[Vocabulary(Vocabularies.ValidationNextId)]
[JsonConverter(typeof(UniqueItemsKeywordJsonConverter))]
public class UniqueItemsKeyword : IJsonSchemaKeyword, IEquatable<UniqueItemsKeyword>
{
	/// <summary>
	/// The JSON name of the keyword.
	/// </summary>
	public const string Name = "uniqueItems";

	/// <summary>
	/// Whether items should be unique.
	/// </summary>
	public bool Value { get; }

	/// <summary>
	/// Creates a new <see cref="UniqueItemsKeyword"/>.
	/// </summary>
	/// <param name="value">Whether items should be unique.</param>
	public UniqueItemsKeyword(bool value)
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
		var scheamValueType = context.LocalInstance.GetSchemaValueType();
		if (scheamValueType != SchemaValueType.Array)
		{
			context.WrongValueKind(scheamValueType);
			return;
		}

		if (!Value)
		{
			context.ExitKeyword(Name, true);
			return;
		}

		var array = (JsonArray)context.LocalInstance!;
		var duplicates = new List<(int, int)>();
		for (int i = 0; i < array.Count - 1; i++)
			for (int j = i + 1; j < array.Count; j++)
			{
				if (array[i].IsEquivalentTo(array[j]))
					duplicates.Add((i, j));
			}

		if (duplicates.Any())
		{
			var pairs = string.Join(", ", duplicates.Select(d => $"({d.Item1}, {d.Item2})"));
			context.LocalResult.Fail(Name, ErrorMessages.UniqueItems, ("duplicates", pairs));
		}

		context.ExitKeyword(Name, context.LocalResult.IsValid);
	}

	/// <summary>Indicates whether the current object is equal to another object of the same type.</summary>
	/// <param name="other">An object to compare with this object.</param>
	/// <returns>true if the current object is equal to the <paramref name="other">other</paramref> parameter; otherwise, false.</returns>
	public bool Equals(UniqueItemsKeyword? other)
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
		return Equals(obj as UniqueItemsKeyword);
	}

	/// <summary>Serves as the default hash function.</summary>
	/// <returns>A hash code for the current object.</returns>
	public override int GetHashCode()
	{
		return Value.GetHashCode();
	}
}

internal class UniqueItemsKeywordJsonConverter : JsonConverter<UniqueItemsKeyword>
{
	public override UniqueItemsKeyword Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		if (reader.TokenType != JsonTokenType.True && reader.TokenType != JsonTokenType.False)
			throw new JsonException("Expected boolean");

		var number = reader.GetBoolean();

		return new UniqueItemsKeyword(number);
	}
	public override void Write(Utf8JsonWriter writer, UniqueItemsKeyword value, JsonSerializerOptions options)
	{
		writer.WriteBoolean(UniqueItemsKeyword.Name, value.Value);
	}
}

public static partial class ErrorMessages
{
	private static string? _uniqueItems;

	/// <summary>
	/// Gets or sets the error message for <see cref="UniqueItemsKeyword"/>.
	/// </summary>
	/// <remarks>
	///	Available tokens are:
	///   - [[duplicates]] - the indices of duplicate pairs as a comma-delimited list of "(x, y)" items
	/// </remarks>
	public static string UniqueItems
	{
		get => _uniqueItems ?? Get();
		set => _uniqueItems = value;
	}
}