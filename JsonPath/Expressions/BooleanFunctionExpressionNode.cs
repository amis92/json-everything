﻿using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using Json.More;

namespace Json.Path.Expressions;

internal class BooleanFunctionExpressionNode : LogicalExpressionNode
{
	public IPathFunctionDefinition Function { get; }
	public ValueExpressionNode[] Parameters { get; }

	public BooleanFunctionExpressionNode(IPathFunctionDefinition function, IEnumerable<ValueExpressionNode> parameters)
	{
		Function = function;
		Parameters = parameters.ToArray();
	}

	public override bool Evaluate(JsonNode? globalParameter, JsonNode? localParameter)
	{
		var parameterValues = Parameters.Select(x =>
		{
			var result = x.Evaluate(globalParameter, localParameter);
			if (result != null) return (NodeList)result;
			return NodeList.Empty;
		});

		var nodeList = Function.Evaluate(parameterValues);

		return nodeList.Count == 1 && nodeList[0].Value.IsEquivalentTo(true);
	}

	public override void BuildString(StringBuilder builder)
	{
		builder.Append(Function.Name);
		builder.Append('(');

		if (Parameters.Any())
		{
			Parameters[0].BuildString(builder);
			for (int i = 1; i < Parameters.Length; i++)
			{
				builder.Append(',');
				Parameters[i].BuildString(builder);
			}
		}

		builder.Append(')');
	}

	public override string ToString()
	{
		return $"{Function.Name}({string.Join(',', (IEnumerable<ValueExpressionNode>)Parameters)})";
	}
}

internal class BooleanFunctionExpressionParser : ILogicalExpressionParser
{
	public bool TryParse(ReadOnlySpan<char> source, ref int index, [NotNullWhen(true)] out LogicalExpressionNode? expression)
	{
		if (!FunctionExpressionParser.TryParseFunction(source, ref index, out var parameters, out var function))
		{
			expression = null;
			return false;
		}

		expression = new BooleanFunctionExpressionNode(function, parameters);
		return true;
	}
}