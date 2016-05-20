using System;

namespace iosh {

    [Flags]
    public enum AnalyzerFlags {
        None = 0,
        Enable = 2 << 0,
        Disable = 2 << 1,
        Warnings = 2 << 2,
        Recommendations = 2 << 3,
    }
}

