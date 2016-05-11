using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Iodine.Runtime;

namespace Future {

    public class NativeInteropClass : IodineClass {

        static readonly IodineTypeDefinition NativeInteropTypeDef = new IodineTypeDefinition ("NativeInterop");

        public NativeInteropClass ()
            : base ("NativeInterop", null, null) {
            SetType (NativeInteropTypeDef);
            SetAttribute ("loadLibrary", new BuiltinMethodCallback (loadLibrary, this));
            SetAttribute ("freeLibrary", new BuiltinMethodCallback (freeLibrary, this));
        }

        IodineObject loadLibrary (VirtualMachine vm, IodineObject self, params IodineObject [] args) {
            var fst = args.FirstOrDefault ();
            var str = fst as IodineString;
            if (fst == default (IodineObject) || str == null) {
                vm.RaiseException ("Expected 1 argument of type: Str");
                return IodineNull.Instance;
            }
            var handle = NativeLoadLibrary (str.Value);
            return new IodineInteger (handle.ToInt64 ());
        }

        IodineObject freeLibrary (VirtualMachine vm, IodineObject self, params IodineObject [] args) {
            var fst = args.FirstOrDefault ();
            var val = fst as IodineInteger;
            if (fst == default (IodineObject) || val == null) {
                vm.RaiseException ("Expected 1 argument of type: Int");
                return IodineNull.Instance;
            }
            var handle = new IntPtr (val.Value);
            var retval = NativeFreeLibrary (handle);
            return new IodineInteger (retval.ToInt64 ());
        }

        [DllImport ("kernel32.dll", EntryPoint = "LoadLibrary")]
        static extern IntPtr NativeLoadLibrary (
                [MarshalAs (UnmanagedType.LPStr)] string lpLibFileName
        );

        [DllImport ("kernel32.dll", EntryPoint = "FreeLibrary")]
        static extern IntPtr NativeFreeLibrary (
            IntPtr hModule
        );

        [DllImport ("kernel32.dll", EntryPoint = "GetProcAddress")]
        static extern IntPtr NativeGetProcAddress (
            IntPtr hModule,
            [MarshalAs (UnmanagedType.LPStr)] string lpProcName
        );

        static Delegate LoadFunction<T> (string dllPath, string functionName) {
            var hModule = NativeLoadLibrary (dllPath);
            if (hModule == IntPtr.Zero)
                throw new ExternalException ($"Failed to load library {Path.GetFileName (dllPath)}");
            var functionAddress = NativeGetProcAddress (hModule, functionName);
            if (functionAddress == IntPtr.Zero)
                throw new ExternalException ($"Failed to get address of function {functionName}");
            return Marshal.GetDelegateForFunctionPointer (functionAddress, typeof (T));
        }

        public class NativeInteropDelegate : IodineObject {

            static readonly IodineTypeDefinition NativeInteropDelegateTypeDef = new IodineTypeDefinition ("NativeInteropDelegate");

            public NativeInteropDelegate ()
            : base (NativeInteropDelegateTypeDef) {
            }
        }
    }
}

