using System;
using Iodine.Runtime;

namespace Future {

    [IodineBuiltinModule ("future", false)]
    public partial class FutureModule : IodineModule {
        
        public FutureModule () 
            : base ("future") {

            SetAttribute ("NativeInterop", new NativeInteropClass ());
        }
    }
}

