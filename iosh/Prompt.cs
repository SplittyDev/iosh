using System;
using System.Collections.Generic;

namespace iosh {

	/// <summary>
	/// Prompt.
	/// </summary>
	public class Prompt {

		/// <summary>
		/// The stack.
		/// </summary>
		readonly Stack<string> stack;

		/// <summary>
		/// The current prompt.
		/// </summary>
		string currentPrompt;

		/// <summary>
		/// Gets the length.
		/// </summary>
		/// <value>The length.</value>
		public int Length {
			get { return ToString ().Length; }
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="iosh.Prompt"/> class.
		/// </summary>
		public Prompt () {
			currentPrompt = string.Empty;
			stack = new Stack<string> ();
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="iosh.Prompt"/> class.
		/// </summary>
		/// <param name="prompt">Prompt.</param>
		public Prompt (string prompt) : this () {
			currentPrompt = prompt;
		}

		/// <summary>
		/// Push the specified prompt.
		/// </summary>
		/// <param name="prompt">Prompt.</param>
		public void Push (string prompt) {
			stack.Push (currentPrompt);
			currentPrompt = prompt;
		}

		/// <summary>
		/// Restore the last prompt.
		/// </summary>
		public void Pop () {
			currentPrompt = stack.Pop ();
		}

		/// <summary>
		/// Print the current prompt.
		/// </summary>
		public void Print () {
			if (currentPrompt != string.Empty)
				Console.Write (this);
		}

		/// <summary>
		/// Returns a <see cref="System.String"/> that represents the current <see cref="iosh.Prompt"/>.
		/// </summary>
		/// <returns>A <see cref="System.String"/> that represents the current <see cref="iosh.Prompt"/>.</returns>
		public override string ToString () {
			if (currentPrompt != string.Empty)
				return string.Format ("{0} ", currentPrompt);
			return currentPrompt;
		}
	}
}

