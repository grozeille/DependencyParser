using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Gendarme.Framework.Rocks;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace DependencyParser {

	/// <summary>
	/// Compute RFC measures
	/// </summary>
	public class ResponseForClassAnalyzer {

		public int ComputeRFC(TypeDefinition t)
		{
			int rfc = 0;
			var methodCalls = new HashSet<MethodReference>();
			foreach (var method in t.Methods)
			{
				if (NeedToBeFiltered(t, method))
				{
					continue;
				}

				if (method.HasBody)
				{
					if (!methodCalls.Contains(method))
					{
						rfc++;
						methodCalls.Add(method);
					}
					
					foreach (Instruction inst in method.Body.Instructions)
					{
						if (inst.OpCode.OperandType == OperandType.InlineMethod)
						{
							var call = inst.Operand as MethodReference;
							if (call != null && !NeedToBeFiltered(t, call) && !methodCalls.Contains(call))
							{
								rfc++;
								methodCalls.Add(call);
							}
						}
					}
				}
			}

			// decrement because we do not take in account
			// call to System.Object.ctor
			if (rfc!=0)
			{
				rfc--;	
			}
			return rfc;
		}

		private bool NeedToBeFiltered(TypeDefinition typeDefinition, MethodReference method)
		{
			try
			{
				return method.IsGeneratedCode();
			} catch(AssemblyResolutionException)
			{
				// third party library not found
				// we assume the code is not generated
				return false;
			}
		}
	}
}
