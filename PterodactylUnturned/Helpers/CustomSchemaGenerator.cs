using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace RestoreMonarchy.PterodactylUnturned.Helpers
{
    public class CustomSchemaGenerator
    {
        private readonly XDocument _xmlDoc;
        private readonly HashSet<Type> _processedTypes = new HashSet<Type>();

        public CustomSchemaGenerator(string xmlDocPath)
        {
            if (File.Exists(xmlDocPath))
            {
                _xmlDoc = XDocument.Load(xmlDocPath);
            }
        }

        public string GenerateSchema<T>(T instance = default)
        {
            _processedTypes.Clear();
            var type = typeof(T);

            object defaultInstance = instance;
            if (defaultInstance == null)
            {
                defaultInstance = Activator.CreateInstance(type);
            }

            var schema = GenerateSchemaForType(type, defaultInstance);
            return schema.ToString(Formatting.Indented);
        }

        private JObject GenerateSchemaForType(Type type, object instance)
        {
            if (_processedTypes.Contains(type))
            {
                // To prevent circular references, just return a reference schema
                return new JObject
                {
                    ["type"] = "object",
                    ["title"] = type.Name
                };
            }

            _processedTypes.Add(type);

            var schema = new JObject
            {
                ["title"] = type.Name,
                ["description"] = GetTypeDescription(type),
                ["type"] = "object",
                ["properties"] = JObject.FromObject(GenerateProperties(type, instance)),
                ["required"] = JArray.FromObject(GetRequiredProperties(type))
            };

            return schema;
        }

        private Dictionary<string, object> GenerateProperties(Type type, object instance)
        {
            var properties = new Dictionary<string, object>();

            // Process properties
            foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (prop.GetIndexParameters().Length > 0)
                    continue; // Skip indexers

                ProcessMember(properties, type, instance, prop.PropertyType, prop.Name,
                    () => prop.GetValue(instance),
                    memberName => GetPropertyDescription(type, prop));
            }

            // Process fields
            foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                ProcessMember(properties, type, instance, field.FieldType, field.Name,
                    () => field.GetValue(instance),
                    memberName => GetFieldDescription(type, field));
            }

            return properties;
        }

        private void ProcessMember(Dictionary<string, object> properties, Type type, object instance,
            Type memberType, string memberName, Func<object> getValue, Func<string, string> getDescription)
        {
            object memberValue = null;
            try
            {
                memberValue = getValue();
            }
            catch
            {
                // Just continue if we can't read the member
            }

            var memberSchema = new JObject();

            var jsonType = GetJsonType(memberType);
            memberSchema["type"] = jsonType;

            var description = getDescription(memberName);
            if (!string.IsNullOrEmpty(description))
            {
                memberSchema["description"] = description;
            }

            AddTypeConstraints(memberSchema, memberType);

            // Handle nested objects
            if (jsonType == "object" && !IsPrimitiveType(memberType))
            {
                if (memberValue != null)
                {
                    var nestedSchema = GenerateSchemaForType(memberType, memberValue);

                    // Copy all properties from the nested schema except "$schema"
                    foreach (var item in nestedSchema)
                    {
                        if (item.Key != "$schema")
                        {
                            memberSchema[item.Key] = item.Value;
                        }
                    }
                }
                else
                {
                    memberSchema["properties"] = new JObject();
                }
            }
            // Handle arrays of complex objects
            else if (jsonType == "array")
            {
                Type elementType = GetElementType(memberType);
                if (elementType != null && !IsPrimitiveType(elementType))
                {
                    try
                    {
                        object elementInstance = Activator.CreateInstance(elementType);
                        var itemSchema = GenerateSchemaForType(elementType, elementInstance);

                        // Create items object and copy properties
                        var items = new JObject
                        {
                            ["type"] = "object"
                        };

                        foreach (var item in itemSchema)
                        {
                            if (item.Key != "$schema" && item.Key != "title")
                            {
                                items[item.Key] = item.Value;
                            }
                        }

                        memberSchema["items"] = items;
                    }
                    catch
                    {
                        // Fallback for types we can't instantiate
                        memberSchema["items"] = new JObject
                        {
                            ["type"] = "object"
                        };
                    }
                }
                else if (elementType != null)
                {
                    // Handle arrays of primitive types
                    var items = new JObject
                    {
                        ["type"] = GetJsonType(elementType)
                    };
                    AddTypeConstraints(items, elementType);
                    memberSchema["items"] = items;
                }
            }

            // Add default value if available
            if (memberValue != null)
            {
                try
                {
                    if (memberType.IsEnum)
                    {
                        memberSchema["default"] = memberValue.ToString();
                    }
                    else if (memberType == typeof(DateTime))
                    {
                        memberSchema["default"] = ((DateTime)memberValue).ToString("o");
                    }
                    else if (IsPrimitiveType(memberType))
                    {
                        memberSchema["default"] = JToken.FromObject(memberValue);
                    }
                    // Don't add default for complex objects - too verbose
                }
                catch
                {
                    // Skip if we can't convert the default value
                }
            }

            properties[memberName] = memberSchema;
        }

        private bool IsPrimitiveType(Type type)
        {
            if (type.IsPrimitive || type.IsEnum || type == typeof(string) || type == typeof(decimal) ||
                type == typeof(DateTime) || type == typeof(TimeSpan) || type == typeof(Guid) ||
                type == typeof(DateTimeOffset))
                return true;

            var nullableType = Nullable.GetUnderlyingType(type);
            if (nullableType != null)
                return IsPrimitiveType(nullableType);

            return false;
        }

        private Type GetElementType(Type type)
        {
            if (type.IsArray)
                return type.GetElementType();

            if (type.IsGenericType)
            {
                var genericArgs = type.GetGenericArguments();
                if (genericArgs.Length > 0)
                    return genericArgs[0];
            }

            var interfaces = type.GetInterfaces();
            foreach (var interfaceType in interfaces)
            {
                if (interfaceType.IsGenericType &&
                    interfaceType.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                {
                    return interfaceType.GetGenericArguments()[0];
                }
            }

            return null;
        }

        private string GetJsonType(Type type)
        {
            Type underlyingType = Nullable.GetUnderlyingType(type) ?? type;

            if (underlyingType == typeof(string) || underlyingType == typeof(char) ||
                underlyingType == typeof(Guid))
                return "string";
            else if (underlyingType == typeof(int) || underlyingType == typeof(long) ||
                     underlyingType == typeof(short) || underlyingType == typeof(byte) ||
                     underlyingType == typeof(uint) || underlyingType == typeof(ulong) ||
                     underlyingType == typeof(ushort) || underlyingType == typeof(sbyte))
                return "integer";
            else if (underlyingType == typeof(float) || underlyingType == typeof(double) ||
                     underlyingType == typeof(decimal))
                return "number";
            else if (underlyingType == typeof(bool))
                return "boolean";
            else if (underlyingType == typeof(DateTime) || underlyingType == typeof(DateTimeOffset))
                return "string";
            else if (underlyingType.IsArray || IsEnumerableType(underlyingType))
                return "array";
            else if (underlyingType.IsEnum)
                return "string";
            else
                return "object";
        }

        private bool IsEnumerableType(Type type)
        {
            return type.GetInterfaces()
                .Any(i => i.IsGenericType &&
                     i.GetGenericTypeDefinition() == typeof(IEnumerable<>));
        }

        private void AddTypeConstraints(JObject propSchema, Type propType)
        {
            Type underlyingType = Nullable.GetUnderlyingType(propType) ?? propType;

            if (underlyingType == typeof(byte))
            {
                propSchema["minimum"] = 0;
                propSchema["maximum"] = byte.MaxValue;
            }
            else if (underlyingType == typeof(sbyte))
            {
                propSchema["minimum"] = sbyte.MinValue;
                propSchema["maximum"] = sbyte.MaxValue;
            }
            else if (underlyingType == typeof(short))
            {
                propSchema["minimum"] = short.MinValue;
                propSchema["maximum"] = short.MaxValue;
            }
            else if (underlyingType == typeof(ushort))
            {
                propSchema["minimum"] = 0;
                propSchema["maximum"] = ushort.MaxValue;
            }
            else if (underlyingType == typeof(int))
            {
                propSchema["minimum"] = int.MinValue;
                propSchema["maximum"] = int.MaxValue;
            }
            else if (underlyingType == typeof(uint))
            {
                propSchema["minimum"] = 0;
                propSchema["maximum"] = uint.MaxValue;
            }
            else if (underlyingType == typeof(long))
            {
                propSchema["minimum"] = long.MinValue;
                propSchema["maximum"] = long.MaxValue;
            }
            else if (underlyingType == typeof(ulong))
            {
                propSchema["minimum"] = 0;
                propSchema["maximum"] = ulong.MaxValue;
            }
            else if (underlyingType == typeof(float))
            {
                propSchema["type"] = "number";
            }
            else if (underlyingType == typeof(double))
            {
                propSchema["type"] = "number";
            }
            else if (underlyingType == typeof(decimal))
            {
                propSchema["type"] = "number";
            }
            else if (underlyingType == typeof(DateTime) || underlyingType == typeof(DateTimeOffset))
            {
                propSchema["format"] = "date-time";
            }
            else if (underlyingType.IsEnum)
            {
                propSchema["enum"] = JArray.FromObject(Enum.GetNames(underlyingType));
            }
        }

        private string[] GetRequiredProperties(Type type)
        {
            var requiredMembers = new List<string>();

            // Add required properties
            requiredMembers.AddRange(type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => {
                    if (p.GetIndexParameters().Length > 0)
                        return false; // Skip indexers

                    Type propType = Nullable.GetUnderlyingType(p.PropertyType) ?? p.PropertyType;
                    return propType.IsValueType && Nullable.GetUnderlyingType(p.PropertyType) == null;
                })
                .Select(p => p.Name));

            // Add required fields
            requiredMembers.AddRange(type.GetFields(BindingFlags.Public | BindingFlags.Instance)
                .Where(f => {
                    Type fieldType = Nullable.GetUnderlyingType(f.FieldType) ?? f.FieldType;
                    return fieldType.IsValueType && Nullable.GetUnderlyingType(f.FieldType) == null;
                })
                .Select(f => f.Name));

            return requiredMembers.ToArray();
        }

        private string GetTypeDescription(Type type)
        {
            if (_xmlDoc == null)
                return null;

            string memberName = $"T:{type.FullName}";
            var memberElement = _xmlDoc.Descendants("member")
                .FirstOrDefault(m => m.Attribute("name")?.Value == memberName);

            if (memberElement == null)
                return null;

            var summaryElement = memberElement.Element("summary");
            if (summaryElement == null)
                return null;

            return CleanXmlDocComment(summaryElement.Value);
        }

        private string GetPropertyDescription(Type type, PropertyInfo property)
        {
            if (_xmlDoc == null)
                return null;

            string memberName = $"P:{type.FullName}.{property.Name}";
            var memberElement = _xmlDoc.Descendants("member")
                .FirstOrDefault(m => m.Attribute("name")?.Value == memberName);

            if (memberElement == null)
                return null;

            var summaryElement = memberElement.Element("summary");
            if (summaryElement == null)
                return null;

            return CleanXmlDocComment(summaryElement.Value);
        }

        private string GetFieldDescription(Type type, FieldInfo field)
        {
            if (_xmlDoc == null)
                return null;

            string memberName = $"F:{type.FullName}.{field.Name}";
            var memberElement = _xmlDoc.Descendants("member")
                .FirstOrDefault(m => m.Attribute("name")?.Value == memberName);

            if (memberElement == null)
                return null;

            var summaryElement = memberElement.Element("summary");
            if (summaryElement == null)
                return null;

            return CleanXmlDocComment(summaryElement.Value);
        }

        private string CleanXmlDocComment(string comment)
        {
            if (string.IsNullOrEmpty(comment))
                return comment;

            var lines = comment.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(line => line.Trim())
                .Where(line => !string.IsNullOrWhiteSpace(line));

            return string.Join(" ", lines);
        }
    }
}