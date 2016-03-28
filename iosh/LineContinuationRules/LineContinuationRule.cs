using System;

namespace iosh {

	/// <summary>
	/// Line continuation rule.
	/// </summary>
	public abstract class LineContinuationRule {

		/// <summary>
		/// Match the specified line.
		/// </summary>
		/// <param name="line">Line.</param>
		public abstract bool Match (string line);
	}
}

