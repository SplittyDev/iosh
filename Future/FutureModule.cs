using System;
using Iodine.Runtime;

namespace Future {

    [IodineBuiltinModule ("future")]
    public class FutureModule : IodineModule {
        
        public FutureModule () 
            : base ("future") {

            SetAttribute ("NativeInterop", NativeInteropClass.TypeDefinition);
        }
    }
}

