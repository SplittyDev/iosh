using System;

namespace iosh {

    [AttributeUsage (AttributeTargets.Field)]
    public class Stable : Attribute { }

    [AttributeUsage (AttributeTargets.Field)]
    public class Untested : Attribute { }
    
    public static class Std {
        
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

        [Untested]
        public const string Itertools = "std.itertools";

        [Untested]
        public const string Math = "std.math";

        [Stable]
        public const string Tupletools = "std.tupletools";

        public static class Crypto {
            
            [Stable]
            public const string Whirlpool = "std.crypto.whirlpool";
        }
    }
}

