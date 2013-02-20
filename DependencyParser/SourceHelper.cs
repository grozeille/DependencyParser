using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace DependencyParser {
	
	public static class SourceHelper {

		public static string GetSourcePath(this TypeDefinition self)
		{
			var firstInstruction = ExtractFirst(self);
			string result = null;
			if (firstInstruction!=null)
			{
				result = firstInstruction.SequencePoint.Document.Url;
			}
			return result;
		}

		/// <summary>
		/// Copy/pasted from gendarme Symbols class
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		private static Instruction ExtractFirst(TypeDefinition type)
		{
			if (type == null)
				return null;
			foreach (MethodDefinition method in type.Methods) {
				Instruction ins = ExtractFirst(method);
				if (ins != null)
					return ins;
			}
			return null;
		}

		/// <summary>
		/// Copy/pasted from gendarme Symbols class
		/// </summary>
		/// <param name="method"></param>
		/// <returns></returns>
		private static Instruction ExtractFirst(MethodDefinition method)
		{
			if ((method == null) || !method.HasBody || method.Body.Instructions.Count == 0)
				return null;
			Instruction ins = method.Body.Instructions[0];
			// note that the first instruction often does not have a sequence point
			while (ins != null && ins.SequencePoint == null)
				ins = ins.Next;

			return (ins != null && ins.SequencePoint != null) ? ins : null;
		}
	}
}
