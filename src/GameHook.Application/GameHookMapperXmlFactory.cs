using GameHook.Application.GameHookProperties;
using GameHook.Domain;
using GameHook.Domain.Interfaces;
using System.Data;
using System.Xml;
using System.Xml.Linq;

namespace GameHook.Application
{
    static class GameHookMapperXmlHelpers
    {
        public static IEnumerable<XAttribute> GetAttributesWithVars(this XDocument doc)
        {
            var properties = doc.Descendants("properties") ?? throw new Exception("Unable to locate <properties>");

            return properties
                .Descendants()
                .Attributes()
                .Where(x => x.Name.NamespaceName == "https://schemas.gamehook.io/attributes/var");
        }

        static string[] AttributesThatCanBeNormalized { get; } = new string[] { "address", "preprocessor" };
        public static List<XAttribute> GetAttributesThatCanBeNormalized(this XDocument doc)
        {
            var properties = doc.Descendants("properties") ?? throw new Exception("Unable to locate <properties>");

            return properties
                .Descendants()
                .Attributes()
                .Where(x => AttributesThatCanBeNormalized.Contains(x.Name.LocalName))
                .Where(x => x.Value.Contains("0x"))
                .ToList();
        }
    }

    public static class GameHookMapperXmlFactory
    {
        static ulong ToULong(string value)
        {
            try
            {
                if (value.StartsWith("0x"))
                {
                    return Convert.ToUInt64(value, 16);
                }

                return Convert.ToUInt64(value);
            }
            catch (Exception ex)
            {
                throw new Exception($"Unable to translate {value} into a ulong.", ex);
            }
        }

        public static MapperMetadata GetMetadata(XDocument doc)
        {
            var root = doc.Element("mapper") ?? throw new Exception($"Unable to find <mapper> root element.");

            return new MapperMetadata()
            {
                Id = Guid.Parse(root.GetAttributeValue("id")),
                GameName = root.GetAttributeValue("name"),
                GamePlatform = root.GetAttributeValue("platform")
            };
        }

        static IEnumerable<IGameHookProperty> GetProperties(XDocument doc, IGameHookInstance? instance)
        {
            return doc.Descendants("properties").Descendants("property")
                .Select<XElement, IGameHookProperty>(x =>
                {
                    try
                    {
                        if (instance == null) { throw new Exception("Instance is null."); }

                        var type = x.GetAttributeValue("type");

                        var variables = new GameHookMapperVariables()
                        {
                            Path = x.GetElementPath(),
                            Type = x.GetAttributeValue("type"),
                            MemoryContainer = x.GetOptionalAttributeValue("memoryContainer"),
                            Address = x.GetOptionalAttributeValue("address"),
                            Length = x.GetOptionalAttributeValueAsInt("length") ?? 1,
                            Size = x.GetOptionalAttributeValueAsInt("size") ?? 1,
                            Position = x.GetOptionalAttributeValueAsInt("position"),
                            Reference = x.GetOptionalAttributeValue("reference"),
                            Description = x.GetOptionalAttributeValue("description"),
                            StaticValue = x.GetOptionalAttributeValue("value"),
                            ReadFunction = x.GetOptionalAttributeValue("read-function"),
                            WriteFunction = x.GetOptionalAttributeValue("write-function"),
                        };

                        if (type == "binaryCodedDecimal") return new BinaryCodedDecimalProperty(instance, variables);
                        else if (type == "bitArray") return new BitFieldProperty(instance, variables);
                        else if (type == "bit") return new BitProperty(instance, variables);
                        else if (type == "bool") return new BooleanProperty(instance, variables);
                        else if (type == "int") return new IntegerProperty(instance, variables);
                        else if (type == "nibble") return new NibbleProperty(instance, variables);
                        else if (type == "string") return new StringProperty(instance, variables);
                        else if (type == "uint") return new UnsignedIntegerProperty(instance, variables);
                        else throw new Exception($"Unknown property type {type}.");
                    }
                    catch (Exception ex)
                    {
                        throw new Exception($"Unable to parse {x.GetElementPath()}. {x}", innerException: ex);
                    }
                })
                .ToArray();
        }

        public static IEnumerable<GlossaryList> GetGlossary(XDocument doc)
        {
            return doc.Descendants("references")
                .Elements()
                .Select(el =>
                {
                    var name = el.Name.LocalName;
                    var type = el.GetOptionalAttributeValue("type") ?? "string";

                    return new GlossaryList()
                    {
                        Name = name,
                        Type = type,
                        Values = el.Elements().Select(y =>
                        {
                            var key = ToULong(y.GetAttributeValue("key"));
                            object? value = null;

                            var valueStr = y.GetOptionalAttributeValue("value");
                            if (string.IsNullOrEmpty(valueStr)) { value = null; }
                            else if (type == "string") { value = valueStr; }
                            else if (type == "number") { value = int.Parse(valueStr); }
                            else throw new Exception($"Unknown type for reference list {type}.");

                            return new GlossaryListItem(key, value);
                        }).ToArray()
                    };
                });
        }

        public static GameHookMapper LoadMapperFromFile(IGameHookInstance? instance, string mapperContents, string? scriptContents)
        {
            var doc = XDocument.Parse(mapperContents);

            // Apply Macros
            var destinationMacros = doc.Descendants("macro").ToArray();
            foreach (var destinationMacro in destinationMacros)
            {
                var macroType = destinationMacro.GetAttributeValue("type");
                var sourceMacro = doc.Descendants("macros").Descendants(macroType).FirstOrDefault() ??
                                  throw new XmlException($"Unable to find macro in <macros> tag of {macroType}.");

                destinationMacro.ReplaceWith(sourceMacro.Elements());
            }

            // Apply Classes.
            var destinationClasses = doc.Descendants("properties").Descendants("class");
            foreach (var destinationClass in destinationClasses)
            {
                var classType = destinationClass.GetAttributeValue("type");
                var sourceClass = doc.Descendants("classes").Descendants(classType).FirstOrDefault() ??
                                  throw new XmlException($"Unable to find class in <classes> tag of {classType}.");

                destinationClass.ReplaceNodes(sourceClass.Elements());
            }

            // Apply static variable replacement.
            var attributesWithVars = doc.GetAttributesWithVars();
            foreach (var attr in attributesWithVars)
            {
                if (attr.Parent == null) throw new Exception($"Cannot get parent from attribute {attr}.");

                var varName = attr.Name.LocalName;
                var varValue = attr.Value;

                foreach (var attribute in attr.Parent.Descendants().Attributes())
                {
                    if (attribute.Name.LocalName == "name") { continue; }
                    if (attribute.Name.LocalName == "type") { continue; }

                    attribute.Value = attribute.Value.Replace(varName, varValue);
                }
            }

            // Apply normalization of hexdecimals.
            foreach (var attr in doc.GetAttributesThatCanBeNormalized())
            {
                attr.Value = attr.Value.NormalizeMemoryAddresses();
            }

            var hasGlobalPreprocessor = scriptContents?.Contains("function preprocessor(") ?? false;
            var hasGlobalPostprocessor = scriptContents?.Contains("function postprocessor(") ?? false;

            return new GameHookMapper(MapperFormats.XML, GetMetadata(doc), GetProperties(doc, instance), GetGlossary(doc), scriptContents, hasGlobalPreprocessor, hasGlobalPostprocessor);
        }
    }
}