#region license
// Copyright 2015 JetBrains s.r.o.
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
// http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Util.PersistentMap;

namespace JetBrains.ReSharper.Plugins.AngularJS.Feature.Services.Caches
{
    public class Directive
    {
        public const string AnyTagName = "ANY";

        public readonly string OriginalName;
        public readonly string Name;
        public readonly string Restrictions;
        public readonly string[] Tags;
        public readonly int Offset;
        public readonly IList<Parameter> Parameters;

        public Directive(string originalName, string name, string restrictions, string[] tags, int offset, IList<Parameter> parameters)
        {
            OriginalName = originalName;
            Name = name;
            Restrictions = restrictions;
            Tags = tags;
            Offset = offset;
            Parameters = parameters;

            IsAttribute = restrictions.Contains('A');
            IsElement = restrictions.Contains('E');
            IsClass = restrictions.Contains('C');
        }

        public bool IsAttribute { get; private set; }
        public bool IsElement { get; private set; }
        public bool IsClass { get; private set; }

        public bool IsForTag(string tagName)
        {
            foreach (var t in Tags)
            {
                if (t.Equals(AnyTagName, StringComparison.InvariantCultureIgnoreCase) ||
                    t.Equals(tagName, StringComparison.InvariantCultureIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }

        public bool IsForTagSpecific(string tagName)
        {
            return Tags.Any(t => t.Equals(tagName, StringComparison.InvariantCultureIgnoreCase));
        }

        public bool IsForAnyTag()
        {
            foreach (var t in Tags)
            {
                if (t.Equals(AnyTagName, StringComparison.InvariantCultureIgnoreCase))
                    return true;
            }
            return false;
        }

        public void Write(UnsafeWriter writer)
        {
            writer.Write(OriginalName);
            writer.Write(Name);
            writer.Write(Restrictions);
            writer.Write(UnsafeWriter.StringDelegate, Tags);
            writer.Write(Offset);
            writer.Write<Parameter, ICollection<Parameter>>((w, parameter) => parameter.Write(w), Parameters.ToList());
        }

        public static Directive Read(UnsafeReader reader)
        {
            var originalName = reader.ReadString();
            var name = reader.ReadString();
            var restrictions = reader.ReadString();
            var tags = reader.ReadArray(UnsafeReader.StringDelegate);
            var offset = reader.ReadInt();
            var parameters = reader.ReadCollection(Parameter.Read, count => new List<Parameter>(count));
            return new Directive(originalName, name, restrictions, tags, offset, parameters);
        }
    }
}