using System;

namespace iosh {

    public class DocParameter {

        string name;
        string type;
        string description;

        public string Name => name;
        public string Type => type;
        public string Description => description;

        public bool HasName => !string.IsNullOrEmpty (name);
        public bool HasDescription => !string.IsNullOrEmpty (description);

        public DocParameter () {
            type = "Object";
            description = string.Empty;
        }

        public void SetName (string name) => this.name = name;
        public void SetType (string type) => this.type = type;
        public void SetDescription (string description) => this.description = description;
    }
    
}
