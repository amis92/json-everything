﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Json.Logic.Rules;

/// <summary>
/// Handles the `-` operation.
/// </summary>
[Operator("-")]
[JsonConverter(typeof(SubtractRuleJsonConverter))]
public class SubtractRule : Rule
{
	internal List<Rule> Items;

	internal SubtractRule(Rule a, params Rule[] more)
	{
		Items = new List<Rule> { a };
		Items.AddRange(more);
	}

	/// <summary>
	/// Applies the rule to the input data.
	/// </summary>
	/// <param name="data">The input data.</param>
	/// <param name="contextData">
	///     Optional secondary data.  Used by a few operators to pass a secondary
	///     data context to inner operators.
	/// </param>
	/// <returns>The result of the rule.</returns>
	public override JsonNode? Apply(JsonNode? data, JsonNode? contextData = null)
	{
		if (Items.Count == 0) return 0;

		var value = Items[0].Apply(data, contextData);
		var number = value.Numberify();

		if (number == null)
			throw new JsonLogicException($"Cannot subtract {value.JsonType()}.");

		var result = number.Value;

		if (Items.Count == 1) return -result;

		foreach (var item in Items.Skip(1))
		{
			value = item.Apply(data, contextData);

			number = value.Numberify();

			if (number == null)
				throw new JsonLogicException($"Cannot subtract {value.JsonType()}.");

			result -= number.Value;
		}

		return result;
	}
}

internal class SubtractRuleJsonConverter : JsonConverter<SubtractRule>
{
	public override SubtractRule? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		var parameters = JsonSerializer.Deserialize<Rule[]>(ref reader, options);

		if (parameters == null || parameters.Length == 0)
			throw new JsonException("The - rule needs an array of parameters.");

		return new SubtractRule(parameters[0], parameters.Skip(1).ToArray());
	}

	public override void Write(Utf8JsonWriter writer, SubtractRule value, JsonSerializerOptions options)
	{
		writer.WriteStartObject();
		writer.WritePropertyName("-");
		writer.WriteRules(value.Items, options);
		writer.WriteEndObject();
	}
}