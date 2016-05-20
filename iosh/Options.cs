using System;
using Codeaddicts.libArgument;

namespace iosh {
	
	public class Options {

		[Switch ("--enable-syntax-coloring", "/enable-syntax-coloring")]
		public bool EnableSyntaxHighlighting;

		[Argument ("--lib", "/lib")]
		public string[] IncludeFolders;

        [Switch ("--nostdlib", "/nostlib")]
        public bool NoStdlib;

        readonly public static Options Default;

        static Options () {
            Default = new Options ();
        }
	}
}

