using System;
using System.Linq;

namespace iosh {

	/// <summary>
	/// Open bracket line continuation rule.
	/// </summary>
	public class OpenBracketRule : LineContinuationRule {

		/// <summary>
		/// Whether the remainder should be checker
		/// </summary>
		bool check;

		/// <summary>
		/// The remainder.
		/// </summary>
		int remainder;

		/// <summary>
		/// The indentation.
		/// </summary>
		public int indent;

		/// <summary>
		/// Initializes a new instance of the <see cref="iosh.OpenBracketRule"/> class.
		/// </summary>
		public OpenBracketRule () {
			check = false;
			remainder = 0;
		}

		/// <summary>
		/// Match the specified line.
		/// </summary>
		/// <description>
		/// * func something () {
		/// * if (cond) {
		/// * else {
		/// </description>
		/// <param name="line">Line.</param>
		public override bool Match (string line) {
			line = line.Trim ();
			int opencount, closecount;
			var balance = GetBalance (line, out opencount, out closecount);
			remainder += balance;
			indent = remainder * 2;
			if (!check && balance == 0)
				return false;
			if (!check)
				check = true;
			if (check && remainder <= 0) {
				check = false;
				return false;
			}
			return true;
		}

		/// <summary>
		/// Gets the bracket balance.
		/// </summary>
		/// <returns>The balance.</returns>
		/// <param name="line">Line.</param>
		/// <param name="open">Open brackets.</param>
		/// <param name="close">Close brackets.</param>
		static int GetBalance (string line, out int open, out int close) {
			open = line.Count (c => c == '{');
			close = line.Count (c => c == '}');
			return open - close;
		}
	}
}

