using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace iosh {
    
    public static class DocParser {

        public static DocElement Parse (string docstring) {
            var element = new DocElement ();
            var lines = docstring.Split ('\n');
            var blockdesc = new StringBuilder ();
            foreach (var rawline in lines) {
                var line = rawline.Trim (' ', '\r', '\t');
                if (line.StartsWith ("@yields", StringComparison.Ordinal)
                    || line.StartsWith ("@returns", StringComparison.Ordinal)
                    || line.StartsWith ("@variadic", StringComparison.Ordinal)) {
                    var match = Regex.Match (line, @"
                    (?<pre>@[a-z]+)\s+
                    (?<type>
                        (?:[a-z_](?:[a-z_]|\d)+)
                        (?:\|(?:[a-z_](?:[a-z_]|\d)+))*
                    )\s+
                    (?<description>.*)$", RegexOptions.IgnorePatternWhitespace | RegexOptions.IgnoreCase);
                    if (!match.Groups ["type"].Success) {
                        Console.WriteLine ($"Malformed {match.Groups ["pre"].Value} in Iododoc.");
                        continue;
                    }
                    var obj = new DocParameter ();
                    if (match.Groups ["type"].Success)
                        obj.SetType (match.Groups ["type"].Value.Trim ());
                    if (match.Groups ["description"].Success)
                        obj.SetDescription (match.Groups ["description"].Value.Trim ());
                    switch (match.Groups ["pre"].Value.Trim ()) {
                    case "@variadic":
                        element.SetVariadicParameter (obj);
                        break;
                    case "@yields":
                        element.SetYieldValue (obj);
                        break;
                    case "@returns":
                        element.SetReturnValue (obj);
                        break;
                    }
                } else if (line.StartsWith ("@param", StringComparison.Ordinal)
                    || line.StartsWith ("@kwparam", StringComparison.Ordinal)) {
                    var match = Regex.Match (line, @"
                    (?<pre>@[a-z]+)\s+
                    (?<name>
                        (?:\*{1,2})?[a-z_](?:[a-z_]|\d)+
                    )\s+
                    (?::\s+
                    (?<type>
                        (?:[a-z_](?:[a-z_]|\d)+)
                        (?:\|(?:[a-z_](?:[a-z_]|\d)+))*
                    )\s+)?
                    (?<description>.*)$", RegexOptions.IgnorePatternWhitespace | RegexOptions.IgnoreCase);
                    if (!match.Groups ["name"].Success) {
                        Console.WriteLine ($"Malformed {match.Groups ["pre"].Value} in Iododoc.");
                        continue;
                    }
                    var obj = new DocParameter ();
                    obj.SetName (match.Groups ["name"].Value.Trim ());
                    if (match.Groups ["type"].Success)
                        obj.SetType (match.Groups ["type"].Value.Trim ());
                    if (match.Groups ["description"].Success)
                        obj.SetDescription (match.Groups ["description"].Value.Trim ());
                    switch (match.Groups ["pre"].Value.Trim ()) {
                    case "@param":
                        element.AddParameter (obj);
                        break;
                    case "@kwparam":
                        element.AddKeywordParameter (obj);
                        break;
                    }
                } else if (line.StartsWith ("@version", StringComparison.Ordinal)
                           || line.StartsWith ("@author", StringComparison.Ordinal)) {
                    var match = Regex.Match (line, "(?<pre>@[a-z]+)\\s+(?<content>.*)$");
                    if (!match.Success) {
                        Console.WriteLine ($"Malformed {match.Groups ["pre"].Value} in Iododoc.");
                        continue;
                    }
                    var content = match.Groups ["content"].Value.Trim ();
                    if (match.Groups ["pre"].Value == "version") {
                        element.SetVersion (content);
                        continue;
                    }
                    element.AddAuthor (content);
                } else {
                    blockdesc.AppendLine (line);
                }
            }
            element.SetDescription (blockdesc.ToString ());
            return element;
        }
    }
}

