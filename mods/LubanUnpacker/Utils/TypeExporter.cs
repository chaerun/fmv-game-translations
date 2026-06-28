using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace LubanUnpacker.Utils
{
    public static class TypeExporter
    {
        private static HashSet<Type> _processedTypes = new HashSet<Type>();

        public static void ExportToFile(Type inputType)
        {
            if (inputType == null) return;

            // Prevent infinite loops (e.g., if Class A references Class B, and Class B references Class A)
            if (!_processedTypes.Add(inputType)) return;

            string className = inputType.Name;

            var members = inputType.GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                                   .Where(m => m.MemberType == MemberTypes.Field || m.MemberType == MemberTypes.Property)
                                   .Where(m => !Attribute.IsDefined(m, typeof(CompilerGeneratedAttribute)))
                                   .Where(m => !m.Name.Contains("<"))
                                   .OrderBy(m => m.MetadataToken)
                                   .ToList();

            // 2. DEPENDENCY HUNTER: Check all variables for custom types and generate them first
            foreach (var member in members)
            {
                Type memberType = GetMemberType(member);
                ExtractAndGenerateDependencies(memberType, inputType.Assembly);
            }

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("using System;");
            sb.AppendLine("using System.Collections.Generic;");
            sb.AppendLine($"using {inputType.Namespace};");
            sb.AppendLine("using Luban;");
            sb.AppendLine();
            sb.AppendLine("namespace LubanUnpacker.Models");
            sb.AppendLine("{");

            // 3. STRUCT VS CLASS DETECTION
            string typeDeclaration = inputType.IsValueType && !inputType.IsEnum ? "struct" : "sealed class";
            sb.AppendLine($"    public {typeDeclaration} {className} : ILubanModel");
            sb.AppendLine("    {");

            // --- INJECT VARIABLES ---
            foreach (var member in members)
            {
                Type type = GetMemberType(member);
                sb.AppendLine($"        public {CleanTypeName(type)} {member.Name} {{ get; set; }}");
            }
            sb.AppendLine();

            // --- INJECT EMPTY CONSTRUCTOR ---
            sb.AppendLine($"        public {className}() {{ }}");
            sb.AppendLine();

            // --- AUTO-GENERATE READ LOGIC ---
            sb.AppendLine($"        public {className}(ByteBuf _buf)");
            sb.AppendLine("        {");
            foreach (var member in members)
            {
                Type type = GetMemberType(member);
                sb.AppendLine(GenerateReadLogic(type, member.Name));
            }
            sb.AppendLine("        }");
            sb.AppendLine();

            // --- AUTO-GENERATE WRITE LOGIC ---
            sb.AppendLine("        public void Serialize(ByteBuf _buf)");
            sb.AppendLine("        {");
            foreach (var member in members)
            {
                Type type = GetMemberType(member);
                sb.AppendLine(GenerateWriteLogic(type, member.Name));
            }
            sb.AppendLine("        }");

            sb.AppendLine("    }");
            sb.AppendLine("}");

            // --- SAVE FILE ---
            Directory.CreateDirectory("Models/Generated");
            string outputPath = Path.Combine("Models/Generated", $"{className}.cs");
            File.WriteAllText(outputPath, sb.ToString());

            Console.WriteLine($"[SUCCESS] Generated Model: {className} ({typeDeclaration})");
        }

        // ==========================================
        // DEPENDENCY EXTRACTOR
        // ==========================================

        private static void ExtractAndGenerateDependencies(Type? type, Assembly targetAssembly)
        {
            // 2. Add an early exit safety check
            if (type == null) return;

            // If it's a generic (like List<vector2>), dig into the arguments
            if (type.IsGenericType)
            {
                foreach (Type arg in type.GetGenericArguments())
                {
                    ExtractAndGenerateDependencies(arg, targetAssembly);
                }
                return;
            }

            // If it's an array, dig into the element type
            if (type.IsArray)
            {
                // The compiler is now happy because this method safely handles nulls!
                ExtractAndGenerateDependencies(type.GetElementType(), targetAssembly);
                return;
            }

            // If the type belongs to the game's assembly and is NOT an enum, it's a custom model!
            if (type.Assembly == targetAssembly && !type.IsEnum && !type.IsPrimitive)
            {
                ExportToFile(type); // RECURSION: Generate this file right now!
            }
        }

        // ==========================================
        // GENERATOR HELPERS
        // ==========================================

        private static Type GetMemberType(MemberInfo member)
        {
            if (member is FieldInfo f) return f.FieldType;
            if (member is PropertyInfo p) return p.PropertyType;
            return typeof(object);
        }

        private static string GenerateReadLogic(Type type, string name, string indent = "            ")
        {
            if (type == typeof(int)) return $"{indent}this.{name} = _buf.ReadInt();";
            if (type == typeof(float)) return $"{indent}this.{name} = _buf.ReadFloat();";
            if (type == typeof(bool)) return $"{indent}this.{name} = _buf.ReadBool();";
            if (type == typeof(string)) return $"{indent}this.{name} = _buf.ReadString();";
            if (type == typeof(long)) return $"{indent}this.{name} = _buf.ReadLong();";
            if (type.IsEnum) return $"{indent}this.{name} = ({CleanTypeName(type)})_buf.ReadInt();";

            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
            {
                Type innerType = type.GetGenericArguments()[0];
                string innerTypeName = CleanTypeName(innerType);
                string countVar = $"count_{name.Replace(".", "")}";

                StringBuilder sb = new StringBuilder();
                sb.AppendLine($"{indent}int {countVar} = _buf.ReadSize();");
                sb.AppendLine($"{indent}this.{name} = new List<{innerTypeName}>({countVar});");
                sb.AppendLine($"{indent}for (int i = 0; i < {countVar}; i++)");
                sb.AppendLine($"{indent}{{");

                string tempRead = GenerateReadLogic(innerType, "tempItem", indent + "    ");
                sb.AppendLine($"{indent}    {innerTypeName} tempItem;");
                sb.AppendLine(tempRead.Replace("this.tempItem", "tempItem"));
                sb.AppendLine($"{indent}    this.{name}.Add(tempItem);");

                sb.Append($"{indent}}}");
                return sb.ToString();
            }

            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>))
            {
                Type keyType = type.GetGenericArguments()[0];
                Type valType = type.GetGenericArguments()[1];
                string keyName = CleanTypeName(keyType);
                string valName = CleanTypeName(valType);
                string countVar = $"count_{name.Replace(".", "")}";

                StringBuilder sb = new StringBuilder();
                sb.AppendLine($"{indent}int {countVar} = _buf.ReadSize();");
                sb.AppendLine($"{indent}this.{name} = new Dictionary<{keyName}, {valName}>({countVar});");
                sb.AppendLine($"{indent}for (int i = 0; i < {countVar}; i++)");
                sb.AppendLine($"{indent}{{");

                sb.AppendLine($"{indent}    {keyName} tempKey;");
                sb.AppendLine(GenerateReadLogic(keyType, "tempKey", indent + "    ").Replace("this.tempKey", "tempKey"));

                sb.AppendLine($"{indent}    {valName} tempVal;");
                sb.AppendLine(GenerateReadLogic(valType, "tempVal", indent + "    ").Replace("this.tempVal", "tempVal"));

                sb.AppendLine($"{indent}    this.{name}.Add(tempKey, tempVal);");
                sb.Append($"{indent}}}");
                return sb.ToString();
            }

            return $"{indent}this.{name} = new {CleanTypeName(type)}(_buf);";
        }

        private static string GenerateWriteLogic(Type type, string name, string indent = "            ")
        {
            if (type == typeof(int)) return $"{indent}_buf.WriteInt(this.{name});";
            if (type == typeof(float)) return $"{indent}_buf.WriteFloat(this.{name});";
            if (type == typeof(bool)) return $"{indent}_buf.WriteBool(this.{name});";
            if (type == typeof(string)) return $"{indent}_buf.WriteString(this.{name} ?? \"\");";
            if (type == typeof(long)) return $"{indent}_buf.WriteLong(this.{name});";
            if (type.IsEnum) return $"{indent}_buf.WriteInt((int)this.{name});";

            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
            {
                Type innerType = type.GetGenericArguments()[0];
                StringBuilder sb = new StringBuilder();

                sb.AppendLine($"{indent}_buf.WriteSize(this.{name}?.Count ?? 0);");
                sb.AppendLine($"{indent}if (this.{name} != null)");
                sb.AppendLine($"{indent}{{");
                sb.AppendLine($"{indent}    foreach (var item in this.{name})");
                sb.AppendLine($"{indent}    {{");

                string innerWrite = GenerateWriteLogic(innerType, "item", indent + "        ");
                sb.AppendLine(innerWrite.Replace("this.item", "item"));

                sb.AppendLine($"{indent}    }}");
                sb.Append($"{indent}}}");
                return sb.ToString();
            }

            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>))
            {
                Type keyType = type.GetGenericArguments()[0];
                Type valType = type.GetGenericArguments()[1];

                StringBuilder sb = new StringBuilder();
                sb.AppendLine($"{indent}_buf.WriteSize(this.{name}?.Count ?? 0);");
                sb.AppendLine($"{indent}if (this.{name} != null)");
                sb.AppendLine($"{indent}{{");
                sb.AppendLine($"{indent}    foreach (var kvp in this.{name})");
                sb.AppendLine($"{indent}    {{");

                sb.AppendLine(GenerateWriteLogic(keyType, "kvp.Key", indent + "        ").Replace("this.kvp.Key", "kvp.Key"));
                sb.AppendLine(GenerateWriteLogic(valType, "kvp.Value", indent + "        ").Replace("this.kvp.Value", "kvp.Value"));

                sb.AppendLine($"{indent}    }}");
                sb.Append($"{indent}}}");
                return sb.ToString();
            }

            if (type.IsValueType)
            {
                return $"{indent}this.{name}.Serialize(_buf);";
            }
            else
            {
                return $"{indent}this.{name}?.Serialize(_buf);";
            }
        }

        private static string CleanTypeName(Type type)
        {
            if (type == typeof(int)) return "int";
            if (type == typeof(float)) return "float";
            if (type == typeof(bool)) return "bool";
            if (type == typeof(string)) return "string";
            if (type == typeof(long)) return "long";

            if (type.IsGenericType)
            {
                string baseName = type.Name.Split('`')[0];
                var args = type.GetGenericArguments().Select(CleanTypeName);
                return $"{baseName}<{string.Join(", ", args)}>";
            }

            return type.Name.Replace("+", ".");
        }
    }
}