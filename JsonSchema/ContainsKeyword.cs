﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Json.More;
using Json.Pointer;

namespace Json.Schema;

/// <summary>
/// Handles `contains`.
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
[JsonConverter(typeof(ContainsKeywordJsonConverter))]
public class ContainsKeyword : IJsonSchemaKeyword, IRefResolvable, ISchemaContainer, IEquatable<ContainsKeyword>
{
	/// <summary>
	/// The JSON name of the keyword.
	/// </summary>
	public const string Name = "contains";

	/// <summary>
	/// The schema to match.
	/// </summary>
	public JsonSchema Schema { get; }

	/// <summary>
	/// Creates a new <see cref="ContainsKeyword"/>.
	/// </summary>
	/// <param name="value">The schema to match.</param>
	public ContainsKeyword(JsonSchema value)
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
		// verify this logic
		if (schemaValueType != SchemaValueType.Array &&
		    schemaValueType != SchemaValueType.Object)
		{
			context.WrongValueKind(schemaValueType);
			return;
		}

		var validIndices = new List<JsonNode>();
		if (schemaValueType == SchemaValueType.Array)
		{
			var array = (JsonArray)context.LocalInstance!;
			for (int i = 0; i < array.Count; i++)
			{
				context.Push(context.InstanceLocation.Combine(i), array[i] ?? JsonNull.SignalNode,
					context.EvaluationPath.Combine(Name), Schema);
				context.Evaluate();
				if (context.LocalResult.IsValid)
					validIndices.Add(i);
				context.Pop();
			}
		}
		else
		{
			if (context.Options.EvaluatingAs != Draft.Unspecified &&
			    context.Options.EvaluatingAs < Draft.DraftNext)
			{
				context.WrongValueKind(schemaValueType);
				return;
			}
			var obj = (JsonObject)context.LocalInstance!;
			foreach (var kvp in obj)
			{
				context.Push(context.InstanceLocation.Combine(kvp.Key), kvp.Value ?? JsonNull.SignalNode,
					context.EvaluationPath.Combine(Name), Schema);
				context.Evaluate();
				if (context.LocalResult.IsValid)
					validIndices.Add(kvp.Key!);
				context.Pop();
			}
		}

		context.LocalResult.TryGetAnnotation(MinContainsKeyword.Name, out var minContainsAnnotation);
		context.LocalResult.TryGetAnnotation(MaxContainsKeyword.Name, out var maxContainsAnnotation);
		var min = minContainsAnnotation?.GetValue<uint>() ?? 1;
		var max = maxContainsAnnotation?.GetValue<uint>() ?? uint.MaxValue;
		var validCount = validIndices.Count;

		if (validCount < min)
			context.LocalResult.Fail(Name, ErrorMessages.ContainsTooFew, ("received", validCount), ("minimum", min));
		else if (validCount > max)
			context.LocalResult.Fail(Name, ErrorMessages.ContainsTooMany, ("received", validCount), ("maximum", max));
		else
			context.LocalResult.SetAnnotation(Name, JsonSerializer.SerializeToNode(validIndices));
		context.ExitKeyword(Name, context.LocalResult.IsValid);
	}

	public IEnumerable<Requirement> GetRequirements(JsonPointer subschemaPath, Uri baseUri, JsonPointer instanceLocation, EvaluationOptions options)
	{
		IEnumerable<Requirement> GetArrayRequirements(int itemCount)
		{
			for (var i = 0; i < itemCount; i++)
			{
				foreach (var requirement in Schema.GenerateRequirements(baseUri, subschemaPath.Combine(Name), instanceLocation.Combine(i), options))
				{
					yield return requirement;
				}
			}
		}

		IEnumerable<Requirement> GetObjectRequirements(IEnumerable<string> targetProperties)
		{
			foreach (var property in targetProperties)
			{
				foreach (var requirement in Schema.GenerateRequirements(baseUri, subschemaPath.Combine(Name), instanceLocation.Combine(property), options))
				{
					yield return requirement;
				}
			}
		}

		yield return new Requirement(subschemaPath, instanceLocation,
			(node, cache, catalog) =>
			{
				IEnumerable<Requirement> dynamicRequirements;
				Func<KeywordResult, JsonNode?> selector;
				if (node is JsonArray arr)
				{
					dynamicRequirements = GetArrayRequirements(arr.Count);
					selector = x => int.Parse(x.InstanceLocation.Segments.Last().Value);
				}
				else if (node is JsonObject obj)
				{
					dynamicRequirements = GetObjectRequirements(obj.Select(x => x.Key));
					selector = x => x.InstanceLocation.Segments.Last().Value;
				}
				else return null;

				var minContainsAnnotation = cache.GetLocalAnnotation(subschemaPath, MinContainsKeyword.Name)?.GetValue<uint>();
				var minContains = minContainsAnnotation ?? 1;

				var maxContainsAnnotation = cache.GetLocalAnnotation(subschemaPath, MaxContainsKeyword.Name)?.GetValue<uint>();
				var maxContains = maxContainsAnnotation ?? uint.MaxValue;

				dynamicRequirements.Evaluate(cache, catalog);

				var localResults = cache.GetLocalResults(subschemaPath, Name);
				var validChildren = JsonSerializer.SerializeToNode(localResults.Where(x => x.ValidationResult != false).Select(selector))!.AsArray();

				var isValid = minContains <= validChildren.Count && validChildren.Count <= maxContains;
				string? error = null;
				if (validChildren.Count < minContains)
					error = ErrorMessages.ContainsTooFew.ReplaceTokens(("received", validChildren.Count), ("minimum", minContains));
				else if (validChildren.Count > maxContains)
					error = ErrorMessages.ContainsTooMany.ReplaceTokens(("received", validChildren.Count), ("maximum", maxContains));

				return new KeywordResult(Name, subschemaPath, baseUri, instanceLocation)
				{
					ValidationResult = isValid,
					Error = error,
					Annotation = JsonSerializer.SerializeToNode(validChildren)
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
	public bool Equals(ContainsKeyword? other)
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
		return Equals(obj as ContainsKeyword);
	}

	/// <summary>Serves as the default hash function.</summary>
	/// <returns>A hash code for the current object.</returns>
	public override int GetHashCode()
	{
		return Schema.GetHashCode();
	}
}

internal class ContainsKeywordJsonConverter : JsonConverter<ContainsKeyword>
{
	public override ContainsKeyword Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		var schema = JsonSerializer.Deserialize<JsonSchema>(ref reader, options)!;

		return new ContainsKeyword(schema);
	}
	public override void Write(Utf8JsonWriter writer, ContainsKeyword value, JsonSerializerOptions options)
	{
		writer.WritePropertyName(ContainsKeyword.Name);
		JsonSerializer.Serialize(writer, value.Schema, options);
	}
}

public static partial class ErrorMessages
{
	private static string? _containsTooFew;
	private static string? _containsTooMany;

	/// <summary>
	/// Gets or sets the error message for <see cref="ContainsKeyword"/> when there are too few matching items.
	/// </summary>
	/// <remarks>
	///	Available tokens are:
	///   - [[received]] - the number of matching items provided in the JSON instance
	///   - [[minimum]] - the lower limit specified in the schema
	/// </remarks>
	public static string ContainsTooFew
	{
		get => _containsTooFew ?? Get();
		set => _containsTooFew = value;
	}

	/// <summary>
	/// Gets or sets the error message for <see cref="ContainsKeyword"/> when there are too many matching items.
	/// </summary>
	/// <remarks>
	///	Available tokens are:
	///   - [[received]] - the number of matching items provided in the JSON instance
	///   - [[maximum]] - the upper limit specified in the schema
	/// </remarks>
	public static string ContainsTooMany
	{
		get => _containsTooMany ?? Get();
		set => _containsTooMany = value;
	}
}