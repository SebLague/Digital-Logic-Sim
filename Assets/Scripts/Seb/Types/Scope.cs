using System.Collections.Generic;

namespace Seb.Types
{
	public class Scope<T> where T : new()
	{
		readonly Pool<T> pool = new();
		readonly Stack<T> stack = new();
		T current;

		public bool IsInsideScope => stack.Count > 0;
		public T CurrentScope => current;

		public T CreateScope()
		{
			T scope = pool.GetNextAvailableOrCreate();
			stack.Push(scope);
			current = scope;
			return scope;
		}

		public void ExitScope()
		{
			pool.Return(stack.Pop());
			stack.TryPeek(out current);
		}

		public bool TryGetCurrentScope(out T scope)
		{
			scope = current;
			return stack.Count > 0;
		}
	}
}