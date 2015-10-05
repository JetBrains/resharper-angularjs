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

using JetBrains.Util.PersistentMap;

namespace JetBrains.ReSharper.Plugins.AngularJS.Feature.Services.Caches
{
    public class Parameter
    {
        public readonly string Name;
        public readonly string Type;
        public readonly bool IsOptional;
        public readonly string Description;

        public Parameter(string name, string type, bool isOptional, string description)
        {
            Name = name;
            Type = type;
            IsOptional = isOptional;
            Description = description;
        }

        public static Parameter Read(UnsafeReader reader)
        {
            var name = reader.ReadString();
            var type = reader.ReadString();
            var isOptional = reader.ReadBoolean();
            var description = reader.ReadString();
            return new Parameter(name, type, isOptional, description);
        }

        public void Write(UnsafeWriter writer)
        {
            writer.Write(Name);
            writer.Write(Type);
            writer.Write(IsOptional);
            writer.Write(Description);
        }
    }
}