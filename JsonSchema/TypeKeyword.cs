﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Json.More;
using Json.Pointer;

namespace Json.Schema;

/// <summary>
/// Handles `type`.
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
[JsonConverter(typeof(TypeKeywordJsonConverter))]
public class TypeKeyword : IJsonSchemaKeyword, IEquatable<TypeKeyword>
{
	/// <summary>
	/// The JSON name of the keyword.
	/// </summary>
	public const string Name = "type";

	/// <summary>
	/// The expected type.
	/// </summary>
	public SchemaValueType Type { get; }

	/// <summary>
	/// Creates a new <see cref="TypeKeyword"/>.
	/// </summary>
	/// <param name="type">The expected type.</param>
	public TypeKeyword(SchemaValueType type)
	{
		Type = type;
	}

	/// <summary>
	/// Creates a new <see cref="TypeKeyword"/>.
	/// </summary>
	/// <param name="types">The expected types.</param>
	public TypeKeyword(params SchemaValueType[] types)
	{
		// TODO: protect input

		Type = types.Aggregate((x, y) => x | y);
	}

	/// <summary>
	/// Creates a new <see cref="TypeKeyword"/>.
	/// </summary>
	/// <param name="types">The expected types.</param>
	public TypeKeyword(IEnumerable<SchemaValueType> types)
	{
		// TODO: protect input

		Type = types.Aggregate((x, y) => x | y);
	}

	/// <summary>
	/// Performs evaluation for the keyword.
	/// </summary>
	/// <param name="context">Contextual details for the evaluation process.</param>
	public void Evaluate(EvaluationContext context)
	{
		context.EnterKeyword(Name);
		bool isValid;
		var schemaValueType = context.LocalInstance.GetSchemaValueType();
		switch (schemaValueType)
		{
			case SchemaValueType.Object:
				isValid = Type.HasFlag(SchemaValueType.Object);
				break;
			case SchemaValueType.Array:
				isValid = Type.HasFlag(SchemaValueType.Array);
				break;
			case SchemaValueType.String:
				isValid = Type.HasFlag(SchemaValueType.String);
				break;
			case SchemaValueType.Integer:
				isValid = Type.HasFlag(SchemaValueType.Integer) || Type.HasFlag(SchemaValueType.Number);
				break;
			case SchemaValueType.Number:
				if (Type.HasFlag(SchemaValueType.Number))
					isValid = true;
				else if (Type.HasFlag(SchemaValueType.Integer))
				{
					var number = context.LocalInstance!.AsValue().GetNumber();
					isValid = number == Math.Truncate(number!.Value);
				}
				else
					isValid = false;
				break;
			case SchemaValueType.Boolean:
				isValid = Type.HasFlag(SchemaValueType.Boolean);
				break;
			case SchemaValueType.Null:
				isValid = Type.HasFlag(SchemaValueType.Null);
				break;
			default:
				throw new ArgumentOutOfRangeException();
		}
		var expected = Type.ToString().ToLower();
		if (!isValid)
			context.LocalResult.Fail(Name, ErrorMessages.Type, ("received", schemaValueType), ("expected", expected));
		context.ExitKeyword(Name, context.LocalResult.IsValid);
	}

	public IEnumerable<IRequirement> GetRequirements(JsonPointer evaluationPath, Uri baseUri, JsonPointer instanceLocation)
	{
		bool IsInteger(JsonNode? node)
		{
			if (Type.HasFlag(SchemaValueType.Number)) return true;
			
			if (Type.HasFlag(SchemaValueType.Integer))
			{
				var number = node.AsValue().GetNumber();
				return number == Math.Truncate(number!.Value);
			}

			return false;
		}

		return new[]
		{
			new Requirement(evaluationPath, baseUri, instanceLocation,
				(node, _) =>
			{
				var schemaValueType = node.GetSchemaValueType();
				return new KeywordResult
				{
					EvaluationPath = evaluationPath,
					InstanceLocation = instanceLocation,
					ValidationResult = schemaValueType switch
					{
						SchemaValueType.Object => Type.HasFlag(SchemaValueType.Object),
						SchemaValueType.Array => Type.HasFlag(SchemaValueType.Object),
						SchemaValueType.Boolean => Type.HasFlag(SchemaValueType.Object),
						SchemaValueType.String => Type.HasFlag(SchemaValueType.Object),
						SchemaValueType.Number => IsInteger(node),
						SchemaValueType.Integer => Type.HasFlag(SchemaValueType.Integer) || Type.HasFlag(SchemaValueType.Number),
						SchemaValueType.Null => Type.HasFlag(SchemaValueType.Object),
						_ => throw new ArgumentOutOfRangeException()
					}
				};
			})
		};
	}

	/// <summary>Indicates whether the current object is equal to another object of the same type.</summary>
	/// <param name="other">An object to compare with this object.</param>
	/// <returns>true if the current object is equal to the <paramref name="other">other</paramref> parameter; otherwise, false.</returns>
	public bool Equals(TypeKeyword? other)
	{
		if (ReferenceEquals(null, other)) return false;
		if (ReferenceEquals(this, other)) return true;
		return Type == other.Type;
	}

	/// <summary>Determines whether the specified object is equal to the current object.</summary>
	/// <param name="obj">The object to compare with the current object.</param>
	/// <returns>true if the specified object  is equal to the current object; otherwise, false.</returns>
	public override bool Equals(object obj)
	{
		return Equals(obj as TypeKeyword);
	}

	/// <summary>Serves as the default hash function.</summary>
	/// <returns>A hash code for the current object.</returns>
	public override int GetHashCode()
	{
		return (int)Type;
	}
}

internal class TypeKeywordJsonConverter : JsonConverter<TypeKeyword>
{
	public override TypeKeyword Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		var type = JsonSerializer.Deserialize<SchemaValueType>(ref reader, options);

		return new TypeKeyword(type);
	}
	public override void Write(Utf8JsonWriter writer, TypeKeyword value, JsonSerializerOptions options)
	{
		writer.WritePropertyName(TypeKeyword.Name);
		JsonSerializer.Serialize(writer, value.Type, options);
	}
}

public static partial class ErrorMessages
{
	private static string? _type;

	/// <summary>
	/// Gets or sets the error message for <see cref="TypeKeyword"/>.
	/// </summary>
	/// <remarks>
	///	Available tokens are:
	///   - [[received]] - the type of value provided in the JSON instance
	///   - [[expected]] - the type(s) required by the schema
	/// </remarks>
	public static string Type
	{
		get => _type ?? Get();
		set => _type = value;
	}
}