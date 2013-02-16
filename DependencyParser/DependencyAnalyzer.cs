using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mono.Cecil;

namespace DependencyParser {


	/// <summary>
	/// Type analyzer that provides dependencies to other classes
	/// </summary>
	public class DependencyAnalyzer {

		public IEnumerable<TypeReference> FindTypeDependencies(TypeDefinition t)
		{
			var result = new HashSet<TypeReference>(new TypeEqualityComparer());
			FindTypeDependencies(result, t);
			result.Remove(t);
			return result;
		}

		/// <summary>
		/// Remove dependencies to .Net framework
		/// </summary>
		/// <param name="unfilteredDependencies"></param>
		/// <returns></returns>
		public IEnumerable<TypeReference> FilterSystemDependencies(IEnumerable<TypeReference> unfilteredDependencies)
		{
			return from t in unfilteredDependencies
			       where
			       	!(t.Scope.Name.Equals("mscorlib") || t.Scope.Name.StartsWith("System") || t.Scope.Name.StartsWith("Microsoft"))
			       select t;
		}

		public void FindTypeDependencies(ISet<TypeReference> result, TypeDefinition t)
		{
			// ignore generated types
			if ((t.DeclaringType == null && t.Namespace.Equals(string.Empty)) || t.Name.StartsWith("<>")) {
				return;
			}

			foreach (var n in t.NestedTypes) {
				FindTypeDependencies(result, n);
			}

			
			foreach (var c in t.CustomAttributes) {
				AddDependency(result, c.AttributeType);
			}

			if (t.BaseType != null) {
				AddDependency(result, t.BaseType);
			}

			foreach (var i in t.Interfaces) {
				AddDependency(result, i);
			}

			foreach (var e in t.Events) {
				AddDependency(result, e.EventType);
			}

			foreach (var f in t.Fields) {
				AddDependency(result, f.FieldType);
			}

			foreach (var p in t.Properties) {
				AddDependency(result, p.PropertyType);
			}

			foreach (var m in t.Methods) {
				AddDependency(result, m.ReturnType);

				foreach (var p in m.Parameters) {
					AddDependency(result, p.ParameterType);
				}

				foreach (var c in m.CustomAttributes) {
					AddDependency(result, c.AttributeType);
				}

				if (m.Body != null) {
	
					foreach (var v in m.Body.Variables) {
						AddDependency(result, v.VariableType);
					}

					foreach (var e in m.Body.ExceptionHandlers) {
						if (e.CatchType != null) {
							AddDependency(result, e.CatchType);
						}
					}
				}
			}
		}

		private void AddDependency(ISet<TypeReference> result, TypeReference to)
		{
			// ignore generic parameters
			if (to.IsGenericParameter || to.Namespace.Equals(string.Empty)) {
				return;
			}

			if (to.IsArray) {
				to = to.GetElementType();
			}

			if (to.IsGenericInstance) {
				var generic = (GenericInstanceType)to;
				foreach (var a in generic.GenericArguments) {
					result.Add(a);
				}
				to = to.GetElementType();
			}

			result.Add(to);	
		}

	}

	public class TypeEqualityComparer : IEqualityComparer<TypeReference>
	{
		public bool Equals(TypeReference t1, TypeReference t2)
		{
			return t1.FullName == t2.FullName;
		}

		public int GetHashCode(TypeReference t)
		{
			return t.FullName.GetHashCode();
		}
	}
}
