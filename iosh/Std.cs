using System;

namespace iosh {

    [AttributeUsage (AttributeTargets.Field)]
    public class Stable : Attribute { }

    [AttributeUsage (AttributeTargets.Field)]
    public class Untested : Attribute { }
    
    public static class Std {

        public static readonly string [] AllModules = {
            Argparse,
            Builtin,
            Base64,
            Collections,
            Exceptions,
            Fastmath,
            Functools,
            Ints,
            Itertools,
            Math,
            Reflection,
            Tupletools,
            Types,
        };
        
        [Untested]
        public const string Argparse = "std.argparse";

        [Stable]
        public const string Builtin = "std.builtin";

        [Stable]
        public const string Base64 = "std.base64";

        [Untested]
        public const string Collections = "std.collections";

        [Stable]
        public const string Exceptions = "std.exceptions";

        [Stable]
        public const string Fastmath = "std.fastmath";

        [Stable]
        public const string Functools = "std.functools";

        [Stable]
        public const string Ints = "std.ints";

        [Untested]
        public const string Itertools = "std.itertools";

        [Untested]
        public const string Math = "std.math";

        [Untested]
        public const string Reflection = "std.reflection";

        [Stable]
        public const string Tupletools = "std.tupletools";

        [Stable]
        public const string Types = "std.types";

        public static class Crypto {

            [Stable]
            public const string RC4 = "std.crypto.rc4";
            
            [Stable]
            public const string Whirlpool = "std.crypto.whirlpool";
        }
    }
}

