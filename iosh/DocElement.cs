using System;
using System.Collections.Generic;

namespace iosh {

    public class DocElement {

        readonly List<string> authors;
        readonly List<DocParameter> parameters;
        readonly List<DocParameter> kwparameters;
        DocParameter returnvalue;
        DocParameter yieldvalue;
        DocParameter varparam;
        string version;
        string description;

        public DocParameter [] KeywordParameters => kwparameters.ToArray ();
        public DocParameter [] Parameters => parameters.ToArray ();
        public DocParameter ReturnValue => returnvalue;
        public DocParameter VariadicValue => varparam;
        public DocParameter YieldValue => yieldvalue;

        public bool HasDescription => !string.IsNullOrEmpty (description);
        public bool HasKeywordParameters => kwparameters.Count > 0;
        public bool HasVersion => !string.IsNullOrEmpty (version);
        public bool HasVariadicArguments => varparam != null;
        public bool HasReturnValue => returnvalue != null;
        public bool HasParameters => parameters.Count > 0;
        public bool HasYieldValue => yieldvalue != null;
        public bool HasAuthors => authors.Count > 0;

        public string [] Authors => authors.ToArray ();
        public string Description => description;
        public string Version => version;

        public DocElement () {
            authors = new List<string> ();
            parameters = new List<DocParameter> ();
            kwparameters = new List<DocParameter> ();
            returnvalue = null;
            yieldvalue = null;
            varparam = null;
            version = string.Empty;
        }

        public void AddAuthor (string author) => authors.Add (author);
        public void AddParameter (DocParameter parameter) => parameters.Add (parameter);
        public void AddKeywordParameter (DocParameter parameter) => kwparameters.Add (parameter);
        public void SetVariadicParameter (DocParameter varparam) => this.varparam = varparam;
        public void SetReturnValue (DocParameter parameter) => returnvalue = parameter;
        public void SetYieldValue (DocParameter parameter) => yieldvalue = parameter;
        public void SetVersion (string version) => this.version = version;
        public void SetDescription (string description) => this.description = description;
    }
}
