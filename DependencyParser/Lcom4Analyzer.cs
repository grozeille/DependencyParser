using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Gendarme.Framework.Rocks;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace DependencyParser {
	

	/// <summary>
	/// LCOM4 analyzer.
	/// See following page for detailed explanations on LCOM4: 
	/// http://www.aivosto.com/project/help/pm-oo-cohesion.html#LCOM4
	/// </summary>
	public class Lcom4Analyzer {

		private IEnumerable<string> ignorableFieldNames = new string[] {};

		public IEnumerable<string> IgnorableFieldNames
		{
			get
			{
				return ignorableFieldNames;
			}
			set
			{
				ignorableFieldNames = from n in value select n.ToLowerInvariant();
			}
		}

		public HashSet<HashSet<MemberReference>> FindLcomBlocks(TypeDefinition t)
		{
			var memberBlocks = new Dictionary<MemberReference, HashSet<MemberReference>>();
			foreach (var method in t.Methods)
			{
				if (NeedToBeFiltered(t, method) || IsEmpty(method))
				{
					continue;
				}

				HashSet<MemberReference> currentBlock = null;
				if (memberBlocks.ContainsKey(method))
				{
					currentBlock = memberBlocks[method];
				} else
				{
					currentBlock = new HashSet<MemberReference>();
					currentBlock.Add(method);
					memberBlocks[method] = currentBlock;
				}

				if (method.HasBody)
				{
					foreach (Instruction inst in method.Body.Instructions)
					{
						MemberReference mr = null;

						switch (inst.OpCode.OperandType)
						{
							case OperandType.InlineField:

								FieldReference fr = inst.Operand as FieldReference;
								if (fr==null || IgnorableFieldNames.Contains(fr.Name.ToLowerInvariant())) {
									break;
								}
								FieldDefinition fd = fr as FieldDefinition;
								if (fd == null)
								{
									fd = ((FieldReference) inst.Operand).Resolve();
								}
								if (null != fd && (!fd.IsGeneratedCode() || method.IsSetter || method.IsGetter))
								{
									mr = fd;
								}
								break;
							case OperandType.InlineMethod:
								// special case for automatic properties since the 'backing' fields won't be used
								MethodDefinition md = inst.Operand as MethodDefinition;
								if (md != null && !NeedToBeFiltered(t, md))
								{
									mr = md;
								}
								break;
						}
						if (mr != null && !currentBlock.Contains(mr))
						{

							if (memberBlocks.ContainsKey(mr))
							{
								memberBlocks[mr].UnionWith(currentBlock);
								currentBlock = memberBlocks[mr];
							}
							else
							{
								currentBlock.Add(mr);
								memberBlocks[mr] = currentBlock;
							}
						}
					}
				}
				memberBlocks[method] = currentBlock;

			}
			return CleanBlocks(MergeBlocks(memberBlocks.Values));
		}

		private bool NeedToBeFiltered(TypeDefinition t, MethodDefinition method)
		{
			return (!t.AllSuperTypes().Contains(method.DeclaringType)
				   || method.IsStatic
			       || (method.IsGeneratedCode() && !method.IsSetter && !method.IsGetter)
			       || method.IsConstructor
			       || (method.IsSpecialName && !method.IsSetter && !method.IsGetter)
			       || "Dispose" == method.Name
			       || "ToString" == method.Name
				   || "Equals" == method.Name
				   || "GetHashCode" == method.Name);

		}

		private HashSet<HashSet<MemberReference>> MergeBlocks(IEnumerable<HashSet<MemberReference>> memberBlocks)
		{
			var inputBlocks = new HashSet<HashSet<MemberReference>>(memberBlocks);
			var blocks = new HashSet<HashSet<MemberReference>>();
			while (inputBlocks.Count > 0)
			{
				var block = inputBlocks.First();
				inputBlocks.Remove(block);
				var blocksToMerge = new HashSet<HashSet<MemberReference>>();
				foreach (var mergeCandidate in inputBlocks)
				{
					if (CanBeMerged(block, mergeCandidate))
					{
						blocksToMerge.Add(mergeCandidate);
					}
				}
				if (blocksToMerge.Count==0)
				{
					blocks.Add(block);	
				}
				else
				{
					foreach (var blockToMerge in blocksToMerge)
					{
						inputBlocks.Remove(blockToMerge);
						block.UnionWith(blockToMerge);
					}
					inputBlocks.Add(block);
				} 
			}
			return blocks;
		}

		private bool CanBeMerged(HashSet<MemberReference> block, HashSet<MemberReference> mergeCandidate)
		{
			var intersection = block.Intersect(mergeCandidate);
			return intersection.Count() > 0;
		}

		private HashSet<HashSet<MemberReference>> CleanBlocks(HashSet<HashSet<MemberReference>> blocks)
		{
			foreach (var block in blocks)
			{
				block.RemoveWhere(ShouldBeRemoved);
			}

			blocks.RemoveWhere(b => b.Count(member => member is MethodDefinition) == 0);
			return blocks;
		}

		private bool ShouldBeRemoved(MemberReference reference)
		{
			var method = reference as MethodDefinition;
			return method!=null && (!method.HasBody || method.IsGeneratedCode());
		}

		private bool IsEmpty(MethodDefinition method)
		{
			return !(method.HasBody && method.Body.Instructions.Count(inst => inst.OpCode.Code != Code.Ret && inst.OpCode.Code != Code.Nop) > 0);
		}
	}
}
